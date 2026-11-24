# Conectar con los servidores de la oficina

Cómo pasar de "todo en una PC" a "cada pieza en su servidor". El código es el mismo: lo único que cambia es un archivo, `RagBasico.Api\conexiones.json`.

Seguí los puntos en orden. Cada uno termina con un comando para comprobarlo: no pases al siguiente hasta que ande.

Los nombres en mayúsculas (`SERVIDOR-SQL`, `SERVIDOR-QDRANT`, `SERVIDOR-IA`, `SERVIDOR-VIEJO`, `USUARIO`, `CLAVE`) son de ejemplo: reemplazalos por los reales. Los comandos son para Windows PowerShell.

## 1. Qué va en cada máquina

```text
   La PC donde corre el RAG                 SERVIDOR-SQL  (puerto 1433)
   ┌─────────────────────────┐              SQL Server con Full-Text, base RagBasico:
   │ navegador               │         ┌──► los PDF arrastrados, los párrafos, las consultas
   │    │                    │         │
   │    ▼                    │         │    SERVIDOR-QDRANT  (puerto 6333)
   │ RagBasico.Api  ─────────┼─────────┼──► Qdrant: los vectores
   │ 127.0.0.1:5090          │         │
   │ + conexiones.json       │         │    SERVIDOR-IA  (puerto 11434)
   └─────────────────────────┘         ├──► Ollama: qwen3.5:4b y qwen3-embedding:0.6b
                                       │
                                       │    SERVIDOR-VIEJO  (puerto 1433)
                                       └──► la base donde ya están los PDF. Solo lectura
```

En la PC del RAG no se instala ni SQL Server, ni Qdrant, ni Ollama: solo el SDK de .NET 10 y git. La API escucha solo en esa PC (`127.0.0.1`): se usa desde su propio navegador.

Todo el tráfico va de la PC del RAG hacia los servidores. Los servidores no necesitan llegar a la PC del RAG, ni verse entre ellos.

## 2. El SQL Server del RAG

Lo hace quien administra ese servidor.

1. **Instalarlo con Full-Text.** En el instalador, "Selección de características": marcar "Extracciones de texto completo y semánticas de búsqueda" ("Full-Text and Semantic Extractions for Search"). Si ya está instalado sin eso, se agrega volviendo a correr el instalador (está paso a paso en [`instalacion.md`](instalacion.md), punto 5).
2. **Habilitar TCP/IP.** SQL Server Express lo trae apagado: así no acepta conexiones de otra máquina. En "Administrador de configuración de SQL Server" ("SQL Server Configuration Manager"): "Configuración de red de SQL Server", "Protocolos de SQLEXPRESS", "TCP/IP": Habilitado.
3. **Fijar el puerto 1433.** En las propiedades de TCP/IP, pestaña "Direcciones IP", al final, en "IPAll": "Puertos dinámicos TCP" vacío y "Puerto TCP" en `1433`. Con un puerto fijo no hace falta el servicio SQL Server Browser.
4. **Reiniciar el servicio** de SQL Server, para que tome los dos cambios.
5. **Abrir el firewall** del servidor para el puerto 1433, como administrador:

   ```powershell
   New-NetFirewallRule -DisplayName 'SQL Server 1433' -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow
   ```

6. **Elegir cómo entra la API.** Dos maneras:
   - **Con un usuario de SQL.** Hace falta el modo mixto: en SSMS, clic derecho en el servidor, "Propiedades", "Seguridad", "Modo de autenticación de Windows y SQL Server", y reiniciar el servicio. Después, crear el usuario (punto 8).
   - **Con el usuario de Windows** de quien corre la API, si la PC y el servidor están en el mismo dominio. No hay clave en ningún archivo.
7. **Crear la base.** En SSMS, abrir `sql\base.sql`, activar el menú "Consulta" → "Modo SQLCMD" ("Query" → "SQLCMD Mode"), agregar como primera línea `:setvar Base RagBasico` y ejecutar. Crea la base `RagBasico` con sus tablas y el índice Full-Text. Se puede correr de nuevo sin romper nada.
8. **Darle permiso al usuario.** En SSMS, sobre la base nueva (cambiá `USUARIO` y `CLAVE`; con usuario de Windows, la primera línea es `CREATE LOGIN [DOMINIO\usuario] FROM WINDOWS;` y la segunda `CREATE USER [DOMINIO\usuario] FOR LOGIN [DOMINIO\usuario];`):

   ```sql
   CREATE LOGIN USUARIO WITH PASSWORD = 'CLAVE';
   USE RagBasico;
   CREATE USER USUARIO FOR LOGIN USUARIO;
   ALTER ROLE db_datareader ADD MEMBER USUARIO;
   ALTER ROLE db_datawriter ADD MEMBER USUARIO;
   ```

   Leer y escribir alcanza: la API no crea tablas.

