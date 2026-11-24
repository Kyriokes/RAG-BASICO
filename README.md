# RAG-BASICO

Busca en documentos PDF y responde preguntas citando de qué documento salió cada cosa. La respuesta la redacta un modelo de lenguaje que corre en un servidor propio: ningún documento sale de la red.

Tiene cuatro piezas:

| Pieza | Qué hace |
|---|---|
| La API (este repo, .NET 10) | Recibe los PDF y las preguntas, y coordina a las otras tres. Trae dos páginas para usarla |
| SQL Server | Guarda los PDF, sus textos y el índice de búsqueda por palabras |
| Qdrant | Guarda los vectores, para buscar por significado |
| Ollama | Corre los dos modelos: uno calcula los vectores, el otro redacta |

Las cuatro pueden correr en una sola PC o cada una en su servidor. Cambiar de un caso al otro es cambiar un archivo de configuración.

## Qué se necesita

- Windows, con Windows PowerShell 5.1 y git 2.31 o posterior.
- SDK de .NET 10: `dotnet --version`, en la carpeta del repo, tiene que decir 10.0.403 o un parche más nuevo de 10.0.4xx.
- SQL Server con Full-Text. Se probó con 2025 Express, instancia `SQLEXPRESS`.
- sqlcmd, para crear la base: `winget install Microsoft.Sqlcmd`.
- Ollama 0.30 o posterior, con los modelos `qwen3.5:4b` y `qwen3-embedding:0.6b`.
- Qdrant 1.20.2.

## Cómo se instala

En una sola PC: [docs/instalacion.md](docs/instalacion.md). Dice qué instalar, en qué orden, cómo comprobar cada cosa y qué hacer si algo falla.

Con cada pieza en su servidor: [docs/oficina.md](docs/oficina.md). En la PC de la API solo se instalan git y el SDK de .NET 10.

## Cómo se ejecuta

Una sola vez, desde la carpeta del repo, se crea la base:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1
```

El mismo script actualiza una base que ya existe y no tiene las columnas de la página, sin tocar lo cargado: se corre con la API frenada y después, en la página "Documentos", se aprieta "Reprocesar todo". El detalle: punto 7 de [docs/instalacion.md](docs/instalacion.md).

Después, cada vez: **doble clic en `iniciar.cmd`**. Arranca Qdrant si corre en esta PC y no está andando, arranca la API y abre el navegador en `http://127.0.0.1:5090/`. Se frena con Ctrl+C en esa ventana.

SQL Server y Ollama tienen que estar andando. Si falta alguno, la página lo dice.

## Cómo se usa

Hay dos páginas:

- **Preguntar** (`/`): se escribe la pregunta y se recibe la respuesta, con sus fuentes. Cada fuente dice el archivo y la página, y con un clic abre el PDF en otra pestaña, en esa página.
- **Documentos** (`/documentos.html`): se arrastran los PDF, se ve cuántos están listos y cuántos faltan, y se borran o se vuelven a procesar. Mientras se cargan, tres barras muestran cuánto va de la subida, cuántos documentos se procesaron y por dónde va el que se está procesando.

Arriba de las dos hay una franja con un punto por cada pieza: verde si anda, amarillo si anda a medias y rojo si no anda, con lo que hay que hacer.

Un PDF arrastrado queda `pendiente` y se procesa solo, en segundo plano. Cuando queda `listo`, ya se le puede preguntar. Un PDF escaneado queda como `sin texto`, y uno roto, como `error`, con el motivo.

Si los PDF ya están guardados en otra base de SQL Server, no hace falta arrastrarlos: el botón "Importar de la base de origen" los trae. Cómo configurarlo: punto 5 de [docs/oficina.md](docs/oficina.md).

## Qué hace cada archivo

