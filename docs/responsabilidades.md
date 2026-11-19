# Responsabilidades

Qué hace cada archivo, clase, función y endpoint de esta versión, y quién lo usa. Esta es la versión de `fase-0-base`.

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
│  ├─ Program.cs                   arranque y endpoints
│  ├─ Properties\
│  │  └─ launchSettings.json       dirección, puerto y forma de arrancar
│  ├─ RagBasico.Api.csproj         el proyecto
│  ├─ RagBasico.Api.http           pedidos de prueba
│  └─ appsettings.json             configuración
├─ RagBasico.slnx                  la solución que abre Visual Studio
├─ docs\                           documentación
│  ├─ arquitectura.md              piezas, recorridos, tablas y configuración
│  ├─ erratas.md                   bugs encontrados después de cerrar una fase
│  ├─ fases\
│  │  └─ fase-0-base.md            qué agregó esta fase
│  ├─ responsabilidades.md         este documento
│  └─ worktrees.md                 una carpeta por fase
├─ global.json                     versión del SDK de .NET
└─ scripts\                        herramientas del repo
   ├─ controlar-docs.ps1           controla la documentación
   └─ crear-worktrees.ps1          crea una carpeta por fase
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
- **Cambió en:** nada todavía.

## RagBasico.Api\Program.cs

- **Responsabilidad:** el punto de entrada de la API: arma la aplicación, declara los endpoints y la deja escuchando.
- **Contiene:** instrucciones de nivel superior, sin clase: el compilador arma solo el método `Main`. Se lee en tres bloques:
  - **Armado:** `WebApplication.CreateBuilder(args)` lee `appsettings.json`, las variables de entorno y los argumentos, y `builder.Build()` crea la aplicación. En esta fase no se registra ningún servicio propio.
  - **Endpoints:** cada `app.MapGet(...)` asocia una ruta con la función que la atiende.
  - **Arranque:** `app.Run()` deja la API escuchando en la dirección de `launchSettings.json` hasta que se la frena con Ctrl+C.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

### GET /

- **Recibe:** nada.
- **Devuelve:** un JSON con el nombre del proyecto, la fase y la versión de .NET, por ejemplo `{ "proyecto": "RAG básico", "fase": "fase-0-base", "dotnet": ".NET 10.0.14" }`. El proyecto y la fase salen de la configuración (`Rag:Proyecto` y `Rag:Fase`).
- **La atiende:** una función sin nombre (lambda) declarada en `Program.cs` con `app.MapGet("/", ...)`.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## RagBasico.Api\Properties\launchSettings.json

- **Responsabilidad:** dice cómo arranca la API cuando se la corre con `dotnet run` o con F5 en Visual Studio: la dirección, el puerto, el entorno y si abre el navegador.
- **Contiene:** un solo perfil, `http`: escucha en `http://127.0.0.1:5080`, con el entorno `Development`, y abre el navegador. El template traía también un perfil `https` con puertos al azar, que se sacó porque chocaría entre fases.
- **Por qué 127.0.0.1:** es la misma dirección que usan Ollama y Qdrant, que escuchan solo en IPv4, así todo el proyecto usa una sola. La consecuencia es que la API también escucha solo en IPv4: se la llama con `http://127.0.0.1:5080`, como hacen el archivo `.http` y el navegador que abre el perfil. Con `localhost`, los clientes de .NET (PowerShell, `WebClient`, `HttpClient`) prueban primero IPv6 y tardan unos 2 segundos más en conectarse.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## RagBasico.Api\RagBasico.Api.csproj

- **Responsabilidad:** describe el proyecto para el compilador: una API web (`Microsoft.NET.Sdk.Web`) para .NET 10.
- **Contiene:** `TargetFramework` `net10.0`; `Nullable` activado, para que el compilador avise cuando algo puede ser `null`; `ImplicitUsings`, que agrega a todos los archivos los `using` más comunes; y `GenerateDocumentationFile`, para que el compilador avise si una clase o función pública no tiene su `/// <summary>`. Todavía no usa paquetes NuGet.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## RagBasico.Api\RagBasico.Api.http

- **Responsabilidad:** los pedidos de prueba de la fase, listos para mandar con "Send Request" desde Visual Studio, o desde VS Code con la extensión "REST Client".
- **Contiene:** la variable `@host`, con la dirección y el puerto de la fase, y el pedido `GET /`.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## RagBasico.Api\appsettings.json

- **Responsabilidad:** la configuración de la API. Cada clave está explicada en `docs\arquitectura.md`.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## RagBasico.slnx

- **Responsabilidad:** la solución, el archivo que se abre con doble clic en Visual Studio. Agrupa los proyectos del repo; hoy, solo `RagBasico.Api`.
- **Contiene:** el formato `.slnx`, el XML que usa .NET 10 por defecto, más corto y legible que el `.sln` de siempre.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## docs\arquitectura.md

- **Responsabilidad:** cómo se arma esta versión: las piezas, los recorridos paso a paso, las tablas, la configuración y lo que está fuera del repo.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## docs\erratas.md

- **Responsabilidad:** los bugs encontrados después de cerrar una fase, y dónde se arreglaron. Solo se actualiza en la punta.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## docs\fases\fase-0-base.md

- **Responsabilidad:** qué agregó la fase 0, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## docs\responsabilidades.md

- **Responsabilidad:** este documento: qué hace cada archivo, clase, función y endpoint, y quién lo usa.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

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
