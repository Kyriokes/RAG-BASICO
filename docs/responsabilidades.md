# Responsabilidades

Qué hace cada archivo, clase, función y endpoint de esta versión, y quién lo usa. Esta es la versión de `fase-8-validaciones`.

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
│  ├─ Buscador.cs                  búsqueda por palabras, por vectores e híbrida
│  ├─ Chunker.cs                   corta el texto en fragmentos por párrafos
│  ├─ DetectorDeCaidas.cs          dice qué servicio no responde
│  ├─ ErrorDeServicio.cs           el error de Ollama o de Qdrant
│  ├─ Errores.cs                   cuándo un error es un servicio caído
│  ├─ Evaluador.cs                 corre el examen
│  ├─ Indexador.cs                 pone la base al día con la carpeta de PDF
│  ├─ ManejadorDeErrores.cs        convierte los errores en respuestas claras
│  ├─ Modelos.cs                   records de pedidos y respuestas
│  ├─ Ollama.cs                    cliente HTTP de Ollama
│  ├─ Planificador.cs              decide qué PDF cargar, dejar o borrar
│  ├─ Program.cs                   arranque, servicios y endpoints
│  ├─ Properties\
│  │  └─ launchSettings.json       dirección, puerto y forma de arrancar
│  ├─ Qdrant.cs                    cliente HTTP de Qdrant
│  ├─ RagBasico.Api.csproj         el proyecto y sus paquetes
│  ├─ RagBasico.Api.http           pedidos de prueba
│  ├─ Redactor.cs                  arma la respuesta con Qwen y la registra
│  ├─ Rrf.cs                       mezcla dos listas de resultados por puesto
│  ├─ Salud.cs                     revisa que anden todas las piezas
│  ├─ Sincronizador.cs             mantiene Qdrant igual a SQL Server
│  ├─ Validador.cs                 controla la pregunta
│  ├─ Vectorizador.cs              calcula el vector de cada fragmento
│  ├─ appsettings.json             configuración
│  └─ wwwroot\
│     └─ index.html                la página de la demo
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
│  │  ├─ fase-5-vectores.md        la búsqueda por significado
│  │  ├─ fase-6-hibrida.md         la híbrida y la página de la demo
│  │  ├─ fase-7-qdrant.md          el índice de vectores
│  │  └─ fase-8-validaciones.md    las validaciones y los errores claros
│  ├─ instalacion.md               qué instalar y cómo comprobar que anda
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
│  ├─ crear-worktrees.ps1          crea una carpeta por fase
│  └─ iniciar-qdrant.ps1           arranca Qdrant solo en esta PC
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
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-8-validaciones.

## RagBasico.Api\Buscador.cs

- **Responsabilidad:** encontrar los fragmentos que mejor responden una pregunta: por palabras, por significado en SQL Server o en Qdrant, o con las dos mezcladas.
- **Contiene:** la clase `Buscador`, con la lista pública `Modos` (`palabras`, `vectores`, `vectores-qdrant` e `hibrida`), la constante `ModoPorDefecto` (`hibrida`) y las constantes `AvisoSinQdrant`, `AvisoErrorQdrant` y `AvisoQdrantVacio`, los avisos de la híbrida cuando Qdrant no responde, contesta con un error o no tiene vectores.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-5-vectores, fase-6-hibrida, fase-7-qdrant.

### Clase Buscador

- **Responsabilidad:** juntar en un solo lugar las formas de buscar: por palabras, con el Full-Text; por vectores en SQL Server, con `VECTOR_DISTANCE`; por vectores en Qdrant, con su índice HNSW; y la híbrida, que mezcla palabras con Qdrant y sigue con SQL si Qdrant no responde.
- **Recibe en el constructor:** los clientes de `Ollama`, para el vector de la pregunta, y de `Qdrant`; el log, para anotar cuando Qdrant no responde; y la configuración, de donde lee `ConnectionStrings:Rag` y `Rag:CandidatosHibrida`.
- **La registra:** Program.cs, con `AddScoped`: un objeto nuevo por pedido HTTP.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-5-vectores (recibe el cliente de Ollama y busca por vectores), fase-6-hibrida (la híbrida, que pasa a ser el modo por defecto), fase-7-qdrant (Qdrant y el respaldo en SQL).

### Buscador.ElegirModo(pedido)

- **Qué hace:** controla el modo que llega en un pedido. Sin modo, devuelve el modo por defecto, `hibrida`. Con un modo que existe, lo devuelve en minúsculas y sin espacios. Con un modo que no existe devuelve `null`, para que quien llama dé un error en lugar de buscar con otro modo sin avisar.
- **Recibe:** el modo del pedido, que puede faltar.
- **Devuelve:** el modo, o `null` si no existe.
- **Efectos:** ninguno.
- **La llaman:** los endpoints `GET /buscar` y `POST /preguntar`.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Buscador.BuscarAsync(pregunta, modo, topK, ct)

- **Qué hace:** busca con el modo pedido: llama a Buscador.PorPalabrasAsync, Buscador.PorVectoresAsync, Buscador.PorVectoresQdrantAsync o Buscador.HibridaAsync. Con un modo que no existe lanza un `ArgumentException`, aunque los endpoints ya lo controlaron antes.
- **Recibe:** la pregunta, el modo, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** un `ResultadoBusqueda`: los fragmentos, del más relevante al menos relevante, y un aviso, vacío salvo que la híbrida haya seguido sin Qdrant.
- **Efectos:** los de la búsqueda que usa.
- **La llaman:** el endpoint `GET /buscar`, Redactor.ResponderAsync y Evaluador.CorrerModoAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** fase-6-hibrida (el modo `hibrida`), fase-7-qdrant (el modo `vectores-qdrant`, y devuelve el aviso).

### Buscador.HibridaAsync(pregunta, topK, ct)

- **Qué hace:** busca por palabras y por vectores al mismo tiempo, pide a cada una los mejores 20 candidatos (`Rag:CandidatosHibrida`) y los mezcla por puesto con Rrf.Mezclar. La parte por vectores usa Qdrant y, si Qdrant no responde, contesta con un error o no tiene vectores, los vectores de SQL Server: así la demo sigue andando. Las dos búsquedas arrancan juntas y recién después se espera a las dos con `Task.WhenAll`: se tarda lo que la más lenta, no la suma.
- **Recibe:** la pregunta, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** un `ResultadoBusqueda` con la lista mezclada, de mayor a menor puntaje RRF, y el aviso si Qdrant no respondió.
- **Efectos:** un pedido a Ollama, uno a Qdrant y dos lecturas de SQL Server.
- **La llaman:** Buscador.BuscarAsync.
- **Desde:** fase-6-hibrida.
- **Cambió en:** fase-7-qdrant (los vectores salen de Qdrant, con respaldo en SQL).

### Buscador.PorPalabrasAsync(pregunta, topK, ct)

- **Qué hace:** busca en el Full-Text de SQL Server los fragmentos que mejor coinciden con las palabras de la pregunta. `FREETEXTTABLE` corta la pregunta en palabras, busca también sus plurales y conjugaciones, y le pone un puntaje (`RANK`) a cada fragmento. Se piden los 50 mejores y se devuelven los `topK` primeros.
- **Recibe:** la pregunta, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** una lista de `Fragmento` (número, archivo y texto), del más relevante al menos relevante. Vacía si la pregunta está vacía o si ninguna palabra coincide.
- **Efectos:** lee `rag.Fragmento` y `rag.Documento`. No escribe nada.
- **La llaman:** Buscador.BuscarAsync y Buscador.HibridaAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** nada todavía.

### Buscador.PorVectoresAsync(pregunta, topK, ct)

- **Qué hace:** busca los fragmentos más parecidos en significado a la pregunta con los vectores de SQL Server: le pide a Ollama el vector de la pregunta y busca los más cercanos con Buscador.CercanosEnSqlAsync. A diferencia de la búsqueda por palabras, casi nunca vuelve vacía: siempre hay un fragmento más cercano que otro.
- **Recibe:** la pregunta, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** la lista de `Fragmento`, del más cercano al más lejano. Vacía si la pregunta está vacía o si ningún fragmento tiene vector.
- **Efectos:** un pedido a Ollama; lee `rag.Fragmento` y `rag.Documento`. No escribe nada.
- **La llaman:** Buscador.BuscarAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** fase-7-qdrant (la consulta con `VECTOR_DISTANCE` pasa a Buscador.CercanosEnSqlAsync, para que la use también el respaldo de la híbrida).

### Buscador.PorVectoresQdrantAsync(pregunta, topK, ct)

- **Qué hace:** busca los fragmentos más parecidos en significado a la pregunta con el índice HNSW de Qdrant: le pide a Ollama el vector de la pregunta, a Qdrant los números de los fragmentos más cercanos, y a SQL Server sus textos. Si Qdrant no responde, falla: este modo existe para medir Qdrant, y caer en SQL sin avisar mediría otra cosa. Si la colección está vacía también falla, con un error que dice que hay que correr `POST /vectores`: si no, cada pregunta diría "no encontré" sin explicar por qué.
- **Recibe:** la pregunta, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** la lista de `Fragmento`, del más cercano al más lejano.
- **Efectos:** un pedido a Ollama y uno a Qdrant; lee `rag.Fragmento` y `rag.Documento`.
- **La llaman:** Buscador.BuscarAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Buscador.VectoresConRespaldoAsync(pregunta, topK, ct)

- **Qué hace:** la parte por significado de la híbrida. Calcula una sola vez el vector de la pregunta y busca en Qdrant. Si Qdrant falla, lo anota en el log y busca con los vectores de SQL Server, con el mismo vector. El aviso dice qué pasó, con Errores.EsCaida: `AvisoSinQdrant` si Qdrant no responde, y `AvisoErrorQdrant` si contestó con un error, por ejemplo porque no existe la colección. No es lo mismo: lo primero se arregla arrancando Qdrant, lo segundo con `POST /vectores` o `POST /qdrant/reconstruir`. Si Qdrant responde pero no tiene vectores, también busca en SQL, con otro aviso: si no, la híbrida quedaría solo con las palabras sin avisar. Si se canceló el pedido, no busca en SQL: corta.
- **Recibe:** la pregunta, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** un `ResultadoBusqueda`: los fragmentos y, si se usó SQL, el aviso `AvisoSinQdrant`, `AvisoErrorQdrant` o `AvisoQdrantVacio`.
- **Efectos:** un pedido a Ollama, uno a Qdrant y una lectura de SQL Server.
- **La llaman:** Buscador.HibridaAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Buscador.CercanosEnSqlAsync(vector, topK, ct)

