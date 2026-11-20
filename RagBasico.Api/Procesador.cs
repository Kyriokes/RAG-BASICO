namespace RagBasico.Api;

/// <summary>El trabajo en segundo plano: arranca con la API y da vueltas procesando de a un documento pendiente. Si se apaga la API, al volver sigue donde quedó, porque el estado está en la base.</summary>
public sealed class Procesador : BackgroundService
{
    // Cuánto espera cuando no hay nada para procesar, y cuánto cuando un servidor no responde.
    private static readonly TimeSpan EsperaSinTrabajo = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan EsperaConError = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _alcances;
    private readonly ILogger<Procesador> _log;

    /// <summary>Recibe la fábrica de alcances, para pedir un Cargador nuevo en cada vuelta, y el log.</summary>
    public Procesador(IServiceScopeFactory alcances, ILogger<Procesador> log)
    {
        _alcances = alcances;
        _log = log;
    }

    /// <summary>El bucle: si hay un pendiente lo procesa y sigue; si no hay, espera 5 segundos; si un servidor no responde, lo anota una vez y espera 30. Termina cuando se apaga la API.</summary>
    protected override async Task ExecuteAsync(CancellationToken apagado)
    {
        string ultimoError = "";
        while (!apagado.IsCancellationRequested)
        {
            TimeSpan espera = TimeSpan.Zero;
            try
            {
                // El Cargador vive lo que dura un documento: se pide uno nuevo en cada vuelta, como en cada pedido HTTP.
                using (IServiceScope alcance = _alcances.CreateScope())
                {
                    Cargador cargador = alcance.ServiceProvider.GetRequiredService<Cargador>();
                    bool habia = await cargador.ProcesarSiguienteAsync(apagado);
                    if (!habia)
                    {
                        espera = EsperaSinTrabajo;
                    }
                }
                ultimoError = "";
            }
            catch (OperationCanceledException) when (apagado.IsCancellationRequested)
            {
                // Se está apagando la API: el documento queda pendiente y se sigue la próxima vez.
                break;
            }
            catch (Exception ex)
            {
                // El mismo error no se repite en el log cada 30 segundos: se anota cuando cambia.
                if (ex.Message != ultimoError)
                {
                    _log.LogWarning("La carga de documentos está frenada: {Mensaje} Se reintenta cada 30 segundos.", ex.Message);
                    ultimoError = ex.Message;
                }
                espera = EsperaConError;
            }

            if (espera > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(espera, apagado);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
