# Fase 8: validaciones

## 1. Objetivo

Que la API responda con claridad cuando algo está mal, en lugar de un error 500 genérico: una pregunta vacía o demasiado larga da un 400 que dice el motivo, un servicio caído da un 503 que dice cuál, y si falta la base o un modelo, la API ni arranca y dice por qué.

Cuando se hizo, esta fase iba por delante de `main`, que estaba en la fase 7. Desde la fase 9, `main` es la fase 9.

## 2. Qué cambió

| Archivo | Qué cambió |
|---|---|
| `RagBasico.Api\Validador.cs` | Nuevo: controla la pregunta |
| `RagBasico.Api\ManejadorDeErrores.cs` | Nuevo: traduce cada error a un problema con título, detalle y código HTTP |
| `RagBasico.Api\ErrorDeServicio.cs` | Nuevo: el error de Ollama o de Qdrant, con el nombre del servicio |
| `RagBasico.Api\DetectorDeCaidas.cs` | Nuevo: dice qué servicio no responde |
| `RagBasico.Api\Errores.cs` | `EsCaida` reconoce el `ErrorDeServicio` de un servicio que no responde |
| `RagBasico.Api\Salud.cs` | Los chequeos de arranque, y qué revisar si no se puede leer la base |
| `RagBasico.Api\Ollama.cs` | Un error de Ollama es un `ErrorDeServicio` |
| `RagBasico.Api\Qdrant.cs` | Un error de Qdrant es un `ErrorDeServicio` |
| `RagBasico.Api\Sincronizador.cs` | El comentario del aviso dice qué tipo de error trae Qdrant ahora |
| `RagBasico.Api\Program.cs` | Los chequeos al arrancar, el manejador de errores, los errores sin cuerpo con ProblemDetails, el detector delante de cada cliente HTTP y la validación de la pregunta |
| `RagBasico.Api\appsettings.json` | La base `RagBasico_F8`, la colección `fragmentos_f8` y las claves `Rag:LargoMaximoPregunta` y `Rag:ChequearAlArrancar` |
| `RagBasico.Api\Properties\launchSettings.json` | Puerto 5088 |
| `RagBasico.Api\RagBasico.Api.http` | Puerto 5088 y los pedidos que tienen que dar 400 |

Endpoints: ninguno nuevo. `GET /buscar` y `POST /preguntar` validan la pregunta, y los errores salen como ProblemDetails (`application/problem+json`), salvo `GET /salud`, que con estado `error` devuelve su propio informe con código 503. Tablas: ninguna.

## 3. Orden de lectura

1. `RagBasico.Api\Validador.cs`: la pieza.
2. `RagBasico.Api\Program.cs`: el chequeo al arrancar, después `UseExceptionHandler` y `UseStatusCodePages`, y la validación en `GET /buscar` y `POST /preguntar`.
3. `RagBasico.Api\ManejadorDeErrores.cs`, con `ErrorDeServicio.cs` y `DetectorDeCaidas.cs`.
4. `Salud.RevisarArranqueAsync` y `Errores.EsCaida`.

## 4. Cómo se corre y se prueba

1. Crear la base `RagBasico_F8`, arrancar Qdrant y la API, `POST /indexar`, `POST /vectores`.
2. Los pedidos nuevos del `.http`: una pregunta vacía, una demasiado larga, una sin letras y un modo que no existe tienen que dar 400 con el motivo en `detail`.
3. En la página de la demo, una pregunta de más de 500 caracteres: tiene que mostrar el motivo.
4. Frenar Ollama y preguntar: tiene que dar 503 con el título "Ollama no responde" y la dirección. Volver a arrancar Ollama.
5. Frenar la API (Ctrl+C en su consola, o Shift+F5 en Visual Studio) y el servicio de SQL Server (como administrador: `Stop-Service 'MSSQL$SQLEXPRESS'`), y arrancar la API: no tiene que arrancar, y el log dice por qué. En esa misma consola, con `$env:Rag__ChequearAlArrancar = 'false'`, arranca igual. Al terminar: frenar la API con Ctrl+C, `Start-Service 'MSSQL$SQLEXPRESS'` (como administrador) y `Remove-Item Env:\Rag__ChequearAlArrancar`, o cerrar esa consola.
6. Con SQL Server y Ollama andando y la API frenada, frenar Qdrant y arrancar la API: tiene que arrancar, porque la demo sigue sin Qdrant. Volver a arrancar Qdrant.

## 5. Resultados

