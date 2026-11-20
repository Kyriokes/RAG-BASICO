# Responsabilidades

Qué hace cada archivo, clase, función y endpoint de esta versión, y quién lo usa. Esta es la versión de `fase-9-local`.

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
│  ├─ Buscador.cs                  búsqueda por palabras, por significado e híbrida
│  ├─ Cargador.cs                  procesa un documento, del PDF a los vectores
│  ├─ Chunker.cs                   corta el texto en fragmentos por párrafos
│  ├─ DetectorDeCaidas.cs          dice qué servicio no responde
│  ├─ Documentos.cs                el registro de cada PDF con su estado
│  ├─ ErrorDeServicio.cs           el error de un servidor
│  ├─ Errores.cs                   cuándo un error es un servicio caído
│  ├─ ManejadorDeErrores.cs        convierte los errores en respuestas claras
│  ├─ Modelos.cs                   records de pedidos y respuestas
│  ├─ Ollama.cs                    cliente HTTP de Ollama
│  ├─ Procesador.cs                el trabajo en segundo plano
│  ├─ Program.cs                   arranque, servicios y endpoints
│  ├─ Properties\
│  │  └─ launchSettings.json       dirección, puerto y forma de arrancar
│  ├─ Qdrant.cs                    cliente HTTP de Qdrant
│  ├─ RagBasico.Api.csproj         el proyecto y sus paquetes
│  ├─ RagBasico.Api.http           pedidos de prueba
│  ├─ Redactor.cs                  arma la respuesta con Qwen y la registra
│  ├─ Rrf.cs                       mezcla dos listas de resultados por puesto
│  ├─ Salud.cs                     revisa que anden todas las piezas
│  ├─ Validador.cs                 controla la pregunta
│  ├─ appsettings.json             configuración
│  └─ wwwroot\                     las páginas
│     ├─ documentos.html           la página para cargar documentos
│     ├─ estado.js                 la franja de estado de las dos páginas
│     ├─ estilos.css               el estilo común a las dos páginas
│     └─ index.html                la página para preguntar
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
│  │  ├─ fase-8-validaciones.md    las validaciones y los errores claros
│  │  └─ fase-9-local.md           la versión para usar: carga por la página
│  ├─ instalacion.md               qué instalar y cómo comprobar que anda
│  ├─ manual.md                    en qué orden leer el código
│  └─ responsabilidades.md         este documento
├─ global.json                     versión del SDK de .NET
├─ iniciar.cmd                     doble clic para arrancar todo
├─ scripts\                        herramientas del repo
│  ├─ controlar-docs.ps1           controla la documentación
│  ├─ crear-base.ps1               crea la base del RAG
│  ├─ crear-origen-de-prueba.ps1   crea una base de origen para probar
│  ├─ iniciar-qdrant.ps1           arranca Qdrant solo en esta PC
│  └─ iniciar.ps1                  arranca Qdrant, si hace falta, y la API
└─ sql\                            scripts de la base
   ├─ base.sql                     toda la base del RAG
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
- **Contiene:** la plantilla oficial de .NET, generada con `dotnet new gitignore`, y un solo agregado al final: `conexiones.json`, el archivo con los servidores y las claves de la oficina.
- **Desde:** fase-0-base.
- **Cambió en:** fase-9-local.

## README.md

- **Responsabilidad:** la portada del repo: qué es, qué hace falta para correr esta fase, cómo correrla, la tabla de fases y dónde está la documentación.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-8-validaciones, fase-9-local.

## RagBasico.Api\Buscador.cs

- **Responsabilidad:** encontrar los fragmentos que mejor responden una pregunta: por palabras en SQL Server, por significado en Qdrant, o con las dos mezcladas.
- **Contiene:** la clase `Buscador`, con la lista pública `Modos` (`palabras`, `vectores` e `hibrida`) y la constante `ModoPorDefecto` (`hibrida`).
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-9-local.

### Clase Buscador

- **Responsabilidad:** juntar en un solo lugar las tres formas de buscar: por palabras, con el Full-Text de SQL Server; por vectores, con el índice HNSW de Qdrant; y la híbrida, que mezcla las dos. Solo devuelve fragmentos de documentos en estado `listo`.
- **Recibe en el constructor:** los clientes de `Ollama`, para el vector de la pregunta, y de `Qdrant`; y la configuración, de donde lee `Conexiones:SqlTextos` y `Rag:CandidatosHibrida`.
- **La registra:** Program.cs, con `AddScoped`: un objeto nuevo por pedido HTTP.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-5-vectores (recibe el cliente de Ollama y busca por vectores), fase-6-hibrida (la híbrida, que pasa a ser el modo por defecto), fase-7-qdrant (Qdrant y el respaldo en SQL), fase-9-local (los vectores están solo en Qdrant: se van el modo `vectores-qdrant`, el respaldo en SQL y los avisos; lee la conexión de `Conexiones:SqlTextos`).

### Buscador.ElegirModo(pedido)

- **Qué hace:** controla el modo que llega en un pedido. Sin modo, devuelve el modo por defecto, `hibrida`. Con un modo que existe, lo devuelve en minúsculas y sin espacios. Con un modo que no existe devuelve `null`, para que quien llama dé un error en lugar de buscar con otro modo sin avisar.
- **Recibe:** el modo del pedido, que puede faltar.
- **Devuelve:** el modo, o `null` si no existe.
- **Efectos:** ninguno.
- **La llaman:** los endpoints `GET /buscar` y `POST /preguntar`.
- **Desde:** fase-5-vectores.
- **Cambió en:** nada todavía.

### Buscador.BuscarAsync(pregunta, modo, topK, ct)

- **Qué hace:** busca con el modo pedido: llama a Buscador.PorPalabrasAsync, Buscador.PorVectoresAsync o Buscador.HibridaAsync. Con un modo que no existe lanza un `ArgumentException`, aunque los endpoints ya lo controlaron antes.
- **Recibe:** la pregunta, el modo, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** la lista de `Fragmento`, del más relevante al menos relevante.
- **Efectos:** los de la búsqueda que usa.
- **La llaman:** el endpoint `GET /buscar` y Redactor.ResponderAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** fase-6-hibrida (el modo `hibrida`), fase-7-qdrant (el modo `vectores-qdrant`, y devuelve el aviso), fase-9-local (quedan tres modos y devuelve la lista sola, sin aviso).

### Buscador.HibridaAsync(pregunta, topK, ct)

- **Qué hace:** busca por palabras y por significado al mismo tiempo, pide a cada una los mejores 20 candidatos (`Rag:CandidatosHibrida`) y los mezcla por puesto con Rrf.Mezclar. Las dos búsquedas arrancan juntas y recién después se espera a las dos con `Task.WhenAll`: se tarda lo que la más lenta, no la suma. Si Qdrant no responde, falla con un error que lo dice: ya no hay vectores en SQL Server para seguir sin él.
- **Recibe:** la pregunta, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** la lista mezclada, de mayor a menor puntaje RRF.
- **Efectos:** un pedido a Ollama, hasta dos a Qdrant y dos lecturas de SQL Server.
- **La llaman:** Buscador.BuscarAsync.
- **Desde:** fase-6-hibrida.
- **Cambió en:** fase-7-qdrant (los vectores salen de Qdrant, con respaldo en SQL), fase-9-local (sin respaldo en SQL: si Qdrant no responde, falla).

### Buscador.PorPalabrasAsync(pregunta, topK, ct)

- **Qué hace:** busca en el Full-Text de SQL Server los fragmentos que mejor coinciden con las palabras de la pregunta. `FREETEXTTABLE` corta la pregunta en palabras, busca también sus plurales y conjugaciones, y le pone un puntaje (`RANK`) a cada fragmento. Se piden los 50 mejores y se devuelven los `topK` primeros. Solo cuentan los documentos en estado `listo`: uno a medio procesar todavía no tiene sus vectores.
- **Recibe:** la pregunta, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** una lista de `Fragmento` (número, archivo y texto), del más relevante al menos relevante. Vacía si la pregunta está vacía o si ninguna palabra coincide.
- **Efectos:** lee `rag.Fragmento` y `rag.Documento`. No escribe nada.
- **La llaman:** Buscador.BuscarAsync y Buscador.HibridaAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-9-local (solo los documentos listos).

### Buscador.PorVectoresAsync(pregunta, topK, ct)

- **Qué hace:** busca los fragmentos más parecidos en significado a la pregunta: le pide a Ollama el vector de la pregunta, a Qdrant los números de los fragmentos más cercanos, y a SQL Server sus textos. Si la colección todavía no existe, porque no se cargó ningún documento, devuelve una lista vacía. Si Qdrant no responde, falla.
- **Recibe:** la pregunta, cuántos fragmentos devolver y el token de cancelación.
- **Devuelve:** la lista de `Fragmento`, del más cercano al más lejano. Vacía si la pregunta está vacía o si no hay colección.
- **Efectos:** un pedido a Ollama y hasta dos a Qdrant; lee `rag.Fragmento` y `rag.Documento`. No escribe nada.
- **La llaman:** Buscador.BuscarAsync y Buscador.HibridaAsync.
- **Desde:** fase-5-vectores.
- **Cambió en:** fase-7-qdrant (la consulta con `VECTOR_DISTANCE` pasa a otra función), fase-9-local (busca en Qdrant y no en SQL Server, que ya no guarda vectores).

### Buscador.LeerFragmentosAsync(ids, ct)

- **Qué hace:** trae de SQL Server los fragmentos de una lista de números, como la que devuelve Qdrant, y los deja en el mismo orden de la lista: SQL no los devuelve en ese orden. Un número que ya no existe en SQL, o que es de un documento que no está `listo`, se saltea.
- **Recibe:** los números de fragmento, del más cercano al más lejano, y el token de cancelación.
- **Devuelve:** la lista de `Fragmento`, en el orden de los números.
- **Efectos:** lee `rag.Fragmento` y `rag.Documento`.
- **La llaman:** Buscador.PorVectoresAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-9-local (saltea los fragmentos de documentos que no están listos).

## RagBasico.Api\Cargador.cs

- **Responsabilidad:** procesar un documento pendiente de punta a punta: sacar el texto del PDF, cortarlo, guardar los fragmentos, calcular sus vectores y subirlos a Qdrant.
- **Contiene:** la clase `Cargador`, su constante privada `PalabrasPorPaginaMinimas` (40), el promedio de palabras por página por debajo del cual un PDF se toma como escaneado, y su record privado `FragmentoNuevo`, un fragmento recién guardado con su número y su texto.
- **Desde:** fase-9-local. Junta lo que hasta la fase 8 hacían el `Indexador`, el `Vectorizador` y el `Sincronizador`.
- **Cambió en:** nada todavía.

### Clase Cargador

