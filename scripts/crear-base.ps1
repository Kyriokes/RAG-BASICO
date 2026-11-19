<#
.SYNOPSIS
Crea la base de la fase y corre en orden los scripts numerados de sql\.

.DESCRIPTION
Toma el servidor y la base de ConnectionStrings:Rag en RagBasico.Api\appsettings.json, salvo que se pasen
-Servidor o -Base. Corre con sqlcmd cada sql\NN_*.sql, en orden, pasándole el nombre de la base en la variable Base.
Los scripts se pueden correr de nuevo: cada paso controla si ya está hecho.
Si el servidor es de esta PC, se conecta por memoria compartida (lpc:), sin el servicio SQL Server Browser.

.PARAMETER Base
Nombre de la base. Por defecto, el de la cadena de conexión (por ejemplo, RagBasico_F2).

.PARAMETER Servidor
Servidor e instancia de SQL Server. Por defecto, el de la cadena de conexión (localhost\SQLEXPRESS).

.EXAMPLE
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-base.ps1
#>
param(
    [string]$Base,
    [string]$Servidor
)

# Git escribe en UTF-8. Para que PowerShell 5.1 lea bien rutas con tildes, la consola pasa a UTF-8
# mientras corre el script, y al final vuelve a como estaba.
$codificacionAnterior = [Console]::OutputEncoding
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
try {
    $raiz = & git rev-parse --show-toplevel
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'ERROR: esta carpeta no está adentro de un repo de git.'
        exit 1
    }
    $raiz = [System.IO.Path]::GetFullPath($raiz)

    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        Write-Host 'ERROR: falta sqlcmd. Instalalo con: winget install Microsoft.Sqlcmd'
        exit 1
    }

    $config = [System.IO.File]::ReadAllText((Join-Path $raiz 'RagBasico.Api\appsettings.json'), [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    $cadena = [string]$config.ConnectionStrings.Rag
    if (-not $Base) { $Base = [regex]::Match($cadena, 'Database=([^;]+)').Groups[1].Value }
    if (-not $Servidor) { $Servidor = [regex]::Match($cadena, 'Server=([^;]+)').Groups[1].Value }
    if (-not $Base -or -not $Servidor) {
        Write-Host 'ERROR: no se pudo leer el servidor o la base de ConnectionStrings:Rag en appsettings.json.'
        exit 1
    }

    # Una instancia de esta PC se conecta por memoria compartida (lpc:). Sin el prefijo, el sqlcmd que instala winget
    # primero le pregunta por la instancia al servicio SQL Server Browser, y si ese servicio no está andando, lo espera
    # hasta agotar el tiempo y falla aunque SQL Server esté andando.
    $destino = $Servidor
    $local = '^(localhost|\.|\(local\)|' + [regex]::Escape($env:COMPUTERNAME) + ')(\\[^,]*)?$'
    if ($destino -notmatch '^\w+:' -and $destino -match $local) {
        $destino = 'lpc:' + $destino
    }

    $scripts = @(Get-ChildItem -LiteralPath (Join-Path $raiz 'sql') -Filter '*.sql' |
        Where-Object { $_.Name -match '^\d\d_' } | Sort-Object Name)
    foreach ($script in $scripts) {
        Write-Host "Corriendo $($script.Name) en $Base..."
        # -E: autenticación de Windows; -C: confiar en el certificado de SQL Express; -b: cortar ante un error.
        & sqlcmd -S $destino -E -C -b -v "Base=$Base" -i $script.FullName
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: falló $($script.Name)."
            # El error más común: el servicio de SQL Server está detenido. Por memoria compartida, sqlcmd falla enseguida
            # con "The system cannot find the file specified", sin decir que es el servicio.
            Write-Host 'Si no pudo conectarse, revisá que esté andando el servicio de SQL Server. Como administrador: Start-Service ''MSSQL$SQLEXPRESS'''
            exit 1
        }
    }
    Write-Host "Listo: la base $Base está al día."
    exit 0
}
finally {
    try { [Console]::OutputEncoding = $codificacionAnterior } catch { }
}
