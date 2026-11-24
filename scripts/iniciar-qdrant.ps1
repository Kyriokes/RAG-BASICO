<#
.SYNOPSIS
Arranca Qdrant escuchando solo en esta PC, con su carpeta de datos fija y sin telemetría.

.DESCRIPTION
Busca qdrant.exe en la carpeta de Qdrant (por defecto RAG-BASICO-datos\qdrant, al lado de la carpeta principal del repo)
y lo arranca con estas variables de entorno, que valen solo para este proceso:
- QDRANT__SERVICE__HOST=127.0.0.1: acepta conexiones solo de esta PC. Sin esto, Qdrant escucha en toda la red y sin clave.
- QDRANT__STORAGE__STORAGE_PATH y QDRANT__STORAGE__SNAPSHOTS_PATH: las carpetas de datos, con la ruta completa. Sin esto,
  Qdrant las crea relativas a la carpeta desde donde se lo arranca.
- QDRANT__TELEMETRY_DISABLED=true: no manda estadísticas de uso por internet.
Qdrant queda corriendo en esta consola y se frena con Ctrl+C. No hace falta abrirle el firewall.

.PARAMETER Carpeta
La carpeta de qdrant.exe. Los datos quedan en sus subcarpetas storage y snapshots.

.EXAMPLE
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\iniciar-qdrant.ps1
#>
param(
    [string]$Carpeta
)

# Git escribe en UTF-8. Para que PowerShell 5.1 lea bien rutas con tildes, la consola pasa a UTF-8
# mientras corre el script, y al final vuelve a como estaba.
$codificacionAnterior = [Console]::OutputEncoding
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
try {
    if (-not $Carpeta) {
        # La carpeta de datos está al lado de la carpeta principal del repo, también si se corre desde otra carpeta de trabajo de git:
        # --git-common-dir es la carpeta .git de la principal en los dos casos.
        $comun = & git rev-parse --path-format=absolute --git-common-dir
        if ($LASTEXITCODE -ne 0) {
            Write-Host 'ERROR: esta carpeta no está adentro de un repo de git. Pasá la carpeta de Qdrant con -Carpeta.'
            exit 1
        }
        $principal = Split-Path -Parent ([System.IO.Path]::GetFullPath($comun))
        $Carpeta = Join-Path (Split-Path -Parent $principal) 'RAG-BASICO-datos\qdrant'
    }
    $Carpeta = [System.IO.Path]::GetFullPath($Carpeta)
    $exe = Join-Path $Carpeta 'qdrant.exe'
    if (-not (Test-Path -LiteralPath $exe)) {
        Write-Host "ERROR: no está $exe."
        Write-Host 'Bajá qdrant-x86_64-pc-windows-msvc.zip de la versión 1.20.2, de https://github.com/qdrant/qdrant/releases/tag/v1.20.2,'
        Write-Host "y descomprimilo en $Carpeta."
        exit 1
    }

    $env:QDRANT__SERVICE__HOST = '127.0.0.1'
    $env:QDRANT__SERVICE__HTTP_PORT = '6333'
    $env:QDRANT__SERVICE__GRPC_PORT = '6334'
    $env:QDRANT__STORAGE__STORAGE_PATH = Join-Path $Carpeta 'storage'
    $env:QDRANT__STORAGE__SNAPSHOTS_PATH = Join-Path $Carpeta 'snapshots'
    $env:QDRANT__TELEMETRY_DISABLED = 'true'

    Write-Host "Qdrant escucha en http://127.0.0.1:6333, solo en esta PC. Datos en $($env:QDRANT__STORAGE__STORAGE_PATH)."
    Write-Host 'Se frena con Ctrl+C.'
    # Qdrant busca su configuración en la carpeta config de donde se lo arranca: se lo arranca desde su propia carpeta.
    Push-Location -LiteralPath $Carpeta
    try {
        & $exe
        $salida = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
    exit $salida
}
finally {
    try { [Console]::OutputEncoding = $codificacionAnterior } catch { }
}
