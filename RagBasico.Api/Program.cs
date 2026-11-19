using RagBasico.Api;

var builder = WebApplication.CreateBuilder(args);

// Ollama como cliente HTTP: la dirección y el tiempo de espera salen de la configuración.
builder.Services.AddHttpClient<Ollama>(cliente =>
{
    cliente.BaseAddress = new Uri(builder.Configuration["Ollama:Url"] ?? "http://127.0.0.1:11434");
    cliente.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Ollama:SegundosDeEspera", 300));
}).AddHttpMessageHandler(() => new DetectorDeCaidas("Ollama"));
// Qdrant también como cliente HTTP. El tiempo de espera es corto: si Qdrant no responde, la híbrida sigue con SQL.
builder.Services.AddHttpClient<Qdrant>(cliente =>
{
    cliente.BaseAddress = new Uri(builder.Configuration["Qdrant:Url"] ?? "http://127.0.0.1:6333");
    cliente.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Qdrant:SegundosDeEspera", 10));
}).AddHttpMessageHandler(() => new DetectorDeCaidas("Qdrant"));
// Un objeto nuevo de cada uno por pedido HTTP.
builder.Services.AddScoped<Sincronizador>();
builder.Services.AddScoped<Indexador>();
builder.Services.AddScoped<Vectorizador>();
builder.Services.AddScoped<Buscador>();
builder.Services.AddScoped<Redactor>();
builder.Services.AddScoped<Evaluador>();
builder.Services.AddScoped<Salud>();
// Los errores salen como ProblemDetails: un JSON con título, detalle y código HTTP, en lugar de un 500 genérico.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorDeErrores>();

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

// Cuando la API ya está escuchando, carga los dos modelos en Ollama en segundo plano:
// así la primera pregunta de la demo no espera 30 segundos. Si falla, solo queda en el log.
app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        using (IServiceScope alcance = app.Services.CreateScope())
        {
            Ollama ollama = alcance.ServiceProvider.GetRequiredService<Ollama>();
            try
            {
                await ollama.CalentarAsync(CancellationToken.None);
                app.Logger.LogInformation("Los dos modelos quedaron cargados en Ollama.");
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning("No se pudieron cargar los modelos en Ollama al arrancar: {Mensaje}", ex.Message);
            }
        }
    });
});

// El manejador de errores va primero: atiende los errores de todo lo que viene después.
app.UseExceptionHandler();

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

// 503 si falta algo sin lo que la demo no anda. Con avisos, como Qdrant apagado, 200: la demo sigue andando.
app.MapGet("/salud", async (Salud salud, CancellationToken ct) =>
{
    InformeSalud informe = await salud.RevisarAsync(ct);
    if (informe.Estado == "error")
    {
        return Results.Json(informe, statusCode: 503);
    }
    return Results.Ok(informe);
});

app.MapGet("/qdrant/estado", async (Sincronizador sincronizador, CancellationToken ct) =>
    await sincronizador.EstadoAsync(ct));

app.MapPost("/qdrant/reconstruir", async (Sincronizador sincronizador, CancellationToken ct) =>
    await sincronizador.ReconstruirAsync(ct));

app.Run();
