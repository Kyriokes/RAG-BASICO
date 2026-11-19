using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace RagBasico.Api;

/// <summary>Mantiene la base igual a la carpeta de PDF: carga los nuevos y los cambiados, y borra los que ya no están.</summary>
public sealed class Indexador
{
    // Un PDF con menos palabras por página que esto, en promedio, se toma como escaneado y no se carga.
    private const int PalabrasPorPaginaMinimas = 40;

    private readonly string _conexion;
    private readonly string _carpeta;
    private readonly int _palabras;
    private readonly int _solapamiento;

    /// <summary>Lee de la configuración la cadena de conexión, la carpeta de PDF y el tamaño de los fragmentos.</summary>
    public Indexador(IConfiguration config)
    {
        _conexion = config.GetConnectionString("Rag") ?? throw new InvalidOperationException("Falta ConnectionStrings:Rag en la configuración.");
        _carpeta = config["Rag:CarpetaDocumentos"] ?? "";
        _palabras = config.GetValue("Rag:PalabrasPorFragmento", 300);
        _solapamiento = config.GetValue("Rag:Solapamiento", 50);
    }

    /// <summary>Pone la base al día con la carpeta: borra los PDF que ya no están, carga los nuevos y los cambiados, y no toca los que siguen igual.</summary>
    public async Task<InformeIndexado> IndexarCarpetaAsync(CancellationToken ct)
    {
        Stopwatch reloj = Stopwatch.StartNew();
        List<string> detalle = new List<string>();
        if (string.IsNullOrWhiteSpace(_carpeta) || !Directory.Exists(_carpeta))
        {
            detalle.Add("ERROR: no existe la carpeta de PDF '" + _carpeta + "'. Configurá la variable de entorno Rag__CarpetaDocumentos. No se borró nada de la base.");
            return new InformeIndexado(0, 0, 0, 0, FormatearTiempo(reloj.Elapsed), detalle);
        }

        List<ArchivoEnCarpeta> enCarpeta = LeerCarpeta(detalle);
        PlanDeCarga plan;
        int cargados = 0;
        int fragmentos = 0;
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            await conexion.OpenAsync(ct);
            List<DocumentoCargado> enBase = (await conexion.QueryAsync<DocumentoCargado>(new CommandDefinition(
                "SELECT DocumentoId, Archivo, Huella FROM rag.Documento;", cancellationToken: ct))).ToList();
            plan = Planificador.Armar(enCarpeta, enBase);
            if (enCarpeta.Count == 0)
            {
                detalle.Add("AVISO: la carpeta '" + _carpeta + "' no tiene ningún PDF. Por las dudas, no se borró nada de la base.");
            }

            // Los fragmentos de cada documento se borran solos, en cascada.
            foreach (DocumentoCargado documento in plan.Borrar)
            {
                await conexion.ExecuteAsync(new CommandDefinition(
                    "DELETE FROM rag.Documento WHERE DocumentoId = @documentoId;",
                    new { documentoId = documento.DocumentoId }, cancellationToken: ct));
                detalle.Add("BORRADO, ya no está en la carpeta: " + documento.Archivo);
            }

            foreach (ArchivoEnCarpeta archivo in plan.Nuevos)
            {
                int cantidad = await CargarArchivoAsync(conexion, archivo, "NUEVO", detalle, ct);
                if (cantidad > 0)
                {
                    cargados++;
                    fragmentos += cantidad;
                }
            }
            foreach (ArchivoEnCarpeta archivo in plan.Cambiados)
            {
                int cantidad = await CargarArchivoAsync(conexion, archivo, "CAMBIÓ", detalle, ct);
                if (cantidad > 0)
                {
                    cargados++;
                    fragmentos += cantidad;
                }
            }
        }

