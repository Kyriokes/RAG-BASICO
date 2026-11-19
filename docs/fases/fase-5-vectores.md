# Fase 5: buscar por significado

## 1. Objetivo

Encontrar los fragmentos que responden una pregunta aunque no compartan las palabras: "¿qué tengo que presentar para que me devuelvan la plata?" tiene que llegar al fragmento que habla de los requisitos del reintegro.

La búsqueda por palabras de las fases 2 a 4 falla justamente ahí. Cada fragmento pasa a tener un vector que representa su significado, y la búsqueda compara el vector de la pregunta con todos. El examen dice si vale la pena.

## 2. Qué cambió

| Archivo | Qué cambió |
|---|---|
| `RagBasico.Api\Vectorizador.cs` | Nuevo: calcula el vector de cada fragmento que no lo tiene, de a lotes |
| `RagBasico.Api\Ollama.cs` | `EmbeddingsAsync` y `EmbeddingPreguntaAsync`, con la instrucción en inglés para la pregunta |
| `RagBasico.Api\Buscador.cs` | `PorVectoresAsync` con `VECTOR_DISTANCE`, los modos y `ElegirModo` |
| `RagBasico.Api\Redactor.cs` | `ResponderAsync` recibe el modo |
| `RagBasico.Api\Evaluador.cs` | Corre los dos modos, cuenta las preguntas sin respuesta en cada uno y avisa si faltan vectores |
| `RagBasico.Api\Indexador.cs` | `FormatearTiempo` pasa a ser pública, para el `Vectorizador` |
| `RagBasico.Api\Modelos.cs` | `PedidoPregunta` con el modo, `InformeVectores`, y las preguntas sin respuesta pasan a `ResultadoModo` |
| `RagBasico.Api\Program.cs` | Registra el `Vectorizador`, agrega `POST /vectores` y el modo en `GET /buscar` y `POST /preguntar` |
| `sql\04_vectores.sql` | Nuevo: la columna `Embedding VECTOR(1024)` |
| `sql\consultas-utiles.sql` | Cuántos fragmentos tienen vector, los vecinos de un fragmento y el tiempo de recorrer todos los vectores |
| `RagBasico.Api\appsettings.json` | La base `RagBasico_F5` y las claves `Rag:LoteVectores`, `Ollama:ModeloEmbeddings` y `Ollama:InstruccionPregunta` |
| `RagBasico.Api\Properties\launchSettings.json` | Puerto 5085 |
| `RagBasico.Api\RagBasico.Api.http` | Puerto 5085 y los pedidos de vectores, de búsqueda por vectores y de un modo mal escrito |

Endpoints: `POST /vectores`; `GET /buscar` y `POST /preguntar` reciben el modo. Tablas: la columna `rag.Fragmento.Embedding`.

## 3. Orden de lectura

1. `sql\04_vectores.sql`: la columna nueva.
2. `RagBasico.Api\Ollama.cs`: `EmbeddingsAsync` y `EmbeddingPreguntaAsync`.
3. `RagBasico.Api\Vectorizador.cs`: `CalcularPendientesAsync`, después `GuardarLoteAsync`, `CalcularYGuardarAsync` y `ComoJson`.
4. `RagBasico.Api\Buscador.cs`: `PorVectoresAsync`, la consulta con `VECTOR_DISTANCE`, y después `ElegirModo` y `BuscarAsync`.
5. `RagBasico.Api\Program.cs`: el modo en `GET /buscar` y `POST /preguntar`.
6. `RagBasico.Api\Evaluador.cs`: el bucle de modos en `CorrerAsync`.
7. Las consultas nuevas de `sql\consultas-utiles.sql`.

## 4. Cómo se corre y se prueba

1. Bajar el modelo: `ollama pull qwen3-embedding:0.6b`.
2. Crear la base `RagBasico_F5` con `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1 -Servidor 'lpc:localhost\SQLEXPRESS'` (sin `-Servidor`, el sqlcmd que instala winget no se conecta: está en `docs\erratas.md`), arrancar la API, `POST /indexar` y `POST /vectores`. Anotar los fragmentos por segundo.
3. `POST /vectores` otra vez: tiene que decir `SIN CAMBIOS`, con `Pendientes` en 0.
4. En SSMS, la consulta de los vecinos de `sql\consultas-utiles.sql`: los cinco más cercanos tienen que ser del mismo tema.
5. `GET /buscar` con la misma pregunta en los dos modos, y una pregunta dicha con otras palabras.
6. `GET /buscar?q=reintegro&modo=vectorial` tiene que dar un error 400 que nombre los modos.
7. `POST /evaluar`: la fila de `vectores` contra la de `palabras`, sobre todo en las preguntas dichas con otras palabras.
8. Una vez, con Ollama sin GPU, `POST /vectores` sobre una base vacía de vectores, para tener los fragmentos por segundo con CPU. Para vaciar los vectores: `UPDATE rag.Fragmento SET Embedding = NULL;`.
9. La instrucción en castellano: cambiar `Ollama:InstruccionPregunta`, reiniciar la API y correr `POST /evaluar`. No hace falta recalcular los vectores de los fragmentos.

