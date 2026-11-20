# Fase 9: la versión final, en una PC

## 1. Objetivo

Dejar el RAG lo más simple de instalar, de usar y de explicar, sin perder lo que hacía la fase 8:

- **Cada dato en un solo lugar.** Los PDF, los párrafos y la búsqueda por palabras en SQL Server; los vectores solo en Qdrant; los modelos en Ollama.
- **Toda la conexión en un bloque.** `Conexiones`, en `appsettings.json`. Para mover una pieza a otro servidor se cambia una línea.
- **Los PDF entran por la página**, arrastrándolos, o desde otra base donde ya están guardados, y se procesan solos en segundo plano.
- **Se arranca con doble clic** y la página dice qué anda, qué no y qué hacer.
- **Menos código:** de 19 archivos y unas 2.700 líneas de C# a 17 archivos y unas 2.250, haciendo más cosas (la carga por la página, la importación y el segundo plano son nuevas).

Esta fase corre toda en una PC. La fase 10 es el mismo código, conectado a servidores de otras máquinas.

## 2. Qué cambió

| Archivo | Qué cambió |
|---|---|
| `RagBasico.Api\Documentos.cs` | Nuevo: el registro de los documentos. Anota los que llegan, dice cuál sigue, guarda su estado, los lista y los borra |
| `RagBasico.Api\Cargador.cs` | Nuevo: procesa un documento de punta a punta, del PDF a los vectores en Qdrant. Trae del `Indexador` cómo sacar el texto y cómo reconocer un escaneo |
| `RagBasico.Api\Procesador.cs` | Nuevo: el trabajo en segundo plano, que procesa los pendientes de a uno |
| `RagBasico.Api\Indexador.cs` | Se borró: lo reemplazan `Documentos` y `Cargador` |
| `RagBasico.Api\Vectorizador.cs` | Se borró: los vectores se calculan al procesar cada documento |
| `RagBasico.Api\Sincronizador.cs` | Se borró: los vectores ya no están en dos lugares, no hay nada que igualar |
| `RagBasico.Api\Planificador.cs` | Se borró: ya no hay una carpeta que comparar con la base |
| `RagBasico.Api\Evaluador.cs` | Se borró: el examen queda en la fase 8 |
| `RagBasico.Api\Buscador.cs` | Tres modos: `palabras`, `vectores` (en Qdrant) e `hibrida`. Se fueron los vectores de SQL y el respaldo. Solo busca en documentos listos |
| `RagBasico.Api\Qdrant.cs` | La clave opcional, crear la colección si falta y borrar los puntos de un documento. Se fue lo que usaba el sincronizador |
| `RagBasico.Api\Salud.cs` | Cada chequeo dice la dirección que probó y qué hacer. Qdrant caído es un error. Chequeo de la base de origen. Se fue el chequeo de arranque |
| `RagBasico.Api\Modelos.cs` | Se fueron los registros de lo que se borró; entraron los de documentos |
| `RagBasico.Api\Ollama.cs` | Lee los modelos de `Conexiones`. Un modelo que no está bajado, o que da vectores de otro tamaño, es un error de configuración |
| `RagBasico.Api\Redactor.cs` | Lee la conexión de `Conexiones`; la respuesta ya no lleva aviso |
| `RagBasico.Api\ManejadorDeErrores.cs` | Una subida demasiado grande es un 413 con su motivo, y un servicio que no respondió a tiempo dice cuál |
| `RagBasico.Api\ErrorDeServicio.cs` | La propiedad `DeConfiguracion`: el error es de cómo está configurado el servicio, no de lo que se le mandó |
| `RagBasico.Api\DetectorDeCaidas.cs` | Lleva el reloj de cada pedido: un servicio que no responde a tiempo es un error 504 con su nombre y su dirección |
| `RagBasico.Api\Errores.cs` | `EsCaida` cuenta también al servicio que no respondió a tiempo |
| `RagBasico.Api\Program.cs` | Carga `conexiones.json` si existe, arranca siempre, registra el `Procesador`, el tope de subida y los pedidos de documentos. Los pedidos que cambian algo solo se aceptan de las páginas de la propia API |
| `RagBasico.Api\appsettings.json` | El bloque `Conexiones`, la base `RagBasico_F9`, la colección `fragmentos_f9` y `Rag:MegasPorSubida`. Se fueron `ConnectionStrings`, `Rag:CarpetaDocumentos`, `Rag:ArchivoExamen` y `Rag:ChequearAlArrancar`. `AllowedHosts` acepta solo `127.0.0.1` y `localhost` |
| `RagBasico.Api\Properties\launchSettings.json` | Puerto 5089 |
| `RagBasico.Api\RagBasico.Api.http` | Los pedidos de esta fase |
| `RagBasico.Api\wwwroot\index.html` | Tres modos, la franja de estado y los enlaces a las dos páginas |
| `RagBasico.Api\wwwroot\documentos.html` | Nueva: cargar, importar, ver el avance y la lista |
| `RagBasico.Api\wwwroot\estado.js` | Nuevo: la franja de estado de las dos páginas |
| `RagBasico.Api\wwwroot\estilos.css` | Nuevo: lo común a las dos páginas |
| `sql\base.sql` | Nuevo: toda la base en un script, sin vectores, con el estado de cada documento |
| `sql\01_base.sql`, `sql\02_consultas.sql`, `sql\03_huella.sql`, `sql\04_vectores.sql` | Se borraron: los reemplaza `sql\base.sql` |
| `sql\consultas-utiles.sql` | Las consultas para mirar la base, al día con las tablas nuevas: por estado, los que fallaron, cuánto ocupa la base |
| `scripts\crear-base.ps1` | Lee `Conexiones:SqlTextos` y corre `sql\base.sql` |
| `scripts\iniciar.ps1` | Nuevo: arranca Qdrant, si hace falta, y la API |
| `iniciar.cmd` | Nuevo: el doble clic |
| `scripts\crear-origen-de-prueba.ps1` | Nuevo: una base de origen de prueba, con los PDF de una carpeta |
| `scripts\crear-worktrees.ps1`, `scripts\crear-manifiesto.ps1` | Se borraron: eran para recorrer las fases en carpetas y para congelar los PDF del examen |
| `scripts\controlar-docs.ps1` | Lo que controla de esta versión |
| `documentos.csv`, `examen.csv`, `examen.ejemplo.csv` | Se borraron, con el examen |
| `.gitignore` | Ignora `conexiones.json` |
| `README.md`, `docs\arquitectura.md`, `docs\responsabilidades.md`, `docs\instalacion.md` | Al día con esta fase |
| `docs\manual.md` | Nuevo: el manual de lectura |
| `docs\worktrees.md` | Se borró |