- **Responsabilidad:** el camino de un documento desde `pendiente` hasta `listo`, `sin texto` o `error`. Procesa de a uno: el documento aparece en las búsquedas recién cuando tiene sus fragmentos en SQL Server y sus vectores en Qdrant.
- **Recibe en el constructor:** el registro de `Documentos`, los clientes de `Ollama` y de `Qdrant`, el log y la configuración, de donde lee `Conexiones:SqlTextos`, `Rag:PalabrasPorFragmento`, `Rag:Solapamiento` y `Rag:LoteVectores`.
- **La registra:** Program.cs, con `AddScoped`. El `Procesador` pide uno nuevo por cada documento.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Cargador.ProcesarSiguienteAsync(ct)

- **Qué hace:** le pide el próximo pendiente a Documentos.SiguientePendienteAsync, toma el turno (`Documentos.Turno`) y lo procesa con Cargador.ProcesarAsync. Mientras tiene el turno, nadie más toca los fragmentos ni los vectores: ni borrar ni reprocesar.
- **Recibe:** el token de cancelación, que se dispara cuando se apaga la API.
- **Devuelve:** `false` si no había ningún pendiente; `true` si procesó uno, haya quedado `listo`, `sin texto` o `error`. Si un servidor no responde, lanza el error y el documento queda pendiente.
- **Efectos:** los de Cargador.ProcesarAsync.
- **La llaman:** Procesador.ExecuteAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Cargador.EsDeUnServidor(error)

- **Qué hace:** dice si un error es de un servidor que no responde y no del documento: una caída de Ollama o de Qdrant (Errores.EsCaida) o cualquier `SqlException`. Un error que el servidor sí contestó, como un texto que Ollama rechaza, es del documento.
- **Recibe:** el error.
- **Devuelve:** `true` si es de un servidor.
- **Efectos:** ninguno.
- **La llaman:** Cargador.ProcesarAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Cargador.ProcesarAsync(pendiente, ct)

- **Qué hace:** los pasos de un documento, en orden.
  1. Borra de Qdrant y de SQL lo que hubiera quedado de un intento anterior o de la versión anterior del PDF.
  2. Lee los bytes del PDF con Documentos.LeerContenidoAsync. Si no están, o no son un PDF, lo deja en `error`.
  3. Saca el texto con Cargador.SacarTexto. Un PDF roto o con clave queda en `error`; uno escaneado (Cargador.EsEscaneado), en `sin texto`.
  4. Corta el texto con Chunker.Partir y guarda los fragmentos con Cargador.GuardarFragmentosAsync.
  5. Crea la colección si no existe, y de a lotes de `Rag:LoteVectores` le pide los vectores a Ollama y los sube a Qdrant, con el número de fragmento como id.
  6. Lo marca `listo`. Si Cargador.RevisarTexto encontró algo raro, queda anotado en el detalle como `texto dudoso`.
  - Si algo falla, borra los fragmentos que hubiera guardado. Si el error es de un servidor (Cargador.EsDeUnServidor), el documento queda pendiente y el error sale, para que el `Procesador` espere y reintente. Si no, lo anota en el log y deja el documento en `error` con el mensaje.
- **Recibe:** el documento pendiente y el token de cancelación.
- **Devuelve:** nada.
- **Efectos:** lee el PDF de la base del RAG o de la base de origen; borra y escribe en `rag.Fragmento`; cambia el estado en `rag.Documento`; pedidos a Ollama; borra y sube puntos en Qdrant.
- **La llaman:** Cargador.ProcesarSiguienteAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Cargador.GuardarFragmentosAsync(pendiente, partes, ct)

- **Qué hace:** guarda los fragmentos de un documento en una sola transacción. Cada uno empieza con `Documento: <nombre sin extensión>`, así la búsqueda también lo encuentra por el nombre. El `INSERT` devuelve el número de cada fragmento con `OUTPUT INSERTED.FragmentoId`.
- **Recibe:** el documento, los textos de los fragmentos en orden y el token de cancelación.
- **Devuelve:** la lista de `FragmentoNuevo`: cada fragmento con su número y su texto.
- **Efectos:** inserta filas en `rag.Fragmento`.
- **La llaman:** Cargador.ProcesarAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Cargador.BorrarFragmentosAsync(documentoId, ct)

- **Qué hace:** borra de SQL Server los fragmentos de un documento.
- **Recibe:** el número del documento y el token de cancelación.
- **Devuelve:** nada.
- **Efectos:** borra filas de `rag.Fragmento`.
- **La llaman:** Cargador.ProcesarAsync y Cargador.BorrarFragmentosSinFallarAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Cargador.BorrarFragmentosSinFallarAsync(documentoId)

- **Qué hace:** borra los fragmentos de un documento después de un error. Si tampoco se puede, lo anota en el log y sigue: se borran al procesarlo de nuevo.
- **Recibe:** el número del documento. No recibe el token de cancelación: tiene que borrar aunque la API se esté apagando.
- **Devuelve:** nada.
- **Efectos:** borra filas de `rag.Fragmento`.
- **La llaman:** Cargador.ProcesarAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Cargador.SacarTexto(contenido)

- **Qué hace:** saca el texto de un PDF con PdfPig, página por página, en el orden en que se lee. PdfPig marca con una línea en blanco el espacio grande entre dos renglones, que es lo que el `Chunker` toma como cambio de párrafo. Si el PDF está roto o tiene clave, lanza el error de PdfPig.
- **Recibe:** los bytes del PDF.
- **Devuelve:** una lista con el texto de cada página. Una página escaneada viene vacía.
- **Efectos:** ninguno.
- **La llaman:** Cargador.ProcesarAsync.
- **Desde:** fase-9-local. Viene de `Indexador.LeerPdf`, que leía el PDF de una carpeta.
- **Cambió en:** nada todavía.

### Cargador.EsEscaneado(paginas)

- **Qué hace:** dice si un PDF parece escaneado: menos de 40 palabras por página en promedio. Un escaneo con un sello de firma en cada página tiene algunas palabras por página, pero no tantas; por eso se mira el promedio y no un total fijo.
- **Recibe:** el texto de cada página.
- **Devuelve:** `true` si parece escaneado, o si no tiene páginas.
- **Efectos:** ninguno.
- **La llaman:** Cargador.ProcesarAsync.
- **Desde:** fase-9-local. Viene de `Indexador.EsEscaneado`, sin cambios.
- **Cambió en:** nada todavía.

### Cargador.RevisarTexto(paginas)

- **Qué hace:** busca señales de que el texto salió mal del PDF, en este orden:
  1. Menos del 80 % de los caracteres son letras o números: la fuente del PDF no se pudo leer.
  2. Más de 12 caracteres por palabra en promedio: el PDF no marca los espacios y las palabras salen pegadas.
  3. Una cuarta parte de las páginas, o más, con menos de 20 palabras: tiene partes escaneadas, como un anexo.
- **Recibe:** el texto de cada página.
- **Devuelve:** el motivo de la duda, o vacío si el texto parece bien.
- **Efectos:** ninguno.
- **La llaman:** Cargador.ProcesarAsync.
- **Desde:** fase-9-local. Viene de `Indexador.RevisarTexto`, sin cambios.
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
- **La llaman:** Cargador.ProcesarAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-4-carga (por párrafos, sin títulos sueltos ni fragmentos casi repetidos).

### Chunker.ContarPalabras(texto)

- **Qué hace:** cuenta las palabras de un texto, separando por espacios, saltos de línea y tabulaciones.
- **Recibe:** el texto.
- **Devuelve:** la cantidad de palabras.
- **Efectos:** ninguno.
- **La llaman:** Cargador.EsEscaneado y Cargador.RevisarTexto.
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

## RagBasico.Api\Documentos.cs

- **Responsabilidad:** el registro de los documentos: anota los que llegan, dice cuál sigue, guarda el estado de cada uno, los lista y los borra. No procesa: eso lo hace el `Cargador`.
- **Contiene:** la clase `Documentos`, con la lista pública `Estados` (`pendiente`, `listo`, `sin texto` y `error`); el `Turno`, un `SemaphoreSlim` de un solo lugar que comparten el `Cargador`, borrar y reprocesar, para no pisarse; la constante privada `PorPagina` (50); y dos records privados: `Anterior`, un documento que ya estaba cargado con el mismo nombre, y `Cuenta`, los números de la base que arman el resumen.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Clase Documentos

- **Responsabilidad:** ser el único lugar que escribe en `rag.Documento`. Un documento llega de dos maneras: arrastrado a la página, y entonces sus bytes se guardan en la columna `Contenido`; o importado de la base de origen, y entonces solo se guarda su número de allá en `OrigenId` y el PDF se lee de allá cuando le toca.
- **Recibe en el constructor:** el cliente de `Qdrant`, para borrar los vectores de un documento, y la configuración, de donde lee `Conexiones:SqlTextos`, `Conexiones:SqlOrigen`, `Conexiones:ConsultaLista`, `Conexiones:ConsultaContenido`, `Rag:PalabrasPorFragmento` y `Rag:Solapamiento`.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Documentos.EsPdf(contenido)

- **Qué hace:** dice si unos bytes son un PDF: busca la marca `%PDF-` dentro de los primeros 1024 bytes. No mira la extensión del nombre.
- **Recibe:** los bytes del archivo.
- **Devuelve:** `true` si es un PDF.
- **Efectos:** ninguno.
- **La llaman:** Documentos.AnotarSubidosAsync y Cargador.ProcesarAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Documentos.CalcularHuella(contenido, parametros)

- **Qué hace:** calcula la huella de carga de un PDF: el sha256 de sus bytes seguidos de los parámetros de corte. Si cambia el PDF o cambia la forma de cortar, cambia la huella.
- **Recibe:** los bytes del PDF y el texto con los parámetros de corte.
- **Devuelve:** los 32 bytes de la huella.
- **Efectos:** ninguno.
- **La llaman:** Documentos.AnotarSubidosAsync.
- **Desde:** fase-9-local. Viene de `Indexador.CalcularHuella`, que leía el archivo de una carpeta.
- **Cambió en:** nada todavía.

### Documentos.ParametrosDeCorte(palabras, solapamiento)

- **Qué hace:** arma el texto con todo lo que cambia el resultado del corte: la versión del `Chunker`, el tamaño de fragmento, el solapamiento y la versión de PdfPig, porque otra versión puede sacar otro texto del mismo PDF. Por ejemplo: `corte=parrafos-1;palabras=300;solapamiento=50;pdfpig=0.1.16.0`.
- **Recibe:** el tamaño de fragmento y el solapamiento, en palabras.
- **Devuelve:** el texto.
- **Efectos:** ninguno.
- **La llaman:** el constructor de `Documentos`.
- **Desde:** fase-9-local. Viene de `Indexador.ParametrosDeCorte`.
- **Cambió en:** nada todavía.

### Documentos.EstimarFalta(pendientes, porMinuto)

