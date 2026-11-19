using Dapper;
using Microsoft.Data.SqlClient;

namespace RagBasico.Api;

/// <summary>Busca los fragmentos que mejor responden una pregunta: por palabras, por significado en SQL o en Qdrant, o con las dos mezcladas.</summary>
public sealed class Buscador
{
    /// <summary>Los modos de búsqueda que existen.</summary>
    public static readonly string[] Modos = new string[] { "palabras", "vectores", "vectores-qdrant", "hibrida" };

    /// <summary>El modo que se usa cuando el pedido no dice cuál.</summary>
    public const string ModoPorDefecto = "hibrida";

    /// <summary>El aviso que acompaña a la búsqueda cuando Qdrant no respondió y la híbrida usó los vectores de SQL.</summary>
    public const string AvisoSinQdrant = "Qdrant no respondió: la búsqueda por significado usó los vectores de SQL Server.";

    /// <summary>El aviso que acompaña a la búsqueda cuando la colección de Qdrant no tiene vectores y la híbrida usó los de SQL.</summary>
    public const string AvisoQdrantVacio = "La colección de Qdrant no tiene vectores: la búsqueda por significado usó los de SQL Server. POST /vectores los sube.";

    private readonly Ollama _ollama;
    private readonly Qdrant _qdrant;
    private readonly ILogger<Buscador> _log;
    private readonly string _conexion;
    private readonly int _candidatos;

    /// <summary>Recibe los clientes de Ollama, para el vector de la pregunta, y de Qdrant, y el log; lee de la configuración la cadena de conexión y cuántos candidatos pide la híbrida a cada búsqueda.</summary>
    public Buscador(Ollama ollama, Qdrant qdrant, ILogger<Buscador> log, IConfiguration config)
    {
        _ollama = ollama;
        _qdrant = qdrant;
        _log = log;
        _conexion = config.GetConnectionString("Rag") ?? throw new InvalidOperationException("Falta ConnectionStrings:Rag en la configuración.");
        _candidatos = Math.Max(1, config.GetValue("Rag:CandidatosHibrida", 20));
    }

    /// <summary>Devuelve el modo pedido en minúsculas, o el modo por defecto si el pedido no dice ninguno. Si el modo no existe, devuelve null: así quien llama da un error en lugar de buscar con otro modo sin avisar.</summary>
    public static string? ElegirModo(string? pedido)
    {
        if (string.IsNullOrWhiteSpace(pedido))
        {
            return ModoPorDefecto;
        }
        string modo = pedido.Trim().ToLowerInvariant();
        return Modos.Contains(modo) ? modo : null;
    }

    /// <summary>Busca con el modo pedido, que tiene que ser uno de Modos. Devuelve los fragmentos y, si la híbrida tuvo que seguir sin Qdrant, el aviso.</summary>
    public async Task<ResultadoBusqueda> BuscarAsync(string pregunta, string modo, int topK, CancellationToken ct)
    {
        if (modo == "palabras")
        {
            return new ResultadoBusqueda(await PorPalabrasAsync(pregunta, topK, ct), "");
        }
        if (modo == "vectores")
        {
            return new ResultadoBusqueda(await PorVectoresAsync(pregunta, topK, ct), "");
        }
        if (modo == "vectores-qdrant")
        {
            return new ResultadoBusqueda(await PorVectoresQdrantAsync(pregunta, topK, ct), "");
        }
        if (modo == "hibrida")
        {
            return await HibridaAsync(pregunta, topK, ct);
        }
        throw new ArgumentException("Modo desconocido: '" + modo + "'. Los modos son: " + string.Join(", ", Modos) + ".");
    }

    /// <summary>Busca por palabras y por vectores en Qdrant al mismo tiempo, pide los mejores candidatos de cada una y los mezcla por puesto con RRF. Si Qdrant no responde, usa los vectores de SQL y lo avisa.</summary>
    public async Task<ResultadoBusqueda> HibridaAsync(string pregunta, int topK, CancellationToken ct)
    {
        // Las dos búsquedas arrancan juntas y recién después se espera a las dos:
        // se tarda lo que la más lenta, no la suma. Cada una abre su propia conexión.
        Task<List<Fragmento>> porPalabras = PorPalabrasAsync(pregunta, _candidatos, ct);
        Task<ResultadoBusqueda> porVectores = VectoresConRespaldoAsync(pregunta, _candidatos, ct);
        await Task.WhenAll(porPalabras, porVectores);

        ResultadoBusqueda vectores = await porVectores;
        List<List<Fragmento>> listas = new List<List<Fragmento>> { await porPalabras, vectores.Fragmentos };
        return new ResultadoBusqueda(Rrf.Mezclar(listas, topK), vectores.Aviso);
    }

    /// <summary>Busca en el Full-Text de SQL Server los fragmentos que mejor coinciden con las palabras de la pregunta, del más relevante al menos relevante.</summary>
    public async Task<List<Fragmento>> PorPalabrasAsync(string pregunta, int topK, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pregunta))
        {
            return new List<Fragmento>();
        }
        // FREETEXTTABLE corta la pregunta en palabras, busca también sus plurales y conjugaciones,
        // y devuelve cada fragmento con un puntaje (RANK). Se piden los 50 mejores y se usan los topK primeros.
        const string sql = @"
