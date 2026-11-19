# Arquitectura

Cómo se arma esta versión, `fase-8-validaciones`. Cada fase actualiza este documento, así que en la carpeta de cada fase describe esa fase.

## 1. Piezas

Cuatro: la API web `RagBasico.Api`, que también sirve la página de la demo; SQL Server, que guarda los documentos, los fragmentos con su vector y el índice Full-Text; Qdrant, que tiene una copia de los vectores con un índice HNSW para buscar rápido; y Ollama, que corre dos modelos: `qwen3.5:4b`, que redacta las respuestas, y `qwen3-embedding:0.6b`, que convierte un texto en un vector de 1024 números.

```text
                                          ┌──►  SQL Server 2025 Express (localhost\SQLEXPRESS, base RagBasico_F8)
                                          │
Navegador o .http ──HTTP──► RagBasico.Api ┼──►  Qdrant 1.20.2 (127.0.0.1:6333, colección fragmentos_f8)
                            127.0.0.1:5088 │
                                  ▲        └──►  Ollama (qwen3.5:4b y qwen3-embedding:0.6b, 127.0.0.1:11434)
                                  │
                                  └── lee los PDF de la carpeta Rag:CarpetaDocumentos
```

Un vector es una lista de números que representa el significado de un texto: dos textos que dicen lo mismo con otras palabras quedan cerca. La distancia coseno mide qué tan cerca: 0 es el mismo sentido y 2 el opuesto.

SQL Server es la base que manda: tiene los textos y los vectores. Qdrant tiene una copia de los vectores, y el id de cada punto es el número de fragmento de SQL: las dos bases se referencian sin una foreign key, así que la API las compara y las iguala. Si Qdrant se apaga, la demo sigue andando con los vectores de SQL.

## 2. Recorridos

**Preparar la base, una vez:** `scripts\crear-base.ps1` corre `sql\01_base.sql` a `sql\04_vectores.sql` en `RagBasico_F8`. Siempre que se recrea la base, se reconstruye la colección con `POST /qdrant/reconstruir`: si no, los números de fragmento se repiten con otros textos.

**Arrancar Qdrant:** `scripts\iniciar-qdrant.ps1` lo arranca escuchando solo en `127.0.0.1`, con sus datos en `RAG-BASICO-datos\qdrant\storage` y sin telemetría. Queda corriendo en esa consola.

**Congelar el corpus, una vez:** `scripts\crear-manifiesto.ps1` anota en `documentos.csv` el sha256 de cada PDF. Antes de medir, `-Verificar` controla que la carpeta no haya cambiado.

**Arrancar la API** (`dotnet run` en `RagBasico.Api`, o F5): registra los servicios y, antes de escuchar, controla que estén la base y los dos modelos de Ollama. Si falta algo, escribe en el log por qué y no arranca; Qdrant no se controla, porque la demo sigue sin él (con `Rag:ChequearAlArrancar` en `false`, arranca igual). Después muestra en el log la carpeta de PDF o avisa que no existe. Cuando ya está escuchando, carga los dos modelos en Ollama en segundo plano, así la primera pregunta no espera la carga. La página de la demo está en la raíz: con F5 en Visual Studio se abre sola; con `dotnet run`, se abre a mano.

**Validar y responder errores claros:**

1. `GET /buscar` y `POST /preguntar` controlan la pregunta con `Validador.RevisarPregunta`: vacía, más larga que `Rag:LargoMaximoPregunta` (500), con caracteres de control o sin letras ni números da un 400 con el motivo. Un modo que no existe, también.
2. Delante de los clientes de Ollama y de Qdrant hay un `DetectorDeCaidas`: si el servicio no responde, el error dice cuál y en qué dirección.
3. Cualquier error que nadie atrapó lo atiende el `ManejadorDeErrores`: Ollama o Qdrant que no responden son 503; que respondieron con un error, 502; que tardaron demasiado, 504; cualquier error de SQL Server, 503; un pedido que no se pudo leer, como un JSON roto, 400; y el resto, 500.
4. Los errores salen en el formato estándar ProblemDetails (`application/problem+json`): `title`, `status` y `detail`. `UseStatusCodePages` le pone ese cuerpo también a los errores que salían vacíos: 404 (una ruta que no existe), 405 (un método que no corresponde) y 415 (un cuerpo que no es JSON). La excepción es `GET /salud`, que con estado `error` devuelve su propio informe con código 503. La página de la demo muestra el `detail`.