- **Qué hace:** busca en SQL Server los fragmentos más cercanos a un vector: los ordena a todos por `VECTOR_DISTANCE('cosine', ...)`, donde 0 es el mismo sentido y 2 el opuesto, y se queda con los primeros. Es una búsqueda exacta: recorre todos los fragmentos que tienen vector, así que tarda más cuantos más haya.
- **Recibe:** el vector, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** la lista de `Fragmento`, del más cercano al más lejano.
- **Efectos:** lee `rag.Fragmento` y `rag.Documento`.
- **La llaman:** Buscador.PorVectoresAsync y Buscador.VectoresConRespaldoAsync.
- **Desde:** fase-7-qdrant. La consulta es la de Buscador.PorVectoresAsync de la fase 5.
- **Cambió en:** nada todavía.

### Buscador.LeerFragmentosAsync(ids, ct)

- **Qué hace:** trae de SQL Server los fragmentos de una lista de números, como la que devuelve Qdrant, y los deja en el mismo orden de la lista: SQL no los devuelve en ese orden. Un número que ya no existe en SQL se saltea.
- **Recibe:** los números de fragmento, del más cercano al más lejano, y el token de cancelación.
- **Devuelve:** la lista de `Fragmento`, en el orden de los números.
- **Efectos:** lee `rag.Fragmento` y `rag.Documento`.
- **La llaman:** Buscador.PorVectoresQdrantAsync y Buscador.VectoresConRespaldoAsync.
- **Desde:** fase-7-qdrant.
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

## RagBasico.Api\DetectorDeCaidas.cs

- **Responsabilidad:** decir qué servicio se cayó. Sin esto, si Ollama o Qdrant no responden, el error es un `HttpRequestException` genérico que no dice de cuál de los dos se trata.
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

### Clase DetectorDeCaidas

- **Responsabilidad:** un `DelegatingHandler`: una pieza que se pone delante del `HttpClient` y ve pasar cada pedido. Hay uno delante del cliente de Ollama y otro delante del de Qdrant.
- **Recibe en el constructor:** el nombre del servicio que vigila: `Ollama` o `Qdrant`.
- **La registra:** Program.cs, con `AddHttpMessageHandler` en cada `AddHttpClient`.
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

### DetectorDeCaidas.SendAsync(pedido, ct)

- **Qué hace:** manda el pedido al servicio. Si no se puede conectar, cambia el `HttpRequestException` por un `ErrorDeServicio` con código 503, el nombre del servicio y la dirección que no respondió, por ejemplo `Ollama no responde en http://127.0.0.1:11434`.
- **Recibe:** el pedido HTTP y el token de cancelación.
- **Devuelve:** la respuesta del servicio, sin tocarla.
- **Efectos:** el pedido HTTP.
- **La llaman:** el `HttpClient` de Ollama y el de Qdrant, en cada pedido.
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

## RagBasico.Api\ErrorDeServicio.cs

- **Responsabilidad:** un tipo de error propio para Ollama y Qdrant, con el nombre del servicio y el código HTTP que le corresponde, así el manejador de errores puede decir qué falló.
- **Contiene:** la clase `ErrorDeServicio`, con las propiedades `Servicio` (`Ollama` o `Qdrant`) y `CodigoHttp` (503 si no responde, 502 si respondió con un error).
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

### Clase ErrorDeServicio

- **Responsabilidad:** heredar de `Exception` y sumarle el servicio y el código HTTP.
- **Recibe en el constructor:** el servicio, el código HTTP, el mensaje y, si hay, el error original, que queda en `InnerException`.
- **La registra:** nadie. La lanzan DetectorDeCaidas.SendAsync, Ollama.VerificarAsync y Qdrant.VerificarAsync. La reconocen ManejadorDeErrores.Describir, para el código HTTP, y Errores.EsCaida, para saber si el servicio no responde.
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

## RagBasico.Api\Errores.cs

- **Responsabilidad:** las reglas sobre los errores que comparten varias piezas.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones.

### Clase Errores

- **Responsabilidad:** un solo lugar para decidir qué error es un servicio caído, así `/vectores` y la búsqueda híbrida usan el mismo criterio. Es estática: no guarda nada.
- **Recibe en el constructor:** nada; una clase estática no tiene constructor.
- **La registra:** nadie. Se usa directo: `Errores.EsCaida(...)`.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Errores.EsCaida(error)

- **Qué hace:** dice si un error muestra que un servicio, Ollama o Qdrant, no responde: no se pudo conectar o no contestó a tiempo (un `TaskCanceledException` con un `TimeoutException` adentro). El `DetectorDeCaidas` convierte el error de conexión en un `ErrorDeServicio` con código 503, que cuenta como caída; uno con 502, que es el servicio contestando un error, no. Un error que el servicio sí contestó, como un texto que Ollama no acepta o una colección que no existe en Qdrant, no es una caída.
- **Recibe:** el error.
- **Devuelve:** `true` si el servicio no responde.
- **Efectos:** ninguno.
- **La llaman:** Vectorizador.CalcularPendientesAsync, Vectorizador.GuardarLoteAsync y Buscador.VectoresConRespaldoAsync.
- **Desde:** fase-7-qdrant. Hasta la versión anterior de esa fase estaba en el Vectorizador, como `Vectorizador.EsCaida`.
- **Cambió en:** fase-8-validaciones (reconoce el `ErrorDeServicio`).

## RagBasico.Api\Evaluador.cs

- **Responsabilidad:** correr el examen y medir qué tan bien busca el sistema, con números y no con "a mí me anduvo".
- **Desde:** fase-3-medicion.
- **Cambió en:** fase-5-vectores, fase-7-qdrant.

### Clase Evaluador

- **Responsabilidad:** leer el examen, pasar cada pregunta por el buscador y las preguntas sin respuesta por el redactor, en cada modo de búsqueda, y resumir los resultados.
- **Recibe en el constructor:** el `Buscador`, el `Redactor`, el `Sincronizador` (para revisar Qdrant antes de medir), la configuración (de donde lee `ConnectionStrings:Rag` y `Rag:ArchivoExamen`) y el entorno, para resolver la ruta del examen contra la carpeta del proyecto.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-3-medicion.
- **Cambió en:** fase-5-vectores (corre todos los modos de `Buscador.Modos`), fase-7-qdrant (revisa Qdrant antes de medir).

### Evaluador.CorrerAsync(ct)

- **Qué hace:** corre el examen completo en cada modo de `Buscador.Modos`.
  1. Avisa si el examen nombra un documento que no está cargado, espera hasta un minuto a que el Full-Text termine de indexar y avisa si hay fragmentos sin vector.
  2. Revisa Qdrant con Sincronizador.EstadoAsync. Corta el examen si Qdrant no responde, si la colección no existe o está vacía, o si sus vectores son de otra base: los modos `vectores-qdrant` e `hibrida` medirían otra cosa. Si solo le faltan o le sobran algunos fragmentos, lo avisa. Cuando corta porque no hay vectores, dice qué falta: `POST /vectores` si hay fragmentos sin vector, o `POST /indexar` si no hay ningún fragmento.
  3. Corre cada modo de `Buscador.Modos` con Evaluador.CorrerModoAsync. Si un modo falla a la mitad, lo avisa y sigue con los demás: no se pierde lo que ya se midió.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `InformeEvaluacion`: una fila por modo, el detalle por pregunta, la lista de fallas y los avisos.
- **Efectos:** lee SQL Server y llama a Ollama y a Qdrant: el vector de cada pregunta en los modos por vectores, y Qwen para las preguntas sin respuesta, que quedan registradas en `rag.Consulta`.
- **La llaman:** el endpoint `POST /evaluar`.
- **Desde:** fase-3-medicion.
- **Cambió en:** fase-5-vectores (todos los modos, y las preguntas sin respuesta en cada uno), fase-7-qdrant (revisa Qdrant antes de medir, y un modo que falla no corta el examen).

### Evaluador.CorrerModoAsync(modo, examen, detalle, fallas, avisos, ct)

- **Qué hace:** corre el examen en un modo.
  1. Pasa cada pregunta con respuesta por el buscador y anota en qué puesto apareció un documento correcto.
  2. Pasa cada pregunta sin respuesta por el redactor y cuenta cuántas veces dijo la frase fija. Se mide por modo porque por vectores la búsqueda casi nunca vuelve vacía, y el "no sé" lo decide Qwen.
  3. Si en alguna búsqueda la híbrida tuvo que seguir sin Qdrant, lo avisa: esa fila no mide Qdrant.
- **Recibe:** el modo, el examen, las listas donde anota el detalle, las fallas y los avisos, y el token de cancelación. Evaluador.CorrerAsync le pasa listas propias de este modo, y las suma a las del examen solo si el modo terminó.
- **Devuelve:** un `ResultadoModo`, con las preguntas sin respuesta.
- **Efectos:** lee SQL Server y llama a Ollama y a Qdrant, según el modo; las preguntas sin respuesta quedan registradas en `rag.Consulta`.
- **La llaman:** Evaluador.CorrerAsync.
- **Desde:** fase-7-qdrant. Hasta la fase 6, esto estaba adentro de Evaluador.CorrerAsync.
- **Cambió en:** nada todavía.

### Evaluador.Puntuar(modo, detalle)

- **Qué hace:** resume un modo: cuántas preguntas tuvieron un documento correcto en el primer lugar, cuántas entre los cinco primeros, y el tiempo promedio. Las preguntas sin respuesta quedan en 0: las completa Evaluador.CorrerModoAsync con `with`, que copia el record cambiando solo esos dos campos.
- **Recibe:** el nombre del modo y el detalle de cada pregunta, con su puesto (0 si no apareció).
- **Devuelve:** un `ResultadoModo`.
- **Efectos:** ninguno.
- **La llaman:** Evaluador.CorrerModoAsync.
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
- **Cambió en:** fase-4-carga, fase-5-vectores, fase-7-qdrant.

### Clase Indexador