En `RagBasico.Api\`:

| Archivo | Qué hace |
|---|---|
| `Program.cs` | El arranque: registra las clases y define los pedidos que acepta la API |
| `appsettings.json` | La configuración. El bloque `Conexiones` tiene todo lo que conecta con otro servidor |
| `Documentos.cs` | Anota los PDF que se suben o se importan, lleva el estado de cada uno, los lista y los borra. También lee de SQL el PDF de un documento, para el pedido que lo abre (`GET /documentos/{id}/pdf`) |
| `Procesador.cs` | El trabajo en segundo plano: toma los documentos pendientes, de a uno |
| `Cargador.cs` | Procesa un documento: le saca el texto, lo corta, guarda cada fragmento con su página, calcula los vectores y los sube a Qdrant. Mientras tanto anota por dónde va, para la barra de la página "Documentos" |
| `Chunker.cs` | Corta el texto en fragmentos, respetando los párrafos. Recibe una página del PDF por elemento y dice en qué página empieza y en cuál termina cada fragmento |
| `Buscador.cs` | Busca los fragmentos que sirven para una pregunta: por palabras, por significado o con las dos mezcladas |
| `Rrf.cs` | Mezcla los resultados de las dos búsquedas |
| `Redactor.cs` | Le pasa la pregunta y los fragmentos al modelo, y devuelve la respuesta con sus fuentes |
| `Validador.cs` | Revisa la pregunta antes de buscar |
| `Ollama.cs` | Habla con Ollama: pide vectores y pide la redacción |
| `Qdrant.cs` | Habla con Qdrant: sube, busca y borra vectores |
| `Salud.cs` | Revisa cada pieza y dice cuál falla y qué hacer. Alimenta la franja de las páginas |
| `DetectorDeCaidas.cs`, `ErrorDeServicio.cs`, `Errores.cs`, `ManejadorDeErrores.cs` | Hacen que un error diga qué servidor falló, en lugar de un error genérico |
| `Modelos.cs` | Los datos que van y vienen entre las clases y en los pedidos |
| `RagBasico.Api.http` | Un ejemplo de cada pedido, para probar la API sin las páginas |
| `wwwroot\` | Las dos páginas, la franja de estado y los estilos |

En el resto del repo:

| Archivo | Qué hace |
|---|---|
| `iniciar.cmd` | El doble clic: corre `scripts\iniciar.ps1` |
| `scripts\iniciar.ps1` | Arranca Qdrant, si corre en esta PC, y después la API |
| `scripts\iniciar-qdrant.ps1` | Arranca Qdrant escuchando solo en esta PC |
| `scripts\crear-base.ps1` | Crea la base, o actualiza una que ya existe: corre `sql\base.sql` |
| `scripts\crear-origen-de-prueba.ps1` | Arma una base con PDF adentro, para probar la importación |
| `sql\base.sql` | Las tablas y el índice Full-Text. `rag.Fragmento` guarda la página de cada fragmento en `Pagina` y `PaginaFin`; a una base que ya existe sin esas dos columnas, se las agrega |
| `sql\consultas-utiles.sql` | Consultas para mirar la base desde SSMS |
| `conexiones.oficina.ejemplo.json` | El modelo para conectar la API a otros servidores |

Archivo por archivo, con más detalle y en el orden en que conviene leerlos: [docs/manual.md](docs/manual.md).

## Dónde tocar para conectarlo

**A otros servidores.** No se toca el código. Se copia `conexiones.oficina.ejemplo.json` a `RagBasico.Api\conexiones.json` y se completa con los nombres reales. Git ignora ese archivo, así los nombres de los servidores y las claves no se suben. El paso a paso: [docs/oficina.md](docs/oficina.md).

**A la solución del trabajo.** Otro sistema puede mandarle la pregunta a la API y recibir la respuesta, sin pasar por la página. Sirve con cualquier lenguaje y versión, también VB.NET sobre .NET Framework 3.5. Qué pedido mandar, un ejemplo en VB.NET y qué líneas cambiar si la API corre en otra máquina: [docs/integracion.md](docs/integracion.md).

## Documentación

- [docs/instalacion.md](docs/instalacion.md): instalar todo en una PC.
- [docs/oficina.md](docs/oficina.md): conectar cada pieza a su servidor y cargar los documentos de otra base.
- [docs/integracion.md](docs/integracion.md): usarlo desde otro sistema.
- [docs/manual.md](docs/manual.md): el código, archivo por archivo.
- [docs/arquitectura.md](docs/arquitectura.md): las piezas, los recorridos, las tablas y la configuración.