## 5. Resultados

| Medición | Valor | Cómo se midió | Fecha |
|---|---|---|---|
| Compilación | 0 advertencias, 0 errores | `dotnet build` | 19/11/2026 |
| Vectores y modos sin servicios | 12 casos: el vector como JSON con Windows en castellano, 1024 números leídos de vuelta iguales, los modos, un vector por texto en un solo pedido, la pregunta con la instrucción y `Query:` pegado, el error de Ollama y el de otra dimensión | Un proyecto de prueba aparte, con un Ollama falso | 19/11/2026 |
| Modo mal escrito | `GET /buscar` y `POST /preguntar` devuelven 400 con el motivo | La API corriendo, sin SQL Server | 19/11/2026 |
| Fragmentos por segundo, con GPU y con CPU | pendiente | `POST /vectores` | pendiente |
| "Entre los 5" y ms por modo; tiempo de recorrer todos los vectores en SQL | pendiente | `POST /evaluar` y `sql\consultas-utiles.sql` | pendiente |
| Instrucción en inglés contra castellano | pendiente | `POST /evaluar` con cada una | pendiente |

Versiones: `qwen3-embedding:0.6b`, más las de la fase 2. Commit medido: el último de `fase-5-vectores`.

## 6. Decisiones

| Decisión | Alternativas descartadas | Por qué |
|---|---|---|
| Los vectores en una columna `VECTOR(1024)` de SQL Server | Una tabla aparte; Qdrant desde ya | En la misma fila del fragmento se borran en cascada con él. Qdrant llega en la fase 7, cuando ya se sabe qué da SQL solo |
| La pregunta lleva instrucción y el fragmento no | Instrucción en los dos, o en ninguno | Así lo indica la ficha del modelo: la instrucción le dice qué tarea hacer con la pregunta |
| La instrucción en inglés, en la configuración | En castellano, en el código | La ficha recomienda inglés; en la configuración se puede probar la otra sin recompilar, y solo cambia el vector de la pregunta |
| `/vectores` avanza por número de fragmento | Volver a pedir "los primeros sin vector" en cada lote | Si un fragmento falla siempre, el otro camino lo vuelve a pedir en cada lote y la corrida no termina |
| Si falla un lote, se prueba de a uno | Saltear el lote entero | Un fragmento con problemas no deja sin vector a los otros quince |
| El vector viaja como texto JSON y SQL lo convierte con `CAST` | El tipo binario `SqlVector` | Dapper no conoce ese tipo, y el texto se puede mirar y copiar a SSMS |
| El JSON con `InvariantCulture` | `ToString()` | Con Windows en castellano, `ToString()` escribe `0,5` |
| Un modo mal escrito da un error 400 | Caer en el modo por defecto | Caer en otro modo sin avisar hace creer que se probó algo que no se probó |
| Las preguntas sin respuesta se miden en cada modo | Una sola vez | Por vectores la búsqueda casi nunca vuelve vacía, así que el "no sé" depende de Qwen y puede salir distinto |

## 7. Limitaciones conocidas

- La búsqueda por vectores en SQL es exacta: recorre todos los fragmentos. Con pocos miles es rápida; con cientos de miles se nota. Para eso está la fase 7.
- Si se cambia el modelo de embeddings, hay que vaciar los vectores (`UPDATE rag.Fragmento SET Embedding = NULL;`) y volver a correr `/vectores`. Mezclar vectores de dos modelos da resultados sin sentido y ningún error.
- Ollama corta en silencio los textos más largos que el contexto del modelo. Con fragmentos de unas 300 palabras no pasa.
- La búsqueda por vectores no encuentra bien números exactos, códigos ni nombres propios raros: para eso sigue estando la búsqueda por palabras, y en la fase 6 se combinan.
- Esta versión no se probó contra SQL Server ni Ollama reales: el servicio de SQL Server estaba detenido. `VECTOR_DISTANCE` y el `CAST` del JSON se escribieron según la documentación de SQL Server 2025, sin correrlos.