- **Responsabilidad:** el camino de un PDF hasta la base, y decidir cuáles hacen ese camino.
- **Recibe en el constructor:** el `Sincronizador`, para poner Qdrant al día después de cargar, y la configuración, de donde lee `ConnectionStrings:Rag`, `Rag:CarpetaDocumentos`, `Rag:PalabrasPorFragmento` y `Rag:Solapamiento`.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-4-carga (carga incremental), fase-7-qdrant (recibe el sincronizador).

### Indexador.IndexarCarpetaAsync(ct)

- **Qué hace:** pone la base al día con la carpeta.
  1. Si la carpeta no existe, no toca la base y lo informa.
  2. Calcula la huella de cada PDF, lee las huellas de la base y le pide el plan a Planificador.Armar.
  3. Borra los documentos que ya no están en la carpeta. Si la carpeta no tiene ningún PDF, no borra nada y lo avisa.
  4. Carga los nuevos y los cambiados. Los que siguen igual no se vuelven a leer.
  5. Si un PDF falla, lo anota y sigue con el siguiente.
  6. Pone Qdrant al día con Sincronizador.PonerAlDiaAsync: los fragmentos borrados en SQL también se borran de Qdrant. Los nuevos todavía no tienen vector: los sube `/vectores`.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `InformeIndexado`: cuántos PDF se cargaron, cuántos quedaron igual y cuántos se borraron, cuántos fragmentos se guardaron, el tiempo como horas:minutos:segundos y el detalle. El detalle tiene una línea por cada PDF cargado, borrado, sin texto o con error; si no hubo nada que hacer, una sola línea `SIN CAMBIOS`.
- **Efectos:** lee los PDF de la carpeta y `rag.Documento`; borra filas de `rag.Documento` (y en cascada de `rag.Fragmento`) e inserta las nuevas; borra de Qdrant los puntos de los fragmentos que ya no están.
- **La llaman:** el endpoint `POST /indexar`.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-4-carga (carga solo lo nuevo o cambiado y borra lo que ya no está, en lugar de recargar todo), fase-7-qdrant (pone Qdrant al día).

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
- **La llaman:** Indexador.IndexarCarpetaAsync, Vectorizador.CalcularPendientesAsync y Sincronizador.ReconstruirAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-5-vectores (pasa a ser pública, para que la use el Vectorizador).

## RagBasico.Api\ManejadorDeErrores.cs

- **Responsabilidad:** que ningún error llegue como un 500 genérico: cada error que nadie atrapó sale como un problema con título, detalle y el código HTTP que corresponde, en el formato estándar ProblemDetails (`application/problem+json`).
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

### Clase ManejadorDeErrores

- **Responsabilidad:** implementa `IExceptionHandler`, la interfaz de ASP.NET Core para atender los errores que nadie atrapó.
- **Recibe en el constructor:** el log.
- **La registra:** Program.cs, con `AddExceptionHandler<ManejadorDeErrores>`; `UseExceptionHandler` la pone en el camino de cada pedido.
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

### ManejadorDeErrores.TryHandleAsync(contexto, error, ct)

- **Qué hace:** anota el error en el log, con todo su detalle, y responde el problema que arma ManejadorDeErrores.Describir.
- **Recibe:** el contexto del pedido, el error y el token de cancelación.
- **Devuelve:** `true`: el error quedó atendido y ASP.NET Core no hace nada más.
- **Efectos:** escribe en el log y escribe la respuesta HTTP.
- **La llaman:** ASP.NET Core, cuando un endpoint lanza un error que nadie atrapó.
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

### ManejadorDeErrores.Describir(error)

- **Qué hace:** traduce un error a un problema:
  - `ErrorDeServicio`: 503 "Ollama no responde" o "Qdrant no responde", o 502 si respondió con un error.
  - `SqlException`: 503 "SQL Server no responde o devolvió un error", con qué revisar.
  - Tiempo de espera vencido (`TaskCanceledException` con un `TimeoutException` adentro): 504 "Un servicio tardó demasiado".
  - `BadHttpRequestException`, el error de ASP.NET Core cuando no puede leer el pedido (un JSON roto, un parámetro que falta): su propio código, 400, con "El pedido no es válido".
  - Cualquier otro, incluido un `ArgumentException`: 500 "Error inesperado", con su mensaje. Un `ArgumentException` no es culpa de quien pregunta: lo que manda el cliente ya lo controlan Validador.RevisarPregunta y Buscador.ElegirModo, así que si aparece es de la API, por ejemplo una configuración inválida.
- **Recibe:** el error.
- **Devuelve:** un `ProblemDetails` con el código, el título y el detalle.
- **Efectos:** ninguno.
- **La llaman:** ManejadorDeErrores.TryHandleAsync.
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

### ManejadorDeErrores.Problema(codigo, titulo, detalle)

- **Qué hace:** arma un `ProblemDetails` con los tres datos.
- **Recibe:** el código HTTP, el título y el detalle.
- **Devuelve:** el `ProblemDetails`.
- **Efectos:** ninguno.
- **La llaman:** ManejadorDeErrores.Describir.
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

## RagBasico.Api\Modelos.cs

- **Responsabilidad:** los records que viajan entre las piezas: lo que llega en un pedido y lo que devuelve cada servicio. Un record es una clase chica de solo datos, que se compara por su contenido.
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores, fase-7-qdrant.

| Record | Campo | Tipo | Para qué |
|---|---|---|---|
| `PedidoPregunta` | `Pregunta` | `string` | La pregunta que llega en el cuerpo de `POST /preguntar` |
| `PedidoPregunta` | `Modo` | `string?` | El modo de búsqueda: `palabras`, `vectores`, `vectores-qdrant` o `hibrida`. Si falta, `hibrida` |
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
| `InformeIndexado` | `Detalle` | `List<string>` | Una línea por PDF cargado (`NUEVO` o `CAMBIÓ`, con `TEXTO DUDOSO` si corresponde), borrado (`BORRADO`), sin texto (`SIN TEXTO`) o con error (`ERROR`), o una sola línea `SIN CAMBIOS`; y al final la línea de Qdrant (`QDRANT:` o `AVISO:`) |
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
| `InformeVectores` | `Detalle` | `List<string>` | Una línea por fragmento que falló, o una sola línea `SIN CAMBIOS`; y al final la línea de Qdrant |
| `Fuente` | `Numero` | `int` | El número con que la cita el modelo: `[1]`, `[2]`... |
| `Fuente` | `Archivo`, `FragmentoId` | `string`, `int` | El PDF y el fragmento exacto |
| `RespuestaRag` | `Texto` | `string` | La respuesta, o la frase de "no encontré" |
| `RespuestaRag` | `Fuentes` | `List<Fuente>` | Las fuentes, en el orden en que se le pasaron al modelo. Vacía si no encontró |
| `RespuestaRag` | `MsBusqueda`, `MsModelo` | `int` | Lo que tardó la búsqueda y lo que tardó el modelo, medido desde la API |
| `RespuestaRag` | `MsCarga`, `MsLectura`, `MsEscritura` | `int` | Los tiempos de Ollama |
| `RespuestaRag` | `Aviso` | `string` | Vacío, salvo que la híbrida haya seguido sin Qdrant: entonces lo dice |
| `ResultadoBusqueda` | `Fragmentos` | `List<Fragmento>` | Los fragmentos encontrados, del más relevante al menos relevante |
| `ResultadoBusqueda` | `Aviso` | `string` | Vacío, salvo que la híbrida haya seguido sin Qdrant |
| `PuntoQdrant` | `Id` | `int` | El id del punto en Qdrant, que es el número de fragmento de SQL |
| `PuntoQdrant` | `Vector` | `float[]` | El vector, de 1024 números |
| `PuntoQdrant` | `DocumentoId` | `int` | El número de documento, que se guarda como dato extra (payload) |
| `InfoColeccion` | `Estado` | `string` | El color que informa Qdrant: `green` si está todo listo, `yellow` si está optimizando |
| `InfoColeccion` | `Puntos`, `VectoresIndexados` | `int` | Cuántos puntos tiene la colección, y cuántos vectores están en el índice HNSW |
| `ComparacionIds` | `Faltan`, `Sobran` | `List<int>` | Los números de fragmento que están en SQL y no en Qdrant, y los que están en Qdrant y no en SQL |
| `EstadoQdrant` | `Iguales` | `bool` | Si SQL y Qdrant tienen los mismos números de fragmento y la muestra de vectores coincide |
| `EstadoQdrant` | `Coleccion`, `EstadoColeccion` | `string` | El nombre de la colección y su color en Qdrant |
| `EstadoQdrant` | `ConVectorEnSql`, `PuntosEnQdrant`, `VectoresIndexados` | `int` | Cuántos fragmentos con vector hay en SQL, cuántos puntos en Qdrant y cuántos en el índice HNSW |
| `EstadoQdrant` | `CantidadFaltan`, `CantidadSobran` | `int` | Cuántos números faltan en Qdrant y cuántos sobran |
| `EstadoQdrant` | `Faltan`, `Sobran` | `List<int>` | Los primeros 20 de cada lista, para buscarlos en SSMS |
| `EstadoQdrant` | `MuestraCoincide` | `bool?` | Si los vectores de la muestra son los mismos en los dos lados; `null` si no hubo nada que comparar |
| `EstadoQdrant` | `Mensaje` | `string` | Si están iguales y, si no, qué hacer |
| `InformeQdrant` | `Subidos`, `Borrados` | `int` | Cuántos puntos se subieron y cuántos tenía la colección antes de borrarla |
| `InformeQdrant` | `Tiempo` | `string` | Lo que tardó, como horas:minutos:segundos |
| `InformeQdrant` | `Detalle` | `List<string>` | Qué se hizo, paso por paso |
| `Chequeo` | `Nombre`, `Estado`, `Detalle` | `string` | Qué se revisó, cómo dio (`ok`, `aviso` o `error`) y el detalle |
| `InformeSalud` | `Estado` | `string` | El estado general: `error` si algún chequeo dio error, `aviso` si alguno dio aviso, `ok` si no |
| `InformeSalud` | `Chequeos` | `List<Chequeo>` | Los cinco chequeos |
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
| `InformeEvaluacion` | `Fallas`, `Avisos` | `List<string>` | Las preguntas que fallaron, y los avisos (examen vacío, documentos no cargados, Full-Text pendiente, fragmentos sin vector, Qdrant distinto de SQL o apagado) |

