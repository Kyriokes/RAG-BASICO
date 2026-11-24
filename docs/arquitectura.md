# Arquitectura

Cómo está armado el proyecto: las piezas, los recorridos, las tablas y la configuración.

## 1. Piezas

Cuatro: la API web `RagBasico.Api`, que también sirve las dos páginas; SQL Server, que guarda los documentos, los fragmentos y el índice Full-Text; Qdrant, que guarda los vectores y los busca con un índice HNSW; y Ollama, que corre dos modelos: `qwen3.5:4b`, que redacta las respuestas, y `qwen3-embedding:0.6b`, que convierte un texto en un vector de 1024 números.

```text
                                           ┌──►  SQL Server (Conexiones:SqlTextos): los PDF arrastrados, los párrafos,
                                           │     el índice de palabras y el registro de consultas
                                           │
Navegador o .http ──HTTP──► RagBasico.Api  ┼──►  Qdrant (Conexiones:Qdrant): los vectores, y solo ahí
                            127.0.0.1:5090 │
                                           ├──►  Ollama (Conexiones:Ollama): el modelo de chat y el de embeddings
                                           │
                                           └──►  SQL Server de origen (Conexiones:SqlOrigen), opcional y solo lectura:
                                                 los PDF que ya están guardados en otra base
```

**Cada dato vive en un solo lugar, y cada lugar tiene su línea en el bloque `Conexiones`.** Para mover una pieza a otro servidor se cambia esa línea; el código no se toca.

Un vector es una lista de números que representa el significado de un texto: dos textos que dicen lo mismo con otras palabras quedan cerca. La distancia coseno mide qué tan cerca.

SQL Server es la base que manda. El id de cada punto de Qdrant es el número de fragmento de SQL, y como el PDF queda guardado en SQL (en la base del RAG si se arrastró, en la de origen si se importó), Qdrant se puede borrar y rehacer entero con "Reprocesar todo". Si Qdrant no responde, la búsqueda por palabras sigue andando y las otras dos dan un error que dice qué pasa.

## 2. Recorridos

**Preparar la base, una vez:** `scripts\crear-base.ps1` corre `sql\base.sql` en la base de `Conexiones:SqlTextos`. Si se recrea la base, los números de fragmento empiezan de nuevo y Qdrant queda con puntos de la base anterior: `GET /salud` lo avisa, y se arregla con "Reprocesar todo".

**Actualizar una base que ya existe:** `sql\base.sql` se puede correr sobre una base con documentos cargados. Crea lo que falte y, si `rag.Fragmento` no tiene las columnas `Pagina` y `PaginaFin`, las agrega (un `IF COL_LENGTH ... ALTER TABLE ... ADD` por columna), sin tocar lo cargado. Los pasos:

1. Frenar la API.
2. Correr `sql\base.sql`. En una PC, con `scripts\crear-base.ps1`, que lee el servidor de `appsettings.json` y no de `conexiones.json`. En un servidor, desde SSMS, con un usuario que pueda cambiar tablas: el usuario de la API solo lee y escribe.
3. Arrancar la API.
4. Apretar "Reprocesar todo" en la página Documentos: un fragmento guardado sin página tiene `Pagina` en `NULL` hasta que se reprocesa.

Reprocesar borra todos los fragmentos y la colección de Qdrant y rehace todo: mientras dura, no se encuentran los documentos que faltan. Como medida, 19.191 fragmentos tardan 49 minutos en una PC con una GTX 1070.

**Arrancar (`iniciar.cmd`):** corre `scripts\iniciar.ps1`. Lee `Conexiones:Qdrant`; si es de esta PC y no responde, arranca Qdrant en otra ventana con `scripts\iniciar-qdrant.ps1` (escuchando solo en `127.0.0.1`, con sus datos en `RAG-BASICO-datos\qdrant\storage` y sin telemetría). Si Qdrant está en otra máquina, no arranca nada. Después corre la API con `dotnet run`, que abre el navegador.

