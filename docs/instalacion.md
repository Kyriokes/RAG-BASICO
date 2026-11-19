# Instalación

Qué hay que instalar en Windows para correr todas las fases, en el orden en que las fases lo van necesitando, cómo comprobar que cada pieza anda y qué hacer si algo falla. Todo corre en la misma PC.

Los comandos son para Windows PowerShell. Los que dicen "como administrador" van en una consola abierta como administrador: en el menú Inicio, buscá "Windows PowerShell", hacé clic derecho y elegí "Ejecutar como administrador".

Después de instalar algo que agrega un comando (`git`, `dotnet`, `sqlcmd`, `ollama`), abrí una consola nueva: las que ya estaban abiertas no lo encuentran. Las terminales de Visual Studio y de VS Code tampoco, aunque sean nuevas, hasta que cerrás el programa (en VS Code, todas sus ventanas) y lo volvés a abrir.

Los scripts del repo se corren parado en la carpeta del repo, con `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\<script>.ps1` y los parámetros al final. Windows, por defecto, no deja correr scripts `.ps1`, y `-ExecutionPolicy Bypass` lo permite solo para ese comando.

Esta guía usa el repo en `F:\RAG-BASICO\` y, al lado, la carpeta de datos `F:\RAG-BASICO-datos\`. Si el repo va en otro lado, cambiá esas dos rutas en los comandos: la carpeta de datos va siempre al lado del repo.

## Qué se instala

| Qué | Versión | Desde | Para qué | Cómo se comprueba |
|---|---|---|---|---|
| Windows PowerShell | 5.1, viene con Windows | fase-0-base | Los scripts de `scripts\` | `$PSVersionTable.PSVersion` muestra 5 en `Major` y 1 en `Minor` |
| git | 2.31 o posterior | fase-0-base | Bajar el repo, las branches de las fases y los scripts | `git --version` |
| SDK de .NET 10 | 10.0.403, o un parche más nuevo de la banda 10.0.4xx | fase-0-base | Compilar y correr la API | `dotnet --version`, parado en la carpeta del repo |
| Un editor | Visual Studio 2026, o VS Code con la extensión REST Client | fase-0-base | Leer el código y mandar los pedidos del `.http` | Visual Studio abre `RagBasico.slnx`; en VS Code, el `.http` muestra "Send Request" arriba de cada pedido |
| Ollama | 0.30 o posterior: `qwen3.5:4b` no se baja con una más vieja | fase-1-ollama | Correr los modelos | `http://127.0.0.1:11434` dice "Ollama is running" |
| El modelo `qwen3.5:4b` | | fase-1-ollama | Redactar las respuestas | `ollama list` |
| SQL Server 2025 Express, con Full-Text | | fase-2-rag-minimo | La base: los textos, los vectores y el índice de palabras | Una consulta con `sqlcmd` (punto 6) |
| sqlcmd | 1.10 o posterior | fase-2-rag-minimo | Lo usa `scripts\crear-base.ps1` para crear la base | `sqlcmd --version` |
| SSMS | 21 o posterior | fase-2-rag-minimo | Mirar la base y correr `sql\consultas-utiles.sql`. Opcional | Se conecta a `localhost\SQLEXPRESS` |
| La carpeta de los PDF | | fase-2-rag-minimo | Los documentos públicos que se cargan | `$env:Rag__CarpetaDocumentos`, en una consola nueva |
| El modelo `qwen3-embedding:0.6b` | | fase-5-vectores | Calcular los vectores | `ollama list` |
| Qdrant | 1.20.2 | fase-7-qdrant | El índice de vectores | `http://127.0.0.1:6333` dice `"version":"1.20.2"` |

Para correr una fase vieja alcanza con lo de las filas hasta esa fase.

## 1. git y el repo

