namespace RagBasico.Api;

/// <summary>Un error de Ollama o de Qdrant, con el nombre del servicio y el código HTTP que corresponde: 503 si no responde, 502 si respondió con un error.</summary>
public sealed class ErrorDeServicio : Exception
{
    /// <summary>Arma el error con el servicio, el código HTTP, el mensaje y, si hay, el error original.</summary>
    public ErrorDeServicio(string servicio, int codigoHttp, string mensaje, Exception? interna)
        : base(mensaje, interna)
    {
        Servicio = servicio;
        CodigoHttp = codigoHttp;
    }

    /// <summary>El servicio que falló: "Ollama" o "Qdrant".</summary>
    public string Servicio { get; }

    /// <summary>El código HTTP que la API devuelve por este error: 503 si el servicio no responde, 502 si respondió con un error.</summary>
    public int CodigoHttp { get; }

    /// <summary>Dice si el error es de cómo está configurado el servicio, como un modelo que no está bajado, y no de lo que se le mandó. Reintentar no lo arregla: hay que corregir la configuración.</summary>
    public bool DeConfiguracion { get; init; }
}
