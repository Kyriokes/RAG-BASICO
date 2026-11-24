# Manual de lectura

Para leer el proyecto entero y poder explicar cada archivo. Sigue dos recorridos de punta a punta, el de un PDF que se carga y el de una pregunta que se responde, y después lo que sostiene a los dos.

Cómo usarlo: abrí cada archivo al lado de este documento y leelo en el orden que dice. Cada archivo tiene acá qué hace, por qué existe, en qué orden leer sus funciones, lo que es nuevo viniendo de VB.NET, y las preguntas que te pueden hacer, con su respuesta.

El detalle de cada función está en [`responsabilidades.md`](responsabilidades.md); las tablas y la configuración, en [`arquitectura.md`](arquitectura.md).

## 1. El proyecto en diez líneas

1. Es un RAG: busca en documentos los párrafos que sirven para una pregunta, y un modelo de lenguaje redacta la respuesta **usando solo esos párrafos** y citando de cuál salió cada cosa.
2. Tiene cuatro piezas: la API (este repo), SQL Server, Qdrant y Ollama.
3. **SQL Server** guarda los PDF, los párrafos y el índice de palabras.
4. **Qdrant** guarda los vectores: una lista de 1024 números por párrafo, que representa su significado.
5. **Ollama** corre dos modelos: uno convierte texto en vectores, el otro redacta.
6. **La API** es lo único que escribimos nosotros: recibe los PDF y las preguntas y coordina a los otros tres.
7. Cada dato vive en un solo lugar, y cada lugar tiene una línea en el bloque `Conexiones` de `appsettings.json`.
8. Un PDF entra por la página (o se importa de otra base), queda `pendiente`, y un trabajo en segundo plano lo procesa.
9. Una pregunta se busca de dos maneras a la vez, por palabras y por significado, y se mezclan los resultados.
10. Si algo no anda, la página dice qué pieza es y qué hacer.

```text
                 PDF                                    pregunta
                  │                                         │
          documentos.html                              index.html
                  │                                         │
                  ▼                                         ▼
   ┌──────────────────────────────── Program.cs ────────────────────────────────┐
   │  POST /documentos            GET /documentos           POST /preguntar      │
   └──────┬──────────────────────────────────────────────────────┬──────────────┘
          ▼                                                      ▼
     Documentos  ◄── Procesador ──► Cargador                 Redactor
     (anota)         (da vueltas)   (procesa uno)                │
          │                            │                         ▼
          │              Chunker ◄─────┤                     Buscador ──► Rrf
          ▼                            ▼                      │      │
      SQL Server ◄─────────────── Ollama, Qdrant ◄────────────┘      │
          ▲                                                          │
          └──────────────────────────────────────────────────────────┘
```

## 2. Antes de empezar: tres archivos que no son código

### `RagBasico.Api\appsettings.json`

**Qué es:** la configuración. Lo primero que hay que mirar es el bloque `Conexiones`, arriba de todo: ahí está todo lo que conecta. La cadena de SQL Server, la dirección de Qdrant con su colección y su clave, la dirección de Ollama con los nombres de los dos modelos, y la base de origen con sus dos consultas.

**Por qué importa:** es la respuesta a "¿qué hay que tocar para instalarlo en otro lado?". Diez líneas, en un solo lugar.

**Te pueden preguntar:**

- *¿Las claves quedan en el repo?* No. Para otros servidores se crea `conexiones.json` al lado, que git ignora, con las líneas que cambian. `Program.cs` lo carga si existe.
- *¿Por qué `127.0.0.1` y no `localhost`?* En casa Qdrant escucha solo en IPv4, y `localhost` a veces resuelve a IPv6.

### `sql\base.sql`

**Qué es:** toda la base, en un script que se puede correr de nuevo sin romper nada. Tres tablas en el esquema `rag`:

- `rag.Documento`: un registro por PDF, con su `Estado` (`pendiente`, `listo`, `sin texto`, `error`). Si se arrastró, guarda los bytes en `Contenido`. Si se importó, guarda su número de la otra base en `OrigenId` y no guarda los bytes.
- `rag.Fragmento`: los párrafos de cada PDF. `FragmentoId` es también el id del vector en Qdrant: ese número es lo que une las dos bases. `Pagina` es la página del PDF donde empieza el fragmento, y `PaginaFin`, la página donde termina, solo si termina en otra.
- `rag.Consulta`: el registro de cada pregunta, con su respuesta y sus tiempos.

Y el índice Full-Text sobre `rag.Fragmento.Texto`, en español, que es lo que hace la búsqueda por palabras.

**Te pueden preguntar:**