**La línea de `conexiones.json`:**

- Con usuario de SQL: `Server=SERVIDOR-SQL,1433;Database=RagBasico;User Id=USUARIO;Password=CLAVE;TrustServerCertificate=True`
- Con usuario de Windows: `Server=SERVIDOR-SQL,1433;Database=RagBasico;Integrated Security=True;TrustServerCertificate=True`

`SERVIDOR-SQL,1433` es el servidor y el puerto, separados por una coma. No lleva `\SQLEXPRESS`: con el puerto ya alcanza.

**Comprobar, desde la PC del RAG:**

```powershell
Test-NetConnection SERVIDOR-SQL -Port 1433
```

Tiene que decir `TcpTestSucceeded : True`. Si dice `False`, es el punto 2, 3, 4 o 5.

## 3. Qdrant

Lo hace quien administra ese servidor. Qdrant, por defecto, no pide clave: cualquiera que llegue al puerto puede leer y borrar. En una red interna alcanza con el firewall; si se quiere, se le pone una clave.

La configuración se hace con variables de entorno que empiezan con `QDRANT__` y separan los niveles con dos guiones bajos.

**En Windows**, con el `qdrant.exe` de la página de versiones (como en casa), en una consola parada en su carpeta:

```powershell
$env:QDRANT__SERVICE__HOST = '0.0.0.0'
$env:QDRANT__SERVICE__HTTP_PORT = '6333'
$env:QDRANT__TELEMETRY_DISABLED = 'true'
# Opcional, si se quiere clave:
# $env:QDRANT__SERVICE__API_KEY = 'CLAVE-DE-QDRANT'
.\qdrant.exe
```

`0.0.0.0` es "escuchar en todas las placas de red". En casa, `scripts\iniciar-qdrant.ps1` usa `127.0.0.1`, que es "solo esta PC": ese script no sirve para un servidor. Y abrir el firewall, como administrador:

```powershell
New-NetFirewallRule -DisplayName 'Qdrant 6333' -Direction Inbound -Protocol TCP -LocalPort 6333 -Action Allow
```

**Con Docker** (en Linux o en Windows), el comando de la documentación de Qdrant, que guarda los datos en la carpeta `qdrant_storage`:

```bash
docker run -p 6333:6333 -p 6334:6334 \
    -v "$(pwd)/qdrant_storage:/qdrant/storage:z" \
    qdrant/qdrant
```

Para ponerle clave, se agrega `-e QDRANT__SERVICE__API_KEY=CLAVE-DE-QDRANT` antes de `qdrant/qdrant`. En Windows, la documentación recomienda un volumen con nombre en lugar de una carpeta: `-v qdrant_storage:/qdrant/storage`.

**En Linux sin Docker**, el binario de la página de versiones, con las mismas variables:

```bash
QDRANT__SERVICE__HOST=0.0.0.0 QDRANT__TELEMETRY_DISABLED=true ./qdrant
```

**Las líneas de `conexiones.json`:** `"Qdrant": "http://SERVIDOR-QDRANT:6333"`, `"ColeccionQdrant": "fragmentos"` y, si se le puso clave, `"ClaveQdrant": "CLAVE-DE-QDRANT"`.

Si ese Qdrant lo usan otros proyectos, la colección tiene que tener un nombre que no use nadie más: "Reprocesar todo" borra esa colección entera.

**Comprobar, desde la PC del RAG:**

```powershell
Test-NetConnection SERVIDOR-QDRANT -Port 6333
Invoke-RestMethod http://SERVIDOR-QDRANT:6333
```

El segundo muestra la versión. Sirve la 1.20.1 o posterior.

## 4. Ollama

Lo hace quien administra esa máquina. Ollama, por defecto, escucha solo en su propia PC (`127.0.0.1`, puerto 11434). Para que atienda a otras máquinas se le pone la variable `OLLAMA_HOST`.

**En Windows:**

1. Cerrar Ollama: clic en su ícono de la bandeja, "Quit Ollama".
2. En Configuración (o en el Panel de control), buscar "variables de entorno" y abrir "Editar las variables de entorno de esta cuenta".
3. Crear la variable `OLLAMA_HOST` con el valor `0.0.0.0:11434`.
4. Aceptar y volver a abrir Ollama desde el menú Inicio.
5. Abrir el firewall, como administrador:

   ```powershell
   New-NetFirewallRule -DisplayName 'Ollama 11434' -Direction Inbound -Protocol TCP -LocalPort 11434 -Action Allow
   ```