| Medición | Valor | Cómo se midió | Fecha |
|---|---|---|---|
| Compilación | 0 advertencias, 0 errores | `dotnet build` | 19/11/2026 |
| Validación de la pregunta | 9 casos: normal, vacía, solo espacios, sin la pregunta en el JSON, más larga que el máximo, justo el máximo, sin letras, caracteres de control, saltos de línea | Un proyecto de prueba aparte | 19/11/2026 |
| Errores claros | 10 casos: servicio caído 503, servicio con error 502, tiempo vencido 504, cancelado sin tiempo vencido 500, un `ArgumentException` 500, un JSON roto o un parámetro que falta conserva su 400, cualquier otro 500, una conexión rechazada que el detector convierte en "Ollama no responde en ...", y cuándo `Errores.EsCaida` toma un `ErrorDeServicio` como caída | Un proyecto de prueba aparte | 19/11/2026 |
| La API corriendo, con el chequeo al arrancar y SQL Server detenido | No arranca: sale con código 1 a los 17 segundos y el log dice por qué | La API sin SQL Server | 19/11/2026 |
| La API corriendo, con una configuración inválida (`Qdrant:SegundosDeEspera` en 0) | No arranca: el log dice "Configuración" y el motivo | La API con un Ollama y un Qdrant falsos | 19/11/2026 |
| La API corriendo, con el chequeo apagado | 400 con el motivo para una pregunta vacía, una que falta en el JSON, una de 501 caracteres, una sin letras y un modo que no existe; 400 para un parámetro que falta y un JSON roto; 404, 405 y 415 con ProblemDetails; 502 con Qdrant sin la colección; 503 "Ollama no responde" con Ollama apagado | La API con un Ollama y un Qdrant falsos | 19/11/2026 |
| Verificación | Cinco revisores y cada hallazgo contrastado por dos o tres más. Lo de esta fase, arreglado: los errores sin cuerpo (404, 405, 415) y los 400 fuera de Development salían vacíos, un `ArgumentException` de la configuración se le echaba al cliente con un 400, y los pasos 4 a 6 de esta sección no volvían a arrancar lo que frenaban | Revisiones aparte del código y la documentación | 19/11/2026 |
| Revisión final de los arreglos | Tres revisores más, y cada hallazgo contrastado por dos. Lo de esta fase, arreglado: los pasos 5 y 6 arrancaban una segunda API sin frenar la primera, que tiene el mismo puerto | Revisiones aparte de la documentación | 19/11/2026 |

Versiones: las mismas de la fase 7. Commit medido: el último de `fase-8-validaciones`.

## 6. Decisiones

| Decisión | Alternativas descartadas | Por qué |
|---|---|---|
| Validar en el endpoint, antes de buscar | Dentro del `Redactor` o del `Buscador` | Una pregunta que no sirve no tiene que llegar a SQL ni a Ollama; y el endpoint es el que sabe responder un 400 |
| Un máximo de 500 caracteres, configurable | Sin máximo, o el de la columna (2.000) | Una pregunta de verdad entra en 500; más largo suele ser un texto pegado, que el modelo chico atiende peor |
| Rechazar una pregunta sin letras ni números | Aceptarla | "???" no tiene nada que buscar |
| ProblemDetails para los errores | Un JSON propio, como `{ "error": ... }` de la fase 5 | Es el formato estándar (RFC 9457) y ASP.NET Core ya lo usa para sus propios 400 |
| `UseStatusCodePages` | Dejar los 404, 405 y 415 sin cuerpo | Un pedido con el tipo de contenido equivocado, como el que manda por defecto `Invoke-RestMethod`, daba un 415 vacío sin explicar nada |
| `ThrowOnBadRequest` en todos los entornos | Solo en Development, como viene | Fuera de Development, un JSON roto daba un 400 vacío; así sale con el motivo |
| Un tipo de error propio con el nombre del servicio | Adivinar el servicio por el mensaje | Ollama y Qdrant lanzan el mismo `HttpRequestException`; el detector delante de cada cliente sabe cuál es |
| 503 si no responde, 502 si respondió con un error, 504 si tardó | 500 para todo | El código ya dice de quién es el problema: de la API (500), de otro servicio (502, 503) o del tiempo (504) |
| Un `ArgumentException` es 500, no 400 | Tomarlo como pedido mal armado | Lo que manda el cliente ya está controlado; un `ArgumentException` que llega al manejador es de la API, por ejemplo de la configuración |
| Frenar el arranque si falta la base o un modelo, o si la configuración es inválida | Arrancar y fallar en la primera pregunta | Se ve antes de la demo, no en medio |
| No frenar el arranque por Qdrant | Frenarlo también | La demo tiene que andar sin Qdrant: la híbrida sigue con SQL |
| `Rag:ChequearAlArrancar` para apagar el chequeo | Siempre activo | Para mirar la página sin tener todo andando |

## 7. Limitaciones conocidas

- El chequeo al arrancar tarda unos 15 segundos en avisar si el servicio de SQL Server está detenido: es lo que tarda el driver en rendirse.
- Los mensajes de error muestran el mensaje original del error. Para una API que solo se usa en la PC está bien; expuesta en una red, habría que esconder ese detalle.
- Un tiempo de espera vencido no dice si fue Ollama o Qdrant: `HttpClient` lo informa igual para los dos.
- Un error de SQL Server da siempre 503, aunque sea una tabla que falta y no el servicio caído: el detalle dice cuál es.
- Quedan para elegir: un proyecto de tests automáticos (Chunker, RRF, lectura del examen, validación) y un build automático en GitHub que marque cada commit con un tilde verde.
- Esta versión no se probó contra SQL Server, Ollama ni Qdrant reales.