**Revisar la salud (`GET /salud`):** cinco chequeos: SQL Server (documentos, fragmentos y vectores), el Full-Text sin cambios pendientes, los dos modelos instalados en Ollama, la dimensión de los vectores (1024) y Qdrant (versión, colección y vectores en el índice). Si falla algo sin lo que la demo no anda, el estado es `error` y el código HTTP 503. Si falta Qdrant, es un `aviso`: la demo sigue.

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
8. Pone Qdrant al día (ver más abajo): los fragmentos borrados en SQL se borran también de Qdrant.
9. El Full-Text indexa los fragmentos nuevos solo, en segundo plano, a los pocos segundos.

**Cortar por párrafos (`Chunker.Partir`):**

1. El texto se separa en párrafos por las líneas en blanco, que es como PdfPig marca un espacio grande entre dos renglones. Un párrafo de menos de 15 palabras, como un título, se pega al que le sigue.
2. Los párrafos se juntan enteros en un fragmento mientras entren en 300 palabras.
3. Si un párrafo no entra y el fragmento ya tiene al menos 150 palabras, el fragmento se cierra antes del párrafo. Si tiene menos, el principio del párrafo lo completa. Un párrafo de más de 300 palabras se corta en ventanas.
4. Cada fragmento empieza con las últimas 50 palabras del anterior. Si al final quedan menos de 50 palabras nuevas, se suman al último fragmento en lugar de abrir uno casi repetido.

**Calcular los vectores (`POST /vectores`):**

1. El `Vectorizador` pide a `rag.Fragmento` los primeros 16 fragmentos sin vector (`Rag:LoteVectores`), en orden de número de fragmento.
2. Le manda sus textos a Ollama en un solo pedido a `/api/embed`, sin instrucción, y recibe un vector de 1024 números por fragmento.
3. Los guarda en `rag.Fragmento.Embedding`, en una transacción. El vector viaja como texto JSON con punto decimal y SQL Server lo convierte con `CAST(... AS VECTOR(1024))`.
4. Si el lote falla, prueba sus fragmentos de a uno y anota los que fallan. Si Ollama no responde, corta: lo que falta queda para el próximo `/vectores`. También corta si fallan todos los fragmentos de cinco lotes seguidos, que es algo general y no un fragmento.
5. Sigue con el lote siguiente, que empieza después del último fragmento visto: no relee lo ya hecho, y un fragmento que falla queda para el próximo `/vectores`.
6. Pone Qdrant al día: sube los vectores nuevos y, si Qdrant estuvo apagado, también los que le faltaban de antes.
7. Devuelve cuántos calculó, cuántos fallaron, cuántos quedan sin vector, cuántos fragmentos por segundo y el tiempo total.

**Poner Qdrant al día (`Sincronizador.PonerAlDiaAsync`, al final de `/indexar` y de `/vectores`):**

1. Si la colección `Qdrant:Coleccion` no existe, la crea.
2. Lee los números de los fragmentos con vector en SQL y los ids de todos los puntos de Qdrant, recorriendo la colección de a 1.000.
3. `Sincronizador.Comparar` arma las dos listas: los que faltan en Qdrant y los que sobran. Compara números, no cantidades.
4. Compara el vector de una muestra de tres fragmentos que están en los dos lados. Si no coincide, la base se recreó y los números se repiten con otros textos: reconstruye la colección entera.
5. Si no, borra los puntos que sobran y sube los vectores que faltan, de a 256, leídos de SQL.
6. Si algo falla, lo anota como aviso en el detalle, con el tipo de error, y no falla: SQL ya quedó bien, y la próxima vez se pone al día.

**Mirar y reconstruir Qdrant:**