**En Linux, con systemd:**

```bash
sudo systemctl edit ollama.service
```

En el editor que se abre, agregar:

```ini
[Service]
Environment="OLLAMA_HOST=0.0.0.0:11434"
```

Y después:

```bash
sudo systemctl daemon-reload
sudo systemctl restart ollama
```

**Con Docker:** el contenedor se publica con `-p 11434:11434` en su `docker run`. Si ya está corriendo y desde otra máquina no responde, ese es el parámetro que falta.

**Los dos modelos**, en esa máquina:

```bash
ollama pull qwen3.5:4b
ollama pull qwen3-embedding:0.6b
```

Hace falta Ollama 0.30 o posterior. Con placa de video, `ollama ps` tiene que decir `100% GPU` con un modelo cargado; con `100% CPU` anda, pero cada respuesta y cada vector tardan mucho más.

**Las líneas de `conexiones.json`:** `"Ollama": "http://SERVIDOR-IA:11434"`. Los nombres de los modelos ya están en `appsettings.json`; solo se ponen si son otros.

**Comprobar, desde la PC del RAG:**

```powershell
Test-NetConnection SERVIDOR-IA -Port 11434
(Invoke-RestMethod http://SERVIDOR-IA:11434/api/tags).models.name
```

El segundo lista los modelos: tienen que estar los dos.

## 5. La base donde ya están los PDF

Es opcional: sin esto, los PDF se cargan arrastrándolos. La API solo lee de esta base.

1. **Un usuario de solo lectura**, en ese servidor (cambiá `BASE-VIEJA`, `USUARIO-LECTOR` y `CLAVE`):

   ```sql
   CREATE LOGIN USUARIO-LECTOR WITH PASSWORD = 'CLAVE';
   USE [BASE-VIEJA];
   CREATE USER [USUARIO-LECTOR] FOR LOGIN [USUARIO-LECTOR];
   ALTER ROLE db_datareader ADD MEMBER [USUARIO-LECTOR];
   ```

   En un SQL Server 2008 o anterior, la última línea es `EXEC sp_addrolemember 'db_datareader', 'USUARIO-LECTOR';`.
2. **Que se llegue por la red**, igual que en el punto 2: TCP/IP habilitado, el puerto y el firewall. Si usa una instancia con nombre y sin puerto fijo, la cadena lleva `SERVIDOR-VIEJO\INSTANCIA` y tiene que estar andando el servicio SQL Server Browser.
3. **Encontrar la tabla y las columnas.** En SSMS, sobre esa base, esta consulta lista las columnas que pueden guardar un PDF:

   ```sql
   SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE
   FROM INFORMATION_SCHEMA.COLUMNS
   WHERE DATA_TYPE IN ('varbinary', 'image')
   ORDER BY TABLE_SCHEMA, TABLE_NAME;
   ```

   Y esta muestra todas las columnas de la tabla elegida, para encontrar la del número y la del nombre:

   ```sql
   SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'LA-TABLA';
   ```

4. **Escribir las dos consultas.** Hacen falta tres columnas: una que identifique a cada documento sin repetirse (la clave de la tabla), el nombre y el binario.
   - `ConsultaLista` devuelve, en este orden, el número y el nombre: `SELECT Id, Nombre FROM dbo.TABLA_DE_PDF`. No tiene que traer el binario.
   - `ConsultaContenido` devuelve el binario de uno. El número llega en el parámetro `@Id`: `SELECT Contenido FROM dbo.TABLA_DE_PDF WHERE Id = @Id`.
   - Si la tabla no tiene una columna con el nombre, se arma uno: `SELECT Id, 'directiva-' + CAST(Id AS VARCHAR(20)) + '.pdf' FROM ...`.
   - Si en la tabla hay otros archivos además de PDF, se filtran en `ConsultaLista` con un `WHERE`. Los que no sean PDF igual quedan en `error`, con "no es un PDF", sin frenar a los demás.
5. **Probar primero con pocos:** `SELECT TOP 20 Id, Nombre FROM ...`. Probá las dos consultas en SSMS antes de ponerlas en el archivo.

**Las líneas de `conexiones.json`:** `SqlOrigen`, `ConsultaLista` y `ConsultaContenido`.

**Comprobar:** `Test-NetConnection SERVIDOR-VIEJO -Port 1433`.

Un SQL Server viejo puede no aceptar la conexión cifrada que la API pide por defecto. Si la franja muestra un error de `SSL` o de `pre-login handshake`, está en la tabla del punto 9. Conviene probar esta conexión antes que las demás.

