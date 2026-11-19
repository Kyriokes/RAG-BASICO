# Arquitectura

Cómo se arma esta versión, `fase-6-hibrida`. Cada fase actualiza este documento, así que en la carpeta de cada fase describe esa fase.

## 1. Piezas

Tres: la API web `RagBasico.Api`, que también sirve la página de la demo; SQL Server, que guarda los documentos, los fragmentos con su vector y el índice Full-Text; y Ollama, que corre dos modelos: `qwen3.5:4b`, que redacta las respuestas, y `qwen3-embedding:0.6b`, que convierte un texto en un vector de 1024 números.

```text
                                          ┌──►  SQL Server 2025 Express (localhost\SQLEXPRESS, base RagBasico_F6)
Navegador o .http ──HTTP──► RagBasico.Api ┤
                            127.0.0.1:5086 └──►  Ollama (qwen3.5:4b y qwen3-embedding:0.6b, 127.0.0.1:11434)
                                  ▲
                                  └── lee los PDF de la carpeta Rag:CarpetaDocumentos
```

Un vector es una lista de números que representa el significado de un texto: dos textos que dicen lo mismo con otras palabras quedan cerca. La distancia coseno mide qué tan cerca: 0 es el mismo sentido y 2 el opuesto. Todavía no hay Qdrant: llega en la fase 7.

## 2. Recorridos

**Preparar la base, una vez:** `scripts\crear-base.ps1` corre `sql\01_base.sql` a `sql\04_vectores.sql` en `RagBasico_F6`.

**Congelar el corpus, una vez:** `scripts\crear-manifiesto.ps1` anota en `documentos.csv` el sha256 de cada PDF. Antes de medir, `-Verificar` controla que la carpeta no haya cambiado.

**Arrancar la API** (`dotnet run` en `RagBasico.Api`, o F5): registra el `Indexador`, el `Vectorizador`, el `Buscador`, el `Redactor` y el `Evaluador`, y muestra en el log la carpeta de PDF o avisa que no existe. El navegador abre la página de la demo, en la raíz.

**Poner la base al día con la carpeta (`POST /indexar`):**

1. El `Indexador` comprueba que exista la carpeta `Rag:CarpetaDocumentos`. Si no existe, no toca la base y lo informa.
2. Calcula la huella de carga de cada PDF: el sha256 de sus bytes seguidos de los parámetros de corte, que son la versión del `Chunker`, el tamaño de fragmento, el solapamiento y la versión de PdfPig. Si un PDF no se puede leer, lo anota y lo deja sin huella.
3. Lee de `rag.Documento` el nombre y la huella de cada documento cargado.
4. `Planificador.Armar` compara las dos listas por nombre: los PDF que no están en la base son nuevos, los que tienen otra huella cambiaron, los que tienen la misma quedan igual, y los documentos que ya no están en la carpeta se borran. Si la carpeta no tiene ningún PDF, no borra nada. Un PDF sin huella no se carga ni se borra.
5. Borra los documentos que ya no están en la carpeta. Sus fragmentos se borran en cascada, con sus vectores.
6. Por cada PDF nuevo o cambiado, PdfPig saca el texto de cada página.
   - Si tiene menos de 40 palabras por página en promedio, es un escaneo: no se carga, y si había una versión anterior se borra. El detalle dice `SIN TEXTO`.
   - Si no, `Chunker.Partir` lo corta por párrafos y `Indexador.RevisarTexto` busca señales de texto mal sacado. En una transacción se borra la versión anterior y se guardan el documento con su huella y sus fragmentos, cada uno empezando con `Documento: <nombre>` y sin vector todavía. Si el texto es dudoso, el detalle lo dice, para revisarlo en SSMS.
7. Los PDF sin cambios no se vuelven a leer, y sus fragmentos conservan el vector. Si no hubo nada que hacer, el detalle dice `SIN CAMBIOS`.
8. El Full-Text indexa los fragmentos nuevos solo, en segundo plano, a los pocos segundos.

**Cortar por párrafos (`Chunker.Partir`):**

