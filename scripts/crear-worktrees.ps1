<#
.SYNOPSIS
Crea o actualiza una carpeta por cada fase del proyecto, con git worktree.

.DESCRIPTION
Cada branch fase-* queda abierta en su propia carpeta, "separada" (detached HEAD):
sirve para leer y correr esa versión, no para commitear.
Toma las branches fase-* locales y también las del remoto (origin/fase-*), así funciona en un clon nuevo.
Se corre solo desde la carpeta principal del repo, porque cada fase trae su propia copia de este script.

.PARAMETER Carpeta
Dónde van las carpetas de las fases. Por defecto, una carpeta al lado del repo
con el mismo nombre y "-fases" al final (por ejemplo, ..\RAG-BASICO-fases).

.EXAMPLE
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\crear-worktrees.ps1
#>
param(
    [string]$Carpeta
)

# Git escribe en UTF-8. Para que PowerShell 5.1 lea bien rutas con tildes, la consola pasa a UTF-8
# mientras corre el script, y al final vuelve a como estaba.
$codificacionAnterior = [Console]::OutputEncoding
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
try {
    # 1. Solo desde la carpeta principal: en una carpeta de fase, git-dir y git-common-dir son distintas.
    $gitDir = & git rev-parse --path-format=absolute --git-dir
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'ERROR: esta carpeta no está adentro de un repo de git.'
        exit 1
    }
    $gitComun = & git rev-parse --path-format=absolute --git-common-dir
    if ($gitDir -ne $gitComun) {
        Write-Host 'ERROR: este script se corre desde la carpeta principal del repo, no desde una carpeta de fase.'
        exit 1
    }

    $raiz = [System.IO.Path]::GetFullPath((& git rev-parse --show-toplevel))
    if (-not $Carpeta) {
        $Carpeta = Join-Path (Split-Path $raiz -Parent) ((Split-Path $raiz -Leaf) + '-fases')
    }
    $Carpeta = [System.IO.Path]::GetFullPath($Carpeta)

    # 2. Limpia el registro de carpetas que se borraron a mano.
    & git worktree prune

    $registradas = @(& git worktree list --porcelain |
        Where-Object { $_ -like 'worktree *' } |
        ForEach-Object { [System.IO.Path]::GetFullPath($_.Substring(9)) })

    # Las fases: las branches locales y las del remoto. Si una fase solo está en el remoto,
    # su carpeta se arma con la versión del remoto (por ejemplo, origin/fase-0-base).
    $locales = @(& git for-each-ref --format='%(refname:short)' 'refs/heads/fase-*')
    $remotas = @{}
    foreach ($ref in @(& git for-each-ref --format='%(refname:lstrip=2)' 'refs/remotes/*/fase-*')) {
        $nombre = $ref.Substring($ref.IndexOf('/') + 1)
        if (-not $remotas.ContainsKey($nombre)) { $remotas[$nombre] = $ref }
    }
    $fases = @(@($locales) + @($remotas.Keys) | Sort-Object -Unique)
    if ($fases.Count -eq 0) {
        Write-Host 'No hay branches fase-*: no hay nada que crear.'
        exit 0
    }
    if (-not (Test-Path -LiteralPath $Carpeta)) {
        New-Item -ItemType Directory -Path $Carpeta | Out-Null
    }

    # 3. Una carpeta por fase: la crea, o la mueve a la versión actual de su branch.
    $errores = 0
    foreach ($fase in $fases) {
        $destino = Join-Path $Carpeta $fase
        if ($locales -contains $fase) { $version = $fase } else { $version = $remotas[$fase] }

        if (-not (Test-Path -LiteralPath $destino)) {
            & git worktree add -q --detach $destino $version
            if ($LASTEXITCODE -ne 0) {
                Write-Host "ERROR: no se pudo crear $destino"
                $errores++
                continue
            }
            Write-Host "Creada:      $destino ($version)"
            continue
        }

        if ($registradas -notcontains $destino) {
            Write-Host "ERROR: $destino existe pero no es una carpeta de worktree. Movela o borrala y volvé a correr el script."
            $errores++
            continue
        }

        # Solo cuentan los archivos que git sigue: uno nuevo, como la configuración que deja un editor, no frena.
        $cambios = @(& git -C $destino status --porcelain --untracked-files=no)
        if ($cambios.Count -gt 0) {
            Write-Host "AVISO: $destino tiene cambios sin guardar. No se toca."
            continue
        }

        & git -C $destino switch -q --detach $version
        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: no se pudo actualizar $destino. ¿La API de esa fase o Visual Studio están abiertos?"
            $errores++
            continue
        }
        Write-Host "Actualizada: $destino ($version)"
    }

    # 4. Carpetas que ya no tienen branch.
    Get-ChildItem -LiteralPath $Carpeta -Directory |
        Where-Object { $fases -notcontains $_.Name } |
        ForEach-Object {
            Write-Host "AVISO: $($_.FullName) no corresponde a ninguna branch fase-*. Si ya no la usás: git worktree remove `"$($_.FullName)`""
        }

    Write-Host ''
    & git worktree list

    if ($errores -gt 0) { exit 1 }
    exit 0
}
finally {
    try { [Console]::OutputEncoding = $codificacionAnterior } catch { }
}
