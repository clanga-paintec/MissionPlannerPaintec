# GenerateIcon.ps1
# Convierte Gridflight-Icon.png a Gridflight-Icon.ico (multi-tamaño, PNG-in-ICO).
#
# Uso: Ejecutar UNA VEZ desde la carpeta GridFlight antes de compilar.
#      El .ico generado se sube a git junto con el .png fuente.
#
# Formato ICO con chunks PNG (soportado desde Windows Vista / VS 2010):
#   - No requiere conversion BMP; cada tamaño se guarda como PNG dentro del ICO.
#   - Las entradas 0x00 en width/height indican tamaño 256.

Add-Type -AssemblyName System.Drawing

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$inputPng  = Join-Path $scriptDir "assets\Gridflight-Icon.png"
$outputIco = Join-Path $scriptDir "assets\Gridflight-Icon.ico"

if (-not (Test-Path $inputPng)) {
    Write-Error "No se encontro: $inputPng"
    exit 1
}

$source = [System.Drawing.Image]::FromFile($inputPng)
$sizes  = @(16, 24, 32, 48, 256)
$chunks = @()

foreach ($sz in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($sz, $sz,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode =
        [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode =
        [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.DrawImage($source, 0, 0, $sz, $sz)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $chunks += , $ms.ToArray()
    $ms.Dispose()
    $bmp.Dispose()
}
$source.Dispose()

# --- Construccion del binario ICO ---
$out = New-Object System.IO.MemoryStream

# ICONDIR: reserved(2) | type=1(2) | count(2)
$count = $sizes.Count
$out.Write([byte[]](0,0, 1,0, $count,0), 0, 6)

# El offset al primer bloque de datos: 6 + 16*count
$offset = 6 + 16 * $count

for ($i = 0; $i -lt $count; $i++) {
    $sz   = $sizes[$i]
    $data = $chunks[$i]
    $len  = $data.Length
    $w    = if ($sz -eq 256) { 0 } else { $sz }
    $h    = if ($sz -eq 256) { 0 } else { $sz }

    # ICONDIRENTRY (16 bytes): w h colorCnt reserved planes bitCnt size[4] offset[4]
    $entry = [byte[]](
        $w, $h, 0, 0,   # width, height, colorCount, reserved
        0, 0,            # planes  (0 = PNG)
        0, 0,            # bitCount (0 = PNG)
        ($len         -band 0xFF),
        (($len -shr 8)  -band 0xFF),
        (($len -shr 16) -band 0xFF),
        (($len -shr 24) -band 0xFF),
        ($offset         -band 0xFF),
        (($offset -shr 8)  -band 0xFF),
        (($offset -shr 16) -band 0xFF),
        (($offset -shr 24) -band 0xFF)
    )
    $out.Write($entry, 0, 16)
    $offset += $len
}

foreach ($data in $chunks) {
    $out.Write($data, 0, $data.Length)
}

[System.IO.File]::WriteAllBytes($outputIco, $out.ToArray())
$out.Dispose()

Write-Host "OK  ->  $outputIco  ($($sizes -join 'x, ')x px)"