1. El texto se separa en párrafos por las líneas en blanco, que es como PdfPig marca un espacio grande entre dos renglones. Un párrafo de menos de 15 palabras, como un título, se pega al que le sigue.
2. Los párrafos se juntan enteros en un fragmento mientras entren en 300 palabras.
3. Si un párrafo no entra y el fragmento ya tiene al menos 150 palabras, el fragmento se cierra antes del párrafo. Si tiene menos, el principio del párrafo lo completa. Un párrafo de más de 300 palabras se corta en ventanas.
4. Cada fragmento empieza con las últimas 50 palabras del anterior. Si al final quedan menos de 50 palabras nuevas, se suman al último fragmento en lugar de abrir uno casi repetido.

**Calcular los vectores (`POST /vectores`):**

1. El `Vectorizador` pide a `rag.Fragmento` los primeros 16 fragmentos sin vector (`Rag:LoteVectores`), en orden de número de fragmento.
2. Le manda sus textos a Ollama en un solo pedido a `/api/embed`, sin instrucción, y recibe un vector de 1024 números por fragmento.
3. Los guarda en `rag.Fragmento.Embedding`, en una transacción. El vector viaja como texto JSON con punto decimal y SQL Server lo convierte con `CAST(... AS VECTOR(1024))`.
4. Si el lote falla, prueba sus fragmentos de a uno y anota los que fallan. Si fallan todos, corta: Ollama no anda o no tiene el modelo.
5. Sigue con el lote siguiente, que empieza después del último fragmento visto: no relee lo ya hecho, y un fragmento que falla queda para el próximo `/vectores`.
6. Devuelve cuántos calculó, cuántos fallaron, cuántos quedan sin vector, cuántos fragmentos por segundo y el tiempo total.

**Buscar (`GET /buscar?q=...&modo=...`):** `Buscador.ElegirModo` controla el modo; si no existe, la API devuelve un error 400. Después:

- **Por palabras:** `FREETEXTTABLE` corta la pregunta en palabras, busca sus plurales y conjugaciones en el índice, puntúa cada fragmento y devuelve los 5 mejores.
- **Por vectores:** Ollama calcula el vector de la pregunta, con la instrucción en inglés de `Ollama:InstruccionPregunta` antes (`Instruct: ...` y `Query:` pegado a la pregunta). SQL Server ordena todos los fragmentos con vector por `VECTOR_DISTANCE('cosine', ...)` y devuelve los 5 más cercanos. Es una búsqueda exacta: recorre todos los vectores.
- **Híbrida**, el modo por defecto: arranca las dos búsquedas al mismo tiempo, pidiendo 20 candidatos a cada una (`Rag:CandidatosHibrida`), y espera a las dos con `Task.WhenAll`: tarda lo que la más lenta, no la suma. `Rrf.Mezclar` las mezcla por puesto: cada fragmento suma 1 / (60 + puesto) en cada lista donde aparece, y quedan los 5 de mayor puntaje. Un fragmento que aparece bien en las dos búsquedas le gana al primero de una sola.

Se mezclan puestos y no puntajes porque el `RANK` del Full-Text y la distancia coseno miden cosas distintas, en escalas distintas: no se pueden sumar.

**Preguntar (`POST /preguntar`):**

1. Si el modo no existe, la API devuelve un error 400 con el motivo.
2. El `Redactor` busca los 5 mejores fragmentos con el modo pedido; sin modo, con la híbrida.
3. Si no encontró ninguno, responde "No encontré esa información en los documentos." sin llamar al modelo. Por vectores y con la híbrida casi nunca pasa: siempre hay un fragmento más cercano que otro, y el "no sé" lo decide Qwen.
4. Si encontró, arma el mensaje: los fragmentos numerados `[1]` a `[5]` entre `<fragmentos>` y `</fragmentos>`, con los `<` y `>` del texto cambiados, y la pregunta al final.
5. Se lo manda a Qwen con las cinco reglas como mensaje de sistema.
6. Si Qwen respondió la frase de "no encontré", la devuelve sin fuentes. Si no, devuelve el texto con citas y las fuentes en el mismo orden.
7. Registra la consulta en `rag.Consulta`: la pregunta, el modo, los fragmentos, la respuesta y los tiempos. Si el registro falla, lo anota en el log y la respuesta sale igual.