SELECT TOP (@topK) f.FragmentoId, d.Archivo, f.Texto
FROM FREETEXTTABLE(rag.Fragmento, Texto, @pregunta, LANGUAGE 3082, 50) AS ft
JOIN rag.Fragmento AS f ON f.FragmentoId = ft.[KEY]
JOIN rag.Documento AS d ON d.DocumentoId = f.DocumentoId
ORDER BY ft.[RANK] DESC;";
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            IEnumerable<Fragmento> filas = await conexion.QueryAsync<Fragmento>(
                new CommandDefinition(sql, new { pregunta, topK }, cancellationToken: ct));
            return filas.ToList();
        }
    }

    /// <summary>Busca los fragmentos más parecidos en significado a la pregunta con los vectores de SQL Server: calcula el vector de la pregunta con Ollama y busca los más cercanos con VECTOR_DISTANCE.</summary>
    public async Task<List<Fragmento>> PorVectoresAsync(string pregunta, int topK, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pregunta))
        {
            return new List<Fragmento>();
        }
        float[] vector = await _ollama.EmbeddingPreguntaAsync(pregunta, ct);
        return await CercanosEnSqlAsync(vector, topK, ct);
    }

    /// <summary>Busca los fragmentos más parecidos en significado a la pregunta con el índice HNSW de Qdrant, y trae sus textos de SQL Server. Si Qdrant no responde, falla: este modo existe para medir Qdrant.</summary>
    public async Task<List<Fragmento>> PorVectoresQdrantAsync(string pregunta, int topK, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pregunta))
        {
            return new List<Fragmento>();
        }
        float[] vector = await _ollama.EmbeddingPreguntaAsync(pregunta, ct);
        List<int> ids = await _qdrant.BuscarAsync(vector, topK, ct);
        if (ids.Count == 0)
        {
            // Si la colección tiene puntos, Qdrant siempre devuelve alguno. Vacía, cada pregunta diría "no encontré" sin explicar por qué.
            throw new InvalidOperationException("La colección " + _qdrant.Coleccion + " de Qdrant no tiene vectores: corré POST /vectores, que se los sube.");
        }
        return await LeerFragmentosAsync(ids, ct);
    }

    /// <summary>La parte por significado de la híbrida: busca en Qdrant y, si Qdrant no responde o no tiene vectores, busca con los vectores de SQL Server y devuelve el aviso. El vector de la pregunta se calcula una sola vez.</summary>
    private async Task<ResultadoBusqueda> VectoresConRespaldoAsync(string pregunta, int topK, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pregunta))
        {
            return new ResultadoBusqueda(new List<Fragmento>(), "");
        }
        float[] vector = await _ollama.EmbeddingPreguntaAsync(pregunta, ct);

        List<int>? ids = null;
        try
        {
            ids = await _qdrant.BuscarAsync(vector, topK, ct);
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested)
            {
                throw;
            }
            // La demo tiene que seguir andando sin Qdrant: se anota en el log y se sigue con SQL.
            _log.LogWarning("Qdrant no respondió, la híbrida usa los vectores de SQL: {Mensaje}", ex.Message);
        }

        if (ids == null)
        {
            return new ResultadoBusqueda(await CercanosEnSqlAsync(vector, topK, ct), AvisoSinQdrant);
        }
        // Una colección vacía responde, pero sin nada: la híbrida quedaría solo con las palabras sin avisar.
        if (ids.Count == 0)
        {
            return new ResultadoBusqueda(await CercanosEnSqlAsync(vector, topK, ct), AvisoQdrantVacio);
        }
        return new ResultadoBusqueda(await LeerFragmentosAsync(ids, ct), "");
    }

    /// <summary>Busca en SQL Server los fragmentos más cercanos a un vector: los ordena a todos por distancia coseno, del más cercano al más lejano.</summary>
    private async Task<List<Fragmento>> CercanosEnSqlAsync(float[] vector, int topK, CancellationToken ct)
    {
        // VECTOR_DISTANCE compara el vector de la pregunta con el de cada fragmento: 0 es el mismo sentido y 2 el opuesto.
        // Es una búsqueda exacta: recorre todos los fragmentos que tienen vector, así que tarda más cuantos más haya.
        const string sql = @"
DECLARE @vectorPregunta VECTOR(1024) = CAST(@vector AS VECTOR(1024));
SELECT TOP (@topK) f.FragmentoId, d.Archivo, f.Texto
FROM rag.Fragmento AS f
JOIN rag.Documento AS d ON d.DocumentoId = f.DocumentoId
WHERE f.Embedding IS NOT NULL
ORDER BY VECTOR_DISTANCE('cosine', f.Embedding, @vectorPregunta);";
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            IEnumerable<Fragmento> filas = await conexion.QueryAsync<Fragmento>(
                new CommandDefinition(sql, new { vector = Vectorizador.ComoJson(vector), topK }, cancellationToken: ct));
            return filas.ToList();
        }
    }

    /// <summary>Trae de SQL Server los fragmentos de una lista de números, en el mismo orden de la lista. Los que ya no existen en SQL se saltean.</summary>
    private async Task<List<Fragmento>> LeerFragmentosAsync(List<int> ids, CancellationToken ct)
    {
        List<Fragmento> ordenados = new List<Fragmento>();
        if (ids.Count == 0)
        {
            return ordenados;
        }
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            IEnumerable<Fragmento> filas = await conexion.QueryAsync<Fragmento>(new CommandDefinition(
                "SELECT f.FragmentoId, d.Archivo, f.Texto FROM rag.Fragmento AS f " +
                "JOIN rag.Documento AS d ON d.DocumentoId = f.DocumentoId WHERE f.FragmentoId IN @ids;",
                new { ids }, cancellationToken: ct));
            // SQL no devuelve las filas en el orden de la lista: se ordenan como las devolvió Qdrant, del más cercano al más lejano.
            Dictionary<int, Fragmento> porNumero = filas.ToDictionary(f => f.FragmentoId);
            foreach (int id in ids)
            {
                Fragmento? fragmento;
                if (porNumero.TryGetValue(id, out fragmento))
                {
                    ordenados.Add(fragmento);
                }
            }
        }
        return ordenados;
    }
}
