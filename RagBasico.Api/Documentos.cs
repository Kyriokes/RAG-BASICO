using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using UglyToad.PdfPig;

namespace RagBasico.Api;

/// <summary>El registro de los documentos: anota los que llegan, dice cuál sigue, guarda el estado de cada uno, los lista y los borra. No procesa: eso lo hace el Cargador.</summary>
public sealed class Documentos
{
    /// <summary>Los estados de un documento.</summary>
    public static readonly string[] Estados = new string[] { "pendiente", "listo", "sin texto", "error" };

    /// <summary>El turno para tocar los fragmentos y los vectores de un documento. Lo toma el Cargador mientras procesa uno, y también borrar y reprocesar: así no se pisan.</summary>
    public static readonly SemaphoreSlim Turno = new SemaphoreSlim(1, 1);

    // Cuántos documentos trae cada página de la lista.
    private const int PorPagina = 50;

    private readonly Qdrant _qdrant;
    private readonly string _conexion;
    private readonly string _conexionOrigen;
    private readonly string _consultaLista;
    private readonly string _consultaContenido;
    private readonly string _parametros;

    /// <summary>Recibe el cliente de Qdrant, para borrar los vectores de un documento, y lee de la configuración las conexiones, las dos consultas de la base de origen y el tamaño de los fragmentos.</summary>
    public Documentos(Qdrant qdrant, IConfiguration config)
    {
        _qdrant = qdrant;
        _conexion = config["Conexiones:SqlTextos"] ?? throw new InvalidOperationException("Falta Conexiones:SqlTextos en la configuración.");
        _conexionOrigen = config["Conexiones:SqlOrigen"] ?? "";
        _consultaLista = config["Conexiones:ConsultaLista"] ?? "";
        _consultaContenido = config["Conexiones:ConsultaContenido"] ?? "";
        _parametros = ParametrosDeCorte(config.GetValue("Rag:PalabrasPorFragmento", 300), config.GetValue("Rag:Solapamiento", 50));
    }

