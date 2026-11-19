# RAG-BASICO

Un prototipo de RAG (búsqueda en documentos y respuesta redactada por un modelo de lenguaje), armado por fases con .NET 10, SQL Server 2025 Express, Qdrant y Ollama.

Cada fase agrega una pieza y queda marcada con su branch, así el proyecto se puede recorrer de la versión más chica a la final.

**Esta es la fase 8: validaciones, por delante de `main`.** Sobre la versión que se presenta (la fase 7, que es `main`), la API valida la pregunta y responde cada error con claridad: un 400 con el motivo si la pregunta no sirve, un 503 que dice qué servicio no responde, y si falta la base o un modelo, no arranca y dice por qué.

## Requisitos de esta fase

- SDK de .NET 10. En la carpeta del repo, `dotnet --version` tiene que decir 10.0.403 o un parche más nuevo de 10.0.4xx (lo fija `global.json`).
- SQL Server 2025 Express con Full-Text, instancia `SQLEXPRESS`, y sqlcmd (`winget install Microsoft.Sqlcmd`).
- Ollama corriendo, con los modelos `qwen3.5:4b` y `qwen3-embedding:0.6b`.
- Qdrant 1.20.2 en `..\RAG-BASICO-datos\qdrant\`, arrancado con `scripts\iniciar-qdrant.ps1`.
- Los PDF en una carpeta, y la variable de entorno de usuario `Rag__CarpetaDocumentos` apuntando a ella.
- Windows PowerShell 5.1 y git 2.31 o posterior, para los scripts.
- Para probar los pedidos del archivo `.http`: Visual Studio, o VS Code con la extensión "REST Client".

Cómo instalar cada cosa, en qué orden y cómo comprobar que anda: [docs/instalacion.md](docs/instalacion.md).

## Cómo correr esta fase

SQL Server tiene que estar andando. Si su servicio arranca a mano, como administrador: `Start-Service 'MSSQL$SQLEXPRESS'`.

Desde la raíz del repo, una sola vez:

```powershell
[Environment]::SetEnvironmentVariable('Rag__CarpetaDocumentos', 'F:\RAG-BASICO-datos\documentos', 'User')
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1
```

Después de crear la variable, cerrá y volvé a abrir Visual Studio, VS Code (todas sus ventanas) y las consolas: los que estaban abiertos no la ven. Para arrancar la API:

```powershell
cd RagBasico.Api
dotnet run
```

Antes, en otra consola, arrancá Qdrant: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\iniciar-qdrant.ps1`. La página de la demo está en `http://127.0.0.1:5088/` (con F5 en Visual Studio se abre sola; con `dotnet run`, abrila a mano), y `GET /salud` dice si anda todo. Antes de preguntar, mandá los pedidos de `RagBasico.Api\RagBasico.Api.http` en orden: `POST /indexar` pone la base al día con la carpeta (la segunda vez seguida dice `SIN CAMBIOS`), `POST /vectores` calcula los vectores y los sube a Qdrant, `GET /buscar` muestra los fragmentos que encuentra en cada modo, `POST /preguntar` responde con citas y `POST /evaluar` corre el examen.

El examen va en `examen.csv`, con el formato de `examen.ejemplo.csv`. Para congelar los PDF del examen: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-manifiesto.ps1`.

Si en la carpeta hay PDF escaneados o que no se pudieron abrir, `POST /indexar` los vuelve a informar en cada carga (`SIN TEXTO` o `ERROR`), así que la segunda vez no dice `SIN CAMBIOS`: alcanza con que `Cargados` y `Borrados` den 0.

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
| 6 | `fase-6-hibrida` | Búsqueda híbrida con RRF, las dos búsquedas en paralelo y la página de la demo | 5086 | `RagBasico_F6` | "entre los 5" de los tres modos: pendiente | [fase-6-hibrida.md](docs/fases/fase-6-hibrida.md) |
| 7 | `fase-7-qdrant` | Qdrant como índice de vectores, sincronizado con SQL y con respaldo si se apaga; `/salud` y los modelos cargados al arrancar. Acá queda `main` | 5087 | `RagBasico_F7` | "entre los 5" de `vectores-qdrant` contra `vectores`: pendiente | [fase-7-qdrant.md](docs/fases/fase-7-qdrant.md) |
| 8 | `fase-8-validaciones` | Validación de la pregunta, errores claros en formato ProblemDetails y chequeos al arrancar. Por delante de `main` | 5088 | `RagBasico_F8` | ninguno nuevo | [fase-8-validaciones.md](docs/fases/fase-8-validaciones.md) |

Para ver qué agregó una fase: `git diff fase-7-qdrant fase-8-validaciones`, o la página "compare" de GitHub entre las dos branches. En un clon nuevo, primero hay que traer las branches de fase: está en [docs/worktrees.md](docs/worktrees.md).

## Cómo recorrer las fases

Cada fase se puede abrir en su propia carpeta, sin cambiar de branch. Desde la raíz del repo:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-worktrees.ps1
```

El paso a paso, también para un clon nuevo, está en [docs/worktrees.md](docs/worktrees.md).

## Documentación

- [docs/instalacion.md](docs/instalacion.md): qué instalar, en qué orden, cómo comprobar que anda y qué hacer si algo falla.
- [docs/arquitectura.md](docs/arquitectura.md): las piezas, los recorridos, las tablas y la configuración de esta versión.
- [docs/responsabilidades.md](docs/responsabilidades.md): qué hace cada archivo, clase, función y endpoint.
- [docs/worktrees.md](docs/worktrees.md): cómo abrir y correr cada fase en su carpeta.
- [docs/fases/](docs/fases/): un documento por fase, con lo que cambió, el orden de lectura y los resultados.

## Erratas

Las erratas de esta versión están en `docs/erratas.md` de la branch de fase más nueva.
