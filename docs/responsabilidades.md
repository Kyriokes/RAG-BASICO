# Responsabilidades

Qué hace cada archivo, clase, función y endpoint de esta versión, y quién lo usa. Esta es la versión de `fase-5-vectores`.

Cómo leerlo:

- Hay una sección `##` por archivo, en el orden en que los lista git.
- Adentro de cada archivo de código hay una entrada `###` por clase, por función y por endpoint.
- **Desde** dice en qué fase apareció. **Cambió en** dice en qué fases cambió después.
- `scripts\controlar-docs.ps1` controla que este documento coincida con el código.

## Árbol

```text
RAG-BASICO\
├─ .editorconfig                   formato del código y de los archivos
├─ .gitattributes                  finales de línea
├─ .gitignore                      qué no se sube a git
├─ README.md                       portada: qué es y cómo correr esta fase
├─ RagBasico.Api\                  la API web
│  ├─ Buscador.cs                  búsqueda por palabras y por vectores
│  ├─ Chunker.cs                   corta el texto en fragmentos por párrafos
│  ├─ Evaluador.cs                 corre el examen
│  ├─ Indexador.cs                 pone la base al día con la carpeta de PDF
│  ├─ Modelos.cs                   records de pedidos y respuestas
│  ├─ Ollama.cs                    cliente HTTP de Ollama
│  ├─ Planificador.cs              decide qué PDF cargar, dejar o borrar
│  ├─ Program.cs                   arranque, servicios y endpoints
│  ├─ Properties\
│  │  └─ launchSettings.json       dirección, puerto y forma de arrancar
│  ├─ RagBasico.Api.csproj         el proyecto y sus paquetes
│  ├─ RagBasico.Api.http           pedidos de prueba
│  ├─ Redactor.cs                  arma la respuesta con Qwen y la registra
│  ├─ Vectorizador.cs              calcula el vector de cada fragmento
│  └─ appsettings.json             configuración
├─ RagBasico.slnx                  la solución que abre Visual Studio
├─ docs\                           documentación
│  ├─ arquitectura.md              piezas, recorridos, tablas y configuración
│  ├─ erratas.md                   bugs encontrados después de cerrar una fase
│  ├─ fases\
│  │  ├─ fase-0-base.md            el esqueleto
│  │  ├─ fase-1-ollama.md          C# y HTTP contra Qwen
│  │  ├─ fase-2-rag-minimo.md      el primer RAG
│  │  ├─ fase-3-medicion.md        el examen y los tiempos
│  │  ├─ fase-4-carga.md           la carga incremental y el corte por párrafos
│  │  └─ fase-5-vectores.md        la búsqueda por significado
│  ├─ responsabilidades.md         este documento
│  └─ worktrees.md                 una carpeta por fase
├─ documentos.csv                  manifiesto de los PDF del examen
├─ examen.csv                      el examen
├─ examen.ejemplo.csv              el formato del examen
├─ global.json                     versión del SDK de .NET
├─ scripts\                        herramientas del repo
│  ├─ controlar-docs.ps1           controla la documentación
│  ├─ crear-base.ps1               crea la base de la fase
│  ├─ crear-manifiesto.ps1         arma o verifica el manifiesto
│  └─ crear-worktrees.ps1          crea una carpeta por fase
└─ sql\                            scripts de la base
   ├─ 01_base.sql                  tablas y Full-Text
   ├─ 02_consultas.sql             registro de preguntas
   ├─ 03_huella.sql                huella de carga de cada documento
   ├─ 04_vectores.sql              vector de cada fragmento
   └─ consultas-utiles.sql         consultas para mirar la base
```

## .editorconfig

- **Responsabilidad:** fija el formato de los archivos y el estilo del código C#. Visual Studio lo lee solo: lo aplica al guardar y lo usa para sugerir cambios. VS Code lo lee con la extensión "EditorConfig for VS Code".
- **Contiene:** el archivo que genera `dotnet new editorconfig`, con estos ajustes:
  - **En todos los archivos:** `charset = utf-8`, `end_of_line = crlf` e `insert_final_newline = true`.
  - **En los .ps1:** `charset = utf-8-bom`, porque Windows PowerShell 5.1 lee mal las tildes de un script sin BOM.
  - **En los .cs:** `dotnet_style_require_accessibility_modifiers = always:warning`, para que el modificador de acceso esté siempre escrito, como necesita el control de documentación.
  - **C# clásico:** `csharp_style_prefer_primary_constructors = false`, `dotnet_style_prefer_collection_expression = never` y `dotnet_diagnostic.SYSLIB1045.severity = none`. Sin esto, Visual Studio sugiere constructores primarios, `[]` y `[GeneratedRegex]`, y un "arreglar todo" cambiaría el estilo del proyecto.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## .gitattributes

- **Responsabilidad:** le dice a git cómo tratar los finales de línea. En tu carpeta los archivos quedan con CRLF, como usa Windows; guardados en git, con LF. Así un editor que guarda distinto no hace aparecer un archivo entero como cambiado en el diff entre dos fases.
- **Contiene:** una sola regla: `* text=auto eol=crlf`.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## .gitignore

- **Responsabilidad:** lista lo que git no tiene que subir: las carpetas de compilación (`bin` y `obj`), los archivos de Visual Studio y de VS Code, y otros temporales.
- **Contiene:** la plantilla oficial de .NET, generada con `dotnet new gitignore`, sin agregados.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## README.md

- **Responsabilidad:** la portada del repo: qué es, qué hace falta para correr esta fase, cómo correrla, cómo recorrer las fases y la tabla de fases.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores.

## RagBasico.Api\Buscador.cs

- **Responsabilidad:** encontrar los fragmentos que mejor responden una pregunta, por palabras o por significado.
- **Contiene:** la clase `Buscador`, con la lista pública `Modos` (`palabras` y `vectores`) y la constante `ModoPorDefecto` (`palabras`).
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-5-vectores.

### Clase Buscador

- **Responsabilidad:** juntar en un solo lugar las formas de buscar: por palabras, con el Full-Text, y por vectores, con `VECTOR_DISTANCE`.
- **Recibe en el constructor:** el cliente de `Ollama`, para calcular el vector de la pregunta, y la configuración, de donde lee `ConnectionStrings:Rag`.
- **La registra:** Program.cs, con `AddScoped`: un objeto nuevo por pedido HTTP.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-5-vectores (recibe el cliente de Ollama y busca por vectores).

### Buscador.ElegirModo(pedido)

- **Qué hace:** controla el modo que llega en un pedido. Sin modo, devuelve el modo por defecto. Con un modo que existe, lo devuelve en minúsculas y sin espacios. Con un modo que no existe devuelve `null`, para que quien llama dé un error en lugar de buscar con otro modo sin avisar.
- **Recibe:** el modo del pedido, que puede faltar.
- **Devuelve:** el modo, o `null` si no existe.
- **Efectos:** ninguno.
- **La llaman:** los endpoints `GET /buscar` y `POST /preguntar`.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Buscador.BuscarAsync(pregunta, modo, topK, ct)

- **Qué hace:** busca con el modo pedido: llama a Buscador.PorPalabrasAsync o a Buscador.PorVectoresAsync. Con un modo que no existe lanza un `ArgumentException`, aunque los endpoints ya lo controlaron antes.
- **Recibe:** la pregunta, el modo, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** la lista de `Fragmento`, del más relevante al menos relevante.
- **Efectos:** los de la búsqueda que usa.
- **La llaman:** el endpoint `GET /buscar`, Redactor.ResponderAsync y Evaluador.CorrerAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Buscador.PorPalabrasAsync(pregunta, topK, ct)

- **Qué hace:** busca en el Full-Text de SQL Server los fragmentos que mejor coinciden con las palabras de la pregunta. `FREETEXTTABLE` corta la pregunta en palabras, busca también sus plurales y conjugaciones, y le pone un puntaje (`RANK`) a cada fragmento. Se piden los 50 mejores y se devuelven los `topK` primeros.
- **Recibe:** la pregunta, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** una lista de `Fragmento` (número, archivo y texto), del más relevante al menos relevante. Vacía si la pregunta está vacía o si ninguna palabra coincide.
- **Efectos:** lee `rag.Fragmento` y `rag.Documento`. No escribe nada.
- **La llaman:** Buscador.BuscarAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Buscador.PorVectoresAsync(pregunta, topK, ct)