**Arrancar la API:** carga `appsettings.json` y, si existe, `conexiones.json`, cuyas líneas pisan a las del primero. Registra los servicios y arranca siempre, aunque falte un servidor: lo que falte lo muestra la página. Cuando ya está escuchando, carga los dos modelos en Ollama en segundo plano, así la primera pregunta no espera la carga, y arranca el `Procesador`.

**Revisar la salud (`GET /salud`):** un chequeo por pieza, y cada uno dice la dirección que probó y qué hacer.

1. SQL Server: se conecta y cuenta los documentos por estado. Si no conecta, si falta la base o si faltan las tablas, es un `error`. Si a `rag.Fragmento` le faltan las columnas de la página, también, y dice que se corra `sql\base.sql`.
2. Full-Text: que esté instalado (si no, `error`) y sin cambios pendientes (si hay, `aviso`).
3. Los dos modelos instalados en Ollama.
4. La dimensión de los vectores: 1024.
5. Qdrant: si no responde, `error`. Si la colección todavía no existe, `aviso`. Si tiene más puntos que fragmentos hay en la base (trae puntos de otra carga), o, sin pendientes, una cantidad distinta a la de fragmentos de los documentos listos, `aviso`, con "Reprocesar todo".
6. La base de origen, solo si está configurada: si no conecta o la consulta de la lista falla, `aviso`.

Los chequeos corren todos a la vez, así un servidor caído no demora a los demás. Con algún `error`, el código HTTP es 503. Las dos páginas muestran estos chequeos arriba, con un punto verde, amarillo o rojo.

**Cargar un PDF arrastrado (`POST /documentos`):**

1. La página manda los archivos como un formulario. Si la subida pasa de `Rag:MegasPorSubida`, la API contesta 413.
2. `Documentos.AnotarSubidosAsync` revisa cada archivo. Si no es un PDF (no tiene la marca `%PDF-` al principio), lo rechaza. Si dos tienen el mismo nombre, vale el último.
3. Calcula la huella de carga: el sha256 de los bytes seguidos de los parámetros de corte (la versión del `Chunker`, el tamaño de fragmento, el solapamiento y la versión de PdfPig).
4. Si no había un documento con ese nombre, lo guarda con sus bytes, en estado `pendiente`. Si había uno con la misma huella, no hace nada. Si había uno con otra huella, guarda los bytes nuevos y lo vuelve a `pendiente`.
5. Contesta enseguida, con cuántos son nuevos, cuántos ya estaban, cuántos se reemplazaron y cuántos se rechazaron. No procesa nada.

**Importar de la base de origen (`POST /documentos/importar`):**

1. `Documentos.AnotarOrigenAsync` corre `Conexiones:ConsultaLista` contra `Conexiones:SqlOrigen`. La consulta devuelve dos columnas: el número de cada documento y su nombre. No trae los PDF.
2. Anota como `pendiente`, con su número en `OrigenId`, cada documento que todavía no esté, de a 1.000. La lista entera son dos textos cortos por documento: decenas de miles entran en memoria sin problema.
3. Los importados no guardan el PDF: se identifican por su número, no por su nombre, así que dos con el mismo nombre no se pisan.

**Procesar los pendientes (el `Procesador`, en segundo plano):**

