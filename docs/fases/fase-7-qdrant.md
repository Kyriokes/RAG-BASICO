# Fase 7: el índice de vectores

## 1. Objetivo

Sumar Qdrant como índice de vectores, al lado de SQL Server: la búsqueda por significado deja de recorrer todos los vectores y usa un índice HNSW. SQL + Qdrant ya está decidido, así que esta fase no tiene que demostrar que Qdrant es más rápido: tiene que demostrar que da los mismos resultados, que las dos bases quedan iguales y que la demo sigue andando si Qdrant se apaga.

Además, la fase blinda la demo: `/salud` dice antes de presentar si falta algo, y los modelos se cargan al arrancar, así la primera pregunta no tarda 30 segundos. Acá queda `main`.

## 2. Qué cambió

| Archivo | Qué cambió |
|---|---|
| `RagBasico.Api\Qdrant.cs` | Nuevo: el cliente HTTP de Qdrant |
| `RagBasico.Api\Sincronizador.cs` | Nuevo: compara SQL con Qdrant por número de fragmento, los iguala y reconstruye la colección |
| `RagBasico.Api\Salud.cs` | Nuevo: los cinco chequeos de `/salud` |
| `RagBasico.Api\Errores.cs` | Nuevo: cuándo un error es un servicio caído, una sola regla para `/vectores` y la híbrida |
| `scripts\crear-base.ps1` | Se conecta por memoria compartida (`lpc:`), sin el SQL Server Browser (una errata de la fase 2), y si un script falla, sugiere revisar el servicio de SQL Server |
| `docs\erratas.md` | Las primeras erratas, todas arregladas en esta fase: `crear-base.ps1` de la fase 2, el corte de `/vectores` de la fase 5 y textos de las fases 0 a 6 |
| `docs\instalacion.md` | Nuevo: qué instalar para correr todas las fases, cómo comprobar que anda, qué arrancar cada vez y qué hacer si algo falla |
| `RagBasico.Api\Buscador.cs` | El modo `vectores-qdrant`; la híbrida usa Qdrant y sigue con SQL si Qdrant no responde o no tiene vectores; las búsquedas devuelven el aviso |
| `RagBasico.Api\Indexador.cs` | Pone Qdrant al día después de cargar: borra de Qdrant lo que ya no está |
| `RagBasico.Api\Vectorizador.cs` | Pone Qdrant al día después de calcular: sube los vectores nuevos. Corta solo si Ollama no responde o si fallan cinco lotes seguidos (una errata de la fase 5) |
| `RagBasico.Api\Evaluador.cs` | Revisa Qdrant antes de medir y corta si no sirve para medir; un modo que falla no corta el examen; avisa si la híbrida siguió sin Qdrant |
| `RagBasico.Api\Redactor.cs` | Pasa el aviso de la búsqueda a la respuesta |
| `RagBasico.Api\Ollama.cs` | La lista de modelos instalados y el calentamiento |
| `RagBasico.Api\Modelos.cs` | El aviso en `RespuestaRag`, y los records de la búsqueda, de Qdrant y de la salud |
| `RagBasico.Api\Program.cs` | Registra Qdrant, el `Sincronizador` y la `Salud`; el calentamiento al arrancar; `/salud`, `/qdrant/estado` y `/qdrant/reconstruir` |
| `RagBasico.Api\wwwroot\index.html` | El modo con Qdrant y el aviso arriba de la respuesta |
| `scripts\iniciar-qdrant.ps1` | Nuevo: arranca Qdrant solo en `127.0.0.1`, con sus datos fijos y sin telemetría |
| `RagBasico.Api\appsettings.json` | La base `RagBasico_F7`, las claves de `Qdrant` y menos líneas de log por cada pedido HTTP |
| `RagBasico.Api\Properties\launchSettings.json` | Puerto 5087 |
| `RagBasico.Api\RagBasico.Api.http` | Puerto 5087, `/salud`, `/qdrant/estado`, `/qdrant/reconstruir` y la búsqueda con Qdrant |

Endpoints: `GET /salud`, `GET /qdrant/estado` y `POST /qdrant/reconstruir`. `GET /buscar` devuelve un objeto con los fragmentos y el aviso, en lugar de la lista sola. Tablas: ninguna; la colección de Qdrant `fragmentos_f7`.

## 3. Orden de lectura

