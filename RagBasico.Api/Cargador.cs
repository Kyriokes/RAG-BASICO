using Dapper;
using Microsoft.Data.SqlClient;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace RagBasico.Api;

/// <summary>Procesa un documento pendiente de punta a punta: saca el texto del PDF, lo corta, guarda los fragmentos, calcula sus vectores y los sube a Qdrant.</summary>
public sealed class Cargador
{
    // Un PDF con texto tiene cientos de palabras por página. Menos de esto en promedio, es un escaneo.
    private const int PalabrasPorPaginaMinimas = 40;

    // Cuánto se espera a que PdfPig saque el texto de un PDF. Uno mal armado puede dejarlo dando vueltas sin terminar.
    private static readonly TimeSpan EsperaDeLectura = TimeSpan.FromMinutes(5);

    // Por dónde va el documento que se está procesando, o null si no hay ninguno. Lo lee GET /documentos para mostrarlo en la página.
    // Vive en memoria y no en la base: si se apaga la API, el documento se procesa de nuevo desde el principio.
    private static volatile AvanceDeCarga? _enCurso;

    /// <summary>Por dónde va el documento que se está procesando, o null si no se está procesando ninguno.</summary>
    public static AvanceDeCarga? EnCurso
    {
        get { return _enCurso; }
    }

    private readonly Documentos _documentos;
    private readonly Ollama _ollama;
    private readonly Qdrant _qdrant;
    private readonly ILogger<Cargador> _log;
    private readonly string _conexion;
    private readonly int _palabras;
    private readonly int _solapamiento;
    private readonly int _lote;

    /// <summary>Recibe el registro de documentos, los clientes de Ollama y de Qdrant y el log; lee de la configuración la conexión, el tamaño de los fragmentos y cuántos manda a Ollama en cada pedido.</summary>
    public Cargador(Documentos documentos, Ollama ollama, Qdrant qdrant, ILogger<Cargador> log, IConfiguration config)
    {
        _documentos = documentos;
        _ollama = ollama;
        _qdrant = qdrant;
        _log = log;
        _conexion = config["Conexiones:SqlTextos"] ?? throw new InvalidOperationException("Falta Conexiones:SqlTextos en la configuración.");
        _palabras = config.GetValue("Rag:PalabrasPorFragmento", 300);
        _solapamiento = config.GetValue("Rag:Solapamiento", 50);
        _lote = Math.Max(1, config.GetValue("Rag:LoteVectores", 16));
    }

    /// <summary>Procesa el próximo documento pendiente. Devuelve false si no había ninguno. Si el problema es del documento, lo deja en error y devuelve true; si es de un servidor, o puede ser pasajero, lo deja pendiente y lanza el error.</summary>
    public async Task<bool> ProcesarSiguienteAsync(CancellationToken ct)
    {
        Pendiente? pendiente = await _documentos.SiguientePendienteAsync(ct);
        if (pendiente == null)
        {
            return false;
        }
        // El turno: mientras se procesa un documento, nadie más le toca los fragmentos ni los vectores.
        await Documentos.Turno.WaitAsync(ct);
        try
        {
            await ProcesarAsync(pendiente, ct);
        }
        finally
        {
            // Termine bien o mal, ya no se está procesando ese documento.
            _enCurso = null;
            Documentos.Turno.Release();
        }
        return true;
    }

    /// <summary>Dice si un error es de un servidor y no del documento: Ollama, Qdrant o la base de origen que no responden, cualquier error de Qdrant (una clave mal puesta, por ejemplo), un error de configuración (un modelo de Ollama que no está bajado, por ejemplo) o un error de SQL Server que no sea de tiempo. El documento queda pendiente y no gasta un intento.</summary>
    public static bool EsDeUnServidor(Exception error)
    {
        if (Errores.EsCaida(error))
        {
            return true;
        }
        ErrorDeServicio? deServicio = error as ErrorDeServicio;
        if (deServicio != null)
        {
            // Qdrant no rechaza un documento en particular: si contesta con un error, es la configuración o el servidor.
            // Y un error de configuración se repite con cualquier documento: hay que corregirla, no pasar al siguiente.
            return deServicio.Servicio == "Qdrant" || deServicio.DeConfiguracion;
        }
        SqlException? deSql = error as SqlException;
        return deSql != null && deSql.Number != NumeroDeTiempoVencido;
    }

    /// <summary>Dice si un error puede ser pasajero o puede ser del documento, y no se sabe cuál: un error que contestó Ollama, la base de origen que tardó demasiado o una consulta de SQL Server que venció. Se reintenta, y al tercer intento el documento queda en error.</summary>
    public static bool EsPasajero(Exception error)
    {
        if (EsDeUnServidor(error))
        {
            return false;
        }
        SqlException? deSql = error as SqlException;
        return error is ErrorDeServicio || (deSql != null && deSql.Number == NumeroDeTiempoVencido);
    }

