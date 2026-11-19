namespace RagBasico.Api;

/// <summary>Va delante del HttpClient de Ollama o de Qdrant: si el servicio no responde, cambia el error de conexión por un ErrorDeServicio que dice cuál es.</summary>
public sealed class DetectorDeCaidas : DelegatingHandler
{
    private readonly string _servicio;

    /// <summary>Recibe el nombre del servicio que va a vigilar, para ponerlo en el mensaje.</summary>
    public DetectorDeCaidas(string servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Manda el pedido. Si no se puede conectar con el servicio, lanza un ErrorDeServicio con código 503 y la dirección que no respondió.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage pedido, CancellationToken ct)
    {
        try
        {
            return await base.SendAsync(pedido, ct);
        }
        catch (HttpRequestException ex)
        {
            string direccion = pedido.RequestUri == null ? "" : pedido.RequestUri.GetLeftPart(UriPartial.Authority);
            throw new ErrorDeServicio(_servicio, 503, _servicio + " no responde en " + direccion + ": " + ex.Message, ex);
        }
    }
}