        if (enCarpeta.Count > 0 && plan.Nuevos.Count == 0 && plan.Cambiados.Count == 0 && plan.Borrar.Count == 0)
        {
            detalle.Add("SIN CAMBIOS: los " + plan.SinCambios.Count + " PDF ya estaban cargados.");
        }
        return new InformeIndexado(cargados, plan.SinCambios.Count, plan.Borrar.Count, fragmentos, FormatearTiempo(reloj.Elapsed), detalle);
    }

    /// <summary>Calcula la huella de carga: el sha256 de los bytes del PDF seguidos de los parámetros de corte. Si cambia el PDF o cambia la forma de cortar, cambia la huella.</summary>
    public static byte[] CalcularHuella(string ruta, string parametros)
    {
        using (FileStream archivo = File.OpenRead(ruta))
        using (IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            // El PDF se lee de a bloques: así un archivo grande no se carga entero en memoria.
            byte[] bloque = new byte[81920];
            int leidos = archivo.Read(bloque, 0, bloque.Length);
            while (leidos > 0)
            {
                sha.AppendData(bloque, 0, leidos);
                leidos = archivo.Read(bloque, 0, bloque.Length);
            }
            sha.AppendData(Encoding.UTF8.GetBytes(parametros));
            return sha.GetHashAndReset();
        }
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

    /// <summary>Lista los PDF de la carpeta, en orden alfabético, con su huella de carga. Si un PDF no se puede leer, lo anota en el detalle y lo lista sin huella: así no se carga, pero tampoco se borra.</summary>
    private List<ArchivoEnCarpeta> LeerCarpeta(List<string> detalle)
    {
        string parametros = ParametrosDeCorte();
        List<ArchivoEnCarpeta> archivos = new List<ArchivoEnCarpeta>();
        foreach (string ruta in Directory.GetFiles(_carpeta, "*.pdf").OrderBy(r => r))
        {
            string archivo = Path.GetFileName(ruta);
            byte[]? huella = null;
            try
            {
                huella = CalcularHuella(ruta, parametros);
            }
            catch (IOException ex)
            {
                detalle.Add("ERROR, no se pudo leer y queda lo que había en la base: " + archivo + ": " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                detalle.Add("ERROR, sin permiso para leerlo, queda lo que había en la base: " + archivo + ": " + ex.Message);
            }
            archivos.Add(new ArchivoEnCarpeta(archivo, ruta, huella));
        }
        return archivos;
    }

    /// <summary>Arma el texto con los parámetros que cambian el corte: la versión del Chunker, el tamaño, el solapamiento y la versión de PdfPig, que cambia el texto que se saca de cada PDF.</summary>
    private string ParametrosDeCorte()
    {
        return "corte=" + Chunker.Version
            + ";palabras=" + _palabras
            + ";solapamiento=" + _solapamiento
            + ";pdfpig=" + typeof(PdfDocument).Assembly.GetName().Version;
    }

    /// <summary>Carga un PDF nuevo o cambiado: lo lee, se fija si es un escaneo, lo corta y lo guarda. Anota el resultado en el detalle. Devuelve cuántos fragmentos guardó: 0 si no tenía texto o si falló.</summary>
    private async Task<int> CargarArchivoAsync(SqlConnection conexion, ArchivoEnCarpeta archivo, string accion, List<string> detalle, CancellationToken ct)
    {
        try
        {
            List<string> paginas = LeerPdf(archivo.Ruta);
            List<string> partes = new List<string>();
            string duda = "";
            if (!EsEscaneado(paginas))
            {
                partes = Chunker.Partir(string.Join("\n\n", paginas), _palabras, _solapamiento);
                duda = RevisarTexto(paginas);
            }

            // Si es un escaneo, igual se borra la versión anterior: la base tiene que mostrar lo que hay en la carpeta.
            await GuardarAsync(conexion, archivo, partes, ct);
            if (partes.Count == 0)
            {
                detalle.Add("SIN TEXTO (¿escaneado?), " + paginas.Count + " páginas: " + archivo.Archivo);
                return 0;
            }
            string linea = accion + ", " + partes.Count + " fragmentos";
            if (duda != "")
            {
                linea = linea + ", TEXTO DUDOSO (" + duda + ")";
            }
            detalle.Add(linea + ": " + archivo.Archivo);
            return partes.Count;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Si el PDF había cambiado, la transacción no llegó a confirmarse y en la base queda la versión anterior.
            string queda = accion == "CAMBIÓ" ? ", queda la versión anterior" : "";
            detalle.Add("ERROR" + queda + ": " + archivo.Archivo + ": " + ex.Message);
            return 0;
        }
    }

    /// <summary>En una sola transacción, borra la versión anterior del PDF, si había, y guarda la nueva con su huella y sus fragmentos. Sin fragmentos, solo borra.</summary>
    private static async Task GuardarAsync(SqlConnection conexion, ArchivoEnCarpeta archivo, List<string> partes, CancellationToken ct)
    {
        string titulo = Path.GetFileNameWithoutExtension(archivo.Archivo);
        using (SqlTransaction transaccion = conexion.BeginTransaction())
        {
            await conexion.ExecuteAsync(new CommandDefinition(
                "DELETE FROM rag.Documento WHERE Archivo = @archivo;",
                new { archivo = archivo.Archivo }, transaccion, cancellationToken: ct));
            if (partes.Count > 0)
            {
                int documentoId = await conexion.ExecuteScalarAsync<int>(new CommandDefinition(
                    "INSERT INTO rag.Documento (Archivo, Titulo, Huella) OUTPUT INSERTED.DocumentoId VALUES (@archivo, @titulo, @huella);",
                    new { archivo = archivo.Archivo, titulo, huella = archivo.Huella }, transaccion, cancellationToken: ct));
                for (int i = 0; i < partes.Count; i++)
                {
                    // Cada fragmento empieza con el nombre del documento, así la búsqueda también lo encuentra por el nombre.
                    await conexion.ExecuteAsync(new CommandDefinition(
                        "INSERT INTO rag.Fragmento (DocumentoId, Orden, Texto) VALUES (@documentoId, @orden, @texto);",
                        new { documentoId, orden = i + 1, texto = "Documento: " + titulo + "\n" + partes[i] },
                        transaccion, cancellationToken: ct));
                }
            }
            // Si algo falla antes de esta línea, la transacción se descarta y queda la versión anterior.
            transaccion.Commit();
        }
    }

    /// <summary>Saca el texto de un PDF con PdfPig, una página por elemento de la lista, en el orden en que se lee.</summary>
    private static List<string> LeerPdf(string ruta)
    {
        List<string> paginas = new List<string>();
        using (PdfDocument pdf = PdfDocument.Open(ruta))
        {
            foreach (Page pagina in pdf.GetPages())
            {
                paginas.Add(ContentOrderTextExtractor.GetText(pagina, true));
            }
        }
        return paginas;
    }

    /// <summary>Muestra un tiempo como horas:minutos:segundos, también cuando pasa de 24 horas.</summary>
    private static string FormatearTiempo(TimeSpan tiempo)
    {
        return ((int)tiempo.TotalHours).ToString("00") + ":" + tiempo.Minutes.ToString("00") + ":" + tiempo.Seconds.ToString("00");
    }
}