## 6. La PC donde corre el RAG

1. Instalar git y el SDK de .NET 10 (puntos 1 y 2 de [`instalacion.md`](instalacion.md)).
2. Bajar el repo:

   ```powershell
   git clone <dirección del repo> C:\RAG-BASICO
   cd C:\RAG-BASICO
   ```

3. Copiar el ejemplo y completarlo:

   ```powershell
   Copy-Item conexiones.oficina.ejemplo.json RagBasico.Api\conexiones.json
   notepad RagBasico.Api\conexiones.json
   ```

   Se reemplazan los nombres en mayúsculas. Las líneas que no hagan falta (la clave de Qdrant, la base de origen) se dejan vacías, con `""`. En JSON, una barra invertida se escribe doble: `SERVIDOR\\INSTANCIA`.

   Git ignora ese archivo: los nombres de los servidores y las claves no se suben. Quedan en esa PC, así que conviene que el archivo lo pueda leer solo quien usa la API.
4. Doble clic en `iniciar.cmd`. Como Qdrant está en otra máquina, no intenta arrancarlo: arranca la API y abre el navegador en `http://127.0.0.1:5090/`.

## 7. Comprobar, en orden

1. Los cuatro `Test-NetConnection` de los puntos anteriores, desde la PC del RAG: todos con `TcpTestSucceeded : True`.
2. La franja de arriba de la página. La primera vez, lo esperado es:
   - SQL Server: amarillo, "todavía no hay documentos listos".
   - Full-Text, Modelos de Ollama y Dimensión de los vectores: verde.
   - Qdrant: amarillo, "todavía no existe la colección: se crea sola al cargar el primer documento".
   - SQL de origen: verde, con la cantidad de documentos que devuelve la consulta de la lista.
3. En "Documentos", soltar un PDF. En unos segundos pasa de `pendiente` a `listo`, y SQL Server y Qdrant pasan a verde.
4. En "Preguntar", una pregunta sobre ese PDF: responde con citas.

Un punto rojo trae debajo el nombre del servidor, la dirección que se probó y qué hacer.

## 8. Cargar las directivas

No se cargan todas de una. La velocidad la pone el modelo de embeddings, y hay que medirla en la máquina de la oficina antes de largar las 50.000.

1. En `ConsultaLista`, poner `TOP 200`. Frenar la API (Ctrl+C) y volver a abrir `iniciar.cmd`: la configuración se lee al arrancar.
2. En "Documentos", "Importar de la base de origen". Anota los 200 como pendientes, en un segundo, y empiezan a procesarse.
3. Mirar el avance: "documentos por minuto" y "falta". Con ese número se calcula el total: 50.000 dividido los documentos por minuto da los minutos.

   | Documentos por minuto | 50.000 documentos |
   |---|---|
   | 10 | unos 3 días y medio |
   | 30 | unas 28 horas |
   | 100 | unas 8 horas |

4. Mirar cuántos quedaron `sin texto` (escaneos) y cuántos en `error`, y por qué: la lista se puede filtrar por estado, y cada uno muestra su motivo. Si de 200 la mitad son escaneos, conviene saberlo antes.
5. Hacer preguntas sobre esos 200 y leer las respuestas.
6. Cuando eso esté bien, sacar el `TOP`, volver a arrancar y apretar "Importar" de nuevo: anota los que faltan, sin repetir los 200. Se puede dejar corriendo de un día para el otro: si se apaga la PC o se cae un servidor, al volver sigue sola donde quedó.

Mientras carga, ya se puede preguntar sobre los documentos que están `listo`.

Lo que ocupa: en Qdrant, cada fragmento son unos 4 KB de memoria, así que un millón de fragmentos son unos 4 GB. En SQL Server, el texto de un millón de fragmentos son unos 2 a 3 GB; los PDF importados no se copian.

## 9. Si algo no anda