1. Bajá git de https://git-scm.com/install/windows y corré el instalador con las opciones que propone. O, en una consola: `winget install --id Git.Git -e --source winget`.
2. Comprobá, en una consola nueva: `git --version`. Hace falta la 2.31 o posterior, porque los scripts usan `git rev-parse --path-format=absolute`, que apareció en esa versión.
3. Bajá el repo con git, en una consola nueva. La dirección es la que muestra el botón "Code" de la página del repo en GitHub:

   ```powershell
   git clone <dirección del repo> F:\RAG-BASICO
   cd F:\RAG-BASICO
   ```

   Los comandos que dicen "parado en la carpeta del repo" se corren ahí, después de ese `cd`. No bajes el ZIP que ofrece GitHub: sin la carpeta `.git` no andan los scripts ni las branches de las fases.

## 2. El SDK de .NET 10

1. Bajá el instalador del SDK más nuevo de la banda 10.0.4xx, para Windows x64, de https://dotnet.microsoft.com/download/dotnet/10.0. O, en una consola: `winget install Microsoft.DotNet.SDK.10`, que instala el SDK de .NET 10 más nuevo; la banda 10.0.4xx es la última de .NET 10. Windows pide permiso de administrador.
2. Comprobá, en una consola nueva, parado en la carpeta del repo: `dotnet --version` tiene que decir 10.0.403 o un 10.0.4xx más nuevo. Fuera del repo dice el SDK más nuevo que haya instalado, por ejemplo uno de .NET 11: `global.json` vale solo adentro del repo.

`global.json` pide la 10.0.403 o un parche más nuevo de la misma banda, 10.0.4xx, y si hay varios usa el más nuevo. Si Windows Update pasa el SDK a un parche más nuevo, por ejemplo de 10.0.403 a 10.0.404, sigue sirviendo. Los otros SDK que haya instalados no molestan, pero no sirven: ni los de las bandas 10.0.1xx a 10.0.3xx ni el de .NET 11. Si no hay ninguno de la banda 10.0.4xx, `dotnet` no hace nada y dice `A compatible .NET SDK was not found` y `Requested SDK version: 10.0.403`.

Visual Studio 2026 instala su propio SDK, que cambia con cada actualización: la 18.12 trae el de .NET 11. Por eso el SDK de .NET 10 se instala aparte, y actualizar Visual Studio no lo toca.

## 3. El editor

Hace falta uno de los dos para mandar los pedidos del `.http`, que usan los pasos de prueba de cada fase y la preparación de la primera vez (`POST /indexar`, `POST /vectores`).

- **Visual Studio 2026** (la edición Community alcanza), con la carga de trabajo "ASP.NET y desarrollo web". Abre `RagBasico.slnx`, corre la API con F5 y manda los pedidos del `.http` sin extensiones: cada pedido tiene arriba un link "Send Request". Visual Studio 2022 no sirve: no puede usar el SDK de .NET 10.
- **VS Code**, con la extensión "REST Client" (`humao.rest-client`) para el `.http`, y la API corriendo con `dotnet run` en una consola.

Sin editor, la API corre igual con `dotnet run`, y los pedidos se mandan desde el navegador o desde PowerShell, en otra consola (la de `dotnet run` queda ocupada), con el puerto de la fase: `508N`, por ejemplo 5087 en la fase 7. Los `GET`, como `GET /salud`, se abren en el navegador: `http://127.0.0.1:5087/salud`. Los `POST` sin cuerpo, como `POST /indexar` o `POST /vectores`, se mandan así: la segunda línea muestra los totales (`cargados` y `borrados`, o `pendientes` y `fragmentosPorSegundo`), con el detalle cortado en los primeros cuatro, y la tercera, el detalle entero:

```powershell
$r = Invoke-RestMethod -Method Post http://127.0.0.1:5087/indexar
$r
$r.detalle
```

`POST /evaluar` devuelve otras partes: los avisos (si el examen se cortó, ahí dice por qué), la tabla por modo, las fallas y el detalle por pregunta:

```powershell
$r = Invoke-RestMethod -Method Post http://127.0.0.1:5087/evaluar
$r.avisos
$r.resultados | Format-Table
$r.fallas
$r.detalle
```

