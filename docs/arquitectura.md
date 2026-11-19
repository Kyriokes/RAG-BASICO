# Arquitectura

Cómo se arma esta versión, `fase-1-ollama`. Cada fase actualiza este documento, así que en la carpeta de cada fase describe esa fase.

## 1. Piezas

Dos: la API web `RagBasico.Api`, hecha con ASP.NET Core sobre .NET 10, y Ollama, que corre el modelo de lenguaje `qwen3.5:4b` en tu PC.

```text
Navegador o archivo .http  ──HTTP──►  RagBasico.Api  ──HTTP──►  Ollama (qwen3.5:4b)
                                     127.0.0.1:5081            127.0.0.1:11434
```

Todavía no hay base de datos ni Qdrant. Llegan en las fases 2 y 7.

## 2. Recorridos

**Arrancar la API** (`dotnet run` en `RagBasico.Api`, o F5 en Visual Studio):

1. `dotnet` lee `global.json` y elige el SDK 10.0.403, o el parche más nuevo instalado de 10.0.4xx.
2. Compila el proyecto que describe `RagBasico.Api.csproj`.
3. Lee `Properties\launchSettings.json`: perfil `http`, dirección `http://127.0.0.1:5081`, entorno `Development`.
4. `Program.cs` arma la aplicación, que lee `appsettings.json` y las variables de entorno, y registra el cliente de Ollama.
5. `app.Run()` deja la API escuchando hasta que se la frena con Ctrl+C.

**Pedir `GET /`:** la función de `app.MapGet("/", ...)` lee `Rag:Proyecto` y `Rag:Fase` y la versión de .NET, y devuelve un JSON.

**Preguntar (`POST /preguntar`):**

1. Llega `{ "pregunta": "..." }` y ASP.NET Core lo convierte en un `PedidoPregunta`.
2. ASP.NET Core le pasa a la función un `Ollama` nuevo, con su `HttpClient` apuntando a `Ollama:Url`.
3. `Ollama.ChatAsync` manda a `POST /api/chat` el modelo, `stream: false`, `think: false`, las opciones de generación y dos mensajes: las reglas (sistema) y la pregunta (usuario).
4. Ollama carga el modelo si no estaba cargado, lee la pregunta y escribe la respuesta. Devuelve el texto y los tiempos de cada parte, en nanosegundos.
5. La función arma la respuesta con el texto, los tiempos en milisegundos y los tokens por segundo.

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
| `Rag:Fase` | `fase-1-ollama` | La fase de esta versión. La usan `GET /` y `scripts\controlar-docs.ps1` |
| `Ollama:Url` | `http://127.0.0.1:11434` | Dónde escucha Ollama. Con `127.0.0.1` y no `localhost`, para no esperar 2 segundos por IPv6 |
| `Ollama:ModeloChat` | `qwen3.5:4b` | El modelo que redacta las respuestas |
| `Ollama:SegundosDeEspera` | `300` | Cuánto espera la API una respuesta de Ollama antes de cortar. La primera carga del modelo puede tardar |
| `Ollama:MantenerCargado` | `30m` | Cuánto tiempo deja Ollama el modelo en memoria después del último pedido (`keep_alive`) |

Opciones de generación, fijas en `Ollama.ChatAsync`: `temperature` 0,3 (respuestas poco creativas), `top_p` 0,8 y `top_k` 20 (cuántas palabras candidatas considera), `num_ctx` 8192 (cuánto texto puede leer de una vez) y `num_predict` 700 (largo máximo de la respuesta, en tokens).

Fuera de `appsettings.json`:

| Dónde | Qué | Para qué |
|---|---|---|
| `Properties\launchSettings.json` | `applicationUrl`: `http://127.0.0.1:5081` | Dirección y puerto de la API. Cada fase usa el puerto `508N` |
| `Properties\launchSettings.json` | `ASPNETCORE_ENVIRONMENT`: `Development` | El entorno: en Development, los errores muestran el detalle |
| `global.json` | `version`: `10.0.403`, `rollForward`: `latestPatch` | El SDK de .NET con el que se compila |

Cualquier clave de `appsettings.json` se puede pisar con una variable de entorno, cambiando `:` por `__`. Por ejemplo, `Ollama__ModeloChat` pisa `Ollama:ModeloChat`.

## 5. Fuera del repo

| Qué | Dónde | Desde |
|---|---|---|
| Una carpeta por fase, creada por `scripts\crear-worktrees.ps1` | Al lado del repo, con el mismo nombre y `-fases` al final: `..\RAG-BASICO-fases\` | fase-0-base |
| Ollama y el modelo `qwen3.5:4b` | Instalados en la PC; Ollama escucha en `127.0.0.1:11434` | fase-1-ollama |
| Los PDF, configurados con la variable de entorno `Rag__CarpetaDocumentos` | Al lado del repo, con `-datos` al final: `..\RAG-BASICO-datos\documentos\` | se usa desde la fase 2 |