## RagBasico.Api\Ollama.cs

- **Responsabilidad:** hablar con Ollama por HTTP. Manda el mismo JSON que se mandaría con Postman y devuelve lo que contesta el modelo.
- **Contiene:** la clase `Ollama`, con la constante pública `Dimension` (1024, el tamaño de cada vector), y cinco records privados para leer las respuestas: `RespuestaChat` (el mensaje y los tiempos de `/api/chat`), `MensajeChat` (el texto que escribió el modelo), `RespuestaEmbed` (los vectores de `/api/embed`), `RespuestaModelos` y `ModeloInstalado` (los modelos de `/api/tags`).
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-5-vectores, fase-7-qdrant, fase-8-validaciones.

### Clase Ollama

- **Responsabilidad:** ser el único lugar del código que conoce el formato de la API de Ollama.
- **Recibe en el constructor:** el `HttpClient` que arma Program.cs, con la dirección y el tiempo de espera de la configuración, y la configuración, de donde lee `Ollama:ModeloChat`, `Ollama:ModeloEmbeddings`, `Ollama:InstruccionPregunta` y `Ollama:MantenerCargado`.
- **La registra:** Program.cs, con `AddHttpClient<Ollama>`, que crea un cliente tipado nuevo en cada pedido.
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-5-vectores (los embeddings), fase-7-qdrant (la lista de modelos y el calentamiento).

### Ollama.ChatAsync(sistema, usuario, ct)

- **Qué hace:** le pide una respuesta al modelo de chat. Manda `stream: false`, para recibir la respuesta entera de una vez, y `think: false`, porque qwen3.5 piensa antes de responder si no se le dice lo contrario. Si el texto trae un bloque de razonamiento, lo descarta.
- **Recibe:** el mensaje de sistema (las reglas), el mensaje del usuario y el token de cancelación del pedido.
- **Devuelve:** un `RespuestaModelo` con el texto y los tiempos que informa Ollama: carga del modelo, lectura de la pregunta y escritura de la respuesta, en milisegundos, y la cantidad de tokens leídos y escritos.
- **Efectos:** un `POST /api/chat` a Ollama.
- **La llaman:** Redactor.ResponderAsync y Ollama.CalentarAsync.
- **Desde:** fase-1-ollama.
- **Cambió en:** nada todavía.

### Ollama.EmbeddingsAsync(textos, ct)

- **Qué hace:** calcula el vector de cada texto con el modelo de embeddings, todos en un solo pedido a `/api/embed`. Los fragmentos se mandan así, sin instrucción, como dice la ficha del modelo. Controla que vuelva un vector por texto y que cada uno tenga 1024 números; si no, lanza un error que lo dice.
- **Recibe:** la lista de textos y el token de cancelación.
- **Devuelve:** un vector (`float[]` de 1024 números) por texto, en el mismo orden.
- **Efectos:** un `POST /api/embed` a Ollama.
- **La llaman:** Ollama.EmbeddingPreguntaAsync, Ollama.CalentarAsync, Vectorizador.CalcularYGuardarAsync y Salud.RevisarDimensionAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Ollama.EmbeddingPreguntaAsync(pregunta, ct)

- **Qué hace:** calcula el vector de una pregunta. Le antepone la instrucción de `Ollama:InstruccionPregunta` con el formato de la ficha del modelo: `Instruct: `, la instrucción, un salto de línea y `Query:` pegado a la pregunta. La instrucción va en inglés, como recomienda la ficha. Como solo cambia el vector de la pregunta, probarla en castellano no obliga a recalcular los fragmentos.
- **Recibe:** la pregunta y el token de cancelación.
- **Devuelve:** el vector de la pregunta.
- **Efectos:** un `POST /api/embed` a Ollama, por medio de Ollama.EmbeddingsAsync.
- **La llaman:** Buscador.PorVectoresAsync, Buscador.PorVectoresQdrantAsync y Buscador.VectoresConRespaldoAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Ollama.ListarModelosAsync(ct)

- **Qué hace:** pide `GET /api/tags` y devuelve los nombres de los modelos que tiene instalados Ollama.
- **Recibe:** el token de cancelación.
- **Devuelve:** la lista de nombres, como `qwen3.5:4b`.
- **Efectos:** un pedido a Ollama.
- **La llaman:** Salud.RevisarModelosAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Ollama.CalentarAsync(ct)

- **Qué hace:** carga los dos modelos en la memoria de la placa de video con un pedido mínimo a cada uno: el vector de "hola" y una respuesta de una palabra. Ollama los deja cargados el tiempo de `Ollama:MantenerCargado`, así la primera pregunta de la demo no espera la carga, que puede llevar 30 segundos.
- **Recibe:** el token de cancelación.
- **Devuelve:** nada.
- **Efectos:** un `POST /api/embed` y un `POST /api/chat` a Ollama.
- **La llaman:** Program.cs, en segundo plano, cuando la API termina de arrancar.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Ollama.VerificarAsync(respuesta, ct)

- **Qué hace:** si Ollama contestó con un error, lanza una excepción con su mensaje real, por ejemplo "model not found", en lugar de un error genérico.
- **Recibe:** la respuesta HTTP de Ollama y el token de cancelación.
- **Devuelve:** nada. Si la respuesta es un error, lanza un `ErrorDeServicio` de Ollama con código 502.
- **Efectos:** lee el cuerpo de la respuesta cuando es un error.
- **La llaman:** Ollama.ChatAsync, Ollama.EmbeddingsAsync y Ollama.ListarModelosAsync.
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-8-validaciones (lanza un `ErrorDeServicio` en lugar de un `InvalidOperationException`).

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

- **Responsabilidad:** el punto de entrada de la API: arma la aplicación, registra los servicios, controla que estén la base y los modelos, carga los modelos, atiende los errores, sirve la página de la demo, declara los endpoints y la deja escuchando.
- **Contiene:** instrucciones de nivel superior, sin clase: el compilador arma solo el método `Main`. Se lee en nueve bloques:
  - **Armado:** `WebApplication.CreateBuilder(args)` lee `appsettings.json`, las variables de entorno y los argumentos.
  - **Servicios:** `AddHttpClient<Ollama>` registra el cliente de Ollama con `Ollama:Url` y `Ollama:SegundosDeEspera`, y `AddHttpClient<Qdrant>` el de Qdrant con `Qdrant:Url` y `Qdrant:SegundosDeEspera`, que es corto: si Qdrant no responde, la híbrida sigue con SQL. Delante de cada cliente, `AddHttpMessageHandler` pone un `DetectorDeCaidas`, que dice qué servicio no responde. `AddScoped` registra el `Sincronizador`, el `Indexador`, el `Vectorizador`, el `Buscador`, el `Redactor`, el `Evaluador` y la `Salud`: un objeto nuevo de cada uno por pedido. Cuando un endpoint o un constructor pide uno, ASP.NET Core se lo crea: eso es la inyección de dependencias. `AddProblemDetails` y `AddExceptionHandler<ManejadorDeErrores>` hacen que los errores salgan como ProblemDetails, y `RouteHandlerOptions.ThrowOnBadRequest` hace que un pedido que no se puede leer, como un JSON roto, llegue al manejador con su motivo en cualquier entorno, no solo en Development.
  - **Chequeo al arrancar:** antes de escuchar, Salud.RevisarArranqueAsync controla la base y los dos modelos. Si falta algo, o si la configuración es inválida (por ejemplo, un tiempo de espera en 0), escribe en el log por qué, deja el código de salida en 1 y termina: la API no arranca. Qdrant no se controla, porque la demo sigue sin él. Con `Rag:ChequearAlArrancar` en `false`, arranca igual.
  - **Control de la carpeta:** muestra en el log la carpeta de PDF que va a usar, o avisa que no existe y qué variable de entorno crear.
  - **Calentamiento:** cuando la API ya está escuchando (`ApplicationStarted`), carga los dos modelos en Ollama en segundo plano con Ollama.CalentarAsync, así la primera pregunta de la demo no tarda 30 segundos. `Task.Run` lo corre sin hacer esperar el arranque; si falla, queda en el log.
  - **Errores:** `UseExceptionHandler` va primero en el camino de cada pedido: si algo lanza un error que nadie atrapó, lo atiende el `ManejadorDeErrores`. `UseStatusCodePages` le pone un cuerpo ProblemDetails a las respuestas de error que salen vacías, como un 404 (una ruta que no existe), un 405 (un método que no corresponde) o un 415 (un cuerpo que no es JSON).
  - **Página:** `UseDefaultFiles` hace que la raíz, `/`, devuelva `wwwroot\index.html`, y `UseStaticFiles` sirve los archivos de `wwwroot`. Por eso la información del proyecto está en `/info`.
  - **Endpoints:** cada `app.MapGet(...)` o `app.MapPost(...)` asocia una ruta con la función que la atiende. `GET /buscar` y `POST /preguntar` controlan la pregunta con Validador.RevisarPregunta (con el máximo de `Rag:LargoMaximoPregunta`) y el modo con Buscador.ElegirModo; si algo no sirve, devuelven `Results.Problem` con código 400 y el motivo.
  - **Arranque:** `app.Run()` deja la API escuchando en la dirección de `launchSettings.json` hasta que se la frena con Ctrl+C.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-8-validaciones.

### GET /info

- **Recibe:** nada.
- **Devuelve:** un JSON con el nombre del proyecto, la fase y la versión de .NET. El proyecto y la fase salen de la configuración (`Rag:Proyecto` y `Rag:Fase`).
- **La atiende:** una función sin nombre (lambda) declarada en `Program.cs` con `app.MapGet("/info", ...)`.
- **Desde:** fase-0-base.
- **Cambió en:** fase-6-hibrida (pasa de `/` a `/info`: la raíz muestra la página de la demo).

### POST /indexar

- **Recibe:** nada. Los PDF salen de la carpeta `Rag:CarpetaDocumentos`.
- **Devuelve:** el `InformeIndexado` de la carga: cuántos PDF se cargaron, cuántos quedaron igual y cuántos se borraron, los fragmentos, el tiempo y el detalle, con la línea de Qdrant al final. La segunda vez seguida dice `SIN CAMBIOS`.
- **La atiende:** una función de `Program.cs` que llama a Indexador.IndexarCarpetaAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-4-carga (pone la base al día con la carpeta en lugar de recargar todo), fase-7-qdrant (también borra de Qdrant lo que ya no está).

