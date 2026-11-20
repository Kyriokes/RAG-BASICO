# Instalación

Qué hay que instalar en Windows para correr esta versión en una sola PC, cómo comprobar que cada pieza anda y qué hacer si algo falla. La columna "Desde" dice desde qué fase hace falta cada cosa, para quien abra una branch anterior.

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
| SQL Server 2025 Express, con Full-Text | | fase-2-rag-minimo | La base: los documentos, los textos y el índice de palabras. Desde la fase 9 no guarda vectores, así que sirve también una versión anterior con Full-Text | Una consulta con `sqlcmd` (punto 6) |
| sqlcmd | 1.10 o posterior | fase-2-rag-minimo | Lo usa `scripts\crear-base.ps1` para crear la base | `sqlcmd --version` |
| SSMS | 21 o posterior | fase-2-rag-minimo | Mirar la base y correr `sql\consultas-utiles.sql`. Opcional | Se conecta a `localhost\SQLEXPRESS` |
| El modelo `qwen3-embedding:0.6b` | | fase-5-vectores | Calcular los vectores | `ollama list` |
| Qdrant | 1.20.2 | fase-7-qdrant | El índice de vectores | `http://127.0.0.1:6333` dice `"version":"1.20.2"` |

Para correr una fase vieja alcanza con lo de las filas hasta esa fase. Las fases 2 a 8 leen los PDF de una carpeta y tienen su propia guía, en `docs\instalacion.md` de esas branches. Desde la fase 9 los PDF se arrastran a la página.

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

Para usar el proyecto alcanza con el navegador: las dos páginas hacen todo. El editor sirve para leer el código y para mandar los pedidos de `RagBasico.Api\RagBasico.Api.http` uno por uno.

- **Visual Studio 2026** (la edición Community alcanza), con la carga de trabajo "ASP.NET y desarrollo web". Abre `RagBasico.slnx`, corre la API con F5 y manda los pedidos del `.http` sin extensiones: cada pedido tiene arriba un link "Send Request". Visual Studio 2022 no sirve: no puede usar el SDK de .NET 10.
- **VS Code**, con la extensión "REST Client" (`humao.rest-client`) para el `.http`, y la API corriendo con `iniciar.cmd`.

Sin editor, los `GET` se abren en el navegador, por ejemplo `http://127.0.0.1:5089/salud` o `http://127.0.0.1:5089/documentos`. Una pregunta se manda desde PowerShell, en otra consola. Lleva JSON, y en Windows PowerShell 5.1 hay que decir que va en UTF-8, o las tildes llegan mal:

```powershell
Invoke-RestMethod -Method Post http://127.0.0.1:5089/preguntar -ContentType 'application/json; charset=utf-8' -Body '{"pregunta":"¿Qué requisitos hay para pedir el reintegro?"}'
```

## 4. Ollama y los modelos

1. Bajá `OllamaSetup.exe` de https://ollama.com/download/windows y correlo. Pesa unos 1,9 GB, y instalado ocupa unos 3,4 GB más los modelos. No pide administrador: se instala para tu usuario, en `%LOCALAPPDATA%\Programs\Ollama`. O, en una consola: `winget install --id Ollama.Ollama -e`. Pide Windows 10 22H2 o posterior.
2. Al terminar abre una ventana que ofrece crear una cuenta: no hace falta para usar los modelos en tu PC ("No thanks, I'll use Ollama locally"). Después queda corriendo en segundo plano, con un ícono en la bandeja de Windows, y arranca solo cada vez que iniciás sesión.
3. Comprobá: `http://127.0.0.1:11434` en el navegador dice "Ollama is running".
4. Bajá los modelos, en una consola nueva:

   ```powershell
   ollama pull qwen3.5:4b
   ollama pull qwen3-embedding:0.6b
   ```

   Son unos 3,3 GB y 640 MB, y quedan en `%USERPROFILE%\.ollama\models`.
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

Express alcanza para el prototipo: hasta 50 GB por base (10 GB en las versiones anteriores a la 2025), 1.410 MB de memoria para los datos y 4 núcleos. Los PDF que se arrastran a la página se guardan adentro de la base: con muchos, hay que mirar ese tope.

## 6. sqlcmd y SSMS

