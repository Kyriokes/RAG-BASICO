# Responsabilidades

Qué hace cada archivo, clase, función y endpoint de esta versión, y quién lo usa. Esta es la versión de `fase-2-rag-minimo`.

Cómo leerlo:

- Hay una sección `##` por archivo, en el orden en que los lista git.
- Adentro de cada archivo de código hay una entrada `###` por clase, por función y por endpoint.
- **Desde** dice en qué fase apareció. **Cambió en** dice en qué fases cambió después.
- `scripts\controlar-docs.ps1` controla que este documento coincida con el código.

## Árbol

```text
RAG-BASICO\
├─ .editorconfig                   formato del código y de los archivos
├─ .gitattributes                  finales de línea
├─ .gitignore                      qué no se sube a git
├─ README.md                       portada: qué es y cómo correr esta fase
├─ RagBasico.Api\                  la API web
│  ├─ Buscador.cs                  búsqueda por palabras
│  ├─ Chunker.cs                   corta el texto en fragmentos
│  ├─ Indexador.cs                 carga los PDF en la base
│  ├─ Modelos.cs                   records de pedidos y respuestas
│  ├─ Ollama.cs                    cliente HTTP de Ollama
│  ├─ Program.cs                   arranque, servicios y endpoints
│  ├─ Properties\
│  │  └─ launchSettings.json       dirección, puerto y forma de arrancar
│  ├─ RagBasico.Api.csproj         el proyecto y sus paquetes
│  ├─ RagBasico.Api.http           pedidos de prueba
│  ├─ Redactor.cs                  arma la respuesta con Qwen
│  └─ appsettings.json             configuración
├─ RagBasico.slnx                  la solución que abre Visual Studio
├─ docs\                           documentación
│  ├─ arquitectura.md              piezas, recorridos, tablas y configuración
│  ├─ erratas.md                   bugs encontrados después de cerrar una fase
│  ├─ fases\
│  │  ├─ fase-0-base.md            el esqueleto
│  │  ├─ fase-1-ollama.md          C# y HTTP contra Qwen
│  │  └─ fase-2-rag-minimo.md      el primer RAG
│  ├─ responsabilidades.md         este documento
│  └─ worktrees.md                 una carpeta por fase
├─ global.json                     versión del SDK de .NET
├─ scripts\                        herramientas del repo
│  ├─ controlar-docs.ps1           controla la documentación
│  ├─ crear-base.ps1               crea la base de la fase
│  └─ crear-worktrees.ps1          crea una carpeta por fase
└─ sql\                            scripts de la base
   ├─ 01_base.sql                  tablas y Full-Text
   └─ consultas-utiles.sql         consultas para mirar la base
```

## .editorconfig

- **Responsabilidad:** fija el formato de los archivos y el estilo del código C#. Visual Studio lo lee solo: lo aplica al guardar y lo usa para sugerir cambios. VS Code lo lee con la extensión "EditorConfig for VS Code".
- **Contiene:** el archivo que genera `dotnet new editorconfig`, con estos ajustes:
  - **En todos los archivos:** `charset = utf-8`, `end_of_line = crlf` e `insert_final_newline = true`.
  - **En los .ps1:** `charset = utf-8-bom`, porque Windows PowerShell 5.1 lee mal las tildes de un script sin BOM.
  - **En los .cs:** `dotnet_style_require_accessibility_modifiers = always:warning`, para que el modificador de acceso esté siempre escrito, como necesita el control de documentación.
  - **C# clásico:** `csharp_style_prefer_primary_constructors = false`, `dotnet_style_prefer_collection_expression = never` y `dotnet_diagnostic.SYSLIB1045.severity = none`. Sin esto, Visual Studio sugiere constructores primarios, `[]` y `[GeneratedRegex]`, y un "arreglar todo" cambiaría el estilo del proyecto.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## .gitattributes

- **Responsabilidad:** le dice a git cómo tratar los finales de línea. En tu carpeta los archivos quedan con CRLF, como usa Windows; guardados en git, con LF. Así un editor que guarda distinto no hace aparecer un archivo entero como cambiado en el diff entre dos fases.
- **Contiene:** una sola regla: `* text=auto eol=crlf`.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## .gitignore

