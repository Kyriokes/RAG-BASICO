using RagBasico.Api;

var builder = WebApplication.CreateBuilder(args);

// Las conexiones de la oficina: si existe conexiones.json al lado de appsettings.json, sus líneas pisan a las de appsettings.json.
// Git lo ignora, así los nombres de los servidores y las claves no se suben.
builder.Configuration.AddJsonFile("conexiones.json", optional: true, reloadOnChange: false);

// Ollama como cliente HTTP: la dirección y el tiempo de espera salen de la configuración.
builder.Services.AddHttpClient<Ollama>(cliente =>
{
    cliente.BaseAddress = new Uri(builder.Configuration["Conexiones:Ollama"] ?? "http://127.0.0.1:11434");
    cliente.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Ollama:SegundosDeEspera", 300));
}).AddHttpMessageHandler(() => new DetectorDeCaidas("Ollama"));
// Qdrant también como cliente HTTP: Qdrant.Configurar le pone la dirección, un tiempo de espera corto y la clave, si hay.
builder.Services.AddHttpClient<Qdrant>(cliente => Qdrant.Configurar(cliente, builder.Configuration))
    .AddHttpMessageHandler(() => new DetectorDeCaidas("Qdrant"));
// Un objeto nuevo de cada uno por pedido HTTP.
builder.Services.AddScoped<Buscador>();
builder.Services.AddScoped<Redactor>();
builder.Services.AddScoped<Salud>();
// Los errores salen como ProblemDetails: un JSON con título, detalle y código HTTP, en lugar de un 500 genérico.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorDeErrores>();
// Un pedido que no se puede leer, como un JSON roto, lanza un error que atiende el ManejadorDeErrores con el motivo.
// ASP.NET Core lo hace solo en Development; así es igual en cualquier entorno.
builder.Services.Configure<RouteHandlerOptions>(opciones => opciones.ThrowOnBadRequest = true);

var app = builder.Build();

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
// Las respuestas de error sin cuerpo, como un 404, un 405 o un 415, salen también como ProblemDetails.
app.UseStatusCodePages();

// Las páginas: wwwroot\index.html y lo que tenga al lado. UseDefaultFiles hace que "/" muestre index.html,
// y UseStaticFiles sirve los archivos de wwwroot. Por eso la información del proyecto está en /info.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/info", () => new
{
    proyecto = app.Configuration["Rag:Proyecto"],
    fase = app.Configuration["Rag:Fase"],
    dotnet = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
});

// Una pregunta que no sirve o un modo mal escrito dan un error 400 que dice el motivo,
// en lugar de buscar con algo que no sirve o con otro modo sin avisar.
int largoMaximo = app.Configuration.GetValue("Rag:LargoMaximoPregunta", 500);

app.MapGet("/buscar", async (string q, string? modo, Buscador buscador, CancellationToken ct) =>
{
    string motivo = Validador.RevisarPregunta(q, largoMaximo);
    if (motivo != "")
    {
        return Results.Problem(title: "La pregunta no es válida", detail: motivo, statusCode: 400);
    }
    string? elegido = Buscador.ElegirModo(modo);
    if (elegido == null)
    {
        return Results.Problem(title: "Modo desconocido", detail: "No existe el modo '" + modo + "'. Los modos son: " + string.Join(", ", Buscador.Modos) + ".", statusCode: 400);
    }
    return Results.Ok(await buscador.BuscarAsync(q, elegido, app.Configuration.GetValue("Rag:TopK", 5), ct));
});

app.MapPost("/preguntar", async (PedidoPregunta pedido, Redactor redactor, CancellationToken ct) =>
{
    string motivo = Validador.RevisarPregunta(pedido.Pregunta, largoMaximo);
    if (motivo != "")
    {
        return Results.Problem(title: "La pregunta no es válida", detail: motivo, statusCode: 400);
    }
    string? elegido = Buscador.ElegirModo(pedido.Modo);
    if (elegido == null)
    {
        return Results.Problem(title: "Modo desconocido", detail: "No existe el modo '" + pedido.Modo + "'. Los modos son: " + string.Join(", ", Buscador.Modos) + ".", statusCode: 400);
    }
    return Results.Ok(await redactor.ResponderAsync(pedido.Pregunta, elegido, ct));
});

// 503 si falta algo sin lo que no se puede preguntar. Con avisos, 200.
app.MapGet("/salud", async (Salud salud, CancellationToken ct) =>
{
    InformeSalud informe = await salud.RevisarAsync(ct);
    if (informe.Estado == "error")
    {
        return Results.Json(informe, statusCode: 503);
    }
    return Results.Ok(informe);
});

app.Run();