### POST /vectores

- **Recibe:** nada.
- **Devuelve:** el `InformeVectores`: cuántos vectores se calcularon, cuántos fallaron, cuántos fragmentos quedan sin vector, cuántos fragmentos por segundo, el tiempo y el detalle, con la línea de Qdrant al final. La segunda vez seguida dice `SIN CAMBIOS`.
- **La atiende:** una función de `Program.cs` que llama a Vectorizador.CalcularPendientesAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** fase-7-qdrant (sube los vectores nuevos a Qdrant, y los que le faltaban si estuvo apagado).

### GET /buscar

- **Recibe:** la pregunta en el parámetro `q` de la dirección y, si se quiere, el modo en `modo`: `palabras`, `vectores` (en SQL), `vectores-qdrant` o `hibrida`, que es el modo por defecto. Por ejemplo: `/buscar?q=requisitos reintegro&modo=vectores-qdrant`.
- **Devuelve:** un `ResultadoBusqueda`: los fragmentos encontrados con ese modo, hasta `Rag:TopK`, y el aviso si la híbrida siguió sin Qdrant. Si falta `q`, si la pregunta no sirve (vacía, más larga que `Rag:LargoMaximoPregunta`, sin letras) o si el modo no existe, un error 400 con el motivo. Con `vectores-qdrant` y Qdrant apagado, un 503 que dice que Qdrant no responde.
- **La atiende:** una función de `Program.cs` que controla el modo con Buscador.ElegirModo y llama a Buscador.BuscarAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-5-vectores (el parámetro `modo`), fase-6-hibrida (la híbrida, que pasa a ser el modo por defecto), fase-7-qdrant (el modo `vectores-qdrant`, y devuelve un objeto con el aviso en lugar de la lista sola), fase-8-validaciones (valida la pregunta, y los errores salen como ProblemDetails).

### POST /preguntar

- **Recibe:** un JSON con la pregunta y, si se quiere, el modo: `{ "pregunta": "...", "modo": "palabras" }`, que ASP.NET Core convierte en un `PedidoPregunta`. Sin modo, usa la híbrida.
- **Devuelve:** el `RespuestaRag`: la respuesta de Qwen con citas `[n]`, las fuentes, los tiempos y el aviso si la híbrida siguió sin Qdrant. Si los documentos no alcanzan, la frase "No encontré esa información en los documentos." sin fuentes. Si la pregunta no sirve o el modo no existe, un error 400 con el motivo; si un servicio no responde, un 503 que dice cuál.
- **La atiende:** una función de `Program.cs` que controla el modo con Buscador.ElegirModo y llama a Redactor.ResponderAsync.
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-2-rag-minimo (responde con los documentos, no con lo que sabe Qwen), fase-5-vectores (el modo), fase-6-hibrida (la híbrida por defecto), fase-7-qdrant (el aviso), fase-8-validaciones (valida la pregunta, y los errores salen como ProblemDetails).

### POST /evaluar

- **Recibe:** nada. El examen sale de `Rag:ArchivoExamen`.
- **Devuelve:** el `InformeEvaluacion`: una fila por modo con "1er lugar", "entre los 5", ms promedio y las preguntas sin respuesta bien contestadas; el detalle por pregunta, las fallas y los avisos. Si Qdrant no responde, solo el aviso: el examen se corta.
- **La atiende:** una función de `Program.cs` que llama a Evaluador.CorrerAsync.
- **Desde:** fase-3-medicion.
- **Cambió en:** fase-5-vectores (compara todos los modos de `Buscador.Modos`), fase-7-qdrant (revisa Qdrant antes de medir).

### GET /salud

- **Recibe:** nada.
- **Devuelve:** el `InformeSalud`: el estado general y cinco chequeos (SQL Server, Full-Text, modelos de Ollama, dimensión de los vectores y Qdrant). Con estado `error`, el código HTTP es 503; con `aviso` u `ok`, 200, porque la demo anda.
- **La atiende:** una función de `Program.cs` que llama a Salud.RevisarAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### GET /qdrant/estado

- **Recibe:** nada.
- **Devuelve:** el `EstadoQdrant`: si SQL y Qdrant tienen los mismos números de fragmento, cuántos faltan y sobran, la muestra de vectores, cuántos vectores están en el índice HNSW y qué hacer.
- **La atiende:** una función de `Program.cs` que llama a Sincronizador.EstadoAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### POST /qdrant/reconstruir

- **Recibe:** nada.
- **Devuelve:** el `InformeQdrant`: cuántos puntos se subieron, cuántos había, el tiempo y el detalle.
- **La atiende:** una función de `Program.cs` que llama a Sincronizador.ReconstruirAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

## RagBasico.Api\Properties\launchSettings.json

- **Responsabilidad:** dice cómo arranca la API cuando se la corre con `dotnet run` o con F5 en Visual Studio: la dirección, el puerto, el entorno y si Visual Studio abre el navegador.
- **Contiene:** un solo perfil, `http`: escucha en `http://127.0.0.1:508N`, donde N es el número de la fase, con el entorno `Development`, y con F5 Visual Studio abre el navegador (`dotnet run` no lo abre: hay que abrir la dirección a mano). Así dos fases pueden correr a la vez. El template traía también un perfil `https` con puertos al azar, que se sacó porque chocaría entre fases.
- **Por qué 127.0.0.1:** es la misma dirección que usan Ollama y Qdrant, que escuchan solo en IPv4, así todo el proyecto usa una sola. La consecuencia es que la API también escucha solo en IPv4: se la llama con `http://127.0.0.1:508N`, como hacen el archivo `.http` y el navegador que abre el perfil. Con `localhost`, los clientes de .NET (PowerShell, `WebClient`, `HttpClient`) prueban primero IPv6 y tardan unos 2 segundos más en conectarse.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-8-validaciones.

## RagBasico.Api\Qdrant.cs

- **Responsabilidad:** hablar con Qdrant por HTTP, con JSON armado a mano, como con Ollama: sin el SDK de Qdrant, para que se vea qué se manda.
- **Contiene:** la clase `Qdrant` y su opción privada `SinNulos`, que manda los pedidos sin los campos en `null`.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones.

### Clase Qdrant

- **Responsabilidad:** ser el único lugar del código que conoce la API de Qdrant. Las respuestas se leen con `JsonDocument`, que recorre el JSON sin declarar una clase por cada respuesta.
- **Recibe en el constructor:** el `HttpClient` que arma Program.cs, con `Qdrant:Url` y `Qdrant:SegundosDeEspera`, y la configuración, de donde lee `Qdrant:Coleccion`. Tiene la propiedad `Coleccion`, con ese nombre.
- **La registra:** Program.cs, con `AddHttpClient<Qdrant>`.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.LeerVersionAsync(ct)

- **Qué hace:** pide `GET /` y lee la versión de Qdrant. Sirve para saber si está andando.
- **Recibe:** el token de cancelación.
- **Devuelve:** la versión, por ejemplo `1.20.2`.
- **Efectos:** un pedido a Qdrant.
- **La llaman:** Salud.RevisarQdrantAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.ExisteColeccionAsync(ct)

- **Qué hace:** pregunta si existe la colección, con `GET /collections/<colección>/exists`.
- **Recibe:** el token de cancelación.
- **Devuelve:** `true` si existe.
- **Efectos:** un pedido a Qdrant.
- **La llaman:** Sincronizador.EstadoAsync, Sincronizador.PonerAlDiaAsync, Sincronizador.ReconstruirAsync y Salud.RevisarQdrantAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.CrearColeccionAsync(ct)

- **Qué hace:** crea la colección con `PUT /collections/<colección>`: vectores de 1024 números comparados por coseno, y los dos umbrales bajos. `indexing_threshold` en 10 KB hace que el índice HNSW se arme enseguida, y `full_scan_threshold` en 10 KB hace que la búsqueda lo use. Los dos se miden en kilobytes y cada vector ocupa 4 KB: con los valores por defecto, unos 2.500 vectores, Qdrant buscaría recorriendo todos los vectores, igual que SQL.
- **Recibe:** el token de cancelación.
- **Devuelve:** nada.
- **Efectos:** crea la colección en Qdrant.
- **La llaman:** Sincronizador.PonerAlDiaAsync y Sincronizador.ReconstruirAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.BorrarColeccionAsync(ct)

- **Qué hace:** borra la colección con todos sus puntos, con `DELETE /collections/<colección>`.
- **Recibe:** el token de cancelación.
- **Devuelve:** lo que contestó Qdrant: `true` si dice que la borró. No alcanza con creerle: Sincronizador.ReconstruirAsync comprueba después que ya no exista.
- **Efectos:** borra la colección en Qdrant.
- **La llaman:** Sincronizador.ReconstruirAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.LeerInfoAsync(ct)

- **Qué hace:** lee el estado de la colección con `GET /collections/<colección>`: el color (`green` si está todo listo, `yellow` si está optimizando), cuántos puntos tiene (`points_count`) y cuántos vectores están en el índice HNSW (`indexed_vectors_count`).
- **Recibe:** el token de cancelación.
- **Devuelve:** un `InfoColeccion`.
- **Efectos:** un pedido a Qdrant.
- **La llaman:** Sincronizador.EstadoAsync, Sincronizador.ReconstruirAsync y Salud.RevisarQdrantAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.SubirAsync(puntos, ct)

- **Qué hace:** sube puntos con `PUT /collections/<colección>/points?wait=true`: el número de fragmento como id, el vector, y el número de documento como dato extra (payload), para poder ver de qué documento es cada punto. Si el id ya existe, lo pisa. Con `wait=true`, Qdrant contesta cuando los puntos ya están guardados.
- **Recibe:** la lista de `PuntoQdrant` y el token de cancelación.
- **Devuelve:** nada.
- **Efectos:** escribe en la colección.
- **La llaman:** Sincronizador.SubirAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.BorrarPuntosAsync(ids, ct)