    // El número con que SQL Server avisa que se venció el tiempo de espera de una consulta.
    private const int NumeroDeTiempoVencido = -2;

    /// <summary>Los pasos de un documento, en orden: contar el intento, borrar lo que hubiera quedado a medias, leer el PDF, sacar el texto, cortarlo, guardar los fragmentos, calcular los vectores, subirlos a Qdrant y marcarlo listo.</summary>
    private async Task ProcesarAsync(Pendiente pendiente, CancellationToken ct)
    {
        try
        {
            // 0. Los intentos. Si este documento ya se empezó tres veces y nunca terminó, es que hace caer o colgar
            //    el proceso: queda en error y se sigue con los demás. Si no, se anota que se empieza.
            if (pendiente.Intentos >= Documentos.IntentosMaximos)
            {
                // El intento que se cortó pudo dejar fragmentos y vectores: como no se va a procesar de nuevo, se borran acá.
                await BorrarVectoresSinFallarAsync(pendiente.DocumentoId);
                await BorrarFragmentosSinFallarAsync(pendiente.DocumentoId);
                await _documentos.MarcarAsync(pendiente.DocumentoId, "error", "se empezó a procesar " + pendiente.Intentos + " veces y no terminó: ¿un PDF que cuelga el lector?", ct);
                return;
            }
            await _documentos.SumarIntentoAsync(pendiente.DocumentoId, ct);

            // 1. Si un intento anterior quedó a medias, o el PDF cambió, se empieza de cero.
            await _qdrant.BorrarDeDocumentoAsync(pendiente.DocumentoId, ct);
            await BorrarFragmentosAsync(pendiente.DocumentoId, ct);

            // 2. Los bytes del PDF: de la base del RAG si se arrastró, de la base de origen si se importó.
            //    Con la base de origen lenta puede tardar: desde acá la página ya muestra qué documento se está procesando.
            _enCurso = new AvanceDeCarga(pendiente.Archivo, "trayendo el PDF", 0, 0);
            byte[]? contenido = await _documentos.LeerContenidoAsync(pendiente, ct);
            if (contenido == null)
            {
                string falta = pendiente.OrigenId == null ? "no tiene el PDF guardado" : "no está en la base de origen";
                await _documentos.MarcarAsync(pendiente.DocumentoId, "error", falta, ct);
                return;
            }
            if (!Documentos.EsPdf(contenido))
            {
                await _documentos.MarcarAsync(pendiente.DocumentoId, "error", "no es un PDF", ct);
                return;
            }

            // 3. El texto, una página por elemento. Un PDF roto o con clave falla acá.
            //    PdfPig no avisa por qué página va: este paso se muestra sin cantidades.
            _enCurso = new AvanceDeCarga(pendiente.Archivo, "leyendo el PDF", 0, 0);
            List<string> paginas;
            try
            {
                // En otro hilo y con un tope de tiempo: si PdfPig no termina, el documento queda en error y la carga sigue.
                paginas = await Task.Run(() => SacarTexto(contenido)).WaitAsync(EsperaDeLectura, ct);
            }
            catch (TimeoutException)
            {
                await _documentos.MarcarAsync(pendiente.DocumentoId, "error", "no se pudo leer el PDF: tardó más de " + (int)EsperaDeLectura.TotalMinutes + " minutos", ct);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _documentos.MarcarAsync(pendiente.DocumentoId, "error", "no se pudo leer el PDF: " + ex.Message, ct);
                return;
            }
            if (EsEscaneado(paginas))
            {
                await _documentos.MarcarAsync(pendiente.DocumentoId, "sin texto", paginas.Count + " páginas casi sin texto: ¿es un escaneo?", ct);
                return;
            }

            // 4. Los fragmentos, guardados en SQL con la página donde empiezan. Su número es el id del vector en Qdrant.
            _enCurso = new AvanceDeCarga(pendiente.Archivo, "cortando el texto", 0, 0);
            List<Parte> partes = Chunker.PartirPaginas(paginas, _palabras, _solapamiento);
            List<FragmentoNuevo> fragmentos = await GuardarFragmentosAsync(pendiente, partes, ct);

            // 5. Los vectores, de a lotes: se piden a Ollama y se suben a Qdrant. Es el paso largo:
            //    después de cada lote se anota cuántos fragmentos van, para que la página muestre la barra.
            _enCurso = new AvanceDeCarga(pendiente.Archivo, "calculando los vectores", 0, fragmentos.Count);
            await _qdrant.AsegurarColeccionAsync(ct);
            for (int i = 0; i < fragmentos.Count; i += _lote)
            {
                List<FragmentoNuevo> lote = fragmentos.Skip(i).Take(_lote).ToList();
                List<float[]> vectores = await _ollama.EmbeddingsAsync(lote.Select(f => f.Texto).ToList(), ct);
                List<PuntoQdrant> puntos = new List<PuntoQdrant>();
                for (int j = 0; j < lote.Count; j++)
                {
                    puntos.Add(new PuntoQdrant(lote[j].FragmentoId, vectores[j], pendiente.DocumentoId));
                }
                await _qdrant.SubirAsync(puntos, ct);
                _enCurso = new AvanceDeCarga(pendiente.Archivo, "calculando los vectores", i + lote.Count, fragmentos.Count);
            }

            // 6. Listo: recién ahora aparece en las búsquedas. Si el texto salió dudoso, queda anotado.
            //    Se marca solo si sigue siendo el mismo PDF: si mientras tanto lo reemplazaron o lo borraron, lo que se armó no sirve.
            string duda = RevisarTexto(paginas);
            bool marcado = await _documentos.MarcarListoAsync(pendiente, duda == "" ? null : "texto dudoso: " + duda, ct);
            if (!marcado)
            {
                _log.LogInformation("{Archivo} cambió o se borró mientras se procesaba: se descarta lo armado.", pendiente.Archivo);
                await _qdrant.BorrarDeDocumentoAsync(pendiente.DocumentoId, ct);
                await BorrarFragmentosAsync(pendiente.DocumentoId, ct);
            }
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            // Pase lo que pase, no quedan fragmentos a medias.
            await BorrarFragmentosSinFallarAsync(pendiente.DocumentoId);
            if (EsDeUnServidor(ex))
            {
                // No es culpa del documento: queda pendiente, se le devuelve el intento y el error sale,
                // para que el Procesador espere y reintente.
                await DevolverIntentoSinFallarAsync(pendiente.DocumentoId);
                throw;
            }
            if (EsPasajero(ex) && pendiente.Intentos + 1 < Documentos.IntentosMaximos)
            {
                // Puede ser pasajero: queda pendiente con el intento contado. Al tercero, queda en error.
                throw;
            }
            // El documento queda en error y no se vuelve a procesar solo: los vectores que llegó a subir no pueden quedar en Qdrant.
            await BorrarVectoresSinFallarAsync(pendiente.DocumentoId);
            _log.LogWarning(ex, "No se pudo cargar {Archivo}.", pendiente.Archivo);
            await _documentos.MarcarAsync(pendiente.DocumentoId, "error", ex.Message, ct);
        }
    }

