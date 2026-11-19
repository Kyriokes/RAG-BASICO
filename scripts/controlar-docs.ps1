<#
.SYNOPSIS
Controla que la documentación de la fase coincida con el código.

.DESCRIPTION
Busca lo que falta y lo que sobra:
  1. Archivos: cada archivo del repo tiene su sección en docs\responsabilidades.md y está en el árbol,
     y cada sección y cada nombre del árbol corresponden a algo que existe.
  2. Los campos obligatorios de cada sección y de cada entrada.
  3. Clases y funciones: cada una de los .cs tiene su entrada y su /// <summary>,
     y cada entrada de clase o de función corresponde a algo que existe.
  4. Endpoints: cada app.MapGet, app.MapPost... tiene su entrada, y cada entrada corresponde a uno que existe.
  5. Que "Desde" y "Cambió en" nombren branches fase-* que existen.
  6. Los cinco títulos de docs\arquitectura.md, y que nombre cada clave de appsettings.json y cada columna de sql\*.sql.
  7. Los siete títulos de docs\fases\<fase>.md, y que "Qué cambió" nombre cada archivo cambiado desde la fase anterior.
  8. Que la fila de la fase en el README no tenga celdas vacías.
Los controles 1, 3 y 4 miran en los dos sentidos; el 6 y el 7 solo buscan lo que falta.
La fase actual sale de Rag:Fase en RagBasico.Api\appsettings.json.
Sale con 0 si no encontró problemas y con 1 si encontró alguno.

.EXAMPLE
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\controlar-docs.ps1
#>
param()

$script:problemas = New-Object System.Collections.Generic.List[string]

# Anota un problema para mostrarlo al final.
function Agregar-Problema([string]$texto) {
    $script:problemas.Add($texto)
}

# Lee un archivo en UTF-8 (con o sin BOM) y deja los finales de línea en LF.
function Leer-Texto([string]$ruta) {
    if (-not (Test-Path -LiteralPath $ruta)) { return $null }
    $texto = [System.IO.File]::ReadAllText($ruta, [System.Text.Encoding]::UTF8)
    return $texto.Replace("`r`n", "`n")
}

# Borra los bloques de código para que su contenido no se tome como títulos.
function Quitar-Codigo([string]$texto) {
    return [regex]::Replace($texto, '(?ms)^```.*?^```[^\n]*$', '')
}

# Corta un texto en secciones por títulos de un nivel ("##" o "###"). Devuelve objetos con Titulo y Cuerpo.
function Obtener-Secciones([string]$texto, [string]$nivel) {
    $secciones = @()
    $titulos = [regex]::Matches($texto, '(?m)^' + [regex]::Escape($nivel) + ' (.+)$')
    for ($i = 0; $i -lt $titulos.Count; $i++) {
        $inicio = $titulos[$i].Index + $titulos[$i].Length
        if ($i + 1 -lt $titulos.Count) { $fin = $titulos[$i + 1].Index } else { $fin = $texto.Length }
        $secciones += New-Object PSObject -Property @{
            Titulo = $titulos[$i].Groups[1].Value.Trim()
            Cuerpo = $texto.Substring($inicio, $fin - $inicio)
        }
    }
    return $secciones
}

# Lee los campos "- **Etiqueta:** valor" de un texto. Devuelve un diccionario etiqueta -> valor.
function Obtener-Campos([string]$texto) {
    $campos = @{}
    foreach ($m in [regex]::Matches($texto, '(?m)^\s*-\s+\*\*([^*]+?):\*\*[ \t]*(.*)$')) {
        $campos[$m.Groups[1].Value.Trim()] = $m.Groups[2].Value.Trim()
    }
    return $campos
}

# Controla que una entrada tenga todos los campos obligatorios con texto. Devuelve los campos leídos.
function Controlar-Campos([string]$donde, [string]$texto, [string[]]$obligatorios) {
    $campos = Obtener-Campos $texto
    foreach ($campo in $obligatorios) {
        if (-not $campos.ContainsKey($campo) -or [string]::IsNullOrWhiteSpace($campos[$campo])) {
            Agregar-Problema "FALTA el campo '$campo' en $donde"
        }
    }
    return $campos
}