Una pregunta lleva JSON, y en Windows PowerShell 5.1 hay que decir que va en UTF-8, o las tildes llegan mal:

```powershell
Invoke-RestMethod -Method Post http://127.0.0.1:5087/preguntar -ContentType 'application/json; charset=utf-8' -Body '{"pregunta":"¿Qué requisitos hay para pedir el reintegro?"}'
```

Desde la fase 6, la página de la demo también pregunta, pero no manda los otros pedidos.

## 4. Ollama y los modelos

1. Bajá `OllamaSetup.exe` de https://ollama.com/download/windows y correlo. Pesa unos 1,9 GB, y instalado ocupa unos 3,4 GB más los modelos. No pide administrador: se instala para tu usuario, en `%LOCALAPPDATA%\Programs\Ollama`. O, en una consola: `winget install --id Ollama.Ollama -e`. Pide Windows 10 22H2 o posterior.
2. Al terminar abre una ventana que ofrece crear una cuenta: no hace falta para usar los modelos en tu PC ("No thanks, I'll use Ollama locally"). Después queda corriendo en segundo plano, con un ícono en la bandeja de Windows, y arranca solo cada vez que iniciás sesión.
3. Comprobá: `http://127.0.0.1:11434` en el navegador dice "Ollama is running".
4. Bajá los modelos, en una consola nueva:

   ```powershell
   ollama pull qwen3.5:4b
   ollama pull qwen3-embedding:0.6b
   ```

   Son unos 3,3 GB y 640 MB, y quedan en `%USERPROFILE%\.ollama\models`. El primero hace falta desde la fase 1; el segundo, desde la 5.
5. Comprobá: `ollama list` muestra los dos.

Si Ollama no está corriendo (`http://127.0.0.1:11434` no responde), abrilo desde el menú Inicio: "Ollama". Para cerrarlo: clic en su ícono de la bandeja, "Quit Ollama".

Con una placa de video, Ollama responde mucho más rápido. Con NVIDIA pide el driver 551.61 o posterior, y para las series GTX 900 y 1000, el 570 o posterior. Con un modelo cargado, `ollama ps` dice dónde corre, en la columna `PROCESSOR`: `100% GPU` es lo esperado; con `100% CPU` anda, pero cada respuesta tarda mucho más.

Ollama se actualiza solo: baja cada versión nueva y la instala cuando elegís "Restart to update" en su ícono, o la próxima vez que arranca con Windows. Si la semana de una demo no querés que cambie, apagá "Auto-download updates" en su "Settings".

## 5. SQL Server 2025 Express, con Full-Text

1. Bajá el instalador de https://www.microsoft.com/sql-server/sql-server-downloads: en "SQL Server 2025 Express", "Download now". Es `SQL2025-SSEI-Expr.exe`, de unos 4 MB, que baja el resto mientras instala.
2. Correlo y elegí la instalación personalizada, "Custom". La básica, "Basic", instala el motor con los valores por defecto, sin preguntar.
3. "Custom" pide una carpeta para bajar los archivos de instalación (sirve la que propone), los baja con "Install" y abre el "Centro de instalación de SQL Server" ("SQL Server Installation Center"). Ahí elegí "Instalación" ("Installation") y "Nueva instalación independiente de SQL Server o agregar características a una instalación existente" ("New SQL Server standalone installation or add features to an existing installation"). Aceptá la licencia y seguí con "Siguiente" ("Next").
4. En "Extensión de Azure para SQL Server" ("Azure Extension for SQL Server"), desmarcá la casilla, que viene marcada: si no, el instalador pide una cuenta de Azure y no deja seguir. El prototipo no usa Azure.
5. En "Selección de características" ("Feature Selection"), dejá marcado el motor ("Database Engine Services") y marcá Full-Text: "Extracciones de texto completo y semánticas de búsqueda" ("Full-Text and Semantic Extractions for Search"). Desde la 2025, Express trae el Full-Text: no hace falta otro paquete. Lo demás que venga marcado, como "AI Services and Language Extensions" o "SQL Server Replication", no hace falta.
6. En el resto, dejá lo que propone: la instancia con nombre `SQLEXPRESS`, la autenticación de Windows y tu usuario como administrador de SQL Server. En la configuración del servidor ("Server Configuration") podés poner el motor ("SQL Server Database Engine") en "Automatic", así arranca solo con Windows.