Endpoints nuevos: `POST /documentos`, `POST /documentos/importar`, `GET /documentos`, `DELETE /documentos/{id}`, `POST /documentos/reintentar` y `POST /documentos/reprocesar`. Se fueron `POST /indexar`, `POST /vectores`, `POST /evaluar`, `GET /qdrant/estado` y `POST /qdrant/reconstruir`.

Tablas: `rag.Documento` gana `OrigenId`, `Contenido`, `Estado`, `Detalle` y `FechaProceso`; `rag.Fragmento` pierde `Embedding`.

## 3. Orden de lectura

El recorrido completo, archivo por archivo, está en [`docs\manual.md`](../manual.md). En corto:

1. `RagBasico.Api\appsettings.json`: el bloque `Conexiones`.
2. `sql\base.sql`: las tres tablas.
3. `RagBasico.Api\Program.cs`: qué servicios hay y qué pedidos atiende.
4. El camino de un PDF: `Documentos.cs`, `Procesador.cs`, `Cargador.cs`.
5. El camino de una pregunta: `Buscador.cs` y `Redactor.cs`, que cambiaron poco.
6. `Salud.cs` y las dos páginas.

## 4. Cómo se corre y se prueba

1. Con SQL Server andando, crear la base: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1`.
2. Doble clic en `iniciar.cmd`. Tiene que arrancar Qdrant, arrancar la API y abrir el navegador. La franja de arriba queda sin rojos: SQL Server y Qdrant en amarillo, porque todavía no hay documentos.
3. En "Documentos", soltar dos o tres PDF. Quedan `pendiente` y a los pocos segundos pasan a `listo`, con su cantidad de fragmentos. La franja queda toda en verde.
4. Soltar un archivo que no es un PDF: lo rechaza y dice cuál.
5. En "Preguntar", hacer una pregunta en cada modo: responde con citas y muestra las fuentes.
6. Para probar la importación: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-origen-de-prueba.ps1`, crear `RagBasico.Api\conexiones.json` con lo que muestra, frenar la API y volver a arrancar con `iniciar.cmd`. El botón "Importar de la base de origen" queda prendido: anota los documentos y se procesan solos.
7. Con documentos pendientes, frenar la API (Ctrl+C) y volver a arrancar: sigue donde quedó.
8. Cerrar la ventana de Qdrant: la franja muestra Qdrant en rojo con lo que hay que hacer, la búsqueda por palabras sigue respondiendo, y la híbrida da un error que dice "Qdrant no responde". Volver a arrancar con `iniciar.cmd`.
9. "Reprocesar todo": deja todo pendiente y lo rehace.

