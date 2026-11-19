# Arquitectura

Cómo se arma esta versión, `fase-4-carga`. Cada fase actualiza este documento, así que en la carpeta de cada fase describe esa fase.

## 1. Piezas

Tres: la API web `RagBasico.Api`, SQL Server, que guarda los documentos, los fragmentos y el índice Full-Text, y Ollama, que corre el modelo `qwen3.5:4b`.

```text
                                          ┌──►  SQL Server 2025 Express (localhost\SQLEXPRESS, base RagBasico_F4)
Navegador o .http ──HTTP──► RagBasico.Api ┤
                            127.0.0.1:5084 └──►  Ollama (qwen3.5:4b, 127.0.0.1:11434)
                                  ▲
                                  └── lee los PDF de la carpeta Rag:CarpetaDocumentos
```

Todavía no hay vectores ni Qdrant. Llegan en las fases 5 y 7.

## 2. Recorridos

**Preparar la base, una vez:** `scripts\crear-base.ps1` corre `sql\01_base.sql`, `sql\02_consultas.sql` y `sql\03_huella.sql` en `RagBasico_F4`.

**Congelar el corpus, una vez:** `scripts\crear-manifiesto.ps1` anota en `documentos.csv` el sha256 de cada PDF. Antes de medir, `-Verificar` controla que la carpeta no haya cambiado.

**Arrancar la API** (`dotnet run` en `RagBasico.Api`, o F5): registra el `Indexador`, el `Buscador`, el `Redactor` y el `Evaluador`, y muestra en el log la carpeta de PDF o avisa que no existe.

**Poner la base al día con la carpeta (`POST /indexar`):**

1. El `Indexador` comprueba que exista la carpeta `Rag:CarpetaDocumentos`. Si no existe, no toca la base y lo informa.
2. Calcula la huella de carga de cada PDF: el sha256 de sus bytes seguidos de los parámetros de corte, que son la versión del `Chunker`, el tamaño de fragmento, el solapamiento y la versión de PdfPig. Si un PDF no se puede leer, lo anota y lo deja sin huella.
3. Lee de `rag.Documento` el nombre y la huella de cada documento cargado.
4. `Planificador.Armar` compara las dos listas por nombre: los PDF que no están en la base son nuevos, los que tienen otra huella cambiaron, los que tienen la misma quedan igual, y los documentos que ya no están en la carpeta se borran. Si la carpeta no tiene ningún PDF, no borra nada. Un PDF sin huella no se carga ni se borra.
5. Borra los documentos que ya no están en la carpeta. Sus fragmentos se borran en cascada.
6. Por cada PDF nuevo o cambiado, PdfPig saca el texto de cada página.
   - Si tiene menos de 40 palabras por página en promedio, es un escaneo: no se carga, y si había una versión anterior se borra. El detalle dice `SIN TEXTO`.
   - Si no, `Chunker.Partir` lo corta por párrafos y `Indexador.RevisarTexto` busca señales de texto mal sacado. En una transacción se borra la versión anterior y se guardan el documento con su huella y sus fragmentos, cada uno empezando con `Documento: <nombre>`. Si el texto es dudoso, el detalle lo dice, para revisarlo en SSMS.
7. Los PDF sin cambios no se vuelven a leer. Si no hubo nada que hacer, el detalle dice `SIN CAMBIOS`.
8. El Full-Text indexa los fragmentos nuevos solo, en segundo plano, a los pocos segundos.

**Cortar por párrafos (`Chunker.Partir`):**

1. El texto se separa en párrafos por las líneas en blanco, que es como PdfPig marca un espacio grande entre dos renglones. Un párrafo de menos de 15 palabras, como un título, se pega al que le sigue.
2. Los párrafos se juntan enteros en un fragmento mientras entren en 300 palabras.
3. Si un párrafo no entra y el fragmento ya tiene al menos 150 palabras, el fragmento se cierra antes del párrafo. Si tiene menos, el principio del párrafo lo completa. Un párrafo de más de 300 palabras se corta en ventanas.
4. Cada fragmento empieza con las últimas 50 palabras del anterior. Si al final quedan menos de 50 palabras nuevas, se suman al último fragmento en lugar de abrir uno casi repetido.