- *¿Por qué no hay una columna con los vectores?* Porque están solo en Qdrant. Tenerlos en los dos lados obligaba a mantenerlos iguales, y a usar SQL Server 2025, que es el único que tiene el tipo `VECTOR`.
- *¿Qué es `ON DELETE CASCADE`?* Si se borra un documento, SQL Server borra solo sus fragmentos.
- *¿Qué son esos índices con `WHERE`?* Índices únicos con filtro: entre los arrastrados no puede haber dos con el mismo nombre; entre los importados, no puede haber dos con el mismo número de origen.
- *¿Anda en un SQL Server viejo?* Sí: usa solo lo que entiende un SQL Server 2014 con Full-Text.
- *¿Por qué `Pagina` y `PaginaFin` aparecen dos veces, en el `CREATE TABLE` y en un `ALTER TABLE`?* El `CREATE TABLE` corre solo si la tabla no existe: sirve para una base nueva. Una base que ya tenía la tabla sin esas columnas necesita el `ALTER TABLE`, que corre solo si la columna falta (`COL_LENGTH` devuelve `NULL`). Así el mismo script sirve para crear y para actualizar, y correrlo dos veces no rompe nada.
- *¿Por qué cada `ALTER TABLE` va en su lote, con su `GO`?* SQL Server revisa un lote entero antes de correrlo. Con cada columna en su lote, lo que viene después ya la ve creada.
- *¿Por qué las dos columnas aceptan `NULL`?* Por dos motivos distintos. `Pagina` en `NULL` es un fragmento que se cargó antes de que existiera la columna: no se inventa una página. `PaginaFin` en `NULL` es un fragmento que empieza y termina en la misma página.
- *¿Cómo se actualiza una base que ya tiene documentos?* Se frena la API y se corre `scripts\crear-base.ps1`: agrega las columnas y no toca lo cargado. Después, "Reprocesar todo" en la página "Documentos", para que los fragmentos se rehagan con su página. Mientras tanto, `GET /salud` avisa: si faltan las columnas, el chequeo de SQL Server da error y dice qué correr.

### `RagBasico.Api\Program.cs`

**Qué es:** el arranque. Se lee de arriba a abajo, en cuatro partes:

1. **La configuración:** carga `conexiones.json` si existe.
2. **Los servicios** (`builder.Services.Add...`): le dice a ASP.NET Core qué clases existen, para que las arme cuando alguien las pida. `Ollama` y `Qdrant` son clientes HTTP; `Documentos`, `Cargador`, `Buscador`, `Redactor` y `Salud` se crean nuevos en cada pedido; `Procesador` arranca con la API y queda corriendo.
3. **El manejo de errores, el control de origen y los archivos de las páginas** (`app.Use...`). El control de origen hace que los pedidos que cambian algo solo se acepten de las páginas de la propia API; si vienen de otra, responde 403.
4. **Los pedidos** (`app.MapGet`, `app.MapPost`, `app.MapDelete`): cada uno es una dirección de la API y la función que la atiende. Casi todos tienen dos o tres líneas: controlan lo que llegó y llaman a una clase.

**Nuevo viniendo de VB.NET:**

| C# | VB.NET | Qué es |
|---|---|---|
| El archivo no tiene `Sub Main` | `Sub Main()` | Las instrucciones sueltas del archivo son el `Main` |
| `builder.Services.AddScoped<Buscador>()` | no hay | Inyección de dependencias: anota que existe `Buscador`. Cuando un pedido lo necesita, ASP.NET Core lo crea y le pasa lo que pide su constructor |
| `async (Buscador buscador, CancellationToken ct) => ...` | `Async Function(buscador, ct) ...` | Una función sin nombre (lambda). Los parámetros los completa ASP.NET Core |
| `await` | `Await` | Espera el resultado sin trabar el hilo |
| `string? modo` | `modo As String` (puede ser `Nothing`) | El `?` avisa que puede venir `null` |

**Te pueden preguntar:**

- *¿Dónde está la lógica?* No acá. `Program.cs` solo conecta: cada pedido llama a una clase.
- *¿Qué es `CancellationToken`?* Un aviso de "el que pidió ya se fue". Si se cierra el navegador a mitad de una pregunta, la API corta en vez de seguir trabajando para nadie.
- *¿Por qué `HttpRequest` en `POST /documentos`?* Para poder mirar el tamaño antes de leer los archivos, y contestar "la subida pasa de 200 MB" con un 413.
- *¿Qué hace `GET /documentos/{id:int}/pdf`?* Devuelve el PDF de un documento para que el navegador lo abra. Es adonde apuntan los enlaces de las fuentes. Es el único pedido que no devuelve JSON, y por eso es el más largo: arma tres encabezados a mano.
- *¿Por qué `Content-Disposition: inline`?* Le dice al navegador "mostralo", no "descargalo". Con `attachment` lo bajaría a la carpeta de descargas y el `#page=N` del enlace no serviría para nada. El nombre del archivo lo escribe `SetHttpFileName`, que lo deja bien armado para un encabezado aunque tenga tildes, eñes o comillas.
- *¿Por qué `nosniff`?* Sin eso, un navegador puede mirar el contenido y decidir por su cuenta que es otra cosa, por ejemplo una página con scripts. Con `nosniff` lo trata como PDF y como nada más. Además, antes de mandarlo se controla que empiece con `%PDF-`: en la base de origen, lo que hay guardado lo cargó otro sistema.
- *¿Por qué `no-store`?* Un documento se puede reemplazar por otro con el mismo nombre y el mismo número. Si el navegador guardara una copia, mostraría el PDF viejo.
- *¿Por qué `{id:int}`?* Si en la dirección no viene un número, ASP.NET Core contesta 404 sin llamar a la función. No hay que controlarlo a mano.

## 3. El camino de un PDF

### `RagBasico.Api\wwwroot\documentos.html`

**Qué hace:** la página de carga. HTML, CSS y JavaScript a mano, sin librerías. Tiene la zona para soltar archivos, el botón de importar, los cinco números del avance y la lista.

**Orden de lectura del JavaScript:** `pedir` (habla con la API y muestra el error si lo hay), `subir` (arma un formulario con los archivos y lo manda a `POST /documentos`), `actualizar` (pide `GET /documentos` cada 5 segundos si hay pendientes, cada 30 si no) y `dibujar`.

**Te pueden preguntar:**

- *¿Por qué `textContent` y no `innerHTML`?* Porque el nombre de un archivo lo elige quien lo sube. Con `innerHTML`, un nombre con etiquetas se ejecutaría como parte de la página.
- *¿La página procesa algo?* No. Solo manda los archivos y mira el estado. Se puede cerrar y la carga sigue.