Si SQL Server ya estaba instalado sin Full-Text, se agrega sin reinstalar: volvé a correr `SQL2025-SSEI-Expr.exe` y elegí "Custom". En el "Centro de instalación de SQL Server", "Instalación" y "Nueva instalación independiente de SQL Server o agregar características a una instalación existente", como en el paso 3. En "Tipo de instalación" ("Installation Type"), "Agregar características a una instancia existente de SQL Server" ("Add features to an existing instance of SQL Server"), con `SQLEXPRESS`. Si aparece "Extensión de Azure para SQL Server", desmarcala también ahí. Y en "Selección de características", marcá Full-Text.

El servicio se llama `MSSQL$SQLEXPRESS`; en la lista de servicios de Windows, "SQL Server (SQLEXPRESS)". Para ver si está andando y cómo arranca:

```powershell
Get-Service 'MSSQL$SQLEXPRESS' | Select-Object Status, StartType
```

Si `StartType` dice `Manual`, hay que arrancarlo cada vez, como administrador: `Start-Service 'MSSQL$SQLEXPRESS'`. Para que arranque solo con Windows, una sola vez, como administrador: `Set-Service 'MSSQL$SQLEXPRESS' -StartupType Automatic`. Van con comillas simples: entre comillas dobles, PowerShell toma `$SQLEXPRESS` como una variable.

Express alcanza de sobra para el prototipo: hasta 50 GB por base, 1.410 MB de memoria para los datos y 4 núcleos. Trae el tipo `VECTOR` y `VECTOR_DISTANCE`, que usa la búsqueda por significado desde la fase 5.

## 6. sqlcmd y SSMS

1. Instalá sqlcmd, en una consola: `winget install --id Microsoft.Sqlcmd -e`. Sin winget, bajá `sqlcmd-amd64.msi` de https://github.com/microsoft/go-sqlcmd/releases/latest y correlo. Windows pide permiso de administrador. Queda en `C:\Program Files\SqlCmd\`.
2. Comprobá, en una consola nueva: `sqlcmd --version` muestra `Version: v1.10.0` o posterior.
3. Con el servicio de SQL Server andando, comprobá SQL Server y el Full-Text:

   ```powershell
   sqlcmd -S lpc:localhost\SQLEXPRESS -E -C -Q "SELECT @@VERSION; SELECT FULLTEXTSERVICEPROPERTY('IsFulltextInstalled')"
   ```

   Tiene que mostrar "Microsoft SQL Server 2025" con "Express Edition", y un `1`. Si da `0`, falta el Full-Text (punto 5).