    /// <summary>Dice si unos bytes son un PDF: la marca "%PDF-" aparece dentro de los primeros 1024 bytes.</summary>
    public static bool EsPdf(byte[] contenido)
    {
        byte[] marca = Encoding.ASCII.GetBytes("%PDF-");
        int hasta = Math.Min(contenido.Length, 1024) - marca.Length;
        for (int i = 0; i <= hasta; i++)
        {
            bool igual = true;
            for (int j = 0; j < marca.Length; j++)
            {
                if (contenido[i + j] != marca[j])
                {
                    igual = false;
                    break;
                }
            }
            if (igual)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Calcula la huella de carga: el sha256 de los bytes del PDF seguidos de los parámetros de corte. Si cambia el PDF o cambia la forma de cortar, cambia la huella.</summary>
    public static byte[] CalcularHuella(byte[] contenido, string parametros)
    {
        using (IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            sha.AppendData(contenido);
            sha.AppendData(Encoding.UTF8.GetBytes(parametros));
            return sha.GetHashAndReset();
        }
    }

    /// <summary>Arma el texto con los parámetros que cambian el corte: la versión del Chunker, el tamaño, el solapamiento y la versión de PdfPig, que cambia el texto que se saca de cada PDF.</summary>
    public static string ParametrosDeCorte(int palabras, int solapamiento)
    {
        return "corte=" + Chunker.Version
            + ";palabras=" + palabras
            + ";solapamiento=" + solapamiento
            + ";pdfpig=" + typeof(PdfDocument).Assembly.GetName().Version;
    }

    /// <summary>Dice en palabras cuánto falta para terminar: minutos hasta 90, horas hasta 36 y días después. Vacío si no hay pendientes.</summary>
    public static string EstimarFalta(int pendientes, double porMinuto)
    {
        if (pendientes <= 0)
        {
            return "";
        }
        if (porMinuto <= 0)
        {
            return "sin datos todavía";
        }
        double minutos = pendientes / porMinuto;
        if (minutos < 1)
        {
            return "menos de un minuto";
        }
        if (minutos <= 90)
        {
            return "unos " + Math.Round(minutos) + " minutos";
        }
        double horas = minutos / 60;
        if (horas <= 36)
        {
            return "unas " + Math.Round(horas) + " horas";
        }
        return "unos " + Math.Round(horas / 24) + " días";
    }

    /// <summary>Anota los archivos que se arrastraron a la página. Un PDF nuevo queda pendiente; uno que ya estaba con el mismo contenido no se toca; uno que cambió se reemplaza y vuelve a pendiente; lo que no es un PDF se rechaza.</summary>
    public async Task<InformeAnotado> AnotarSubidosAsync(List<ArchivoSubido> archivos, CancellationToken ct)
    {
        int nuevos = 0;
        int sinCambios = 0;
        int reemplazados = 0;
        int rechazados = 0;
        List<string> detalle = new List<string>();

        // Si en la tanda vienen dos con el mismo nombre, vale el último.
        Dictionary<string, ArchivoSubido> porNombre = new Dictionary<string, ArchivoSubido>(StringComparer.OrdinalIgnoreCase);
        foreach (ArchivoSubido archivo in archivos)
        {
            string nombre = LimpiarNombre(archivo.Nombre);
            if (porNombre.ContainsKey(nombre))
            {
                detalle.Add(nombre + ": vino dos veces en la misma tanda; se tomó el último.");
            }
            porNombre[nombre] = new ArchivoSubido(nombre, archivo.Contenido);
        }

        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            foreach (ArchivoSubido archivo in porNombre.Values)
            {
                if (!EsPdf(archivo.Contenido))
                {
                    rechazados++;
                    detalle.Add(archivo.Nombre + ": no es un PDF; no se cargó.");
                    continue;
                }
                byte[] huella = CalcularHuella(archivo.Contenido, _parametros);
                Anterior? anterior = await conexion.QuerySingleOrDefaultAsync<Anterior>(new CommandDefinition(
                    "SELECT DocumentoId, Huella FROM rag.Documento WHERE OrigenId IS NULL AND Archivo = @archivo;",
                    new { archivo = archivo.Nombre }, cancellationToken: ct));
                if (anterior == null)
                {
                    await conexion.ExecuteAsync(new CommandDefinition(
                        "INSERT INTO rag.Documento (Archivo, Titulo, Contenido, Huella) VALUES (@archivo, @titulo, @contenido, @huella);",
                        new { archivo = archivo.Nombre, titulo = Titulo(archivo.Nombre), contenido = archivo.Contenido, huella }, cancellationToken: ct));
                    nuevos++;
                }
                else if (anterior.Huella != null && anterior.Huella.SequenceEqual(huella))
                {
                    sinCambios++;
                    detalle.Add(archivo.Nombre + ": ya estaba cargado, igual.");
                }
                else
                {
                    // Solo se guarda el contenido nuevo y vuelve a pendiente: los fragmentos y los vectores viejos los borra el Cargador
                    // antes de procesarlo. Mientras tanto no aparece en las búsquedas, porque no está listo.
                    await conexion.ExecuteAsync(new CommandDefinition(
                        "UPDATE rag.Documento SET Contenido = @contenido, Huella = @huella, Estado = 'pendiente', Detalle = NULL, FechaProceso = NULL " +
                        "WHERE DocumentoId = @id;",
                        new { contenido = archivo.Contenido, huella, id = anterior.DocumentoId }, cancellationToken: ct));
                    reemplazados++;
                    detalle.Add(archivo.Nombre + ": cambió; se reemplaza.");
                }
            }
        }
        return new InformeAnotado(nuevos, sinCambios, reemplazados, rechazados, detalle);
    }

    /// <summary>Anota como pendientes los documentos de la base de origen que todavía no están: corre la consulta de la lista, que devuelve el número y el nombre de cada uno, sin traer los PDF.</summary>
    public async Task<InformeAnotado> AnotarOrigenAsync(CancellationToken ct)
    {
        if (_conexionOrigen == "")
        {
            throw new ErrorDeServicio("SQL de origen", 400, "No hay base de origen configurada: completá Conexiones:SqlOrigen, Conexiones:ConsultaLista y Conexiones:ConsultaContenido.", null);
        }
        int nuevos = 0;
        int sinCambios = 0;
        using (SqlConnection conexion = new SqlConnection(_conexion))
        using (SqlConnection origen = new SqlConnection(_conexionOrigen))
        {
            // Los números de origen que ya están anotados, para no anotarlos dos veces.
            IEnumerable<string> anotados = await conexion.QueryAsync<string>(new CommandDefinition(
                "SELECT OrigenId FROM rag.Documento WHERE OrigenId IS NOT NULL;", cancellationToken: ct));
            HashSet<string> yaEstan = new HashSet<string>(anotados, StringComparer.OrdinalIgnoreCase);

            // La lista se lee de a una fila, sin cargarla entera en memoria, y se anota de a lotes de 1.000.
            List<object> lote = new List<object>();
            using (DbDataReader filas = await origen.ExecuteReaderAsync(new CommandDefinition(_consultaLista, cancellationToken: ct)))
            {
                while (await filas.ReadAsync(ct))
                {
                    string origenId = Convert.ToString(filas.GetValue(0), CultureInfo.InvariantCulture) ?? "";
                    if (origenId == "" || !yaEstan.Add(origenId))
                    {
                        sinCambios++;
                        continue;
                    }
                    string nombre = filas.IsDBNull(1) ? "" : (Convert.ToString(filas.GetValue(1), CultureInfo.InvariantCulture) ?? "");
                    nombre = nombre.Trim() == "" ? "documento-" + origenId + ".pdf" : LimpiarNombre(nombre);
                    lote.Add(new { archivo = nombre, titulo = Titulo(nombre), origenId });
                    if (lote.Count == 1000)
                    {
                        nuevos += await GuardarLoteAsync(conexion, lote, ct);
                        lote.Clear();
                    }
                }
            }
            if (lote.Count > 0)
            {
                nuevos += await GuardarLoteAsync(conexion, lote, ct);
            }
        }
        List<string> detalle = new List<string>();
        detalle.Add("De la base de origen: " + nuevos + " documentos nuevos quedaron pendientes; " + sinCambios + " ya estaban anotados.");
        return new InformeAnotado(nuevos, sinCambios, 0, 0, detalle);
    }

    /// <summary>Devuelve el próximo documento pendiente, o null si no hay. Los arrastrados van antes que los importados: así uno que se suelta en la página no espera detrás de miles.</summary>
    public async Task<Pendiente?> SiguientePendienteAsync(CancellationToken ct)
    {
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            return await conexion.QueryFirstOrDefaultAsync<Pendiente>(new CommandDefinition(
                "SELECT TOP (1) DocumentoId, Archivo, OrigenId FROM rag.Documento WHERE Estado = 'pendiente' " +
                "ORDER BY CASE WHEN OrigenId IS NULL THEN 0 ELSE 1 END, DocumentoId;",
                cancellationToken: ct));
        }
    }