- **Qué hace:** borra puntos por su id, que es el número de fragmento, con `POST /collections/<colección>/points/delete?wait=true`.
- **Recibe:** los ids y el token de cancelación.
- **Devuelve:** nada.
- **Efectos:** borra puntos de la colección.
- **La llaman:** Sincronizador.PonerAlDiaAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.ListarIdsAsync(ct)

- **Qué hace:** lista los ids de todos los puntos recorriendo la colección de a 1.000 con `POST /collections/<colección>/points/scroll`, sin pedir vectores ni payload. Cada respuesta dice desde qué id sigue la página siguiente (`next_page_offset`), o `null` cuando no hay más.
- **Recibe:** el token de cancelación.
- **Devuelve:** la lista de ids, en orden.
- **Efectos:** un pedido a Qdrant por cada 1.000 puntos.
- **La llaman:** Sincronizador.EstadoAsync y Sincronizador.PonerAlDiaAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.LeerVectorAsync(id, ct)

- **Qué hace:** lee el vector guardado de un punto con `POST /collections/<colección>/points`. Qdrant lo devuelve normalizado, porque la colección compara por coseno.
- **Recibe:** el id y el token de cancelación.
- **Devuelve:** el vector, o `null` si el punto no existe.
- **Efectos:** un pedido a Qdrant.
- **La llaman:** Sincronizador.MuestraCoincideAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.BuscarAsync(vector, topK, ct)

- **Qué hace:** busca los puntos más cercanos a un vector con el índice HNSW, con `POST /collections/<colección>/points/query`.
- **Recibe:** el vector de la pregunta, cuántos devolver y el token de cancelación.
- **Devuelve:** los ids de los puntos, del más cercano al más lejano.
- **Efectos:** un pedido a Qdrant.
- **La llaman:** Buscador.PorVectoresQdrantAsync y Buscador.VectoresConRespaldoAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.LeerResultadoAsync(respuesta, ct)

- **Qué hace:** controla la respuesta con Qdrant.VerificarAsync y devuelve su campo `result`, donde Qdrant pone siempre el resultado. Lo copia con `Clone`, porque el documento JSON se libera al salir del `using`.
- **Recibe:** la respuesta HTTP y el token de cancelación.
- **Devuelve:** el contenido de `result`, como `JsonElement`.
- **Efectos:** lee el cuerpo de la respuesta.
- **La llaman:** las funciones de Qdrant que leen un resultado.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.VerificarAsync(respuesta, ct)

- **Qué hace:** si Qdrant contestó con un error, lanza una excepción con su mensaje real, por ejemplo que la colección no existe.
- **Recibe:** la respuesta HTTP y el token de cancelación.
- **Devuelve:** nada. Si la respuesta es un error, lanza un `ErrorDeServicio` de Qdrant con código 502.
- **Efectos:** lee el cuerpo de la respuesta cuando es un error.
- **La llaman:** todas las funciones de Qdrant que hacen un pedido.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones (lanza un `ErrorDeServicio` en lugar de un `InvalidOperationException`).

### Qdrant.LeerEntero(elemento, campo)

- **Qué hace:** lee un número entero de un campo del JSON, o 0 si el campo no está o viene en `null`.
- **Recibe:** el elemento JSON y el nombre del campo.
- **Devuelve:** el número.
- **Efectos:** ninguno.
- **La llaman:** Qdrant.LeerInfoAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

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
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-8-validaciones.

## RagBasico.Api\Redactor.cs

- **Responsabilidad:** la "G" de RAG: armar la respuesta con los fragmentos encontrados.
- **Contiene:** la clase `Redactor`, con dos constantes: `SinRespuesta`, la única frase para "no encontré", y `Reglas`, el mensaje de sistema con las cinco reglas para Qwen (usar solo los fragmentos, citar con `[n]`, la frase exacta cuando no alcanza, e ignorar instrucciones que vengan dentro de los documentos).
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-3-medicion, fase-5-vectores, fase-7-qdrant.

### Clase Redactor

- **Responsabilidad:** buscar, pedirle la respuesta a Qwen, armar la respuesta con sus fuentes y registrar la consulta.
- **Recibe en el constructor:** el `Buscador`, el cliente de `Ollama`, el log y la configuración, de donde lee `ConnectionStrings:Rag` y `Rag:TopK`.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-3-medicion (recibe el log y registra cada consulta).

### Redactor.ResponderAsync(pregunta, modo, ct)

- **Qué hace:** busca los fragmentos de la pregunta con el modo pedido y le pide la respuesta a Qwen. Si no hay fragmentos, responde la frase fija sin llamar al modelo; por vectores casi nunca pasa, así que el "no sé" lo decide Qwen. Si Qwen responde la frase de "no encontré", la devuelve sin fuentes. Al final registra la consulta, con el modo.
- **Recibe:** la pregunta, el modo de búsqueda y el token de cancelación.
- **Devuelve:** un `RespuestaRag`: el texto, las fuentes numeradas como las cita el modelo, los tiempos y el aviso de la búsqueda, si la híbrida siguió sin Qdrant.
- **Efectos:** busca con el buscador (SQL Server, Ollama y Qdrant según el modo), llama a Ollama para redactar y escribe una fila en `rag.Consulta`.
- **La llaman:** el endpoint `POST /preguntar` y Evaluador.CorrerModoAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-3-medicion (registra la consulta), fase-5-vectores (recibe el modo), fase-7-qdrant (pasa el aviso de la búsqueda).

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
- **La llaman:** Redactor.ResponderAsync y Evaluador.CorrerModoAsync.
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

## RagBasico.Api\Rrf.cs

- **Responsabilidad:** mezclar listas de resultados de búsquedas distintas por el puesto de cada fragmento, con RRF (Reciprocal Rank Fusion).
- **Contiene:** la clase `Rrf`, con la constante pública `K` (60).
- **Desde:** fase-6-hibrida.
- **Cambió en:** nada todavía.

### Clase Rrf

- **Responsabilidad:** la mezcla de la búsqueda híbrida. Es estática: no guarda nada. No mira los puntajes de cada búsqueda, porque el `RANK` del Full-Text y la distancia coseno no se pueden comparar entre sí; mira en qué puesto quedó cada fragmento.
- **Recibe en el constructor:** nada; una clase estática no tiene constructor.
- **La registra:** nadie. Se usa directo: `Rrf.Mezclar(...)`.
- **Desde:** fase-6-hibrida.
- **Cambió en:** nada todavía.

### Rrf.Mezclar(listas, topK)

- **Qué hace:** cada fragmento suma 1 / (60 + puesto) por cada lista en la que aparece, con el puesto contado desde 1. Así, un fragmento tercero en las dos listas (2/63) le gana al primero de una sola (1/61): lo que aparece bien en las dos búsquedas sube. Si dos empatan, queda primero el que apareció antes, porque el ordenamiento de LINQ es estable.
- **Recibe:** las listas de fragmentos, cada una ordenada de mejor a peor, y cuántos devolver.
- **Devuelve:** los `topK` fragmentos de mayor puntaje, sin repetir.
- **Efectos:** ninguno.
- **La llaman:** Buscador.HibridaAsync.
- **Desde:** fase-6-hibrida.
- **Cambió en:** nada todavía.

## RagBasico.Api\Salud.cs

- **Responsabilidad:** revisar que estén andando todas las piezas de la demo, para saberlo antes de presentarla y no en medio.
- **Contiene:** la clase `Salud` y su record privado `Conteo`, con los números de la base que muestra el chequeo de SQL Server.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones.

### Clase Salud

- **Responsabilidad:** los cinco chequeos de `GET /salud`, y los dos que hacen falta para arrancar. Cada uno atrapa su propio error, así un chequeo que falla no impide ver los demás.
- **Recibe en el constructor:** los clientes de `Ollama` y de `Qdrant`, y la configuración, de donde lee `ConnectionStrings:Rag`, `Ollama:ModeloChat` y `Ollama:ModeloEmbeddings`.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones (los chequeos de arranque).

### Salud.RevisarAsync(ct)

- **Qué hace:** corre los cinco chequeos y arma el estado general: `error` si falla algo sin lo que la demo no anda, `aviso` si algo anda a medias o si falta Qdrant, que tiene respaldo, y `ok` si anda todo.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `InformeSalud`.
- **Efectos:** los de cada chequeo: lee SQL Server y hace pedidos a Ollama y a Qdrant.
- **La llaman:** el endpoint `GET /salud`.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Salud.RevisarArranqueAsync(ct)

- **Qué hace:** corre los dos chequeos sin los que la API no puede arrancar: la base (Salud.RevisarSqlAsync) y los dos modelos (Salud.RevisarModelosAsync). Qdrant no, porque la demo sigue sin él. Un aviso, como una base sin documentos, no frena el arranque: hace falta la API andando para correr `/indexar`.
- **Recibe:** el token de cancelación.
- **Devuelve:** los chequeos que dieron `error`. Vacía si la API puede arrancar.
- **Efectos:** lee SQL Server y hace un pedido a Ollama.
- **La llaman:** Program.cs, antes de escuchar.
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

### Salud.RevisarSqlAsync(ct)

- **Qué hace:** se conecta a la base y cuenta documentos, fragmentos y fragmentos con vector. Avisa si no hay documentos o si faltan vectores. Si no se puede conectar, dice qué revisar: el servicio de SQL Server y si se creó la base.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `Chequeo`: `ok`, `aviso` o `error` si no se pudo conectar.
- **Efectos:** lee `rag.Documento` y `rag.Fragmento`.
- **La llaman:** Salud.RevisarAsync y Salud.RevisarArranqueAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones (dice qué revisar).

### Salud.RevisarFullTextAsync(ct)

- **Qué hace:** controla que el Full-Text no tenga cambios pendientes: mientras tanto, la búsqueda por palabras no ve todo.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `Chequeo`: `ok`, `aviso` si hay cambios pendientes, o `error`.
- **Efectos:** lee `OBJECTPROPERTYEX(..., 'TableFulltextPendingChanges')`.
- **La llaman:** Salud.RevisarAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Salud.RevisarModelosAsync(ct)

- **Qué hace:** pide a Ollama la lista de modelos instalados y controla que estén los dos de la configuración. Si falta alguno, dice con qué comando bajarlo: un `ollama pull` por cada modelo que falta.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `Chequeo`: `ok`, o `error` si Ollama no responde o falta un modelo.
- **Efectos:** un pedido a Ollama.
- **La llaman:** Salud.RevisarAsync y Salud.RevisarArranqueAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Salud.RevisarDimensionAsync(ct)