`-E` es la autenticación de Windows, `-C` confía en el certificado que SQL Express se crea solo, y `lpc:` conecta por memoria compartida, la conexión local de SQL Server. Sin `lpc:`, este sqlcmd le pregunta primero por la instancia al servicio SQL Server Browser, y si ese servicio no está andando, lo espera hasta agotar el tiempo y falla aunque SQL Server esté andando. Desde la fase 7, `scripts\crear-base.ps1` le agrega `lpc:` por su cuenta. En las fases 2 a 6 no lo hace, así que pasale el servidor, parado en la carpeta de la fase:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1 -Servidor 'lpc:localhost\SQLEXPRESS'
```

SSMS es opcional: sirve para mirar la base y correr `sql\consultas-utiles.sql`. Hace falta la 21 o posterior, que entiende el tipo `VECTOR`; se baja de https://learn.microsoft.com/ssms/install/install. Para conectarte: servidor `localhost\SQLEXPRESS`, autenticación de Windows y "Trust server certificate" marcado.

## 7. La carpeta de los PDF

1. Juntá los PDF públicos que se van a cargar en una carpeta, por ejemplo `F:\RAG-BASICO-datos\documentos\`, al lado del repo. Tienen que tener texto seleccionable: los escaneados no se pueden leer, y la carga los informa como `SIN TEXTO`.
2. La API toma la carpeta de la variable de entorno de usuario `Rag__CarpetaDocumentos`, con la ruta completa. Se crea una sola vez:

   ```powershell
   [Environment]::SetEnvironmentVariable('Rag__CarpetaDocumentos', 'F:\RAG-BASICO-datos\documentos', 'User')
   ```

   Si tu carpeta está en otro lado, poné su ruta completa. El comando no controla que exista: si te equivocás, la API avisa al arrancar `No existe la carpeta de PDF`.
3. Cerrá y volvé a abrir Visual Studio, VS Code y las consolas: los que estaban abiertos no ven la variable nueva.
4. Comprobá, en una consola nueva: `$env:Rag__CarpetaDocumentos` muestra la carpeta.

## 8. Qdrant

1. Bajá `qdrant-x86_64-pc-windows-msvc.zip` (31 MB) de la página de la versión 1.20.2: https://github.com/qdrant/qdrant/releases/tag/v1.20.2, en "Assets". El link directo es https://github.com/qdrant/qdrant/releases/download/v1.20.2/qdrant-x86_64-pc-windows-msvc.zip.
2. Descomprimilo en la carpeta `RAG-BASICO-datos\qdrant\`, al lado del repo: con el repo en `F:\RAG-BASICO\`, en `F:\RAG-BASICO-datos\qdrant\`. `qdrant.exe` tiene que quedar directamente en esa carpeta, sin una subcarpeta en el medio, porque el script lo busca ahí. Con "Extraer todo" del Explorador, cambiá el destino: propone una carpeta con el nombre del zip. Desde PowerShell, parado en la carpeta del repo y con el zip en Descargas:

   ```powershell
   Expand-Archive -Path "$env:USERPROFILE\Downloads\qdrant-x86_64-pc-windows-msvc.zip" -DestinationPath ..\RAG-BASICO-datos\qdrant
   ```

3. Arrancalo en una consola aparte, parado en la carpeta del repo (el script usa git para encontrar la carpeta de Qdrant):

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File scripts\iniciar-qdrant.ps1
   ```

   Queda corriendo en esa consola hasta Ctrl+C. La primera vez crea las carpetas `storage` y `snapshots` al lado de `qdrant.exe`: ahí quedan los datos.
4. Comprobá: `http://127.0.0.1:6333` en el navegador muestra `"version":"1.20.2"`.

No hace falta instalarlo ni ser administrador, y no pide abrir el firewall: el script lo hace escuchar solo en `127.0.0.1`. El zip para Windows no trae la página de administración de Qdrant (`/dashboard` da 404): no se usa.

`qdrant.exe` necesita el Visual C++ Redistributable de Microsoft (`VCRUNTIME140.dll`), que no viene con Windows. Casi siempre ya está, porque lo instalan Visual Studio y muchos otros programas. Si al arrancar Qdrant, Windows avisa que no encuentra `VCRUNTIME140.dll`, instalalo de https://aka.ms/vc14/vc_redist.x64.exe (el link de la página oficial, https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist), o con `winget install --id Microsoft.VCRedist.2015+.x64 -e`.

## 9. Cada vez que vas a usar el proyecto

1. **SQL Server:** si su servicio está en inicio manual, arrancalo como administrador: `Start-Service 'MSSQL$SQLEXPRESS'`.
2. **Ollama:** tiene que estar su ícono en la bandeja. Si no, abrilo desde el menú Inicio.
3. **Qdrant**, desde la fase 7: en una consola aparte, parado en la carpeta del repo, `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\iniciar-qdrant.ps1`.
4. **La API:** `dotnet run` en la carpeta `RagBasico.Api`, o F5 en Visual Studio.
5. **Desde la fase 7, `GET /salud`** dice si anda todo: `http://127.0.0.1:5087/salud` en el navegador, con el puerto de la fase. Con la fase preparada, los cinco chequeos dan `ok`.