- **Responsabilidad:** lista lo que git no tiene que subir: las carpetas de compilación (`bin` y `obj`), los archivos de Visual Studio y de VS Code, y otros temporales.
- **Contiene:** la plantilla oficial de .NET, generada con `dotnet new gitignore`, sin agregados.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## README.md

- **Responsabilidad:** la portada del repo: qué es, qué hace falta para correr esta fase, cómo correrla, cómo recorrer las fases y la tabla de fases.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo.

## RagBasico.Api\Buscador.cs

- **Responsabilidad:** encontrar los fragmentos que mejor responden una pregunta.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Clase Buscador

- **Responsabilidad:** juntar en un solo lugar las formas de buscar. En esta fase hay una: por palabras.
- **Recibe en el constructor:** la configuración, de donde lee `ConnectionStrings:Rag`.
- **La registra:** Program.cs, con `AddScoped`: un objeto nuevo por pedido HTTP.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Buscador.PorPalabrasAsync(pregunta, topK, ct)

- **Qué hace:** busca en el Full-Text de SQL Server los fragmentos que mejor coinciden con las palabras de la pregunta. `FREETEXTTABLE` corta la pregunta en palabras, busca también sus plurales y conjugaciones, y le pone un puntaje (`RANK`) a cada fragmento. Se piden los 50 mejores y se devuelven los `topK` primeros.
- **Recibe:** la pregunta, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** una lista de `Fragmento` (número, archivo y texto), del más relevante al menos relevante. Vacía si la pregunta está vacía o si ninguna palabra coincide.
- **Efectos:** lee `rag.Fragmento` y `rag.Documento`. No escribe nada.
- **La llaman:** el endpoint `GET /buscar` y Redactor.ResponderAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

## RagBasico.Api\Chunker.cs

- **Responsabilidad:** cortar el texto de un documento en fragmentos del tamaño justo para buscar y para pasarle al modelo.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Clase Chunker

- **Responsabilidad:** las funciones que cortan y cuentan palabras. Es estática: no guarda nada, solo transforma texto.
- **Recibe en el constructor:** nada; una clase estática no tiene constructor.
- **La registra:** nadie. Se usa directo: `Chunker.Partir(...)`.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Chunker.Partir(texto, palabras, solapamiento)

- **Qué hace:** corta el texto en ventanas de `palabras` palabras. Cada ventana repite las últimas `solapamiento` palabras de la anterior, para no cortar una idea al medio. Si el solapamiento no es menor que el tamaño, lo ignora.
- **Recibe:** el texto, el tamaño de cada fragmento en palabras y cuántas palabras repetir.
- **Devuelve:** la lista de fragmentos, en orden. Vacía si el texto no tiene palabras.
- **Efectos:** ninguno.
- **La llaman:** Indexador.IndexarArchivoAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Chunker.ContarPalabras(texto)

- **Qué hace:** cuenta las palabras de un texto, separando por espacios, saltos de línea y tabulaciones.
- **Recibe:** el texto.
- **Devuelve:** la cantidad de palabras.
- **Efectos:** ninguno.
- **La llaman:** Indexador.IndexarArchivoAsync, para descartar los PDF sin texto.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

## RagBasico.Api\Indexador.cs

- **Responsabilidad:** cargar los PDF de la carpeta en la base: leerlos, cortarlos en fragmentos y guardarlos.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Clase Indexador

- **Responsabilidad:** el camino de un PDF hasta la base.
- **Recibe en el constructor:** la configuración, de donde lee `ConnectionStrings:Rag`, `Rag:CarpetaDocumentos`, `Rag:PalabrasPorFragmento` y `Rag:Solapamiento`.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Indexador.IndexarCarpetaAsync(ct)