| Lo que se ve | Qué pasa y qué hacer |
|---|---|
| `Test-NetConnection` da `TcpTestSucceeded : False` | No se llega a ese puerto. El servicio no está andando, no escucha en la red (TCP/IP de SQL Server apagado, `OLLAMA_HOST` sin poner, Qdrant en `127.0.0.1`) o lo tapa un firewall, el del servidor o uno de la red |
| `Test-NetConnection` dice que no puede resolver el nombre | El nombre del servidor está mal, o esa PC no lo conoce. Probá con la dirección IP |
| SQL Server en rojo: `No se pudo conectar`, con `error: 26` o `error: 40` | No llega al servidor: puntos 2 a 5 del SQL Server. Si la cadena lleva `\INSTANCIA` sin puerto, falta el servicio SQL Server Browser: poné `SERVIDOR,1433` |
| SQL Server en rojo: `Login failed for user` | El usuario o la clave están mal, o el servidor no está en modo mixto (punto 6 del SQL Server) |
| SQL Server en rojo: `No se pudo abrir la base` | La base no existe o el usuario no tiene permiso: puntos 7 y 8 del SQL Server |
| SQL Server en rojo: `faltan las tablas del RAG` | La base existe pero no se corrió `sql\base.sql` en ella, o se corrió en otra |
| Full-Text en rojo | Ese SQL Server se instaló sin Full-Text: punto 1 del SQL Server |
| Al cargar un documento, un error de permisos (`permission was denied`) | Al usuario le falta `db_datawriter`: punto 8 del SQL Server |
| Modelos de Ollama en rojo: `Ollama no responde en http://SERVIDOR-IA:11434` | Ollama está cerrado, o escucha solo en su PC: falta `OLLAMA_HOST` y reiniciarlo (punto 4), o falta abrir el firewall |
| Modelos de Ollama en rojo: `faltan ...` | Falta bajar un modelo en la máquina de Ollama: el texto trae el `ollama pull` |
| Dimensión de los vectores en rojo | El modelo de embeddings no es `qwen3-embedding:0.6b`, o no da 1024 números. Revisá `Conexiones:ModeloEmbeddings` |
| Qdrant en rojo: `Qdrant no responde` | No está andando, escucha solo en `127.0.0.1` (falta `QDRANT__SERVICE__HOST=0.0.0.0`) o lo tapa el firewall |
| Qdrant en rojo: `Qdrant respondió 401` o `403` | Ese Qdrant pide clave y `Conexiones:ClaveQdrant` está vacía o mal |
| Qdrant en amarillo: los puntos y los fragmentos no coinciden | La colección tiene datos de otra carga, o se recreó la base. "Reprocesar todo". Si la colección la usa otro proyecto, cambiá `Conexiones:ColeccionQdrant` por un nombre propio |
| SQL de origen en amarillo: `Invalid object name` o `Invalid column name` | Los nombres de la tabla o de las columnas en `ConsultaLista` no son los de esa base: punto 3 de la base vieja |
| SQL Server o SQL de origen: un error que habla de `SSL`, de `certificate` o de `pre-login handshake` | La API pide una conexión cifrada y ese servidor no la ofrece como ella espera. Pasa sobre todo con un SQL Server viejo. Primero, que la cadena tenga `TrustServerCertificate=True`. Si sigue, agregale `Encrypt=False`. Si sigue, a ese SQL Server le falta la actualización que le agrega TLS 1.2: lo resuelve quien lo administra |
| SQL de origen en amarillo: `Login failed` o `No se pudo conectar` | La cadena `SqlOrigen`: igual que con el SQL Server del RAG |
| Todos los importados quedan en `error`: "no está en la base de origen" | `ConsultaContenido` no devuelve nada: el `WHERE` no usa la misma columna que el número de `ConsultaLista`, o no usa `@Id` |
| Todos los importados quedan en `error`: "no es un PDF" | La columna de `ConsultaContenido` no es la del binario, o el archivo está guardado comprimido o en otro formato |
| Muchos quedan `sin texto` | Son escaneos. Esta versión no los lee |
| Los pendientes no avanzan | La página "Documentos" dice en rojo `La carga de documentos está frenada`, con el servidor y el motivo. Cuando el servidor vuelve, sigue sola |
| Un servidor `no respondió a tiempo` | Llega la conexión pero nadie contesta: casi siempre es un firewall que descarta los pedidos, o el servidor está muy cargado. Si es Qdrant bajo una carga grande, subí `Qdrant:SegundosDeEspera` |
| Un documento queda en `error`: "se empezó a procesar 3 veces y no terminó" | Ese PDF cuelga o hace caer el lector. Se saltea solo; los demás siguen |
| La carga es muy lenta | En la máquina de Ollama, `ollama ps`: si dice `100% CPU`, no está usando la placa de video |
| Cambié `conexiones.json` y no pasa nada | La configuración se lee al arrancar: Ctrl+C y volver a abrir `iniciar.cmd` |
| La API no arranca y habla de `conexiones.json` | El archivo tiene un error de JSON: una coma de más, una comilla sin cerrar, o una barra invertida sin duplicar |

## 10. Volver a correr todo en una PC

Borrar o renombrar `RagBasico.Api\conexiones.json`. Sin ese archivo, la API usa lo de `appsettings.json`, que apunta a esta misma PC.