- **Qué hace:** dice en palabras cuánto falta para terminar la carga: `menos de un minuto`, minutos hasta 90, horas hasta 36 y días después. Sin pendientes devuelve vacío; sin velocidad, `sin datos todavía`.
- **Recibe:** cuántos documentos quedan pendientes y cuántos se procesan por minuto.
- **Devuelve:** el texto, por ejemplo `unas 3 horas`.
- **Efectos:** ninguno.
- **La llaman:** Documentos.ResumirAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Documentos.AnotarSubidosAsync(archivos, ct)

- **Qué hace:** anota los archivos que se arrastraron a la página. Compara por nombre de archivo, entre los arrastrados.
  - Un PDF nuevo se guarda con sus bytes y queda `pendiente`.
  - Uno que ya estaba con la misma huella no se toca.
  - Uno que cambió guarda el contenido nuevo y vuelve a `pendiente`. Los fragmentos y los vectores viejos los borra el `Cargador` antes de procesarlo; mientras tanto no aparece en las búsquedas, porque no está `listo`.
  - Lo que no es un PDF (Documentos.EsPdf) se rechaza.
  - Si en la misma tanda vienen dos con el mismo nombre, vale el último.
- **Recibe:** la lista de `ArchivoSubido` y el token de cancelación.
- **Devuelve:** un `InformeAnotado`: cuántos nuevos, sin cambios, reemplazados y rechazados, y el detalle.
- **Efectos:** lee, inserta y actualiza filas de `rag.Documento`.
- **La llaman:** el endpoint `POST /documentos`.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Documentos.AnotarOrigenAsync(ct)

- **Qué hace:** anota como pendientes los documentos de la base de origen que todavía no están. Corre la consulta de `Conexiones:ConsultaLista`, que devuelve el número y el nombre de cada documento, sin traer los PDF. Lee la lista de a una fila, sin cargarla entera en memoria, y guarda de a lotes de 1.000 con Documentos.GuardarLoteAsync. Un documento sin nombre queda como `documento-<número>.pdf`. Si no hay base de origen configurada, lanza un `ErrorDeServicio` con código 400.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `InformeAnotado`: cuántos nuevos quedaron pendientes y cuántos ya estaban anotados.
- **Efectos:** lee la base de origen; lee e inserta filas en `rag.Documento`.
- **La llaman:** el endpoint `POST /documentos/importar`.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Documentos.SiguientePendienteAsync(ct)

- **Qué hace:** busca el próximo documento pendiente. Los arrastrados van antes que los importados: así uno que se suelta en la página no espera detrás de miles. Dentro de cada grupo, el de número más bajo.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `Pendiente`, o `null` si no hay ninguno.
- **Efectos:** lee `rag.Documento`.
- **La llaman:** Cargador.ProcesarSiguienteAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Documentos.LeerContenidoAsync(pendiente, ct)

- **Qué hace:** trae los bytes del PDF. Si se arrastró, de la columna `Contenido` de la base del RAG. Si se importó, de la base de origen, con la consulta de `Conexiones:ConsultaContenido`, que recibe el número en el parámetro `@Id`. Si hay un importado pendiente y `Conexiones:SqlOrigen` está vacía, lanza un `ErrorDeServicio` con código 503.
- **Recibe:** el documento pendiente y el token de cancelación.
- **Devuelve:** los bytes, o `null` si la fila ya no está o no tiene binario.
- **Efectos:** lee `rag.Documento` o la base de origen.
- **La llaman:** Cargador.ProcesarAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Documentos.MarcarAsync(documentoId, estado, detalle, ct)

- **Qué hace:** guarda el estado de un documento y el motivo, si hay, recortado a 1.000 caracteres. Cuando deja de estar pendiente anota la hora en `FechaProceso`: con eso se calcula la velocidad de la carga.
- **Recibe:** el número del documento, el estado, el detalle (que puede ser `null`) y el token de cancelación.
- **Devuelve:** nada.
- **Efectos:** actualiza una fila de `rag.Documento`.
- **La llaman:** Cargador.ProcesarAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Documentos.ResumirAsync(estado, pagina, ct)

- **Qué hace:** cuenta los documentos por estado y los fragmentos; calcula la velocidad, que son los documentos procesados en los últimos 10 minutos dividido 10; estima lo que falta con Documentos.EstimarFalta; y trae una página de 50 documentos, el más nuevo primero, de todos o de un solo estado.
- **Recibe:** el estado para filtrar, o `null` para todos; el número de página, desde 1; y el token de cancelación.
- **Devuelve:** un `ResumenDocumentos`.
- **Efectos:** lee `rag.Documento` y `rag.Fragmento`.
- **La llaman:** el endpoint `GET /documentos`.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Documentos.BorrarAsync(documentoId, ct)

- **Qué hace:** borra un documento: primero sus vectores de Qdrant y después su fila, que se lleva sus fragmentos en cascada. En ese orden, si Qdrant falla, el documento sigue entero y se puede volver a intentar. Espera el turno (`Documentos.Turno`), para no borrar un documento que el `Cargador` está procesando.
- **Recibe:** el número del documento y el token de cancelación.
- **Devuelve:** `true` si lo borró; `false` si no existe.
- **Efectos:** borra puntos de Qdrant y una fila de `rag.Documento`, con sus filas de `rag.Fragmento`.
- **La llaman:** el endpoint `DELETE /documentos/{id:int}`.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Documentos.ReintentarAsync(ct)

- **Qué hace:** pasa a `pendiente` los documentos que quedaron en `error`, para que se procesen de nuevo. Los que quedaron `sin texto` no: van a dar lo mismo.
- **Recibe:** el token de cancelación.
- **Devuelve:** cuántos documentos pasó a pendiente.
- **Efectos:** actualiza filas de `rag.Documento`.
- **La llaman:** el endpoint `POST /documentos/reintentar`.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Documentos.ReprocesarTodoAsync(ct)

- **Qué hace:** rehace todo desde los PDF: borra la colección de Qdrant, si existe, y todos los fragmentos, y deja todos los documentos en `pendiente`. Espera el turno (`Documentos.Turno`). Le da hasta 10 minutos a cada comando de SQL, porque con muchos fragmentos el borrado tarda. Sirve cuando cambia la forma de cortar, o cuando Qdrant y la base no coinciden.
- **Recibe:** el token de cancelación.
- **Devuelve:** cuántos documentos quedaron pendientes.
- **Efectos:** borra la colección de Qdrant, borra todas las filas de `rag.Fragmento` y actualiza todas las de `rag.Documento`.
- **La llaman:** el endpoint `POST /documentos/reprocesar`.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Documentos.GuardarLoteAsync(conexion, lote, ct)

- **Qué hace:** guarda un lote de documentos importados, todos en una transacción. Abre la conexión si todavía no está abierta.
- **Recibe:** la conexión a la base del RAG, el lote (archivo, título y número de origen de cada uno) y el token de cancelación.
- **Devuelve:** cuántos guardó.
- **Efectos:** inserta filas en `rag.Documento`.
- **La llaman:** Documentos.AnotarOrigenAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Documentos.LimpiarNombre(nombre)

- **Qué hace:** deja solo el nombre del archivo, sin carpetas, y lo corta a 260 caracteres, que es lo que entra en la columna `Archivo`. Si queda vacío, usa `sin-nombre.pdf`.
- **Recibe:** el nombre como llegó.
- **Devuelve:** el nombre limpio.
- **Efectos:** ninguno.
- **La llaman:** Documentos.AnotarSubidosAsync y Documentos.AnotarOrigenAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Documentos.Titulo(archivo)

- **Qué hace:** arma el título de un documento: el nombre del archivo sin la extensión.
- **Recibe:** el nombre del archivo.
- **Devuelve:** el título.
- **Efectos:** ninguno.
- **La llaman:** Documentos.AnotarSubidosAsync y Documentos.AnotarOrigenAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

## RagBasico.Api\ErrorDeServicio.cs

- **Responsabilidad:** un tipo de error propio para Ollama, Qdrant y la base de origen, con el nombre del servicio y el código HTTP que le corresponde, así el manejador de errores puede decir qué falló.
- **Contiene:** la clase `ErrorDeServicio`, con las propiedades `Servicio` (`Ollama`, `Qdrant` o `SQL de origen`) y `CodigoHttp` (503 si no responde, 502 si respondió con un error, 400 si falta configurarlo).
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

### Clase ErrorDeServicio

- **Responsabilidad:** heredar de `Exception` y sumarle el servicio y el código HTTP.
- **Recibe en el constructor:** el servicio, el código HTTP, el mensaje y, si hay, el error original, que queda en `InnerException`.
- **La registra:** nadie. La lanzan DetectorDeCaidas.SendAsync, Ollama.VerificarAsync, Qdrant.VerificarAsync, Documentos.AnotarOrigenAsync y Documentos.LeerContenidoAsync. La reconocen ManejadorDeErrores.Describir, para el código HTTP, y Errores.EsCaida, para saber si el servicio no responde.
- **Desde:** fase-8-validaciones.
- **Cambió en:** nada todavía.

## RagBasico.Api\Errores.cs

- **Responsabilidad:** las reglas sobre los errores que comparten varias piezas.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones.

### Clase Errores

- **Responsabilidad:** un solo lugar para decidir qué error es un servicio caído, así todas las piezas usan el mismo criterio. Es estática: no guarda nada.
- **Recibe en el constructor:** nada; una clase estática no tiene constructor.
- **La registra:** nadie. Se usa directo: `Errores.EsCaida(...)`.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Errores.EsCaida(error)

- **Qué hace:** dice si un error muestra que un servicio, Ollama o Qdrant, no responde: no se pudo conectar o no contestó a tiempo (un `TaskCanceledException` con un `TimeoutException` adentro). El `DetectorDeCaidas` convierte el error de conexión en un `ErrorDeServicio` con código 503, que cuenta como caída; uno con 502, que es el servicio contestando un error, no. Un error que el servicio sí contestó, como un texto que Ollama no acepta o una colección que no existe en Qdrant, no es una caída.
- **Recibe:** el error.
- **Devuelve:** `true` si el servicio no responde.
- **Efectos:** ninguno.
- **La llaman:** Cargador.EsDeUnServidor.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones (reconoce el `ErrorDeServicio`).

## RagBasico.Api\ManejadorDeErrores.cs

- **Responsabilidad:** que ningún error llegue como un 500 genérico: cada error que nadie atrapó sale como un problema con título, detalle y el código HTTP que corresponde, en el formato estándar ProblemDetails (`application/problem+json`).
- **Desde:** fase-8-validaciones.
- **Cambió en:** fase-9-local.

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
  - `ErrorDeServicio`: el código que trae el error. Con 503, el título es "Ollama no responde" o "Qdrant no responde"; con otro código, "respondió con un error".
  - `SqlException`: 503 "SQL Server no responde o devolvió un error", con qué revisar.
  - Tiempo de espera vencido (`TaskCanceledException` con un `TimeoutException` adentro): 504 "Un servicio tardó demasiado".
  - `BadHttpRequestException`, el error de ASP.NET Core cuando no puede leer el pedido (un JSON roto, un parámetro que falta): su propio código, 400, con "El pedido no es válido". Si el código es 413, porque la subida pasa del tope de `Rag:MegasPorSubida`, el título es "La subida es demasiado grande" y el detalle dice de subir menos archivos por vez.
  - Cualquier otro, incluido un `ArgumentException`: 500 "Error inesperado", con su mensaje. Un `ArgumentException` no es culpa de quien pregunta: lo que manda el cliente ya lo controlan Validador.RevisarPregunta y Buscador.ElegirModo, así que si aparece es de la API, por ejemplo una configuración inválida.