1. El `Procesador` es un bucle que arranca con la API. En cada vuelta le pide a `Cargador.ProcesarSiguienteAsync` que procese un documento. Si no había ninguno, espera 5 segundos. Si un servidor no responde, lo anota en el log una vez y espera 30.
2. El `Cargador` pide el próximo pendiente: primero los arrastrados, después los importados, así un PDF que se suelta en la página no espera detrás de miles.
3. Toma el turno (`Documentos.Turno`): mientras procesa un documento, nadie le borra los fragmentos ni los vectores.
4. Le suma un intento (`Intentos`). Si ya llevaba 3 sin terminar, borra los fragmentos y los vectores que hubiera dejado, lo deja en `error` y sigue: así un PDF que cuelga o hace caer el proceso no frena a los demás. Después borra lo que hubiera quedado de un intento anterior: sus puntos de Qdrant y sus fragmentos de SQL.
5. Consigue los bytes: de `rag.Documento.Contenido` si se arrastró, o de la base de origen con `Conexiones:ConsultaContenido` si se importó. Si no están, el documento queda en `error`.
6. PdfPig saca el texto de cada página, con un tope de 5 minutos. Una página sin texto también ocupa su lugar en la lista: el número de página sale de la posición. Si el PDF está roto, tiene clave o no termina, queda en `error` con el motivo. Si tiene menos de 40 palabras por página en promedio, es un escaneo: queda `sin texto`.
7. `Chunker.PartirPaginas` lo corta por párrafos, y los fragmentos se guardan en `rag.Fragmento`, en una transacción, cada uno empezando con `Documento: <nombre>` y con su página en `Pagina`. `PaginaFin` se guarda solo si el fragmento termina en otra página.
8. De a `Rag:LoteVectores` fragmentos, le pide los vectores a Ollama y los sube a Qdrant: el id del punto es el número de fragmento, y lleva el número de documento como dato extra.
9. Marca el documento `listo`, solo si sigue siendo el mismo PDF: si mientras tanto lo reemplazaron o lo borraron, descarta lo armado. Recién `listo` aparece en las búsquedas. Si el texto salió dudoso (palabras pegadas, caracteres raros), queda anotado en `Detalle`.
10. Si algo falla, depende de quién tuvo la culpa:
    - **El documento** (no es un PDF, está roto, es un escaneo): queda en `error` o `sin texto` con el motivo, sin fragmentos en SQL ni vectores en Qdrant, y se sigue con el próximo.
    - **Un servidor** (Ollama, Qdrant o la base de origen no responden, Qdrant contesta con un error, como una clave mal puesta, Ollama está mal configurado, porque el modelo no está bajado o da vectores de otro tamaño, o falla SQL Server): el documento sigue `pendiente`, sin fragmentos a medias y sin gastar un intento, y se reintenta cada 30 segundos. La página muestra el motivo.
    - **No se sabe** (Ollama contestó con otro error, o una consulta tardó demasiado): se reintenta, y al tercer intento queda en `error`.

Como el estado está en la tabla, la API se puede apagar en cualquier momento: al volver sigue donde quedó.

**Cortar por párrafos (`Chunker.PartirPaginas`):** recibe el texto con una página por elemento, y cada palabra lleva el número de su página, contado desde 1.

1. El texto se separa en párrafos por las líneas en blanco, que es como PdfPig marca un espacio grande entre dos renglones. Un párrafo de menos de 15 palabras, como un título, se pega al que le sigue, aunque esté al pie de una página y el que le sigue abra la siguiente.
2. Los párrafos se juntan enteros en un fragmento mientras entren en 300 palabras.
3. Si un párrafo no entra y el fragmento ya tiene al menos 150 palabras, el fragmento se cierra antes del párrafo. Si tiene menos, el principio del párrafo lo completa. Un párrafo de más de 300 palabras se corta en ventanas.
4. Cada fragmento empieza con las últimas 50 palabras del anterior. Si al final quedan menos de 50 palabras nuevas, se suman al último fragmento en lugar de abrir uno casi repetido.
5. Cada fragmento sale (record `Parte`) con la página de su primera palabra, contando las que repite del anterior, y la de la última.

`Chunker.Partir(texto, ...)` es `PartirPaginas` con una sola página, y devuelve solo los textos. La página no entra en `Chunker.Version`, que es `parrafos-1`, porque no cambia el corte: por eso la huella de un PDF es la misma con página o sin ella, y a un fragmento guardado sin página se la pone "Reprocesar todo", no volver a arrastrar el PDF.

