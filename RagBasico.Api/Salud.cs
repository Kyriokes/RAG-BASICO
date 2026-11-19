using Dapper;
using Microsoft.Data.SqlClient;

namespace RagBasico.Api;

/// <summary>Revisa que estén andando todas las piezas de la demo: SQL Server, el Full-Text, los modelos de Ollama, la dimensión de los vectores y Qdrant.</summary>
public sealed class Salud
{
    private readonly Ollama _ollama;
    private readonly Qdrant _qdrant;
    private readonly string _conexion;
    private readonly string _modeloChat;
    private readonly string _modeloEmbeddings;

    /// <summary>Recibe los clientes de Ollama y de Qdrant, y lee de la configuración la conexión y los nombres de los dos modelos.</summary>
    public Salud(Ollama ollama, Qdrant qdrant, IConfiguration config)
    {
        _ollama = ollama;
        _qdrant = qdrant;
        _conexion = config.GetConnectionString("Rag") ?? throw new InvalidOperationException("Falta ConnectionStrings:Rag en la configuración.");
        _modeloChat = config["Ollama:ModeloChat"] ?? "qwen3.5:4b";
        _modeloEmbeddings = config["Ollama:ModeloEmbeddings"] ?? "qwen3-embedding:0.6b";
    }

    /// <summary>Corre los cinco chequeos. El estado general es "error" si falla algo sin lo que la demo no anda, "aviso" si algo anda a medias o si falta Qdrant, que tiene respaldo, y "ok" si anda todo.</summary>
    public async Task<InformeSalud> RevisarAsync(CancellationToken ct)
    {
        List<Chequeo> chequeos = new List<Chequeo>();
        chequeos.Add(await RevisarSqlAsync(ct));
        chequeos.Add(await RevisarFullTextAsync(ct));
        chequeos.Add(await RevisarModelosAsync(ct));
        chequeos.Add(await RevisarDimensionAsync(ct));
        chequeos.Add(await RevisarQdrantAsync(ct));

        string estado = "ok";
        if (chequeos.Any(c => c.Estado == "aviso"))
        {
            estado = "aviso";
        }
        if (chequeos.Any(c => c.Estado == "error"))
        {
            estado = "error";
        }
        return new InformeSalud(estado, chequeos);
    }

    /// <summary>SQL Server: se conecta a la base y cuenta los documentos, los fragmentos y los que tienen vector.</summary>
    private async Task<Chequeo> RevisarSqlAsync(CancellationToken ct)
    {
        const string nombre = "SQL Server";
        try
        {
            using (SqlConnection conexion = new SqlConnection(_conexion))
            {
                Conteo conteo = await conexion.QuerySingleAsync<Conteo>(new CommandDefinition(
                    "SELECT DB_NAME() AS Base, " +
                    "(SELECT COUNT(*) FROM rag.Documento) AS Documentos, " +
                    "(SELECT COUNT(*) FROM rag.Fragmento) AS Fragmentos, " +
                    "(SELECT COUNT(*) FROM rag.Fragmento WHERE Embedding IS NOT NULL) AS ConVector;",
                    cancellationToken: ct));
                string detalle = "Base " + conteo.Base + ": " + conteo.Documentos + " documentos, " + conteo.Fragmentos + " fragmentos, " + conteo.ConVector + " con vector.";
                if (conteo.Documentos == 0)
                {
                    return new Chequeo(nombre, "aviso", detalle + " No hay documentos: corré POST /indexar.");
                }
                if (conteo.ConVector < conteo.Fragmentos)
                {
                    return new Chequeo(nombre, "aviso", detalle + " Faltan vectores: corré POST /vectores.");
                }
                return new Chequeo(nombre, "ok", detalle);
            }
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            return new Chequeo(nombre, "error", "No se pudo leer la base: " + ex.Message);
        }
    }