La primera vez en cada fase, desde la 2, hay que prepararla; las fases 0 y 1 no usan base. Antes de arrancar la API, su base: parado en la carpeta de esa fase, `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1` (en las fases 2 a 6, con `-Servidor 'lpc:localhost\SQLEXPRESS'`, como dice el punto 6). Desde la fase 8, sin la base la API no arranca. Después, con la API andando, los pedidos de la primera vez: están, en orden, en [worktrees.md](worktrees.md#la-primera-vez-en-cada-carpeta).

Al terminar: Ctrl+C en la consola de la API y en la de Qdrant. SQL Server y Ollama pueden quedar andando.

## 10. Si algo no anda

| Lo que pasa | Qué hacer |
|---|---|
| `git`, `dotnet`, `sqlcmd` u `ollama` no se reconoce como comando | Abrí una consola nueva: la que estaba abierta no ve lo que se instaló después. En la terminal de Visual Studio o de VS Code no alcanza con abrir otra: cerrá el programa y volvé a abrirlo |
| `winget` no se reconoce como comando | En una PC recién instalada puede tardar unos minutos en aparecer. Mientras tanto, usá el instalador de la página que dice cada punto |
| PowerShell dice que el script no se puede cargar porque la ejecución de scripts está deshabilitada en este sistema (`running scripts is disabled on this system`) | Windows no deja correr un `.ps1` suelto: correlo con `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\<script>.ps1`, con los parámetros al final |
| Un script dice `ERROR: esta carpeta no está adentro de un repo de git`, o `powershell` dice `The argument 'scripts\...' to the -File parameter does not exist` | La consola no está parada en la carpeta del repo: `cd F:\RAG-BASICO`, o la carpeta donde lo bajaste, y correlo de nuevo. Si bajaste el repo como ZIP, le falta la carpeta `.git`: bajalo con `git clone` (punto 1) |
| `dotnet` dice `A compatible .NET SDK was not found` | Falta el SDK de la banda 10.0.4xx (punto 2) |
| `Start-Service` dice `Cannot open MSSQL$SQLEXPRESS service` | La consola no está abierta como administrador: abrila como dice el principio de esta guía |
| `scripts\crear-base.ps1` dice `ERROR: falta sqlcmd` | Instalá sqlcmd (punto 6) y abrí una consola nueva |
| `scripts\crear-base.ps1` dice `The system cannot find the file specified` | El servicio de SQL Server está detenido. Como administrador: `Start-Service 'MSSQL$SQLEXPRESS'` |
| En las fases 2 a 6, `scripts\crear-base.ps1` tarda unos 45 segundos y dice `Timed out waiting for pipe` | Pasale el servidor con `lpc:`: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1 -Servidor 'lpc:localhost\SQLEXPRESS'` (punto 6). Si sigue fallando, el servicio de SQL Server está detenido |
| `scripts\crear-base.ps1` falla en `01_base.sql` con el error 7609, "Full-Text Search is not installed" | SQL Server se instaló sin Full-Text: agregalo (punto 5) |
| El instalador de SQL Server termina con un error en "Database Engine Services": `Wait on the Database Engine recovery handle failed` | Puede ser el disco: con algunos NVMe, Windows 11 informa sectores de más de 4 KB, y ninguna versión de SQL Server arranca así. Para verlo, en una consola como administrador: `fsutil fsinfo sectorinfo C:`. Si `PhysicalBytesPerSectorForAtomicity` o `PhysicalBytesPerSectorForPerformance` dan más de 4096, seguí el arreglo de Microsoft, una clave del registro y reiniciar: https://learn.microsoft.com/troubleshoot/sql/database-engine/database-file-operations/troubleshoot-os-4kb-disk-sector-size. Después, desinstalá lo que quedó de SQL Server y volvé a instalarlo (punto 5) |
| `GET /salud` da `error` en SQL Server: `No se pudo leer la base` | Si dice `error: 26`, el servicio de SQL Server está detenido, aunque el mensaje hable del nombre de la instancia y de las conexiones remotas: arrancalo (punto 9). Si dice `Cannot open database` o `Invalid object name`, falta la base de la fase o sus tablas: creala con `scripts\crear-base.ps1` (punto 9) |
| La API avisa al arrancar `No existe la carpeta de PDF`, o `POST /indexar` dice `ERROR: no existe la carpeta de PDF` | El mensaje muestra entre comillas la carpeta que buscó. Si está vacía (`''`), la API no ve la variable `Rag__CarpetaDocumentos`: creala (punto 7). Si es otra carpeta o una que no existe, volvé a crear la variable con la ruta completa de tu carpeta (punto 7). En los dos casos, después cerrá y volvé a abrir Visual Studio, VS Code y las consolas |
| `GET /salud` dice que falta un modelo | Corré el `ollama pull` que muestra |
| `GET /salud` dice `Ollama no responde` | Abrí Ollama desde el menú Inicio |
| `scripts\iniciar-qdrant.ps1` dice `ERROR: no está ...\qdrant.exe` | `qdrant.exe` quedó en una subcarpeta o en otra carpeta: movelo a `RAG-BASICO-datos\qdrant\` (punto 8) |
| `qdrant.exe` no arranca porque falta `VCRUNTIME140.dll` | Instalá el Visual C++ Redistributable (punto 8) |
| Windows avisa que el Control inteligente de aplicaciones (Smart App Control) bloqueó un programa, o `dotnet run`, F5 o `scripts\iniciar-qdrant.ps1` fallan con `An Application Control policy has blocked this file` | Ese control bloquea los programas sin firma que Microsoft no conoce, y no admite excepciones para uno solo: `qdrant.exe` viene sin firma, y la API, que se compila en tu PC, también. Para usar el proyecto hay que apagarlo: Seguridad de Windows, "Control de aplicaciones y navegador" ("App & browser control"), "Configuración de Control Inteligente de Aplicaciones" ("Smart App Control settings"), "Desactivado" ("Off"). Ojo: en algunas PC, por ejemplo sin las últimas actualizaciones de Windows, una vez apagado no se puede volver a prender sin restablecer o reinstalar Windows |
| La híbrida avisa que usó los vectores de SQL, o `GET /salud` da `aviso` en Qdrant | Leé el aviso: dice qué pasó. Si Qdrant no respondió (`Qdrant no respondió`, o `No responde` en `GET /salud`), arrancalo con `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\iniciar-qdrant.ps1`. Si respondió, hacé lo que dice el detalle de `GET /salud`: `POST /qdrant/reconstruir` si no existe la colección de la fase, o `POST /vectores` si está vacía |
| La API no arranca: `address already in use` | Ya hay una API de esta misma fase escuchando en su puerto (cada fase tiene el suyo, `508N`): en otra consola, en Visual Studio o en la otra carpeta de la fase (la principal y la de `RAG-BASICO-fases`). Frenala con Ctrl+C, o Shift+F5 en Visual Studio |
| Desde la fase 8, la API no arranca y el log dice `La API no arranca.` | Lo que sigue dice qué falta. `SQL Server` con `error: 26`: el servicio de SQL Server está detenido; arrancalo (punto 9). `SQL Server` con `Cannot open database`: falta la base de la fase; creala con `scripts\crear-base.ps1` (punto 9). `Modelos de Ollama`: abrí Ollama desde el menú Inicio, o corré el `ollama pull` que muestra. Para mirar la página sin todo andando, en esa consola: `$env:Rag__ChequearAlArrancar = 'false'` y otra vez `dotnet run` |
| Las respuestas tardan mucho | Ollama está usando el procesador: mirá la columna `PROCESSOR` de `ollama ps` (punto 4) |
