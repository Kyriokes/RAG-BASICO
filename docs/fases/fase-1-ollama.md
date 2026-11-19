# Fase 1: C# y HTTP contra Qwen

## 1. Objetivo

Que la API hable con Qwen por HTTP: mandar una pregunta, recibir la respuesta y medir cuánto tarda cada parte. Todavía sin documentos.

Viene antes del RAG porque junta lo nuevo de C# en un solo lugar chico: la inyección de dependencias, `HttpClient`, `async`/`await`, los records y el JSON. En la fase 2 se suman SQL, los PDF y la búsqueda, y conviene llegar con esto ya entendido.

## 2. Qué cambió

| Archivo | Qué cambió |
|---|---|
| `RagBasico.Api\Ollama.cs` | Nuevo: el cliente HTTP de Ollama, con `ChatAsync` |
| `RagBasico.Api\Modelos.cs` | Nuevo: los records `PedidoPregunta` y `RespuestaModelo` |
| `RagBasico.Api\Program.cs` | Registra el cliente de Ollama y agrega `POST /preguntar` |
| `RagBasico.Api\appsettings.json` | La sección `Ollama` y la fase `fase-1-ollama` |
| `RagBasico.Api\Properties\launchSettings.json` | Puerto 5081 |
| `RagBasico.Api\RagBasico.Api.http` | Puerto 5081 y el pedido de `POST /preguntar` |

Endpoints: `POST /preguntar`. Tablas: ninguna. Configuración: `Ollama:Url`, `Ollama:ModeloChat`, `Ollama:SegundosDeEspera` y `Ollama:MantenerCargado`.

## 3. Orden de lectura

1. `RagBasico.Api\appsettings.json`: la sección `Ollama`.
2. `RagBasico.Api\Modelos.cs`: los dos records.
3. `RagBasico.Api\Program.cs`: primero `AddHttpClient<Ollama>`, después `POST /preguntar`.
4. `RagBasico.Api\Ollama.cs`: el constructor, `ChatAsync`, `VerificarAsync`, `NanosAMilisegundos` y los records privados del final.
5. `RagBasico.Api\RagBasico.Api.http`: el pedido de prueba.

## 4. Cómo se corre y se prueba

Requisitos, además de los de la fase 0: Ollama instalado y corriendo, con el modelo descargado (`ollama pull qwen3.5:4b`).

1. Comprobar que Ollama contesta: `ollama list` tiene que mostrar `qwen3.5:4b`.
2. Arrancar la API: `cd RagBasico.Api` y `dotnet run`. Tiene que decir `Now listening on: http://127.0.0.1:5081`.
3. Mandar el pedido `POST /preguntar` de `RagBasico.Api.http`.
4. La primera vez tarda más: `msCarga` muestra lo que tardó Ollama en cargar el modelo. Repetir el pedido: `msCarga` tiene que bajar casi a 0.
5. Preguntar algo que solo dicen tus PDF: Qwen va a inventar o a decir que no sabe. Ese es el problema que resuelve la fase 2.
6. Mientras responde, `ollama ps` muestra si el modelo corre en la GPU ("100% GPU").

## 5. Resultados

| Medición | Valor | Cómo se midió | Fecha |
|---|---|---|---|
| Compilación | 0 advertencias, 0 errores | `dotnet build` | 18/11/2026 |
| `POST /preguntar` con un Ollama de prueba | Manda `think: false` y `stream: false`, y lee los cuatro tiempos | Un servidor falso que imita `/api/chat` | 18/11/2026 |
| Carga del modelo, tokens por segundo con GPU | pendiente: se mide contra Ollama real | `POST /preguntar` dos veces | pendiente |

Versiones:

| Pieza | Versión | Comando |
|---|---|---|
| SDK de .NET | 10.0.403 | `dotnet --version`, en la carpeta del repo |
| Ollama | pendiente | `ollama --version` |
| Modelo | `qwen3.5:4b` | `ollama list` |
| Paquetes NuGet | ninguno | `dotnet list package` |
| Commit medido | el último de `fase-1-ollama` | `git log -1 fase-1-ollama` |

## 6. Decisiones

| Decisión | Alternativas descartadas | Por qué |
|---|---|---|
| `HttpClient` y JSON a mano | El SDK de Ollama para .NET | Se ve exactamente qué viaja, el mismo JSON que en Postman |
| Cliente tipado con `AddHttpClient<Ollama>` | Un `new HttpClient()` en cada pedido | .NET reutiliza las conexiones y la dirección queda en un solo lugar |
| `think: false` | Dejar que el modelo piense | qwen3.5 piensa por defecto y eso multiplica el tiempo de respuesta |
| `stream: false` | Recibir la respuesta palabra por palabra | Es más simple de leer y de medir. La página de la fase 6 puede mostrarla entera |
| Tiempos de Ollama separados | Un solo tiempo total | La carga del modelo, la lectura y la escritura se resuelven distinto: no se puede mejorar lo que no se separa |
| `127.0.0.1` y no `localhost` | `localhost` | Ollama escucha solo en IPv4, y con `localhost` cada conexión nueva espera unos 2 segundos |

## 7. Limitaciones conocidas

- Qwen responde con lo que sabe, no con tus documentos: puede inventar. Lo resuelve la fase 2.
- Si Ollama no está corriendo, la API devuelve un error 500 con el detalle técnico. Los errores claros llegan en la fase 8.
- La pregunta no se valida: una vacía igual se manda a Qwen. Lo resuelve la fase 8.
- Esta versión no se probó contra Ollama real: solo contra un servidor de prueba que imita su API.