- **Recibe:** el error.
- **Devuelve:** un `ProblemDetails` con el código, el título y el detalle.
- **Efectos:** ninguno.
- **La llaman:** ManejadorDeErrores.TryHandleAsync.
- **Desde:** fase-8-validaciones.
- **Cambió en:** fase-9-local (el 413 de una subida demasiado grande).

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
- **Cambió en:** fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores, fase-7-qdrant, fase-9-local.

| Record | Campo | Tipo | Para qué |
|---|---|---|---|
| `PedidoPregunta` | `Pregunta` | `string` | La pregunta que llega en el cuerpo de `POST /preguntar` |
| `PedidoPregunta` | `Modo` | `string?` | El modo de búsqueda: `palabras`, `vectores` o `hibrida`. Si falta, `hibrida` |
| `RespuestaModelo` | `Texto` | `string` | Lo que escribió el modelo |
| `RespuestaModelo` | `MsCarga`, `MsLectura`, `MsEscritura` | `int` | Lo que tardó Ollama en cargar el modelo (0 si ya estaba), en leer el pedido y en escribir la respuesta |
| `RespuestaModelo` | `TokensPregunta`, `TokensRespuesta` | `int` | Cuántos tokens leyó y cuántos escribió |
| `Fragmento` | `FragmentoId` | `int` | El número del fragmento en `rag.Fragmento` |
| `Fragmento` | `Archivo` | `string` | El PDF de donde salió |
| `Fragmento` | `Texto` | `string` | El texto, que empieza con `Documento: <nombre>` |
| `Fuente` | `Numero` | `int` | El número con que la cita el modelo: `[1]`, `[2]`... |
| `Fuente` | `Archivo`, `FragmentoId` | `string`, `int` | El PDF y el fragmento exacto |
| `RespuestaRag` | `Texto` | `string` | La respuesta, o la frase de "no encontré" |
| `RespuestaRag` | `Fuentes` | `List<Fuente>` | Las fuentes, en el orden en que se le pasaron al modelo. Vacía si no encontró |
| `RespuestaRag` | `MsBusqueda`, `MsModelo` | `int` | Lo que tardó la búsqueda y lo que tardó el modelo, medido desde la API |
| `RespuestaRag` | `MsCarga`, `MsLectura`, `MsEscritura` | `int` | Los tiempos de Ollama |
| `PuntoQdrant` | `Id` | `int` | El id del punto en Qdrant, que es el número de fragmento de SQL |
| `PuntoQdrant` | `Vector` | `float[]` | El vector, de 1024 números |
| `PuntoQdrant` | `DocumentoId` | `int` | El número de documento, que se guarda como dato extra (payload) y sirve para borrar todos los puntos de un documento |
| `InfoColeccion` | `Estado` | `string` | El color que informa Qdrant: `green` si está todo listo, `yellow` si está optimizando |
| `InfoColeccion` | `Puntos`, `VectoresIndexados` | `int` | Cuántos puntos tiene la colección, y cuántos vectores están en el índice HNSW |
| `Chequeo` | `Nombre`, `Estado`, `Detalle` | `string` | Qué se revisó, cómo dio (`ok`, `aviso` o `error`) y el detalle, que dice qué dirección se probó y qué hacer |
| `InformeSalud` | `Estado` | `string` | El estado general: `error` si algún chequeo dio error, `aviso` si alguno dio aviso, `ok` si no |
| `InformeSalud` | `Chequeos` | `List<Chequeo>` | Los cinco chequeos, o seis si hay una base de origen configurada |
| `ArchivoSubido` | `Nombre` | `string` | El nombre del archivo que llegó a la página |
| `ArchivoSubido` | `Contenido` | `byte[]` | Sus bytes |
| `InformeAnotado` | `Nuevos`, `SinCambios` | `int` | Cuántos documentos quedaron pendientes por primera vez, y cuántos ya estaban igual |
| `InformeAnotado` | `Reemplazados`, `Rechazados` | `int` | Cuántos cambiaron y vuelven a pendiente, y cuántos no eran un PDF. Al importar quedan en 0 |
| `InformeAnotado` | `Detalle` | `List<string>` | Una línea por cada archivo que no entró como nuevo, o el resumen de lo importado |
| `Pendiente` | `DocumentoId` | `int` | El número del documento en `rag.Documento` |
| `Pendiente` | `Archivo` | `string` | El nombre del PDF |
| `Pendiente` | `OrigenId` | `string?` | Su número en la base de origen; `null` si se arrastró a la página |
| `DocumentoFila` | `DocumentoId`, `Archivo`, `OrigenId` | `int`, `string`, `string?` | El documento, como en `Pendiente` |
| `DocumentoFila` | `Estado`, `Detalle` | `string`, `string?` | El estado (`pendiente`, `listo`, `sin texto` o `error`) y el motivo, si falló o si el texto salió dudoso |
| `DocumentoFila` | `Fragmentos` | `int` | Cuántos fragmentos tiene |
| `DocumentoFila` | `FechaCarga` | `DateTime` | Cuándo se anotó |
| `ResumenDocumentos` | `Listos`, `Pendientes`, `SinTexto`, `ConError` | `int` | Cuántos documentos hay en cada estado |
| `ResumenDocumentos` | `Fragmentos` | `int` | Cuántos fragmentos hay en total |
| `ResumenDocumentos` | `DocumentosPorMinuto` | `double` | La velocidad de la carga en los últimos 10 minutos |
| `ResumenDocumentos` | `TiempoQueFalta` | `string` | Lo que falta, en palabras; vacío si no hay pendientes |
| `ResumenDocumentos` | `Pagina` | `List<DocumentoFila>` | Una página de 50 documentos de la lista |

## RagBasico.Api\Ollama.cs

- **Responsabilidad:** hablar con Ollama por HTTP. Manda el mismo JSON que se mandaría con Postman y devuelve lo que contesta el modelo.
- **Contiene:** la clase `Ollama`, con la constante pública `Dimension` (1024, el tamaño de cada vector), y cinco records privados para leer las respuestas: `RespuestaChat` (el mensaje y los tiempos de `/api/chat`), `MensajeChat` (el texto que escribió el modelo), `RespuestaEmbed` (los vectores de `/api/embed`), `RespuestaModelos` y `ModeloInstalado` (los modelos de `/api/tags`).
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-5-vectores, fase-7-qdrant, fase-8-validaciones, fase-9-local.

### Clase Ollama

- **Responsabilidad:** ser el único lugar del código que conoce el formato de la API de Ollama.
- **Recibe en el constructor:** el `HttpClient` que arma Program.cs, con la dirección (`Conexiones:Ollama`) y el tiempo de espera (`Ollama:SegundosDeEspera`), y la configuración, de donde lee `Conexiones:ModeloChat`, `Conexiones:ModeloEmbeddings`, `Ollama:InstruccionPregunta` y `Ollama:MantenerCargado`.
- **La registra:** Program.cs, con `AddHttpClient<Ollama>`, que crea un cliente tipado nuevo en cada pedido.
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-5-vectores (los embeddings), fase-7-qdrant (la lista de modelos y el calentamiento), fase-9-local (lee los nombres de los dos modelos del bloque `Conexiones`).

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
- **La llaman:** Ollama.EmbeddingPreguntaAsync, Ollama.CalentarAsync, Cargador.ProcesarAsync y Salud.RevisarDimensionAsync.
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

## RagBasico.Api\Procesador.cs

- **Responsabilidad:** el trabajo en segundo plano: arranca con la API y da vueltas procesando de a un documento pendiente. Si se apaga la API, al volver sigue donde quedó, porque el estado de cada documento está en la base.
- **Contiene:** la clase `Procesador` y sus dos esperas privadas: `EsperaSinTrabajo` (5 segundos) y `EsperaConError` (30 segundos).
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Clase Procesador

- **Responsabilidad:** hereda de `BackgroundService`, la clase de ASP.NET Core para un trabajo que corre mientras la API está prendida, sin que nadie le mande un pedido. Por eso la carga sigue aunque se cierre la página.
- **Recibe en el constructor:** la fábrica de alcances (`IServiceScopeFactory`), para pedir un `Cargador` nuevo en cada vuelta, y el log. No puede recibir el `Cargador` directo: el `Procesador` vive lo que vive la API y el `Cargador` se crea uno por pedido.
- **La registra:** Program.cs, con `AddHostedService<Procesador>`.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Procesador.ExecuteAsync(apagado)

- **Qué hace:** el bucle. En cada vuelta pide un `Cargador` y llama a Cargador.ProcesarSiguienteAsync. Si procesó un documento, sigue enseguida con el próximo. Si no había ninguno, espera 5 segundos. Si un servidor no responde, lo anota en el log una sola vez, no cada 30 segundos, y espera 30 segundos antes de reintentar. Termina cuando se apaga la API.
- **Recibe:** el token de cancelación, que se dispara cuando se apaga la API.
- **Devuelve:** nada.
- **Efectos:** los de Cargador.ProcesarSiguienteAsync, y escribe en el log.
- **La llaman:** ASP.NET Core, una sola vez, cuando arranca la API.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

## RagBasico.Api\Program.cs