    /// <summary>El Full-Text: que no tenga cambios pendientes, porque mientras tanto la búsqueda por palabras no ve todo.</summary>
    private async Task<Chequeo> RevisarFullTextAsync(CancellationToken ct)
    {
        const string nombre = "Full-Text";
        try
        {
            using (SqlConnection conexion = new SqlConnection(_conexion))
            {
                int pendientes = await conexion.ExecuteScalarAsync<int>(new CommandDefinition(
                    "SELECT CAST(OBJECTPROPERTYEX(OBJECT_ID(N'rag.Fragmento'), 'TableFulltextPendingChanges') AS INT);",
                    cancellationToken: ct));
                if (pendientes > 0)
                {
                    return new Chequeo(nombre, "aviso", pendientes + " cambios pendientes: la búsqueda por palabras todavía no ve todo.");
                }
                return new Chequeo(nombre, "ok", "Sin cambios pendientes.");
            }
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            return new Chequeo(nombre, "error", "No se pudo revisar el Full-Text: " + ex.Message);
        }
    }

    /// <summary>Los dos modelos: que Ollama responda y que los tenga instalados.</summary>
    private async Task<Chequeo> RevisarModelosAsync(CancellationToken ct)
    {
        const string nombre = "Modelos de Ollama";
        try
        {
            List<string> instalados = await _ollama.ListarModelosAsync(ct);
            List<string> faltan = new List<string>();
            foreach (string modelo in new string[] { _modeloChat, _modeloEmbeddings })
            {
                if (!EstaInstalado(instalados, modelo))
                {
                    faltan.Add(modelo);
                }
            }
            if (faltan.Count > 0)
            {
                return new Chequeo(nombre, "error", "Faltan " + string.Join(" y ", faltan) + ". Se bajan con: ollama pull " + faltan[0] + ".");
            }
            return new Chequeo(nombre, "ok", _modeloChat + " y " + _modeloEmbeddings + " instalados.");
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            return new Chequeo(nombre, "error", "Ollama no responde: " + ex.Message);
        }
    }

    /// <summary>La dimensión: calcula el vector de una palabra y controla que tenga 1024 números, como la columna Embedding y la colección de Qdrant.</summary>
    private async Task<Chequeo> RevisarDimensionAsync(CancellationToken ct)
    {
        const string nombre = "Dimensión de los vectores";
        try
        {
            // EmbeddingsAsync ya lanza un error claro si el modelo devuelve otra dimensión.
            List<float[]> vectores = await _ollama.EmbeddingsAsync(new List<string> { "prueba" }, ct);
            return new Chequeo(nombre, "ok", vectores[0].Length + " números, igual que la base y Qdrant.");
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            return new Chequeo(nombre, "error", ex.Message);
        }
    }

    /// <summary>Qdrant: que responda, que sea la versión 1.20.1 o posterior y que exista la colección. Si falla es un aviso y no un error: la híbrida sigue con los vectores de SQL.</summary>
    private async Task<Chequeo> RevisarQdrantAsync(CancellationToken ct)
    {
        const string nombre = "Qdrant";
        try
        {
            string version = await _qdrant.LeerVersionAsync(ct);
            Version? leida;
            if (Version.TryParse(version, out leida) && leida < new Version(1, 20, 1))
            {
                return new Chequeo(nombre, "aviso", "Versión " + version + ": antes de la 1.20.1, en Windows, borrar una colección podía fallar. Usá la 1.20.2.");
            }
            if (!await _qdrant.ExisteColeccionAsync(ct))
            {
                return new Chequeo(nombre, "aviso", "Versión " + version + ", pero no existe la colección " + _qdrant.Coleccion + ": corré POST /qdrant/reconstruir.");
            }
            InfoColeccion info = await _qdrant.LeerInfoAsync(ct);
            return new Chequeo(nombre, "ok", "Versión " + version + ". Colección " + _qdrant.Coleccion + ": " + info.Puntos + " puntos, " + info.VectoresIndexados + " en el índice HNSW.");
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            return new Chequeo(nombre, "aviso", "No responde (" + ex.Message + "). La híbrida sigue con los vectores de SQL; el modo vectores-qdrant no anda. Arrancalo con scripts\\iniciar-qdrant.ps1.");
        }
    }

    /// <summary>Dice si un modelo está en la lista de instalados. Un nombre sin etiqueta, como "llama3", es el mismo que "llama3:latest".</summary>
    private static bool EstaInstalado(List<string> instalados, string modelo)
    {
        string buscado = modelo.Contains(':') ? modelo : modelo + ":latest";
        return instalados.Any(m => string.Equals(m, buscado, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Los números de la base que muestra el chequeo de SQL Server.</summary>
    private sealed record Conteo(string Base, int Documentos, int Fragmentos, int ConVector);
}
