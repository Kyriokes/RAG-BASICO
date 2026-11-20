<#
.SYNOPSIS
Arranca todo lo que corre en esta PC: Qdrant, si hace falta, y la API.

.DESCRIPTION
Lo corre iniciar.cmd, el doble clic de la raíz del repo.
1. Lee la dirección de Qdrant de Conexiones:Qdrant, en RagBasico.Api\appsettings.json. Si existe
   RagBasico.Api\conexiones.json y trae esa línea, vale la de conexiones.json, igual que en la API.
2. Si Qdrant es de esta PC (127.0.0.1 o localhost) y no responde, lo arranca en otra ventana, minimizada,
   con scripts\iniciar-qdrant.ps1, y lo espera hasta 30 segundos.
   Si Qdrant está en otra máquina, no arranca nada: ahí lo tiene que levantar quien administra ese servidor.
3. Corre la API con dotnet run, que abre el navegador. Queda en esta ventana hasta Ctrl+C.
SQL Server y Ollama no se arrancan desde acá: SQL Server es un servicio de Windows y Ollama se abre solo
al iniciar sesión. Si alguno falta, la página lo muestra en rojo, arriba, con lo que hay que hacer.
Si algo falla antes de arrancar la API, como un error de JSON en conexiones.json, muestra el error y sale con 1.

.EXAMPLE
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\iniciar.ps1
#>
param()

# Cualquier error corta el script y cae en el catch del final, que lo muestra y sale con 1.
# Sin esto, PowerShell 5.1 sale con 0 y iniciar.cmd cierra la ventana antes de poder leer el error.
$ErrorActionPreference = 'Stop'

# Para que PowerShell 5.1 muestre bien las tildes, la consola pasa a UTF-8 mientras corre el script.
$codificacionAnterior = [Console]::OutputEncoding
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
try {
    $raiz = Split-Path -Parent $PSScriptRoot
    $api = Join-Path $raiz 'RagBasico.Api'

    # Lee Conexiones:Qdrant de un archivo JSON. Devuelve vacío si el archivo no existe o no trae esa línea.
    # Si el archivo tiene un error de JSON, el error dice cuál archivo es.
    function Leer-Qdrant([string]$ruta) {
        if (-not (Test-Path -LiteralPath $ruta)) { return '' }
        try {
            $json = [System.IO.File]::ReadAllText($ruta, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
        }
        catch {
            throw "$ruta tiene un error de JSON (una coma de más, una comilla sin cerrar o una barra invertida sin duplicar): $($_.Exception.Message)"
        }
        if ($json.Conexiones -and $json.Conexiones.Qdrant) { return [string]$json.Conexiones.Qdrant }
        return ''
    }

    # Dice si Qdrant contesta en una dirección.
    function Probar-Qdrant([string]$direccion) {
        try {
            Invoke-WebRequest -Uri $direccion -UseBasicParsing -TimeoutSec 2 | Out-Null
            return $true
        }
        catch {
            return $false
        }
    }

    $direccion = Leer-Qdrant (Join-Path $api 'appsettings.json')
    $deOficina = Leer-Qdrant (Join-Path $api 'conexiones.json')
    if ($deOficina) { $direccion = $deOficina }
    if (-not $direccion) { $direccion = 'http://127.0.0.1:6333' }

    $maquina = ([Uri]$direccion).Host
    if ($maquina -eq '127.0.0.1' -or $maquina -eq 'localhost') {
        if (Probar-Qdrant $direccion) {
            Write-Host "Qdrant ya está andando en $direccion."
        }
        else {
            Write-Host "Arrancando Qdrant en otra ventana (minimizada)..."
            $iniciarQdrant = Join-Path $PSScriptRoot 'iniciar-qdrant.ps1'
            Start-Process -FilePath 'powershell' -WindowStyle Minimized -WorkingDirectory $raiz `
                -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$iniciarQdrant`"")
            $anda = $false
            for ($i = 0; $i -lt 30 -and -not $anda; $i++) {
                Start-Sleep -Seconds 1
                $anda = Probar-Qdrant $direccion
            }
            if ($anda) {
                Write-Host "Qdrant quedó andando en $direccion."
            }
            else {
                Write-Host "AVISO: Qdrant no respondió en 30 segundos. La API arranca igual y la página lo va a mostrar en rojo."
                Write-Host "Para ver por qué no arranca, corré a mano: powershell -NoProfile -ExecutionPolicy Bypass -File scripts\iniciar-qdrant.ps1"
            }
        }
    }
    else {
        Write-Host "Qdrant está en otra máquina ($direccion): no se arranca desde acá."
    }

    Write-Host 'Arrancando la API. Se frena con Ctrl+C.'
    Push-Location -LiteralPath $api
    try {
        & dotnet run
        $salida = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
    exit $salida
}
catch {
    Write-Host "ERROR: no se pudo arrancar. $($_.Exception.Message)"
    exit 1
}
finally {
    try { [Console]::OutputEncoding = $codificacionAnterior } catch { }
}
