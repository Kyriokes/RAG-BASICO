<#
.SYNOPSIS
Crea una base de origen de prueba: una tabla con PDF guardados en binario, como la base vieja de la oficina.

.DESCRIPTION
Sirve para probar "Importar de la base de origen" en casa, sin la base de la oficina.
Crea la base RagBasico_Origen en el mismo servidor de Conexiones:SqlTextos, con la tabla
dbo.TABLA_DE_PDF (Id, Nombre, Contenido), y guarda adentro cada PDF de una carpeta.
Los nombres de la tabla y de las columnas son los mismos de ejemplo de Conexiones:ConsultaLista y
Conexiones:ConsultaContenido, así que las dos consultas andan sin tocarlas.
Se puede correr de nuevo: vacía la tabla antes de cargar.
Al final muestra el archivo RagBasico.Api\conexiones.json que hay que crear para que la API la use.
Usa la autenticación de Windows y no necesita sqlcmd.

.PARAMETER Carpeta
La carpeta con los PDF. Por defecto, RAG-BASICO-datos\documentos, al lado del repo.

.PARAMETER Base
El nombre de la base de origen de prueba. Por defecto, RagBasico_Origen.

.EXAMPLE
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-origen-de-prueba.ps1
#>
param(
    [string]$Carpeta,
    [string]$Base = 'RagBasico_Origen'
)

# Para que PowerShell 5.1 muestre bien las tildes, la consola pasa a UTF-8 mientras corre el script.
$codificacionAnterior = [Console]::OutputEncoding
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
try {
    $raiz = Split-Path -Parent $PSScriptRoot
    if (-not $Carpeta) { $Carpeta = Join-Path (Split-Path -Parent $raiz) 'RAG-BASICO-datos\documentos' }
    if (-not (Test-Path -LiteralPath $Carpeta)) {
        Write-Host "ERROR: no existe la carpeta $Carpeta. Pasá la carpeta de los PDF con -Carpeta."
        exit 1
    }
    if ($Base -notmatch '^[A-Za-z0-9_]+$') {
        Write-Host 'ERROR: el nombre de la base solo puede tener letras, números y guion bajo.'
        exit 1
    }
    $archivos = @(Get-ChildItem -LiteralPath $Carpeta -Filter '*.pdf' | Sort-Object Name)
    if ($archivos.Count -eq 0) {
        Write-Host "ERROR: no hay ningún PDF en $Carpeta."
        exit 1
    }

    # El servidor sale de la cadena de la base del RAG: la base de prueba va en el mismo servidor.
    $config = [System.IO.File]::ReadAllText((Join-Path $raiz 'RagBasico.Api\appsettings.json'), [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    $cadena = New-Object System.Data.SqlClient.SqlConnectionStringBuilder ([string]$config.Conexiones.SqlTextos)
    $cadena['Initial Catalog'] = 'master'

    # Corre un comando de SQL sin resultado. Los parámetros van en un diccionario nombre -> valor.
    function Correr-Sql([System.Data.SqlClient.SqlConnection]$conexion, [string]$sql, [hashtable]$parametros) {
        $comando = $conexion.CreateCommand()
        $comando.CommandText = $sql
        $comando.CommandTimeout = 300
        if ($parametros) {
            foreach ($nombre in $parametros.Keys) {
                [void]$comando.Parameters.AddWithValue($nombre, $parametros[$nombre])
            }
        }
        [void]$comando.ExecuteNonQuery()
    }

    $conexion = New-Object System.Data.SqlClient.SqlConnection ($cadena.ConnectionString)
    try {
        $conexion.Open()
    }
    catch {
        Write-Host "ERROR: no se pudo conectar a $($cadena.DataSource): $($_.Exception.Message)"
        Write-Host 'Revisá que esté andando el servicio de SQL Server. Como administrador: Start-Service ''MSSQL$SQLEXPRESS'''
        exit 1
    }
    try {
        Correr-Sql $conexion "IF DB_ID(N'$Base') IS NULL CREATE DATABASE [$Base];" $null
        $conexion.ChangeDatabase($Base)
        Correr-Sql $conexion @"
IF OBJECT_ID(N'dbo.TABLA_DE_PDF') IS NULL
    CREATE TABLE dbo.TABLA_DE_PDF (
        Id         INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Nombre     NVARCHAR(260) NOT NULL,
        Contenido  VARBINARY(MAX) NOT NULL
    );
TRUNCATE TABLE dbo.TABLA_DE_PDF;
"@ $null

        foreach ($archivo in $archivos) {
            $bytes = [System.IO.File]::ReadAllBytes($archivo.FullName)
            Correr-Sql $conexion 'INSERT INTO dbo.TABLA_DE_PDF (Nombre, Contenido) VALUES (@nombre, @contenido);' @{ '@nombre' = $archivo.Name; '@contenido' = $bytes }
            Write-Host "Guardado: $($archivo.Name) ($([math]::Round($bytes.Length / 1MB, 1)) MB)"
        }
    }
    finally {
        $conexion.Close()
    }

    $cadena['Initial Catalog'] = $Base
    # En JSON la barra invertida se escribe doble.
    $paraJson = $cadena.ConnectionString.Replace('\', '\\')
    Write-Host ''
    Write-Host "Listo: $($archivos.Count) PDF en la base $Base, tabla dbo.TABLA_DE_PDF."
    Write-Host 'Para que la API la use, creá el archivo RagBasico.Api\conexiones.json con esto, y volvé a arrancar la API:'
    Write-Host ''
    Write-Host '{'
    Write-Host '  "Conexiones": {'
    Write-Host "    `"SqlOrigen`": `"$paraJson`""
    Write-Host '  }'
    Write-Host '}'
    exit 0
}
finally {
    try { [Console]::OutputEncoding = $codificacionAnterior } catch { }
}