- **Qué hace:** busca los fragmentos más parecidos en significado a la pregunta. Le pide a Ollama el vector de la pregunta, y SQL Server ordena todos los fragmentos que tienen vector por `VECTOR_DISTANCE('cosine', ...)`: 0 es el mismo sentido y 2 el opuesto. Es una búsqueda exacta: recorre todos los vectores, así que tarda más cuantos más fragmentos haya. A diferencia de la búsqueda por palabras, casi nunca vuelve vacía: siempre hay un fragmento más cercano que otro.
- **Recibe:** la pregunta, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** la lista de `Fragmento`, del más cercano al más lejano. Vacía si la pregunta está vacía o si ningún fragmento tiene vector.
- **Efectos:** un pedido a Ollama; lee `rag.Fragmento` y `rag.Documento`. No escribe nada.
- **La llaman:** Buscador.BuscarAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

## RagBasico.Api\Chunker.cs

- **Responsabilidad:** cortar el texto de un documento en fragmentos del tamaño justo para buscar y para pasarle al modelo, respetando los párrafos.
- **Contiene:** la clase `Chunker`, con la constante pública `Version`, que entra en la huella de carga, y tres valores privados: `PalabrasDeParrafoCorto` (15), `Separadores` (los caracteres que separan palabras) y `LineaEnBlanco`, la expresión regular que reconoce dónde empieza un párrafo.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-4-carga.

### Clase Chunker

- **Responsabilidad:** las funciones que cortan y cuentan palabras. Es estática: no guarda nada, solo transforma texto.
- **Recibe en el constructor:** nada; una clase estática no tiene constructor.
- **La registra:** nadie. Se usa directo: `Chunker.Partir(...)`.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-4-carga (corta por párrafos y tiene la constante `Version`).

### Chunker.Partir(texto, palabras, solapamiento)

- **Qué hace:** corta el texto en fragmentos de hasta `palabras` palabras, juntando párrafos enteros mientras entren.
  - Un párrafo que no entra cierra el fragmento antes de empezar, si el fragmento ya tiene la mitad del tamaño. Si tiene menos, el principio del párrafo lo completa: así no quedan fragmentos chicos.
  - Un párrafo más largo que el tamaño se corta en ventanas, como hacía la fase 2 con todo el texto.
  - Cada fragmento repite las últimas `solapamiento` palabras del anterior. Si al final quedan menos palabras nuevas que repetidas, se suman al último fragmento en lugar de abrir uno que sería casi una copia del anterior.
  - Si el solapamiento no es menor que la mitad del tamaño, lo ignora.
- **Recibe:** el texto, el tamaño máximo de cada fragmento en palabras y cuántas palabras repetir.
- **Devuelve:** la lista de fragmentos, en orden. Vacía si el texto no tiene palabras. El último puede pasar el tamaño por menos palabras que el solapamiento.
- **Efectos:** ninguno.
- **La llaman:** Indexador.CargarArchivoAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-4-carga (por párrafos, sin títulos sueltos ni fragmentos casi repetidos).

### Chunker.ContarPalabras(texto)

- **Qué hace:** cuenta las palabras de un texto, separando por espacios, saltos de línea y tabulaciones.
- **Recibe:** el texto.
- **Devuelve:** la cantidad de palabras.
- **Efectos:** ninguno.
- **La llaman:** Indexador.EsEscaneado e Indexador.RevisarTexto.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Chunker.SepararParrafos(texto)

- **Qué hace:** separa el texto en párrafos por las líneas en blanco, que es como PdfPig marca un espacio grande entre dos renglones, y parte cada párrafo en palabras. Un párrafo de menos de 15 palabras, como un título o un número de página, se pega al que le sigue: así nunca queda solo en un fragmento ni al final del fragmento anterior, separado de su texto.
- **Recibe:** el texto.
- **Devuelve:** la lista de párrafos, cada uno como un arreglo de palabras. Vacía si el texto no tiene palabras.
- **Efectos:** ninguno.
- **La llaman:** Chunker.Partir.
- **Desde:** fase-4-carga.
- **Cambió en:** nada todavía.

### Chunker.Cerrar(fragmentos, actual, solapamiento)

- **Qué hace:** cierra el fragmento que se está armando: lo agrega a la lista y deja en `actual` solo sus últimas `solapamiento` palabras, que abren el fragmento siguiente.
- **Recibe:** la lista de fragmentos, las palabras del fragmento que se está armando y cuántas repetir.
- **Devuelve:** nada. Cambia las dos listas que recibe.
- **Efectos:** ninguno fuera de esas dos listas.
- **La llaman:** Chunker.Partir.
- **Desde:** fase-4-carga.
- **Cambió en:** nada todavía.

## RagBasico.Api\Evaluador.cs

- **Responsabilidad:** correr el examen y medir qué tan bien busca el sistema, con números y no con "a mí me anduvo".
- **Desde:** fase-3-medicion.
- **Cambió en:** fase-5-vectores.

### Clase Evaluador

- **Responsabilidad:** leer el examen, pasar cada pregunta por el buscador y las preguntas sin respuesta por el redactor, en cada modo de búsqueda, y resumir los resultados.
- **Recibe en el constructor:** el `Buscador`, el `Redactor`, la configuración (de donde lee `ConnectionStrings:Rag` y `Rag:ArchivoExamen`) y el entorno, para resolver la ruta del examen contra la carpeta del proyecto.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-3-medicion.
- **Cambió en:** fase-5-vectores (corre todos los modos de `Buscador.Modos`).

### Evaluador.CorrerAsync(ct)

- **Qué hace:** corre el examen completo en cada modo de `Buscador.Modos`.
  1. Avisa si el examen nombra un documento que no está cargado, espera hasta un minuto a que el Full-Text termine de indexar y avisa si hay fragmentos sin vector.
  2. En cada modo, pasa cada pregunta con respuesta por el buscador y anota en qué puesto apareció un documento correcto.
  3. En cada modo, pasa cada pregunta sin respuesta por el redactor y cuenta cuántas veces dijo la frase fija. Se mide por modo porque por vectores la búsqueda casi nunca vuelve vacía, y el "no sé" lo decide Qwen.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `InformeEvaluacion`: una fila por modo, el detalle por pregunta, la lista de fallas y los avisos.
- **Efectos:** lee SQL Server y llama a Ollama: el vector de cada pregunta en el modo `vectores`, y Qwen para las preguntas sin respuesta, que quedan registradas en `rag.Consulta`.
- **La llaman:** el endpoint `POST /evaluar`.
- **Desde:** fase-3-medicion.
- **Cambió en:** fase-5-vectores (todos los modos, y las preguntas sin respuesta en cada uno).

### Evaluador.Puntuar(modo, detalle)

- **Qué hace:** resume un modo: cuántas preguntas tuvieron un documento correcto en el primer lugar, cuántas entre los cinco primeros, y el tiempo promedio. Las preguntas sin respuesta quedan en 0: las completa Evaluador.CorrerAsync con `with`, que copia el record cambiando solo esos dos campos.
- **Recibe:** el nombre del modo y el detalle de cada pregunta, con su puesto (0 si no apareció).
- **Devuelve:** un `ResultadoModo`.
- **Efectos:** ninguno.
- **La llaman:** Evaluador.CorrerAsync.
- **Desde:** fase-3-medicion.
- **Cambió en:** fase-5-vectores (devuelve también las preguntas sin respuesta, en 0).

### Evaluador.LeerExamen(ruta)

- **Qué hace:** lee el examen, una pregunta por línea con la forma `pregunta;documento`. Acepta UTF-8 y, si el archivo no lo es, la codificación de Windows que usa Excel. Saltea el encabezado y las líneas vacías. La última columna son los documentos, separados con `|`; el resto es la pregunta, así un `;` adentro de la pregunta no la rompe. `ninguno` marca una pregunta sin respuesta.
- **Recibe:** la ruta del examen.
- **Devuelve:** la lista de `PreguntaExamen`.
- **Efectos:** lee el archivo.
- **La llaman:** Evaluador.CorrerAsync.
- **Desde:** fase-3-medicion.
- **Cambió en:** nada todavía.

### Evaluador.PartirLinea(linea)

- **Qué hace:** parte una línea de CSV por `;`, respetando los campos entre comillas y las comillas dobladas adentro (`""`).
- **Recibe:** la línea.
- **Devuelve:** la lista de columnas.
- **Efectos:** ninguno.
- **La llaman:** Evaluador.LeerExamen.
- **Desde:** fase-3-medicion.
- **Cambió en:** nada todavía.