- `GET /qdrant/estado` hace la misma comparación sin cambiar nada, y muestra cuántos vectores tiene el índice HNSW (`indexed_vectors_count`).
- `POST /qdrant/reconstruir` borra la colección, comprueba que se borró (en Windows, antes de la 1.20.1, a veces no se borraba), la vuelve a crear y le sube todos los vectores de SQL.

La colección se crea con vectores de 1024 números comparados por coseno, y con dos umbrales bajos: `indexing_threshold` en 10 KB, para que Qdrant arme el índice HNSW enseguida, y `full_scan_threshold` en 10 KB, para que la búsqueda lo use. Con los valores por defecto, unos 2.500 vectores, Qdrant buscaría recorriendo todos los vectores, igual que SQL.

**Buscar (`GET /buscar?q=...&modo=...`):** `Buscador.ElegirModo` controla el modo; si no existe, la API devuelve un error 400. Devuelve los fragmentos y un aviso, vacío salvo que la híbrida haya seguido sin Qdrant.

- **Por palabras:** `FREETEXTTABLE` corta la pregunta en palabras, busca sus plurales y conjugaciones en el índice, puntúa cada fragmento y devuelve los 5 mejores.
- **Por vectores, en SQL Server:** Ollama calcula el vector de la pregunta, con la instrucción en inglés de `Ollama:InstruccionPregunta` antes (`Instruct: ...` y `Query:` pegado a la pregunta). SQL Server ordena todos los fragmentos con vector por `VECTOR_DISTANCE('cosine', ...)` y devuelve los 5 más cercanos. Es una búsqueda exacta: recorre todos los vectores.
- **Por vectores, en Qdrant (`vectores-qdrant`):** el mismo vector de la pregunta; Qdrant devuelve los números de los 5 fragmentos más cercanos usando el índice HNSW, y SQL Server sus textos, en ese orden. Si Qdrant no responde, o la colección está vacía, da error: este modo existe para medir Qdrant.
- **Híbrida**, el modo por defecto: arranca las dos búsquedas al mismo tiempo, por palabras y por vectores en Qdrant, pidiendo 20 candidatos a cada una (`Rag:CandidatosHibrida`), y espera a las dos con `Task.WhenAll`: tarda lo que la más lenta, no la suma. Si Qdrant no responde, contesta con un error o no tiene vectores, la parte por vectores usa SQL con el mismo vector de la pregunta, y la respuesta lleva un aviso que dice cuál de las tres pasó. `Rrf.Mezclar` mezcla las dos listas por puesto: cada fragmento suma 1 / (60 + puesto) en cada lista donde aparece, y quedan los 5 de mayor puntaje.

Se mezclan puestos y no puntajes porque el `RANK` del Full-Text y la distancia coseno miden cosas distintas, en escalas distintas: no se pueden sumar.

**Preguntar (`POST /preguntar`):**

1. Si el modo no existe, la API devuelve un error 400 con el motivo.
2. El `Redactor` busca los 5 mejores fragmentos con el modo pedido; sin modo, con la híbrida.
3. Si no encontró ninguno, responde "No encontré esa información en los documentos." sin llamar al modelo. Por vectores y con la híbrida casi nunca pasa: siempre hay un fragmento más cercano que otro, y el "no sé" lo decide Qwen.
4. Si encontró, arma el mensaje: los fragmentos numerados `[1]` a `[5]` entre `<fragmentos>` y `</fragmentos>`, con los `<` y `>` del texto cambiados, y la pregunta al final.
5. Se lo manda a Qwen con las cinco reglas como mensaje de sistema.
6. Si Qwen respondió la frase de "no encontré", la devuelve sin fuentes. Si no, devuelve el texto con citas y las fuentes en el mismo orden. Si la búsqueda siguió sin Qdrant, agrega el aviso.
7. Registra la consulta en `rag.Consulta`: la pregunta, el modo, los fragmentos, la respuesta y los tiempos. Si el registro falla, lo anota en el log y la respuesta sale igual.

**Usar la página de la demo (`/`):**