# Devuelve los nombres de fase (fase-N-nombre) que aparecen en un texto.
function Buscar-Fases([string]$texto) {
    return @([regex]::Matches($texto, 'fase-\d+-[A-Za-z0-9-]*[A-Za-z0-9]') | ForEach-Object { $_.Value })
}

# Controla que "Desde" y "Cambió en" nombren branches fase-* que existen.
function Controlar-Fases([string]$donde, $campos) {
    if ($campos.ContainsKey('Desde') -and $campos['Desde']) {
        $nombres = Buscar-Fases $campos['Desde']
        if ($nombres.Count -eq 0) { Agregar-Problema "'Desde' no nombra una fase en $donde" }
        foreach ($nombre in $nombres) {
            if ($script:branches -notcontains $nombre) { Agregar-Problema "'Desde' nombra $nombre, que no es una branch, en $donde" }
        }
    }
    if ($campos.ContainsKey('Cambió en') -and $campos['Cambió en']) {
        $valor = $campos['Cambió en']
        if ($valor -notmatch '^nada todav[ií]a\.?$') {
            $nombres = Buscar-Fases $valor
            if ($nombres.Count -eq 0) { Agregar-Problema "'Cambió en' tiene que nombrar fases o decir 'nada todavía' en $donde" }
            foreach ($nombre in $nombres) {
                if ($script:branches -notcontains $nombre) { Agregar-Problema "'Cambió en' nombra $nombre, que no es una branch, en $donde" }
            }
        }
    }
}