### Evaluador.LeerDocumentosCargadosAsync(ct)

- **Qué hace:** lee los nombres de los PDF cargados, para avisar si el examen nombra uno que no está.
- **Recibe:** el token de cancelación.
- **Devuelve:** el conjunto de nombres, sin distinguir mayúsculas.
- **Efectos:** lee `rag.Documento`.
- **La llaman:** Evaluador.CorrerAsync.
- **Desde:** fase-3-medicion.
- **Cambió en:** nada todavía.

### Evaluador.ContarSinVectorAsync(ct)

- **Qué hace:** cuenta los fragmentos que todavía no tienen vector, para avisar antes de medir: con fragmentos sin vector, la búsqueda por vectores sale baja.
- **Recibe:** el token de cancelación.
- **Devuelve:** la cantidad.
- **Efectos:** lee `rag.Fragmento`.
- **La llaman:** Evaluador.CorrerAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Evaluador.EsperarFullTextAsync(ct)

- **Qué hace:** espera hasta un minuto, preguntando cada 2 segundos, a que el Full-Text no tenga cambios pendientes. Medir justo después de cargar daría números bajos.
- **Recibe:** el token de cancelación.
- **Devuelve:** cuántos cambios quedaron pendientes: 0 si terminó.
- **Efectos:** lee `OBJECTPROPERTYEX(..., 'TableFulltextPendingChanges')`.
- **La llaman:** Evaluador.CorrerAsync.
- **Desde:** fase-3-medicion.
- **Cambió en:** nada todavía.

## RagBasico.Api\Indexador.cs

- **Responsabilidad:** mantener la base igual a la carpeta de PDF: cargar los nuevos y los cambiados, no tocar los que siguen igual y borrar los que ya no están.
- **Contiene:** la clase `Indexador` y su constante privada `PalabrasPorPaginaMinimas` (40): el promedio de palabras por página por debajo del cual un PDF se toma como escaneado.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-4-carga, fase-5-vectores.

### Clase Indexador

- **Responsabilidad:** el camino de un PDF hasta la base, y decidir cuáles hacen ese camino.
- **Recibe en el constructor:** la configuración, de donde lee `ConnectionStrings:Rag`, `Rag:CarpetaDocumentos`, `Rag:PalabrasPorFragmento` y `Rag:Solapamiento`.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-4-carga (carga incremental).

### Indexador.IndexarCarpetaAsync(ct)

- **Qué hace:** pone la base al día con la carpeta.
  1. Si la carpeta no existe, no toca la base y lo informa.
  2. Calcula la huella de cada PDF, lee las huellas de la base y le pide el plan a Planificador.Armar.
  3. Borra los documentos que ya no están en la carpeta. Si la carpeta no tiene ningún PDF, no borra nada y lo avisa.
  4. Carga los nuevos y los cambiados. Los que siguen igual no se vuelven a leer.
  5. Si un PDF falla, lo anota y sigue con el siguiente.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `InformeIndexado`: cuántos PDF se cargaron, cuántos quedaron igual y cuántos se borraron, cuántos fragmentos se guardaron, el tiempo como horas:minutos:segundos y el detalle. El detalle tiene una línea por cada PDF cargado, borrado, sin texto o con error; si no hubo nada que hacer, una sola línea `SIN CAMBIOS`.
- **Efectos:** lee los PDF de la carpeta y `rag.Documento`; borra filas de `rag.Documento` (y en cascada de `rag.Fragmento`) e inserta las nuevas.
- **La llaman:** el endpoint `POST /indexar`.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-4-carga (carga solo lo nuevo o cambiado y borra lo que ya no está, en lugar de recargar todo).

### Indexador.CalcularHuella(ruta, parametros)

- **Qué hace:** calcula la huella de carga de un PDF: el sha256 de sus bytes seguidos de los parámetros de corte. Lee el archivo de a bloques de 80 KB, así un PDF grande no se carga entero en memoria. No es el sha256 del manifiesto (`documentos.csv`), que mira solo el archivo: esta huella también cambia si cambia la forma de cortar.
- **Recibe:** la ruta del PDF y el texto con los parámetros de corte.
- **Devuelve:** los 32 bytes de la huella.
- **Efectos:** lee el archivo.
- **La llaman:** Indexador.LeerCarpeta.
- **Desde:** fase-4-carga.
- **Cambió en:** nada todavía.

### Indexador.EsEscaneado(paginas)

- **Qué hace:** dice si un PDF parece escaneado: menos de 40 palabras por página en promedio. Un escaneo con un sello de firma en cada página tiene algunas palabras por página, pero no tantas; por eso se mira el promedio y no un total fijo.
- **Recibe:** el texto de cada página.
- **Devuelve:** `true` si parece escaneado, o si no tiene páginas.
- **Efectos:** ninguno.
- **La llaman:** Indexador.CargarArchivoAsync.
- **Desde:** fase-4-carga.
- **Cambió en:** nada todavía.

### Indexador.RevisarTexto(paginas)

- **Qué hace:** busca señales de que el texto salió mal del PDF, en este orden:
  1. Menos del 80 % de los caracteres son letras o números: la fuente del PDF no se pudo leer.
  2. Más de 12 caracteres por palabra en promedio: el PDF no marca los espacios y las palabras salen pegadas.
  3. Una cuarta parte de las páginas, o más, con menos de 20 palabras: tiene partes escaneadas, como un anexo.
- **Recibe:** el texto de cada página.
- **Devuelve:** el motivo de la duda, o vacío si el texto parece bien.
- **Efectos:** ninguno.
- **La llaman:** Indexador.CargarArchivoAsync.
- **Desde:** fase-4-carga.
- **Cambió en:** nada todavía.

### Indexador.LeerCarpeta(detalle)

- **Qué hace:** lista los PDF de la carpeta, en orden alfabético, con su huella. Si un PDF no se puede leer, porque está bloqueado o no hay permiso, lo anota en el detalle y lo lista sin huella: así no se carga, pero como está en la carpeta tampoco se borra, y queda lo que había en la base.
- **Recibe:** la lista del detalle, donde anota los errores.
- **Devuelve:** la lista de `ArchivoEnCarpeta`.
- **Efectos:** lee los PDF de la carpeta.
- **La llaman:** Indexador.IndexarCarpetaAsync.
- **Desde:** fase-4-carga.
- **Cambió en:** nada todavía.

### Indexador.ParametrosDeCorte()

- **Qué hace:** arma el texto con todo lo que cambia el resultado del corte: la versión del `Chunker`, el tamaño de fragmento, el solapamiento y la versión de PdfPig, porque otra versión puede sacar otro texto del mismo PDF. Por ejemplo: `corte=parrafos-1;palabras=300;solapamiento=50;pdfpig=0.1.16.0`.
- **Recibe:** nada; usa el tamaño y el solapamiento de la configuración.
- **Devuelve:** el texto.
- **Efectos:** ninguno.
- **La llaman:** Indexador.LeerCarpeta.
- **Desde:** fase-4-carga.
- **Cambió en:** nada todavía.

### Indexador.CargarArchivoAsync(conexion, archivo, accion, detalle, ct)

- **Qué hace:** carga un PDF nuevo o cambiado: lo lee, se fija si es un escaneo, lo corta, revisa si el texto es dudoso y lo guarda. Anota en el detalle una línea con la acción (`NUEVO` o `CAMBIÓ`), la cantidad de fragmentos y, si corresponde, `TEXTO DUDOSO` con el motivo, para revisarlo en SSMS. Si es un escaneo, igual borra la versión anterior y lo anota como `SIN TEXTO`. Si falla, lo anota como `ERROR` y, si el PDF había cambiado, queda la versión anterior.
- **Recibe:** la conexión abierta, el PDF, la acción para el detalle, la lista del detalle y el token de cancelación.
- **Devuelve:** cuántos fragmentos guardó: 0 si no tenía texto o si falló.
- **Efectos:** lee el PDF; borra y escribe en `rag.Documento` y `rag.Fragmento`, por medio de Indexador.GuardarAsync.
- **La llaman:** Indexador.IndexarCarpetaAsync.
- **Desde:** fase-4-carga. Reemplaza a `IndexarArchivoAsync` de las fases 2 y 3.
- **Cambió en:** nada todavía.