1. La página manda la pregunta y el modo elegido a `POST /preguntar`.
2. Si la respuesta trae un aviso, lo muestra arriba.
3. Muestra la respuesta y, debajo, las fuentes: las que el modelo citó con `[n]` en texto normal, y las que se le pasaron pero no citó, atenuadas.
4. Si la respuesta es "no encontré", la API no manda fuentes y la página no muestra ese bloque.
5. Muestra lo que tardó la búsqueda y lo que tardó el modelo. Si la API devuelve un error, lo muestra.

**Evaluar (`POST /evaluar`):**

1. El `Evaluador` lee `examen.csv` (ruta `Rag:ArchivoExamen`). Si no existe o no tiene preguntas, lo avisa.
2. Avisa si el examen nombra un PDF que no está cargado, espera hasta un minuto a que el Full-Text no tenga cambios pendientes y avisa si hay fragmentos sin vector.
3. Revisa Qdrant: corta el examen si no responde, si la colección no existe o está vacía, o si sus vectores son de otra base, porque los modos que lo usan medirían otra cosa. Si solo le faltan o le sobran algunos fragmentos, lo avisa.
4. En cada modo, `palabras`, `vectores`, `vectores-qdrant` e `hibrida`, cada pregunta con respuesta pasa por la búsqueda: se anota en qué puesto apareció un documento correcto, de 1 a 5, o 0 si no apareció.
5. En cada modo, cada pregunta sin respuesta pasa por el `Redactor`: se cuenta si respondió la frase fija.
6. Si en algún modo la híbrida tuvo que seguir sin Qdrant, lo avisa: esa fila no mide Qdrant. Si un modo falla a la mitad, lo avisa y sigue con los demás.
7. Devuelve una fila por modo ("1er lugar", "entre los 5", ms promedio y preguntas sin respuesta bien contestadas), el detalle por pregunta, las fallas y los avisos.

## 3. Tablas

Base `RagBasico_F8`, esquema `rag`:

| Tabla | Columna | Tipo | Para qué |
|---|---|---|---|
| `rag.Documento` | `DocumentoId` | `INT IDENTITY` | Número del documento, clave primaria |
| `rag.Documento` | `Archivo` | `NVARCHAR(260)` | Nombre del PDF. Único |
| `rag.Documento` | `Titulo` | `NVARCHAR(400)` | Nombre sin `.pdf`; va al principio de cada fragmento |
| `rag.Documento` | `FechaCarga` | `DATETIME2` | Cuándo se cargó. Por defecto, la fecha del servidor |
| `rag.Documento` | `Huella` | `VARBINARY(32)` | La huella de carga: sha256 de los bytes del PDF más los parámetros de corte. `NULL` si se cargó antes de `sql\03_huella.sql` |
| `rag.Fragmento` | `FragmentoId` | `INT IDENTITY` | Número del fragmento, clave primaria y clave del índice Full-Text. Es también el id de su punto en Qdrant |
| `rag.Fragmento` | `DocumentoId` | `INT` | A qué documento pertenece. Si se borra el documento, se borra el fragmento (cascada). Va a Qdrant como dato extra del punto |
| `rag.Fragmento` | `Orden` | `INT` | Posición del fragmento dentro del documento, desde 1 |
| `rag.Fragmento` | `Texto` | `NVARCHAR(MAX)` | El texto, que empieza con `Documento: <nombre>` |
| `rag.Fragmento` | `Embedding` | `VECTOR(1024)` | El vector del texto, calculado por `qwen3-embedding:0.6b`. `NULL` hasta que corre `POST /vectores` |
| `rag.Consulta` | `ConsultaId` | `INT IDENTITY` | Número de la consulta, clave primaria |
| `rag.Consulta` | `Fecha` | `DATETIME2` | Cuándo se hizo. Por defecto, la fecha del servidor |
| `rag.Consulta` | `Pregunta` | `NVARCHAR(2000)` | La pregunta, recortada a 2.000 caracteres |
| `rag.Consulta` | `Modo` | `VARCHAR(20)` | El modo de búsqueda: `palabras`, `vectores`, `vectores-qdrant` o `hibrida` |
| `rag.Consulta` | `FragmentosIds` | `NVARCHAR(400)` | Los números de los fragmentos que se le pasaron al modelo, separados por comas |
| `rag.Consulta` | `Respuesta` | `NVARCHAR(MAX)` | Lo que respondió |
| `rag.Consulta` | `MsBusqueda` | `INT` | Lo que tardó la búsqueda; por vectores, incluye calcular el vector de la pregunta |
| `rag.Consulta` | `MsModelo` | `INT` | Lo que tardó el modelo, medido desde la API |
| `rag.Consulta` | `MsCarga` | `INT` | Lo que tardó Ollama en cargar el modelo |
| `rag.Consulta` | `MsLectura` | `INT` | Lo que tardó el modelo en leer los fragmentos y la pregunta |
| `rag.Consulta` | `MsEscritura` | `INT` | Lo que tardó en escribir la respuesta |