- **Qué hace:** borra todo lo cargado y vuelve a cargar cada PDF de la carpeta, en orden alfabético. Si un PDF falla, lo anota y sigue con el siguiente. Si la carpeta no existe, no toca la base y lo informa.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `InformeIndexado`: cuántos documentos y fragmentos se cargaron, el tiempo como horas:minutos:segundos y una línea por archivo ("OK", "SIN TEXTO" o "ERROR").
- **Efectos:** borra todas las filas de `rag.Documento` y, en cascada, de `rag.Fragmento`, y después inserta las nuevas. Lee los PDF de la carpeta.
- **La llaman:** el endpoint `POST /indexar`.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Indexador.IndexarArchivoAsync(conexion, ruta, ct)

- **Qué hace:** carga un PDF: lo lee, lo corta y guarda el documento y sus fragmentos en una sola transacción, así nunca queda un documento a medias. Cada fragmento empieza con `Documento: <nombre>`, para que la búsqueda también lo encuentre por el nombre del archivo. Un PDF con menos de 20 palabras se toma como escaneado y no se carga.
- **Recibe:** la conexión abierta, la ruta del PDF y el token de cancelación.
- **Devuelve:** la cantidad de fragmentos guardados, o -1 si el PDF no tiene texto.
- **Efectos:** inserta una fila en `rag.Documento` y una por fragmento en `rag.Fragmento`.
- **La llaman:** Indexador.IndexarCarpetaAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Indexador.LeerPdf(ruta)

- **Qué hace:** saca el texto de un PDF con PdfPig, página por página, en el orden en que se lee. Separa las páginas con una línea en blanco.
- **Recibe:** la ruta del PDF.
- **Devuelve:** todo el texto del PDF. Vacío si es una imagen escaneada.
- **Efectos:** lee el archivo.
- **La llaman:** Indexador.IndexarArchivoAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Indexador.FormatearTiempo(tiempo)

- **Qué hace:** muestra un tiempo como horas:minutos:segundos, también cuando pasa de 24 horas.
- **Recibe:** el tiempo.
- **Devuelve:** el texto, por ejemplo `00:03:27`.
- **Efectos:** ninguno.
- **La llaman:** Indexador.IndexarCarpetaAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

## RagBasico.Api\Modelos.cs

- **Responsabilidad:** los records que viajan entre las piezas: lo que llega en un pedido y lo que devuelve cada servicio. Un record es una clase chica de solo datos, que se compara por su contenido.
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-2-rag-minimo.

| Record | Campo | Tipo | Para qué |
|---|---|---|---|
| `PedidoPregunta` | `Pregunta` | `string` | La pregunta que llega en el cuerpo de `POST /preguntar` |
| `RespuestaModelo` | `Texto` | `string` | Lo que escribió el modelo |
| `RespuestaModelo` | `MsCarga` | `int` | Lo que tardó Ollama en cargar el modelo en memoria. Es 0 si ya estaba cargado |
| `RespuestaModelo` | `MsLectura` | `int` | Lo que tardó el modelo en leer el pedido |
| `RespuestaModelo` | `MsEscritura` | `int` | Lo que tardó en escribir la respuesta |
| `RespuestaModelo` | `TokensPregunta` | `int` | Cuántos tokens leyó |
| `RespuestaModelo` | `TokensRespuesta` | `int` | Cuántos tokens escribió |
| `Fragmento` | `FragmentoId` | `int` | El número del fragmento en `rag.Fragmento` |
| `Fragmento` | `Archivo` | `string` | El PDF de donde salió |
| `Fragmento` | `Texto` | `string` | El texto, que empieza con `Documento: <nombre>` |
| `InformeIndexado` | `Documentos` | `int` | Cuántos PDF se cargaron |
| `InformeIndexado` | `Fragmentos` | `int` | Cuántos fragmentos se guardaron |
| `InformeIndexado` | `Tiempo` | `string` | Lo que tardó, como horas:minutos:segundos |
| `InformeIndexado` | `Detalle` | `List<string>` | Una línea por archivo: "OK", "SIN TEXTO" o "ERROR" |
| `Fuente` | `Numero` | `int` | El número con que la cita el modelo: `[1]`, `[2]`... |
| `Fuente` | `Archivo` | `string` | El PDF de esa fuente |
| `Fuente` | `FragmentoId` | `int` | El fragmento exacto |
| `RespuestaRag` | `Texto` | `string` | La respuesta, o la frase de "no encontré" |
| `RespuestaRag` | `Fuentes` | `List<Fuente>` | Las fuentes, en el orden en que se le pasaron al modelo. Vacía si no encontró |
| `RespuestaRag` | `MsBusqueda` | `int` | Lo que tardó la búsqueda |
| `RespuestaRag` | `MsModelo` | `int` | Lo que tardó el modelo, medido desde la API |
| `RespuestaRag` | `MsCarga`, `MsLectura`, `MsEscritura` | `int` | Los tiempos de Ollama, como en `RespuestaModelo` |