- **Qué hace:** calcula el vector de una palabra y controla que tenga 1024 números, como la columna `Embedding` y la colección de Qdrant. Si alguien cambia el modelo de embeddings por otro de otra dimensión, este chequeo lo dice.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `Chequeo`: `ok` o `error`.
- **Efectos:** un pedido a Ollama.
- **La llaman:** Salud.RevisarAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Salud.RevisarQdrantAsync(ct)

- **Qué hace:** controla que Qdrant responda, que sea la versión 1.20.1 o posterior (antes, en Windows, borrar una colección podía fallar), que exista la colección y que no esté vacía; muestra cuántos puntos tiene y cuántos están en el índice HNSW. Si algo falla es un aviso y no un error: la híbrida sigue con los vectores de SQL.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `Chequeo`: `ok` o `aviso`.
- **Efectos:** pedidos a Qdrant.
- **La llaman:** Salud.RevisarAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Salud.EstaInstalado(instalados, modelo)

- **Qué hace:** dice si un modelo está en la lista de instalados, sin distinguir mayúsculas. Un nombre sin etiqueta, como `llama3`, es el mismo que `llama3:latest`.
- **Recibe:** la lista de modelos instalados y el modelo buscado.
- **Devuelve:** `true` si está.
- **Efectos:** ninguno.
- **La llaman:** Salud.RevisarModelosAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

## RagBasico.Api\Sincronizador.cs

- **Responsabilidad:** mantener la colección de Qdrant igual a los vectores de SQL Server. SQL es la base que manda; Qdrant es una copia de los vectores con un índice para buscar rápido.
- **Contiene:** la clase `Sincronizador`, su constante privada `Lote` (256 vectores por pedido) y su record privado `VectorGuardado`, una fila de SQL con el vector como texto JSON.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones.

### Clase Sincronizador

- **Responsabilidad:** las dos bases referencian los mismos fragmentos sin una foreign key entre ellas: el id de cada punto de Qdrant es el número de fragmento de SQL. Esta clase compara los dos lados y los iguala.
- **Recibe en el constructor:** el cliente de `Qdrant` y la configuración, de donde lee `ConnectionStrings:Rag`.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Sincronizador.Comparar(enSql, enQdrant)

- **Qué hace:** compara los números de fragmento con vector en SQL con los ids de los puntos de Qdrant. Faltan en Qdrant los que están solo en SQL; sobran los que están solo en Qdrant. Compara números y no cantidades: con la misma cantidad de cada lado, puede faltar uno y sobrar otro.
- **Recibe:** los números de SQL y los ids de Qdrant, en cualquier orden.
- **Devuelve:** un `ComparacionIds` con las dos listas, ordenadas.
- **Efectos:** ninguno.
- **La llaman:** Sincronizador.EstadoAsync y Sincronizador.PonerAlDiaAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Sincronizador.EstadoAsync(ct)

- **Qué hace:** compara SQL con Qdrant sin cambiar nada: los números de fragmento de los dos lados, una muestra de vectores (Sincronizador.MuestraCoincideAsync) y cuántos vectores tiene el índice HNSW. Arma un mensaje que dice si están iguales y, si no, qué hacer.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `EstadoQdrant`.
- **Efectos:** lee `rag.Fragmento` y hace pedidos a Qdrant.
- **La llaman:** el endpoint `GET /qdrant/estado` y Evaluador.CorrerAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Sincronizador.PonerAlDiaAsync(detalle, ct)

- **Qué hace:** pone Qdrant al día con SQL y anota el resultado en el detalle.
  1. Si la colección no existe, la crea.
  2. Si la muestra de vectores no coincide, la base se recreó: reconstruye la colección entera.
  3. Si no, borra los puntos que sobran y sube los vectores que faltan.
  4. Si algo falla, lo anota como aviso con el tipo de error, que dice dónde falló: `ErrorDeServicio` es Qdrant (503 si no responde, 502 si contestó con un error), `TaskCanceledException` es que tardó más de `Qdrant:SegundosDeEspera`, y `SqlException` es SQL Server. No falla: SQL ya quedó bien, y la próxima vez que se llame se pone al día.
- **Recibe:** la lista del detalle de `/indexar` o `/vectores` y el token de cancelación.
- **Devuelve:** nada. Agrega al detalle una línea que empieza con `QDRANT:` o con `AVISO:`.
- **Efectos:** lee `rag.Fragmento` y escribe en Qdrant.
- **La llaman:** Indexador.IndexarCarpetaAsync y Vectorizador.CalcularPendientesAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones (el aviso dice qué tipo de error trae Qdrant).

### Sincronizador.ReconstruirAsync(ct)

- **Qué hace:** borra la colección, comprueba que se borró, la vuelve a crear y le sube todos los vectores de SQL. En Windows, antes de Qdrant 1.20.1, el borrado podía contestar que sí y dejar la colección: por eso pregunta si sigue existiendo y lo intenta hasta tres veces antes de dar un error que dice cómo arreglarlo.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `InformeQdrant`: cuántos puntos se subieron, cuántos había antes, el tiempo y el detalle.
- **Efectos:** borra y crea la colección en Qdrant, y lee `rag.Fragmento`.
- **La llaman:** el endpoint `POST /qdrant/reconstruir` y Sincronizador.PonerAlDiaAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Sincronizador.LeerIdsSqlAsync(ct)

- **Qué hace:** lee de SQL los números de los fragmentos que tienen vector. Los que todavía no tienen vector no cuentan: no hay nada que subir a Qdrant.
- **Recibe:** el token de cancelación.
- **Devuelve:** la lista de números, en orden.
- **Efectos:** lee `rag.Fragmento`.
- **La llaman:** Sincronizador.EstadoAsync, Sincronizador.PonerAlDiaAsync y Sincronizador.ReconstruirAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Sincronizador.SubirAsync(ids, ct)

- **Qué hace:** lee de SQL los vectores de los fragmentos pedidos, de a 256, y los sube a Qdrant. SQL Server devuelve el vector como texto JSON cuando se lo convierte a `NVARCHAR(MAX)`.
- **Recibe:** los números de fragmento y el token de cancelación.
- **Devuelve:** cuántos puntos subió.
- **Efectos:** lee `rag.Fragmento` y escribe en Qdrant.
- **La llaman:** Sincronizador.PonerAlDiaAsync y Sincronizador.ReconstruirAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Sincronizador.MuestraCoincideAsync(enSql, comparacion, ct)

- **Qué hace:** compara el vector de hasta tres fragmentos que están en los dos lados: el primero, el del medio y el último. Si la base se recreó, los números se repiten pero los textos son otros, y Qdrant devolvería fragmentos equivocados sin ningún error: los vectores lo delatan. Compara la dirección (coseno de 0,999 o más) y no número por número, porque Qdrant guarda los vectores normalizados.
- **Recibe:** los números de SQL, la comparación y el token de cancelación.
- **Devuelve:** `true` si coinciden, `false` si no, o `null` si no hay fragmentos en común para comparar.
- **Efectos:** lee `rag.Fragmento` y hace pedidos a Qdrant.
- **La llaman:** Sincronizador.EstadoAsync y Sincronizador.PonerAlDiaAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Sincronizador.Coseno(a, b)

- **Qué hace:** calcula el coseno entre dos vectores: el producto dividido por el largo de cada uno. Da 1 si apuntan para el mismo lado.
- **Recibe:** los dos vectores.
- **Devuelve:** el coseno, o 0 si los vectores tienen distinto largo o son todos ceros.
- **Efectos:** ninguno.
- **La llaman:** Sincronizador.MuestraCoincideAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

## RagBasico.Api\Validador.cs

- **Responsabilidad:** controlar lo que llega en un pedido antes de usarlo, para responder un error claro en lugar de buscar con algo que no sirve.
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

### Clase Validador

- **Responsabilidad:** las reglas de lo que se acepta. Es estática: no guarda nada.
- **Recibe en el constructor:** nada; una clase estática no tiene constructor.
- **La registra:** nadie. Se usa directo: `Validador.RevisarPregunta(...)`.
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

### Validador.RevisarPregunta(pregunta, maximo)

- **Qué hace:** controla una pregunta, en este orden: que no esté vacía (ni sea solo espacios, ni falte en el JSON), que no pase el máximo de caracteres, que no tenga caracteres de control (los saltos de línea y las tabulaciones sí se aceptan) y que tenga alguna letra o número.
- **Recibe:** la pregunta, que puede ser `null`, y el máximo de caracteres (`Rag:LargoMaximoPregunta`).
- **Devuelve:** el motivo del rechazo, para mostrárselo a quien preguntó; o vacío si la pregunta está bien.
- **Efectos:** ninguno.
- **La llaman:** los endpoints `GET /buscar` y `POST /preguntar`.
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

## RagBasico.Api\Vectorizador.cs

- **Responsabilidad:** calcular el vector de cada fragmento que todavía no lo tiene y guardarlo en `rag.Fragmento`.
- **Desde:** fase-5-vectores.
- **Cambió en:** fase-7-qdrant.

### Clase Vectorizador

- **Responsabilidad:** el camino de un fragmento hasta su vector: leerlo de la base, pedirle el vector a Ollama y guardarlo, en SQL y en Qdrant.
- **Recibe en el constructor:** el cliente de `Ollama`, el `Sincronizador`, para subir los vectores nuevos a Qdrant, y la configuración, de donde lee `ConnectionStrings:Rag` y `Rag:LoteVectores`.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-5-vectores.
- **Cambió en:** fase-7-qdrant (recibe el sincronizador).

### Vectorizador.CalcularPendientesAsync(ct)

