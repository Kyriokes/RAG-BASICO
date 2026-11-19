using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace RagBasico.Api;

/// <summary>Atrapa los errores que nadie atrapó y responde un problema claro, con el formato estándar ProblemDetails, en lugar de un 500 genérico.</summary>
public sealed class ManejadorDeErrores : IExceptionHandler
{
    private readonly ILogger<ManejadorDeErrores> _log;

    /// <summary>Recibe el log, donde anota cada error con su detalle completo.</summary>
    public ManejadorDeErrores(ILogger<ManejadorDeErrores> log)
    {
        _log = log;
    }

    /// <summary>Anota el error en el log y responde el problema que le corresponde. Devuelve true: el error quedó atendido.</summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception error, CancellationToken ct)
    {
        ProblemDetails problema = Describir(error);
        _log.LogError(error, "Error en {Ruta}: {Titulo}", contexto.Request.Path, problema.Title);
        contexto.Response.StatusCode = problema.Status ?? 500;
        await contexto.Response.WriteAsJsonAsync(problema, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json", ct);
        return true;
    }

    /// <summary>Traduce un error a un problema: qué pasó, qué hacer y el código HTTP. Un servicio caído es 503, uno que respondió con un error 502, uno que tardó demasiado 504, un pedido que no se pudo leer 400, y el resto 500.</summary>
    public static ProblemDetails Describir(Exception error)
    {
        ErrorDeServicio? deServicio = error as ErrorDeServicio;
        if (deServicio != null)
        {
            string titulo = deServicio.CodigoHttp == 503 ? deServicio.Servicio + " no responde" : deServicio.Servicio + " respondió con un error";
            return Problema(deServicio.CodigoHttp, titulo, deServicio.Message);
        }
        if (error is SqlException)
        {
            return Problema(503, "SQL Server no responde o devolvió un error",
                error.Message + " Revisá que esté andando el servicio de SQL Server y que exista la base: scripts\\crear-base.ps1 la crea.");
        }
        if (error is TaskCanceledException && error.InnerException is TimeoutException)
        {
            return Problema(504, "Un servicio tardó demasiado",
                "Ollama o Qdrant no respondieron a tiempo (Ollama:SegundosDeEspera y Qdrant:SegundosDeEspera). " + error.Message);
        }
        // Un pedido que ASP.NET Core no pudo leer, como un JSON roto o un parámetro que falta, ya trae su código (400).
        BadHttpRequestException? malArmado = error as BadHttpRequestException;
        if (malArmado != null)
        {
            return Problema(malArmado.StatusCode, "El pedido no es válido", error.Message);
        }
        // Un ArgumentException no es culpa de quien pregunta: lo que manda el cliente ya lo controlan el Validador y
        // Buscador.ElegirModo. Si aparece, es de la API, por ejemplo una configuración inválida, y es un 500.
        return Problema(500, "Error inesperado", error.Message);
    }

    /// <summary>Arma un ProblemDetails con el código, el título y el detalle.</summary>
    private static ProblemDetails Problema(int codigo, string titulo, string detalle)
    {
        ProblemDetails problema = new ProblemDetails();
        problema.Status = codigo;
        problema.Title = titulo;
        problema.Detail = detalle;
        return problema;
    }
}