Índice Full-Text: catálogo `ftRag`, sin distinguir acentos, sobre `rag.Fragmento.Texto`, en español (3082), con la lista de palabras vacías del sistema y actualización automática. Los vectores de SQL no tienen índice: la búsqueda los recorre todos. El índice de vectores está en Qdrant.

Colección de Qdrant `fragmentos_f8`: un punto por fragmento con vector. El id es `FragmentoId`, el vector son los mismos 1024 números de `Embedding` (normalizados), y el dato extra (payload) es `documentoId`.

## 4. Configuración

En `RagBasico.Api\appsettings.json`:

| Clave | Valor | Para qué |
|---|---|---|
| `Logging:LogLevel:Default` | `Information` | Cuánto detalle escribe el log en la consola |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Warning` | Menos detalle para los mensajes internos de ASP.NET Core |
| `Logging:LogLevel:System.Net.Http.HttpClient` | `Warning` | Sin cuatro líneas en la consola por cada pedido a Ollama y a Qdrant |
| `AllowedHosts` | `*` | Qué nombres de servidor acepta la API: `*` es cualquiera |
| `ConnectionStrings:Rag` | `Server=localhost\SQLEXPRESS;Database=RagBasico_F8;...` | La conexión con SQL Server: autenticación de Windows y confiar en el certificado de SQL Express |
| `Rag:Proyecto` | `RAG básico` | El nombre del proyecto que devuelve `GET /info` |
| `Rag:Fase` | `fase-8-validaciones` | La fase de esta versión. La usan `GET /info` y `scripts\controlar-docs.ps1` |
| `Rag:CarpetaDocumentos` | vacío | La carpeta de los PDF. Se configura con la variable de entorno `Rag__CarpetaDocumentos` |
| `Rag:PalabrasPorFragmento` | `300` | El tamaño máximo de cada fragmento, en palabras. Entra en la huella: si se cambia, el próximo `/indexar` recarga todos los PDF |
| `Rag:Solapamiento` | `50` | Cuántas palabras repite cada fragmento del anterior. También entra en la huella |
| `Rag:TopK` | `5` | Cuántos fragmentos devuelve la búsqueda y se le pasan al modelo |
| `Rag:ArchivoExamen` | `..\examen.csv` | El examen, relativo a la carpeta del proyecto: `..` es la raíz del repo |
| `Rag:LoteVectores` | `16` | Cuántos fragmentos manda `/vectores` a Ollama en cada pedido |
| `Rag:CandidatosHibrida` | `20` | Cuántos fragmentos le pide la híbrida a cada búsqueda antes de mezclarlos. Más candidatos le dan a RRF más para elegir |
| `Rag:LargoMaximoPregunta` | `500` | El largo máximo de una pregunta, en caracteres. Una más larga da un error 400 |
| `Rag:ChequearAlArrancar` | `true` | Si la API controla la base y los modelos antes de arrancar. En `false`, arranca igual: sirve para mirar la página sin tener todo andando |
| `Ollama:Url` | `http://127.0.0.1:11434` | Dónde escucha Ollama |
| `Ollama:ModeloChat` | `qwen3.5:4b` | El modelo que redacta las respuestas |
| `Ollama:ModeloEmbeddings` | `qwen3-embedding:0.6b` | El modelo que calcula los vectores. Tiene que dar vectores de 1024 números, como la columna `Embedding` y la colección de Qdrant |
| `Ollama:InstruccionPregunta` | `Given a question, retrieve relevant passages that answer the question` | La instrucción que va antes de cada pregunta al calcular su vector. Cambiarla no obliga a recalcular los fragmentos |
| `Ollama:SegundosDeEspera` | `300` | Cuánto espera la API una respuesta de Ollama antes de cortar |
| `Ollama:MantenerCargado` | `30m` | Cuánto tiempo deja Ollama cada modelo en memoria después del último pedido |
| `Qdrant:Url` | `http://127.0.0.1:6333` | Dónde escucha Qdrant. Con `127.0.0.1` y no `localhost`, porque Qdrant escucha solo en IPv4 |
| `Qdrant:Coleccion` | `fragmentos_f8` | La colección de esta fase. Cada fase desde la 7 tiene la suya, como su base |
| `Qdrant:SegundosDeEspera` | `10` | Cuánto espera la API a Qdrant. Es corto a propósito: si Qdrant no responde, la híbrida sigue con SQL |

