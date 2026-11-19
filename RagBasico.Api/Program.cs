using System.Diagnostics;
using RagBasico.Api;

var builder = WebApplication.CreateBuilder(args);

// Ollama como cliente HTTP: la dirección y el tiempo de espera salen de la configuración.
builder.Services.AddHttpClient<Ollama>(cliente =>
{
    cliente.BaseAddress = new Uri(builder.Configuration["Ollama:Url"] ?? "http://127.0.0.1:11434");
    cliente.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Ollama:SegundosDeEspera", 300));
});

var app = builder.Build();

app.MapGet("/", () => new
{
    proyecto = app.Configuration["Rag:Proyecto"],
    fase = app.Configuration["Rag:Fase"],
    dotnet = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
});

app.MapPost("/preguntar", async (PedidoPregunta pedido, Ollama ollama, CancellationToken ct) =>
{
    Stopwatch reloj = Stopwatch.StartNew();
    RespuestaModelo respuesta = await ollama.ChatAsync("Respondé en castellano, en pocas oraciones.", pedido.Pregunta, ct);
    double tokensPorSegundo = 0;
    if (respuesta.MsEscritura > 0)
    {
        tokensPorSegundo = Math.Round(respuesta.TokensRespuesta * 1000.0 / respuesta.MsEscritura, 1);
    }
    return new
    {
        texto = respuesta.Texto,
        msTotal = reloj.ElapsedMilliseconds,
        msCarga = respuesta.MsCarga,
        msLectura = respuesta.MsLectura,
        msEscritura = respuesta.MsEscritura,
        tokensPorSegundo = tokensPorSegundo
    };
});

app.Run();
