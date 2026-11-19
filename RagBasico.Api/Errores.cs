namespace RagBasico.Api;

/// <summary>Reglas sobre los errores que comparten varias piezas.</summary>
public static class Errores
{
    /// <summary>Dice si un error muestra que un servicio, Ollama o Qdrant, no responde: no se pudo conectar, o no contestó a tiempo. Un error que el servicio sí contestó, como un texto que Ollama no acepta o una colección que no existe en Qdrant, no lo es.</summary>
    public static bool EsCaida(Exception error)
    {
        return error is HttpRequestException || (error is TaskCanceledException && error.InnerException is TimeoutException);
    }
}