Fuera de `appsettings.json`:

| Dónde | Qué | Para qué |
|---|---|---|
| Variable de entorno de usuario | `Rag__CarpetaDocumentos` | Pisa `Rag:CarpetaDocumentos` en todas las fases y carpetas. Se crea una vez y después hay que reabrir Visual Studio y las terminales |
| `Properties\launchSettings.json` | `applicationUrl`: `http://127.0.0.1:5088` | Dirección y puerto. Cada fase usa el puerto `508N` |
| `global.json` | `version`: `10.0.403`, `rollForward`: `latestPatch` | El SDK de .NET con el que se compila |
| `scripts\iniciar-qdrant.ps1` | `QDRANT__SERVICE__HOST`, `QDRANT__STORAGE__STORAGE_PATH`, `QDRANT__STORAGE__SNAPSHOTS_PATH` y `QDRANT__TELEMETRY_DISABLED` | La configuración de Qdrant, con variables de entorno que valen solo para su proceso: solo `127.0.0.1`, carpetas de datos fijas y sin telemetría |

## 5. Fuera del repo

Cómo instalar cada pieza y comprobar que anda: [`docs\instalacion.md`](instalacion.md).

| Qué | Dónde | Desde |
|---|---|---|
| Una carpeta por fase, creada por `scripts\crear-worktrees.ps1` | Al lado del repo: `..\RAG-BASICO-fases\` | fase-0-base |
| Ollama y el modelo `qwen3.5:4b` | Instalados en la PC; Ollama escucha en `127.0.0.1:11434` | fase-1-ollama |
| SQL Server 2025 Express, con Full-Text | Instancia `localhost\SQLEXPRESS`, base `RagBasico_F8` | fase-2-rag-minimo |
| Los PDF | La carpeta de la variable `Rag__CarpetaDocumentos`, por ejemplo `..\RAG-BASICO-datos\documentos\` | fase-2-rag-minimo |
| Una carpeta de PDF de prueba, con su base y su colección, para probar el borrado sin tocar las del examen | `..\RAG-BASICO-datos\documentos-prueba\`, una base aparte y, desde la fase 7, la colección `fragmentos_prueba`, elegidas solo en una consola con `$env:Rag__CarpetaDocumentos`, `$env:ConnectionStrings__Rag` y `$env:Qdrant__Coleccion`. Sin la colección aparte, la base de prueba le borraría a Qdrant los vectores del examen | fase-4-carga |
| El modelo `qwen3-embedding:0.6b` | En Ollama: `ollama pull qwen3-embedding:0.6b` | fase-5-vectores |
| Qdrant 1.20.2 y sus datos | `qdrant.exe` en `..\RAG-BASICO-datos\qdrant\`, datos en su subcarpeta `storage`. Una sola instancia para todas las fases, con una colección por fase | fase-7-qdrant |
