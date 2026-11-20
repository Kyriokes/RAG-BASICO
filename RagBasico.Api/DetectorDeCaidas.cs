namespace RagBasico.Api;

/// <summary>Va delante del HttpClient de Ollama o de Qdrant: si el servicio no responde, o no responde a tiempo, cambia el error por un ErrorDeServicio que dice cuál es y en qué dirección.</summary>
public sealed class DetectorDeCaidas : DelegatingHandler
{
    private readonly string _servicio;
    private readonly TimeSpan _espera;

    /// <summary>Recibe el nombre del servicio que va a vigilar, para ponerlo en el mensaje, y cuánto esperarlo.</summary>
    public DetectorDeCaidas(string servicio, TimeSpan espera)
    {
        _servicio = servicio;
        _espera = espera;
    }

    /// <summary>Manda el pedido. Si no se puede conectar con el servicio, lanza un ErrorDeServicio con código 503 y la dirección que no respondió; si se vence el tiempo de espera, uno con código 504.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage pedido, CancellationToken ct)
    {
        string direccion = pedido.RequestUri == null ? "" : pedido.RequestUri.GetLeftPart(UriPartial.Authority);
        // Un reloj propio, atado al aviso de cancelación de quien pidió: así se distingue "se venció el tiempo"
        // de "el que pidió se fue". Con el tiempo de espera del HttpClient no se puede saber cuál de los dos fue.
        using (CancellationTokenSource reloj = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            reloj.CancelAfter(_espera);
            try
            {
                return await base.SendAsync(pedido, reloj.Token);
            }
            catch (HttpRequestException ex)
            {
                throw new ErrorDeServicio(_servicio, 503, _servicio + " no responde en " + direccion + ": " + ex.Message, ex);
            }
            catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
            {
                // Nadie canceló el pedido: se venció el reloj. Pasa cuando el servidor está pero no contesta,
                // o cuando un firewall descarta los pedidos sin rechazarlos.
                throw new ErrorDeServicio(_servicio, 504, _servicio + " no respondió a tiempo en " + direccion + " (se esperó " + (int)_espera.TotalSeconds + " segundos). ¿Está andando? ¿Hay un firewall en el medio?", ex);
            }
        }
    }
}
