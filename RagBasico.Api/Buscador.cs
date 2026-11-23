using Dapper;
using Microsoft.Data.SqlClient;

namespace RagBasico.Api;

/// <summary>Busca los fragmentos que mejor responden una pregunta: por palabras en SQL Server, por significado en Qdrant, o con las dos mezcladas.</summary>
public sealed class Buscador
{
    /// <summary>Los modos de búsqueda que existen.</summary>
    public static readonly string[] Modos = new string[] { "palabras", "vectores", "hibrida" };

    /// <summary>El modo que se usa cuando el pedido no dice cuál.</summary>
    public const string ModoPorDefecto = "hibrida";

    // Las columnas que traen las dos consultas, en el mismo orden y con los mismos nombres que el record Fragmento:
    // Dapper arma el record con su constructor, y si no coinciden falla al ejecutar.
    private const string Columnas = "f.FragmentoId, d.DocumentoId, d.Archivo, f.Pagina, f.PaginaFin, f.Texto";

    private readonly Ollama _ollama;
    private readonly Qdrant _qdrant;
    private readonly string _conexion;
    private readonly int _candidatos;

    /// <summary>Recibe los clientes de Ollama, para el vector de la pregunta, y de Qdrant; lee de la configuración la cadena de conexión y cuántos candidatos pide la híbrida a cada búsqueda.</summary>
    public Buscador(Ollama ollama, Qdrant qdrant, IConfiguration config)
    {
        _ollama = ollama;
        _qdrant = qdrant;
        _conexion = config["Conexiones:SqlTextos"] ?? throw new InvalidOperationException("Falta Conexiones:SqlTextos en la configuración.");
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

    /// <summary>Busca con el modo pedido, que tiene que ser uno de Modos, y devuelve los fragmentos del mejor al peor.</summary>
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

    /// <summary>Busca por palabras y por significado al mismo tiempo, pide los mejores candidatos de cada una y los mezcla por puesto con RRF. Si Qdrant no responde, falla con un error que lo dice.</summary>
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
        // y devuelve cada fragmento con un puntaje (RANK). Se piden los 200 mejores y se usan los topK primeros.
        // Solo cuentan los documentos listos: uno a medio procesar todavía no tiene sus vectores. Por eso se piden
        // 200 y no menos: si los primeros son de un documento a medio procesar, igual quedan para elegir.
        const string sql = @"
SELECT TOP (@topK) " + Columnas + @"
FROM FREETEXTTABLE(rag.Fragmento, Texto, @pregunta, LANGUAGE 3082, 200) AS ft
JOIN rag.Fragmento AS f ON f.FragmentoId = ft.[KEY]
JOIN rag.Documento AS d ON d.DocumentoId = f.DocumentoId
WHERE d.Estado = 'listo'
ORDER BY ft.[RANK] DESC;";
        using (SqlConnection conexion = new SqlConnection(_conexion))
        {
            IEnumerable<Fragmento> filas = await conexion.QueryAsync<Fragmento>(
                new CommandDefinition(sql, new { pregunta, topK }, cancellationToken: ct));
            return filas.ToList();
        }
    }

    /// <summary>Busca los fragmentos más parecidos en significado a la pregunta: calcula el vector de la pregunta con Ollama, le pide los más cercanos a Qdrant y trae sus textos de SQL Server.</summary>
    public async Task<List<Fragmento>> PorVectoresAsync(string pregunta, int topK, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pregunta))
        {
            return new List<Fragmento>();
        }
        float[] vector = await _ollama.EmbeddingPreguntaAsync(pregunta, ct);
        // Si todavía no se cargó ningún documento, la colección no existe: no hay nada que buscar.
        if (!await _qdrant.ExisteColeccionAsync(ct))
        {
            return new List<Fragmento>();
        }
        List<int> ids = await _qdrant.BuscarAsync(vector, topK, ct);
        return await LeerFragmentosAsync(ids, ct);
    }

    /// <summary>Trae de SQL Server los fragmentos de una lista de números, en el mismo orden de la lista. Los que ya no existen en SQL, o son de un documento que no está listo, se saltean.</summary>
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
                "SELECT " + Columnas + " FROM rag.Fragmento AS f " +
                "JOIN rag.Documento AS d ON d.DocumentoId = f.DocumentoId " +
                "WHERE f.FragmentoId IN @ids AND d.Estado = 'listo';",
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