    /// <summary>Trae los bytes del PDF: de la base del RAG si se arrastró, o de la base de origen, con la consulta del contenido, si se importó. Devuelve null si la fila ya no está o no tiene binario.</summary>
    public async Task<byte[]?> LeerContenidoAsync(Pendiente pendiente, CancellationToken ct)
    {
        if (pendiente.OrigenId == null)
        {
            using (SqlConnection conexion = new SqlConnection(_conexion))
            {
                return await conexion.ExecuteScalarAsync<byte[]?>(new CommandDefinition(
                    "SELECT Contenido FROM rag.Documento WHERE DocumentoId = @id;",
                    new { id = pendiente.DocumentoId }, cancellationToken: ct));
            }
        }
        if (_conexionOrigen == "")
        {
            throw new ErrorDeServicio("SQL de origen", 503, "Hay documentos importados pendientes, pero Conexiones:SqlOrigen está vacía: no se pueden leer.", null);
        }
        using (SqlConnection origen = new SqlConnection(_conexionOrigen))
        {
            object? valor = await origen.ExecuteScalarAsync<object?>(new CommandDefinition(
                _consultaContenido, new { Id = pendiente.OrigenId }, cancellationToken: ct));
            return valor as byte[];
        }
    }

    /// <summary>Guarda el estado de un documento y el motivo, si hay. Cuando deja de estar pendiente, anota la hora: con eso se calcula la velocidad.</summary>
    public async Task MarcarAsync(int documentoId, string estado, string? detalle, CancellationToken ct)
    {
        string? recortado = detalle != null && detalle.Length > 1000 ? detalle.Substring(0, 1000) : detalle;
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            await conexion.ExecuteAsync(new CommandDefinition(
                "UPDATE rag.Documento SET Estado = @estado, Detalle = @detalle, " +
                "FechaProceso = CASE WHEN @estado = 'pendiente' THEN NULL ELSE SYSDATETIME() END WHERE DocumentoId = @id;",
                new { estado, detalle = recortado, id = documentoId }, cancellationToken: ct));
        }
    }

    /// <summary>Cuenta los documentos por estado, calcula la velocidad de los últimos 10 minutos y lo que falta, y trae una página de 50 documentos, el más nuevo primero, de todos o de un solo estado.</summary>
    public async Task<ResumenDocumentos> ResumirAsync(string? estado, int pagina, CancellationToken ct)
    {
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            Cuenta cuenta = await conexion.QuerySingleAsync<Cuenta>(new CommandDefinition(
                "SELECT " +
                "(SELECT COUNT(*) FROM rag.Documento WHERE Estado = 'listo') AS Listos, " +
                "(SELECT COUNT(*) FROM rag.Documento WHERE Estado = 'pendiente') AS Pendientes, " +
                "(SELECT COUNT(*) FROM rag.Documento WHERE Estado = 'sin texto') AS SinTexto, " +
                "(SELECT COUNT(*) FROM rag.Documento WHERE Estado = 'error') AS ConError, " +
                "(SELECT COUNT(*) FROM rag.Fragmento) AS Fragmentos, " +
                "(SELECT COUNT(*) FROM rag.Documento WHERE FechaProceso >= DATEADD(MINUTE, -10, SYSDATETIME())) AS Recientes;",
                cancellationToken: ct));
            IEnumerable<DocumentoFila> filas = await conexion.QueryAsync<DocumentoFila>(new CommandDefinition(
                "SELECT d.DocumentoId, d.Archivo, d.OrigenId, d.Estado, d.Detalle, " +
                "(SELECT COUNT(*) FROM rag.Fragmento AS f WHERE f.DocumentoId = d.DocumentoId) AS Fragmentos, d.FechaCarga " +
                "FROM rag.Documento AS d WHERE (@estado IS NULL OR d.Estado = @estado) " +
                "ORDER BY d.DocumentoId DESC OFFSET @saltar ROWS FETCH NEXT @cuantos ROWS ONLY;",
                new { estado, saltar = (Math.Max(1, pagina) - 1) * PorPagina, cuantos = PorPagina }, cancellationToken: ct));

            double porMinuto = cuenta.Recientes / 10.0;
            return new ResumenDocumentos(cuenta.Listos, cuenta.Pendientes, cuenta.SinTexto, cuenta.ConError, cuenta.Fragmentos,
                porMinuto, EstimarFalta(cuenta.Pendientes, porMinuto), filas.ToList());
        }
    }

    /// <summary>Borra un documento: primero sus vectores de Qdrant y después su fila, que se lleva sus fragmentos. Devuelve false si no existe.</summary>
    public async Task<bool> BorrarAsync(int documentoId, CancellationToken ct)
    {
        await Turno.WaitAsync(ct);
        try
        {
            using (SqlConnection conexion = new SqlConnection(_conexion))
            {
                int existe = await conexion.ExecuteScalarAsync<int>(new CommandDefinition(
                    "SELECT COUNT(*) FROM rag.Documento WHERE DocumentoId = @id;", new { id = documentoId }, cancellationToken: ct));
                if (existe == 0)
                {
                    return false;
                }
                await _qdrant.BorrarDeDocumentoAsync(documentoId, ct);
                await conexion.ExecuteAsync(new CommandDefinition(
                    "DELETE FROM rag.Documento WHERE DocumentoId = @id;", new { id = documentoId }, cancellationToken: ct));
                return true;
            }
        }
        finally
        {
            Turno.Release();
        }
    }

    /// <summary>Pasa a pendiente los documentos que quedaron con error, para que se procesen de nuevo. Devuelve cuántos.</summary>
    public async Task<int> ReintentarAsync(CancellationToken ct)
    {
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            return await conexion.ExecuteAsync(new CommandDefinition(
                "UPDATE rag.Documento SET Estado = 'pendiente', Detalle = NULL, FechaProceso = NULL WHERE Estado = 'error';",
                cancellationToken: ct));
        }
    }

    /// <summary>Rehace todo desde los PDF: borra la colección de Qdrant y todos los fragmentos, y deja todos los documentos pendientes. Devuelve cuántos.</summary>
    public async Task<int> ReprocesarTodoAsync(CancellationToken ct)
    {
        await Turno.WaitAsync(ct);
        try
        {
            if (await _qdrant.ExisteColeccionAsync(ct))
            {
                await _qdrant.BorrarColeccionAsync(ct);
            }
            using (SqlConnection conexion = new SqlConnection(_conexion))
            {
                // Con muchos fragmentos el borrado tarda: se le da hasta 10 minutos.
                await conexion.ExecuteAsync(new CommandDefinition(
                    "DELETE FROM rag.Fragmento;", commandTimeout: 600, cancellationToken: ct));
                return await conexion.ExecuteAsync(new CommandDefinition(
                    "UPDATE rag.Documento SET Estado = 'pendiente', Detalle = NULL, FechaProceso = NULL;",
                    commandTimeout: 600, cancellationToken: ct));
            }
        }
        finally
        {
            Turno.Release();
        }
    }

    /// <summary>Guarda un lote de documentos importados, todos en una transacción. Devuelve cuántos guardó.</summary>
    private static async Task<int> GuardarLoteAsync(SqlConnection conexion, List<object> lote, CancellationToken ct)
    {
        if (conexion.State != System.Data.ConnectionState.Open)
        {
            await conexion.OpenAsync(ct);
        }
        using (SqlTransaction transaccion = conexion.BeginTransaction())
        {
            int guardados = await conexion.ExecuteAsync(new CommandDefinition(
                "INSERT INTO rag.Documento (Archivo, Titulo, OrigenId) VALUES (@archivo, @titulo, @origenId);",
                lote, transaccion, cancellationToken: ct));
            transaccion.Commit();
            return guardados;
        }
    }

    /// <summary>Deja solo el nombre del archivo, sin carpetas, y lo corta a 260 caracteres, que es lo que entra en la columna.</summary>
    private static string LimpiarNombre(string nombre)
    {
        string limpio = nombre.Replace('\\', '/');
        limpio = limpio.Substring(limpio.LastIndexOf('/') + 1).Trim();
        if (limpio == "")
        {
            limpio = "sin-nombre.pdf";
        }
        return limpio.Length > 260 ? limpio.Substring(0, 260) : limpio;
    }

    /// <summary>El título de un documento: el nombre del archivo sin la extensión.</summary>
    private static string Titulo(string archivo)
    {
        string titulo = Path.GetFileNameWithoutExtension(archivo);
        return titulo == "" ? archivo : titulo;
    }

    /// <summary>Un documento que ya estaba cargado con el mismo nombre, con la huella con que se cargó.</summary>
    private sealed record Anterior(int DocumentoId, byte[]? Huella);

    /// <summary>Los números de la base que arman el resumen.</summary>
    private sealed record Cuenta(int Listos, int Pendientes, int SinTexto, int ConError, int Fragmentos, int Recientes);
}