**Buscar (`GET /buscar?q=...`):** `FREETEXTTABLE` corta la pregunta en palabras, busca sus plurales y conjugaciones en el índice, puntúa cada fragmento y devuelve los 5 mejores.

**Preguntar (`POST /preguntar`):**

1. El `Redactor` busca los 5 mejores fragmentos por palabras.
2. Si no encontró ninguno, responde "No encontré esa información en los documentos." sin llamar al modelo.
3. Si encontró, arma el mensaje: los fragmentos numerados `[1]` a `[5]` entre `<fragmentos>` y `</fragmentos>`, con los `<` y `>` del texto cambiados, y la pregunta al final.
4. Se lo manda a Qwen con las cinco reglas como mensaje de sistema.
5. Si Qwen respondió la frase de "no encontré", la devuelve sin fuentes. Si no, devuelve el texto con citas y las fuentes en el mismo orden.
6. Registra la consulta en `rag.Consulta`: la pregunta, el modo, los fragmentos, la respuesta y los tiempos. Si el registro falla, lo anota en el log y la respuesta sale igual.

**Evaluar (`POST /evaluar`):**

1. El `Evaluador` lee `examen.csv` (ruta `Rag:ArchivoExamen`). Si no existe o no tiene preguntas, lo avisa.
2. Avisa si el examen nombra un PDF que no está cargado, y espera hasta un minuto a que el Full-Text no tenga cambios pendientes.
3. Cada pregunta con respuesta pasa por la búsqueda por palabras: se anota en qué puesto apareció un documento correcto, de 1 a 5, o 0 si no apareció.
4. Cada pregunta sin respuesta pasa por el `Redactor`: se cuenta si respondió la frase fija.
5. Devuelve la tabla por modo ("1er lugar", "entre los 5", ms promedio), el detalle por pregunta, las fallas y los avisos.

## 3. Tablas

Base `RagBasico_F4`, esquema `rag`:

| Tabla | Columna | Tipo | Para qué |
|---|---|---|---|
| `rag.Documento` | `DocumentoId` | `INT IDENTITY` | Número del documento, clave primaria |
| `rag.Documento` | `Archivo` | `NVARCHAR(260)` | Nombre del PDF. Único |
| `rag.Documento` | `Titulo` | `NVARCHAR(400)` | Nombre sin `.pdf`; va al principio de cada fragmento |
| `rag.Documento` | `FechaCarga` | `DATETIME2` | Cuándo se cargó. Por defecto, la fecha del servidor |
| `rag.Documento` | `Huella` | `VARBINARY(32)` | La huella de carga: sha256 de los bytes del PDF más los parámetros de corte. `NULL` si se cargó antes de `sql\03_huella.sql` |
| `rag.Fragmento` | `FragmentoId` | `INT IDENTITY` | Número del fragmento, clave primaria y clave del índice Full-Text |
| `rag.Fragmento` | `DocumentoId` | `INT` | A qué documento pertenece. Si se borra el documento, se borra el fragmento (cascada) |
| `rag.Fragmento` | `Orden` | `INT` | Posición del fragmento dentro del documento, desde 1 |
| `rag.Fragmento` | `Texto` | `NVARCHAR(MAX)` | El texto, que empieza con `Documento: <nombre>` |
| `rag.Consulta` | `ConsultaId` | `INT IDENTITY` | Número de la consulta, clave primaria |
| `rag.Consulta` | `Fecha` | `DATETIME2` | Cuándo se hizo. Por defecto, la fecha del servidor |
| `rag.Consulta` | `Pregunta` | `NVARCHAR(2000)` | La pregunta, recortada a 2.000 caracteres |
| `rag.Consulta` | `Modo` | `VARCHAR(20)` | El modo de búsqueda; en esta fase, `palabras` |
| `rag.Consulta` | `FragmentosIds` | `NVARCHAR(400)` | Los números de los fragmentos que se le pasaron al modelo, separados por comas |
| `rag.Consulta` | `Respuesta` | `NVARCHAR(MAX)` | Lo que respondió |
| `rag.Consulta` | `MsBusqueda` | `INT` | Lo que tardó la búsqueda |
| `rag.Consulta` | `MsModelo` | `INT` | Lo que tardó el modelo, medido desde la API |
| `rag.Consulta` | `MsCarga` | `INT` | Lo que tardó Ollama en cargar el modelo |
| `rag.Consulta` | `MsLectura` | `INT` | Lo que tardó el modelo en leer los fragmentos y la pregunta |
| `rag.Consulta` | `MsEscritura` | `INT` | Lo que tardó en escribir la respuesta |