La página es la física, la que muestra el visor, no la que está impresa en la hoja. Con fragmentos de 300 palabras, cerca de la mitad cruza de página.

**Mirar, borrar y rehacer:**

- `GET /documentos` devuelve cuántos documentos hay en cada estado, cuántos fragmentos, cuántos documentos por minuto se procesaron en los últimos 10 minutos, lo que falta, una página de 50 documentos, de todos o de un estado, y, si la carga está frenada, el aviso con el motivo.
- `DELETE /documentos/{id}` borra los puntos de Qdrant del documento y después su fila; los fragmentos se van en cascada.
- `POST /documentos/reintentar` pasa los que quedaron en `error` a `pendiente`.
- `POST /documentos/reprocesar` deja todos los documentos `pendiente`, borra todos los fragmentos (de a 20.000) y borra la colección de Qdrant: se rehace todo desde los PDF. Si a `rag.Fragmento` le faltan las columnas de la página, no borra nada y devuelve un 502 que dice que se corra `sql\base.sql`.

La colección de Qdrant se crea sola antes de subir los primeros vectores, con vectores de 1024 números comparados por coseno y con dos umbrales bajos: `indexing_threshold` en 10 KB, para que Qdrant arme el índice HNSW enseguida, y `full_scan_threshold` en 10 KB, para que la búsqueda lo use. También se le crea un índice sobre el dato `documentoId`, para que borrar los puntos de un documento no recorra toda la colección.

**Buscar (`GET /buscar?q=...&modo=...`):** `Buscador.ElegirModo` controla el modo; si no existe, la API devuelve un error 400. Solo se buscan los documentos `listo`. Cada fragmento vuelve con su número, el número y el archivo de su documento, `pagina`, `paginaFin` y el texto.

- **Por palabras (`palabras`):** `FREETEXTTABLE` corta la pregunta en palabras, busca sus plurales y conjugaciones en el índice, puntúa cada fragmento y devuelve los 5 mejores.
- **Por significado (`vectores`):** Ollama calcula el vector de la pregunta, con la instrucción en inglés de `Ollama:InstruccionPregunta` antes. Qdrant devuelve los números de los 5 fragmentos más cercanos, y SQL Server sus textos, en ese orden.
- **Híbrida (`hibrida`)**, el modo por defecto: arranca las dos búsquedas al mismo tiempo, pidiendo 20 candidatos a cada una (`Rag:CandidatosHibrida`), y espera a las dos con `Task.WhenAll`: tarda lo que la más lenta, no la suma. `Rrf.Mezclar` mezcla las dos listas por puesto: cada fragmento suma 1 / (60 + puesto) en cada lista donde aparece, y quedan los 5 de mayor puntaje.

Se mezclan puestos y no puntajes porque el `RANK` del Full-Text y la distancia coseno miden cosas distintas, en escalas distintas: no se pueden sumar.

**Preguntar (`POST /preguntar`):**

1. `Validador.RevisarPregunta` controla la pregunta: vacía, más larga que `Rag:LargoMaximoPregunta`, con caracteres de control o sin letras ni números da un 400 con el motivo. Un modo que no existe, también.
2. El `Redactor` busca los 5 mejores fragmentos con el modo pedido; sin modo, con la híbrida.
3. Si no encontró ninguno, responde "No encontré esa información en los documentos." sin llamar al modelo.
4. Si encontró, arma el mensaje: los fragmentos numerados `[1]` a `[5]` entre `<fragmentos>` y `</fragmentos>`, con los `<` y `>` del texto cambiados, y la pregunta al final. Al modelo no se le dice la página.
5. Se lo manda al modelo de chat con las cinco reglas como mensaje de sistema.
6. Si el modelo respondió la frase de "no encontré", la devuelve sin fuentes. Si no, devuelve el texto con citas y las fuentes en el mismo orden. Cada fuente lleva el número de la cita, `documentoId`, el archivo, `pagina`, `paginaFin` y el número de fragmento.
7. Registra la consulta en `rag.Consulta`: la pregunta, el modo, los fragmentos, la respuesta y los tiempos. Si el registro falla, lo anota en el log y la respuesta sale igual.