    /// <summary>Guarda los fragmentos de un documento, con la página donde empieza cada uno y la página donde termina si cruza a otra, en una sola transacción. Devuelve cada uno con su número.</summary>
    private async Task<List<FragmentoNuevo>> GuardarFragmentosAsync(Pendiente pendiente, List<Parte> partes, CancellationToken ct)
    {
        string titulo = Path.GetFileNameWithoutExtension(pendiente.Archivo);
        List<FragmentoNuevo> fragmentos = new List<FragmentoNuevo>();
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            await conexion.OpenAsync(ct);
            using (SqlTransaction transaccion = conexion.BeginTransaction())
            {
                for (int i = 0; i < partes.Count; i++)
                {
                    // Cada fragmento empieza con el nombre del documento, así la búsqueda también lo encuentra por el nombre.
                    string texto = "Documento: " + titulo + "\n" + partes[i].Texto;
                    // La página de fin se guarda solo si el fragmento cruza a otra página: si no, queda en NULL.
                    int? paginaFin = null;
                    if (partes[i].PaginaFin != partes[i].Pagina)
                    {
                        paginaFin = partes[i].PaginaFin;
                    }
                    int fragmentoId = await conexion.ExecuteScalarAsync<int>(new CommandDefinition(
                        "INSERT INTO rag.Fragmento (DocumentoId, Orden, Texto, Pagina, PaginaFin) OUTPUT INSERTED.FragmentoId " +
                        "VALUES (@documentoId, @orden, @texto, @pagina, @paginaFin);",
                        new { documentoId = pendiente.DocumentoId, orden = i + 1, texto, pagina = partes[i].Pagina, paginaFin }, transaccion, cancellationToken: ct));
                    fragmentos.Add(new FragmentoNuevo(fragmentoId, texto));
                }
                transaccion.Commit();
            }
        }
        return fragmentos;
    }

    /// <summary>Borra de SQL los fragmentos de un documento.</summary>
    private async Task BorrarFragmentosAsync(int documentoId, CancellationToken ct)
    {
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            await conexion.ExecuteAsync(new CommandDefinition(
                "DELETE FROM rag.Fragmento WHERE DocumentoId = @documentoId;", new { documentoId }, cancellationToken: ct));
        }
    }

    /// <summary>Borra los fragmentos de un documento después de un error. Si tampoco se puede, no importa: se borran al procesarlo de nuevo.</summary>
    private async Task BorrarFragmentosSinFallarAsync(int documentoId)
    {
        try
        {
            await BorrarFragmentosAsync(documentoId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "No se pudieron borrar los fragmentos a medias del documento {DocumentoId}.", documentoId);
        }
    }

    /// <summary>Borra de Qdrant los vectores de un documento que queda en error. Si tampoco se puede, no importa: se borran al reintentarlo, y mientras tanto la búsqueda los descarta porque el documento no está listo.</summary>
    private async Task BorrarVectoresSinFallarAsync(int documentoId)
    {
        try
        {
            await _qdrant.BorrarDeDocumentoAsync(documentoId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "No se pudieron borrar de Qdrant los vectores a medias del documento {DocumentoId}.", documentoId);
        }
    }

    /// <summary>Le devuelve el intento a un documento después de un error de un servidor. Si tampoco se puede, no importa: a lo sumo le queda un intento menos.</summary>
    private async Task DevolverIntentoSinFallarAsync(int documentoId)
    {
        try
        {
            await _documentos.DevolverIntentoAsync(documentoId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "No se pudo devolver el intento del documento {DocumentoId}.", documentoId);
        }
    }

    /// <summary>Un fragmento recién guardado: su número y su texto.</summary>
    private sealed record FragmentoNuevo(int FragmentoId, string Texto);

    /// <summary>Saca el texto de un PDF con PdfPig, una página por elemento de la lista, en el orden en que se lee. Si el PDF está roto o tiene clave, lanza el error de PdfPig.</summary>
    public static List<string> SacarTexto(byte[] contenido)
    {
        // Una página sin texto también ocupa su lugar en la lista: el número de página de cada fragmento sale de la posición.
        List<string> paginas = new List<string>();
        using (PdfDocument pdf = PdfDocument.Open(contenido))
        {
            foreach (Page pagina in pdf.GetPages())
            {
                paginas.Add(ContentOrderTextExtractor.GetText(pagina, true));
            }
        }
        return paginas;
    }

    /// <summary>Dice si un PDF parece escaneado: menos de 40 palabras por página en promedio. Un escaneo con un sello de firma por página tiene algunas palabras, pero no tantas.</summary>
    public static bool EsEscaneado(List<string> paginas)
    {
        if (paginas.Count == 0)
        {
            return true;
        }
        int palabras = 0;
        foreach (string pagina in paginas)
        {
            palabras += Chunker.ContarPalabras(pagina);
        }
        return palabras < PalabrasPorPaginaMinimas * paginas.Count;
    }

    /// <summary>Busca señales de que el texto salió mal del PDF: caracteres que no son letras ni números, palabras pegadas o páginas casi vacías. Devuelve el motivo de la duda, o vacío si el texto parece bien.</summary>
    public static string RevisarTexto(List<string> paginas)
    {
        int palabras = 0;
        int caracteres = 0;
        int letrasYNumeros = 0;
        int paginasCasiVacias = 0;
        foreach (string pagina in paginas)
        {
            int palabrasDeLaPagina = Chunker.ContarPalabras(pagina);
            palabras += palabrasDeLaPagina;
            if (palabrasDeLaPagina < 20)
            {
                paginasCasiVacias++;
            }
            foreach (char letra in pagina)
            {
                if (!char.IsWhiteSpace(letra))
                {
                    caracteres++;
                    if (char.IsLetterOrDigit(letra))
                    {
                        letrasYNumeros++;
                    }
                }
            }
        }
        if (palabras == 0)
        {
            return "no tiene texto";
        }

        // En un texto normal, casi todo lo que no es espacio son letras y números. Si no, la fuente del PDF no se pudo leer bien.
        int porcentaje = letrasYNumeros * 100 / caracteres;
        if (porcentaje < 80)
        {
            return "solo el " + porcentaje + " % de los caracteres son letras o números: ¿la fuente del PDF no se puede leer?";
        }
        // Una palabra en castellano tiene unas 5 letras. Si el promedio pasa de 12, el PDF no marca los espacios.
        int largo = caracteres / palabras;
        if (largo > 12)
        {
            return "palabras pegadas, " + largo + " caracteres por palabra en promedio";
        }
        // Una página escaneada adentro de un PDF con texto, como un anexo, queda casi vacía.
        if (paginasCasiVacias * 4 >= paginas.Count)
        {
            return paginasCasiVacias + " de " + paginas.Count + " páginas casi sin texto: ¿tiene partes escaneadas?";
        }
        return "";
    }
}