## RagBasico.Api\Ollama.cs

- **Responsabilidad:** hablar con Ollama por HTTP. Manda el mismo JSON que se mandaría con Postman y devuelve lo que contesta el modelo.
- **Contiene:** la clase `Ollama` y dos records privados para leer la respuesta de `/api/chat`: `RespuestaChat` (el mensaje y los tiempos) y `MensajeChat` (el texto que escribió el modelo).
- **Desde:** fase-1-ollama.
- **Cambió en:** nada todavía.

### Clase Ollama

- **Responsabilidad:** ser el único lugar del código que conoce el formato de la API de Ollama.
- **Recibe en el constructor:** el `HttpClient` que arma Program.cs, con la dirección y el tiempo de espera de la configuración, y la configuración, de donde lee `Ollama:ModeloChat` y `Ollama:MantenerCargado`.
- **La registra:** Program.cs, con `AddHttpClient<Ollama>`, que crea un cliente tipado nuevo en cada pedido.
- **Desde:** fase-1-ollama.
- **Cambió en:** nada todavía.

### Ollama.ChatAsync(sistema, usuario, ct)

- **Qué hace:** le pide una respuesta al modelo de chat. Manda `stream: false`, para recibir la respuesta entera de una vez, y `think: false`, porque qwen3.5 piensa antes de responder si no se le dice lo contrario. Si el texto trae un bloque de razonamiento, lo descarta.
- **Recibe:** el mensaje de sistema (las reglas), el mensaje del usuario y el token de cancelación del pedido.
- **Devuelve:** un `RespuestaModelo` con el texto y los tiempos que informa Ollama: carga del modelo, lectura de la pregunta y escritura de la respuesta, en milisegundos, y la cantidad de tokens leídos y escritos.
- **Efectos:** un `POST /api/chat` a Ollama.
- **La llaman:** el endpoint `POST /preguntar` de Program.cs.
- **Desde:** fase-1-ollama.
- **Cambió en:** nada todavía.

### Ollama.VerificarAsync(respuesta, ct)

- **Qué hace:** si Ollama contestó con un error, lanza una excepción con su mensaje real, por ejemplo "model not found", en lugar de un error genérico.
- **Recibe:** la respuesta HTTP de Ollama y el token de cancelación.
- **Devuelve:** nada. Si la respuesta es un error, lanza `InvalidOperationException`.
- **Efectos:** lee el cuerpo de la respuesta cuando es un error.
- **La llaman:** Ollama.ChatAsync.
- **Desde:** fase-1-ollama.
- **Cambió en:** nada todavía.

### Ollama.NanosAMilisegundos(nanosegundos)

- **Qué hace:** pasa un tiempo de Ollama, que viene en nanosegundos, a milisegundos.
- **Recibe:** el tiempo en nanosegundos.
- **Devuelve:** el tiempo en milisegundos, como número entero.
- **Efectos:** ninguno.
- **La llaman:** Ollama.ChatAsync.
- **Desde:** fase-1-ollama.
- **Cambió en:** nada todavía.

## RagBasico.Api\Program.cs