1. Instalá sqlcmd, en una consola: `winget install --id Microsoft.Sqlcmd -e`. Sin winget, bajá `sqlcmd-amd64.msi` de https://github.com/microsoft/go-sqlcmd/releases/latest y correlo. Windows pide permiso de administrador. Queda en `C:\Program Files\SqlCmd\`.
2. Comprobá, en una consola nueva: `sqlcmd --version` muestra `Version: v1.10.0` o posterior.
3. Con el servicio de SQL Server andando, comprobá SQL Server y el Full-Text:

   ```powershell
   sqlcmd -S lpc:localhost\SQLEXPRESS -E -C -Q "SELECT @@VERSION; SELECT FULLTEXTSERVICEPROPERTY('IsFulltextInstalled')"
   ```

   Tiene que mostrar "Microsoft SQL Server 2025" con "Express Edition", y un `1`. Si da `0`, falta el Full-Text (punto 5).

`-E` es la autenticación de Windows, `-C` confía en el certificado que SQL Express se crea solo, y `lpc:` conecta por memoria compartida, la conexión local de SQL Server. Sin `lpc:`, este sqlcmd le pregunta primero por la instancia al servicio SQL Server Browser, y si ese servicio no está andando, lo espera hasta agotar el tiempo y falla aunque SQL Server esté andando. `scripts\crear-base.ps1` le agrega `lpc:` por su cuenta.

SSMS es opcional: sirve para mirar la base y correr `sql\consultas-utiles.sql`. Se baja de https://learn.microsoft.com/ssms/install/install. Para conectarte: servidor `localhost\SQLEXPRESS`, autenticación de Windows y "Trust server certificate" marcado.

## 7. La base del RAG

Con el servicio de SQL Server andando, parado en la carpeta del repo, una sola vez:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1
```

Crea la base `RagBasico_F9` (la de `Conexiones:SqlTextos`, en `RagBasico.Api\appsettings.json`) con sus tres tablas y el índice Full-Text. Se puede correr de nuevo sin romper nada.

Los PDF no van en ninguna carpeta especial: se arrastran a la página "Documentos" y quedan guardados en la base. Tienen que tener texto seleccionable: los escaneados no se pueden leer, y quedan como `sin texto`.

## 8. Qdrant

1. Bajá `qdrant-x86_64-pc-windows-msvc.zip` (31 MB) de la página de la versión 1.20.2: https://github.com/qdrant/qdrant/releases/tag/v1.20.2, en "Assets". El link directo es https://github.com/qdrant/qdrant/releases/download/v1.20.2/qdrant-x86_64-pc-windows-msvc.zip.
2. Descomprimilo en la carpeta `RAG-BASICO-datos\qdrant\`, al lado del repo: con el repo en `F:\RAG-BASICO\`, en `F:\RAG-BASICO-datos\qdrant\`. `qdrant.exe` tiene que quedar directamente en esa carpeta, sin una subcarpeta en el medio, porque el script lo busca ahí. Con "Extraer todo" del Explorador, cambiá el destino: propone una carpeta con el nombre del zip. Desde PowerShell, parado en la carpeta del repo y con el zip en Descargas:

   ```powershell
   Expand-Archive -Path "$env:USERPROFILE\Downloads\qdrant-x86_64-pc-windows-msvc.zip" -DestinationPath ..\RAG-BASICO-datos\qdrant
   ```

3. No hace falta arrancarlo a mano: lo arranca `iniciar.cmd` (punto 9), en otra ventana, minimizada. La primera vez crea las carpetas `storage` y `snapshots` al lado de `qdrant.exe`: ahí quedan los datos. Para arrancarlo solo, y ver sus mensajes, parado en la carpeta del repo:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File scripts\iniciar-qdrant.ps1
   ```

4. Comprobá: `http://127.0.0.1:6333` en el navegador muestra `"version":"1.20.2"`.

No hace falta instalarlo ni ser administrador, y no pide abrir el firewall: el script lo hace escuchar solo en `127.0.0.1`. El zip para Windows no trae la página de administración de Qdrant (`/dashboard` da 404): no se usa.

