namespace RagBasico.Api;

/// <summary>Reglas sobre los errores que comparten varias piezas.</summary>
public static class Errores
{
    /// <summary>Dice si un error muestra que un servicio, Ollama o Qdrant, no responde: no se pudo conectar, o no contestó a tiempo. Un error que el servicio sí contestó, como un texto que Ollama no acepta o una colección que no existe en Qdrant, no lo es.</summary>
    public static bool EsCaida(Exception error)
    {
        // El DetectorDeCaidas cambia el error de conexión por un ErrorDeServicio con código 503, y el de tiempo vencido por uno con 504.
        // Uno con 502 es el servicio que contestó con un error: no es una caída.
        ErrorDeServicio? deServicio = error as ErrorDeServicio;
        if (deServicio != null)
        {
            return deServicio.CodigoHttp == 503 || deServicio.CodigoHttp == 504;
        }
        return error is HttpRequestException || (error is TaskCanceledException && error.InnerException is TimeoutException);
    }
}
