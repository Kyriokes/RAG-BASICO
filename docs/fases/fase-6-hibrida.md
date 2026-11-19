# Fase 6: lo mejor de las dos, y la página

## 1. Objetivo

Quedarse con lo mejor de cada búsqueda: las palabras encuentran números de resolución, códigos y nombres propios; los vectores encuentran lo que se dijo con otras palabras. La híbrida corre las dos al mismo tiempo y mezcla los resultados, y pasa a ser el modo por defecto.

Y tener la demo: una página donde se escribe la pregunta, se elige el modo y se ve la respuesta con sus fuentes. El momento clave es una pregunta dicha con otras palabras que falla en "solo palabras" y acierta en "híbrida".

## 2. Qué cambió

| Archivo | Qué cambió |
|---|---|
| `RagBasico.Api\Rrf.cs` | Nuevo: mezcla dos listas de resultados por puesto |
| `RagBasico.Api\Buscador.cs` | El modo `hibrida`, con las dos búsquedas en paralelo, y pasa a ser el modo por defecto |
| `RagBasico.Api\wwwroot\index.html` | Nuevo: la página de la demo |
| `RagBasico.Api\Program.cs` | Sirve la página en la raíz, y la información del proyecto pasa de `/` a `/info` |
| `RagBasico.Api\appsettings.json` | La base `RagBasico_F6` y la clave `Rag:CandidatosHibrida` |
| `RagBasico.Api\Properties\launchSettings.json` | Puerto 5086 |
| `RagBasico.Api\RagBasico.Api.http` | Puerto 5086, `GET /info` y los pedidos con la híbrida |

Endpoints: `GET /` pasa a ser la página y la información del proyecto pasa a `GET /info`. El modo por defecto de `GET /buscar` y `POST /preguntar` pasa a ser `hibrida`. Tablas: ninguna.

## 3. Orden de lectura

1. `RagBasico.Api\Rrf.cs`: la mezcla. Es corta y no depende de nada.
2. `RagBasico.Api\Buscador.cs`: `HibridaAsync`, y después los cambios en `Modos`, `ModoPorDefecto` y `BuscarAsync`.
3. `RagBasico.Api\Program.cs`: `UseDefaultFiles`, `UseStaticFiles` y `/info`.
4. `RagBasico.Api\wwwroot\index.html`: primero el HTML, después el script.

## 4. Cómo se corre y se prueba

1. Crear la base `RagBasico_F6` con `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1 -Servidor 'lpc:localhost\SQLEXPRESS'` (sin `-Servidor`, el sqlcmd que instala winget no se conecta: está en `docs\erratas.md`), arrancar la API (con F5 en Visual Studio el navegador abre la página; con `dotnet run`, se abre a mano), `POST /indexar` y `POST /vectores`.
2. `POST /evaluar`: la tabla de los tres modos. La híbrida tendría que quedar igual o mejor que la mejor de las otras dos en "entre los 5".
3. Elegir la pregunta de la demo con el detalle por pregunta del examen: una dicha con otras palabras que tenga puesto 0 en `palabras` y un puesto de 1 a 5 en `hibrida`. Probarla en la página con los dos modos.
4. Revisar a mano 10 respuestas de la página: ¿cita el fragmento correcto? ¿Inventa algo? Anotarlo en la sección 5.
5. Una vez, con Ollama sin GPU: los segundos por respuesta de Qwen, que se ven en la página como "Modelo".

## 5. Resultados

| Medición | Valor | Cómo se midió | Fecha |
|---|---|---|---|
| Compilación | 0 advertencias, 0 errores | `dotnet build` | 19/11/2026 |
| RRF | 7 casos: el que aparece en las dos listas gana, los empates en orden de aparición, `topK`, listas vacías, una lista vacía, tercero en las dos contra primero en una, k = 60 | Un proyecto de prueba aparte | 19/11/2026 |
| Página de la demo | Se ve en la raíz; muestra la respuesta con las fuentes citadas y las no citadas atenuadas; sin fuentes con "no encontré"; un texto con HTML se muestra como texto; un error de la API se muestra | La API corriendo sin SQL Server, en un navegador | 19/11/2026 |
| La tabla de los tres modos | pendiente | `POST /evaluar` | pendiente |
| 10 respuestas revisadas a mano | pendiente | La página | pendiente |
| Segundos por respuesta de Qwen con CPU sola | pendiente | La página, con Ollama sin GPU | pendiente |

Versiones: las mismas de la fase 5. Commit medido: el último de `fase-6-hibrida`.

## 6. Decisiones

| Decisión | Alternativas descartadas | Por qué |
|---|---|---|
| Mezclar puestos con RRF | Sumar o promediar los puntajes | El `RANK` del Full-Text y la distancia coseno están en escalas distintas; normalizarlos depende de cada pregunta. RRF solo mira el puesto |
| k = 60 | Otro valor, ajustado con el examen | Es el valor del trabajo original y funciona bien sin ajustarlo; ajustarlo con un examen de 40 preguntas lo acomodaría a esas preguntas |
| 20 candidatos de cada búsqueda | Los 5 de cada una | Un fragmento sexto en las dos listas tiene que poder entrar: con 5 candidatos no aparece en ninguna |
| Las dos búsquedas en paralelo | Una después de la otra | Se tarda lo que la más lenta, no la suma |
| La híbrida como modo por defecto | Seguir con palabras | Es el modo que se va a presentar; los otros quedan para comparar |
| La página dentro de la API, en `wwwroot` | Un proyecto aparte, o un framework de JavaScript | Un solo programa que arrancar, y un archivo que se lee entero |
| La página sin nada de internet | Una biblioteca de estilos de un CDN | La demo tiene que andar sin conexión |
| `textContent` para el texto de la respuesta | `innerHTML` | Un documento con HTML adentro no puede cambiar la página |
| La información del proyecto pasa a `/info` | Dejarla en `/` y la página en otra dirección | Con F5 en Visual Studio, el navegador abre la raíz: la demo arranca sola |

## 7. Limitaciones conocidas

- Si Ollama no responde, la híbrida falla entera, porque necesita el vector de la pregunta. En la fase 7 la híbrida aprende a seguir sin Qdrant, pero no sin Ollama.
- El examen mide si el documento correcto está entre los 5, no si la respuesta es buena: por eso la revisión a mano.
- La página no guarda el historial de preguntas; cada consulta queda en `rag.Consulta`.
- Esta versión no se probó contra SQL Server ni Ollama reales: el servicio de SQL Server estaba detenido.
