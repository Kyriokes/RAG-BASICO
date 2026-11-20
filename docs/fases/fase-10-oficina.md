# Fase 10: la misma versión, en los servidores de la oficina

## 1. Objetivo

Correr el RAG con cada pieza en su servidor: SQL Server en uno, Qdrant en otro, Ollama en otro, y los PDF que ya existen en la base vieja. **Sin cambiar el código:** lo que cambia entre la fase 9 y la 10 es la configuración y la documentación.

La diferencia entre correr en una PC y correr en la oficina es un archivo, `RagBasico.Api\conexiones.json`, que se arma copiando `conexiones.oficina.ejemplo.json`.

## 2. Qué cambió

| Archivo | Qué cambió |
|---|---|
| `conexiones.oficina.ejemplo.json` | Nuevo: las diez líneas de `Conexiones` con nombres de ejemplo, para copiar a `RagBasico.Api\conexiones.json` y completar |
| `docs\oficina.md` | Nuevo: la guía para conectar cada servidor, comprobarlo, cargar las directivas y qué hacer si algo no anda |
| `docs\fases\fase-10-oficina.md` | Nuevo: este documento |
| `RagBasico.Api\appsettings.json` | `Rag:Fase` en `fase-10-oficina`, la base `RagBasico_F10` y la colección `fragmentos_f10` |
| `RagBasico.Api\Properties\launchSettings.json` | Puerto 5090 |
| `RagBasico.Api\RagBasico.Api.http` | Puerto 5090 |
| `README.md`, `docs\arquitectura.md`, `docs\responsabilidades.md`, `docs\instalacion.md`, `docs\manual.md` | Al día con esta fase |

Ningún archivo `.cs`, `.sql`, `.html`, `.js` ni `.css` cambió: `git diff fase-9-local fase-10-oficina --stat` lo muestra.

Endpoints: ninguno nuevo. Tablas: ninguna.

## 3. Orden de lectura

1. `conexiones.oficina.ejemplo.json`: las diez líneas.
2. `docs\oficina.md`: de arriba a abajo. Es una guía para seguir, no para estudiar.
3. En `RagBasico.Api\Program.cs`, la línea que carga `conexiones.json`: es todo el código que hace posible esta fase, y ya estaba en la fase 9.

## 4. Cómo se corre y se prueba

En casa, sin los servidores de la oficina, esta fase corre igual que la 9: sin `conexiones.json`, usa lo de `appsettings.json`.

1. Crear la base: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1`. Crea `RagBasico_F10`.
2. Doble clic en `iniciar.cmd`: la página abre en `http://127.0.0.1:5090/`.

En la oficina: [`docs\oficina.md`](../oficina.md), en orden.

Para probar en casa el mismo camino que en la oficina, se puede armar un `conexiones.json` que apunte a la propia PC por su nombre de red en lugar de `localhost`, con SQL Server por TCP y un usuario de SQL. Los pasos para habilitar eso son los del punto 2 de `docs\oficina.md`.

## 5. Resultados

| Medición | Valor | Cómo se midió | Fecha |
|---|---|---|---|
| Compilación | 0 advertencias, 0 errores | `dotnet build` | 20/11/2026 |
| El código es el de la fase 9 | Ningún `.cs`, `.sql`, `.html`, `.js` ni `.css` cambió | `git diff fase-9-local fase-10-oficina --stat` | 20/11/2026 |
| Las instrucciones de Ollama y de Qdrant | `OLLAMA_HOST`, `QDRANT__SERVICE__API_KEY`, el encabezado `api-key` y el `docker run` de Qdrant, comparados con su documentación oficial | Las páginas de preguntas frecuentes de Ollama y de seguridad, configuración e inicio rápido de Qdrant | 20/11/2026 |
| Contra los servidores de la oficina | pendiente | Siguiendo `docs\oficina.md` en la oficina | pendiente |

Versiones: las de la fase 9. Commit medido: el último de `fase-10-oficina`.

## 6. Decisiones

| Decisión | Por qué |
|---|---|
| El mismo código que la fase 9 | Lo que se probó en casa es lo que corre en la oficina. Si la fase 10 necesita un arreglo de código, se hace en la 9 y se trae |
| Las conexiones de la oficina, en un archivo que git ignora | Los nombres de los servidores y las claves no llegan a GitHub |
| Un ejemplo con nombres en mayúsculas | Se ve de un vistazo qué hay que reemplazar, y si se copia sin completar, falla enseguida con un nombre que no existe |
| El ejemplo trae `TOP 200` en la consulta de la lista | La primera importación tiene que ser chica: sirve para medir la velocidad y ver cuántos son escaneos |
| La guía comprueba cada servidor con `Test-NetConnection` antes de abrir la página | Lo que más falla al pasar a varios servidores es la red: TCP apagado, firewall, un servicio que escucha solo en su PC |
| La API sigue escuchando solo en su PC | No tiene usuarios ni permisos. Abrirla a la red pide agregar eso primero |
| La base se crea a mano, con `sql\base.sql` en SSMS | La hace una vez quien administra el servidor, con sus permisos. La API entra con un usuario que solo lee y escribe |

## 7. Limitaciones conocidas

- No se probó contra los servidores de la oficina desde casa: la primera prueba real es ahí.
- La clave de SQL Server queda escrita en `conexiones.json`, en la PC donde corre la API. Con un usuario de Windows no hay clave en ningún archivo.
- La conexión con Qdrant y con Ollama va sin cifrar (HTTP). En una red interna es lo habitual; la clave de Qdrant, si se usa, viaja así.
- Cambiar `conexiones.json` pide volver a arrancar la API.
- La carga de las 50.000 directivas tarda de varias horas a varios días, según la máquina de Ollama. Se mide con las primeras 200.
- Las limitaciones de la fase 9 siguen: no lee escaneados, procesa un documento por vez, no tiene usuarios.