- **Responsabilidad:** el punto de entrada de la API: arma la aplicación, registra los servicios, declara los endpoints y la deja escuchando.
- **Contiene:** instrucciones de nivel superior, sin clase: el compilador arma solo el método `Main`. Se lee en cinco bloques:
  - **Armado:** `WebApplication.CreateBuilder(args)` lee `appsettings.json`, las variables de entorno y los argumentos.
  - **Servicios:** `AddHttpClient<Ollama>` registra el cliente de Ollama con `Ollama:Url` y `Ollama:SegundosDeEspera`. `AddScoped` registra el `Indexador`, el `Buscador` y el `Redactor`: un objeto nuevo de cada uno por pedido. Cuando un endpoint o un constructor pide uno, ASP.NET Core se lo crea: eso es la inyección de dependencias.
  - **Control al arrancar:** muestra en el log la carpeta de PDF que va a usar, o avisa que no existe y qué variable de entorno crear.
  - **Endpoints:** cada `app.MapGet(...)` o `app.MapPost(...)` asocia una ruta con la función que la atiende.
  - **Arranque:** `app.Run()` deja la API escuchando en la dirección de `launchSettings.json` hasta que se la frena con Ctrl+C.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo.

### GET /

- **Recibe:** nada.
- **Devuelve:** un JSON con el nombre del proyecto, la fase y la versión de .NET. El proyecto y la fase salen de la configuración (`Rag:Proyecto` y `Rag:Fase`).
- **La atiende:** una función sin nombre (lambda) declarada en `Program.cs` con `app.MapGet("/", ...)`.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

### POST /indexar

- **Recibe:** nada. Los PDF salen de la carpeta `Rag:CarpetaDocumentos`.
- **Devuelve:** el `InformeIndexado` de la carga: documentos, fragmentos, tiempo y una línea por archivo.
- **La atiende:** una función de `Program.cs` que llama a Indexador.IndexarCarpetaAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### GET /buscar

- **Recibe:** la pregunta en el parámetro `q` de la dirección, por ejemplo `/buscar?q=requisitos reintegro`.
- **Devuelve:** la lista de fragmentos encontrados por palabras, hasta `Rag:TopK`. Si falta `q`, un error 400.
- **La atiende:** una función de `Program.cs` que llama a Buscador.PorPalabrasAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### POST /preguntar

- **Recibe:** un JSON con la pregunta, `{ "pregunta": "..." }`, que ASP.NET Core convierte en un `PedidoPregunta`.
- **Devuelve:** el `RespuestaRag`: la respuesta de Qwen con citas `[n]`, las fuentes y los tiempos. Si los documentos no alcanzan, la frase "No encontré esa información en los documentos." sin fuentes.
- **La atiende:** una función de `Program.cs` que llama a Redactor.ResponderAsync.
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-2-rag-minimo (responde con los documentos, no con lo que sabe Qwen).

## RagBasico.Api\Properties\launchSettings.json

- **Responsabilidad:** dice cómo arranca la API cuando se la corre con `dotnet run` o con F5 en Visual Studio: la dirección, el puerto, el entorno y si abre el navegador.
- **Contiene:** un solo perfil, `http`: escucha en `http://127.0.0.1:508N`, donde N es el número de la fase, con el entorno `Development`, y abre el navegador. Así dos fases pueden correr a la vez. El template traía también un perfil `https` con puertos al azar, que se sacó porque chocaría entre fases.
- **Por qué 127.0.0.1:** es la misma dirección que usan Ollama y Qdrant, que escuchan solo en IPv4, así todo el proyecto usa una sola. La consecuencia es que la API también escucha solo en IPv4: se la llama con `http://127.0.0.1:508N`, como hacen el archivo `.http` y el navegador que abre el perfil. Con `localhost`, los clientes de .NET (PowerShell, `WebClient`, `HttpClient`) prueban primero IPv6 y tardan unos 2 segundos más en conectarse.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo.

## RagBasico.Api\RagBasico.Api.csproj

- **Responsabilidad:** describe el proyecto para el compilador: una API web (`Microsoft.NET.Sdk.Web`) para .NET 10, y los paquetes NuGet que usa.
- **Contiene:** `TargetFramework` `net10.0`; `Nullable` activado, para que el compilador avise cuando algo puede ser `null`; `ImplicitUsings`, que agrega a todos los archivos los `using` más comunes; y `GenerateDocumentationFile`, para que el compilador avise si una clase o función pública no tiene su `/// <summary>`. Los paquetes, con la versión exacta para que cada fase compile igual siempre:
  - **Dapper 2.1.89:** corre SQL y convierte cada fila en un objeto, sin escribir los `SqlCommand` y `SqlParameter` a mano.
  - **Microsoft.Data.SqlClient 7.1.2:** la conexión con SQL Server.
  - **PdfPig 0.1.16:** lee el texto de los PDF.