`qdrant.exe` necesita el Visual C++ Redistributable de Microsoft (`VCRUNTIME140.dll`), que no viene con Windows. Casi siempre ya está, porque lo instalan Visual Studio y muchos otros programas. Si al arrancar Qdrant, Windows avisa que no encuentra `VCRUNTIME140.dll`, instalalo de https://aka.ms/vc14/vc_redist.x64.exe (el link de la página oficial, https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist), o con `winget install --id Microsoft.VCRedist.2015+.x64 -e`.

## 9. Cada vez que vas a usar el proyecto

1. **SQL Server:** si su servicio está en inicio manual, arrancalo como administrador: `Start-Service 'MSSQL$SQLEXPRESS'`.
2. **Ollama:** tiene que estar su ícono en la bandeja. Si no, abrilo desde el menú Inicio.
3. **Doble clic en `iniciar.cmd`**, en la carpeta del repo. Arranca Qdrant si no está andando, arranca la API y abre el navegador en `http://127.0.0.1:5089/`.
4. **Mirá la franja de arriba de la página:** un punto por pieza. Con todo en verde, anda todo. Un punto amarillo o rojo trae debajo qué pasa y qué hacer.

La primera vez, antes del paso 3, hay que crear la base (punto 7). Después, en la página "Documentos", se arrastran los PDF.

Al terminar: Ctrl+C en la ventana de `iniciar.cmd`, y cerrar la ventana de Qdrant. SQL Server y Ollama pueden quedar andando. Si quedaban documentos pendientes, la próxima vez siguen solos.

## 10. Si algo no anda

