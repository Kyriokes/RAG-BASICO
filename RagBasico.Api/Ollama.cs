using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace RagBasico.Api;

/// <summary>Habla con Ollama por HTTP: manda el mismo JSON que se mandaría con Postman.</summary>
public sealed class Ollama
{
    /// <summary>Cuántos números tiene cada vector de qwen3-embedding:0.6b. La columna rag.Fragmento.Embedding tiene el mismo tamaño.</summary>
    public const int Dimension = 1024;

    private readonly HttpClient _http;
    private readonly string _modeloChat;
    private readonly string _modeloEmbeddings;
    private readonly string _instruccion;
    private readonly string _mantenerCargado;

    /// <summary>Recibe el HttpClient que arma Program.cs y lee de la configuración los modelos y la instrucción de las preguntas.</summary>
    public Ollama(HttpClient http, IConfiguration config)
    {
        _http = http;
        _modeloChat = config["Ollama:ModeloChat"] ?? "qwen3.5:4b";
        _modeloEmbeddings = config["Ollama:ModeloEmbeddings"] ?? "qwen3-embedding:0.6b";
        _instruccion = config["Ollama:InstruccionPregunta"] ?? "Given a question, retrieve relevant passages that answer the question";
        _mantenerCargado = config["Ollama:MantenerCargado"] ?? "30m";
    }

    /// <summary>Le pide una respuesta al modelo de chat y devuelve el texto con los tiempos de Ollama.</summary>
    public async Task<RespuestaModelo> ChatAsync(string sistema, string usuario, CancellationToken ct)
    {
        var pedido = new
        {
            model = _modeloChat,
            stream = false,
            // qwen3.5 piensa antes de responder si no se le dice lo contrario, y eso multiplica el tiempo.
            think = false,
            keep_alive = _mantenerCargado,
            options = new { temperature = 0.3, top_p = 0.8, top_k = 20, num_ctx = 8192, num_predict = 700 },
            messages = new[]
            {
                new { role = "system", content = sistema },
                new { role = "user", content = usuario }
            }
        };

        using (HttpResponseMessage respuesta = await _http.PostAsJsonAsync("/api/chat", pedido, ct))
        {
            await VerificarAsync(respuesta, ct);
            RespuestaChat? cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaChat>(ct);
            if (cuerpo == null || cuerpo.Message == null || cuerpo.Message.Content == null)
            {
                throw new InvalidOperationException("Ollama devolvió una respuesta sin texto.");
            }

            // Si alguna versión de Ollama deja el razonamiento en el texto, se descarta.
            string texto = cuerpo.Message.Content;
            int finRazonamiento = texto.IndexOf("</think>", StringComparison.OrdinalIgnoreCase);
            if (finRazonamiento >= 0)
            {
                texto = texto.Substring(finRazonamiento + "</think>".Length);
            }

            return new RespuestaModelo(
                texto.Trim(),
                NanosAMilisegundos(cuerpo.LoadDuration),
                NanosAMilisegundos(cuerpo.PromptEvalDuration),
                NanosAMilisegundos(cuerpo.EvalDuration),
                cuerpo.PromptEvalCount,
                cuerpo.EvalCount);
        }
    }

    /// <summary>Calcula el vector de cada texto con el modelo de embeddings, todos en un solo pedido. Los fragmentos van así, sin instrucción.</summary>
    public async Task<List<float[]>> EmbeddingsAsync(List<string> textos, CancellationToken ct)
    {
        var pedido = new
        {
            model = _modeloEmbeddings,
            input = textos,
            keep_alive = _mantenerCargado
        };

        using (HttpResponseMessage respuesta = await _http.PostAsJsonAsync("/api/embed", pedido, ct))
        {
            await VerificarAsync(respuesta, ct);
            RespuestaEmbed? cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaEmbed>(ct);
            if (cuerpo == null || cuerpo.Embeddings == null || cuerpo.Embeddings.Count != textos.Count)
            {
                throw new InvalidOperationException("Ollama no devolvió un vector por cada texto.");
            }
            // Si alguien cambia el modelo por otro de otra dimensión, mejor un error claro que vectores que SQL no acepta.
            foreach (float[] vector in cuerpo.Embeddings)
            {
                if (vector.Length != Dimension)
                {
                    throw new InvalidOperationException("El modelo " + _modeloEmbeddings + " devolvió vectores de " + vector.Length + " números, y la base espera " + Dimension + ".");
                }
            }
            return cuerpo.Embeddings;
        }
    }

