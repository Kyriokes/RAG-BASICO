using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using RagBasico.Api;

var builder = WebApplication.CreateBuilder(args);

// Las conexiones de la oficina: si existe conexiones.json al lado de appsettings.json, sus líneas pisan a las de appsettings.json.
// Git lo ignora, así los nombres de los servidores y las claves no se suben.
builder.Configuration.AddJsonFile("conexiones.json", optional: true, reloadOnChange: false);

// Cuánto se espera a cada servicio. El reloj lo lleva el DetectorDeCaidas, que va delante de cada cliente:
// así, si se vence, el error dice qué servicio fue. El tiempo del HttpClient queda 10 segundos más largo, de respaldo.
TimeSpan esperaOllama = TimeSpan.FromSeconds(builder.Configuration.GetValue("Ollama:SegundosDeEspera", 300));
TimeSpan esperaQdrant = TimeSpan.FromSeconds(builder.Configuration.GetValue("Qdrant:SegundosDeEspera", 30));
// Ollama como cliente HTTP: la dirección sale de la configuración.
builder.Services.AddHttpClient<Ollama>(cliente =>
{
    cliente.BaseAddress = new Uri(builder.Configuration["Conexiones:Ollama"] ?? "http://127.0.0.1:11434");
    cliente.Timeout = esperaOllama + TimeSpan.FromSeconds(10);
}).AddHttpMessageHandler(() => new DetectorDeCaidas("Ollama", esperaOllama));
// Qdrant también como cliente HTTP: Qdrant.Configurar le pone la dirección, el tiempo de respaldo y la clave, si hay.
builder.Services.AddHttpClient<Qdrant>(cliente => Qdrant.Configurar(cliente, builder.Configuration))
    .AddHttpMessageHandler(() => new DetectorDeCaidas("Qdrant", esperaQdrant));
// Un objeto nuevo de cada uno por pedido HTTP.
builder.Services.AddScoped<Documentos>();
builder.Services.AddScoped<Cargador>();
builder.Services.AddScoped<Buscador>();
builder.Services.AddScoped<Redactor>();
builder.Services.AddScoped<Salud>();
// El trabajo en segundo plano: arranca con la API y procesa los documentos pendientes, de a uno.
builder.Services.AddHostedService<Procesador>();
// Los errores salen como ProblemDetails: un JSON con título, detalle y código HTTP, en lugar de un 500 genérico.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorDeErrores>();
// Un pedido que no se puede leer, como un JSON roto, lanza un error que atiende el ManejadorDeErrores con el motivo.
// ASP.NET Core lo hace solo en Development; así es igual en cualquier entorno.
builder.Services.Configure<RouteHandlerOptions>(opciones => opciones.ThrowOnBadRequest = true);
// El tope de una subida de PDF, en megas. Va en los dos lugares donde ASP.NET Core corta por tamaño:
// el servidor (Kestrel) y la lectura del formulario.
long topeDeSubida = builder.Configuration.GetValue("Rag:MegasPorSubida", 200) * 1024L * 1024L;
builder.WebHost.ConfigureKestrel(opciones => opciones.Limits.MaxRequestBodySize = topeDeSubida);
builder.Services.Configure<FormOptions>(opciones => opciones.MultipartBodyLengthLimit = topeDeSubida);

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
                app.Logger.LogWarning(ex, "No se pudieron cargar los modelos en Ollama al arrancar.");
            }
        }
    });
});

// El manejador de errores va primero: atiende los errores de todo lo que viene después.
app.UseExceptionHandler();
// Las respuestas de error sin cuerpo, como un 404, un 405 o un 415, salen también como ProblemDetails.
app.UseStatusCodePages();

// Los pedidos que cambian algo, como subir, borrar o reprocesar, solo se aceptan de las páginas de esta misma API.
// El navegador manda en el encabezado Origin la dirección de la página que hace el pedido: si es de otro sitio, se rechaza.
// Así una página cualquiera de internet, abierta en esta PC, no puede borrar ni reprocesar los documentos.
// Un pedido sin Origin, como los de RagBasico.Api.http, no viene de una página y pasa.
app.Use(async (HttpContext contexto, RequestDelegate siguiente) =>
{
    HttpRequest pedido = contexto.Request;
    bool soloLee = HttpMethods.IsGet(pedido.Method) || HttpMethods.IsHead(pedido.Method) || HttpMethods.IsOptions(pedido.Method);
    string origen = pedido.Headers.Origin.ToString();
    string propio = pedido.Scheme + "://" + pedido.Host.Value;
    if (!soloLee && origen != "" && !string.Equals(origen, propio, StringComparison.OrdinalIgnoreCase))
    {
        await Results.Problem(title: "El pedido viene de otra página", detail: "Este pedido solo se acepta desde las páginas de la propia API, y llegó desde " + origen + ".", statusCode: 403).ExecuteAsync(contexto);
        return;
    }
    await siguiente(contexto);
});

// Las páginas: wwwroot\index.html y lo que tenga al lado. UseDefaultFiles hace que "/" muestre index.html,
// y UseStaticFiles sirve los archivos de wwwroot. Por eso la información del proyecto está en /info.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/info", () => new
{
    proyecto = app.Configuration["Rag:Proyecto"],
    dotnet = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    // La página de documentos prende el botón de importar solo si hay una base de origen configurada.
    origenConfigurado = (app.Configuration["Conexiones:SqlOrigen"] ?? "") != ""
});