1. `scripts\iniciar-qdrant.ps1`: cómo se arranca Qdrant y por qué así.
2. `RagBasico.Api\Qdrant.cs`: `CrearColeccionAsync` con los dos umbrales, después `SubirAsync`, `BuscarAsync` y `ListarIdsAsync`.
3. `RagBasico.Api\Sincronizador.cs`: `Comparar`, que es la pieza; después `EstadoAsync`, `PonerAlDiaAsync`, `ReconstruirAsync` y `MuestraCoincideAsync`.
4. `RagBasico.Api\Buscador.cs`: `PorVectoresQdrantAsync`, `HibridaAsync` y `VectoresConRespaldoAsync`.
5. El final de `Indexador.IndexarCarpetaAsync` y de `Vectorizador.CalcularPendientesAsync`.
6. `RagBasico.Api\Salud.cs` y el calentamiento en `Program.cs`.

## 4. Cómo se corre y se prueba

1. Instalar Qdrant 1.20.2 como dice `docs\instalacion.md`: el zip `qdrant-x86_64-pc-windows-msvc.zip`, descomprimido de modo que quede `F:\RAG-BASICO-datos\qdrant\qdrant.exe`.
2. En una consola aparte: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\iniciar-qdrant.ps1`. Queda corriendo.
3. Crear la base `RagBasico_F7` con `scripts\crear-base.ps1` y arrancar la API. En el log tiene que aparecer "Los dos modelos quedaron cargados en Ollama".
4. `POST /indexar` y `POST /vectores`: al final del detalle, la línea `QDRANT:` dice cuántos puntos subió.
5. `GET /qdrant/estado`: tiene que decir "Iguales", con `VectoresIndexados` igual a `PuntosEnQdrant`.
6. `GET /salud`: los cinco chequeos en `ok`.
7. La telemetría de Qdrant, para ver que busca con el índice: después de unas búsquedas con `vectores-qdrant`, abrir `http://127.0.0.1:6333/telemetry?details_level=10` y buscar `vector_index_searches`: tienen que crecer las búsquedas `unfiltered_hnsw` y no las `unfiltered_plain`.
8. Con una carpeta, una base y una colección de prueba, para no tocar las del examen. Antes, frenar la API que quedó corriendo desde el paso 3 (Ctrl+C en su consola, o Shift+F5 en Visual Studio): las dos escuchan en el puerto 5087. En una sola consola, desde la raíz del repo:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1 -Base RagBasico_F7_Prueba
   $env:Rag__CarpetaDocumentos = 'F:\RAG-BASICO-datos\documentos-prueba'
   $env:ConnectionStrings__Rag = 'Server=localhost\SQLEXPRESS;Database=RagBasico_F7_Prueba;Integrated Security=True;TrustServerCertificate=True'
   $env:Qdrant__Coleccion = 'fragmentos_prueba'
   cd RagBasico.Api
   dotnet run
   ```

   Agregar un PDF, pisar otro, `POST /indexar`, `POST /vectores` y `GET /qdrant/estado`: tiene que decir "Iguales". Sin `Qdrant__Coleccion`, la base de prueba usaría la colección del examen y le borraría los vectores. Después, cerrar esa consola y volver a arrancar la API en una nueva.
9. `POST /evaluar`: los cuatro modos. `vectores-qdrant` tendría que dar igual o casi igual que `vectores`, y la híbrida igual o casi igual que en la fase 6.
10. Medir el tiempo de `POST /qdrant/reconstruir` y los ms de búsqueda de `vectores` contra `vectores-qdrant` en la tabla del examen.
11. Al final, frenar Qdrant (Ctrl+C en su consola) y preguntar en la página con la híbrida: tiene que responder igual, con el aviso de que usó los vectores de SQL. `GET /salud` da `aviso`, no `error`. Para seguir, volver a arrancar Qdrant con `scripts\iniciar-qdrant.ps1`.

## 5. Resultados

| Medición | Valor | Cómo se midió | Fecha |
|---|---|---|---|
| Compilación | 0 advertencias, 0 errores | `dotnet build` | 19/11/2026 |
| Comparación SQL contra Qdrant | 5 casos: faltan, sobran, mismo orden, colección vacía, misma cantidad con números distintos | Un proyecto de prueba aparte | 19/11/2026 |
| Cliente de Qdrant | 11 casos contra un Qdrant falso que controla la forma de cada pedido: versión, crear con los dos umbrales, subir 2.500 puntos, listar en tres páginas, buscar, leer un vector, borrar puntos y la colección, y el error 404 con su mensaje | Un proyecto de prueba aparte | 19/11/2026 |
| Reconstruir con un borrado que dice que sí y no borra | Lo intenta de nuevo y la vuelve a crear | El Qdrant falso, que imita el bug de Windows | 19/11/2026 |
| `/salud` sin SQL Server | 503, con SQL en `error`, los modelos y la dimensión en `ok` y Qdrant en `aviso` por la colección | La API corriendo con un Ollama y un Qdrant falsos | 19/11/2026 |
| Calentamiento al arrancar | El log dice "Los dos modelos quedaron cargados en Ollama" | La API con el Ollama falso | 19/11/2026 |
| Revisión de las fases 5 a 7 | Contra la documentación de SQL Server 2025, Qdrant 1.20 y Ollama, sin errores de sintaxis ni de formato. Encontró cuatro problemas de lógica, arreglados: el examen con la colección vacía, la híbrida con la colección vacía, el corte de `/vectores` y el aviso de Qdrant | Una revisión aparte del código | 19/11/2026 |
| Arreglos de la revisión | 5 casos: cuándo un error muestra que Ollama no responde, y el aviso de la colección vacía | Un proyecto de prueba aparte | 19/11/2026 |
| Verificación de las fases 7 y 8 | Cinco revisores (los arreglos, la fase 8, la documentación, las notas y un ensayo de los pasos en Windows), y cada hallazgo contrastado por dos o tres más. Lo de la fase 7, arreglado: `/vectores` cortado decía `SIN CAMBIOS` y perdía la cuenta, el examen con la colección vacía, el aviso de la híbrida cuando Qdrant contesta un error, los `ollama pull`, el paso 8 que borraba los vectores del examen, y textos de la documentación | Revisiones aparte del código y la documentación | 19/11/2026 |
| Arreglos de la verificación | 4 casos: la regla de servicio caído, en su lugar nuevo, y el aviso de Qdrant con un error | Un proyecto de prueba aparte | 19/11/2026 |
| Revisión final de los arreglos | Tres revisores más, y cada hallazgo contrastado por dos. Lo de esta fase, arreglado: el examen sin nada cargado mandaba a `POST /vectores` en lugar de a `POST /indexar`, el paso 8 no frenaba la API del paso 3, dos filas de "Qué cambió" y la aclaración de `SIN CAMBIOS` con PDF escaneados | Revisiones aparte del código y la documentación | 19/11/2026 |
| Arranque de Qdrant 1.20.2 real con `scripts\iniciar-qdrant.ps1` | Escucha solo en `127.0.0.1`, en los puertos 6333 y 6334; los datos quedan en `storage` y `snapshots`, adentro de su carpeta; `GET /` responde la versión 1.20.2 | Qdrant bajado de GitHub, en Windows 11 | 19/11/2026 |
| Conexión a `localhost\SQLEXPRESS` con el SQL Server Browser detenido | sqlcmd sin prefijo espera al Browser hasta agotar el tiempo y no llega a la memoria compartida, aunque esté disponible; con `lpc:`, se conecta en 0,1 s. SqlClient, el de la API, prueba primero la memoria compartida y no necesita el Browser | sqlcmd 1.10.0 y Microsoft.Data.SqlClient 7.1.2 contra un pipe de prueba con el nombre de la memoria compartida de una instancia | 19/11/2026 |
| Tiempo de reconstruir Qdrant | pendiente | `POST /qdrant/reconstruir` | pendiente |
| ms de búsqueda en SQL contra Qdrant, y el examen de los cuatro modos | pendiente | `POST /evaluar` | pendiente |
| Búsquedas con el índice HNSW según la telemetría | pendiente | `GET /telemetry` de Qdrant | pendiente |

Versiones: Qdrant 1.20.2, más las de la fase 5. Commit medido: el último de `fase-7-qdrant`.

## 6. Decisiones

| Decisión | Alternativas descartadas | Por qué |
|---|---|---|
| SQL Server manda y Qdrant es una copia de los vectores | Qdrant como única base de vectores | Si Qdrant se pierde, se reconstruye desde SQL; y la demo puede seguir sin Qdrant |
| El id del punto es el número de fragmento | Un id propio de Qdrant | Los dos lados se comparan por el mismo número, sin una tabla de equivalencias |
| Comparar números de fragmento, no cantidades | Contar los puntos de cada lado | Con la misma cantidad puede faltar uno y sobrar otro |
| Una muestra de vectores en la comparación | Confiar en que se reconstruye cada vez que se recrea la base | Si alguien se olvida, Qdrant devuelve otros textos sin ningún error; la muestra lo detecta |
| Poner Qdrant al día al final de `/indexar` y `/vectores` | Un endpoint aparte para sincronizar | Nadie se olvida de correrlo, y si Qdrant estuvo apagado, la próxima corrida lo arregla |
| Los dos umbrales en 10 KB | Los valores por defecto | Con pocos miles de vectores, Qdrant no armaría el índice o no lo usaría, y la fase "del índice" no tendría índice |
| La híbrida sigue con SQL si Qdrant no responde, y lo avisa | Un error 500 | La demo no se cae por una pieza que tiene respaldo, y el aviso no esconde lo que pasó |
| `vectores-qdrant` da error si Qdrant no responde | Caer en SQL también | Ese modo existe para medir Qdrant; caer en SQL mediría otra cosa |
| `/evaluar` se corta sin Qdrant, sin la colección, con la colección vacía o con vectores de otra base | Medir lo que se pueda | La tabla mezclaría modos que miden otra cosa |
| Un modo que falla a la mitad no corta el examen | Que el error corte todo | Se perderían los minutos de los otros modos, con sus llamadas a Qwen |
| Una colección vacía lleva al respaldo en SQL, con su propio aviso | Seguir con la lista vacía | La híbrida quedaba solo con las palabras, sin ningún aviso |
| `/vectores` corta solo si Ollama no responde, o si fallan cinco lotes seguidos | Cortar si falla un lote entero, como hasta la fase 6 | Un lote de textos que Ollama no acepta trababa todas las corridas en el mismo lugar |
| El aviso de la híbrida distingue Qdrant apagado de Qdrant con un error | Un solo aviso | Lo primero se arregla arrancando Qdrant; lo segundo, con `POST /vectores` o `POST /qdrant/reconstruir` |
| Una sola regla de servicio caído, `Errores.EsCaida`, para `/vectores` y la híbrida | Una en cada clase | Si cambia cómo llega el error, como en la fase 8, se cambia en un solo lugar |
| `/salud` con Qdrant apagado da aviso y 200 | Error y 503 | La demo anda; un 503 haría creer que no |
| Qdrant solo en `127.0.0.1`, con datos fijos y sin telemetría | Los valores por defecto | Por defecto escucha en toda la red sin clave y guarda los datos donde se lo arranca |
| Calentar los modelos al arrancar | Esperar a la primera pregunta | La primera pregunta de una demo no puede tardar 30 segundos |
| Qdrant por HTTP con JSON a mano | El SDK de Qdrant para .NET | Se ve qué se manda, igual que con Ollama, y no suma una dependencia |
| `crear-base.ps1` se conecta por memoria compartida (`lpc:`) | Arrancar el servicio SQL Server Browser | Es un servicio más, que no siempre está andando; sin él, el sqlcmd que instala winget no encuentra una instancia con nombre |

## 7. Limitaciones conocidas

- Comparar SQL con Qdrant lee todos los ids de los dos lados: con un millón de fragmentos son unos mil pedidos a Qdrant en cada `/indexar` y `/vectores`. Para eso alcanza; para más, habría que comparar por lotes.
- Mientras Qdrant está apagado, `/indexar` puede borrar fragmentos de SQL que siguen en Qdrant. La híbrida los saltea, porque busca los textos en SQL, y la próxima corrida los borra de Qdrant.
- El índice HNSW es aproximado: en colecciones grandes, puede no devolver exactamente los mismos 5 que la búsqueda exacta de SQL.
- Si Ollama no responde, la búsqueda por significado no anda, ni en SQL ni en Qdrant: los dos necesitan el vector de la pregunta.
- Esta versión no se probó contra SQL Server, Ollama ni Qdrant reales: el servicio de SQL Server estaba detenido, Qdrant todavía no estaba instalado, y en lugar de Ollama y de Qdrant se usaron servidores falsos que siguen la documentación de sus API. De Qdrant real se probó después solo el arranque, con `scripts\iniciar-qdrant.ps1` (en la sección 5).