- **Qué hace:** recorre los fragmentos sin vector de a lotes de `Rag:LoteVectores`, en orden de número de fragmento.
  - Cada lote empieza después del último fragmento visto, en lugar de volver a buscar desde el principio. Así no relee lo ya hecho, que con un millón de fragmentos haría crecer el costo al cuadrado, y un fragmento que falla no traba la corrida: queda en `NULL` y se vuelve a intentar en el próximo `/vectores`.
  - Corta la corrida si Ollama no responde (Errores.EsCaida), o si fallan todos los fragmentos de cinco lotes seguidos, que ya no es un fragmento con problemas sino algo general, como la base. Un lote entero que falla por su contenido no corta: hasta la fase 6 sí, y un lote de textos que Ollama no acepta trababa todas las corridas en el mismo lugar (está en `docs\erratas.md`).
  - Al final pone Qdrant al día con Sincronizador.PonerAlDiaAsync: sube los vectores nuevos y, si Qdrant estuvo apagado, también los que le faltaban de antes.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `InformeVectores`: cuántos vectores se calcularon, cuántos fallaron, cuántos fragmentos quedan sin vector, cuántos fragmentos por segundo, el tiempo como horas:minutos:segundos y el detalle. Los cuenta en dos campos de la clase, `_calculados` y `_fallados`, así no se pierden si la corrida se corta a la mitad de un lote. Si no había nada que hacer y no quedó nada pendiente, el detalle dice `SIN CAMBIOS`; una corrida cortada nunca lo dice.
- **Efectos:** lee `rag.Fragmento` y `rag.Documento`, llama a Ollama, escribe `rag.Fragmento.Embedding` y sube los vectores a Qdrant.
- **La llaman:** el endpoint `POST /vectores`.
- **Desde:** fase-5-vectores.
- **Cambió en:** fase-7-qdrant (pone Qdrant al día; corta solo si Ollama no responde o fallan cinco lotes seguidos; los contadores quedan en la clase).

### Vectorizador.ComoJson(vector)

- **Qué hace:** escribe un vector como lo recibe SQL Server: un arreglo JSON, como `[0.5,-0.25,0.0000123]`. Usa siempre el punto decimal (`CultureInfo.InvariantCulture`): con Windows en castellano, `ToString()` escribiría `0,5` y el JSON quedaría mal. Tampoco usa notación científica (`1E-05`).
- **Recibe:** el vector.
- **Devuelve:** el texto JSON.
- **Efectos:** ninguno.
- **La llaman:** Vectorizador.CalcularYGuardarAsync y Buscador.CercanosEnSqlAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Vectorizador.GuardarLoteAsync(conexion, lote, detalle, ct)

- **Qué hace:** calcula y guarda los vectores de un lote en un solo pedido a Ollama. Si falla, prueba los fragmentos de a uno, así un fragmento con problemas no deja sin vector a los demás, y cuenta y anota cada uno que falla. Si se canceló el pedido, o si Ollama no responde (Errores.EsCaida), lanza el error y no prueba más: probar de a uno sería fallar dieciséis veces.
- **Recibe:** la conexión abierta, los fragmentos del lote, la lista del detalle y el token de cancelación.
- **Devuelve:** nada. Los vectores guardados los suma Vectorizador.CalcularYGuardarAsync; los fragmentos que fallan los suma este método, en `_fallados`.
- **Efectos:** los de Vectorizador.CalcularYGuardarAsync.
- **La llaman:** Vectorizador.CalcularPendientesAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** fase-7-qdrant (si Ollama no responde, lanza el error en lugar de probar de a uno; cuenta los que fallan en la clase en lugar de devolver cuántos guardó).

### Vectorizador.CalcularYGuardarAsync(conexion, fragmentos, ct)

- **Qué hace:** le pide a Ollama los vectores de los fragmentos en un solo pedido y los guarda en `rag.Fragmento.Embedding`, todos en una transacción. SQL Server convierte el texto JSON en un `VECTOR(1024)` con `CAST`. Después de confirmar la transacción, los suma a `_calculados`.
- **Recibe:** la conexión abierta, los fragmentos y el token de cancelación.
- **Devuelve:** nada.
- **Efectos:** un `POST /api/embed` a Ollama y un `UPDATE` por fragmento.
- **La llaman:** Vectorizador.GuardarLoteAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

## RagBasico.Api\appsettings.json

- **Responsabilidad:** la configuración de la API. Cada clave está explicada en `docs\arquitectura.md`.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-8-validaciones.

## RagBasico.Api\wwwroot\index.html

- **Responsabilidad:** la página de la demo: una caja para la pregunta, el selector de modo (híbrida, solo palabras, solo vectores en SQL Server o solo vectores en Qdrant) y la respuesta con sus fuentes y los tiempos. La sirve la propia API en la raíz, `http://127.0.0.1:5088/`.
- **Contiene:** HTML, el estilo y un script, todo en el mismo archivo y sin nada de internet, para que la demo ande sin conexión. El script:
  - manda la pregunta y el modo a `POST /preguntar` con `fetch`;
  - muestra el aviso, arriba de la respuesta, si la híbrida siguió sin Qdrant;
  - muestra el texto con `textContent` y no con `innerHTML`, así el texto de un documento nunca se interpreta como HTML;
  - lista las fuentes y atenúa las que se le pasaron al modelo pero no citó;
  - no muestra el bloque de fuentes cuando la respuesta es "no encontré", porque cinco documentos debajo de "no encontré" confunden;
  - muestra el error de la API, por ejemplo el de un modo que no existe.
- **Desde:** fase-6-hibrida.
- **Cambió en:** fase-7-qdrant (el modo con Qdrant y el aviso).

## RagBasico.slnx

- **Responsabilidad:** la solución, el archivo que se abre con doble clic en Visual Studio. Agrupa los proyectos del repo; hoy, solo `RagBasico.Api`.
- **Contiene:** el formato `.slnx`, el XML que usa .NET 10 por defecto, más corto y legible que el `.sln` de siempre.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## docs\arquitectura.md

- **Responsabilidad:** cómo se arma esta versión: las piezas, los recorridos paso a paso, las tablas, la configuración y lo que está fuera del repo.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-8-validaciones.

## docs\erratas.md

- **Responsabilidad:** los bugs encontrados después de cerrar una fase, y dónde se arreglaron. Solo se actualiza en la punta.
- **Desde:** fase-0-base.
- **Cambió en:** fase-7-qdrant.

## docs\fases\fase-0-base.md

- **Responsabilidad:** qué agregó la fase 0, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## docs\fases\fase-1-ollama.md

- **Responsabilidad:** qué agregó la fase 1, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-7-qdrant.

## docs\fases\fase-2-rag-minimo.md

- **Responsabilidad:** qué agregó la fase 2, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-7-qdrant.

## docs\fases\fase-3-medicion.md

- **Responsabilidad:** qué agregó la fase 3, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-3-medicion.
- **Cambió en:** fase-7-qdrant.

## docs\fases\fase-4-carga.md

- **Responsabilidad:** qué agregó la fase 4, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-4-carga.
- **Cambió en:** fase-7-qdrant.

## docs\fases\fase-5-vectores.md

- **Responsabilidad:** qué agregó la fase 5, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-5-vectores.
- **Cambió en:** fase-7-qdrant.

## docs\fases\fase-6-hibrida.md

- **Responsabilidad:** qué agregó la fase 6, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-6-hibrida.
- **Cambió en:** fase-7-qdrant.

## docs\fases\fase-7-qdrant.md

- **Responsabilidad:** qué agregó la fase 7, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

## docs\fases\fase-8-validaciones.md

- **Responsabilidad:** qué agregó la fase 8, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

## docs\instalacion.md

- **Responsabilidad:** qué hay que instalar para correr todas las fases, en el orden en que las fases lo necesitan: cómo instalar cada pieza, cómo comprobar que anda, qué arrancar cada vez y qué hacer si algo falla.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones.

## docs\responsabilidades.md

- **Responsabilidad:** este documento: qué hace cada archivo, clase, función y endpoint, y quién lo usa.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-8-validaciones.

## docs\worktrees.md

- **Responsabilidad:** cómo abrir y correr cada fase en su propia carpeta, con git worktree, también en un clon nuevo.
- **Desde:** fase-0-base.
- **Cambió en:** fase-7-qdrant.

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
- **Pasos:** lee la cadena de conexión; busca los `sql\NN_*.sql` y los ordena por nombre; corre cada uno con `sqlcmd -E -C -b -v Base=<base>`: autenticación de Windows, confiar en el certificado de SQL Express y cortar ante el primer error. Si el servidor es de esta PC (`localhost`, `.`, `(local)` o el nombre de la PC), le agrega `lpc:` adelante: se conecta por memoria compartida, sin el servicio SQL Server Browser. Sin el prefijo, el sqlcmd que instala winget le pregunta primero por la instancia al Browser, y si no está andando, lo espera hasta agotar el tiempo y falla aunque SQL Server esté andando. Si un script falla, dice cuál y sugiere revisar que esté andando el servicio de SQL Server, que es el error más común: con el servicio detenido, sqlcmd falla enseguida con `The system cannot find the file specified`, sin decir que es el servicio.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-7-qdrant.

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

## scripts\iniciar-qdrant.ps1

- **Responsabilidad:** arranca Qdrant escuchando solo en esta PC (`127.0.0.1`), con su carpeta de datos fija y sin telemetría. Sin esto, Qdrant escucha en toda la red y sin clave, y guarda los datos en una carpeta relativa a donde se lo arranca.
- **Parámetros:** `-Carpeta`, opcional: la carpeta de `qdrant.exe`. Por defecto, `RAG-BASICO-datos\qdrant` al lado de la carpeta principal del repo, también si se lo corre desde una carpeta de fase. Los datos quedan en sus subcarpetas `storage` y `snapshots`.
- **Ejemplo:** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\iniciar-qdrant.ps1`
- **Requiere:** `qdrant.exe` 1.20.2 (el archivo `qdrant-x86_64-pc-windows-msvc.zip` de la página de versiones de Qdrant en GitHub), git 2.31 o posterior y Windows PowerShell 5.1. Si falta `qdrant.exe`, dice de dónde bajarlo.
- **Pasos:** busca `qdrant.exe`; define, solo para este proceso, `QDRANT__SERVICE__HOST=127.0.0.1`, los puertos 6333 y 6334, `QDRANT__STORAGE__STORAGE_PATH`, `QDRANT__STORAGE__SNAPSHOTS_PATH` y `QDRANT__TELEMETRY_DISABLED=true`; y arranca Qdrant desde su carpeta. Queda corriendo en esa consola hasta Ctrl+C. No hace falta abrirle el firewall.
- **Desde:** fase-7-qdrant.
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

