# Fase 4: cargar bien

## 1. Objetivo

Que `/indexar` ponga la base al día con la carpeta en lugar de recargar todo: que no vuelva a procesar lo que no cambió, que borre lo que ya no está y que corte mejor el texto.

Con 50.000 documentos, recargar todo en cada corrida lleva horas, y desde la fase 5 también obliga a recalcular todos los vectores. Además, los PDF que se sacaban de la carpeta quedaban en la base y seguían apareciendo en las respuestas.

## 2. Qué cambió

| Archivo | Qué cambió |
|---|---|
| `RagBasico.Api\Planificador.cs` | Nuevo: compara la carpeta con la base y arma el plan de carga |
| `RagBasico.Api\Indexador.cs` | Carga incremental con huella, borrado de lo que ya no está, detección de escaneados y de texto dudoso |
| `RagBasico.Api\Chunker.cs` | Corta por párrafos: sin títulos sueltos ni fragmentos casi repetidos. La constante `Version` |
| `RagBasico.Api\Modelos.cs` | `InformeIndexado` con cargados, sin cambios y borrados; los records `ArchivoEnCarpeta`, `DocumentoCargado` y `PlanDeCarga` |
| `sql\03_huella.sql` | Nuevo: la columna `Huella` en `rag.Documento` |
| `sql\consultas-utiles.sql` | La huella de cada documento, los fragmentos de un documento y los fragmentos muy cortos |
| `RagBasico.Api\appsettings.json` | La base `RagBasico_F4` |
| `RagBasico.Api\Properties\launchSettings.json` | Puerto 5084 |
| `RagBasico.Api\RagBasico.Api.http` | Puerto 5084 y el aviso de que la segunda carga dice `SIN CAMBIOS` |

Endpoints: ninguno nuevo; `POST /indexar` cambió lo que hace y lo que devuelve. Tablas: la columna `rag.Documento.Huella`.

## 3. Orden de lectura

1. `RagBasico.Api\Planificador.cs`: la comparación entre la carpeta y la base. Es la pieza central y no necesita SQL para entenderse.
2. `RagBasico.Api\Indexador.cs`: `IndexarCarpetaAsync` de arriba abajo, después `LeerCarpeta`, `ParametrosDeCorte` y `CalcularHuella`, y al final `CargarArchivoAsync` con `EsEscaneado`, `RevisarTexto` y `GuardarAsync`.
3. `RagBasico.Api\Chunker.cs`: `Partir`, después `SepararParrafos` y `Cerrar`.
4. `sql\03_huella.sql` y las consultas nuevas de `sql\consultas-utiles.sql`.

## 4. Cómo se corre y se prueba