## 5. Resultados

| Medición | Valor | Cómo se midió | Fecha |
|---|---|---|---|
| Compilación | 0 advertencias, 0 errores | `dotnet build` | 20/11/2026 |
| Pruebas | 167 casos, 0 fallas | Un proyecto de prueba aparte, contra SQL Server, Qdrant 1.20.2 y Ollama reales | 21/11/2026 |
| Lo que cubren las pruebas nuevas | El registro: nuevos, repetidos, reemplazados, rechazados, dos con el mismo nombre en una tanda, importar sin duplicar. El cargador: un PDF con texto, uno que no es PDF, uno de 0 bytes, uno cortado, un escaneo, un importado que ya no está en el origen, Qdrant caído, Ollama caído, procesar dos veces sin duplicar, y el turno. Los arreglos de la revisión: un PDF reemplazado mientras se procesa, un documento que no termina en tres intentos, un servidor caído que no gasta intentos, un servicio que no contesta a tiempo, y reprocesar todo | El mismo proyecto | 20/11/2026 |
| La API corriendo, de punta a punta | Subir un PDF y un `.txt`: 1 nuevo y 1 rechazado. El PDF se procesó solo: 397 fragmentos en SQL y 397 puntos en Qdrant. Preguntas en los tres modos, con 5 fuentes cada una. Un estado mal escrito, 400; borrar uno que no existe, 404; importar sin base de origen, 400; una subida que pasa el tope, 413 | Pedidos con `curl` contra la API | 20/11/2026 |
| El aviso de Qdrant desparejo | Con 10 fragmentos borrados a mano en SQL, `GET /salud` avisó "397 puntos y la base 387 fragmentos" y "Reprocesar todo" lo dejó en verde | Pedidos con `curl` | 20/11/2026 |
| Velocidad de carga | Entre 6 y 13 fragmentos por segundo; un PDF de 30 fragmentos, unos 5 segundos | 397 fragmentos en 64 segundos, y la importación de los 13 PDF de prueba, en una GTX 1070 | 20/11/2026 |
| El doble clic | Con Qdrant apagado, `iniciar.cmd` lo arrancó en otra ventana y después arrancó la API | A mano | 20/11/2026 |
| Revisión | Un revisor aparte leyó toda la fase. Nada crítico. Arreglado: un documento problemático podía frenar toda la cola (ahora hay un tope de tres intentos y de 5 minutos para leer un PDF); una clave de Qdrant mal puesta dejaba todos los documentos en error (ahora quedan pendientes); un PDF reemplazado mientras se procesaba quedaba listo con el contenido viejo; cuando la carga se frenaba, la página no lo mostraba; los errores de la base de origen y de un servicio que no contesta a tiempo no decían qué servidor era; "Reprocesar todo" podía quedar a medias con muchos fragmentos | Una revisión aparte del código | 20/11/2026 |
| Segunda revisión | Una auditoría de fallas, seguridad y legibilidad. Nada crítico ni alto. Arreglado: si `iniciar.ps1` fallaba, la ventana se cerraba sin mostrar el error; un modelo de Ollama sin bajar, o de otra dimensión, dejaba toda la cola en error (ahora la carga se frena con el aviso); un documento que terminaba en error dejaba sus vectores en Qdrant; cualquier página abierta en la PC podía pedir un borrado o un reproceso (ahora se controla el origen y el nombre del servidor); en la página, el error de una acción se borraba solo a los pocos segundos; los pedidos de `/salud` se apilaban si tardaba; y el log guardaba solo el mensaje del error, sin el detalle | 8 casos nuevos en el proyecto de prueba; pedidos con `curl` con y sin `Origin` y con otro `Host`; `iniciar.ps1` con un JSON roto y sin `dotnet`; la página en el navegador | 21/11/2026 |
| Las páginas | La franja, la lista, el avance y una pregunta con fuentes, en el navegador; a 375 px de ancho no hay barra horizontal | A mano | 20/11/2026 |