### `RagBasico.Api\Documentos.cs`

**Qué hace:** es el registro de los documentos. Anota los que llegan, dice cuál sigue, guarda el estado de cada uno, los lista y los borra. **No procesa nada.**

**Por qué existe:** para que haya un solo lugar que escriba en `rag.Documento`. El `Cargador` procesa; `Documentos` lleva la cuenta.

**Orden de lectura:**

1. `EsPdf`, `CalcularHuella`, `ParametrosDeCorte`, `EstimarFalta`: cuatro funciones sueltas (`static`), sin base de datos. Son las más fáciles.
2. `AnotarSubidosAsync`: lo que pasa cuando se sueltan archivos en la página. Por cada uno: ¿es un PDF? ¿ya estaba? ¿cambió?
3. `AnotarOrigenAsync`: lo mismo para la base de origen. Corre la consulta de la lista y anota los que faltan, de a 1.000.
4. `SumarIntentoAsync`, `DevolverIntentoAsync` y `MarcarListoAsync`: lo que usa el `Cargador` para llevar la cuenta de los intentos y cerrar un documento.
5. `SiguientePendienteAsync` y `LeerContenidoAsync`: lo que le pide el `Cargador`.
6. `LeerPdfAsync`: trae el PDF de un documento para abrirlo. Busca el documento y llama a `LeerContenidoAsync`, la misma función que usa el `Cargador`.
7. `MarcarAsync`, `ResumirAsync`, `BorrarAsync`, `ReintentarAsync`, `ReprocesarTodoAsync`.

**Nuevo viniendo de VB.NET:**

| C# | VB.NET | Qué es |
|---|---|---|
| `using (SqlConnection conexion = new SqlConnection(_conexion)) { ... }` | `Using conexion As New SqlConnection(_conexion) ... End Using` | Cierra la conexión al salir del bloque, pase lo que pase |
| `conexion.QueryAsync<DocumentoFila>(sql, new { estado })` | no hay | Dapper: corre el SQL y arma un objeto por fila, emparejando columnas con propiedades por el nombre |
| `new { archivo = nombre, huella }` | `New With { .archivo = nombre, .huella = huella }` | Un objeto sin clase, para pasar los parámetros del SQL |
| `public sealed record Pendiente(int DocumentoId, ...)` | una `Class` con propiedades de solo lectura y constructor | Un tipo de datos en una línea |
| `SemaphoreSlim Turno` | `SyncLock`, pero que se puede esperar con `Await` | Un candado: lo toma uno por vez |

**Te pueden preguntar:**

- *¿Qué es la huella?* El sha256 de los bytes del PDF más los parámetros de corte. Si se sube de nuevo el mismo archivo, la huella es igual y no se hace nada. Si cambia el PDF, o cambia la forma de cortarlo, la huella cambia y se reprocesa.
- *¿Hay riesgo de inyección de SQL?* No en los datos: van siempre como parámetros (`@archivo`), nunca pegados al texto del SQL. Las dos consultas de la base de origen sí son texto, pero las escribe quien administra, en la configuración; no vienen de la página.
- *¿Por qué los importados no guardan el PDF?* Ya está guardado en la otra base. Con decenas de miles, copiarlos puede pasar el tope de SQL Server Express.
- *¿Y si en la otra base hay dos PDF con el mismo nombre?* No se pisan: un importado se reconoce por su número, no por su nombre.
- *¿Para qué es `Turno`?* Para que "borrar" y "reprocesar todo" no le saquen los fragmentos a un documento que el `Cargador` está procesando en ese momento.
- *¿Cómo sabe cuánto falta?* Cuenta cuántos documentos se terminaron en los últimos 10 minutos (`FechaProceso`) y divide los pendientes por esa velocidad. No guarda nada en memoria: todo sale de la tabla.
- *¿De dónde sale el PDF que se abre desde una fuente?* De SQL, no de una carpeta. Si se arrastró, de `rag.Documento.Contenido`. Si se importó, de la base de origen, con la misma consulta que usa la carga. No hay carpeta porque no hay ninguna: el PDF ya está en una base, y una carpeta sería una segunda copia para mantener igual, con sus permisos y su ruta para configurar.
- *¿Por qué `LeerPdfAsync` devuelve `null` en un caso y un PDF sin contenido en otro?* Para poder decir cosas distintas. `null`: no existe ese documento. Con nombre y sin contenido: el documento existe pero su PDF no está, por ejemplo porque lo borraron de la base de origen. Los dos son un 404, con otro mensaje.
- *¿Se puede abrir el PDF de un documento que quedó en `error`?* Sí, en cualquier estado. Sirve justamente para mirar qué tiene.
- *¿Para qué es la columna `Intentos`?* Cuenta cuántas veces se empezó a procesar un documento sin terminar. Al tercero queda en `error`. Sin eso, un PDF que cuelga el lector frenaría la cola para siempre.

### `RagBasico.Api\Procesador.cs`

**Qué hace:** el trabajo en segundo plano. Arranca con la API y da vueltas: ¿hay un documento pendiente? Lo procesa y sigue. ¿No hay? Espera 5 segundos. ¿Un servidor no responde? Lo anota en el log una vez y espera 30.

**Por qué existe:** una carga grande tarda horas. No puede depender de que una página quede abierta ni de un pedido HTTP que no se corte.

**Nuevo viniendo de VB.NET:**

