# RAG-BASICO

Un prototipo de RAG (búsqueda en documentos y respuesta redactada por un modelo de lenguaje), armado por fases con .NET 10, SQL Server 2025 Express, Qdrant y Ollama.

Cada fase agrega una pieza y queda marcada con su branch, así el proyecto se puede recorrer de la versión más chica a la final.

**Esta es la fase 2: el primer RAG, solo por palabras.** La API carga los PDF en SQL Server, busca los fragmentos con el Full-Text y Qwen responde usando solo esos fragmentos, con citas.

## Requisitos de esta fase

- SDK de .NET 10. En la carpeta del repo, `dotnet --version` tiene que decir 10.0.403 o un parche más nuevo de 10.0.4xx (lo fija `global.json`).
- SQL Server 2025 Express con Full-Text, instancia `SQLEXPRESS`, y sqlcmd (`winget install Microsoft.Sqlcmd`).
- Ollama corriendo, con el modelo `qwen3.5:4b`.
- Los PDF en una carpeta, y la variable de entorno de usuario `Rag__CarpetaDocumentos` apuntando a ella.
- Windows PowerShell 5.1, para los scripts.
- Para probar los pedidos del archivo `.http`: Visual Studio, o VS Code con la extensión "REST Client".

## Cómo correr esta fase

Desde la raíz del repo, una sola vez:

```powershell
[Environment]::SetEnvironmentVariable('Rag__CarpetaDocumentos', 'F:\RAG-BASICO-datos\documentos', 'User')
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1
```

Después de crear la variable, cerrá y volvé a abrir Visual Studio y las terminales. Para arrancar la API:

```powershell
cd RagBasico.Api
dotnet run
```

Mandá los pedidos de `RagBasico.Api\RagBasico.Api.http` en orden: `POST /indexar` carga los PDF, `GET /buscar` muestra los fragmentos que encuentra y `POST /preguntar` responde con citas.

Para controlar que la documentación coincida con el código, desde la raíz del repo:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\controlar-docs.ps1
```

## Fases

| Fase | Branch | Qué agrega | Puerto | Base | Número principal | Detalle |
|---|---|---|---|---|---|---|
| 0 | `fase-0-base` | El esqueleto: configuración del repo, API vacía, documentación y scripts | 5080 | no usa | ninguno | [fase-0-base.md](docs/fases/fase-0-base.md) |
| 1 | `fase-1-ollama` | El cliente HTTP de Ollama y `POST /preguntar`, sin documentos | 5081 | no usa | tokens por segundo: pendiente | [fase-1-ollama.md](docs/fases/fase-1-ollama.md) |
| 2 | `fase-2-rag-minimo` | Carga de PDF en SQL Server, búsqueda por palabras y respuesta con citas | 5082 | `RagBasico_F2` | ms por respuesta: pendiente | [fase-2-rag-minimo.md](docs/fases/fase-2-rag-minimo.md) |

Para ver qué agregó una fase: `git diff fase-1-ollama fase-2-rag-minimo`, o la página "compare" de GitHub entre las dos branches. En un clon nuevo, primero hay que traer las branches de fase: está en [docs/worktrees.md](docs/worktrees.md).

## Cómo recorrer las fases

Cada fase se puede abrir en su propia carpeta, sin cambiar de branch. Desde la raíz del repo:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-worktrees.ps1
```

El paso a paso, también para un clon nuevo, está en [docs/worktrees.md](docs/worktrees.md).

## Documentación

- [docs/arquitectura.md](docs/arquitectura.md): las piezas, los recorridos, las tablas y la configuración de esta versión.
- [docs/responsabilidades.md](docs/responsabilidades.md): qué hace cada archivo, clase, función y endpoint.
- [docs/worktrees.md](docs/worktrees.md): cómo abrir y correr cada fase en su carpeta.
- [docs/fases/](docs/fases/): un documento por fase, con lo que cambió, el orden de lectura y los resultados.

## Erratas

Las erratas de esta versión están en `docs/erratas.md` de la branch de fase más nueva.