### Indexador.GuardarAsync(conexion, archivo, partes, ct)

- **Qué hace:** en una sola transacción, borra la versión anterior del PDF, si había, y guarda la nueva: una fila en `rag.Documento` con su huella y una por fragmento en `rag.Fragmento`, cada uno empezando con `Documento: <nombre>`. Si algo falla antes de confirmar, la transacción se descarta y queda la versión anterior. Sin fragmentos, solo borra.
- **Recibe:** la conexión abierta, el PDF (nombre y huella), los fragmentos y el token de cancelación.
- **Devuelve:** nada.
- **Efectos:** borra la fila del PDF en `rag.Documento`, y en cascada sus fragmentos, e inserta las nuevas.
- **La llaman:** Indexador.CargarArchivoAsync.
- **Desde:** fase-4-carga.
- **Cambió en:** nada todavía.

### Indexador.LeerPdf(ruta)

- **Qué hace:** saca el texto de un PDF con PdfPig, página por página, en el orden en que se lee. PdfPig marca con una línea en blanco el espacio grande entre dos renglones, que es lo que el `Chunker` toma como cambio de párrafo.
- **Recibe:** la ruta del PDF.
- **Devuelve:** una lista con el texto de cada página. Una página escaneada viene vacía.
- **Efectos:** lee el archivo.
- **La llaman:** Indexador.CargarArchivoAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-4-carga (devuelve cada página por separado, para contar las palabras por página).

### Indexador.FormatearTiempo(tiempo)

- **Qué hace:** muestra un tiempo como horas:minutos:segundos, también cuando pasa de 24 horas.
- **Recibe:** el tiempo.
- **Devuelve:** el texto, por ejemplo `00:03:27`.
- **Efectos:** ninguno.
- **La llaman:** Indexador.IndexarCarpetaAsync y Vectorizador.CalcularPendientesAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-5-vectores (pasa a ser pública, para que la use el Vectorizador).

## RagBasico.Api\Modelos.cs

- **Responsabilidad:** los records que viajan entre las piezas: lo que llega en un pedido y lo que devuelve cada servicio. Un record es una clase chica de solo datos, que se compara por su contenido.
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores.

| Record | Campo | Tipo | Para qué |
|---|---|---|---|
| `PedidoPregunta` | `Pregunta` | `string` | La pregunta que llega en el cuerpo de `POST /preguntar` |
| `PedidoPregunta` | `Modo` | `string?` | El modo de búsqueda, `palabras` o `vectores`. Si falta, `palabras` |
| `RespuestaModelo` | `Texto` | `string` | Lo que escribió el modelo |
| `RespuestaModelo` | `MsCarga`, `MsLectura`, `MsEscritura` | `int` | Lo que tardó Ollama en cargar el modelo (0 si ya estaba), en leer el pedido y en escribir la respuesta |
| `RespuestaModelo` | `TokensPregunta`, `TokensRespuesta` | `int` | Cuántos tokens leyó y cuántos escribió |
| `Fragmento` | `FragmentoId` | `int` | El número del fragmento en `rag.Fragmento` |
| `Fragmento` | `Archivo` | `string` | El PDF de donde salió |
| `Fragmento` | `Texto` | `string` | El texto, que empieza con `Documento: <nombre>` |
| `InformeIndexado` | `Cargados` | `int` | Cuántos PDF nuevos o cambiados se cargaron |
| `InformeIndexado` | `SinCambios`, `Borrados` | `int` | Cuántos PDF quedaron igual, y cuántos documentos se borraron porque ya no están en la carpeta |
| `InformeIndexado` | `Fragmentos` | `int` | Cuántos fragmentos se guardaron en esta carga |
| `InformeIndexado` | `Tiempo` | `string` | Lo que tardó, como horas:minutos:segundos |
| `InformeIndexado` | `Detalle` | `List<string>` | Una línea por PDF cargado (`NUEVO` o `CAMBIÓ`, con `TEXTO DUDOSO` si corresponde), borrado (`BORRADO`), sin texto (`SIN TEXTO`) o con error (`ERROR`); o una sola línea `SIN CAMBIOS` |
| `ArchivoEnCarpeta` | `Archivo`, `Ruta` | `string` | El nombre del PDF y su ruta completa |
| `ArchivoEnCarpeta` | `Huella` | `byte[]?` | La huella de carga; `null` si el archivo no se pudo leer |
| `DocumentoCargado` | `DocumentoId` | `int` | El número del documento en `rag.Documento` |
| `DocumentoCargado` | `Archivo` | `string` | El nombre del PDF |
| `DocumentoCargado` | `Huella` | `byte[]?` | La huella con que se cargó; `null` si se cargó antes de `sql\03_huella.sql` |
| `PlanDeCarga` | `Nuevos`, `Cambiados`, `SinCambios` | `List<ArchivoEnCarpeta>` | Los PDF que hay que cargar porque no estaban o porque cambiaron, y los que quedan igual |
| `PlanDeCarga` | `Borrar` | `List<DocumentoCargado>` | Los documentos de la base que ya no están en la carpeta. Vacía si la carpeta no tiene ningún PDF |
| `InformeVectores` | `Calculados`, `Fallados` | `int` | Cuántos vectores se calcularon y guardaron, y cuántos fragmentos fallaron |
| `InformeVectores` | `Pendientes` | `int` | Cuántos fragmentos quedan sin vector al terminar |
| `InformeVectores` | `FragmentosPorSegundo` | `double` | La velocidad, que es el dato para dimensionar el servidor |
| `InformeVectores` | `Tiempo` | `string` | Lo que tardó, como horas:minutos:segundos |
| `InformeVectores` | `Detalle` | `List<string>` | Una línea por fragmento que falló, o una sola línea `SIN CAMBIOS` |
| `Fuente` | `Numero` | `int` | El número con que la cita el modelo: `[1]`, `[2]`... |
| `Fuente` | `Archivo`, `FragmentoId` | `string`, `int` | El PDF y el fragmento exacto |
| `RespuestaRag` | `Texto` | `string` | La respuesta, o la frase de "no encontré" |
| `RespuestaRag` | `Fuentes` | `List<Fuente>` | Las fuentes, en el orden en que se le pasaron al modelo. Vacía si no encontró |
| `RespuestaRag` | `MsBusqueda`, `MsModelo` | `int` | Lo que tardó la búsqueda y lo que tardó el modelo, medido desde la API |
| `RespuestaRag` | `MsCarga`, `MsLectura`, `MsEscritura` | `int` | Los tiempos de Ollama |
| `PreguntaExamen` | `Pregunta` | `string` | La pregunta del examen |
| `PreguntaExamen` | `Documentos` | `List<string>` | Los PDF que la responden. Vacía si es una pregunta sin respuesta |
| `DetallePregunta` | `Pregunta`, `Modo` | `string` | La pregunta y el modo de búsqueda |
| `DetallePregunta` | `Puesto` | `int` | En qué puesto apareció un documento correcto: 1 a 5, o 0 si no apareció |
| `DetallePregunta` | `Ms` | `int` | Lo que tardó la búsqueda |
| `ResultadoModo` | `Modo` | `string` | El modo de búsqueda |
| `ResultadoModo` | `Preguntas`, `PrimerLugar`, `EntreLosCinco` | `int` | Cuántas preguntas, cuántas en el primer lugar y cuántas entre los cinco primeros |
| `ResultadoModo` | `MsPromedio` | `int` | El tiempo promedio de búsqueda |
| `ResultadoModo` | `SinRespuestaCorrectas`, `SinRespuestaTotal` | `int` | De las preguntas sin respuesta, cuántas recibieron la frase fija con este modo |
| `InformeEvaluacion` | `Resultados` | `List<ResultadoModo>` | La tabla, con una fila por modo |
| `InformeEvaluacion` | `Detalle` | `List<DetallePregunta>` | El puesto de cada pregunta en cada modo |
| `InformeEvaluacion` | `Fallas`, `Avisos` | `List<string>` | Las preguntas que fallaron, y los avisos (examen vacío, documentos no cargados, Full-Text pendiente, fragmentos sin vector) |

## RagBasico.Api\Ollama.cs