**Usar la página de la demo (`/`):**

1. La página manda la pregunta y el modo elegido a `POST /preguntar`.
2. Muestra la respuesta y, debajo, las fuentes: las que el modelo citó con `[n]` en texto normal, y las que se le pasaron pero no citó, atenuadas.
3. Si la respuesta es "no encontré", la API no manda fuentes y la página no muestra ese bloque.
4. Muestra lo que tardó la búsqueda y lo que tardó el modelo. Si la API devuelve un error, lo muestra.

**Evaluar (`POST /evaluar`):**

1. El `Evaluador` lee `examen.csv` (ruta `Rag:ArchivoExamen`). Si no existe o no tiene preguntas, lo avisa.
2. Avisa si el examen nombra un PDF que no está cargado, espera hasta un minuto a que el Full-Text no tenga cambios pendientes y avisa si hay fragmentos sin vector.
3. En cada modo, `palabras`, `vectores` e `hibrida`, cada pregunta con respuesta pasa por la búsqueda: se anota en qué puesto apareció un documento correcto, de 1 a 5, o 0 si no apareció.
4. En cada modo, cada pregunta sin respuesta pasa por el `Redactor`: se cuenta si respondió la frase fija.
5. Devuelve una fila por modo ("1er lugar", "entre los 5", ms promedio y preguntas sin respuesta bien contestadas), el detalle por pregunta, las fallas y los avisos.

## 3. Tablas

Base `RagBasico_F6`, esquema `rag`:

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
| `rag.Fragmento` | `Embedding` | `VECTOR(1024)` | El vector del texto, calculado por `qwen3-embedding:0.6b`. `NULL` hasta que corre `POST /vectores` |
| `rag.Consulta` | `ConsultaId` | `INT IDENTITY` | Número de la consulta, clave primaria |
| `rag.Consulta` | `Fecha` | `DATETIME2` | Cuándo se hizo. Por defecto, la fecha del servidor |
| `rag.Consulta` | `Pregunta` | `NVARCHAR(2000)` | La pregunta, recortada a 2.000 caracteres |
| `rag.Consulta` | `Modo` | `VARCHAR(20)` | El modo de búsqueda: `palabras`, `vectores` o `hibrida` |
| `rag.Consulta` | `FragmentosIds` | `NVARCHAR(400)` | Los números de los fragmentos que se le pasaron al modelo, separados por comas |
| `rag.Consulta` | `Respuesta` | `NVARCHAR(MAX)` | Lo que respondió |
| `rag.Consulta` | `MsBusqueda` | `INT` | Lo que tardó la búsqueda; por vectores, incluye calcular el vector de la pregunta |
| `rag.Consulta` | `MsModelo` | `INT` | Lo que tardó el modelo, medido desde la API |
| `rag.Consulta` | `MsCarga` | `INT` | Lo que tardó Ollama en cargar el modelo |
| `rag.Consulta` | `MsLectura` | `INT` | Lo que tardó el modelo en leer los fragmentos y la pregunta |
| `rag.Consulta` | `MsEscritura` | `INT` | Lo que tardó en escribir la respuesta |

Índice Full-Text: catálogo `ftRag`, sin distinguir acentos, sobre `rag.Fragmento.Texto`, en español (3082), con la lista de palabras vacías del sistema y actualización automática. Los vectores no tienen índice: la búsqueda los recorre todos.

## 4. Configuración

En `RagBasico.Api\appsettings.json`:

| Clave | Valor | Para qué |
|---|---|---|
| `Logging:LogLevel:Default` | `Information` | Cuánto detalle escribe el log en la consola |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Warning` | Menos detalle para los mensajes internos de ASP.NET Core |
| `AllowedHosts` | `*` | Qué nombres de servidor acepta la API: `*` es cualquiera |
| `ConnectionStrings:Rag` | `Server=localhost\SQLEXPRESS;Database=RagBasico_F6;...` | La conexión con SQL Server: autenticación de Windows y confiar en el certificado de SQL Express |
| `Rag:Proyecto` | `RAG básico` | El nombre del proyecto que devuelve `GET /` |
| `Rag:Fase` | `fase-6-hibrida` | La fase de esta versión. La usan `GET /` y `scripts\controlar-docs.ps1` |
| `Rag:CarpetaDocumentos` | vacío | La carpeta de los PDF. Se configura con la variable de entorno `Rag__CarpetaDocumentos` |
| `Rag:PalabrasPorFragmento` | `300` | El tamaño máximo de cada fragmento, en palabras. Entra en la huella: si se cambia, el próximo `/indexar` recarga todos los PDF |
| `Rag:Solapamiento` | `50` | Cuántas palabras repite cada fragmento del anterior. También entra en la huella |
| `Rag:TopK` | `5` | Cuántos fragmentos devuelve la búsqueda y se le pasan al modelo |
| `Rag:ArchivoExamen` | `..\examen.csv` | El examen, relativo a la carpeta del proyecto: `..` es la raíz del repo |
| `Rag:LoteVectores` | `16` | Cuántos fragmentos manda `/vectores` a Ollama en cada pedido |
| `Rag:CandidatosHibrida` | `20` | Cuántos fragmentos le pide la híbrida a cada búsqueda antes de mezclarlos. Más candidatos le dan a RRF más para elegir |
| `Ollama:Url` | `http://127.0.0.1:11434` | Dónde escucha Ollama |
| `Ollama:ModeloChat` | `qwen3.5:4b` | El modelo que redacta las respuestas |
| `Ollama:ModeloEmbeddings` | `qwen3-embedding:0.6b` | El modelo que calcula los vectores. Tiene que dar vectores de 1024 números, como la columna `Embedding` |
| `Ollama:InstruccionPregunta` | `Given a question, retrieve relevant passages that answer the question` | La instrucción que va antes de cada pregunta al calcular su vector. Cambiarla no obliga a recalcular los fragmentos |
| `Ollama:SegundosDeEspera` | `300` | Cuánto espera la API una respuesta de Ollama antes de cortar |
| `Ollama:MantenerCargado` | `30m` | Cuánto tiempo deja Ollama cada modelo en memoria después del último pedido |

Fuera de `appsettings.json`:

| Dónde | Qué | Para qué |
|---|---|---|
| Variable de entorno de usuario | `Rag__CarpetaDocumentos` | Pisa `Rag:CarpetaDocumentos` en todas las fases y carpetas. Se crea una vez y después hay que reabrir Visual Studio y las terminales |
| `Properties\launchSettings.json` | `applicationUrl`: `http://127.0.0.1:5086` | Dirección y puerto. Cada fase usa el puerto `508N` |
| `global.json` | `version`: `10.0.403`, `rollForward`: `latestPatch` | El SDK de .NET con el que se compila |

## 5. Fuera del repo

| Qué | Dónde | Desde |
|---|---|---|
| Una carpeta por fase, creada por `scripts\crear-worktrees.ps1` | Al lado del repo: `..\RAG-BASICO-fases\` | fase-0-base |
| Ollama y el modelo `qwen3.5:4b` | Instalados en la PC; Ollama escucha en `127.0.0.1:11434` | fase-1-ollama |
| SQL Server 2025 Express, con Full-Text | Instancia `localhost\SQLEXPRESS`, base `RagBasico_F6` | fase-2-rag-minimo |
| Los PDF | La carpeta de la variable `Rag__CarpetaDocumentos`, por ejemplo `..\RAG-BASICO-datos\documentos\` | fase-2-rag-minimo |
| Una carpeta de PDF de prueba y su base, para probar el borrado sin tocar las del examen | `..\RAG-BASICO-datos\documentos-prueba\` y una base aparte, elegidas solo en una consola con `$env:Rag__CarpetaDocumentos` y `$env:ConnectionStrings__Rag` | fase-4-carga |
| El modelo `qwen3-embedding:0.6b` | En Ollama: `ollama pull qwen3-embedding:0.6b` | fase-5-vectores |