// Cargar PDF arrastrados a la página. Solo los anota como pendientes: los procesa el trabajo en segundo plano.
app.MapPost("/documentos", async (HttpRequest pedido, Documentos documentos, CancellationToken ct) =>
{
    int megas = app.Configuration.GetValue("Rag:MegasPorSubida", 200);
    if (pedido.ContentLength > topeDeSubida)
    {
        return Results.Problem(title: "La subida es demasiado grande", detail: "La subida pasa de " + megas + " MB. Subí menos archivos por vez.", statusCode: 413);
    }
    if (!pedido.HasFormContentType)
    {
        return Results.Problem(title: "El pedido no es válido", detail: "Los PDF se mandan como un formulario (multipart/form-data), en el campo archivos.", statusCode: 400);
    }
    IFormCollection formulario = await pedido.ReadFormAsync(ct);
    List<ArchivoSubido> archivos = new List<ArchivoSubido>();
    foreach (IFormFile archivo in formulario.Files)
    {
        using (MemoryStream memoria = new MemoryStream())
        {
            await archivo.CopyToAsync(memoria, ct);
            archivos.Add(new ArchivoSubido(archivo.FileName, memoria.ToArray()));
        }
    }
    if (archivos.Count == 0)
    {
        return Results.Problem(title: "El pedido no es válido", detail: "No llegó ningún archivo.", statusCode: 400);
    }
    return Results.Ok(await documentos.AnotarSubidosAsync(archivos, ct));
});

// Importar de la base de origen: anota como pendientes los documentos que todavía no están. No trae los PDF.
app.MapPost("/documentos/importar", async (Documentos documentos, CancellationToken ct) =>
{
    if ((app.Configuration["Conexiones:SqlOrigen"] ?? "") == "")
    {
        return Results.Problem(title: "No hay base de origen configurada", detail: "Completá Conexiones:SqlOrigen, Conexiones:ConsultaLista y Conexiones:ConsultaContenido, y volvé a arrancar la API.", statusCode: 400);
    }
    return Results.Ok(await documentos.AnotarOrigenAsync(ct));
});

// El avance de la carga y una página de la lista de documentos, de todos o de un solo estado.
app.MapGet("/documentos", async (string? estado, int? pagina, Documentos documentos, CancellationToken ct) =>
{
    string? filtro = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim().ToLowerInvariant();
    if (filtro != null && !Documentos.Estados.Contains(filtro))
    {
        return Results.Problem(title: "Estado desconocido", detail: "No existe el estado '" + estado + "'. Los estados son: " + string.Join(", ", Documentos.Estados) + ".", statusCode: 400);
    }
    ResumenDocumentos resumen = await documentos.ResumirAsync(filtro, pagina ?? 1, ct);
    // Si la carga está frenada porque un servidor no responde, la página lo muestra.
    // Y si se está procesando un documento, muestra por dónde va: con un PDF enorme, así se ve que avanza.
    return Results.Ok(resumen with { Aviso = Procesador.Aviso, EnCurso = Cargador.EnCurso });
});

// Abre el PDF de un documento en el visor del navegador. Los PDF no están en una carpeta: se leen de SQL,
// de la base del RAG si se arrastró o de la base de origen si se importó. Las fuentes de una respuesta enlazan acá,
// con #page=N al final de la dirección; esa parte no llega a la API: la usa el visor para ir a la página.
app.MapGet("/documentos/{id:int}/pdf", async (int id, HttpResponse respuesta, Documentos documentos, CancellationToken ct) =>
{
    PdfGuardado? pdf = await documentos.LeerPdfAsync(id, ct);
    if (pdf == null)
    {
        return Results.Problem(title: "No existe ese documento", detail: "No hay un documento con el número " + id + ".", statusCode: 404);
    }
    if (pdf.Contenido == null)
    {
        return Results.Problem(title: "No está el PDF de ese documento", detail: "El documento " + id + " (" + pdf.Archivo + ") no tiene el PDF guardado, o ya no está en la base de origen.", statusCode: 404);
    }
    if (!Documentos.EsPdf(pdf.Contenido))
    {
        return Results.Problem(title: "No está el PDF de ese documento", detail: "Lo que está guardado para el documento " + id + " (" + pdf.Archivo + ") no es un PDF.", statusCode: 404);
    }
    // "inline" le dice al navegador que lo muestre en su visor en lugar de descargarlo. SetHttpFileName escribe el nombre
    // del archivo de forma segura para un encabezado, también con tildes, eñes o comillas.
    ContentDispositionHeaderValue disposicion = new ContentDispositionHeaderValue("inline");
    disposicion.SetHttpFileName(pdf.Archivo);
    respuesta.Headers.ContentDisposition = disposicion.ToString();
    // nosniff: el navegador lo trata como PDF y como nada más, diga lo que diga el contenido.
    respuesta.Headers.XContentTypeOptions = "nosniff";
    // no-store: el PDF de un documento se puede reemplazar, así que el navegador no guarda una copia.
    respuesta.Headers.CacheControl = "no-store";
    return Results.File(pdf.Contenido, "application/pdf");
});

app.MapDelete("/documentos/{id:int}", async (int id, Documentos documentos, CancellationToken ct) =>
{
    if (!await documentos.BorrarAsync(id, ct))
    {
        return Results.Problem(title: "No existe ese documento", detail: "No hay un documento con el número " + id + ".", statusCode: 404);
    }
    return Results.NoContent();
});

// Vuelve a intentar los documentos que quedaron con error.
app.MapPost("/documentos/reintentar", async (Documentos documentos, CancellationToken ct) =>
    new { cuantos = await documentos.ReintentarAsync(ct) });

// Rehace todo desde los PDF: borra la colección de Qdrant y los fragmentos, y deja todos los documentos pendientes.
app.MapPost("/documentos/reprocesar", async (Documentos documentos, CancellationToken ct) =>
    new { cuantos = await documentos.ReprocesarTodoAsync(ct) });

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