| Lo que pasa | Qué hacer |
|---|---|
| `git`, `dotnet`, `sqlcmd` u `ollama` no se reconoce como comando | Abrí una consola nueva: la que estaba abierta no ve lo que se instaló después. En la terminal de Visual Studio o de VS Code no alcanza con abrir otra: cerrá el programa y volvé a abrirlo |
| `winget` no se reconoce como comando | En una PC recién instalada puede tardar unos minutos en aparecer. Mientras tanto, usá el instalador de la página que dice cada punto |
| PowerShell dice que el script no se puede cargar porque la ejecución de scripts está deshabilitada en este sistema (`running scripts is disabled on this system`) | Windows no deja correr un `.ps1` suelto: correlo con `powershell -NoProfile -ExecutionPolicy Bypass -File scripts\<script>.ps1`, con los parámetros al final |
| Un script dice `ERROR: esta carpeta no está adentro de un repo de git`, o `powershell` dice `The argument 'scripts\...' to the -File parameter does not exist` | La consola no está parada en la carpeta del repo: `cd F:\RAG-BASICO`, o la carpeta donde lo bajaste, y correlo de nuevo. Si bajaste el repo como ZIP, le falta la carpeta `.git`: bajalo con `git clone` (punto 1). `iniciar.cmd` se puede abrir con doble clic desde cualquier lado |
| `dotnet` dice `A compatible .NET SDK was not found` | Falta el SDK de la banda 10.0.4xx (punto 2) |
| `Start-Service` dice `Cannot open MSSQL$SQLEXPRESS service` | La consola no está abierta como administrador: abrila como dice el principio de esta guía |
| `scripts\crear-base.ps1` dice `ERROR: falta sqlcmd` | Instalá sqlcmd (punto 6) y abrí una consola nueva |
| `scripts\crear-base.ps1` dice `The system cannot find the file specified` | El servicio de SQL Server está detenido. Como administrador: `Start-Service 'MSSQL$SQLEXPRESS'` |
| `scripts\crear-base.ps1` falla en `base.sql` con el error 7609, "Full-Text Search is not installed" | SQL Server se instaló sin Full-Text: agregalo (punto 5) |
| El instalador de SQL Server termina con un error en "Database Engine Services": `Wait on the Database Engine recovery handle failed` | Puede ser el disco: con algunos NVMe, Windows 11 informa sectores de más de 4 KB, y ninguna versión de SQL Server arranca así. Para verlo, en una consola como administrador: `fsutil fsinfo sectorinfo C:`. Si `PhysicalBytesPerSectorForAtomicity` o `PhysicalBytesPerSectorForPerformance` dan más de 4096, seguí el arreglo de Microsoft, una clave del registro y reiniciar: https://learn.microsoft.com/troubleshoot/sql/database-engine/database-file-operations/troubleshoot-os-4kb-disk-sector-size. Después, desinstalá lo que quedó de SQL Server y volvé a instalarlo (punto 5) |
| La franja muestra SQL Server en rojo | El texto de abajo dice qué pasa. `No se pudo conectar`: el servicio de SQL Server está detenido, aunque el mensaje de SQL hable del nombre de la instancia y de las conexiones remotas (`error: 26`): arrancalo (punto 9). `No se pudo abrir la base` o `faltan las tablas`: creala con `scripts\crear-base.ps1` (punto 7) |
| Un PDF queda en `error` | La lista de "Documentos" muestra el motivo debajo del nombre: no es un PDF, está roto o tiene clave. Arreglado el motivo, "Reintentar los que fallaron" lo vuelve a procesar |
| Un PDF queda en `sin texto` | Es un escaneo: no tiene texto seleccionable. No se puede cargar |
| Los documentos quedan en `pendiente` y no avanzan | La consola de la API dice `La carga de documentos está frenada` y el motivo: un servidor no responde. La franja muestra cuál. Cuando vuelve, sigue sola. Un PDF muy grande también tarda: se procesa a unos 6 a 13 fragmentos por segundo |
| "Importar de la base de origen" está apagado | Falta `Conexiones:SqlOrigen`. Para probar: `scripts\crear-origen-de-prueba.ps1` arma una base de prueba y dice qué poner en `RagBasico.Api\conexiones.json`. Después hay que volver a arrancar la API |
| La franja muestra "SQL de origen" en amarillo | No se pudo leer la base de origen: el texto trae el mensaje de SQL Server. Revisá `Conexiones:SqlOrigen` y que `Conexiones:ConsultaLista` tenga bien los nombres de la tabla y de las columnas |
| La franja dice que los puntos de Qdrant y los fragmentos de la base no coinciden | Se recreó la base, o se borró la colección. En "Documentos", "Reprocesar todo" |
| La página dice que la subida es demasiado grande | Pasa de `Rag:MegasPorSubida` (200 MB): subí menos archivos por vez |
| La franja dice que falta un modelo | Corré el `ollama pull` que muestra |
| La franja dice `Ollama no responde` | Abrí Ollama desde el menú Inicio |
| `scripts\iniciar-qdrant.ps1` dice `ERROR: no está ...\qdrant.exe` | `qdrant.exe` quedó en una subcarpeta o en otra carpeta: movelo a `RAG-BASICO-datos\qdrant\` (punto 8) |
| `qdrant.exe` no arranca porque falta `VCRUNTIME140.dll` | Instalá el Visual C++ Redistributable (punto 8) |
| Windows avisa que el Control inteligente de aplicaciones (Smart App Control) bloqueó un programa, o `iniciar.cmd`, F5 o `scripts\iniciar-qdrant.ps1` fallan con `An Application Control policy has blocked this file` | Ese control bloquea los programas sin firma que Microsoft no conoce, y no admite excepciones para uno solo: `qdrant.exe` viene sin firma, y la API, que se compila en tu PC, también. Para usar el proyecto hay que apagarlo: Seguridad de Windows, "Control de aplicaciones y navegador" ("App & browser control"), "Configuración de Control Inteligente de Aplicaciones" ("Smart App Control settings"), "Desactivado" ("Off"). Ojo: en algunas PC, por ejemplo sin las últimas actualizaciones de Windows, una vez apagado no se puede volver a prender sin restablecer o reinstalar Windows |
| La franja muestra Qdrant en rojo, o una pregunta da el error `Qdrant no responde` | Qdrant no está andando. Frená la API con Ctrl+C y volvé a abrir `iniciar.cmd`, que lo arranca. Si no arranca, corré `scripts\iniciar-qdrant.ps1` a mano para ver por qué (punto 8). Mientras tanto, la búsqueda "Solo palabras" sigue andando |
| La API no arranca: `address already in use` | Ya hay una API escuchando en el puerto 5089: en otra ventana de `iniciar.cmd` o en Visual Studio. Frenala con Ctrl+C, o Shift+F5 en Visual Studio |
| Las respuestas tardan mucho | Ollama está usando el procesador: mirá la columna `PROCESSOR` de `ollama ps` (punto 4) |