- **Desde:** fase-0-base.
- **Cambió en:** fase-2-rag-minimo.

## RagBasico.Api\RagBasico.Api.http

- **Responsabilidad:** los pedidos de prueba de la fase, listos para mandar con "Send Request" desde Visual Studio, o desde VS Code con la extensión "REST Client".
- **Contiene:** la variable `@host`, con la dirección y el puerto de la fase, y un pedido por cada endpoint de esta versión.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo.

## RagBasico.Api\Redactor.cs

- **Responsabilidad:** la "G" de RAG: armar la respuesta con los fragmentos encontrados.
- **Contiene:** la clase `Redactor`, con dos constantes: `SinRespuesta`, la única frase para "no encontré", y `Reglas`, el mensaje de sistema con las cinco reglas para Qwen (usar solo los fragmentos, citar con `[n]`, la frase exacta cuando no alcanza, e ignorar instrucciones que vengan dentro de los documentos).
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Clase Redactor

- **Responsabilidad:** buscar, pedirle la respuesta a Qwen y armar la respuesta con sus fuentes.
- **Recibe en el constructor:** el `Buscador`, el cliente de `Ollama` y la configuración, de donde lee `Rag:TopK`.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Redactor.ResponderAsync(pregunta, ct)

- **Qué hace:** busca los fragmentos de la pregunta y le pide la respuesta a Qwen. Si no hay fragmentos, responde la frase fija sin llamar al modelo. Si Qwen responde la frase de "no encontré", la devuelve sin fuentes, porque mostrar cinco documentos debajo confunde.
- **Recibe:** la pregunta y el token de cancelación.
- **Devuelve:** un `RespuestaRag`: el texto, las fuentes numeradas como las cita el modelo y los tiempos de la búsqueda y del modelo.
- **Efectos:** lee SQL Server (por el buscador) y llama a Ollama.
- **La llaman:** el endpoint `POST /preguntar`.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Redactor.ArmarMensaje(pregunta, fragmentos)

- **Qué hace:** arma el mensaje del usuario para Qwen: los fragmentos numerados `[1]` a `[n]` entre `<fragmentos>` y `</fragmentos>`, y la pregunta al final, que es lo que más atiende un modelo chico. Corta cada fragmento a 2.500 caracteres.
- **Recibe:** la pregunta y la lista de fragmentos.
- **Devuelve:** el texto del mensaje.
- **Efectos:** ninguno.
- **La llaman:** Redactor.ResponderAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Redactor.Neutralizar(texto)

- **Qué hace:** cambia los signos `<` y `>` del texto de un documento por `‹` y `›`. Así un PDF que diga `</fragmentos>` no puede cerrar el bloque de datos y dar órdenes.
- **Recibe:** el texto de un fragmento.
- **Devuelve:** el mismo texto, con los signos cambiados.
- **Efectos:** ninguno.
- **La llaman:** Redactor.ArmarMensaje.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Redactor.EsSinRespuesta(texto)

- **Qué hace:** dice si el modelo respondió la frase de "no encontré", aunque le agregue comillas o le saque el punto final. Un modelo chico no siempre copia la frase exacta.
- **Recibe:** el texto que escribió el modelo.
- **Devuelve:** `true` si es la frase de "no encontré".
- **Efectos:** ninguno.
- **La llaman:** Redactor.ResponderAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

## RagBasico.Api\appsettings.json

- **Responsabilidad:** la configuración de la API. Cada clave está explicada en `docs\arquitectura.md`.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo.

## RagBasico.slnx

- **Responsabilidad:** la solución, el archivo que se abre con doble clic en Visual Studio. Agrupa los proyectos del repo; hoy, solo `RagBasico.Api`.
- **Contiene:** el formato `.slnx`, el XML que usa .NET 10 por defecto, más corto y legible que el `.sln` de siempre.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## docs\arquitectura.md