- **Responsabilidad:** el punto de entrada de la API: arma la aplicación, registra los servicios, pone el tope de las subidas, carga los modelos, atiende los errores, sirve las páginas, declara los endpoints y la deja escuchando.
- **Contiene:** instrucciones de nivel superior, sin clase: el compilador arma solo el método `Main`. Se lee en ocho bloques:
  - **Armado:** `WebApplication.CreateBuilder(args)` lee `appsettings.json`, las variables de entorno y los argumentos. Después `AddJsonFile("conexiones.json", optional: true, ...)` suma el archivo de las conexiones de la oficina, si existe al lado de `appsettings.json`: sus líneas pisan a las de `appsettings.json`. Git lo ignora, así los nombres de los servidores y las claves no se suben.
  - **Servicios:** `AddHttpClient<Ollama>` registra el cliente de Ollama con `Conexiones:Ollama` y `Ollama:SegundosDeEspera`, y `AddHttpClient<Qdrant>` el de Qdrant, que se prepara con Qdrant.Configurar. Delante de cada cliente, `AddHttpMessageHandler` pone un `DetectorDeCaidas`, que dice qué servicio no responde. `AddScoped` registra `Documentos`, el `Cargador`, el `Buscador`, el `Redactor` y la `Salud`: un objeto nuevo de cada uno por pedido. Cuando un endpoint o un constructor pide uno, ASP.NET Core se lo crea: eso es la inyección de dependencias. `AddHostedService<Procesador>` registra el trabajo en segundo plano, que arranca con la API. `AddProblemDetails` y `AddExceptionHandler<ManejadorDeErrores>` hacen que los errores salgan como ProblemDetails, y `RouteHandlerOptions.ThrowOnBadRequest` hace que un pedido que no se puede leer, como un JSON roto, llegue al manejador con su motivo en cualquier entorno, no solo en Development.
  - **Tope de subida:** `Rag:MegasPorSubida` (200) va en los dos lugares donde ASP.NET Core corta por tamaño: el servidor (`ConfigureKestrel`, `MaxRequestBodySize`) y la lectura del formulario (`FormOptions`, `MultipartBodyLengthLimit`).
  - **Calentamiento:** cuando la API ya está escuchando (`ApplicationStarted`), carga los dos modelos en Ollama en segundo plano con Ollama.CalentarAsync, así la primera pregunta no tarda 30 segundos. `Task.Run` lo corre sin hacer esperar el arranque; si falla, queda en el log.
  - **Errores:** `UseExceptionHandler` va primero en el camino de cada pedido: si algo lanza un error que nadie atrapó, lo atiende el `ManejadorDeErrores`. `UseStatusCodePages` le pone un cuerpo ProblemDetails a las respuestas de error que salen vacías, como un 404 (una ruta que no existe), un 405 (un método que no corresponde) o un 415 (un cuerpo que no es JSON).
  - **Páginas:** `UseDefaultFiles` hace que la raíz, `/`, devuelva `wwwroot\index.html`, y `UseStaticFiles` sirve los archivos de `wwwroot`. Por eso la información del proyecto está en `/info`.
  - **Endpoints:** cada `app.MapGet(...)`, `app.MapPost(...)` o `app.MapDelete(...)` asocia una ruta con la función que la atiende. `GET /buscar` y `POST /preguntar` controlan la pregunta con Validador.RevisarPregunta (con el máximo de `Rag:LargoMaximoPregunta`) y el modo con Buscador.ElegirModo; si algo no sirve, devuelven `Results.Problem` con código 400 y el motivo.
  - **Arranque:** `app.Run()` deja la API escuchando en la dirección de `launchSettings.json` hasta que se la frena con Ctrl+C.
- **Ya no controla nada al arrancar:** hasta la fase 8, si faltaba la base o un modelo la API no arrancaba. Ahora arranca siempre, y lo que falta lo muestra `GET /salud` en la franja de estado de las páginas, con qué servidor es y qué hacer.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-8-validaciones, fase-9-local.

### GET /info

- **Recibe:** nada.
- **Devuelve:** un JSON con el nombre del proyecto, la fase, la versión de .NET y `origenConfigurado`, que dice si `Conexiones:SqlOrigen` tiene algo. El proyecto y la fase salen de la configuración (`Rag:Proyecto` y `Rag:Fase`).
- **La atiende:** una función sin nombre (lambda) declarada en `Program.cs` con `app.MapGet("/info", ...)`.
- **Desde:** fase-0-base.
- **Cambió en:** fase-6-hibrida (pasa de `/` a `/info`: la raíz muestra la página de la demo), fase-9-local (`origenConfigurado`: la página de documentos prende el botón de importar solo si hay base de origen).

### POST /documentos

- **Recibe:** un formulario (`multipart/form-data`) con uno o más PDF en el campo `archivos`. Es lo que manda la página cuando se le sueltan archivos.
- **Devuelve:** el `InformeAnotado`: cuántos nuevos, sin cambios, reemplazados y rechazados, y el detalle. Los PDF solo quedan anotados como pendientes: los procesa el trabajo en segundo plano. Si la subida pasa de `Rag:MegasPorSubida`, un error 413; si no es un formulario o no trae ningún archivo, un 400.
- **La atiende:** una función de `Program.cs` que lee cada archivo en memoria, arma la lista de `ArchivoSubido` y llama a Documentos.AnotarSubidosAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### POST /documentos/importar

- **Recibe:** nada. La lista sale de la base de origen, con `Conexiones:ConsultaLista`.
- **Devuelve:** el `InformeAnotado`: cuántos documentos de la base de origen quedaron pendientes y cuántos ya estaban anotados. No trae los PDF: cada uno se lee cuando le toca. Sin `Conexiones:SqlOrigen`, un error 400 que dice qué claves completar.
- **La atiende:** una función de `Program.cs` que llama a Documentos.AnotarOrigenAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### GET /documentos

- **Recibe:** si se quiere, `estado` (`pendiente`, `listo`, `sin texto` o `error`) y `pagina` en la dirección. Por ejemplo: `/documentos?estado=error&pagina=2`. Sin `estado`, todos; sin `pagina`, la primera.
- **Devuelve:** el `ResumenDocumentos`: cuántos documentos hay en cada estado, cuántos fragmentos, la velocidad de la carga, lo que falta y una página de 50 documentos. Con un estado que no existe, un error 400 que lista los estados.
- **La atiende:** una función de `Program.cs` que controla el estado contra `Documentos.Estados` y llama a Documentos.ResumirAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### DELETE /documentos/{id:int}

- **Recibe:** el número del documento en la ruta. `{id:int}` hace que la ruta solo coincida si es un número entero.
- **Devuelve:** 204, sin cuerpo, si lo borró, con sus fragmentos y sus vectores. Si no existe, un error 404.
- **La atiende:** una función de `Program.cs` que llama a Documentos.BorrarAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### POST /documentos/reintentar

- **Recibe:** nada.
- **Devuelve:** un JSON con `cuantos`: cuántos documentos pasaron de `error` a `pendiente`.
- **La atiende:** una función de `Program.cs` que llama a Documentos.ReintentarAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### POST /documentos/reprocesar

- **Recibe:** nada.
- **Devuelve:** un JSON con `cuantos`: cuántos documentos quedaron pendientes para procesar de nuevo. Antes borra la colección de Qdrant y todos los fragmentos.
- **La atiende:** una función de `Program.cs` que llama a Documentos.ReprocesarTodoAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### GET /buscar

- **Recibe:** la pregunta en el parámetro `q` de la dirección y, si se quiere, el modo en `modo`: `palabras`, `vectores` o `hibrida`, que es el modo por defecto. Por ejemplo: `/buscar?q=requisitos reintegro&modo=vectores`.
- **Devuelve:** la lista de `Fragmento` encontrados con ese modo, hasta `Rag:TopK`. Si falta `q`, si la pregunta no sirve (vacía, más larga que `Rag:LargoMaximoPregunta`, sin letras) o si el modo no existe, un error 400 con el motivo. Con `vectores` o `hibrida` y Qdrant apagado, un 503 que dice que Qdrant no responde.
- **La atiende:** una función de `Program.cs` que controla el modo con Buscador.ElegirModo y llama a Buscador.BuscarAsync.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-5-vectores (el parámetro `modo`), fase-6-hibrida (la híbrida, que pasa a ser el modo por defecto), fase-7-qdrant (el modo `vectores-qdrant`, y devuelve un objeto con el aviso en lugar de la lista sola), fase-8-validaciones (valida la pregunta, y los errores salen como ProblemDetails), fase-9-local (quedan tres modos y vuelve a devolver la lista sola).

### POST /preguntar

- **Recibe:** un JSON con la pregunta y, si se quiere, el modo: `{ "pregunta": "...", "modo": "palabras" }`, que ASP.NET Core convierte en un `PedidoPregunta`. Sin modo, usa la híbrida.
- **Devuelve:** el `RespuestaRag`: la respuesta de Qwen con citas `[n]`, las fuentes y los tiempos. Si los documentos no alcanzan, la frase "No encontré esa información en los documentos." sin fuentes. Si la pregunta no sirve o el modo no existe, un error 400 con el motivo; si un servicio no responde, un 503 que dice cuál.
- **La atiende:** una función de `Program.cs` que controla el modo con Buscador.ElegirModo y llama a Redactor.ResponderAsync.
- **Desde:** fase-1-ollama.
- **Cambió en:** fase-2-rag-minimo (responde con los documentos, no con lo que sabe Qwen), fase-5-vectores (el modo), fase-6-hibrida (la híbrida por defecto), fase-7-qdrant (el aviso), fase-8-validaciones (valida la pregunta, y los errores salen como ProblemDetails), fase-9-local (quedan tres modos y se va el aviso).

### GET /salud

- **Recibe:** nada.
- **Devuelve:** el `InformeSalud`: el estado general y los chequeos (SQL Server, Full-Text, modelos de Ollama, dimensión de los vectores, Qdrant y, si está configurada, la base de origen). Con estado `error`, el código HTTP es 503, pero igual trae el informe; con `aviso` u `ok`, 200. Es lo que dibuja la franja de estado de las dos páginas.
- **La atiende:** una función de `Program.cs` que llama a Salud.RevisarAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-9-local (el chequeo de la base de origen; cada chequeo dice qué dirección probó; Qdrant apagado pasa de aviso a error).

## RagBasico.Api\Properties\launchSettings.json

- **Responsabilidad:** dice cómo arranca la API cuando se la corre con `dotnet run` o con F5 en Visual Studio: la dirección, el puerto, el entorno y si Visual Studio abre el navegador.
- **Contiene:** un solo perfil, `http`: escucha en `http://127.0.0.1:508N`, donde N es el número de la fase, con el entorno `Development`, y con F5 Visual Studio abre el navegador (`dotnet run` no lo abre: hay que abrir la dirección a mano). Así dos fases pueden correr a la vez. El template traía también un perfil `https` con puertos al azar, que se sacó porque chocaría entre fases.
- **Por qué 127.0.0.1:** es la misma dirección que usan Ollama y Qdrant, que escuchan solo en IPv4, así todo el proyecto usa una sola. La consecuencia es que la API también escucha solo en IPv4: se la llama con `http://127.0.0.1:508N`, como hacen el archivo `.http` y el navegador que abre el perfil. Con `localhost`, los clientes de .NET (PowerShell, `WebClient`, `HttpClient`) prueban primero IPv6 y tardan unos 2 segundos más en conectarse.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-8-validaciones, fase-9-local.

## RagBasico.Api\Qdrant.cs

- **Responsabilidad:** hablar con Qdrant por HTTP, con JSON armado a mano, como con Ollama: sin el SDK de Qdrant, para que se vea qué se manda.
- **Contiene:** la clase `Qdrant`.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones, fase-9-local.

### Clase Qdrant

