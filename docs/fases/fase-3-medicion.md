# Fase 3: medir antes de mejorar

## 1. Objetivo

Poder decir con números qué tan bien busca el sistema: un examen escrito antes de construir, un evaluador que lo corre, y el registro de cada pregunta con lo que tardó cada parte.

Viene antes de cualquier mejora porque sin una línea base no se sabe si una mejora mejora. Las fases 4, 5 y 6 se justifican con lo que este examen muestre.

## 2. Qué cambió

| Archivo | Qué cambió |
|---|---|
| `RagBasico.Api\Evaluador.cs` | Nuevo: lee el examen, lo corre y resume |
| `RagBasico.Api\Redactor.cs` | Registra cada consulta en `rag.Consulta`; recibe el log |
| `RagBasico.Api\Modelos.cs` | Los records `PreguntaExamen`, `DetallePregunta`, `ResultadoModo` e `InformeEvaluacion` |
| `RagBasico.Api\Program.cs` | Registra el `Evaluador` y agrega `POST /evaluar` |
| `sql\02_consultas.sql` | Nuevo: la tabla `rag.Consulta` |
| `scripts\crear-manifiesto.ps1` | Nuevo: arma o verifica el manifiesto de los PDF |
| `examen.csv` | Nuevo: el examen, con el encabezado |
| `examen.ejemplo.csv` | Nuevo: el formato del examen con cinco casos |
| `documentos.csv` | Nuevo: el manifiesto, con el encabezado |
| `RagBasico.Api\appsettings.json` | La base `RagBasico_F3` y la clave `Rag:ArchivoExamen` |
| `RagBasico.Api\Properties\launchSettings.json` | Puerto 5083 |
| `RagBasico.Api\RagBasico.Api.http` | Puerto 5083 y el pedido de `POST /evaluar` |

Endpoints: `POST /evaluar`. Tablas: `rag.Consulta`.

## 3. Orden de lectura

1. `examen.ejemplo.csv`: el formato del examen.
2. `RagBasico.Api\Evaluador.cs`: `CorrerAsync`, después `Puntuar` y `LeerExamen` con `PartirLinea`, y al final las dos consultas a la base.
3. `sql\02_consultas.sql` y `Redactor.RegistrarAsync`.
4. `scripts\crear-manifiesto.ps1`.

## 4. Cómo se corre y se prueba

1. Escribir `examen.csv` siguiendo `examen.ejemplo.csv`: 15 preguntas con las mismas palabras del documento, 15 dichas con otras palabras y 10 sin respuesta. Si podés, que otra persona escriba un tercio sin mirar los PDF.
2. Congelar el corpus: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-manifiesto.ps1` y completar las URL de `documentos.csv`.
3. Crear la base `RagBasico_F3` con `scripts\crear-base.ps1`, arrancar la API y `POST /indexar`.
4. `POST /evaluar`: la tabla, el detalle por pregunta y las fallas.
5. Para leer los números: "entre los 5" es lo que más importa, porque si el documento está entre los cinco, Qwen tiene con qué responder. Una diferencia de menos de 3 preguntas no prueba nada.
6. En SSMS, las últimas filas de `rag.Consulta` muestran lo que tardó cada parte de cada respuesta.

## 5. Resultados

| Medición | Valor | Cómo se midió | Fecha |
|---|---|---|---|
| Compilación | 0 advertencias, 0 errores | `dotnet build` | 18/11/2026 |
| Lectura del examen y puntaje | 10 de 10 casos: BOM, Excel en ANSI, `;` entre comillas, comillas dobladas, varios documentos, `ninguno`, columnas vacías al final | Un proyecto de prueba aparte | 18/11/2026 |
| Manifiesto | Arma, verifica, detecta un PDF cambiado y uno de más, y conserva las URL | Un repo temporal | 18/11/2026 |
| Línea base: "1er lugar" y "entre los 5" por palabras, preguntas sin respuesta bien contestadas | pendiente: se mide con el examen escrito y todo instalado | `POST /evaluar` | pendiente |

Versiones: las mismas de la fase 2. Commit medido: el último de `fase-3-medicion`.

## 6. Decisiones

| Decisión | Alternativas descartadas | Por qué |
|---|---|---|
| El examen se escribe antes de construir | Escribirlo después | Escrito después, sin querer se hacen preguntas que el sistema ya sabe contestar |
| "1er lugar" y "entre los 5" | Un solo porcentaje | "Entre los 5" dice si Qwen tiene con qué responder; "1er lugar" dice qué tan fino ordena |
| Las preguntas sin respuesta pasan por Qwen | Medir solo la búsqueda | La búsqueda siempre trae algo: el "no sé" lo decide el modelo, y hay que medirlo |
| Varios documentos válidos con `|` | Un solo documento por pregunta | Con normativa repetida o modificada, más de un documento responde bien |
| Manifiesto con sha256 | Confiar en la carpeta | Si la carpeta cambia, los números de dos fases no se pueden comparar |
| Esperar al Full-Text antes de medir | Medir enseguida | El índice se actualiza en segundo plano: justo después de cargar, la búsqueda por palabras sale baja |
| Registrar cada consulta en SQL | Solo el log | Los tiempos quedan para analizarlos después con una consulta |

## 7. Limitaciones conocidas

- El examen mide la búsqueda y el "no sé", no la calidad de la redacción: eso se revisa a mano en la fase 6.
- Con un examen chico, las diferencias entre modos son ruido. Con 40 preguntas, menos de 3 de diferencia no prueba nada.
- Esta versión no se probó contra SQL Server ni Ollama reales, porque no estaban instalados.
