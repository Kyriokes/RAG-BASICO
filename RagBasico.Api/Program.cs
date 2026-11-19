using RagBasico.Api;

var builder = WebApplication.CreateBuilder(args);

// Ollama como cliente HTTP: la dirección y el tiempo de espera salen de la configuración.
builder.Services.AddHttpClient<Ollama>(cliente =>
{
    cliente.BaseAddress = new Uri(builder.Configuration["Ollama:Url"] ?? "http://127.0.0.1:11434");
    cliente.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Ollama:SegundosDeEspera", 300));
});
// Qdrant también como cliente HTTP. El tiempo de espera es corto: si Qdrant no responde, la híbrida sigue con SQL.
builder.Services.AddHttpClient<Qdrant>(cliente =>
{
    cliente.BaseAddress = new Uri(builder.Configuration["Qdrant:Url"] ?? "http://127.0.0.1:6333");
    cliente.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Qdrant:SegundosDeEspera", 10));
});
// Un objeto nuevo de cada uno por pedido HTTP.
builder.Services.AddScoped<Sincronizador>();
builder.Services.AddScoped<Indexador>();
builder.Services.AddScoped<Vectorizador>();
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

// La página de la demo: wwwroot\index.html. UseDefaultFiles hace que "/" muestre index.html,
// y UseStaticFiles sirve los archivos de wwwroot. Por eso la información del proyecto pasa a /info.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/info", () => new
{
    proyecto = app.Configuration["Rag:Proyecto"],
    fase = app.Configuration["Rag:Fase"],
    dotnet = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
});

app.MapPost("/indexar", async (Indexador indexador, CancellationToken ct) =>
    await indexador.IndexarCarpetaAsync(ct));

app.MapPost("/vectores", async (Vectorizador vectorizador, CancellationToken ct) =>
    await vectorizador.CalcularPendientesAsync(ct));

// Un modo mal escrito da un error 400 que lo dice, en lugar de buscar con otro modo sin avisar.
app.MapGet("/buscar", async (string q, string? modo, Buscador buscador, CancellationToken ct) =>
{
    string? elegido = Buscador.ElegirModo(modo);
    if (elegido == null)
    {
        return Results.BadRequest(new { error = "Modo desconocido: '" + modo + "'. Los modos son: " + string.Join(", ", Buscador.Modos) + "." });
    }
    return Results.Ok(await buscador.BuscarAsync(q, elegido, app.Configuration.GetValue("Rag:TopK", 5), ct));
});

app.MapPost("/preguntar", async (PedidoPregunta pedido, Redactor redactor, CancellationToken ct) =>
{
    string? elegido = Buscador.ElegirModo(pedido.Modo);
    if (elegido == null)
    {
        return Results.BadRequest(new { error = "Modo desconocido: '" + pedido.Modo + "'. Los modos son: " + string.Join(", ", Buscador.Modos) + "." });
    }
    return Results.Ok(await redactor.ResponderAsync(pedido.Pregunta, elegido, ct));
});

app.MapPost("/evaluar", async (Evaluador evaluador, CancellationToken ct) =>
    await evaluador.CorrerAsync(ct));

app.MapGet("/qdrant/estado", async (Sincronizador sincronizador, CancellationToken ct) =>
    await sincronizador.EstadoAsync(ct));

app.MapPost("/qdrant/reconstruir", async (Sincronizador sincronizador, CancellationToken ct) =>
    await sincronizador.ReconstruirAsync(ct));

app.Run();
