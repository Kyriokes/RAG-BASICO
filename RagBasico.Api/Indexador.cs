using System.Diagnostics;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace RagBasico.Api;

/// <summary>Carga los PDF de la carpeta en la base: los lee, los corta en fragmentos y los guarda.</summary>
public sealed class Indexador
{
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

    /// <summary>Borra todo lo cargado y vuelve a cargar cada PDF de la carpeta, con una transacción por documento.</summary>
    public async Task<InformeIndexado> IndexarCarpetaAsync(CancellationToken ct)
    {
        Stopwatch reloj = Stopwatch.StartNew();
        List<string> detalle = new List<string>();
        if (string.IsNullOrWhiteSpace(_carpeta) || !Directory.Exists(_carpeta))
        {
            detalle.Add("ERROR: no existe la carpeta de PDF '" + _carpeta + "'. Configurá la variable de entorno Rag__CarpetaDocumentos.");
            return new InformeIndexado(0, 0, FormatearTiempo(reloj.Elapsed), detalle);
        }

        int documentos = 0;
        int fragmentos = 0;
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            await conexion.OpenAsync(ct);
            // En esta fase se recarga todo: se borran los documentos y, en cascada, sus fragmentos.
            await conexion.ExecuteAsync(new CommandDefinition("DELETE FROM rag.Documento;", cancellationToken: ct));

            foreach (string ruta in Directory.GetFiles(_carpeta, "*.pdf").OrderBy(r => r))
            {
                string archivo = Path.GetFileName(ruta);
                try
                {
                    int cantidad = await IndexarArchivoAsync(conexion, ruta, ct);
                    if (cantidad < 0)
                    {
                        detalle.Add("SIN TEXTO (¿escaneado?): " + archivo);
                    }
                    else
                    {
                        detalle.Add("OK, " + cantidad + " fragmentos: " + archivo);
                        documentos++;
                        fragmentos += cantidad;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    detalle.Add("ERROR: " + archivo + ": " + ex.Message);
                }
            }
        }
        return new InformeIndexado(documentos, fragmentos, FormatearTiempo(reloj.Elapsed), detalle);
    }

    /// <summary>Carga un PDF: lo lee, lo corta y guarda el documento y sus fragmentos en una sola transacción. Devuelve -1 si el PDF no tiene texto.</summary>
    private async Task<int> IndexarArchivoAsync(SqlConnection conexion, string ruta, CancellationToken ct)
    {
        string archivo = Path.GetFileName(ruta);
        string titulo = Path.GetFileNameWithoutExtension(ruta);
        string texto = LeerPdf(ruta);
        if (Chunker.ContarPalabras(texto) < 20)
        {
            return -1;
        }

        List<string> partes = Chunker.Partir(texto, _palabras, _solapamiento);
        using (SqlTransaction transaccion = conexion.BeginTransaction())
        {
            int documentoId = await conexion.ExecuteScalarAsync<int>(new CommandDefinition(
                "INSERT INTO rag.Documento (Archivo, Titulo) OUTPUT INSERTED.DocumentoId VALUES (@archivo, @titulo);",
                new { archivo, titulo }, transaccion, cancellationToken: ct));
            for (int i = 0; i < partes.Count; i++)
            {
                // Cada fragmento empieza con el nombre del documento, así la búsqueda también lo encuentra por el nombre.
                await conexion.ExecuteAsync(new CommandDefinition(
                    "INSERT INTO rag.Fragmento (DocumentoId, Orden, Texto) VALUES (@documentoId, @orden, @texto);",
                    new { documentoId, orden = i + 1, texto = "Documento: " + titulo + "\n" + partes[i] },
                    transaccion, cancellationToken: ct));
            }
            transaccion.Commit();
        }
        return partes.Count;
    }

    /// <summary>Saca el texto de un PDF, página por página, en el orden en que se lee.</summary>
    private static string LeerPdf(string ruta)
    {
        StringBuilder texto = new StringBuilder();
        using (PdfDocument pdf = PdfDocument.Open(ruta))
        {
            foreach (Page pagina in pdf.GetPages())
            {
                texto.Append(ContentOrderTextExtractor.GetText(pagina, true));
                texto.Append("\n\n");
            }
        }
        return texto.ToString();
    }

    /// <summary>Muestra un tiempo como horas:minutos:segundos, también cuando pasa de 24 horas.</summary>
    private static string FormatearTiempo(TimeSpan tiempo)
    {
        return ((int)tiempo.TotalHours).ToString("00") + ":" + tiempo.Minutes.ToString("00") + ":" + tiempo.Seconds.ToString("00");
    }
}