| C# | VB.NET | Qué es |
|---|---|---|
| `class Procesador : BackgroundService` | `Inherits BackgroundService` | Una clase que ASP.NET Core arranca con la API y frena cuando se apaga. Es lo más parecido a un servicio de Windows adentro de la API |
| `protected override async Task ExecuteAsync(...)` | `Protected Overrides Async Function ExecuteAsync(...)` | El método que corre de fondo |
| `_alcances.CreateScope()` | no hay | Pide un `Cargador` nuevo para cada vuelta, igual que cada pedido HTTP recibe los suyos |

**Te pueden preguntar:**

- *¿Qué pasa si se apaga la API a mitad de un documento?* Ese documento sigue `pendiente`. Al volver a arrancar se procesa de nuevo, desde cero.
- *¿Por qué de a uno y no varios a la vez?* La velocidad la pone el modelo de embeddings, que atiende de a un pedido. Varios a la vez no irían más rápido, y de a uno nunca hay dos documentos pisándose.
- *¿Y si un PDF está roto? ¿Frena todo?* No. Ese queda en `error` con el motivo y se sigue con el próximo. Lo que frena es un servidor que no responde o que está mal configurado, como un modelo de Ollama sin bajar, y ahí conviene frenar: si no, quedarían todos en error.
- *¿Cómo me entero de que está frenado?* La página "Documentos" lo muestra en rojo, con el servidor y el motivo. El `Procesador` guarda ese aviso en `Procesador.Aviso` y `GET /documentos` lo devuelve.

### `RagBasico.Api\Cargador.cs`

**Qué hace:** procesa **un** documento, de punta a punta. Es el archivo más importante del camino de un PDF.

**Orden de lectura:**

1. `ProcesarSiguienteAsync`: pide el próximo pendiente, toma el turno y llama a `ProcesarAsync`.
2. `ProcesarAsync`: los pasos, numerados en los comentarios.
   0. Cuenta el intento. Si ya van tres sin terminar, borra lo que hubiera dejado y lo deja en `error`.
   1. Borra lo que hubiera quedado de un intento anterior.
   2. Consigue los bytes del PDF.
   3. Saca el texto (`SacarTexto`) y se fija si es un escaneo (`EsEscaneado`).
   4. Lo corta (`Chunker.PartirPaginas`) y guarda los fragmentos en SQL (`GuardarFragmentosAsync`), cada uno con la página donde empieza y, si cruza a otra, la página donde termina.
   5. De a 16 fragmentos, pide los vectores a Ollama y los sube a Qdrant.
   6. Lo marca `listo`, si sigue siendo el mismo PDF.
3. El `catch` del final: decide de quién fue la culpa. De un servidor (`EsDeUnServidor`): queda pendiente y se espera. No se sabe (`EsPasajero`): se reintenta hasta tres veces. Del documento: se borran los vectores que llegó a subir y queda en `error`.
4. `SacarTexto`, `EsEscaneado`, `RevisarTexto`: las tres funciones sueltas.

**Te pueden preguntar:**

- *¿Por qué empieza borrando?* Para que procesar dos veces el mismo documento dé siempre lo mismo. Si un intento quedó a medias, no quedan fragmentos ni vectores duplicados.
- *¿Cómo sabe que un PDF es un escaneo?* Cuenta las palabras: menos de 40 por página en promedio. Un escaneo es una foto, no tiene texto.
- *¿Por qué recién al final lo marca `listo`?* Porque la búsqueda solo mira los documentos `listo`. Uno a medio procesar no aparece.
- *¿Qué pasa si Qdrant se cae en el paso 5?* El error es "de un servidor": se borran los fragmentos que se habían guardado, el documento sigue `pendiente`, no gasta un intento, y el `Procesador` espera y reintenta.
- *¿Y si alguien reemplaza el PDF justo mientras se procesa?* Al final se marca `listo` solo si la huella es la misma con que se empezó. Si cambió, se descarta lo armado y se procesa el nuevo.
- *¿Y si un PDF deja al lector dando vueltas?* Se lo espera 5 minutos. Si no termina, queda en `error` y se sigue.
- *¿Por qué cada fragmento empieza con `Documento: <nombre>`?* Para que la búsqueda también lo encuentre por el nombre del documento.
- *¿Por qué al `Chunker` se le pasa una lista de páginas y no el texto entero?* Porque la página no está escrita en el texto: es la posición en la lista. `SacarTexto` devuelve un texto por página, en orden, y una página en blanco también ocupa su lugar. Si se uniera todo antes de cortar, ese dato se perdería.
- *¿Por qué `PaginaFin` se guarda en `NULL` si el `Chunker` siempre la devuelve?* El `Chunker` devuelve las dos páginas, iguales si el fragmento no cruza. En la tabla se guarda la de fin solo cuando es otra. Así, en SSMS y en la página, "tiene página de fin" quiere decir "cruza de página".
- *¿Con qué se lee el PDF?* Con PdfPig, una librería de código abierto. Es el único paquete, junto con Dapper y el cliente de SQL Server.

### `RagBasico.Api\Chunker.cs`

**Qué hace:** corta un texto largo en fragmentos de hasta 300 palabras, respetando los párrafos, y anota en qué página del PDF empieza y termina cada uno.

**Por qué existe:** al modelo no se le puede pasar un PDF entero, y un vector de un texto muy largo no representa nada en particular. Los fragmentos son la unidad de búsqueda.

**Orden de lectura:** `PartirPaginas`, que es la que hace el trabajo, y sus ayudantes: `SepararParrafos`, `Cerrar`, `ArmarParte` y `Unir`. Al final, `Partir`: es `PartirPaginas` con una sola página, para cortar un texto suelto. Hoy la usan solo las pruebas.

