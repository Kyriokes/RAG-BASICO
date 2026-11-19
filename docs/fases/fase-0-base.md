# Fase 0: el esqueleto

## 1. Objetivo

Dejar el repo listo para construir el RAG fase por fase: la configuración que heredan todas las fases, una API web vacía que arranca, la documentación con su control automático y el script que abre cada fase en su propia carpeta.

Viene primero porque todo lo que se agregue después se compara contra esta versión. Si los archivos de configuración llegaran más tarde, los finales de línea y el formato ensuciarían todos los diffs entre fases.

## 2. Qué cambió

Archivos nuevos:

| Archivo | Para qué |
|---|---|
| `.editorconfig` | Formato de los archivos y estilo del código C# |
| `.gitattributes` | Finales de línea: CRLF en tu carpeta, LF guardado en git |
| `.gitignore` | Lo que no se sube: `bin`, `obj` y archivos de los editores |
| `global.json` | Fija el SDK de .NET 10.0.403 |
| `RagBasico.slnx` | La solución |
| `RagBasico.Api\RagBasico.Api.csproj` | El proyecto web |
| `RagBasico.Api\Program.cs` | El arranque de la API y el endpoint `GET /` |
| `RagBasico.Api\appsettings.json` | La configuración, con `Rag:Proyecto` y `Rag:Fase` |
| `RagBasico.Api\Properties\launchSettings.json` | Un solo perfil, `http`, en `http://127.0.0.1:5080` |
| `RagBasico.Api\RagBasico.Api.http` | El pedido de prueba de `GET /` |
| `scripts\crear-worktrees.ps1` | Crea una carpeta por fase con git worktree |
| `scripts\controlar-docs.ps1` | Controla que la documentación coincida con el código |

Documentación nueva: `docs\arquitectura.md`, `docs\responsabilidades.md`, `docs\worktrees.md`, `docs\erratas.md` y este archivo. El `README.md` reemplaza al que tenía solo el título.

Endpoints: `GET /`. Tablas: ninguna. Configuración: `Rag:Proyecto` y `Rag:Fase`.

## 3. Orden de lectura

1. `README.md`: qué es el proyecto y cómo se recorre.
2. `global.json`, `.gitattributes` y `.gitignore`: tres archivos cortos que fijan el SDK, los finales de línea y lo que no se sube.
3. `.editorconfig`: alcanza con las líneas que se ajustaron, que están en `docs\responsabilidades.md`.
4. `RagBasico.slnx` y `RagBasico.Api\RagBasico.Api.csproj`: la solución y el proyecto.
5. `RagBasico.Api\Properties\launchSettings.json` y `RagBasico.Api\appsettings.json`: cómo arranca y qué configuración lee.
6. `RagBasico.Api\Program.cs`: el arranque y `GET /`.
7. `RagBasico.Api\RagBasico.Api.http`: cómo probarlo.
8. `scripts\crear-worktrees.ps1`: de arriba hacia abajo, son cuatro pasos.
9. `scripts\controlar-docs.ps1`: primero las funciones, en el orden de la tabla de `docs\responsabilidades.md`, y después los ocho controles.
10. `docs\worktrees.md` y `docs\arquitectura.md`.

## 4. Cómo se corre y se prueba

Requisito: el SDK de .NET 10. En la carpeta del repo, `dotnet --version` tiene que decir 10.0.403 o un parche más nuevo de 10.0.4xx.

1. Compilar desde la raíz del repo: `dotnet build`. Tiene que terminar sin advertencias ni errores.
2. Arrancar la API:

   ```powershell
   cd RagBasico.Api
   dotnet run
   ```

   Tiene que decir `Now listening on: http://127.0.0.1:5080`.
3. Probar `GET /`: abrir `http://127.0.0.1:5080/` en el navegador, o mandar el pedido de `RagBasico.Api.http`. Tiene que devolver el JSON con `proyecto`, `fase` y `dotnet`.
4. Frenar la API con Ctrl+C y volver a la raíz con `cd ..`.
5. Controlar la documentación: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\controlar-docs.ps1`. Tiene que decir `OK`.
6. Después de commitear la fase, crear su carpeta: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-worktrees.ps1`. Tiene que aparecer `fase-0-base` en la carpeta de las fases.

## 5. Resultados

| Medición | Valor | Cómo se midió | Fecha |
|---|---|---|---|
| Compilación | 0 advertencias, 0 errores | `dotnet build` | 18/11/2026 |
| Arranque | Escucha en `http://127.0.0.1:5080` | `dotnet run` | 18/11/2026 |
| `GET /` | Devuelve `proyecto`, `fase` y `dotnet` | Navegador o `RagBasico.Api.http` | 18/11/2026 |
| Control de documentación | `OK` | `scripts\controlar-docs.ps1` | 18/11/2026 |

Versiones:

| Pieza | Versión | Comando |
|---|---|---|
| SDK de .NET | 10.0.403 | `dotnet --version`, en la carpeta del repo |
| Git | 2.56.0.windows.4 | `git --version` |
| Paquetes NuGet | ninguno | `dotnet list package` |
| SQL Server, Ollama y Qdrant | todavía no se usan | |
| Commit medido | el último de `fase-0-base` | `git log -1 fase-0-base` |

## 6. Decisiones

| Decisión | Alternativas descartadas | Por qué |
|---|---|---|
| Una sola historia, con una branch que marca el final de cada fase | Branches independientes; solo tags; una carpeta por fase | El diff entre dos fases muestra exactamente lo que agregó la segunda, y un arreglo se hace una sola vez |
| Una carpeta por fase con git worktree | Cambiar de branch en la misma carpeta | Se pueden tener dos fases abiertas a la vez, y la carpeta de trabajo no se toca |
| `.slnx` | `.sln` | Es el formato por defecto de .NET 10, más corto y legible |
| Un solo perfil `http` en `127.0.0.1` | El perfil `https` del template; `localhost` | Los puertos al azar del perfil `https` chocarían entre fases. `127.0.0.1` es la misma dirección que usan Ollama y Qdrant, que escuchan solo en IPv4 |
| `global.json` con `latestPatch` | Sin `global.json`; `latestFeature` | Todas las fases compilan con la misma banda del SDK, y si no hay ninguno, el error dice cuál instalar |
| C# clásico | El estilo moderno que sugiere el `.editorconfig` generado | Es más explícito: cada constructor y cada lista se ven escritos completos |
| Documentación con un control automático | Sin control automático | Sin control, la documentación se desactualiza en el primer cambio |
| `GenerateDocumentationFile` | Confiar solo en el control de documentación | El compilador marca en Visual Studio la clase o función pública que no tiene su `/// <summary>` |

## 7. Limitaciones conocidas

- La API no hace nada más que contestar `GET /`. El chat con Qwen llega en la fase 1.
- `scripts\controlar-docs.ps1` lee el C# con expresiones regulares, no con el compilador. Funciona mientras se respeten las convenciones: una clase por archivo, el modificador de acceso escrito y sin sobrecargas.
- Los scripts están pensados para Windows PowerShell 5.1.
- Las carpetas de fase viven fuera del repo: en otra PC se vuelven a crear con el script, que toma las fases del remoto.