1. Crear la base `RagBasico_F4` con `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1 -Servidor 'lpc:localhost\SQLEXPRESS'` (sin `-Servidor`, el sqlcmd que instala winget no se conecta: está en `docs\erratas.md`) y arrancar la API.
2. `POST /indexar`: la primera vez carga todo, con una línea `NUEVO` por PDF. Revisar las líneas `SIN TEXTO` y `TEXTO DUDOSO`: los fragmentos de un documento dudoso se miran en SSMS con la consulta de `sql\consultas-utiles.sql`.
3. `POST /indexar` otra vez: tiene que decir `SIN CAMBIOS` y tardar mucho menos. Si en el paso 2 hubo PDF `SIN TEXTO` o con `ERROR`, esos no quedaron en la base y vuelven a salir en cada carga, así que la línea `SIN CAMBIOS` no aparece: alcanza con que `Cargados` y `Borrados` den 0.
4. Probar el borrado con una carpeta y una base aparte, para no tocar las del examen. Antes, frenar la API que quedó corriendo (Ctrl+C en su consola, o Shift+F5 en Visual Studio): la de prueba usa el mismo puerto. En una sola consola, desde la raíz del repo:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1 -Base RagBasico_F4_Prueba -Servidor 'lpc:localhost\SQLEXPRESS'
   $env:Rag__CarpetaDocumentos = 'F:\RAG-BASICO-datos\documentos-prueba'
   $env:ConnectionStrings__Rag = 'Server=localhost\SQLEXPRESS;Database=RagBasico_F4_Prueba;Integrated Security=True;TrustServerCertificate=True'
   cd RagBasico.Api
   dotnet run
   ```

   Con tres PDF en la carpeta de prueba: cargar, sacar uno y volver a cargar: el detalle dice `BORRADO`. Pisar otro con una versión distinta: dice `CAMBIÓ`. Vaciar la carpeta: dice que no borró nada. Las variables de `$env:` valen solo en esa consola.
5. Con la carpeta del examen verificada (`scripts\crear-manifiesto.ps1 -Verificar`), `POST /evaluar` y comparar con la fase 3.

## 5. Resultados

| Medición | Valor | Cómo se midió | Fecha |
|---|---|---|---|
| Compilación | 0 advertencias, 0 errores | `dotnet build` | 18/11/2026 |
| Corte por párrafos | 13 de 13 casos: sin marcas de párrafo, título pegado a su párrafo, firma al final, renglones marcados como párrafos, sin fragmentos casi repetidos, sin perder palabras | Un proyecto de prueba aparte | 18/11/2026 |
| Plan de carga | 7 de 7 casos: nuevo, cambiado, sin cambios, borrar, carpeta vacía, base vacía, PDF que no se pudo leer | Un proyecto de prueba aparte | 18/11/2026 |
| Huella y escaneados | 4 casos de huella y 9 de escaneados y texto dudoso, más un PDF generado que muestra que PdfPig marca los párrafos con una línea en blanco | Un proyecto de prueba aparte | 18/11/2026 |
| Segunda carga "sin cambios", tiempo de la primera y de la segunda | pendiente: se mide con SQL Server y los PDF reales | `POST /indexar` dos veces | pendiente |
| "Entre los 5" por palabras con el corte por párrafos, contra la fase 3 | pendiente | `POST /evaluar` | pendiente |

Versiones: las mismas de la fase 2. Commit medido: el último de `fase-4-carga`.

## 6. Decisiones

| Decisión | Alternativas descartadas | Por qué |
|---|---|---|
| Huella de los bytes del PDF más los parámetros de corte | Huella del texto extraído; fecha de modificación del archivo | La del texto obliga a leer cada PDF para saber si cambió, y no cambia si cambia el tamaño de fragmento. La fecha no dice lo que importa: bajar de nuevo o descomprimir un PDF le cambia la fecha sin cambiarlo, pisarlo con una versión vieja deja una fecha anterior, y no sabe nada del corte |
| La versión de PdfPig y la del `Chunker` entran en la huella | Recargar a mano cuando cambian | Otra versión de PdfPig puede sacar otro texto del mismo PDF; olvidarse de recargar dejaría la base con un corte viejo sin ningún aviso |
| Si la carpeta no tiene ningún PDF, no se borra nada | Borrar lo que no está, siempre | Una carpeta vacía suele ser un disco desconectado o una ruta mal escrita, no la decisión de borrar todo |
| Borrar y volver a insertar un PDF cambiado, en una transacción | Actualizar los fragmentos uno por uno | Un PDF cambiado puede tener otra cantidad de fragmentos. En una transacción, si algo falla queda la versión anterior entera |
| Escaneado: menos de 40 palabras por página en promedio | Menos de 20 palabras en total | Un escaneo con un sello de firma por página pasa el control de un total fijo |
| Texto dudoso: se carga y se avisa | No cargarlo | Puede estar bien; lo decide una persona mirando los fragmentos en SSMS |
| Un párrafo de menos de 15 palabras se pega al siguiente | Un fragmento por párrafo | Un título solo es un fragmento inútil, y al final del fragmento anterior queda separado de su texto |
| Las palabras nuevas del final, si son menos que el solapamiento, se suman al último fragmento | Un fragmento más | Sería casi una copia del anterior y ocuparía un lugar entre los 5 |
| La comparación en una clase aparte, sin SQL | Dentro del `Indexador` | Se puede probar sin base de datos, y es la regla que más importa no romper |

## 7. Limitaciones conocidas

- Los PDF escaneados no quedan en la base, así que se vuelven a leer en cada carga y aparecen siempre como `SIN TEXTO`. Leerlos es rápido porque no tienen texto, y el aviso repetido recuerda que les falta OCR.
- `/indexar` refleja la carpeta: apuntarlo a otra carpeta con PDF borra de la base lo que no esté en ella. Para probar con otra carpeta, usar otra base, como en el paso 4.
- Una presentación exportada a PDF, con pocas palabras por página, puede tomarse como escaneada.
- Dos `/indexar` al mismo tiempo pueden chocar. Es una tarea de mantenimiento que corre una persona.
- Calcular la huella obliga a leer todos los bytes de cada PDF en cada corrida, aunque no cambien. Es mucho más rápido que sacar el texto y cortarlo, pero con 50.000 archivos en una carpeta de red se nota.
- Esta versión no se probó contra SQL Server ni Ollama reales: el servicio de SQL Server estaba detenido.