- **Responsabilidad:** hablar con Ollama por HTTP. Manda el mismo JSON que se mandaría con Postman y devuelve lo que contesta el modelo.
- **Contiene:** la clase `Ollama`, con la constante pública `Dimension` (1024, el tamaño de cada vector), y tres records privados para leer las respuestas: `RespuestaChat` (el mensaje y los tiempos de `/api/chat`), `MensajeChat` (el texto que escribió el modelo) y `RespuestaEmbed` (los vectores de `/api/embed`).
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-5-vectores.

### Clase Ollama

- **Responsabilidad:** ser el único lugar del código que conoce el formato de la API de Ollama.
- **Recibe en el constructor:** el `HttpClient` que arma Program.cs, con la dirección y el tiempo de espera de la configuración, y la configuración, de donde lee `Ollama:ModeloChat`, `Ollama:ModeloEmbeddings`, `Ollama:InstruccionPregunta` y `Ollama:MantenerCargado`.
- **La registra:** Program.cs, con `AddHttpClient<Ollama>`, que crea un cliente tipado nuevo en cada pedido.
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-5-vectores (los embeddings).

### Ollama.ChatAsync(sistema, usuario, ct)

- **Qué hace:** le pide una respuesta al modelo de chat. Manda `stream: false`, para recibir la respuesta entera de una vez, y `think: false`, porque qwen3.5 piensa antes de responder si no se le dice lo contrario. Si el texto trae un bloque de razonamiento, lo descarta.
- **Recibe:** el mensaje de sistema (las reglas), el mensaje del usuario y el token de cancelación del pedido.
- **Devuelve:** un `RespuestaModelo` con el texto y los tiempos que informa Ollama: carga del modelo, lectura de la pregunta y escritura de la respuesta, en milisegundos, y la cantidad de tokens leídos y escritos.
- **Efectos:** un `POST /api/chat` a Ollama.
- **La llaman:** Redactor.ResponderAsync.
- **Desde:** fase-1-ollama.
- **Cambió en:** nada todavía.

### Ollama.EmbeddingsAsync(textos, ct)

- **Qué hace:** calcula el vector de cada texto con el modelo de embeddings, todos en un solo pedido a `/api/embed`. Los fragmentos se mandan así, sin instrucción, como dice la ficha del modelo. Controla que vuelva un vector por texto y que cada uno tenga 1024 números; si no, lanza un error que lo dice.
- **Recibe:** la lista de textos y el token de cancelación.
- **Devuelve:** un vector (`float[]` de 1024 números) por texto, en el mismo orden.
- **Efectos:** un `POST /api/embed` a Ollama.
- **La llaman:** Ollama.EmbeddingPreguntaAsync y Vectorizador.CalcularYGuardarAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Ollama.EmbeddingPreguntaAsync(pregunta, ct)

- **Qué hace:** calcula el vector de una pregunta. Le antepone la instrucción de `Ollama:InstruccionPregunta` con el formato de la ficha del modelo: `Instruct: `, la instrucción, un salto de línea y `Query:` pegado a la pregunta. La instrucción va en inglés, como recomienda la ficha. Como solo cambia el vector de la pregunta, probarla en castellano no obliga a recalcular los fragmentos.
- **Recibe:** la pregunta y el token de cancelación.
- **Devuelve:** el vector de la pregunta.
- **Efectos:** un `POST /api/embed` a Ollama, por medio de Ollama.EmbeddingsAsync.
- **La llaman:** Buscador.PorVectoresAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Ollama.VerificarAsync(respuesta, ct)

- **Qué hace:** si Ollama contestó con un error, lanza una excepción con su mensaje real, por ejemplo "model not found", en lugar de un error genérico.
- **Recibe:** la respuesta HTTP de Ollama y el token de cancelación.
- **Devuelve:** nada. Si la respuesta es un error, lanza `InvalidOperationException`.
- **Efectos:** lee el cuerpo de la respuesta cuando es un error.
- **La llaman:** Ollama.ChatAsync y Ollama.EmbeddingsAsync.
- **Desde:** fase-1-ollama.
- **Cambió en:** nada todavía.

### Ollama.NanosAMilisegundos(nanosegundos)

- **Qué hace:** pasa un tiempo de Ollama, que viene en nanosegundos, a milisegundos.
- **Recibe:** el tiempo en nanosegundos.
- **Devuelve:** el tiempo en milisegundos, como número entero.
- **Efectos:** ninguno.
- **La llaman:** Ollama.ChatAsync.
- **Desde:** fase-1-ollama.
- **Cambió en:** nada todavía.

## RagBasico.Api\Planificador.cs

- **Responsabilidad:** decidir qué hacer con cada PDF comparando la carpeta con la base. No lee archivos ni la base: recibe las dos listas y devuelve el plan, así se puede probar sin SQL Server.
- **Desde:** fase-4-carga.
- **Cambió en:** nada todavía.

### Clase Planificador

- **Responsabilidad:** la comparación entre la carpeta y la base. Es estática: no guarda nada.
- **Recibe en el constructor:** nada; una clase estática no tiene constructor.
- **La registra:** nadie. Se usa directo: `Planificador.Armar(...)`.
- **Desde:** fase-4-carga.
- **Cambió en:** nada todavía.

### Planificador.Armar(enCarpeta, enBase)

- **Qué hace:** compara los PDF de la carpeta con los documentos cargados, por nombre y sin distinguir mayúsculas, como Windows y SQL Server.
  - Un PDF que no está en la base es nuevo. Uno con la misma huella queda igual. Uno con otra huella, o cargado sin huella, cambió.
  - Un documento de la base que no está en la carpeta se borra, salvo que la carpeta no tenga ningún PDF: puede ser un disco desconectado o una carpeta equivocada, y borrar todo por eso sería un desastre.
  - Un PDF que no se pudo leer, sin huella, no se carga ni se borra.
- **Recibe:** los PDF de la carpeta con su huella y los documentos de la base con la suya.
- **Devuelve:** un `PlanDeCarga` con cuatro listas: nuevos, cambiados, sin cambios y para borrar.
- **Efectos:** ninguno.
- **La llaman:** Indexador.IndexarCarpetaAsync.
- **Desde:** fase-4-carga.
- **Cambió en:** nada todavía.

## RagBasico.Api\Program.cs

- **Responsabilidad:** el punto de entrada de la API: arma la aplicación, registra los servicios, declara los endpoints y la deja escuchando.
- **Contiene:** instrucciones de nivel superior, sin clase: el compilador arma solo el método `Main`. Se lee en cinco bloques:
  - **Armado:** `WebApplication.CreateBuilder(args)` lee `appsettings.json`, las variables de entorno y los argumentos.
  - **Servicios:** `AddHttpClient<Ollama>` registra el cliente de Ollama con `Ollama:Url` y `Ollama:SegundosDeEspera`. `AddScoped` registra el `Indexador`, el `Vectorizador`, el `Buscador`, el `Redactor` y el `Evaluador`: un objeto nuevo de cada uno por pedido. Cuando un endpoint o un constructor pide uno, ASP.NET Core se lo crea: eso es la inyección de dependencias.
  - **Control al arrancar:** muestra en el log la carpeta de PDF que va a usar, o avisa que no existe y qué variable de entorno crear.
  - **Endpoints:** cada `app.MapGet(...)` o `app.MapPost(...)` asocia una ruta con la función que la atiende. `GET /buscar` y `POST /preguntar` controlan el modo con Buscador.ElegirModo y, si no existe, devuelven `Results.BadRequest`: un error 400 con el motivo.
  - **Arranque:** `app.Run()` deja la API escuchando en la dirección de `launchSettings.json` hasta que se la frena con Ctrl+C.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-5-vectores.

### GET /

- **Recibe:** nada.
- **Devuelve:** un JSON con el nombre del proyecto, la fase y la versión de .NET. El proyecto y la fase salen de la configuración (`Rag:Proyecto` y `Rag:Fase`).
- **La atiende:** una función sin nombre (lambda) declarada en `Program.cs` con `app.MapGet("/", ...)`.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

### POST /indexar

- **Recibe:** nada. Los PDF salen de la carpeta `Rag:CarpetaDocumentos`.
- **Devuelve:** el `InformeIndexado` de la carga: cuántos PDF se cargaron, cuántos quedaron igual y cuántos se borraron, los fragmentos, el tiempo y el detalle. La segunda vez seguida dice `SIN CAMBIOS`.
- **La atiende:** una función de `Program.cs` que llama a Indexador.IndexarCarpetaAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-4-carga (pone la base al día con la carpeta en lugar de recargar todo).

### POST /vectores