    /// <summary>Calcula el vector de una pregunta. Le antepone la instrucción con el formato de la ficha del modelo: "Instruct: ", la instrucción, un salto de línea y "Query:" pegado a la pregunta.</summary>
    public async Task<float[]> EmbeddingPreguntaAsync(string pregunta, CancellationToken ct)
    {
        string texto = "Instruct: " + _instruccion + "\nQuery:" + pregunta;
        List<float[]> vectores = await EmbeddingsAsync(new List<string> { texto }, ct);
        return vectores[0];
    }

    /// <summary>Lista los nombres de los modelos que tiene instalados Ollama, como "qwen3.5:4b".</summary>
    public async Task<List<string>> ListarModelosAsync(CancellationToken ct)
    {
        using (HttpResponseMessage respuesta = await _http.GetAsync("/api/tags", ct))
        {
            await VerificarAsync(respuesta, ct);
            RespuestaModelos? cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaModelos>(ct);
            List<string> nombres = new List<string>();
            if (cuerpo != null && cuerpo.Models != null)
            {
                foreach (ModeloInstalado modelo in cuerpo.Models)
                {
                    if (modelo.Name != null)
                    {
                        nombres.Add(modelo.Name);
                    }
                }
            }
            return nombres;
        }
    }

    /// <summary>Carga los dos modelos en memoria con un pedido mínimo a cada uno, así la primera pregunta de la demo no espera la carga.</summary>
    public async Task CalentarAsync(CancellationToken ct)
    {
        await EmbeddingsAsync(new List<string> { "hola" }, ct);
        await ChatAsync("Respondé solo con la palabra: listo.", "Hola", ct);
    }

    /// <summary>Si Ollama contesta con un error, lanza una excepción con su mensaje real (por ejemplo, "model not found").</summary>
    private static async Task VerificarAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return;
        }
        string detalle = await respuesta.Content.ReadAsStringAsync(ct);
        throw new ErrorDeServicio("Ollama", 502, "Ollama respondió " + (int)respuesta.StatusCode + ": " + detalle, null);
    }

    /// <summary>Pasa un tiempo de Ollama, que viene en nanosegundos, a milisegundos.</summary>
    private static int NanosAMilisegundos(long nanosegundos)
    {
        return (int)(nanosegundos / 1000000);
    }

    /// <summary>La parte de la respuesta de /api/chat que se usa.</summary>
    private sealed record RespuestaChat(
        [property: JsonPropertyName("message")] MensajeChat? Message,
        [property: JsonPropertyName("load_duration")] long LoadDuration,
        [property: JsonPropertyName("prompt_eval_count")] int PromptEvalCount,
        [property: JsonPropertyName("prompt_eval_duration")] long PromptEvalDuration,
        [property: JsonPropertyName("eval_count")] int EvalCount,
        [property: JsonPropertyName("eval_duration")] long EvalDuration);

    /// <summary>El mensaje que escribió el modelo.</summary>
    private sealed record MensajeChat([property: JsonPropertyName("content")] string? Content);

    /// <summary>La parte de la respuesta de /api/embed que se usa: un vector por texto, en el mismo orden.</summary>
    private sealed record RespuestaEmbed([property: JsonPropertyName("embeddings")] List<float[]>? Embeddings);

    /// <summary>La respuesta de /api/tags: los modelos instalados.</summary>
    private sealed record RespuestaModelos([property: JsonPropertyName("models")] List<ModeloInstalado>? Models);

    /// <summary>Un modelo instalado en Ollama.</summary>
    private sealed record ModeloInstalado([property: JsonPropertyName("name")] string? Name);
}
