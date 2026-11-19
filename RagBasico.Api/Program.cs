using RagBasico.Api;

var builder = WebApplication.CreateBuilder(args);

// Ollama como cliente HTTP: la dirección y el tiempo de espera salen de la configuración.
builder.Services.AddHttpClient<Ollama>(cliente =>
{
    cliente.BaseAddress = new Uri(builder.Configuration["Ollama:Url"] ?? "http://127.0.0.1:11434");
    cliente.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Ollama:SegundosDeEspera", 300));
});
// Un objeto nuevo de cada uno por pedido HTTP.
builder.Services.AddScoped<Indexador>();
builder.Services.AddScoped<Buscador>();
builder.Services.AddScoped<Redactor>();
builder.Services.AddScoped<Evaluador>();

var app = builder.Build();

// Al arrancar, la API dice qué carpeta de PDF va a usar, o avisa que no existe.
string carpeta = app.Configuration["Rag:CarpetaDocumentos"] ?? "";
if (carpeta.Length > 0 && Directory.Exists(carpeta))
{
    app.Logger.LogInformation("Carpeta de PDF: {Carpeta}", Path.GetFullPath(carpeta));
}
else
{
    app.Logger.LogWarning("No existe la carpeta de PDF '{Carpeta}'. Configurá la variable de entorno Rag__CarpetaDocumentos.", carpeta);
}

app.MapGet("/", () => new
{
    proyecto = app.Configuration["Rag:Proyecto"],
    fase = app.Configuration["Rag:Fase"],
    dotnet = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
});

app.MapPost("/indexar", async (Indexador indexador, CancellationToken ct) =>
    await indexador.IndexarCarpetaAsync(ct));

app.MapGet("/buscar", async (string q, Buscador buscador, CancellationToken ct) =>
    await buscador.PorPalabrasAsync(q, app.Configuration.GetValue("Rag:TopK", 5), ct));

app.MapPost("/preguntar", async (PedidoPregunta pedido, Redactor redactor, CancellationToken ct) =>
    await redactor.ResponderAsync(pedido.Pregunta, ct));

app.MapPost("/evaluar", async (Evaluador evaluador, CancellationToken ct) =>
    await evaluador.CorrerAsync(ct));

app.Run();