- **Recibe:** nada.
- **Devuelve:** el `InformeVectores`: cuántos vectores se calcularon, cuántos fallaron, cuántos fragmentos quedan sin vector, cuántos fragmentos por segundo, el tiempo y el detalle. La segunda vez seguida dice `SIN CAMBIOS`.
- **La atiende:** una función de `Program.cs` que llama a Vectorizador.CalcularPendientesAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### GET /buscar

- **Recibe:** la pregunta en el parámetro `q` de la dirección y, si se quiere, el modo en `modo`: `palabras`, que es el modo por defecto, o `vectores`. Por ejemplo: `/buscar?q=requisitos reintegro&modo=vectores`.
- **Devuelve:** la lista de fragmentos encontrados con ese modo, hasta `Rag:TopK`. Si falta `q`, o si el modo no existe, un error 400.
- **La atiende:** una función de `Program.cs` que controla el modo con Buscador.ElegirModo y llama a Buscador.BuscarAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-5-vectores (el parámetro `modo`).

### POST /preguntar

- **Recibe:** un JSON con la pregunta y, si se quiere, el modo: `{ "pregunta": "...", "modo": "vectores" }`, que ASP.NET Core convierte en un `PedidoPregunta`. Sin modo, busca por palabras.
- **Devuelve:** el `RespuestaRag`: la respuesta de Qwen con citas `[n]`, las fuentes y los tiempos. Si los documentos no alcanzan, la frase "No encontré esa información en los documentos." sin fuentes. Si el modo no existe, un error 400 con el motivo.
- **La atiende:** una función de `Program.cs` que controla el modo con Buscador.ElegirModo y llama a Redactor.ResponderAsync.
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-2-rag-minimo (responde con los documentos, no con lo que sabe Qwen), fase-5-vectores (el modo).

### POST /evaluar

- **Recibe:** nada. El examen sale de `Rag:ArchivoExamen`.
- **Devuelve:** el `InformeEvaluacion`: una fila por modo con "1er lugar", "entre los 5", ms promedio y las preguntas sin respuesta bien contestadas; el detalle por pregunta, las fallas y los avisos.
- **La atiende:** una función de `Program.cs` que llama a Evaluador.CorrerAsync.
- **Desde:** fase-3-medicion.
- **Cambió en:** fase-5-vectores (compara palabras contra vectores).

## RagBasico.Api\Properties\launchSettings.json

- **Responsabilidad:** dice cómo arranca la API cuando se la corre con `dotnet run` o con F5 en Visual Studio: la dirección, el puerto, el entorno y si abre el navegador.
- **Contiene:** un solo perfil, `http`: escucha en `http://127.0.0.1:508N`, donde N es el número de la fase, con el entorno `Development`, y abre el navegador. Así dos fases pueden correr a la vez. El template traía también un perfil `https` con puertos al azar, que se sacó porque chocaría entre fases.
- **Por qué 127.0.0.1:** es la misma dirección que usan Ollama y Qdrant, que escuchan solo en IPv4, así todo el proyecto usa una sola. La consecuencia es que la API también escucha solo en IPv4: se la llama con `http://127.0.0.1:508N`, como hacen el archivo `.http` y el navegador que abre el perfil. Con `localhost`, los clientes de .NET (PowerShell, `WebClient`, `HttpClient`) prueban primero IPv6 y tardan unos 2 segundos más en conectarse.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores.

## RagBasico.Api\RagBasico.Api.csproj

- **Responsabilidad:** describe el proyecto para el compilador: una API web (`Microsoft.NET.Sdk.Web`) para .NET 10, y los paquetes NuGet que usa.
- **Contiene:** `TargetFramework` `net10.0`; `Nullable` activado, para que el compilador avise cuando algo puede ser `null`; `ImplicitUsings`, que agrega a todos los archivos los `using` más comunes; y `GenerateDocumentationFile`, para que el compilador avise si una clase o función pública no tiene su `/// <summary>`. Los paquetes, con la versión exacta para que cada fase compile igual siempre:
  - **Dapper 2.1.89:** corre SQL y convierte cada fila en un objeto, sin escribir los `SqlCommand` y `SqlParameter` a mano.
  - **Microsoft.Data.SqlClient 7.1.2:** la conexión con SQL Server.
  - **PdfPig 0.1.16:** lee el texto de los PDF.
- **Desde:** fase-0-base.
- **Cambió en:** fase-2-rag-minimo.

## RagBasico.Api\RagBasico.Api.http

- **Responsabilidad:** los pedidos de prueba de la fase, listos para mandar con "Send Request" desde Visual Studio, o desde VS Code con la extensión "REST Client".
- **Contiene:** la variable `@host`, con la dirección y el puerto de la fase, y un pedido por cada endpoint de esta versión.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores.

## RagBasico.Api\Redactor.cs

- **Responsabilidad:** la "G" de RAG: armar la respuesta con los fragmentos encontrados.
- **Contiene:** la clase `Redactor`, con dos constantes: `SinRespuesta`, la única frase para "no encontré", y `Reglas`, el mensaje de sistema con las cinco reglas para Qwen (usar solo los fragmentos, citar con `[n]`, la frase exacta cuando no alcanza, e ignorar instrucciones que vengan dentro de los documentos).
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-3-medicion, fase-5-vectores.

### Clase Redactor

- **Responsabilidad:** buscar, pedirle la respuesta a Qwen, armar la respuesta con sus fuentes y registrar la consulta.
- **Recibe en el constructor:** el `Buscador`, el cliente de `Ollama`, el log y la configuración, de donde lee `ConnectionStrings:Rag` y `Rag:TopK`.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-3-medicion (recibe el log y registra cada consulta).

### Redactor.ResponderAsync(pregunta, modo, ct)

- **Qué hace:** busca los fragmentos de la pregunta con el modo pedido y le pide la respuesta a Qwen. Si no hay fragmentos, responde la frase fija sin llamar al modelo; por vectores casi nunca pasa, así que el "no sé" lo decide Qwen. Si Qwen responde la frase de "no encontré", la devuelve sin fuentes. Al final registra la consulta, con el modo.
- **Recibe:** la pregunta, el modo de búsqueda y el token de cancelación.
- **Devuelve:** un `RespuestaRag`: el texto, las fuentes numeradas como las cita el modelo y los tiempos.
- **Efectos:** lee SQL Server (por el buscador), llama a Ollama y escribe una fila en `rag.Consulta`.
- **La llaman:** el endpoint `POST /preguntar` y Evaluador.CorrerAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-3-medicion (registra la consulta), fase-5-vectores (recibe el modo).

### Redactor.ArmarMensaje(pregunta, fragmentos)

- **Qué hace:** arma el mensaje del usuario para Qwen: los fragmentos numerados `[1]` a `[n]` entre `<fragmentos>` y `</fragmentos>`, y la pregunta al final, que es lo que más atiende un modelo chico. Corta cada fragmento a 2.500 caracteres.
- **Recibe:** la pregunta y la lista de fragmentos.
- **Devuelve:** el texto del mensaje.
- **Efectos:** ninguno.
- **La llaman:** Redactor.ResponderAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Redactor.Neutralizar(texto)

- **Qué hace:** cambia los signos `<` y `>` del texto de un documento por `‹` y `›`. Así un PDF que diga `</fragmentos>` no puede cerrar el bloque de datos y dar órdenes.
- **Recibe:** el texto de un fragmento.
- **Devuelve:** el mismo texto, con los signos cambiados.
- **Efectos:** ninguno.
- **La llaman:** Redactor.ArmarMensaje.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Redactor.EsSinRespuesta(texto)

- **Qué hace:** dice si el modelo respondió la frase de "no encontré", aunque le agregue comillas o le saque el punto final. Un modelo chico no siempre copia la frase exacta.
- **Recibe:** el texto que escribió el modelo.
- **Devuelve:** `true` si es la frase de "no encontré".
- **Efectos:** ninguno.
- **La llaman:** Redactor.ResponderAsync y Evaluador.CorrerAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Redactor.RegistrarAsync(pregunta, modo, fragmentos, respuesta)

- **Qué hace:** guarda la consulta en `rag.Consulta`: la pregunta (recortada a 2.000 caracteres), el modo, los fragmentos usados, la respuesta y los tiempos. Si falla, lo anota en el log como advertencia y la respuesta sale igual.
- **Recibe:** la pregunta, el modo de búsqueda, los fragmentos encontrados y la respuesta armada.
- **Devuelve:** nada.
- **Efectos:** inserta una fila en `rag.Consulta`.
- **La llaman:** Redactor.ResponderAsync.
- **Desde:** fase-3-medicion.
- **Cambió en:** nada todavía.