# Normaliza una ruta para comparar: barras hacia adelante y sin comillas invertidas.
function Normalizar-Ruta([string]$ruta) {
    return $ruta.Trim().Trim('`').Replace('\', '/')
}

# Devuelve las claves de un objeto JSON, escritas Seccion:Clave.
function Aplanar-Json($objeto, [string]$prefijo) {
    $claves = @()
    foreach ($propiedad in $objeto.PSObject.Properties) {
        if ($prefijo) { $nombre = $prefijo + ':' + $propiedad.Name } else { $nombre = $propiedad.Name }
        if ($propiedad.Value -is [System.Management.Automation.PSCustomObject]) {
            $claves += Aplanar-Json $propiedad.Value $nombre
        }
        else {
            $claves += $nombre
        }
    }
    return $claves
}

# Dice si la línea de un tipo o de una función tiene su /// <summary> arriba, salteando los atributos,
# también los que ocupan varias líneas.
function Tiene-Resumen([string[]]$lineas, [int]$indice) {
    $j = $indice - 1
    while ($j -ge 0) {
        $arriba = $lineas[$j].Trim()
        if ($arriba.StartsWith('[')) { $j--; continue }
        if ($arriba.EndsWith(']') -and -not $arriba.StartsWith('//')) {
            # Última línea de un atributo de varias líneas: se sube hasta la que lo abre.
            while ($j -ge 0 -and -not $lineas[$j].Trim().StartsWith('[')) { $j-- }
            continue
        }
        break
    }
    return ($j -ge 0 -and $lineas[$j].Trim().StartsWith('///'))
}

# Lee un .cs y devuelve sus tipos y funciones, cada uno con su línea y si tiene /// <summary> arriba.
# El nombre de una función es el identificador que va justo antes de "(", salteando modificadores,
# tipos genéricos y tuplas. Un constructor (mismo nombre que la clase) se documenta en la entrada de la clase.
function Analizar-CSharp([string]$ruta) {
    $lineas = [System.IO.File]::ReadAllLines($ruta, [System.Text.Encoding]::UTF8)
    $clasePrincipal = [System.IO.Path]::GetFileNameWithoutExtension($ruta)
    $patronTipo = '^(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|file)\s+)*(?:class|record|struct|interface|enum)\s+(\w+)'
    $patronFuncion = '^(?:public|private|protected|internal)\b(?<antes>.*?)\s(?!(?:public|private|protected|internal|static|async|override|virtual|abstract|sealed|new|extern|unsafe|partial|readonly|const)\b)(?<nombre>\w+)\s*(?:<[^()<>]*(?:<[^()<>]*>[^()<>]*)*>)?\s*\('
    $tipos = @()
    $funciones = @()
    for ($i = 0; $i -lt $lineas.Count; $i++) {
        $linea = $lineas[$i].Trim()
        $tipo = [regex]::Match($linea, $patronTipo)
        $nombreFuncion = $null
        if (-not $tipo.Success) {
            $m = [regex]::Match($linea, $patronFuncion)
            if ($m.Success) {
                $antes = $m.Groups['antes'].Value
                $esDeclaracion = $antes -notmatch '[={]' -and $antes -notmatch '\b(class|record|struct|interface|enum|delegate|event|operator)\b'
                if ($esDeclaracion -and $m.Groups['nombre'].Value -ne $clasePrincipal) {
                    $nombreFuncion = $m.Groups['nombre'].Value
                }
            }
        }
        if (-not $tipo.Success -and -not $nombreFuncion) { continue }

        $tieneResumen = Tiene-Resumen $lineas $i
        if ($tipo.Success) {
            $tipos += New-Object PSObject -Property @{ Nombre = $tipo.Groups[1].Value; Linea = $i + 1; Resumen = $tieneResumen }
        }
        else {
            $funciones += New-Object PSObject -Property @{ Nombre = $nombreFuncion; Linea = $i + 1; Resumen = $tieneResumen }
        }
    }
    return New-Object PSObject -Property @{ ClasePrincipal = $clasePrincipal; Tipos = $tipos; Funciones = $funciones }
}

# Devuelve los endpoints (app.MapGet, app.MapPost...) de un .cs, sin contar los que están en comentarios.
function Buscar-Endpoints([string]$ruta) {
    $codigo = [System.IO.File]::ReadAllText($ruta, [System.Text.Encoding]::UTF8)
    $codigo = [regex]::Replace($codigo, '(?s)/\*.*?\*/', '')
    $codigo = [regex]::Replace($codigo, '(?m)^\s*//.*$', '')
    return @([regex]::Matches($codigo, '\bapp\.Map(Get|Post|Put|Delete|Patch)\(\s*"([^"]*)"') |
        ForEach-Object { $_.Groups[1].Value.ToUpper() + ' ' + $_.Groups[2].Value })
}

# Git escribe en UTF-8. Para que PowerShell 5.1 lea bien rutas con tildes, la consola pasa a UTF-8
# mientras corre el script, y al final vuelve a como estaba.
$codificacionAnterior = [Console]::OutputEncoding
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
try {
    # -----------------------------------------------------------------------
    # Preparación
    # -----------------------------------------------------------------------
    $raiz = & git rev-parse --show-toplevel
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'ERROR: esta carpeta no está adentro de un repo de git.'
        exit 1
    }
    $raiz = [System.IO.Path]::GetFullPath($raiz)

    $textoConfig = Leer-Texto (Join-Path $raiz 'RagBasico.Api\appsettings.json')
    if ($null -eq $textoConfig) {
        Write-Host 'ERROR: no existe RagBasico.Api\appsettings.json.'
        exit 1
    }
    $config = $textoConfig | ConvertFrom-Json
    $fase = [string]$config.Rag.Fase
    $mFase = [regex]::Match($fase, '^fase-(\d+)-')
    if (-not $mFase.Success) {
        Write-Host 'ERROR: falta Rag:Fase en appsettings.json, o no tiene la forma fase-N-nombre.'
        exit 1
    }
    $numeroFase = [int]$mFase.Groups[1].Value

    $script:branches = @(& git -C $raiz for-each-ref --format='%(refname:short)' 'refs/heads/fase-*' 'refs/remotes/*/fase-*' |
        ForEach-Object { [regex]::Match($_, 'fase-\d+-.*$').Value } | Sort-Object -Unique)

    $archivos = @(& git -C $raiz -c core.quotepath=false ls-files --cached --others --exclude-standard |
        Where-Object { Test-Path -LiteralPath (Join-Path $raiz $_) } | Sort-Object -Unique)

    $textoResp = Leer-Texto (Join-Path $raiz 'docs\responsabilidades.md')
    $textoArq = Leer-Texto (Join-Path $raiz 'docs\arquitectura.md')
    $textoFase = Leer-Texto (Join-Path $raiz "docs\fases\$fase.md")
    $textoReadme = Leer-Texto (Join-Path $raiz 'README.md')
    if ($null -eq $textoResp) { Agregar-Problema 'FALTA docs\responsabilidades.md' }
    if ($null -eq $textoArq) { Agregar-Problema 'FALTA docs\arquitectura.md' }
    if ($null -eq $textoFase) { Agregar-Problema "FALTA docs\fases\$fase.md" }
    if ($null -eq $textoReadme) { Agregar-Problema 'FALTA README.md' }

    # -----------------------------------------------------------------------
    # 1 a 5. responsabilidades.md
    # -----------------------------------------------------------------------
    if ($null -ne $textoResp) {
        $sinCodigo = Quitar-Codigo $textoResp
        $secciones = @(Obtener-Secciones $sinCodigo '##')

        # El árbol: el primer bloque de código después de "## Árbol".
        $arbol = ''
        $mArbol = [regex]::Match($textoResp, '(?ms)^## Árbol\s*$.*?^```[^\n]*\n(.*?)^```')
        if ($mArbol.Success) { $arbol = $mArbol.Groups[1].Value }
        else { Agregar-Problema 'FALTA en responsabilidades.md la sección "## Árbol" con el árbol de archivos en un bloque de código' }

        $porRuta = @{}
        foreach ($seccion in $secciones) {
            if ($seccion.Titulo -eq 'Árbol') { continue }
            $porRuta[(Normalizar-Ruta $seccion.Titulo)] = $seccion
        }

        # 1. Cada archivo con su sección y en el árbol, y cada sección con su archivo.
        foreach ($archivo in $archivos) {
            if (-not $porRuta.ContainsKey($archivo)) { Agregar-Problema "FALTA en responsabilidades.md la sección del archivo $archivo" }
            $hoja = Split-Path $archivo -Leaf
            if ($arbol -and -not $arbol.Contains($hoja)) { Agregar-Problema "FALTA en el árbol de responsabilidades.md el archivo $archivo" }
        }
        foreach ($ruta in $porRuta.Keys) {
            if ($archivos -notcontains $ruta) { Agregar-Problema "SOBRA en responsabilidades.md la sección $($ruta): ese archivo no existe" }
        }

        # 1. Y al revés: cada nombre del árbol tiene que ser un archivo o una carpeta del repo.
        # La primera línea, si es una carpeta sin dibujo de rama, es la raíz del repo y no se controla.
        $conocidos = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
        foreach ($archivo in $archivos) { foreach ($parte in $archivo.Split('/')) { [void]$conocidos.Add($parte) } }
        $lineasArbol = @($arbol.Split("`n") | Where-Object { $_.Trim() })
        for ($k = 0; $k -lt $lineasArbol.Count; $k++) {
            $token = (($lineasArbol[$k] -replace '^[\s│├└─]+', '') -split '\s+')[0]
            if (-not $token) { continue }
            if ($k -eq 0 -and $token.EndsWith('\') -and $lineasArbol[$k] -notmatch '[│├└]') { continue }
            foreach ($parte in ($token.TrimEnd('\', '/') -split '[\\/]')) {
                if ($parte -and -not $conocidos.Contains($parte)) {
                    Agregar-Problema "SOBRA en el árbol de responsabilidades.md: $parte no es ningún archivo ni carpeta del repo"
                    break
                }
            }
        }

        # 2 y 5. Campos de cada archivo y de cada entrada, y las fases que nombran.
        $endpointsDocumentados = @()
        foreach ($ruta in $porRuta.Keys) {
            $cuerpo = $porRuta[$ruta].Cuerpo
            $corte = $cuerpo.IndexOf("`n### ")
            if ($corte -ge 0) { $cuerpoArchivo = $cuerpo.Substring(0, $corte) } else { $cuerpoArchivo = $cuerpo }

            $obligatorios = @('Responsabilidad', 'Desde', 'Cambió en')
            if ($ruta -like '*.ps1') { $obligatorios += @('Parámetros', 'Ejemplo', 'Requiere') }
            if ($ruta -like '*.sql') { $obligatorios += @('Qué crea', 'Variables sqlcmd') }
            $campos = Controlar-Campos "la sección $ruta" $cuerpoArchivo $obligatorios
            Controlar-Fases "la sección $ruta" $campos

            foreach ($parte in @(Obtener-Secciones $cuerpo '###')) {
                $titulo = $parte.Titulo
                $donde = "la entrada '$titulo' de $ruta"
                $mEndpoint = [regex]::Match($titulo, '^(GET|POST|PUT|DELETE|PATCH) (\S+)$')
                if ($titulo -match '^Clase \w+$') {
                    $c = Controlar-Campos $donde $parte.Cuerpo @('Responsabilidad', 'Recibe en el constructor', 'La registra', 'Desde', 'Cambió en')
                    Controlar-Fases $donde $c
                }
                elseif ($mEndpoint.Success) {
                    $endpointsDocumentados += ($mEndpoint.Groups[1].Value + ' ' + $mEndpoint.Groups[2].Value)
                    $c = Controlar-Campos $donde $parte.Cuerpo @('Recibe', 'Devuelve', 'La atiende', 'Desde', 'Cambió en')
                    Controlar-Fases $donde $c
                }
                elseif ($titulo -match '^\w+\.\w+\(') {
                    $c = Controlar-Campos $donde $parte.Cuerpo @('Qué hace', 'Recibe', 'Devuelve', 'Efectos', 'La llaman', 'Desde', 'Cambió en')
                    Controlar-Fases $donde $c
                }
            }
        }

        # 3 y 4. Clases, funciones y endpoints de los .cs, en los dos sentidos.
        $endpointsEnCodigo = @()
        foreach ($archivo in ($archivos | Where-Object { $_ -like '*.cs' })) {
            $analisis = Analizar-CSharp (Join-Path $raiz $archivo)
            $texto = ''
            if ($porRuta.ContainsKey($archivo)) { $texto = $porRuta[$archivo].Cuerpo }
            $titulos = @([regex]::Matches($texto, '(?m)^### (.+)$') | ForEach-Object { $_.Groups[1].Value.Trim() })
            $nombresTipos = @($analisis.Tipos | ForEach-Object { $_.Nombre })

            foreach ($tipo in $analisis.Tipos) {
                if (-not $tipo.Resumen) { Agregar-Problema "FALTA el /// <summary> del tipo $($tipo.Nombre) en $archivo, línea $($tipo.Linea)" }
                if ($tipo.Nombre -eq $analisis.ClasePrincipal) {
                    if ($titulos -notcontains "Clase $($tipo.Nombre)") { Agregar-Problema "FALTA en responsabilidades.md la entrada '### Clase $($tipo.Nombre)' en la sección de $archivo" }
                }
                elseif (-not [regex]::IsMatch($texto, '\b' + [regex]::Escape($tipo.Nombre) + '\b')) {
                    Agregar-Problema "FALTA en responsabilidades.md el tipo $($tipo.Nombre) en la sección de $archivo"
                }
            }

            $nombresEnCodigo = @()
            foreach ($funcion in $analisis.Funciones) {
                $nombresEnCodigo += $funcion.Nombre
                if (-not $funcion.Resumen) { Agregar-Problema "FALTA el /// <summary> de la función $($funcion.Nombre) en $archivo, línea $($funcion.Linea)" }
                $buscado = $analisis.ClasePrincipal + '.' + $funcion.Nombre + '('
                if (-not ($titulos | Where-Object { $_.StartsWith($buscado) })) {
                    Agregar-Problema "FALTA en responsabilidades.md la entrada '### $($analisis.ClasePrincipal).$($funcion.Nombre)(...)' en la sección de $archivo"
                }
            }
            foreach ($titulo in $titulos) {
                $m = [regex]::Match($titulo, '^(\w+)\.(\w+)\(')
                if ($m.Success -and $nombresEnCodigo -notcontains $m.Groups[2].Value) {
                    Agregar-Problema "SOBRA en responsabilidades.md la entrada '### $titulo': esa función no está en $archivo"
                }
                $mc = [regex]::Match($titulo, '^Clase (\w+)$')
                if ($mc.Success -and $nombresTipos -notcontains $mc.Groups[1].Value) {
                    Agregar-Problema "SOBRA en responsabilidades.md la entrada '### $titulo': esa clase no está en $archivo"
                }
            }

            $endpointsEnCodigo += Buscar-Endpoints (Join-Path $raiz $archivo)
        }
        foreach ($e in $endpointsEnCodigo) {
            if ($endpointsDocumentados -notcontains $e) { Agregar-Problema "FALTA en responsabilidades.md la entrada '### $e'" }
        }
        foreach ($e in $endpointsDocumentados) {
            if ($endpointsEnCodigo -notcontains $e) { Agregar-Problema "SOBRA en responsabilidades.md la entrada '### $e': ese endpoint no está en el código" }
        }
    }

    # -----------------------------------------------------------------------
    # 6. arquitectura.md
    # -----------------------------------------------------------------------
    if ($null -ne $textoArq) {
        $seccionesArq = @(Obtener-Secciones (Quitar-Codigo $textoArq) '##')
        foreach ($t in @('1. Piezas', '2. Recorridos', '3. Tablas', '4. Configuración', '5. Fuera del repo')) {
            $s = $seccionesArq | Where-Object { $_.Titulo -eq $t } | Select-Object -First 1
            if (-not $s) { Agregar-Problema "FALTA en arquitectura.md el título '## $t'" }
            elseif ([string]::IsNullOrWhiteSpace($s.Cuerpo)) { Agregar-Problema "SIN TEXTO en arquitectura.md el título '## $t' (si todavía no hay nada, escribí 'Todavía no hay')" }
        }
        foreach ($clave in (Aplanar-Json $config '')) {
            if ($clave -eq '$schema') { continue }
            if (-not $textoArq.Contains($clave)) { Agregar-Problema "FALTA en arquitectura.md la clave de configuración $clave" }
        }
        $tiposSql = 'INT|BIGINT|SMALLINT|TINYINT|BIT|DECIMAL|NUMERIC|FLOAT|REAL|NVARCHAR|VARCHAR|NCHAR|CHAR|VARBINARY|BINARY|DATETIME2|DATETIME|DATE|TIME|UNIQUEIDENTIFIER|VECTOR|JSON|XML'
        foreach ($archivo in ($archivos | Where-Object { $_ -like 'sql/*.sql' })) {
            $sql = Leer-Texto (Join-Path $raiz $archivo)
            $columnas = @([regex]::Matches($sql, '(?im)^\s*(?:ALTER\s+TABLE\s+\S+\s+)?(?:ADD\s+)?\[?(\w+)\]?\s+(?:' + $tiposSql + ')\b') | ForEach-Object { $_.Groups[1].Value })
            foreach ($columna in ($columnas | Where-Object { $_ -notmatch '^(ADD|DECLARE|RETURNS|AS|CAST|CONVERT)$' } | Sort-Object -Unique)) {
                if (-not [regex]::IsMatch($textoArq, '\b' + [regex]::Escape($columna) + '\b')) {
                    Agregar-Problema "FALTA en arquitectura.md la columna $columna de $archivo"
                }
            }
        }
    }

    # -----------------------------------------------------------------------
    # 7. docs\fases\<fase>.md
    # -----------------------------------------------------------------------
    if ($null -ne $textoFase) {
        $seccionesFase = @(Obtener-Secciones (Quitar-Codigo $textoFase) '##')
        foreach ($t in @('1. Objetivo', '2. Qué cambió', '3. Orden de lectura', '4. Cómo se corre y se prueba', '5. Resultados', '6. Decisiones', '7. Limitaciones conocidas')) {
            $s = $seccionesFase | Where-Object { $_.Titulo -eq $t } | Select-Object -First 1
            if (-not $s) { Agregar-Problema "FALTA en docs\fases\$fase.md el título '## $t'" }
            elseif ([string]::IsNullOrWhiteSpace($s.Cuerpo)) { Agregar-Problema "SIN TEXTO en docs\fases\$fase.md el título '## $t'" }
        }

        # La fase anterior: la branch fase-(N-1)-*; para la fase 0, el primer commit del repo.
        if ($numeroFase -eq 0) {
            $base = @(& git -C $raiz rev-list --max-parents=0 HEAD) | Select-Object -Last 1
        }
        else {
            $anterior = $numeroFase - 1
            $base = @(& git -C $raiz for-each-ref --format='%(refname:short)' "refs/heads/fase-$anterior-*" "refs/remotes/*/fase-$anterior-*") | Select-Object -First 1
        }
        if (-not $base) {
            Agregar-Problema "No se encontró la fase anterior a $fase para controlar 'Qué cambió'"
        }
        else {
            $mQue = [regex]::Match($textoFase, '(?ms)^## 2\. Qué cambió\s*$(.*?)(?=^## |\z)')
            $queCambio = ''
            if ($mQue.Success) { $queCambio = $mQue.Groups[1].Value.Replace('\', '/') }
            $cambiados = @(& git -C $raiz -c core.quotepath=false diff --name-only $base) +
                @(& git -C $raiz -c core.quotepath=false ls-files --others --exclude-standard)
            foreach ($archivo in ($cambiados | Sort-Object -Unique)) {
                if ($archivo -like 'docs/*' -or $archivo -eq 'README.md') { continue }
                if (-not $queCambio.Contains($archivo)) { Agregar-Problema "FALTA en 'Qué cambió' de docs\fases\$fase.md el archivo $archivo" }
            }
        }
    }

    # -----------------------------------------------------------------------
    # 8. README
    # -----------------------------------------------------------------------
    if ($null -ne $textoReadme) {
        $marca = '`' + $fase + '`'
        $fila = $textoReadme.Split("`n") | Where-Object { $_.TrimStart().StartsWith('|') -and $_.Contains($marca) } | Select-Object -First 1
        if (-not $fila) {
            Agregar-Problema "FALTA en el README la fila de $fase en la tabla de fases"
        }
        else {
            $celdas = $fila.Trim().Trim('|').Split('|')
            if ($celdas.Count -ne 7) { Agregar-Problema "La fila de $fase en el README tiene $($celdas.Count) columnas y tiene que tener 7" }
            for ($k = 0; $k -lt $celdas.Count; $k++) {
                if ([string]::IsNullOrWhiteSpace($celdas[$k])) { Agregar-Problema "SIN TEXTO la columna $($k + 1) de la fila de $fase en el README" }
            }
        }
    }

    # -----------------------------------------------------------------------
    # Resultado
    # -----------------------------------------------------------------------
    if ($script:problemas.Count -eq 0) {
        Write-Host "OK: la documentación de $fase coincide con el código."
        exit 0
    }
    foreach ($p in $script:problemas) { Write-Host $p }
    Write-Host ''
    Write-Host "$($script:problemas.Count) problema(s) en la documentación de $fase."
    exit 1
}
finally {
    try { [Console]::OutputEncoding = $codificacionAnterior } catch { }
}
