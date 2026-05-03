# One-shot conversion: Icon.jpg (project root) -> src/CrmApp.WinForms/icon.ico (multi-size).
#
# Pipeline:
#   1. Load source JPG.
#   2. Auto-detect the inner rectangle (the dark CRM browser graphic) by counting
#      dark pixels per row and per column. The longest contiguous run of "rectangle-dense"
#      rows/cols is the inner shape we want — circle ring rows are short (just two edges),
#      circle cap rows are also short, only the rectangle gives a long continuous block.
#   3. Crop to the detected rectangle (no circle, no alpha mask). Pad to square so the icon
#      isn't squished at small sizes.
#   4. Render at 256/128/64/48/32/16 px.
#   5. Pack as multi-size .ico (PNG-encoded entries).
#
# Run from project root:
#   powershell -ExecutionPolicy Bypass -File .\make-icon.ps1

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

$src = Resolve-Path 'Icon.jpg'
$dst = Join-Path (Resolve-Path 'src\CrmApp.WinForms').Path 'icon.ico'
$sizes = @(256, 128, 64, 48, 32, 16)

Write-Host "Source: $src" -ForegroundColor Cyan
Write-Host "Target: $dst" -ForegroundColor Cyan

$source = [System.Drawing.Image]::FromFile($src)
$srcW = $source.Width
$srcH = $source.Height

# --- Fast pixel scan via LockBits ---
$bmp = New-Object System.Drawing.Bitmap $source
$rect = New-Object System.Drawing.Rectangle 0, 0, $srcW, $srcH
$data = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$stride = [Math]::Abs($data.Stride)
$bytes = New-Object byte[] ($stride * $srcH)
[System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $bytes, 0, $bytes.Length)
$bmp.UnlockBits($data)
$bmp.Dispose()

$darkThreshold = 240
$rowCounts = New-Object int[] $srcH
$colCounts = New-Object int[] $srcW

for ($y = 0; $y -lt $srcH; $y++) {
    $base = $y * $stride
    for ($x = 0; $x -lt $srcW; $x++) {
        $i = $base + $x * 4
        if ($bytes[$i] -lt $darkThreshold -or $bytes[$i + 1] -lt $darkThreshold -or $bytes[$i + 2] -lt $darkThreshold) {
            $rowCounts[$y]++
            $colCounts[$x]++
        }
    }
}

# --- Find longest run of "rectangle-dense" rows / cols ---
# A row is "rectangle-dense" if its dark pixel count is at least 10% of width.
# Circle ring rows have just 2 dark segments (~14 px); rectangle rows have many more.
# 10% (= 70 px in 700 wide) is enough to catch even the URL-bar rows but well above
# any single ring edge.
$rowMin = [int]($srcW * 0.10)
$colMin = [int]($srcH * 0.10)

function Find-LongestRun([int[]]$counts, [int]$min) {
    $bestStart = -1
    $bestEnd = -1
    $bestLen = 0
    $curStart = -1
    for ($i = 0; $i -lt $counts.Length; $i++) {
        if ($counts[$i] -ge $min) {
            if ($curStart -lt 0) { $curStart = $i }
            $curLen = $i - $curStart + 1
            if ($curLen -gt $bestLen) {
                $bestLen = $curLen
                $bestStart = $curStart
                $bestEnd = $i
            }
        } else {
            $curStart = -1
        }
    }
    return @{ Start = $bestStart; End = $bestEnd; Length = $bestLen }
}

$rowRun = Find-LongestRun $rowCounts $rowMin
$colRun = Find-LongestRun $colCounts $colMin

if ($rowRun.Length -eq 0 -or $colRun.Length -eq 0) {
    Write-Host "Failed to detect inner rectangle (no dense row/col runs)." -ForegroundColor Red
    exit 1
}

$rectLeft   = $colRun.Start
$rectRight  = $colRun.End
$rectTop    = $rowRun.Start
$rectBottom = $rowRun.End
$rectW = $rectRight - $rectLeft + 1
$rectH = $rectBottom - $rectTop + 1

Write-Host ("Detected rectangle: ({0},{1})-({2},{3}) size={4}x{5}" -f $rectLeft, $rectTop, $rectRight, $rectBottom, $rectW, $rectH) -ForegroundColor Gray

# --- Build square master at 1024 px, rectangle centered, transparent padding ---
# Important: we crop ONLY the rectangle region of the source (no white space around).
# Then scale-draw it into the master while preserving aspect ratio. The remaining area
# stays alpha-transparent so the icon shows just the dark rectangle on any background.
$masterSize = 1024
$master = New-Object System.Drawing.Bitmap $masterSize, $masterSize
$g = [System.Drawing.Graphics]::FromImage($master)
$g.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$g.Clear([System.Drawing.Color]::Transparent)

# Scale to fit master while preserving aspect.
$scale = $masterSize / [double]([Math]::Max($rectW, $rectH))
$drawW = $rectW * $scale
$drawH = $rectH * $scale
$drawX = ($masterSize - $drawW) / 2.0
$drawY = ($masterSize - $drawH) / 2.0

$srcRectF = New-Object System.Drawing.RectangleF $rectLeft, $rectTop, $rectW, $rectH
$dstRectF = New-Object System.Drawing.RectangleF $drawX, $drawY, $drawW, $drawH
$g.DrawImage($source, $dstRectF, $srcRectF, [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()
$source.Dispose()

# Save preview PNG to project root for visual check.
$master.Save((Join-Path (Get-Location) 'Icon-preview.png'), [System.Drawing.Imaging.ImageFormat]::Png)

# --- Resample master to target sizes, encode as PNG ---
$pngBytes = New-Object 'System.Collections.Generic.List[byte[]]'
foreach ($size in $sizes) {
    $img = New-Object System.Drawing.Bitmap $size, $size
    $g2 = [System.Drawing.Graphics]::FromImage($img)
    $g2.SmoothingMode      = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g2.InterpolationMode  = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g2.PixelOffsetMode    = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g2.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g2.Clear([System.Drawing.Color]::Transparent)
    $g2.DrawImage($master, 0, 0, $size, $size)
    $g2.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $img.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytes.Add($ms.ToArray())
    $img.Dispose()
    $ms.Dispose()
}
$master.Dispose()

# --- Pack ICO container ---
$stream = [System.IO.File]::Open($dst, [System.IO.FileMode]::Create)
$writer = New-Object System.IO.BinaryWriter $stream

# ICONDIR (6 bytes).
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$sizes.Count)

# Directory entries (16 bytes each).
$dataOffset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $w = if ($s -ge 256) { 0 } else { $s }
    $writer.Write([byte]$w)
    $writer.Write([byte]$w)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$pngBytes[$i].Length)
    $writer.Write([uint32]$dataOffset)
    $dataOffset += $pngBytes[$i].Length
}

foreach ($b in $pngBytes) { $writer.Write($b) }
$writer.Close()

$kb = [math]::Round((Get-Item $dst).Length / 1KB, 1)
$sizesStr = $sizes -join ','
Write-Host "Done. icon.ico = $kb KB ($sizesStr px), inner rectangle only." -ForegroundColor Green
Write-Host "Preview: Icon-preview.png" -ForegroundColor Green