**Abrir el PDF de una fuente (`GET /documentos/{id}/pdf`):**

1. `Documentos.LeerPdfAsync` busca el documento y lee sus bytes de SQL, del mismo lugar que cuando se procesa: `rag.Documento.Contenido` si se arrastró, la base de origen con `Conexiones:ConsultaContenido` si se importó. No importa en qué estado esté.
2. Si no existe el documento, si no tiene PDF o si lo guardado no es un PDF, responde 404. Si falla la base de origen, 502 o 503.
3. Devuelve el PDF con tres encabezados: `Content-Disposition: inline`, para que el navegador lo muestre en su visor; `X-Content-Type-Options: nosniff`; y `Cache-Control: no-store`, porque un documento se puede reemplazar.
4. La página no viaja en el pedido. Quien enlaza le agrega `#page=N` a la dirección, y esa parte la usa el visor del navegador: no llega a la API.

Límites: el PDF se lee entero de SQL en cada clic, y un importado con la base de origen lenta puede tardar hasta 300 segundos. El enlace abre donde empieza el fragmento, que por las 50 palabras repetidas a veces es la página anterior a la de la parte principal. Si el navegador está configurado para descargar los PDF, lo descarga y no va a la página. Un error al abrir (el documento se borró, la base de origen está apagada) se ve como JSON en la pestaña. Si un documento se reemplaza, una respuesta que quedó en pantalla abre el PDF que hay guardado en la página del anterior.

**Responder errores claros:**

1. Delante de los clientes de Ollama y de Qdrant hay un `DetectorDeCaidas`, que lleva el reloj de cada pedido: si el servicio no responde, o no responde a tiempo (un firewall que descarta los pedidos, por ejemplo), el error dice cuál y en qué dirección. Un error de la base de origen también dice que es esa base y qué revisar.
2. Cualquier error que nadie atrapó lo atiende el `ManejadorDeErrores`: Ollama o Qdrant que no responden son 503; que respondieron con un error, 502; que no respondieron a tiempo, 504; cualquier error de SQL Server, 503; un pedido que no se pudo leer, 400; una subida demasiado grande, 413; y el resto, 500.
3. Los errores salen en el formato estándar ProblemDetails (`application/problem+json`): `title`, `status` y `detail`. `UseStatusCodePages` le pone ese cuerpo también a los errores que salían vacíos: 404, 405 y 415. Las páginas muestran el título y el detalle.

**Las páginas:**

- `index.html`: manda la pregunta y el modo a `POST /preguntar`, y muestra la respuesta, las fuentes (las citadas con `[n]` en texto normal, las otras atenuadas) y lo que tardó cada parte. Cada fuente es un enlace a `GET /documentos/{id}/pdf` que abre el PDF en otra pestaña, en la página del fragmento (`#page=N`): "archivo, página 3 (fragmento 41)", "archivo, páginas 3 a 4 (fragmento 41)" si cruza de página, o "archivo (fragmento 41)", sin `#page`, si el fragmento no tiene página.
- `documentos.html`: la zona para soltar PDF, el botón de importar (prendido solo si `GET /info` dice que hay base de origen), los números del avance y la lista. Se actualiza sola: cada 5 segundos si hay pendientes, cada 30 si no.
- `estado.js` dibuja la franja de estado de las dos, y `estilos.css` tiene lo común.

## 3. Tablas

Base de `Conexiones:SqlTextos` (en casa, `RagBasico`), esquema `rag`. Todo `sql\base.sql` usa SQL que entiende un SQL Server 2014.

