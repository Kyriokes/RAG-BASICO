<#
.SYNOPSIS
Arma o verifica documentos.csv, el manifiesto de los PDF del examen.

.DESCRIPTION
Por cada PDF de la carpeta de documentos escribe una línea "archivo;url;sha256" en documentos.csv, en la raíz del repo.
Conserva las URL que ya estaban anotadas. Con -Verificar no escribe nada: compara la carpeta con el manifiesto
y sale con 1 si falta, sobra o cambió algún PDF. Así el corpus del examen queda congelado.
La carpeta sale de la variable de entorno Rag__CarpetaDocumentos, salvo que se pase -Carpeta.

.PARAMETER Carpeta
La carpeta de los PDF. Por defecto, la de la variable de entorno Rag__CarpetaDocumentos.

.PARAMETER Verificar
Solo compara la carpeta con el manifiesto, sin escribir.

.EXAMPLE
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-manifiesto.ps1 -Verificar
#>
param(
    [string]$Carpeta,
    [switch]$Verificar
)

$codificacionAnterior = [Console]::OutputEncoding
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
try {
    $raiz = & git rev-parse --show-toplevel
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'ERROR: esta carpeta no está adentro de un repo de git.'
        exit 1
    }
    $raiz = [System.IO.Path]::GetFullPath($raiz)
    if (-not $Carpeta) { $Carpeta = $env:Rag__CarpetaDocumentos }
    if (-not $Carpeta -or -not (Test-Path -LiteralPath $Carpeta)) {
        Write-Host "ERROR: no existe la carpeta de PDF '$Carpeta'. Configurá la variable de entorno Rag__CarpetaDocumentos o pasá -Carpeta."
        exit 1
    }
    $manifiesto = Join-Path $raiz 'documentos.csv'

    # Lo que ya estaba anotado: archivo -> (url, sha256).
    $anotados = @{}
    if (Test-Path -LiteralPath $manifiesto) {
        foreach ($linea in [System.IO.File]::ReadAllLines($manifiesto, [System.Text.Encoding]::UTF8)) {
            $partes = $linea.Split(';')
            if ($partes.Count -ge 3 -and $partes[0] -ne 'archivo') {
                $anotados[$partes[0]] = @{ Url = $partes[1]; Sha = $partes[2] }
            }
        }
    }

    # Lo que hay en la carpeta: archivo -> sha256.
    $actuales = @{}
    foreach ($pdf in (Get-ChildItem -LiteralPath $Carpeta -Filter '*.pdf' | Sort-Object Name)) {
        $actuales[$pdf.Name] = (Get-FileHash -LiteralPath $pdf.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }

    if ($Verificar) {
        $diferencias = 0
        foreach ($archivo in $actuales.Keys) {
            if (-not $anotados.ContainsKey($archivo)) { Write-Host "SOBRA en la carpeta: $archivo"; $diferencias++ }
            elseif ($anotados[$archivo].Sha -ne $actuales[$archivo]) { Write-Host "CAMBIÓ: $archivo"; $diferencias++ }
        }
        foreach ($archivo in $anotados.Keys) {
            if (-not $actuales.ContainsKey($archivo)) { Write-Host "FALTA en la carpeta: $archivo"; $diferencias++ }
        }
        if ($diferencias -gt 0) {
            Write-Host "$diferencias diferencia(s) entre la carpeta y documentos.csv."
            exit 1
        }
        Write-Host "OK: la carpeta coincide con documentos.csv ($($actuales.Count) PDF)."
        exit 0
    }

    $lineas = New-Object System.Collections.Generic.List[string]
    $lineas.Add('archivo;url;sha256')
    foreach ($archivo in ($actuales.Keys | Sort-Object)) {
        $url = ''
        if ($anotados.ContainsKey($archivo)) { $url = $anotados[$archivo].Url }
        $lineas.Add($archivo + ';' + $url + ';' + $actuales[$archivo])
    }
    [System.IO.File]::WriteAllLines($manifiesto, [string[]]$lineas, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host "Listo: documentos.csv con $($actuales.Count) PDF. Completá la columna url de los que no la tienen."
    exit 0
}
finally {
    try { [Console]::OutputEncoding = $codificacionAnterior } catch { }
}
