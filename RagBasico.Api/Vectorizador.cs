using System.Diagnostics;
using System.Globalization;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RagBasico.Api;

/// <summary>Calcula el vector de cada fragmento que todavía no lo tiene y lo guarda en rag.Fragmento.</summary>
public sealed class Vectorizador
{
    private readonly Ollama _ollama;
    private readonly Sincronizador _sincronizador;
    private readonly string _conexion;
    private readonly int _lote;

    /// <summary>Recibe el cliente de Ollama y el sincronizador, para subir los vectores nuevos a Qdrant, y lee de la configuración la conexión y cuántos fragmentos mandar en cada pedido.</summary>
    public Vectorizador(Ollama ollama, Sincronizador sincronizador, IConfiguration config)
    {
        _ollama = ollama;
        _sincronizador = sincronizador;
        _conexion = config.GetConnectionString("Rag") ?? throw new InvalidOperationException("Falta ConnectionStrings:Rag en la configuración.");
        _lote = Math.Max(1, config.GetValue("Rag:LoteVectores", 16));
    }

    /// <summary>Recorre de a lotes los fragmentos sin vector, en orden de número de fragmento: le pide los vectores a Ollama y los guarda. Si un fragmento falla, lo anota y sigue con los demás; si Ollama no responde, o fallan cinco lotes seguidos, corta.</summary>
    public async Task<InformeVectores> CalcularPendientesAsync(CancellationToken ct)
    {
        Stopwatch reloj = Stopwatch.StartNew();
        List<string> detalle = new List<string>();
        int calculados = 0;
        int fallados = 0;
        int pendientes;
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            await conexion.OpenAsync(ct);
            // Cada lote empieza después del último fragmento visto: así no se relee lo ya hecho, y un fragmento
            // que falla no traba la corrida. Queda en NULL y se vuelve a intentar en el próximo /vectores.
            int ultimo = 0;
            int lotesFallidosSeguidos = 0;
            while (true)
            {
                List<Fragmento> lote = (await conexion.QueryAsync<Fragmento>(new CommandDefinition(
                    "SELECT TOP (@lote) f.FragmentoId, d.Archivo, f.Texto " +
                    "FROM rag.Fragmento AS f JOIN rag.Documento AS d ON d.DocumentoId = f.DocumentoId " +
                    "WHERE f.Embedding IS NULL AND f.FragmentoId > @ultimo ORDER BY f.FragmentoId;",
                    new { lote = _lote, ultimo }, cancellationToken: ct))).ToList();
                if (lote.Count == 0)
                {
                    break;
                }
                ultimo = lote[lote.Count - 1].FragmentoId;

                int guardados;
                try
                {
                    guardados = await GuardarLoteAsync(conexion, lote, detalle, ct);
                }
                catch (Exception ex)
                {
                    if (ct.IsCancellationRequested || !EsCaida(ex))
                    {
                        throw;
                    }
                    // Si Ollama no responde, seguir sería fallar en cada lote. Lo que falta queda en NULL para el próximo /vectores.
                    detalle.Add("ERROR: Ollama no responde o tardó más de Ollama:SegundosDeEspera (" + ex.Message + "). Se cortó la corrida; los fragmentos que faltan quedan para el próximo POST /vectores.");
                    break;
                }
                calculados += guardados;
                fallados += lote.Count - guardados;

                // Un lote entero que falla puede ser de textos que Ollama no acepta, y se sigue. Cinco seguidos ya no:
                // es algo general, como la base, y seguir solo llenaría el detalle de errores iguales.
                lotesFallidosSeguidos = guardados == 0 ? lotesFallidosSeguidos + 1 : 0;
                if (lotesFallidosSeguidos == 5)
                {
                    detalle.Add("ERROR: fallaron todos los fragmentos de 5 lotes seguidos. Se cortó la corrida: el problema no parece de un fragmento; revisá los errores de arriba.");
                    break;
                }
            }
            pendientes = await conexion.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(*) FROM rag.Fragmento WHERE Embedding IS NULL;", cancellationToken: ct));
        }