| Tabla | Columna | Tipo | Para qué |
|---|---|---|---|
| `rag.Documento` | `DocumentoId` | `INT IDENTITY` | Número del documento, clave primaria. Va a Qdrant como dato extra de cada punto |
| `rag.Documento` | `Archivo` | `NVARCHAR(260)` | Nombre del PDF. Único entre los arrastrados |
| `rag.Documento` | `Titulo` | `NVARCHAR(400)` | Nombre sin `.pdf` |
| `rag.Documento` | `OrigenId` | `NVARCHAR(100)` | El número del documento en la base de origen, si se importó. `NULL` si se arrastró. Único entre los importados |
| `rag.Documento` | `Contenido` | `VARBINARY(MAX)` | Los bytes del PDF, solo si se arrastró. Los importados se leen de la base de origen |
| `rag.Documento` | `Huella` | `VARBINARY(32)` | La huella de carga de los arrastrados: sha256 de los bytes más los parámetros de corte |
| `rag.Documento` | `Estado` | `VARCHAR(20)` | `pendiente`, `listo`, `sin texto` o `error`. Por defecto, `pendiente` |
| `rag.Documento` | `Detalle` | `NVARCHAR(1000)` | El motivo, si quedó en `error` o `sin texto`, o la duda sobre el texto si quedó `listo` |
| `rag.Documento` | `FechaCarga` | `DATETIME2` | Cuándo se anotó. Por defecto, la fecha del servidor |
| `rag.Documento` | `FechaProceso` | `DATETIME2` | Cuándo dejó de estar pendiente. Con esto se calcula la velocidad |
| `rag.Documento` | `Intentos` | `INT` | Cuántas veces se empezó a procesar sin terminar. Con 3, queda en `error` |
| `rag.Fragmento` | `FragmentoId` | `INT IDENTITY` | Número del fragmento, clave primaria y clave del índice Full-Text. Es también el id de su punto en Qdrant |
| `rag.Fragmento` | `DocumentoId` | `INT` | A qué documento pertenece. Si se borra el documento, se borra el fragmento (cascada) |
| `rag.Fragmento` | `Orden` | `INT` | Posición del fragmento dentro del documento, desde 1 |
| `rag.Fragmento` | `Texto` | `NVARCHAR(MAX)` | El texto, que empieza con `Documento: <nombre>` |
| `rag.Fragmento` | `Pagina` | `INT` | La página física del PDF, contada desde 1, donde está la primera palabra del fragmento, incluidas las que repite del anterior. `NULL` en un fragmento guardado sin página, hasta "Reprocesar todo" |
| `rag.Fragmento` | `PaginaFin` | `INT` | La página donde termina el fragmento, solo si termina en otra. `NULL` si empieza y termina en la misma |
| `rag.Consulta` | `ConsultaId` | `INT IDENTITY` | Número de la consulta, clave primaria |
| `rag.Consulta` | `Fecha` | `DATETIME2` | Cuándo se hizo. Por defecto, la fecha del servidor |
| `rag.Consulta` | `Pregunta` | `NVARCHAR(2000)` | La pregunta, recortada a 2.000 caracteres |
| `rag.Consulta` | `Modo` | `VARCHAR(20)` | El modo de búsqueda: `palabras`, `vectores` o `hibrida` |
| `rag.Consulta` | `FragmentosIds` | `NVARCHAR(400)` | Los números de los fragmentos que se le pasaron al modelo, separados por comas |
| `rag.Consulta` | `Respuesta` | `NVARCHAR(MAX)` | Lo que respondió |
| `rag.Consulta` | `MsBusqueda` | `INT` | Lo que tardó la búsqueda; por significado, incluye calcular el vector de la pregunta |
| `rag.Consulta` | `MsModelo` | `INT` | Lo que tardó el modelo, medido desde la API |
| `rag.Consulta` | `MsCarga` | `INT` | Lo que tardó Ollama en cargar el modelo |
| `rag.Consulta` | `MsLectura` | `INT` | Lo que tardó el modelo en leer los fragmentos y la pregunta |
| `rag.Consulta` | `MsEscritura` | `INT` | Lo que tardó en escribir la respuesta |