- **Responsabilidad:** cómo se arma esta versión: las piezas, los recorridos paso a paso, las tablas, la configuración y lo que está fuera del repo.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo.

## docs\erratas.md

- **Responsabilidad:** los bugs encontrados después de cerrar una fase, y dónde se arreglaron. Solo se actualiza en la punta.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## docs\fases\fase-0-base.md

- **Responsabilidad:** qué agregó la fase 0, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## docs\fases\fase-1-ollama.md

- **Responsabilidad:** qué agregó la fase 1, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-1-ollama.
- **Cambió en:** nada todavía.

## docs\fases\fase-2-rag-minimo.md

- **Responsabilidad:** qué agregó la fase 2, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

## docs\responsabilidades.md

- **Responsabilidad:** este documento: qué hace cada archivo, clase, función y endpoint, y quién lo usa.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo.

## docs\worktrees.md

- **Responsabilidad:** cómo abrir y correr cada fase en su propia carpeta, con git worktree, también en un clon nuevo.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## global.json

- **Responsabilidad:** fija la versión del SDK de .NET con la que se compila: 10.0.403, o el parche más nuevo instalado de la misma banda 10.0.4xx (`rollForward: latestPatch`). Si no hay ninguno, `dotnet` falla con un error que dice qué SDK instalar, en lugar de compilar con otro.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## scripts\controlar-docs.ps1

- **Responsabilidad:** controla que la documentación de la fase coincida con el código. Sale con 0 si coincide, y con 1 si encontró problemas, que muestra uno por línea. Una fase no se cierra hasta que sale con 0.
- **Parámetros:** ninguno. La fase sale de `Rag:Fase` en `RagBasico.Api\appsettings.json`.
- **Ejemplo:** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\controlar-docs.ps1`
- **Requiere:** git y Windows PowerShell 5.1. Se corre desde cualquier carpeta del repo, también desde una carpeta de fase y desde una ruta con tildes.
- **Qué controla:**
  1. Cada archivo del repo contra su sección `##` en este documento y contra el árbol. También al revés: cada sección y cada nombre del árbol tienen que ser algo que existe.
  2. Los campos obligatorios de cada sección y de cada entrada.
  3. Cada clase y cada función de los `.cs` contra su entrada `###`, y que tengan su `/// <summary>`. También al revés: cada entrada de clase o de función tiene que corresponder a una que existe.
  4. Cada endpoint (`app.MapGet`, `app.MapPost`...) contra su entrada `### GET /ruta`, en los dos sentidos. Los que están en comentarios no cuentan.
  5. Que **Desde** y **Cambió en** nombren branches `fase-*` que existen, locales o del remoto.
  6. Los cinco títulos de `docs\arquitectura.md`, y que nombre cada clave de `appsettings.json` y cada columna de `sql\*.sql`. Solo busca lo que falta.
  7. Los siete títulos de `docs\fases\<fase>.md`, y que "Qué cambió" nombre cada archivo cambiado desde la fase anterior, salvo `docs\` y el README. Solo busca lo que falta.
  8. Que la fila de la fase en el README no tenga celdas vacías.
- **Convenciones que necesita:** una clase por archivo con el mismo nombre (salvo `Modelos.cs`, que junta los records); el modificador de acceso siempre escrito; sin sobrecargas; los endpoints declarados con `app.MapGet`, `app.MapPost`...; y los campos escritos como `- **Etiqueta:** valor`.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

### Funciones de controlar-docs.ps1

| Función | Qué hace |
|---|---|
| `Agregar-Problema` | Anota un problema para mostrarlo al final |
| `Leer-Texto` | Lee un archivo en UTF-8, con o sin BOM, y deja los finales de línea en LF |
| `Quitar-Codigo` | Borra los bloques de código de un Markdown, para que su contenido no se tome como títulos |
| `Obtener-Secciones` | Corta un Markdown en secciones por títulos de un nivel (`##` o `###`) |
| `Obtener-Campos` | Lee los campos `- **Etiqueta:** valor` de una sección |
| `Controlar-Campos` | Controla que una sección tenga sus campos obligatorios con texto |
| `Buscar-Fases` | Devuelve los nombres de fase (`fase-N-nombre`) que aparecen en un texto |
| `Controlar-Fases` | Controla que **Desde** y **Cambió en** nombren branches que existen |
| `Normalizar-Ruta` | Pasa una ruta a barras hacia adelante, para comparar con lo que lista git |
| `Aplanar-Json` | Devuelve las claves de un JSON escritas `Seccion:Clave` |
| `Tiene-Resumen` | Dice si una clase o una función tiene su `/// <summary>` arriba, salteando los atributos, también los de varias líneas |
| `Analizar-CSharp` | Lee un `.cs` y devuelve sus tipos y funciones, con su línea y si tienen `/// <summary>`. Reconoce métodos genéricos y los que devuelven tuplas |
| `Buscar-Endpoints` | Devuelve los endpoints de un `.cs`, sin contar los que están en comentarios |