        if (calculados == 0 && fallados == 0)
        {
            detalle.Add("SIN CAMBIOS: todos los fragmentos ya tenían vector.");
        }
        // Los vectores nuevos se suben a Qdrant. Si Qdrant estuvo apagado, también sube los que le faltaban de antes.
        await _sincronizador.PonerAlDiaAsync(detalle, ct);
        double segundos = reloj.Elapsed.TotalSeconds;
        double porSegundo = segundos > 0 ? Math.Round(calculados / segundos, 1) : 0;
        return new InformeVectores(calculados, fallados, pendientes, porSegundo, Indexador.FormatearTiempo(reloj.Elapsed), detalle);
    }

    /// <summary>Escribe un vector como lo recibe SQL Server: un arreglo JSON con punto decimal, sin importar la configuración regional de Windows, que en castellano usa coma.</summary>
    public static string ComoJson(float[] vector)
    {
        StringBuilder texto = new StringBuilder("[");
        for (int i = 0; i < vector.Length; i++)
        {
            if (i > 0)
            {
                texto.Append(',');
            }
            // Sin notación científica (1E-05), con hasta 9 decimales: alcanza para un número de un vector.
            texto.Append(vector[i].ToString("0.#########", CultureInfo.InvariantCulture));
        }
        texto.Append(']');
        return texto.ToString();
    }

    /// <summary>Dice si un error muestra que Ollama no responde: no se pudo conectar, o no contestó a tiempo. El error de un fragmento en particular, como un texto que Ollama no acepta, no lo es.</summary>
    public static bool EsCaida(Exception error)
    {
        return error is HttpRequestException || (error is TaskCanceledException && error.InnerException is TimeoutException);
    }

    /// <summary>Calcula y guarda los vectores de un lote en un solo pedido a Ollama. Si falla, prueba los fragmentos de a uno, así un fragmento con problemas no deja sin vector a los demás. Si Ollama no responde, no prueba más: lanza el error. Devuelve cuántos guardó.</summary>
    private async Task<int> GuardarLoteAsync(SqlConnection conexion, List<Fragmento> lote, List<string> detalle, CancellationToken ct)
    {
        try
        {
            await CalcularYGuardarAsync(conexion, lote, ct);
            return lote.Count;
        }
        catch (Exception ex)
        {
            // Si se canceló el pedido, por ejemplo porque se cerró la API, o si Ollama no responde, se corta todo.
            // Cualquier otro error es de este lote: se prueba de a uno, para encontrar el fragmento que falla.
            if (ct.IsCancellationRequested || EsCaida(ex))
            {
                throw;
            }
        }

        int guardados = 0;
        foreach (Fragmento fragmento in lote)
        {
            try
            {
                await CalcularYGuardarAsync(conexion, new List<Fragmento> { fragmento }, ct);
                guardados++;
            }
            catch (Exception ex)
            {
                if (ct.IsCancellationRequested || EsCaida(ex))
                {
                    throw;
                }
                detalle.Add("ERROR en el fragmento " + fragmento.FragmentoId + " de " + fragmento.Archivo + ": " + ex.Message);
            }
        }
        return guardados;
    }

    /// <summary>Le pide a Ollama los vectores de los fragmentos en un solo pedido y los guarda en rag.Fragmento, todos en una transacción.</summary>
    private async Task CalcularYGuardarAsync(SqlConnection conexion, List<Fragmento> fragmentos, CancellationToken ct)
    {
        List<float[]> vectores = await _ollama.EmbeddingsAsync(fragmentos.Select(f => f.Texto).ToList(), ct);
        using (SqlTransaction transaccion = conexion.BeginTransaction())
        {
            for (int i = 0; i < fragmentos.Count; i++)
            {
                // SQL Server convierte el texto JSON en un VECTOR(1024).
                await conexion.ExecuteAsync(new CommandDefinition(
                    "UPDATE rag.Fragmento SET Embedding = CAST(@vector AS VECTOR(1024)) WHERE FragmentoId = @fragmentoId;",
                    new { vector = ComoJson(vectores[i]), fragmentoId = fragmentos[i].FragmentoId },
                    transaccion, cancellationToken: ct));
            }
            transaccion.Commit();
        }
    }
}