- **Responsabilidad:** ser el único lugar del código que conoce la API de Qdrant. Las respuestas se leen con `JsonDocument`, que recorre el JSON sin declarar una clase por cada respuesta. Desde esta fase, Qdrant es el único lugar donde están los vectores: SQL Server guarda solo los textos.
- **Recibe en el constructor:** el `HttpClient` que arma Program.cs y la configuración, de donde lee `Conexiones:ColeccionQdrant`. Tiene la propiedad `Coleccion`, con ese nombre.
- **La registra:** Program.cs, con `AddHttpClient<Qdrant>`.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-9-local (lee el nombre de la colección de `Conexiones:ColeccionQdrant`; se van las funciones que comparaban Qdrant con los vectores de SQL: `ListarIdsAsync`, `LeerVectorAsync` y `BorrarPuntosAsync`).

### Qdrant.Configurar(cliente, config)

- **Qué hace:** prepara el `HttpClient` de Qdrant: la dirección (`Conexiones:Qdrant`), el tiempo de espera (`Qdrant:SegundosDeEspera`) y, si `Conexiones:ClaveQdrant` trae una clave, el encabezado `api-key` en todos los pedidos. Un Qdrant de esta PC no lleva clave; uno en un servidor de la oficina, sí.
- **Recibe:** el `HttpClient` y la configuración.
- **Devuelve:** nada. Cambia el cliente que recibe.
- **Efectos:** ninguno fuera de ese cliente.
- **La llaman:** Program.cs, en `AddHttpClient<Qdrant>`.
- **Desde:** fase-9-local.
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
- **La llaman:** Qdrant.AsegurarColeccionAsync, Qdrant.BorrarDeDocumentoAsync, Buscador.PorVectoresAsync, Documentos.ReprocesarTodoAsync y Salud.RevisarQdrantAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.CrearColeccionAsync(ct)

- **Qué hace:** crea la colección con `PUT /collections/<colección>`: vectores de 1024 números comparados por coseno, y los dos umbrales bajos. `indexing_threshold` en 10 KB hace que el índice HNSW se arme enseguida, y `full_scan_threshold` en 10 KB hace que la búsqueda lo use. Los dos se miden en kilobytes y cada vector ocupa 4 KB: con los valores por defecto, unos 2.500 vectores, Qdrant buscaría recorriendo todos los vectores.
- **Recibe:** el token de cancelación.
- **Devuelve:** nada.
- **Efectos:** crea la colección en Qdrant.
- **La llaman:** Qdrant.AsegurarColeccionAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.AsegurarColeccionAsync(ct)

- **Qué hace:** crea la colección si todavía no existe. Así no hace falta ningún paso de instalación en Qdrant: la colección aparece sola al cargar el primer documento.
- **Recibe:** el token de cancelación.
- **Devuelve:** nada.
- **Efectos:** uno o dos pedidos a Qdrant.
- **La llaman:** Cargador.ProcesarAsync, antes de subir los vectores de un documento.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Qdrant.BorrarColeccionAsync(ct)

- **Qué hace:** borra la colección con todos sus puntos, con `DELETE /collections/<colección>`.
- **Recibe:** el token de cancelación.
- **Devuelve:** lo que contestó Qdrant: `true` si dice que la borró.
- **Efectos:** borra la colección en Qdrant.
- **La llaman:** Documentos.ReprocesarTodoAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.LeerInfoAsync(ct)

- **Qué hace:** lee el estado de la colección con `GET /collections/<colección>`: el color (`green` si está todo listo, `yellow` si está optimizando), cuántos puntos tiene (`points_count`) y cuántos vectores están en el índice HNSW (`indexed_vectors_count`).
- **Recibe:** el token de cancelación.
- **Devuelve:** un `InfoColeccion`.
- **Efectos:** un pedido a Qdrant.
- **La llaman:** Salud.RevisarQdrantAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.SubirAsync(puntos, ct)

- **Qué hace:** sube puntos con `PUT /collections/<colección>/points?wait=true`: el número de fragmento como id, el vector, y el número de documento como dato extra (payload), que después sirve para borrar todos los puntos de un documento. Si el id ya existe, lo pisa. Con `wait=true`, Qdrant contesta cuando los puntos ya están guardados.
- **Recibe:** la lista de `PuntoQdrant` y el token de cancelación.
- **Devuelve:** nada.
- **Efectos:** escribe en la colección.
- **La llaman:** Cargador.ProcesarAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Qdrant.BorrarDeDocumentoAsync(documentoId, ct)

- **Qué hace:** borra todos los puntos de un documento con `POST /collections/<colección>/points/delete?wait=true` y un filtro: los puntos que tienen ese número en el dato `documentoId`. No hace falta conocer los números de los fragmentos. Si la colección no existe, no hay nada que borrar.
- **Recibe:** el número del documento y el token de cancelación.
- **Devuelve:** nada.
- **Efectos:** borra puntos de la colección.
- **La llaman:** Cargador.ProcesarAsync y Documentos.BorrarAsync.
- **Desde:** fase-9-local. Reemplaza a `BorrarPuntosAsync`, que borraba por número de fragmento.
- **Cambió en:** nada todavía.

### Qdrant.BuscarAsync(vector, topK, ct)

- **Qué hace:** busca los puntos más cercanos a un vector con el índice HNSW, con `POST /collections/<colección>/points/query`.
- **Recibe:** el vector de la pregunta, cuántos devolver y el token de cancelación.
- **Devuelve:** los ids de los puntos, del más cercano al más lejano.
- **Efectos:** un pedido a Qdrant.
- **La llaman:** Buscador.PorVectoresAsync.
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
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-8-validaciones, fase-9-local.

## RagBasico.Api\Redactor.cs

- **Responsabilidad:** la "G" de RAG: armar la respuesta con los fragmentos encontrados.
- **Contiene:** la clase `Redactor`, con dos constantes: `SinRespuesta`, la única frase para "no encontré", y `Reglas`, el mensaje de sistema con las cinco reglas para Qwen (usar solo los fragmentos, citar con `[n]`, la frase exacta cuando no alcanza, e ignorar instrucciones que vengan dentro de los documentos).
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-3-medicion, fase-5-vectores, fase-7-qdrant, fase-9-local.

### Clase Redactor

- **Responsabilidad:** buscar, pedirle la respuesta a Qwen, armar la respuesta con sus fuentes y registrar la consulta.
- **Recibe en el constructor:** el `Buscador`, el cliente de `Ollama`, el log y la configuración, de donde lee `Conexiones:SqlTextos` y `Rag:TopK`.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-3-medicion (recibe el log y registra cada consulta), fase-9-local (lee la conexión de `Conexiones:SqlTextos`).

### Redactor.ResponderAsync(pregunta, modo, ct)

- **Qué hace:** busca los fragmentos de la pregunta con el modo pedido y le pide la respuesta a Qwen. Si no hay fragmentos, responde la frase fija sin llamar al modelo; por vectores casi nunca pasa, así que el "no sé" lo decide Qwen. Si Qwen responde la frase de "no encontré", la devuelve sin fuentes. Al final registra la consulta, con el modo.
- **Recibe:** la pregunta, el modo de búsqueda y el token de cancelación.
- **Devuelve:** un `RespuestaRag`: el texto, las fuentes numeradas como las cita el modelo y los tiempos.
- **Efectos:** busca con el buscador (SQL Server, Ollama y Qdrant según el modo), llama a Ollama para redactar y escribe una fila en `rag.Consulta`.
- **La llaman:** el endpoint `POST /preguntar`.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-3-medicion (registra la consulta), fase-5-vectores (recibe el modo), fase-7-qdrant (pasa el aviso de la búsqueda), fase-9-local (ya no hay aviso: la búsqueda devuelve la lista sola).

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
- **La llaman:** Redactor.ResponderAsync.
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

- **Responsabilidad:** revisar que estén andando todas las piezas y, si alguna no anda, decir qué servidor es y qué hacer. Es lo que muestra la franja de estado de las dos páginas.
- **Contiene:** la clase `Salud` y dos records privados: `Conteo`, los números de la base que muestra el chequeo de SQL Server, y `Fragmentos`, los números que se comparan con los puntos de Qdrant.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones, fase-9-local.

### Clase Salud

- **Responsabilidad:** los chequeos de `GET /salud`: cinco siempre y uno más, la base de origen, si está configurada. Cada uno atrapa su propio error, así un chequeo que falla no impide ver los demás, y su detalle nombra la dirección o el servidor que probó: con los servidores repartidos en varias máquinas, hay que saber cuál falló.
- **Recibe en el constructor:** los clientes de `Ollama` y de `Qdrant`, y la configuración, de donde lee `Conexiones:SqlTextos`, `Conexiones:SqlOrigen`, `Conexiones:ConsultaLista`, `Conexiones:Ollama`, `Conexiones:Qdrant`, `Conexiones:ModeloChat` y `Conexiones:ModeloEmbeddings`.
- **La registra:** Program.cs, con `AddScoped`.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones (los chequeos de arranque), fase-9-local (se van los chequeos de arranque, porque la API arranca siempre; se suma la base de origen; cada chequeo dice qué dirección probó).

### Salud.RevisarAsync(ct)

- **Qué hace:** corre los chequeos y arma el estado general: `error` si falla algo sin lo que no se puede preguntar, `aviso` si algo anda a medias, y `ok` si anda todo. El chequeo de la base de origen corre solo si `Conexiones:SqlOrigen` tiene algo.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `InformeSalud`.
- **Efectos:** los de cada chequeo: lee SQL Server y hace pedidos a Ollama y a Qdrant.
- **La llaman:** el endpoint `GET /salud`.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-9-local (el chequeo de la base de origen).

### Salud.Describir(conexion)

- **Qué hace:** saca de una cadena de conexión el servidor y la base, sin el usuario ni la clave, para mostrarlos en un mensaje. Si la cadena no se puede leer, lo dice en lugar de fallar.
- **Recibe:** la cadena de conexión.
- **Devuelve:** un texto como `servidor localhost\SQLEXPRESS, base RagBasico_F9`.
- **Efectos:** ninguno.
- **La llaman:** Salud.RevisarSqlAsync y Salud.RevisarOrigenAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Salud.RevisarSqlAsync(ct)

- **Qué hace:** se conecta a la base del RAG y cuenta los documentos listos, los pendientes, los que quedaron sin cargar (`error` o `sin texto`) y los fragmentos. Avisa si todavía no hay documentos listos. Si falla, dice qué pasó según el número de error de SQL Server: 208, faltan las tablas; 4060, no existe la base o el usuario no tiene permiso; cualquier otro, no se pudo conectar. En los dos primeros dice que hay que correr `sql\base.sql`.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `Chequeo`: `ok`, `aviso` o `error`.
- **Efectos:** lee `rag.Documento` y `rag.Fragmento`.
- **La llaman:** Salud.RevisarAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones (dice qué revisar), fase-9-local (cuenta por estado, nombra el servidor y la base, y distingue la base que falta de las tablas que faltan).

### Salud.RevisarFullTextAsync(ct)

