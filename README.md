# RAG-BASICO

Un prototipo de RAG (búsqueda en documentos y respuesta redactada por un modelo de lenguaje), armado por fases con .NET 10, SQL Server, Qdrant y Ollama.

Cada fase agrega una pieza y queda marcada con su branch, así el proyecto se puede recorrer de la versión más chica a la final.

**Esta es la fase 9: la versión final, corriendo toda en una PC.** Hace lo mismo que la fase 8 (pregunta, busca y responde citando) con menos código y con cada dato en un solo lugar: los PDF, los párrafos y la búsqueda por palabras en SQL Server; los vectores solo en Qdrant; los modelos en Ollama. Todo lo que conecta está en un solo bloque, `Conexiones`, de `RagBasico.Api\appsettings.json`. Los PDF se cargan arrastrándolos a la página o importándolos de otra base, y se procesan solos en segundo plano. Se arranca con doble clic en `iniciar.cmd`.

Para entender el código archivo por archivo: [docs/manual.md](docs/manual.md).

## Requisitos de esta fase

- SDK de .NET 10. En la carpeta del repo, `dotnet --version` tiene que decir 10.0.403 o un parche más nuevo de 10.0.4xx (lo fija `global.json`).
- SQL Server con Full-Text, instancia `SQLEXPRESS`, y sqlcmd (`winget install Microsoft.Sqlcmd`) para crear la base. Ya no hace falta que sea la versión 2025: la base no guarda vectores.
- Ollama corriendo, con los modelos `qwen3.5:4b` y `qwen3-embedding:0.6b`.
- Qdrant 1.20.2 en `..\RAG-BASICO-datos\qdrant\`. Lo arranca `iniciar.cmd`.
- Windows PowerShell 5.1 y git 2.31 o posterior, para los scripts.
- Para probar los pedidos del archivo `.http`: Visual Studio, o VS Code con la extensión "REST Client". No hace falta para usar las páginas.

Cómo instalar cada cosa, en qué orden y cómo comprobar que anda: [docs/instalacion.md](docs/instalacion.md).

## Cómo correr esta fase

SQL Server tiene que estar andando. Si su servicio arranca a mano, como administrador: `Start-Service 'MSSQL$SQLEXPRESS'`.

Una sola vez, desde la raíz del repo, se crea la base:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1
```

Si a una base ya creada le faltan las columnas de la página, la franja lo avisa: se frena la API, se corre el mismo script, que las agrega sin tocar lo cargado, y después, en "Documentos", "Reprocesar todo".

Después, cada vez: **doble clic en `iniciar.cmd`**. Arranca Qdrant si no está andando, arranca la API y abre el navegador en `http://127.0.0.1:5089/`. Se frena con Ctrl+C en esa ventana.

Hay dos páginas:

- **Preguntar** (`/`): la pregunta, el modo de búsqueda y la respuesta con sus fuentes. Cada fuente dice el archivo y la página, y con un clic abre el PDF en otra pestaña, en esa página.
- **Documentos** (`/documentos.html`): se arrastran los PDF, se ve cuántos están listos y cuántos faltan, y se borran o se vuelven a procesar.

Arriba de las dos hay una franja con un punto por cada pieza (SQL Server, Full-Text, los modelos, Qdrant): verde si anda, amarillo si anda a medias y rojo si no anda, con lo que hay que hacer.

Un PDF arrastrado queda `pendiente` y se procesa solo: se le saca el texto, se corta en fragmentos, se calculan sus vectores y se suben a Qdrant. Cuando queda `listo`, ya se le puede preguntar. Un PDF escaneado queda como `sin texto`, y uno roto, como `error`, con el motivo.

### Importar de otra base

Si los PDF ya están guardados en binario en otra base de SQL Server, no hace falta arrastrarlos. En `Conexiones` van la cadena de esa base (`SqlOrigen`) y dos consultas: `ConsultaLista`, que devuelve el número y el nombre de cada documento, y `ConsultaContenido`, que devuelve el binario de uno. El botón "Importar de la base de origen" los anota a todos como pendientes y se procesan igual que los arrastrados. Los PDF importados no se copian: se leen de esa base cuando se procesan.

Para probarlo sin esa base, este script arma una de prueba con los PDF de una carpeta, y dice qué poner en la configuración:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-origen-de-prueba.ps1
```

### Otros servidores

Para conectar la API a servidores de otra máquina no se toca el código ni `appsettings.json`: se crea `RagBasico.Api\conexiones.json` con las líneas de `Conexiones` que cambian. Git ignora ese archivo, así los nombres de los servidores y las claves no se suben.

### Controlar la documentación

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
| 8 | `fase-8-validaciones` | Validación de la pregunta, errores claros en formato ProblemDetails y chequeos al arrancar | 5088 | `RagBasico_F8` | ninguno nuevo | [fase-8-validaciones.md](docs/fases/fase-8-validaciones.md) |
| 9 | `fase-9-local` | Cada dato en un solo lugar: vectores solo en Qdrant, toda la conexión en el bloque `Conexiones`, carga de PDF por la página o desde otra base, en segundo plano, y arranque con doble clic. Se van el examen y la carpeta de PDF | 5089 | `RagBasico_F9` | fragmentos por segundo al cargar: entre 6 y 13 | [fase-9-local.md](docs/fases/fase-9-local.md) |

Para ver qué agregó una fase: `git diff fase-8-validaciones fase-9-local`, o la página "compare" de GitHub entre las dos branches. Las fases anteriores siguen en sus branches: `git switch fase-8-validaciones` abre la fase 8, con su propia documentación.

## Documentación

- [docs/manual.md](docs/manual.md): el manual de lectura. Recorre los archivos en el orden en que conviene leerlos, siguiendo un PDF y una pregunta de punta a punta.
- [docs/instalacion.md](docs/instalacion.md): qué instalar, en qué orden, cómo comprobar que anda y qué hacer si algo falla.
- [docs/arquitectura.md](docs/arquitectura.md): las piezas, los recorridos, las tablas y la configuración de esta versión.
- [docs/responsabilidades.md](docs/responsabilidades.md): qué hace cada archivo, clase, función y endpoint.
- [docs/fases/](docs/fases/): un documento por fase, con lo que cambió, el orden de lectura y los resultados.

## Erratas

Las erratas de esta versión están en `docs/erratas.md` de la branch de fase más nueva.
