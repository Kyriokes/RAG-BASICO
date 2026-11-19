# Erratas

Bugs encontrados después de cerrar una fase, y dónde se arreglaron.

Este archivo solo se actualiza en la punta. Para ver las erratas de una fase vieja, mirá este archivo en la branch de fase más nueva.

| Fase | Archivo | Función | Síntoma | Arreglo |
|---|---|---|---|---|
| fase-0-base | `docs\responsabilidades.md` | `RagBasico.Api\Properties\launchSettings.json` | Dice que el perfil abre el navegador al correr la API con `dotnet run` o con F5; lo hace F5 en Visual Studio, no `dotnet run` | En fase-7-qdrant |
| fase-1-ollama a fase-6-hibrida | `docs\fases\fase-1-ollama.md` a `docs\fases\fase-6-hibrida.md` | Resultados y Limitaciones conocidas | Dicen que Ollama no estaba instalado, y las fases 2 y 3, que SQL Server tampoco. Los dos ya estaban instalados; lo que faltó fue probar esas versiones contra ellos | En fase-7-qdrant |
| fase-2-rag-minimo | `scripts\crear-base.ps1` | ninguna | Con el servicio de SQL Server detenido, falla a los 45 segundos sin decir por qué | En fase-7-qdrant: si falla, sugiere revisar el servicio |
| fase-2-rag-minimo | `scripts\crear-base.ps1`, y los pasos de `docs\fases\fase-2-rag-minimo.md` a `docs\fases\fase-6-hibrida.md` que lo corren | ninguna | Con el sqlcmd que instala winget y el servicio SQL Server Browser detenido, no se conecta ni con SQL Server andando: espera al Browser hasta agotar el tiempo y dice `Timed out waiting for pipe` | En fase-7-qdrant: se conecta por memoria compartida (`lpc:`). En las fases 2 a 6: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1 -Servidor 'lpc:localhost\SQLEXPRESS'` |
| fase-4-carga | `docs\fases\fase-4-carga.md` | Decisiones | Dice que la fecha de modificación cambia al copiar un archivo; en Windows, copiar la conserva | En fase-7-qdrant, con los motivos verdaderos para no usar la fecha |
| fase-4-carga | `docs\fases\fase-4-carga.md`, `README.md` y `RagBasico.Api\RagBasico.Api.http` | Cómo se corre y se prueba | Dicen que la segunda carga seguida dice `SIN CAMBIOS`, pero con PDF escaneados o con error en la carpeta, esos se repiten y no lo dice; y la prueba del borrado no dice que hay que frenar la API que estaba corriendo | En fase-7-qdrant |
| fase-5-vectores | `RagBasico.Api\Vectorizador.cs` | `CalcularPendientesAsync` | Si fallaban todos los fragmentos de un lote por su contenido, `/vectores` cortaba la corrida culpando a Ollama, y como cada corrida empieza de nuevo, se trababa siempre en el mismo lote: los fragmentos de después no tenían vector nunca | En fase-7-qdrant: corta solo si Ollama no responde, o si fallan cinco lotes seguidos |
| fase-6-hibrida | `README.md`, `docs\arquitectura.md` y `docs\fases\fase-6-hibrida.md` | ninguna | Dicen que el navegador abre la página de la demo al arrancar; lo hace F5 en Visual Studio, no `dotnet run` | En fase-7-qdrant |