## RagBasico.Api\Vectorizador.cs

- **Responsabilidad:** calcular el vector de cada fragmento que todavía no lo tiene y guardarlo en `rag.Fragmento`.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Clase Vectorizador

- **Responsabilidad:** el camino de un fragmento hasta su vector: leerlo de la base, pedirle el vector a Ollama y guardarlo.
- **Recibe en el constructor:** el cliente de `Ollama` y la configuración, de donde lee `ConnectionStrings:Rag` y `Rag:LoteVectores`.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Vectorizador.CalcularPendientesAsync(ct)

- **Qué hace:** recorre los fragmentos sin vector de a lotes de `Rag:LoteVectores`, en orden de número de fragmento.
  - Cada lote empieza después del último fragmento visto, en lugar de volver a buscar desde el principio. Así no relee lo ya hecho, que con un millón de fragmentos haría crecer el costo al cuadrado, y un fragmento que falla no traba la corrida: queda en `NULL` y se vuelve a intentar en el próximo `/vectores`.
  - Si fallan todos los fragmentos de un lote, uno por uno, corta la corrida: el problema no es de un fragmento sino de Ollama o del modelo.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `InformeVectores`: cuántos vectores se calcularon, cuántos fallaron, cuántos fragmentos quedan sin vector, cuántos fragmentos por segundo, el tiempo como horas:minutos:segundos y el detalle. Si no había nada que hacer, el detalle dice `SIN CAMBIOS`.
- **Efectos:** lee `rag.Fragmento` y `rag.Documento`, llama a Ollama y escribe `rag.Fragmento.Embedding`.
- **La llaman:** el endpoint `POST /vectores`.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Vectorizador.ComoJson(vector)

- **Qué hace:** escribe un vector como lo recibe SQL Server: un arreglo JSON, como `[0.5,-0.25,0.0000123]`. Usa siempre el punto decimal (`CultureInfo.InvariantCulture`): con Windows en castellano, `ToString()` escribiría `0,5` y el JSON quedaría mal. Tampoco usa notación científica (`1E-05`).
- **Recibe:** el vector.
- **Devuelve:** el texto JSON.
- **Efectos:** ninguno.
- **La llaman:** Vectorizador.CalcularYGuardarAsync y Buscador.PorVectoresAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Vectorizador.GuardarLoteAsync(conexion, lote, detalle, ct)

- **Qué hace:** calcula y guarda los vectores de un lote en un solo pedido a Ollama. Si falla, prueba los fragmentos de a uno, así un fragmento con problemas no deja sin vector a los demás, y anota cada uno que falla. Si se canceló el pedido, corta todo; cualquier otro error, incluido un tiempo de espera de Ollama, cuenta como error del lote.
- **Recibe:** la conexión abierta, los fragmentos del lote, la lista del detalle y el token de cancelación.
- **Devuelve:** cuántos vectores guardó.
- **Efectos:** los de Vectorizador.CalcularYGuardarAsync.
- **La llaman:** Vectorizador.CalcularPendientesAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Vectorizador.CalcularYGuardarAsync(conexion, fragmentos, ct)

- **Qué hace:** le pide a Ollama los vectores de los fragmentos en un solo pedido y los guarda en `rag.Fragmento.Embedding`, todos en una transacción. SQL Server convierte el texto JSON en un `VECTOR(1024)` con `CAST`.
- **Recibe:** la conexión abierta, los fragmentos y el token de cancelación.
- **Devuelve:** nada.
- **Efectos:** un `POST /api/embed` a Ollama y un `UPDATE` por fragmento.
- **La llaman:** Vectorizador.GuardarLoteAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

## RagBasico.Api\appsettings.json

- **Responsabilidad:** la configuración de la API. Cada clave está explicada en `docs\arquitectura.md`.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores.

## RagBasico.slnx

- **Responsabilidad:** la solución, el archivo que se abre con doble clic en Visual Studio. Agrupa los proyectos del repo; hoy, solo `RagBasico.Api`.
- **Contiene:** el formato `.slnx`, el XML que usa .NET 10 por defecto, más corto y legible que el `.sln` de siempre.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## docs\arquitectura.md

- **Responsabilidad:** cómo se arma esta versión: las piezas, los recorridos paso a paso, las tablas, la configuración y lo que está fuera del repo.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores.

## docs\erratas.md

- **Responsabilidad:** los bugs encontrados después de cerrar una fase, y dónde se arreglaron. Solo se actualiza en la punta.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## docs\fases\fase-0-base.md

- **Responsabilidad:** qué agregó la fase 0, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## docs\fases\fase-1-ollama.md

- **Responsabilidad:** qué agregó la fase 1, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-1-ollama.
- **Cambió en:** nada todavía.

## docs\fases\fase-2-rag-minimo.md

- **Responsabilidad:** qué agregó la fase 2, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

## docs\fases\fase-3-medicion.md

- **Responsabilidad:** qué agregó la fase 3, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-3-medicion.
- **Cambió en:** nada todavía.

## docs\fases\fase-4-carga.md

- **Responsabilidad:** qué agregó la fase 4, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-4-carga.
- **Cambió en:** nada todavía.

## docs\fases\fase-5-vectores.md

- **Responsabilidad:** qué agregó la fase 5, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

## docs\responsabilidades.md

- **Responsabilidad:** este documento: qué hace cada archivo, clase, función y endpoint, y quién lo usa.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores.

## docs\worktrees.md

- **Responsabilidad:** cómo abrir y correr cada fase en su propia carpeta, con git worktree, también en un clon nuevo.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## documentos.csv

- **Responsabilidad:** el manifiesto del corpus del examen: una línea por PDF con su nombre, la URL de donde se bajó y su huella sha256. Con él, el examen y los documentos quedan congelados.
- **Contiene:** el encabezado `archivo;url;sha256`. Las líneas las escribe `scripts\crear-manifiesto.ps1`; las URL se completan a mano.
- **Desde:** fase-3-medicion.
- **Cambió en:** nada todavía.

## examen.csv

- **Responsabilidad:** el examen: las preguntas, escritas antes de construir, con el documento que las responde. Lo lee Evaluador.LeerExamen.
- **Contiene:** el encabezado `pregunta;documento`. Las preguntas se escriben siguiendo `examen.ejemplo.csv`: 15 con las mismas palabras del documento, 15 dichas con otras palabras y 10 sin respuesta.
- **Desde:** fase-3-medicion.
- **Cambió en:** nada todavía.

## examen.ejemplo.csv

- **Responsabilidad:** muestra el formato del examen con cinco casos: una pregunta común, una con tildes, una con `;` adentro (entre comillas), una con dos documentos válidos separados con `|` y una sin respuesta (`ninguno`).
- **Desde:** fase-3-medicion.
- **Cambió en:** nada todavía.

## global.json

- **Responsabilidad:** fija la versión del SDK de .NET con la que se compila: 10.0.403, o el parche más nuevo instalado de la misma banda 10.0.4xx (`rollForward: latestPatch`). Si no hay ninguno, `dotnet` falla con un error que dice qué SDK instalar, en lugar de compilar con otro.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## scripts\controlar-docs.ps1