Abajo de todo está `Palabra`, un `record` privado: una palabra con el número de su página. Lo que devuelve `PartirPaginas` es una lista de `Parte` (está en `Modelos.cs`): el texto, la página de inicio y la página de fin.

**Te pueden preguntar:**

- *¿Por qué 300 palabras?* Es un punto medio: entra una idea completa y el modelo de embeddings lo procesa bien. Está en la configuración.
- *¿Qué es el solapamiento?* Cada fragmento repite las últimas 50 palabras del anterior. Así una idea que quedó cortada entre dos fragmentos aparece entera en alguno.
- *¿Por qué por párrafos y no cada 300 palabras justas?* Para no partir una oración por la mitad ni dejar un título solo al final de un fragmento.
- *¿Por qué la página se anota en cada palabra y no en cada párrafo o en cada fragmento?* Porque un fragmento no respeta las páginas: junta párrafos de dos páginas, corta un párrafo largo por la mitad y arranca con 50 palabras del anterior. La única unidad que nunca se parte es la palabra. Si cada palabra sabe su página, la página de un fragmento sale sola, se lo arme como se lo arme: es la de su primera palabra y la de su última. No hay ninguna cuenta aparte que pueda quedar desfasada.
- *¿Qué es la página de inicio?* La página donde está la primera palabra del fragmento, contando las 50 repetidas del anterior. Es adonde abre el enlace de la fuente. Se cuenta desde 1, en el orden del PDF: es el número que usa el visor del navegador.
- *¿Y la de fin?* La página donde está la última palabra. Si es la misma que la de inicio, el fragmento no cruza de página.
- *¿Por qué el párrafo corto que queda pendiente sigue de una página a la otra?* Un párrafo de menos de 15 palabras, como un título, se pega al que le sigue. Un título al pie de una página es el título de lo que abre la página siguiente: si se cortara ahí, quedaría separado de su texto. Además, así el corte da lo mismo que si el PDF fuera un solo texto largo. El fragmento que arranca con ese título dice que empieza en la página del título, que es donde hay que abrir el PDF para leerlo entero.
- *¿Cambió la forma de cortar? ¿Hay que recargar todo?* El corte no cambió: los fragmentos tienen el mismo texto que da unir las páginas con una línea en blanco y cortar. Por eso `Chunker.Version` sigue en `parrafos-1` y la huella de los documentos es la misma. Se probó con 3.000 documentos armados al azar, comparando contra una copia del corte sin páginas: dan igual. Lo que sí hace falta en una base que ya tenía documentos es "Reprocesar todo", pero por otro motivo: para que los fragmentos guardados tengan su página.

### `RagBasico.Api\Ollama.cs`

**Qué hace:** habla con Ollama por HTTP. Manda el mismo JSON que se mandaría con Postman.

**Orden de lectura:** `EmbeddingsAsync` (texto → vector, lo usa el `Cargador`), `EmbeddingPreguntaAsync` (lo mismo para la pregunta, con una instrucción adelante), `ChatAsync` (la respuesta), `ListarModelosAsync` y `CalentarAsync`.

**Te pueden preguntar:**

- *¿Los documentos salen de la red?* No. Ollama corre en un servidor propio; nada va a internet.
- *¿Por qué la pregunta lleva una instrucción en inglés?* Así se entrenó ese modelo de embeddings: los documentos van sin nada, y las preguntas con la instrucción adelante. Mejora lo que encuentra.
- *¿Qué es `Dimension = 1024`?* Cuántos números tiene cada vector. Tiene que coincidir con la colección de Qdrant. Si se cambia el modelo por uno de otra dimensión, `GET /salud` lo delata.

### `RagBasico.Api\Qdrant.cs`

**Qué hace:** habla con Qdrant por HTTP: crea la colección, sube puntos, borra los de un documento y busca los más cercanos a un vector.

**Orden de lectura:** `Configurar`, `AsegurarColeccionAsync`, `SubirAsync`, `BorrarDeDocumentoAsync`, `BuscarAsync`.

**Te pueden preguntar:**

- *¿Qué es un punto?* Un vector con un id y un dato extra. El id es el `FragmentoId` de SQL y el dato extra es el `DocumentoId`.
- *¿Cómo se borran los vectores de un documento?* Con un filtro: "borrá los puntos cuyo `documentoId` es este".
- *¿Qué es HNSW?* El índice de Qdrant. En vez de comparar la pregunta contra todos los vectores, salta entre vecinos hasta llegar a los más cercanos. Por eso responde en milisegundos con un millón de vectores.
- *¿Por qué no se usa el paquete oficial de Qdrant?* Son seis pedidos HTTP. Escritos a mano se ve exactamente qué se manda, y es una dependencia menos.

## 4. El camino de una pregunta

### `RagBasico.Api\wwwroot\index.html`

**Qué hace:** manda la pregunta y el modo a `POST /preguntar` y muestra la respuesta, las fuentes y los tiempos. Las fuentes que el modelo citó con `[n]` salen en texto normal; las que se le pasaron pero no citó, atenuadas.

Cada fuente es un enlace que abre el PDF en otra pestaña, con el visor del navegador, en la página donde empieza el fragmento. Dice "archivo, página 3 (fragmento 41)". Si el fragmento cruza de página, "archivo, páginas 3 a 4 (fragmento 41)". Si no tiene página guardada, "archivo (fragmento 41)", y el enlace abre el PDF desde el principio.

**Orden de lectura del JavaScript:** la función del `submit` del formulario manda el pedido, `mostrarRespuesta` dibuja lo que vuelve y `mostrarError` muestra un error. Dentro de `mostrarRespuesta`, el bucle que recorre `datos.fuentes` arma los enlaces: la dirección es `/documentos/` + el número del documento + `/pdf`, y si hay página se le agrega `#page=` + la página.

