using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace RagBasico.Api;

/// <summary>Habla con Ollama por HTTP: manda el mismo JSON que se mandaría con Postman.</summary>
public sealed class Ollama
{
    private readonly HttpClient _http;
    private readonly string _modeloChat;
    private readonly string _mantenerCargado;

    /// <summary>Recibe el HttpClient que arma Program.cs y lee el modelo de la configuración.</summary>
    public Ollama(HttpClient http, IConfiguration config)
    {
        _http = http;
        _modeloChat = config["Ollama:ModeloChat"] ?? "qwen3.5:4b";
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

    /// <summary>Si Ollama contesta con un error, lanza una excepción con su mensaje real (por ejemplo, "model not found").</summary>
    private static async Task VerificarAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return;
        }
        string detalle = await respuesta.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException("Ollama respondió " + (int)respuesta.StatusCode + ": " + detalle);
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
}