- **Responsabilidad:** controla que la documentación de la fase coincida con el código. Sale con 0 si coincide, y con 1 si encontró problemas, que muestra uno por línea. Una fase no se cierra hasta que sale con 0.
- **Parámetros:** ninguno. La fase sale de `Rag:Fase` en `RagBasico.Api\appsettings.json`.
- **Ejemplo:** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\controlar-docs.ps1`
- **Requiere:** git y Windows PowerShell 5.1. Se corre desde cualquier carpeta del repo, también desde una carpeta de fase y desde una ruta con tildes.
- **Qué controla:**
  1. Cada archivo del repo contra su sección `##` en este documento y contra el árbol. También al revés: cada sección y cada nombre del árbol tienen que ser algo que existe.
  2. Los campos obligatorios de cada sección y de cada entrada.
  3. Cada clase y cada función de los `.cs` contra su entrada `###`, y que tengan su `/// <summary>`. También al revés: cada entrada de clase o de función tiene que corresponder a una que existe.
  4. Cada endpoint (`app.MapGet`, `app.MapPost`...) contra su entrada `### GET /ruta`, en los dos sentidos. Los que están en comentarios no cuentan.
  5. Que **Desde** y **Cambió en** nombren branches `fase-*` que existen, locales o del remoto.
  6. Los cinco títulos de `docs\arquitectura.md`, y que nombre cada clave de `appsettings.json` y cada columna de `sql\*.sql`. Solo busca lo que falta.
  7. Los siete títulos de `docs\fases\<fase>.md`, y que "Qué cambió" nombre cada archivo cambiado desde la fase anterior, salvo `docs\` y el README. Solo busca lo que falta.
  8. Que la fila de la fase en el README no tenga celdas vacías.
- **Convenciones que necesita:** una clase por archivo con el mismo nombre (salvo `Modelos.cs`, que junta los records); el modificador de acceso siempre escrito; sin sobrecargas; los endpoints declarados con `app.MapGet`, `app.MapPost`...; y los campos escritos como `- **Etiqueta:** valor`.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

### Funciones de controlar-docs.ps1

| Función | Qué hace |
|---|---|
| `Agregar-Problema` | Anota un problema para mostrarlo al final |
| `Leer-Texto` | Lee un archivo en UTF-8, con o sin BOM, y deja los finales de línea en LF |
| `Quitar-Codigo` | Borra los bloques de código de un Markdown, para que su contenido no se tome como títulos |
| `Obtener-Secciones` | Corta un Markdown en secciones por títulos de un nivel (`##` o `###`) |
| `Obtener-Campos` | Lee los campos `- **Etiqueta:** valor` de una sección |
| `Controlar-Campos` | Controla que una sección tenga sus campos obligatorios con texto |
| `Buscar-Fases` | Devuelve los nombres de fase (`fase-N-nombre`) que aparecen en un texto |
| `Controlar-Fases` | Controla que **Desde** y **Cambió en** nombren branches que existen |
| `Normalizar-Ruta` | Pasa una ruta a barras hacia adelante, para comparar con lo que lista git |
| `Aplanar-Json` | Devuelve las claves de un JSON escritas `Seccion:Clave` |
| `Tiene-Resumen` | Dice si una clase o una función tiene su `/// <summary>` arriba, salteando los atributos, también los de varias líneas |
| `Analizar-CSharp` | Lee un `.cs` y devuelve sus tipos y funciones, con su línea y si tienen `/// <summary>`. Reconoce métodos genéricos y los que devuelven tuplas |
| `Buscar-Endpoints` | Devuelve los endpoints de un `.cs`, sin contar los que están en comentarios |

## scripts\crear-base.ps1

- **Responsabilidad:** crea la base de la fase y corre en orden los scripts numerados de `sql\`.
- **Parámetros:** `-Base` y `-Servidor`, opcionales. Por defecto salen de `ConnectionStrings:Rag` en `RagBasico.Api\appsettings.json`.
- **Ejemplo:** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1`
- **Requiere:** sqlcmd (`winget install Microsoft.Sqlcmd`), SQL Server con la instancia de la cadena de conexión y Windows PowerShell 5.1.
- **Pasos:** lee la cadena de conexión; busca los `sql\NN_*.sql` y los ordena por nombre; corre cada uno con `sqlcmd -E -C -b -v Base=<base>`: autenticación de Windows, confiar en el certificado de SQL Express y cortar ante el primer error.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

## scripts\crear-manifiesto.ps1

- **Responsabilidad:** arma o verifica `documentos.csv`. Sin parámetros, escribe una línea por PDF con su sha256 y conserva las URL que ya estaban anotadas. Con `-Verificar`, no escribe: compara la carpeta con el manifiesto y sale con 1 si falta, sobra o cambió algún PDF.
- **Parámetros:** `-Carpeta`, opcional, la carpeta de los PDF (por defecto, la variable de entorno `Rag__CarpetaDocumentos`); `-Verificar`, para solo comparar.
- **Ejemplo:** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-manifiesto.ps1 -Verificar`
- **Requiere:** git y Windows PowerShell 5.1.
- **Desde:** fase-3-medicion.
- **Cambió en:** nada todavía.

## scripts\crear-worktrees.ps1

- **Responsabilidad:** crea o actualiza una carpeta por cada fase, con git worktree, para leer y correr cada fase sin cambiar de branch.
- **Parámetros:** `-Carpeta`, opcional: dónde van las carpetas. Por defecto, una carpeta al lado del repo, con el mismo nombre y `-fases` al final.
- **Ejemplo:** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-worktrees.ps1`
- **Requiere:** git 2.31 o posterior y Windows PowerShell 5.1. Se corre solo desde la carpeta principal del repo, porque cada fase trae su propia copia del script.
- **Pasos:**
  1. Si se lo corre desde una carpeta de fase, sale con error.
  2. Corre `git worktree prune`, que limpia el registro de carpetas borradas a mano.
  3. Por cada branch `fase-*`, local o del remoto: si su carpeta no existe, la crea separada (`git worktree add --detach`). Si existe y tiene cambios en archivos que git sigue, avisa y no la toca. Si no, la mueve a la versión actual de su branch; los archivos nuevos que no sigue git, como la configuración que deja un editor, se conservan. Si una fase está solo en el remoto, usa esa versión, por ejemplo `origin/fase-0-base`.
  4. Avisa qué carpetas ya no tienen branch, y termina mostrando `git worktree list`.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## sql\01_base.sql

- **Responsabilidad:** crea la base de la fase, el esquema `rag`, las tablas de documentos y fragmentos, y el índice Full-Text en español.
- **Qué crea:** la base (si no existe), el esquema `rag`, las tablas `rag.Documento` y `rag.Fragmento`, el catálogo `ftRag` sin distinguir acentos y el índice Full-Text sobre `rag.Fragmento.Texto`, en español (3082) y con actualización automática. Cada paso controla si ya está hecho, así se puede correr de nuevo.
- **Variables sqlcmd:** `Base`, el nombre de la base. Lo pasa `scripts\crear-base.ps1`; en SSMS se define con `:setvar Base RagBasico_F2` en modo SQLCMD.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

## sql\02_consultas.sql

- **Responsabilidad:** crea la tabla del registro de preguntas.
- **Qué crea:** la tabla `rag.Consulta`, si no existe.
- **Variables sqlcmd:** `Base`, el nombre de la base.
- **Desde:** fase-3-medicion.
- **Cambió en:** nada todavía.

## sql\03_huella.sql

- **Responsabilidad:** agrega la huella de carga a los documentos, para que `/indexar` no vuelva a procesar los PDF que no cambiaron.
- **Qué crea:** la columna `Huella VARBINARY(32)` en `rag.Documento`, si no existe. Queda en `NULL` en los documentos cargados antes, que el próximo `/indexar` toma como cambiados y recarga.
- **Variables sqlcmd:** `Base`, el nombre de la base.
- **Desde:** fase-4-carga.
- **Cambió en:** nada todavía.

## sql\04_vectores.sql

- **Responsabilidad:** agrega el vector de cada fragmento, para buscar por significado.
- **Qué crea:** la columna `Embedding VECTOR(1024)` en `rag.Fragmento`, si no existe. Queda en `NULL` hasta que `POST /vectores` la calcula.
- **Variables sqlcmd:** `Base`, el nombre de la base.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

## sql\consultas-utiles.sql

- **Responsabilidad:** consultas para mirar la base desde SSMS:
  - cuántos documentos y fragmentos hay, y tres fragmentos al azar;
  - cada documento con su huella y su cantidad de fragmentos;
  - los fragmentos de un documento, para revisar un PDF que `/indexar` marcó con `TEXTO DUDOSO`;
  - los fragmentos de menos de 30 palabras, que con el corte por párrafos casi no tendría que haber;
  - si el Full-Text terminó de indexar, una búsqueda a mano y qué palabras busca el Full-Text a partir de una;
  - cuántos fragmentos tienen vector, los vecinos más cercanos de un fragmento y cuánto tarda SQL Server en recorrer todos los vectores. Toman el primer fragmento con vector que exista, no el número 1, que puede no existir después de recargar.
- **Qué crea:** nada. `scripts\crear-base.ps1` no la corre, porque su nombre no empieza con un número.
- **Variables sqlcmd:** ninguna. Antes de usarla se elige la base de la fase en SSMS.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-4-carga, fase-5-vectores.