**Te pueden preguntar:**

- *¿Qué es `#page=3`?* Una indicación para el visor de PDF del navegador: "andá a la página 3". Lo que va después del `#` no se manda al servidor. La API devuelve siempre el PDF entero y no se entera de la página.
- *¿Por qué abre en otra pestaña?* Para no perder la respuesta que está en pantalla: se puede mirar el PDF y volver a la respuesta sin preguntar de nuevo. `rel="noopener"` hace que la pestaña nueva no pueda tocar a la que la abrió.
- *¿Por qué el enlace se arma con `createElement` y `textContent`?* Por lo mismo que en la página de documentos: el nombre del archivo lo eligió quien lo subió, y así nunca se interpreta como HTML. La dirección se arma solo con números.
- *¿Qué pasa con un documento cargado antes de que se guardara la página?* La fuente sale sin página y el enlace abre el PDF desde el principio. Se arregla con "Reprocesar todo".

### `RagBasico.Api\Validador.cs`

**Qué hace:** una sola función, `RevisarPregunta`. Devuelve el motivo del rechazo, o vacío si la pregunta sirve: vacía, demasiado larga, con caracteres de control, o sin letras ni números.

**Te pueden preguntar:**

- *¿Por qué devuelve un texto y no verdadero o falso?* Porque el motivo es lo que le llega a quien preguntó.

### `RagBasico.Api\Redactor.cs`

**Qué hace:** arma la respuesta. Le pide los fragmentos al `Buscador`, se los pasa al modelo con las reglas, y registra la consulta.

**Orden de lectura:** la constante `Reglas` (el mensaje de sistema: es lo que hace que el modelo no invente), `ResponderAsync`, `ArmarMensaje`, `Neutralizar`, `EsSinRespuesta`, `RegistrarAsync`.

**Te pueden preguntar:**

- *¿Cómo se evita que el modelo invente?* Tres cosas. Las reglas le dicen que use solo los fragmentos y que cite cada oración. Si no alcanza, tiene que responder una frase fija, "No encontré esa información en los documentos". Y la página muestra las fuentes, así cualquiera puede comprobar.
- *¿Y si un PDF trae un texto que dice "ignorá las instrucciones"?* La regla 5 le dice al modelo que lo que está entre `<fragmentos>` son datos, no órdenes. Y `Neutralizar` cambia los `<` y `>` del texto, para que un PDF no pueda cerrar ese bloque por su cuenta.
- *¿Queda registro de lo que se preguntó?* Sí, en `rag.Consulta`: la pregunta, los fragmentos que se usaron, la respuesta y los tiempos.
- *¿El modelo sabe de qué página salió cada fragmento?* No. `ArmarMensaje` le pasa solo el número `[n]` y el texto. La página la pone la API en las fuentes, sacada de la tabla: el modelo no la puede inventar ni equivocar.

### `RagBasico.Api\Buscador.cs`

**Qué hace:** encuentra los fragmentos que mejor responden una pregunta. Tres modos.

**Orden de lectura:** `ElegirModo`, `BuscarAsync`, `PorPalabrasAsync`, `PorVectoresAsync`, `HibridaAsync`, `LeerFragmentosAsync`.

- **Por palabras:** `FREETEXTTABLE`, del Full-Text de SQL Server. Busca las palabras de la pregunta, con sus plurales y conjugaciones.
- **Por significado:** Ollama convierte la pregunta en un vector, Qdrant devuelve los números de los fragmentos más cercanos y SQL Server trae sus textos.
- **Híbrida**, la que se usa por defecto: las dos a la vez, mezcladas.

**Te pueden preguntar:**

- *¿Por qué dos búsquedas?* Se equivocan en cosas distintas. La de palabras encuentra un número de expediente o una sigla exacta, pero no entiende sinónimos. La de significado entiende "que me devuelvan la plata" como "reintegro", pero puede pasar por alto un código exacto. Juntas cubren los dos casos.
- *¿Qué es `Task.WhenAll`?* Arranca las dos búsquedas y espera a las dos. Se tarda lo que la más lenta, no la suma.
- *¿Para qué es la constante `Columnas`?* Las dos consultas, la de palabras y la que trae los textos por número, tienen que devolver las mismas columnas. Dapper arma el `record` `Fragmento` con su constructor: las columnas tienen que venir en el mismo orden y con los mismos nombres, y si no coinciden falla recién al ejecutar. Con la lista escrita una sola vez, no pueden quedar distintas.
- *¿Qué pasa si Qdrant no responde?* La búsqueda por palabras sigue andando. La de significado y la híbrida dan un error que dice "Qdrant no responde en tal dirección". No se sigue a medias sin avisar.

### `RagBasico.Api\Rrf.cs`

**Qué hace:** mezcla dos listas de resultados en una. Cada fragmento suma 1 / (60 + su puesto) por cada lista en la que aparece; quedan los de mayor puntaje.

**Te pueden preguntar:**

- *¿Por qué por puesto y no por puntaje?* Porque el puntaje del Full-Text y la distancia entre vectores miden cosas distintas, en escalas distintas. No se pueden sumar. El puesto sí: "segundo en una lista, cuarto en la otra".
- *¿De dónde sale el 60?* Es el valor del trabajo que propuso el método (Reciprocal Rank Fusion). Hace que estar en las dos listas pese más que estar primero en una sola.

## 5. Lo que sostiene todo

### `RagBasico.Api\Salud.cs`

