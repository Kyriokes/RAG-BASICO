using Dapper;
using Microsoft.Data.SqlClient;

namespace RagBasico.Api;

/// <summary>Busca los fragmentos que mejor responden una pregunta: por palabras, por significado o con las dos mezcladas.</summary>
public sealed class Buscador
{
    /// <summary>Los modos de búsqueda que existen.</summary>
    public static readonly string[] Modos = new string[] { "palabras", "vectores", "hibrida" };

    /// <summary>El modo que se usa cuando el pedido no dice cuál.</summary>
    public const string ModoPorDefecto = "hibrida";

    private readonly Ollama _ollama;
    private readonly string _conexion;
    private readonly int _candidatos;

    /// <summary>Recibe el cliente de Ollama, para el vector de la pregunta, y lee de la configuración la cadena de conexión y cuántos candidatos pide la híbrida a cada búsqueda.</summary>
    public Buscador(Ollama ollama, IConfiguration config)
    {
        _ollama = ollama;
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

    /// <summary>Busca con el modo pedido, que tiene que ser uno de Modos.</summary>
    public async Task<List<Fragmento>> BuscarAsync(string pregunta, string modo, int topK, CancellationToken ct)
    {
        if (modo == "palabras")
        {
            return await PorPalabrasAsync(pregunta, topK, ct);
        }
        if (modo == "vectores")
        {
            return await PorVectoresAsync(pregunta, topK, ct);
        }
        if (modo == "hibrida")
        {
            return await HibridaAsync(pregunta, topK, ct);
        }
        throw new ArgumentException("Modo desconocido: '" + modo + "'. Los modos son: " + string.Join(", ", Modos) + ".");
    }

    /// <summary>Busca por palabras y por vectores al mismo tiempo, pide los mejores candidatos de cada una y los mezcla por puesto con RRF.</summary>
    public async Task<List<Fragmento>> HibridaAsync(string pregunta, int topK, CancellationToken ct)
    {
        // Las dos búsquedas arrancan juntas y recién después se espera a las dos:
        // se tarda lo que la más lenta, no la suma. Cada una abre su propia conexión.
        Task<List<Fragmento>> porPalabras = PorPalabrasAsync(pregunta, _candidatos, ct);
        Task<List<Fragmento>> porVectores = PorVectoresAsync(pregunta, _candidatos, ct);
        await Task.WhenAll(porPalabras, porVectores);

        List<List<Fragmento>> listas = new List<List<Fragmento>> { await porPalabras, await porVectores };
        return Rrf.Mezclar(listas, topK);
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

    /// <summary>Busca los fragmentos más parecidos en significado a la pregunta: calcula su vector con Ollama, y SQL Server ordena los fragmentos por distancia coseno, del más cercano al más lejano.</summary>
    public async Task<List<Fragmento>> PorVectoresAsync(string pregunta, int topK, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pregunta))
        {
            return new List<Fragmento>();
        }
        float[] vector = await _ollama.EmbeddingPreguntaAsync(pregunta, ct);

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
}
