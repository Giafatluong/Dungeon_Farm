Add-Type -AssemblyName System.Drawing

function CreateTrackBitmap([int]$w, [int]$h, [int]$radius) {
    $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($w - $d - 1, 0, $d, $d, 270, 90)
    $path.AddArc($w - $d - 1, $h - $d - 1, $d, $d, 0, 90)
    $path.AddArc(0, $h - $d - 1, $d, $d, 90, 90)
    $path.CloseFigure()

    # Fill dark inner cavity (clean dark recessed trough)
    $brushBg = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(250, 18, 12, 9))
    $g.FillPath($brushBg, $path)
    $brushBg.Dispose()

    # Outer border (warm dark bronze/rustic wood frame)
    $penBorder = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 62, 40, 26), 1.5)
    $g.DrawPath($penBorder, $path)
    $penBorder.Dispose()

    # Inner top inset shadow line
    $penShadow = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(180, 5, 3, 2), 1.0)
    $g.DrawLine($penShadow, $radius, 2, $w - $radius, 2)
    $penShadow.Dispose()

    $path.Dispose()
    $g.Dispose()
    return $bmp
}

function CreateFillBitmap([int]$w, [int]$h, [int]$radius) {
    $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($w - $d - 1, 0, $d, $d, 270, 90)
    $path.AddArc($w - $d - 1, $h - $d - 1, $d, $d, 0, 90)
    $path.AddArc(0, $h - $d - 1, $d, $d, 90, 90)
    $path.CloseFigure()

    # Vertical gradient: pure white with subtle depth
    $rect = New-Object System.Drawing.Rectangle(0, 0, $w, $h)
    $brushGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(255, 255, 255, 255),
        [System.Drawing.Color]::FromArgb(255, 228, 228, 228),
        [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
    )
    $g.FillPath($brushGrad, $path)
    $brushGrad.Dispose()

    # Top highlight line (crisp shine)
    $penHi = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(220, 255, 255, 255), 1.0)
    $g.DrawLine($penHi, $radius, 1, $w - $radius, 1)
    $penHi.Dispose()

    $path.Dispose()
    $g.Dispose()
    return $bmp
}

if (-not (Test-Path 'scratch')) { New-Item -ItemType Directory -Path 'scratch' | Out-Null }

$track = CreateTrackBitmap 256 24 6
$track.Save('scratch\test_track.png', [System.Drawing.Imaging.ImageFormat]::Png)
$track.Dispose()

$fill = CreateFillBitmap 256 20 4
$fill.Save('scratch\test_fill.png', [System.Drawing.Imaging.ImageFormat]::Png)
$fill.Dispose()

Write-Output 'Generated test_track.png and test_fill.png successfully!'