**Qué hace:** revisa cada pieza y dice cómo está. Es lo que se ve en la franja de arriba de las páginas.

**Orden de lectura:** `RevisarAsync` y después un método por chequeo. Todos tienen la misma forma: un `try` que prueba la pieza y devuelve `ok` o `aviso`, y un `catch` que devuelve `error` con la dirección que probó y qué hacer.

**Te pueden preguntar:**

- *¿Qué pasa si falta un servidor?* La API arranca igual y la franja lo muestra en rojo, con el nombre del servidor, la dirección y qué hacer.
- *¿Cómo se sabe si Qdrant quedó desparejo con SQL?* Compara la cantidad de puntos con la cantidad de fragmentos. Si Qdrant tiene más puntos que fragmentos hay en la base, trae puntos de otra carga. Y sin pendientes, las dos cantidades tienen que ser iguales. Si no, avisa y dice "Reprocesar todo".
- *¿Qué pasa si la base quedó sin actualizar?* Si a `rag.Fragmento` le faltan las columnas de la página, el chequeo "SQL Server" da error y dice que se corra `sql\base.sql`. Se controla ahí porque sin esas columnas fallarían la carga y la búsqueda, con un error de SQL que no dice qué hacer.
- *¿Muestra la clave de SQL en el mensaje?* No. `Describir` saca de la cadena solo el servidor y la base.

### Los cuatro archivos de errores

Son cortos y se leen juntos, en este orden:

1. `ErrorDeServicio.cs`: un tipo de error propio, que lleva el nombre del servicio y el código HTTP.
2. `DetectorDeCaidas.cs`: va delante de los clientes de Ollama y de Qdrant, y lleva el reloj de cada pedido. Si no se puede conectar, o no contesta a tiempo, cambia el error genérico por un `ErrorDeServicio` que dice "Qdrant no responde en http://..." o "Qdrant no respondió a tiempo en http://...".
3. `Errores.cs`: `EsCaida` dice si un error es un servidor que no responde, o uno que sí respondió con un error.
4. `ManejadorDeErrores.cs`: atiende cualquier error que nadie atrapó y lo convierte en una respuesta con título, detalle y código: 503 si un servidor no responde, 502 si respondió con un error, 504 si no respondió a tiempo, 400 si el pedido está mal, 413 si la subida es muy grande.

**Te pueden preguntar:**

- *¿Qué ve el usuario si algo falla?* Nunca un "error 500" pelado. Ve qué falló y qué hacer.
- *¿Qué es ProblemDetails?* El formato estándar para devolver errores en una API: un JSON con `title`, `detail` y `status`.

### `RagBasico.Api\Modelos.cs`

Los tipos de datos que van y vienen: lo que se manda a `/preguntar`, lo que devuelve, un fragmento recién cortado (`Parte`), un fragmento encontrado (`Fragmento`), una fuente (`Fuente`), un PDF para abrir (`PdfGuardado`), un documento de la lista. Cada uno es un `record` de una línea. No tiene lógica.

### `RagBasico.Api\wwwroot\estado.js` y `estilos.css`

`estado.js` es una función, `mostrarEstado`: pide `GET /salud`, vuelve a pedirlo 15 segundos después de cada respuesta y dibuja un punto por chequeo. `estilos.css` tiene los colores y las formas que comparten las dos páginas, en claro y en oscuro.

### Los scripts

| Archivo | Qué hace |
|---|---|
| `iniciar.cmd` | El doble clic. Una línea: corre `scripts\iniciar.ps1` |
| `scripts\iniciar.ps1` | Lee la dirección de Qdrant. Si es de esta PC y no responde, lo arranca. Después corre la API |
| `scripts\iniciar-qdrant.ps1` | Arranca Qdrant escuchando solo en esta PC, con su carpeta de datos fija y sin telemetría |
| `scripts\crear-base.ps1` | Corre `sql\base.sql` en la base de `Conexiones:SqlTextos`, la de `appsettings.json`. Sirve para crearla y para actualizar una ya creada. `iniciar.cmd` no lo corre |
| `scripts\crear-origen-de-prueba.ps1` | Arma una base como la de origen, con los PDF de una carpeta, para probar la importación |
| `scripts\controlar-docs.ps1` | Controla que la documentación coincida con el código |

**Te pueden preguntar:**

- *¿Por qué el arranque está en un script y no en el programa?* Para que el programa sea el mismo en una PC y en varios servidores. Lo que cambia entre un lugar y otro queda afuera del código.

## 6. De una PC a los servidores de la oficina

No hay código nuevo para esto. Es la misma API, leyendo otras diez líneas.

1. En `Program.cs`, la línea `builder.Configuration.AddJsonFile("conexiones.json", optional: true, ...)` carga ese archivo si existe, y sus líneas pisan a las de `appsettings.json`.
2. `conexiones.oficina.ejemplo.json` es el modelo: las diez líneas de `Conexiones` con nombres en mayúsculas para reemplazar.
3. [`oficina.md`](oficina.md) es la guía: qué preparar en cada servidor, cómo comprobarlo desde la PC del RAG y qué hacer si algo falla.

**Te pueden preguntar:**

- *¿Qué cambia entre tu PC y la oficina?* Un archivo de configuración. `git diff fase-9-local fase-10-oficina --stat` muestra que no cambió ningún archivo de código.
- *¿Dónde quedan las claves?* En `conexiones.json`, en la PC donde corre la API. Git lo ignora. Con un usuario de Windows para SQL Server no hay ninguna clave escrita.
- *¿Por qué `scripts\iniciar.ps1` no arranca Qdrant en la oficina?* Porque mira la dirección: si no es de esta PC, no es suyo arrancarlo.
- *¿Qué es lo primero que falla al pasar a varios servidores?* La red: SQL Server Express trae TCP/IP apagado, Ollama escucha solo en su propia PC, y siempre hay un firewall. Por eso la guía comprueba cada puerto antes de abrir la página.