Índice Full-Text: catálogo `ftRag`, sin distinguir acentos, sobre `rag.Fragmento.Texto`, en español (3082), con la lista de palabras vacías del sistema y actualización automática.

## 4. Configuración

En `RagBasico.Api\appsettings.json`:

| Clave | Valor | Para qué |
|---|---|---|
| `Logging:LogLevel:Default` | `Information` | Cuánto detalle escribe el log en la consola |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Warning` | Menos detalle para los mensajes internos de ASP.NET Core |
| `AllowedHosts` | `*` | Qué nombres de servidor acepta la API: `*` es cualquiera |
| `ConnectionStrings:Rag` | `Server=localhost\SQLEXPRESS;Database=RagBasico_F4;...` | La conexión con SQL Server: autenticación de Windows y confiar en el certificado de SQL Express |
| `Rag:Proyecto` | `RAG básico` | El nombre del proyecto que devuelve `GET /` |
| `Rag:Fase` | `fase-4-carga` | La fase de esta versión. La usan `GET /` y `scripts\controlar-docs.ps1` |
| `Rag:CarpetaDocumentos` | vacío | La carpeta de los PDF. Se configura con la variable de entorno `Rag__CarpetaDocumentos` |
| `Rag:PalabrasPorFragmento` | `300` | El tamaño máximo de cada fragmento, en palabras. Entra en la huella: si se cambia, el próximo `/indexar` recarga todos los PDF |
| `Rag:Solapamiento` | `50` | Cuántas palabras repite cada fragmento del anterior. También entra en la huella |
| `Rag:TopK` | `5` | Cuántos fragmentos devuelve la búsqueda y se le pasan al modelo |
| `Rag:ArchivoExamen` | `..\examen.csv` | El examen, relativo a la carpeta del proyecto: `..` es la raíz del repo |
| `Ollama:Url` | `http://127.0.0.1:11434` | Dónde escucha Ollama |
| `Ollama:ModeloChat` | `qwen3.5:4b` | El modelo que redacta las respuestas |
| `Ollama:SegundosDeEspera` | `300` | Cuánto espera la API una respuesta de Ollama antes de cortar |
| `Ollama:MantenerCargado` | `30m` | Cuánto tiempo deja Ollama el modelo en memoria después del último pedido |

Fuera de `appsettings.json`:

| Dónde | Qué | Para qué |
|---|---|---|
| Variable de entorno de usuario | `Rag__CarpetaDocumentos` | Pisa `Rag:CarpetaDocumentos` en todas las fases y carpetas. Se crea una vez y después hay que reabrir Visual Studio y las terminales |
| `Properties\launchSettings.json` | `applicationUrl`: `http://127.0.0.1:5084` | Dirección y puerto. Cada fase usa el puerto `508N` |
| `global.json` | `version`: `10.0.403`, `rollForward`: `latestPatch` | El SDK de .NET con el que se compila |

## 5. Fuera del repo

| Qué | Dónde | Desde |
|---|---|---|
| Una carpeta por fase, creada por `scripts\crear-worktrees.ps1` | Al lado del repo: `..\RAG-BASICO-fases\` | fase-0-base |
| Ollama y el modelo `qwen3.5:4b` | Instalados en la PC; Ollama escucha en `127.0.0.1:11434` | fase-1-ollama |
| SQL Server 2025 Express, con Full-Text | Instancia `localhost\SQLEXPRESS`, base `RagBasico_F4` | fase-2-rag-minimo |
| Los PDF | La carpeta de la variable `Rag__CarpetaDocumentos`, por ejemplo `..\RAG-BASICO-datos\documentos\` | fase-2-rag-minimo |
| Una carpeta de PDF de prueba y su base, para probar el borrado sin tocar las del examen | `..\RAG-BASICO-datos\documentos-prueba\` y la base `RagBasico_F4_Prueba`, elegidas solo en una consola con `$env:Rag__CarpetaDocumentos` y `$env:ConnectionStrings__Rag` | fase-4-carga |