- **Qué hace:** controla que el SQL Server tenga instalado el Full-Text y que no tenga cambios pendientes: mientras tanto, la búsqueda por palabras no ve todo.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `Chequeo`: `ok`, `aviso` si hay cambios pendientes, o `error` si no está instalado o no se pudo revisar.
- **Efectos:** lee `FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')` y `OBJECTPROPERTYEX(..., 'TableFulltextPendingChanges')`.
- **La llaman:** Salud.RevisarAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-9-local (controla que esté instalado).

### Salud.RevisarModelosAsync(ct)

- **Qué hace:** pide a Ollama la lista de modelos instalados y controla que estén los dos de la configuración. Si falta alguno, dice con qué comando bajarlo: un `ollama pull` por cada modelo que falta. Si Ollama no responde, dice qué revisar: `Conexiones:Ollama` y, si está en otra máquina, que escuche en la red.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `Chequeo`: `ok`, o `error` si Ollama no responde o falta un modelo.
- **Efectos:** un pedido a Ollama.
- **La llaman:** Salud.RevisarAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-9-local (nombra la dirección de Ollama y dice qué revisar).

### Salud.RevisarDimensionAsync(ct)

- **Qué hace:** calcula el vector de una palabra y controla que tenga 1024 números, como la colección de Qdrant. Si alguien cambia el modelo de embeddings por otro de otra dimensión, este chequeo lo dice.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `Chequeo`: `ok` o `error`.
- **Efectos:** un pedido a Ollama.
- **La llaman:** Salud.RevisarAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

### Salud.RevisarQdrantAsync(ct)

- **Qué hace:** controla que Qdrant responda y que tenga un punto por cada fragmento de los documentos listos.
  - Si no responde, es un `error`: sin Qdrant no hay búsqueda por significado ni híbrida.
  - Si la colección todavía no existe, es un `aviso`: se crea sola al cargar el primer documento.
  - Si no quedan documentos pendientes y los puntos no coinciden con los fragmentos, es un `aviso` que dice de apretar "Reprocesar todo". Mientras hay pendientes no compara, porque los números se mueven.
  - Si Qdrant responde pero SQL Server no, es un `aviso`: no se pudo comparar.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `Chequeo`: `ok`, `aviso` o `error`.
- **Efectos:** pedidos a Qdrant, y lee `rag.Fragmento` y `rag.Documento` por medio de Salud.ContarFragmentosAsync.
- **La llaman:** Salud.RevisarAsync.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-9-local (Qdrant apagado es un error y no un aviso, porque ya no hay respaldo en SQL; compara los puntos con los fragmentos; ya no controla la versión).

### Salud.RevisarOrigenAsync(ct)

- **Qué hace:** controla que se pueda conectar a la base de origen y que la consulta de la lista ande: la cuenta poniéndola adentro de un `SELECT COUNT(*)`, así la prueba sin traer las filas. Si falla es un aviso y no un error: preguntar sigue andando; importar, no.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `Chequeo`: `ok`, con cuántos documentos devuelve la consulta, o `aviso`.
- **Efectos:** lee la base de origen.
- **La llaman:** Salud.RevisarAsync, solo si hay base de origen configurada.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Salud.ContarFragmentosAsync(ct)

- **Qué hace:** cuenta los fragmentos de los documentos listos y los documentos pendientes.
- **Recibe:** el token de cancelación.
- **Devuelve:** un `Fragmentos`, con los dos números.
- **Efectos:** lee `rag.Fragmento` y `rag.Documento`.
- **La llaman:** Salud.RevisarQdrantAsync.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

### Salud.EstaInstalado(instalados, modelo)

- **Qué hace:** dice si un modelo está en la lista de instalados, sin distinguir mayúsculas. Un nombre sin etiqueta, como `llama3`, es el mismo que `llama3:latest`.
- **Recibe:** la lista de modelos instalados y el modelo buscado.
- **Devuelve:** `true` si está.
- **Efectos:** ninguno.
- **La llaman:** Salud.RevisarModelosAsync.
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

## RagBasico.Api\appsettings.json

- **Responsabilidad:** la configuración de la API. Cada clave está explicada en `docs\arquitectura.md`.
- **Contiene:** cinco bloques. `Conexiones` va primero y junta todo lo que cambia entre casa y la oficina: las dos bases, Qdrant, Ollama, los dos modelos y las dos consultas de la base de origen. `Rag`, `Ollama` y `Qdrant` tienen los ajustes que no dependen del lugar, y `Logging`, el nivel del log. Si al lado existe `conexiones.json`, que git ignora, sus líneas pisan a las de este archivo: así los servidores y las claves de la oficina no se suben.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-8-validaciones, fase-9-local.

## RagBasico.Api\wwwroot\documentos.html

- **Responsabilidad:** la página para cargar documentos y seguir la carga. La sirve la propia API en `http://127.0.0.1:5089/documentos.html`.
- **Contiene:** HTML, el estilo propio de esta página y un script, sin nada de internet. Usa `estilos.css` y `estado.js`. El script:
  - sube los PDF que se sueltan en la zona, o que se eligen con un clic, a `POST /documentos`, y muestra el informe: cuántos nuevos, reemplazados, ya cargados y rechazados;
  - prende el botón "Importar de la base de origen" solo si `GET /info` dice que hay una configurada, y entonces llama a `POST /documentos/importar`;
  - muestra los números de `GET /documentos` (listos, pendientes, sin texto, con error y fragmentos), la velocidad y lo que falta, y se actualiza solo: cada 5 segundos si hay pendientes, cada 30 si no;
  - lista los documentos de a 50, con un filtro por estado y los botones "Anterior" y "Siguiente"; cada fila tiene el estado, el motivo si falló y un botón "Borrar", que pide confirmación y llama a `DELETE /documentos/{id}`;
  - "Reintentar los que fallaron" llama a `POST /documentos/reintentar`, y "Reprocesar todo", después de confirmar, a `POST /documentos/reprocesar`;
  - muestra el nombre de cada archivo con `textContent` y no con `innerHTML`, así nunca se interpreta como HTML;
  - muestra el error de la API, con el título y el detalle del ProblemDetails.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

## RagBasico.Api\wwwroot\estado.js

- **Responsabilidad:** la franja de estado que está arriba de las dos páginas: un punto por cada chequeo de `GET /salud`. Verde, anda; amarillo, anda a medias; rojo, no anda. Debajo muestra el detalle de los que no están en verde, que dice qué hacer.
- **Contiene:** una sola función, `mostrarEstado(elemento)`, que cada página llama una vez con el lugar donde dibujar. Adentro tiene `pedirSalud`, que pide `/salud` ahora y cada 15 segundos, y `dibujarSalud`, que arma los puntos. Lee el informe aunque `/salud` responda 503. Si no puede hablar con la API, dice que se arranca con `iniciar.cmd`. Todo queda adentro de `mostrarEstado`, así sus nombres no chocan con los de la página que la usa.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

## RagBasico.Api\wwwroot\estilos.css

- **Responsabilidad:** el estilo común a las dos páginas: los colores, las letras, las tarjetas, los botones, la barra de arriba con los dos enlaces y la franja de estado.
- **Contiene:** los colores como variables de CSS (`--fondo`, `--texto`, `--acento`, `--ok`, `--aviso`, `--error`...) en `:root`, con otro juego para el modo oscuro (`prefers-color-scheme: dark`), y las reglas de `nav`, `.tarjeta`, `button`, `.error`, `.nota`, `#estado` y `.punto`. Lo que usa una sola página queda en el `<style>` de esa página.
- **Desde:** fase-9-local. Hasta la fase 8, el estilo estaba adentro de `index.html`.
- **Cambió en:** nada todavía.

## RagBasico.Api\wwwroot\index.html

- **Responsabilidad:** la página para preguntar: la franja de estado, una caja para la pregunta, el selector de modo (híbrida, solo palabras o solo significado) y la respuesta con sus fuentes y los tiempos. La sirve la propia API en la raíz, `http://127.0.0.1:5089/`.
- **Contiene:** HTML, un estilo corto propio y un script, sin nada de internet, para que ande sin conexión. Usa `estilos.css` y `estado.js`, y arriba tiene los enlaces a "Preguntar" y a "Documentos". El script:
  - manda la pregunta y el modo a `POST /preguntar` con `fetch`;
  - muestra el texto con `textContent` y no con `innerHTML`, así el texto de un documento nunca se interpreta como HTML;
  - lista las fuentes y atenúa las que se le pasaron al modelo pero no citó;
  - no muestra el bloque de fuentes cuando la respuesta es "no encontré", porque cinco documentos debajo de "no encontré" confunden;
  - muestra el error de la API, con el título y el detalle del ProblemDetails, por ejemplo el de un servicio que no responde.
- **Desde:** fase-6-hibrida.
- **Cambió en:** fase-7-qdrant (el modo con Qdrant y el aviso), fase-9-local (quedan tres modos, se va el aviso, suma la franja de estado y el enlace a Documentos, y el estilo común pasa a `estilos.css`).

## RagBasico.slnx

- **Responsabilidad:** la solución, el archivo que se abre con doble clic en Visual Studio. Agrupa los proyectos del repo; hoy, solo `RagBasico.Api`.
- **Contiene:** el formato `.slnx`, el XML que usa .NET 10 por defecto, más corto y legible que el `.sln` de siempre.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## docs\arquitectura.md

- **Responsabilidad:** cómo se arma esta versión: las piezas, los recorridos paso a paso, las tablas, la configuración y lo que está fuera del repo.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-8-validaciones, fase-9-local.

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

## docs\fases\fase-9-local.md

- **Responsabilidad:** qué agregó la fase 9, en qué orden leerla, cómo probarla, qué se midió y qué se decidió.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

## docs\instalacion.md

- **Responsabilidad:** qué hay que instalar en Windows para correr esta versión en una sola PC: cómo instalar cada pieza, cómo comprobar que anda, qué arrancar cada vez y qué hacer si algo falla. Dice desde qué fase hace falta cada cosa.
- **Desde:** fase-7-qdrant.
- **Cambió en:** fase-8-validaciones, fase-9-local.

## docs\manual.md

- **Responsabilidad:** el manual de lectura: en qué orden leer el proyecto para poder explicar cada archivo. Sigue dos recorridos de punta a punta, el de un PDF que se carga y el de una pregunta que se responde, y después lo que sostiene a los dos. De cada archivo dice qué hace, por qué existe, en qué orden leer sus funciones y las preguntas que pueden hacer, con su respuesta.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

## docs\responsabilidades.md

- **Responsabilidad:** este documento: qué hace cada archivo, clase, función y endpoint, y quién lo usa.
- **Desde:** fase-0-base.
- **Cambió en:** fase-1-ollama, fase-2-rag-minimo, fase-3-medicion, fase-4-carga, fase-5-vectores, fase-6-hibrida, fase-7-qdrant, fase-8-validaciones, fase-9-local.

## global.json

- **Responsabilidad:** fija la versión del SDK de .NET con la que se compila: 10.0.403, o el parche más nuevo instalado de la misma banda 10.0.4xx (`rollForward: latestPatch`). Si no hay ninguno, `dotnet` falla con un error que dice qué SDK instalar, en lugar de compilar con otro.
- **Desde:** fase-0-base.
- **Cambió en:** nada todavía.

