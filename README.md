# RAG-BASICO

Un prototipo de RAG (búsqueda en documentos y respuesta redactada por un modelo de lenguaje), armado por fases con .NET 10, SQL Server 2025 Express, Qdrant y Ollama.

Cada fase agrega una pieza y queda marcada con su branch, así el proyecto se puede recorrer de la versión más chica a la final.

**Esta es la fase 5: buscar por significado.** Cada fragmento tiene un vector calculado con `qwen3-embedding:0.6b` y guardado en SQL Server, y la búsqueda puede ser por palabras o por vectores: encuentra el fragmento aunque la pregunta use otras palabras. El examen compara los dos modos.

## Requisitos de esta fase

- SDK de .NET 10. En la carpeta del repo, `dotnet --version` tiene que decir 10.0.403 o un parche más nuevo de 10.0.4xx (lo fija `global.json`).
- SQL Server 2025 Express con Full-Text, instancia `SQLEXPRESS`, y sqlcmd (`winget install Microsoft.Sqlcmd`).
- Ollama corriendo, con los modelos `qwen3.5:4b` y `qwen3-embedding:0.6b`.
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

Mandá los pedidos de `RagBasico.Api\RagBasico.Api.http` en orden: `POST /indexar` pone la base al día con la carpeta (la segunda vez seguida dice `SIN CAMBIOS`), `POST /vectores` calcula los vectores, `GET /buscar` muestra los fragmentos que encuentra en cada modo, `POST /preguntar` responde con citas y `POST /evaluar` corre el examen.

El examen va en `examen.csv`, con el formato de `examen.ejemplo.csv`. Para congelar los PDF del examen: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-manifiesto.ps1`.

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
| 3 | `fase-3-medicion` | El examen, el evaluador, el manifiesto de los PDF y el registro de consultas | 5083 | `RagBasico_F3` | "entre los 5" por palabras: pendiente | [fase-3-medicion.md](docs/fases/fase-3-medicion.md) |
| 4 | `fase-4-carga` | Carga incremental con huella, borrado de lo que ya no está, corte por párrafos y aviso de escaneados | 5084 | `RagBasico_F4` | segunda carga "sin cambios" y "entre los 5": pendiente | [fase-4-carga.md](docs/fases/fase-4-carga.md) |
| 5 | `fase-5-vectores` | Vectores en SQL Server, búsqueda por significado con `VECTOR_DISTANCE` y el examen en los dos modos | 5085 | `RagBasico_F5` | "entre los 5" por vectores contra palabras: pendiente | [fase-5-vectores.md](docs/fases/fase-5-vectores.md) |

Para ver qué agregó una fase: `git diff fase-4-carga fase-5-vectores`, o la página "compare" de GitHub entre las dos branches. En un clon nuevo, primero hay que traer las branches de fase: está en [docs/worktrees.md](docs/worktrees.md).

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