Índices: dos únicos con filtro en `rag.Documento` (`Archivo` entre los arrastrados, `OrigenId` entre los importados), uno por `Estado` para encontrar rápido el próximo pendiente, y uno por `DocumentoId` en `rag.Fragmento`.

Índice Full-Text: catálogo `ftRag`, sin distinguir acentos, sobre `rag.Fragmento.Texto`, en español (3082), con la lista de palabras vacías del sistema y actualización automática.

Colección de Qdrant (`Conexiones:ColeccionQdrant`): un punto por fragmento de un documento procesado. El id es `FragmentoId`, el vector son los 1024 números que calculó el modelo de embeddings, y el dato extra (payload) es `documentoId`.

Base de origen, la de prueba que arma `scripts\crear-origen-de-prueba.ps1`: tabla `dbo.TABLA_DE_PDF` con `Id`, `Nombre` y `Contenido`. En la base real, la tabla y las columnas tienen otros nombres: se escriben en las dos consultas.

## 4. Configuración

En `RagBasico.Api\appsettings.json`. El bloque `Conexiones` es todo lo que conecta:

| Clave | Valor | Para qué |
|---|---|---|
| `Conexiones:SqlTextos` | `Server=localhost\SQLEXPRESS;Database=RagBasico;...` | La base del RAG: documentos, fragmentos, Full-Text y consultas |
| `Conexiones:Qdrant` | `http://127.0.0.1:6333` | Dónde escucha Qdrant. Con `127.0.0.1` y no `localhost`, porque en casa Qdrant escucha solo en IPv4 |
| `Conexiones:ColeccionQdrant` | `fragmentos` | La colección donde van los vectores. "Reprocesar todo" borra esta colección y ninguna otra |
| `Conexiones:ClaveQdrant` | vacío | La clave de Qdrant, si el servidor pide una. Va en el encabezado `api-key` de cada pedido |
| `Conexiones:Ollama` | `http://127.0.0.1:11434` | Dónde escucha Ollama |
| `Conexiones:ModeloChat` | `qwen3.5:4b` | El modelo que redacta las respuestas |
| `Conexiones:ModeloEmbeddings` | `qwen3-embedding:0.6b` | El modelo que calcula los vectores. Tiene que dar vectores de 1024 números |
| `Conexiones:SqlOrigen` | vacío | La base donde ya están guardados los PDF. Vacío: no hay importación |
| `Conexiones:ConsultaLista` | `SELECT Id, Nombre FROM dbo.TABLA_DE_PDF` | Devuelve el número y el nombre de cada documento de la base de origen. Para probar con pocos, se le pone un `TOP` o un `WHERE` |
| `Conexiones:ConsultaContenido` | `SELECT Contenido FROM dbo.TABLA_DE_PDF WHERE Id = @Id` | Devuelve el binario de un documento. `@Id` es su número |

El resto no hace falta tocarlo para cambiar de servidor:

| Clave | Valor | Para qué |
|---|---|---|
| `Rag:Proyecto` | `RAG básico` | El nombre del proyecto que devuelve `GET /info` |
| `Rag:PalabrasPorFragmento` | `300` | El tamaño máximo de cada fragmento, en palabras. Entra en la huella |
| `Rag:Solapamiento` | `50` | Cuántas palabras repite cada fragmento del anterior. También entra en la huella |
| `Rag:TopK` | `5` | Cuántos fragmentos devuelve la búsqueda y se le pasan al modelo |
| `Rag:LoteVectores` | `16` | Cuántos fragmentos se le mandan a Ollama en cada pedido de vectores |
| `Rag:CandidatosHibrida` | `20` | Cuántos fragmentos le pide la híbrida a cada búsqueda antes de mezclarlos |
| `Rag:LargoMaximoPregunta` | `500` | El largo máximo de una pregunta, en caracteres. Una más larga da un error 400 |
| `Rag:MegasPorSubida` | `200` | El tope de una subida de PDF, en megas. Una más grande da un error 413 |
| `Ollama:InstruccionPregunta` | `Given a question, retrieve relevant passages that answer the question` | La instrucción que va antes de cada pregunta al calcular su vector |
| `Ollama:SegundosDeEspera` | `300` | Cuánto espera la API una respuesta de Ollama antes de cortar |
| `Ollama:MantenerCargado` | `30m` | Cuánto tiempo deja Ollama cada modelo en memoria después del último pedido |
| `Qdrant:SegundosDeEspera` | `30` | Cuánto espera la API a Qdrant |
| `Logging:LogLevel:Default` | `Information` | Cuánto detalle escribe el log en la consola |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Warning` | Menos detalle para los mensajes internos de ASP.NET Core |
| `Logging:LogLevel:System.Net.Http.HttpClient` | `Warning` | Sin cuatro líneas en la consola por cada pedido a Ollama y a Qdrant |
| `AllowedHosts` | `127.0.0.1;localhost` | A qué nombres tiene que venir dirigido un pedido para que la API lo acepte: solo los de esta PC. Con otro, responde 400. Además, `Program.cs` rechaza con 403 los pedidos que cambian algo (`POST`, `DELETE`) si vienen de una página de otro sitio: mira el encabezado `Origin` |

Fuera de `appsettings.json`:

| Dónde | Qué | Para qué |
|---|---|---|
| `RagBasico.Api\conexiones.json` | Las líneas de `Conexiones` que cambian | Opcional. Si existe, sus líneas pisan a las de `appsettings.json`. Git lo ignora: ahí van los servidores y las claves que no se suben |
| `conexiones.oficina.ejemplo.json` | Las diez líneas de `Conexiones`, con nombres de ejemplo | El modelo para armar `conexiones.json`: se copia y se reemplazan los nombres en mayúsculas. Cómo, en [`docs\oficina.md`](oficina.md) |
| `Properties\launchSettings.json` | `applicationUrl`: `http://127.0.0.1:5090` | Dirección y puerto. La API escucha solo en esta PC |
| `global.json` | `version`: `10.0.403`, `rollForward`: `latestPatch` | El SDK de .NET con el que se compila |
| `scripts\iniciar-qdrant.ps1` | `QDRANT__SERVICE__HOST`, `QDRANT__STORAGE__STORAGE_PATH`, `QDRANT__STORAGE__SNAPSHOTS_PATH` y `QDRANT__TELEMETRY_DISABLED` | La configuración del Qdrant de casa, con variables de entorno que valen solo para su proceso |

## 5. Fuera del repo

Cómo instalar cada pieza y comprobar que anda: [`docs\instalacion.md`](instalacion.md).

| Qué | Dónde |
|---|---|
| Ollama y el modelo `qwen3.5:4b` | Instalados en la PC; Ollama escucha en `127.0.0.1:11434` |
| SQL Server, con Full-Text | Instancia `localhost\SQLEXPRESS`, base `RagBasico` |
| El modelo `qwen3-embedding:0.6b` | En Ollama: `ollama pull qwen3-embedding:0.6b` |
| Qdrant 1.20.2 y sus datos | `qdrant.exe` en `..\RAG-BASICO-datos\qdrant\`, datos en su subcarpeta `storage` |
| Los PDF de prueba | Cualquier carpeta, por ejemplo `..\RAG-BASICO-datos\documentos\`: se arrastran a la página |
| La base de origen de prueba | Base `RagBasico_Origen`, creada por `scripts\crear-origen-de-prueba.ps1` |
| Las conexiones a otros servidores | `RagBasico.Api\conexiones.json`, que git ignora |
| Los servidores de la oficina: SQL Server, Qdrant, Ollama y la base donde ya están los PDF | Cada uno en su máquina. Cómo prepararlos y comprobarlos: [`docs\oficina.md`](oficina.md) |