## iniciar.cmd

- **Responsabilidad:** el doble clic para arrancar todo en esta PC: Qdrant, si hace falta, y la API, que abre el navegador.
- **Contiene:** una sola orden: corre `scripts\iniciar.ps1` con `powershell -NoProfile -ExecutionPolicy Bypass`, desde la carpeta donde está el `.cmd` (`%~dp0`), así anda desde cualquier lugar. Si el script termina con error, hace `pause` para que la ventana no se cierre antes de poder leerlo. Se frena con Ctrl+C.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

## scripts\controlar-docs.ps1

- **Responsabilidad:** controla que la documentación de la fase coincida con el código. Sale con 0 si coincide, y con 1 si encontró problemas, que muestra uno por línea. Una fase no se cierra hasta que sale con 0.
- **Parámetros:** ninguno. La fase sale de `Rag:Fase` en `RagBasico.Api\appsettings.json`.
- **Ejemplo:** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\controlar-docs.ps1`
- **Requiere:** git y Windows PowerShell 5.1. Se corre desde cualquier carpeta del repo, también desde una ruta con tildes.
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

- **Responsabilidad:** crea la base del RAG: corre `sql\base.sql`. Es para la base de casa, porque usa la autenticación de Windows; en un servidor con usuario de SQL, `sql\base.sql` se corre desde SSMS.
- **Parámetros:** `-Base` y `-Servidor`, opcionales. Por defecto salen de `Conexiones:SqlTextos` en `RagBasico.Api\appsettings.json`.
- **Ejemplo:** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1`
- **Requiere:** sqlcmd (`winget install Microsoft.Sqlcmd`), SQL Server con la instancia de la cadena de conexión y Windows PowerShell 5.1.
- **Pasos:** lee la cadena de conexión y corre `sql\base.sql` con `sqlcmd -E -C -b -v Base=<base>`: autenticación de Windows, confiar en el certificado de SQL Express y cortar ante el primer error. Se puede correr de nuevo: cada paso del script de SQL controla si ya está hecho. Si el servidor es de esta PC (`localhost`, `.`, `(local)` o el nombre de la PC), le agrega `lpc:` adelante: se conecta por memoria compartida, sin el servicio SQL Server Browser. Sin el prefijo, el sqlcmd que instala winget le pregunta primero por la instancia al Browser, y si no está andando, lo espera hasta agotar el tiempo y falla aunque SQL Server esté andando. Si el script falla, sugiere revisar que esté andando el servicio de SQL Server, que es el error más común: con el servicio detenido, sqlcmd falla enseguida con `The system cannot find the file specified`, sin decir que es el servicio.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-7-qdrant, fase-9-local.

## scripts\crear-origen-de-prueba.ps1

- **Responsabilidad:** crea una base de origen de prueba, para probar "Importar de la base de origen" en casa, sin la base de la oficina: una tabla con PDF guardados en binario.
- **Parámetros:** `-Carpeta`, opcional: la carpeta con los PDF; por defecto, `RAG-BASICO-datos\documentos`, al lado del repo. `-Base`, opcional: el nombre de la base de prueba; por defecto, `RagBasico_Origen`.
- **Ejemplo:** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-origen-de-prueba.ps1`
- **Requiere:** SQL Server, con el servidor de `Conexiones:SqlTextos` y autenticación de Windows, y Windows PowerShell 5.1. No necesita sqlcmd: se conecta con `System.Data.SqlClient`.
- **Pasos:** controla que la carpeta exista y tenga algún PDF, y que el nombre de la base tenga solo letras, números y guion bajo; crea la base, si no existe, en el mismo servidor de la base del RAG; crea la tabla `dbo.TABLA_DE_PDF` (`Id`, `Nombre`, `Contenido`), la vacía y guarda adentro cada PDF de la carpeta. Los nombres de la tabla y de las columnas son los del ejemplo de `Conexiones:ConsultaLista` y `Conexiones:ConsultaContenido`, así que las dos consultas andan sin tocarlas. Se puede correr de nuevo. Al final muestra el contenido de `RagBasico.Api\conexiones.json` que hay que crear, con `Conexiones:SqlOrigen`, para que la API use esa base.
- **Funciones:** `Correr-Sql`, que corre un comando de SQL sin resultado, con los parámetros en un diccionario.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

## scripts\iniciar-qdrant.ps1

- **Responsabilidad:** arranca Qdrant escuchando solo en esta PC (`127.0.0.1`), con su carpeta de datos fija y sin telemetría. Sin esto, Qdrant escucha en toda la red y sin clave, y guarda los datos en una carpeta relativa a donde se lo arranca. Lo corre `scripts\iniciar.ps1` cuando Qdrant es de esta PC y no está andando; también se puede correr a mano.
- **Parámetros:** `-Carpeta`, opcional: la carpeta de `qdrant.exe`. Por defecto, `RAG-BASICO-datos\qdrant` al lado de la carpeta principal del repo, también si se lo corre desde una carpeta de fase. Los datos quedan en sus subcarpetas `storage` y `snapshots`.
- **Ejemplo:** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\iniciar-qdrant.ps1`
- **Requiere:** `qdrant.exe` 1.20.2 (el archivo `qdrant-x86_64-pc-windows-msvc.zip` de la página de versiones de Qdrant en GitHub), git 2.31 o posterior y Windows PowerShell 5.1. Si falta `qdrant.exe`, dice de dónde bajarlo.
- **Pasos:** busca `qdrant.exe`; define, solo para este proceso, `QDRANT__SERVICE__HOST=127.0.0.1`, los puertos 6333 y 6334, `QDRANT__STORAGE__STORAGE_PATH`, `QDRANT__STORAGE__SNAPSHOTS_PATH` y `QDRANT__TELEMETRY_DISABLED=true`; y arranca Qdrant desde su carpeta. Queda corriendo en esa consola hasta Ctrl+C. No hace falta abrirle el firewall.
- **Desde:** fase-7-qdrant.
- **Cambió en:** nada todavía.

## scripts\iniciar.ps1

- **Responsabilidad:** arranca todo lo que corre en esta PC: Qdrant, si hace falta, y la API. Lo corre `iniciar.cmd`.
- **Parámetros:** ninguno.
- **Ejemplo:** `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\iniciar.ps1`
- **Requiere:** el SDK de .NET y Windows PowerShell 5.1. Si Qdrant es de esta PC, lo que pide `scripts\iniciar-qdrant.ps1`.
- **Pasos:**
  1. Lee la dirección de Qdrant de `Conexiones:Qdrant`, en `RagBasico.Api\appsettings.json`. Si existe `RagBasico.Api\conexiones.json` y trae esa línea, vale la de `conexiones.json`, igual que en la API.
  2. Si Qdrant es de esta PC (`127.0.0.1` o `localhost`) y no responde, lo arranca en otra ventana, minimizada, con `scripts\iniciar-qdrant.ps1`, y lo espera hasta 30 segundos. Si no llega a responder, avisa y sigue: la API arranca igual y la página lo muestra en rojo. Si Qdrant está en otra máquina, no arranca nada.
  3. Corre la API con `dotnet run` desde `RagBasico.Api`, que abre el navegador. Queda en esa ventana hasta Ctrl+C, y sale con el código de `dotnet run`.
- **Qué no arranca:** SQL Server, que es un servicio de Windows, ni Ollama, que se abre solo al iniciar sesión. Si alguno falta, la página lo muestra en rojo con lo que hay que hacer.
- **Funciones:** `Leer-Qdrant`, que lee `Conexiones:Qdrant` de un archivo JSON, y `Probar-Qdrant`, que dice si Qdrant contesta en una dirección.
- **Desde:** fase-9-local.
- **Cambió en:** nada todavía.

## sql\base.sql

- **Responsabilidad:** crea toda la base del RAG en un solo script: los documentos con su estado, los fragmentos, el índice Full-Text en español y el registro de consultas. No guarda vectores, que van solo en Qdrant: por eso usa solo SQL que entiende un SQL Server 2014.
- **Qué crea:** cada paso controla si ya está hecho, así se puede correr de nuevo.
  - La base, si no existe, y el esquema `rag`.
  - La tabla `rag.Documento`: un registro por PDF, con su estado (`pendiente`, `listo`, `sin texto` o `error`). Un PDF arrastrado guarda sus bytes en `Contenido` y no tiene `OrigenId`; uno importado guarda su número de la base de origen en `OrigenId` y no guarda los bytes.
  - Tres índices sobre `rag.Documento`: `UX_rag_Documento_Archivo`, para que entre los arrastrados no haya dos con el mismo nombre; `UX_rag_Documento_OrigenId`, para que entre los importados no haya dos con el mismo número; e `IX_rag_Documento_Estado`, para encontrar rápido el próximo pendiente. Los dos primeros son índices filtrados (con `WHERE`), y por eso el script prende `QUOTED_IDENTIFIER`.
  - La tabla `rag.Fragmento`, con borrado en cascada al borrar su documento, y el índice `IX_rag_Fragmento_Documento`.
  - El catálogo `ftRag`, sin distinguir acentos, y el índice Full-Text sobre `rag.Fragmento.Texto`, en español (3082) y con actualización automática.
  - La tabla `rag.Consulta`, el registro de cada pregunta.
- **Variables sqlcmd:** `Base`, el nombre de la base. Lo pasa `scripts\crear-base.ps1`; en SSMS se define con `:setvar Base RagBasico` en modo SQLCMD.
- **Desde:** fase-9-local. Reemplaza a los cuatro scripts numerados de las fases 2 a 5, sin la columna del vector.
- **Cambió en:** nada todavía.

## sql\consultas-utiles.sql

- **Responsabilidad:** consultas para mirar la base desde SSMS:
  - cuántos documentos hay en cada estado, cuántos fragmentos y tres fragmentos al azar;
  - los 100 documentos más nuevos, con su estado, de dónde vinieron (`OrigenId`), cuántos fragmentos tienen y cuánto ocupa el PDF guardado;
  - los documentos que no se pudieron cargar, con el motivo, y los que quedaron listos con el texto dudoso;
  - los fragmentos de un documento, para revisar un PDF con el texto dudoso;
  - los fragmentos de menos de 30 palabras, que con el corte por párrafos casi no tendría que haber;
  - si el Full-Text terminó de indexar, una búsqueda a mano y qué palabras busca el Full-Text a partir de una;
  - cuánto ocupa la base, y las últimas 20 preguntas con lo que tardó cada parte.
- **Qué crea:** nada. `scripts\crear-base.ps1` no la corre: corre solo `sql\base.sql`.
- **Variables sqlcmd:** ninguna. Antes de usarla se elige la base del RAG en SSMS.
- **Desde:** fase-2-rag-minimo.
- **Cambió en:** fase-4-carga, fase-5-vectores, fase-9-local.
