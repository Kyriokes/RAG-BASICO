# Arquitectura

Cómo se arma esta versión, `fase-0-base`. Cada fase actualiza este documento, así que en la carpeta de cada fase describe esa fase.

## 1. Piezas

Una sola: la API web `RagBasico.Api`, hecha con ASP.NET Core sobre .NET 10.

```text
Navegador o archivo .http  ──HTTP──►  RagBasico.Api  (http://127.0.0.1:5080)
```

Todavía no hay modelo de lenguaje, base de datos ni Qdrant. Llegan en las fases 1, 2 y 7.

## 2. Recorridos

**Arrancar la API** (`dotnet run` en `RagBasico.Api`, o F5 en Visual Studio):

1. `dotnet` lee `global.json` y elige el SDK 10.0.403, o el parche más nuevo instalado de 10.0.4xx.
2. Compila el proyecto que describe `RagBasico.Api.csproj`.
3. Lee `Properties\launchSettings.json`: perfil `http`, dirección `http://127.0.0.1:5080`, entorno `Development`.
4. `Program.cs` arma la aplicación, que lee `appsettings.json` y las variables de entorno.
5. `app.Run()` deja la API escuchando hasta que se la frena con Ctrl+C.

**Pedir `GET /`:**

1. El navegador o el archivo `.http` manda `GET http://127.0.0.1:5080/`.
2. ASP.NET Core busca la función que atiende esa ruta: la que se declaró con `app.MapGet("/", ...)`.
3. La función lee `Rag:Proyecto` y `Rag:Fase` de la configuración, y la versión de .NET.
4. Devuelve un objeto, y ASP.NET Core lo convierte en JSON.

## 3. Tablas

Todavía no hay. La base de datos llega en la fase 2.

## 4. Configuración

En `RagBasico.Api\appsettings.json`:

| Clave | Valor | Para qué |
|---|---|---|
| `Logging:LogLevel:Default` | `Information` | Cuánto detalle escribe el log en la consola |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Warning` | Menos detalle para los mensajes internos de ASP.NET Core |
| `AllowedHosts` | `*` | Qué nombres de servidor acepta la API: `*` es cualquiera |
| `Rag:Proyecto` | `RAG básico` | El nombre del proyecto que devuelve `GET /` |
| `Rag:Fase` | `fase-0-base` | La fase de esta versión. La usan `GET /` y `scripts\controlar-docs.ps1` |

Fuera de `appsettings.json`:

| Dónde | Qué | Para qué |
|---|---|---|
| `Properties\launchSettings.json` | `applicationUrl`: `http://127.0.0.1:5080` | Dirección y puerto de la API. Cada fase usa el puerto `508N` |
| `Properties\launchSettings.json` | `ASPNETCORE_ENVIRONMENT`: `Development` | El entorno: en Development, los errores muestran el detalle |
| `global.json` | `version`: `10.0.403`, `rollForward`: `latestPatch` | El SDK de .NET con el que se compila |

Cualquier clave de `appsettings.json` se puede pisar con una variable de entorno, cambiando `:` por `__`. Por ejemplo, `Rag__Fase` pisa `Rag:Fase`. Desde la fase 2, la carpeta de los PDF se configura así.

## 5. Fuera del repo

| Qué | Dónde | Desde |
|---|---|---|
| Una carpeta por fase, creada por `scripts\crear-worktrees.ps1` | Al lado del repo, con el mismo nombre y `-fases` al final: `..\RAG-BASICO-fases\` | fase-0-base |
| Los PDF, configurados con la variable de entorno `Rag__CarpetaDocumentos` | Al lado del repo, con `-datos` al final: `..\RAG-BASICO-datos\documentos\` | se usa desde la fase 2 |