Versiones: .NET 10.0.15, Qdrant 1.20.2, `qwen3.5:4b` y `qwen3-embedding:0.6b`. Commit medido: el último de `fase-9-local`.

## 6. Decisiones

| Decisión | Por qué |
|---|---|
| Los vectores, solo en Qdrant | Guardarlos también en SQL pedía SQL Server 2025, un sincronizador entre los dos lados y un respaldo. Con un solo lugar se va todo eso, y la base del RAG anda en cualquier SQL Server con Full-Text |
| El PDF queda guardado en SQL | Así Qdrant se puede rehacer entero, y la API no depende de una carpeta de la PC donde corre |
| Los importados no se copian | Ya están en la otra base. Copiar decenas de miles de PDF puede pasar el tope de 50 GB de SQL Server Express |
| Un importado se reconoce por su número, no por su nombre | En la otra base puede haber dos con el mismo nombre |
| El procesamiento va en segundo plano | Una carga grande tarda horas: no puede depender de que una página quede abierta. Con el estado en la tabla, se puede apagar y sigue después |
| Un documento por vez | Ollama calcula los vectores de a uno igual. Y así un documento nunca queda a medias con otro |
| La API arranca siempre | Con los servidores en otras máquinas, sirve más que arranque y diga qué falta |
| Si Qdrant no responde, la híbrida da error | Seguir solo con palabras, sin avisar, daría respuestas peores sin que nadie lo note |
| Sin interfaces para cambiar de motor | Serían más archivos y un concepto más. Cambiar de servidor es cambiar una línea; cambiar de motor es otro proyecto |
| El arranque, en un script y no en C# | El código queda igual en una PC y en varios servidores |
| Se sacó el examen | No hay preguntas escritas para los documentos reales. Sigue en la fase 8 para cuando haga falta medir |
| Las conexiones de otros servidores, en un archivo que git ignora | Los nombres de los servidores y las claves no se suben |

## 7. Limitaciones conocidas

- No lee PDF escaneados: quedan como `sin texto`.
- No tiene usuarios ni claves: la API escucha solo en la PC donde corre.
- Procesa un documento por vez. La velocidad la pone el modelo de embeddings.
- Si dos personas aprietan "Importar" al mismo tiempo, una recibe un error; alcanza con apretar de nuevo.
- Si la base de origen guarda los PDF comprimidos o como texto, no los lee: tienen que estar en binario (`varbinary` o `image`).
- No se probó con decenas de miles de documentos: en casa se probó con 13 PDF.
- "Reprocesar todo" con muchos documentos tarda lo mismo que la primera carga.
- Si se cambia el modelo de embeddings por uno que no da 1024 números, hay que cambiar `Ollama.Dimension` y reprocesar todo.
- La página muestra la velocidad en documentos por minuto: con documentos muy grandes, el número tarda en aparecer.
- El tamaño de la base: los PDF arrastrados se guardan adentro, y SQL Server Express admite hasta 50 GB por base (10 GB antes de la versión 2025).