## scripts\crear-base.ps1

- **Responsabilidad:** crea la base de la fase y corre en orden los scripts numerados de `sql\`.
- **Parámetros:** `-Base` y `-Servidor`, opcionales. Por defecto salen de `ConnectionStrings:Rag` en `RagBasico.Api\appsettings.json`.
- **Ejemplo:** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1`
- **Requiere:** sqlcmd (`winget install Microsoft.Sqlcmd`), SQL Server con la instancia de la cadena de conexión y Windows PowerShell 5.1.
- **Pasos:** lee la cadena de conexión; busca los `sql\NN_*.sql` y los ordena por nombre; corre cada uno con `sqlcmd -E -C -b -v Base=<base>`: autenticación de Windows, confiar en el certificado de SQL Express y cortar ante el primer error.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

## scripts\crear-worktrees.ps1

- **Responsabilidad:** crea o actualiza una carpeta por cada fase, con git worktree, para leer y correr cada fase sin cambiar de branch.
- **Parámetros:** `-Carpeta`, opcional: dónde van las carpetas. Por defecto, una carpeta al lado del repo, con el mismo nombre y `-fases` al final.
- **Ejemplo:** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-worktrees.ps1`
- **Requiere:** git 2.31 o posterior y Windows PowerShell 5.1. Se corre solo desde la carpeta principal del repo, porque cada fase trae su propia copia del script.
- **Pasos:**
  1. Si se lo corre desde una carpeta de fase, sale con error.
  2. Corre `git worktree prune`, que limpia el registro de carpetas borradas a mano.
  3. Por cada branch `fase-*`, local o del remoto: si su carpeta no existe, la crea separada (`git worktree add --detach`). Si existe y tiene cambios en archivos que git sigue, avisa y no la toca. Si no, la mueve a la versión actual de su branch; los archivos nuevos que no sigue git, como la configuración que deja un editor, se conservan. Si una fase está solo en el remoto, usa esa versión, por ejemplo `origin/fase-0-base`.
  4. Avisa qué carpetas ya no tienen branch, y termina mostrando `git worktree list`.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## sql\01_base.sql

- **Responsabilidad:** crea la base de la fase, el esquema `rag`, las tablas de documentos y fragmentos, y el índice Full-Text en español.
- **Qué crea:** la base (si no existe), el esquema `rag`, las tablas `rag.Documento` y `rag.Fragmento`, el catálogo `ftRag` sin distinguir acentos y el índice Full-Text sobre `rag.Fragmento.Texto`, en español (3082) y con actualización automática. Cada paso controla si ya está hecho, así se puede correr de nuevo.
- **Variables sqlcmd:** `Base`, el nombre de la base. Lo pasa `scripts\crear-base.ps1`; en SSMS se define con `:setvar Base RagBasico_F2` en modo SQLCMD.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

## sql\consultas-utiles.sql

- **Responsabilidad:** consultas para mirar la base desde SSMS: cuántos documentos y fragmentos hay, fragmentos al azar, si el Full-Text terminó de indexar, una búsqueda a mano y qué palabras busca el Full-Text a partir de una.
- **Qué crea:** nada. `scripts\crear-base.ps1` no la corre, porque su nombre no empieza con un número.
- **Variables sqlcmd:** ninguna. Antes de usarla se elige la base de la fase en SSMS.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

