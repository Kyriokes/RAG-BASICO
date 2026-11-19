var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => new
{
    proyecto = app.Configuration["Rag:Proyecto"],
    fase = app.Configuration["Rag:Fase"],
    dotnet = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
});

app.Run();
