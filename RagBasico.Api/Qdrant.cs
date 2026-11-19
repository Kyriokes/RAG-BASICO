using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RagBasico.Api;

/// <summary>Habla con Qdrant por HTTP: crea y borra la colección, sube y borra puntos, y busca los vectores más cercanos.</summary>
public sealed class Qdrant
{
    // Los pedidos se mandan sin los campos en null: por ejemplo, la primera página de un recorrido va sin "offset".
    private static readonly JsonSerializerOptions SinNulos = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly string _coleccion;

    /// <summary>Recibe el HttpClient que arma Program.cs y lee el nombre de la colección de la configuración.</summary>
    public Qdrant(HttpClient http, IConfiguration config)
    {
        _http = http;
        _coleccion = config["Qdrant:Coleccion"] ?? "fragmentos";
    }

    /// <summary>El nombre de la colección de esta fase.</summary>
    public string Coleccion
    {
        get { return _coleccion; }
    }

    /// <summary>Devuelve la versión de Qdrant. Sirve para saber si está andando.</summary>
    public async Task<string> LeerVersionAsync(CancellationToken ct)
    {
        using (HttpResponseMessage respuesta = await _http.GetAsync("/", ct))
        {
            await VerificarAsync(respuesta, ct);
            using (JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct)))
            {
                return documento.RootElement.GetProperty("version").GetString() ?? "";
            }
        }
    }

    /// <summary>Dice si la colección existe.</summary>
    public async Task<bool> ExisteColeccionAsync(CancellationToken ct)
    {
        using (HttpResponseMessage respuesta = await _http.GetAsync("/collections/" + _coleccion + "/exists", ct))
        {
            JsonElement resultado = await LeerResultadoAsync(respuesta, ct);
            return resultado.GetProperty("exists").GetBoolean();
        }
    }

    /// <summary>Crea la colección: vectores de 1024 números comparados por coseno, con los dos umbrales bajos para que el índice HNSW se arme y se use aun con pocos vectores.</summary>
    public async Task CrearColeccionAsync(CancellationToken ct)
    {
        var pedido = new
        {
            vectors = new { size = Ollama.Dimension, distance = "Cosine" },
            // Los dos umbrales se miden en kilobytes, y cada vector de 1024 números ocupa 4 KB.
            // Con los valores por defecto (10.000 KB, unos 2.500 vectores) Qdrant no arma el índice
            // o no lo usa, y con pocos miles de fragmentos buscaría recorriéndolos todos, igual que SQL.
            hnsw_config = new { full_scan_threshold = 10 },
            optimizers_config = new { indexing_threshold = 10 }
        };
        using (HttpResponseMessage respuesta = await _http.PutAsJsonAsync("/collections/" + _coleccion, pedido, ct))
        {
            await VerificarAsync(respuesta, ct);
        }
    }

    /// <summary>Borra la colección con todos sus puntos. Devuelve lo que contestó Qdrant: true si la borró.</summary>
    public async Task<bool> BorrarColeccionAsync(CancellationToken ct)
    {
        using (HttpResponseMessage respuesta = await _http.DeleteAsync("/collections/" + _coleccion, ct))
        {
            JsonElement resultado = await LeerResultadoAsync(respuesta, ct);
            return resultado.ValueKind == JsonValueKind.True;
        }
    }

    /// <summary>Lee el estado de la colección: el color que informa Qdrant, cuántos puntos tiene y cuántos vectores están en el índice HNSW.</summary>
    public async Task<InfoColeccion> LeerInfoAsync(CancellationToken ct)
    {
        using (HttpResponseMessage respuesta = await _http.GetAsync("/collections/" + _coleccion, ct))
        {
            JsonElement resultado = await LeerResultadoAsync(respuesta, ct);
            return new InfoColeccion(
                resultado.GetProperty("status").GetString() ?? "",
                LeerEntero(resultado, "points_count"),
                LeerEntero(resultado, "indexed_vectors_count"));
        }
    }

    /// <summary>Sube puntos a la colección: el número de fragmento como id, el vector, y el número de documento como dato extra. Si el id ya existe, lo pisa.</summary>
    public async Task SubirAsync(List<PuntoQdrant> puntos, CancellationToken ct)
    {
        var pedido = new
        {
            points = puntos.Select(p => new
            {
                id = p.Id,
                vector = p.Vector,
                payload = new { documentoId = p.DocumentoId }
            })
        };
        // wait=true: Qdrant contesta cuando los puntos ya están guardados, no antes.
        using (HttpResponseMessage respuesta = await _http.PutAsJsonAsync("/collections/" + _coleccion + "/points?wait=true", pedido, ct))
        {
            await VerificarAsync(respuesta, ct);
        }
    }

    /// <summary>Borra puntos de la colección por su id, que es el número de fragmento.</summary>
    public async Task BorrarPuntosAsync(List<int> ids, CancellationToken ct)
    {
        var pedido = new { points = ids };
        using (HttpResponseMessage respuesta = await _http.PostAsJsonAsync("/collections/" + _coleccion + "/points/delete?wait=true", pedido, ct))
        {
            await VerificarAsync(respuesta, ct);
        }
    }

    /// <summary>Lista los ids de todos los puntos de la colección, recorriéndola de a 1.000.</summary>
    public async Task<List<int>> ListarIdsAsync(CancellationToken ct)
    {
        List<int> ids = new List<int>();
        int? desde = null;
        do
        {
            var pedido = new { limit = 1000, with_payload = false, with_vector = false, offset = desde };
            using (HttpResponseMessage respuesta = await _http.PostAsJsonAsync("/collections/" + _coleccion + "/points/scroll", pedido, SinNulos, ct))
            {
                JsonElement resultado = await LeerResultadoAsync(respuesta, ct);
                foreach (JsonElement punto in resultado.GetProperty("points").EnumerateArray())
                {
                    ids.Add(punto.GetProperty("id").GetInt32());
                }
                // Qdrant dice desde qué id sigue la próxima página, o null si no hay más.
                JsonElement siguiente;
                desde = null;
                if (resultado.TryGetProperty("next_page_offset", out siguiente) && siguiente.ValueKind == JsonValueKind.Number)
                {
                    desde = siguiente.GetInt32();
                }
            }
        }
        while (desde != null);
        return ids;
    }

    /// <summary>Lee el vector guardado de un punto. Devuelve null si el punto no existe.</summary>
    public async Task<float[]?> LeerVectorAsync(int id, CancellationToken ct)
    {
        var pedido = new { ids = new int[] { id }, with_vector = true, with_payload = false };
        using (HttpResponseMessage respuesta = await _http.PostAsJsonAsync("/collections/" + _coleccion + "/points", pedido, ct))
        {
            JsonElement resultado = await LeerResultadoAsync(respuesta, ct);
            foreach (JsonElement punto in resultado.EnumerateArray())
            {
                return punto.GetProperty("vector").Deserialize<float[]>();
            }
            return null;
        }
    }

    /// <summary>Busca los puntos más cercanos a un vector con el índice HNSW. Devuelve sus ids, del más cercano al más lejano.</summary>
    public async Task<List<int>> BuscarAsync(float[] vector, int topK, CancellationToken ct)
    {
        var pedido = new { query = vector, limit = topK, with_payload = false };
        using (HttpResponseMessage respuesta = await _http.PostAsJsonAsync("/collections/" + _coleccion + "/points/query", pedido, ct))
        {
            JsonElement resultado = await LeerResultadoAsync(respuesta, ct);
            List<int> ids = new List<int>();
            foreach (JsonElement punto in resultado.GetProperty("points").EnumerateArray())
            {
                ids.Add(punto.GetProperty("id").GetInt32());
            }
            return ids;
        }
    }

    /// <summary>Controla la respuesta y devuelve su campo "result", donde Qdrant pone siempre el resultado.</summary>
    private static async Task<JsonElement> LeerResultadoAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        await VerificarAsync(respuesta, ct);
        using (JsonDocument documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct)))
        {
            // Clone: el documento se libera al salir del using, y el resultado tiene que seguir existiendo.
            return documento.RootElement.GetProperty("result").Clone();
        }
    }

    /// <summary>Si Qdrant contesta con un error, lanza una excepción con su mensaje real.</summary>
    private static async Task VerificarAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return;
        }
        string detalle = await respuesta.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException("Qdrant respondió " + (int)respuesta.StatusCode + ": " + detalle);
    }

    /// <summary>Lee un número entero de un campo, o 0 si el campo no está o viene en null.</summary>
    private static int LeerEntero(JsonElement elemento, string campo)
    {
        JsonElement valor;
        if (elemento.TryGetProperty(campo, out valor) && valor.ValueKind == JsonValueKind.Number)
        {
            return valor.GetInt32();
        }
        return 0;
    }
}