## 7. Las decisiones y sus porqués

| Decisión | En una frase |
|---|---|
| Cada dato en un solo lugar | No hay nada que mantener igual en dos lados, y mover una pieza es cambiar una línea |
| Los vectores, solo en Qdrant | Es para lo que está hecho; SQL Server queda con lo que hace bien: guardar y buscar por palabras |
| El PDF queda en SQL | Qdrant se puede rehacer entero, y la API no depende de una carpeta. De ahí mismo se lee para abrirlo desde una fuente |
| La página se anota al cortar, palabra por palabra | Es el único momento en que se sabe, y la palabra es lo único que nunca se parte |
| La página la pone la API, no el modelo | Sale de la tabla: no se puede inventar |
| Los importados no se copian | Ya están guardados en la otra base |
| El segundo plano | Una carga grande tarda horas y no puede depender de una página abierta |
| El estado en la tabla | Se puede apagar en cualquier momento y sigue después. No hay nada en memoria que se pierda |
| La API arranca siempre | Es más útil que diga qué falta a que no arranque |
| Sin Qdrant no hay híbrida | Mejor un error claro que una respuesta peor sin aviso |
| Sin librerías en las páginas | Son tres archivos que se leen enteros; no hay nada que instalar ni actualizar |
| Tres paquetes y nada más | Dapper (SQL), el cliente de SQL Server y PdfPig (leer PDF). Ollama y Qdrant se hablan por HTTP a mano |
| Sin interfaces para cambiar de motor | Cambiar de servidor es cambiar una línea. Cambiar de motor es otro proyecto, y prepararlo ahora serían archivos que no se usan |

## 8. Lo que no hace

Conviene decirlo antes de que lo pregunten:

- **No lee PDF escaneados.** Quedan como `sin texto`. Leerlos pide OCR, que es otro paso.
- **No tiene usuarios ni permisos.** La API escucha solo en la PC donde corre. Para abrirla a una red hay que agregar eso primero.
- **Procesa un documento por vez.** La velocidad la pone el modelo de embeddings: entre 6 y 13 fragmentos por segundo en la PC de prueba.
- **No se probó con decenas de miles de documentos.** Se probó con 13 PDF. El diseño lo contempla (anota de a 1.000, tiene índices por estado, tope de intentos, borra de a 20.000), pero la primera carga grande es la prueba real.
- **No mide la calidad de las respuestas con números.** Hasta la fase 8 había un examen automático; se sacó porque no hay preguntas escritas para los documentos reales. Sigue en la branch `fase-8-validaciones`.
- **No responde lo que no está en los documentos.** Eso es a propósito.

Y los límites de las fuentes con página:

- **La página es la física, no la impresa.** Es la que muestra el visor, contando desde la primera hoja del PDF. Si el documento tiene carátula o índice, no coincide con el número impreso en la hoja.
- **La mayoría de las fuentes dice "páginas N a M".** Un fragmento tiene 300 palabras, y eso casi siempre cruza de una página a la otra.
- **El enlace abre donde empieza el fragmento.** A veces es la página anterior a la de la parte principal, porque el fragmento arranca con 50 palabras repetidas del anterior.
- **Si el navegador está configurado para descargar los PDF, `#page` no hace nada.** El archivo se baja y se abre desde el principio.
- **Un error al abrir se ve como JSON.** Si el documento se borró o la base de origen está apagada, la pestaña nueva muestra el error tal como lo devuelve la API, sin el formato de las páginas.
- **Un importado puede tardar en abrir.** Con la base de origen lenta, hasta 300 segundos.
- **Una respuesta vieja puede abrir un PDF nuevo.** Si un documento se reemplaza mientras hay una respuesta en pantalla, sus enlaces abren el PDF nuevo en la página del viejo.
- **El PDF se lee entero de SQL en cada clic.** No se guarda una copia, ni en la API ni en el navegador.
- **Ponerle la página a lo ya cargado lleva tiempo.** "Reprocesar todo" rehace todos los vectores: en la PC de prueba, 19.191 fragmentos tardaron 49 minutos, y mientras dura no se encuentran los documentos que faltan.

## 9. Cómo comprobar que entendiste

Con el proyecto andando, probá predecir qué va a pasar antes de hacerlo:

1. Soltá un PDF y mirá `rag.Documento` en SSMS: ¿en qué estado está? ¿Cuándo cambia?
2. Soltá el mismo PDF de nuevo: ¿qué dice la página? ¿Por qué?
3. Cerrá la ventana de Qdrant y preguntá en cada modo: ¿cuál responde y cuál no?
4. Con documentos pendientes, frená la API con Ctrl+C y volvé a arrancarla: ¿qué pasa con el que estaba a medias?
5. Cambiá `Conexiones:ColeccionQdrant` por otro nombre en `conexiones.json` y arrancá: ¿qué muestra la franja? ¿Cómo se arregla?
6. Buscá en `rag.Consulta` tu última pregunta: ¿qué fragmentos se le pasaron al modelo?
7. Hacé una pregunta y apretá una fuente que diga "páginas N a M": ¿en qué página abre? Buscá ese fragmento en `rag.Fragmento`: ¿qué tienen `Pagina` y `PaginaFin`?

Si podés explicar los siete, podés explicar el proyecto.
