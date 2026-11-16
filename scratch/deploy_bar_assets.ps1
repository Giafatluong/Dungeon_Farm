Add-Type -AssemblyName System.Drawing

function GenerateTrack([int]$w, [int]$h, [int]$radius, [string]$outputPath) {
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

    # Recessed dark cavity background (warm dark charcoal/wood)
    $brushBg = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(250, 19, 13, 9))
    $g.FillPath($brushBg, $path)
    $brushBg.Dispose()

    # Outer border: rustic dark bronze/wood bezel
    $penBorder = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 66, 44, 30), 1.5)
    $g.DrawPath($penBorder, $path)
    $penBorder.Dispose()

    # Inset top shadow: gives 3D depth to the trough
    $penShadow = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(180, 6, 4, 3), 1.0)
    $g.DrawLine($penShadow, $radius, 2, $w - $radius, 2)
    $penShadow.Dispose()

    # Subtle bottom highlight edge on the outer frame
    $penBot = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(80, 110, 80, 55), 1.0)
    $g.DrawLine($penBot, $radius, $h - 1, $w - $radius, $h - 1)
    $penBot.Dispose()

    $path.Dispose()
    $g.Dispose()

    $bmp.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "Generated track: $outputPath ($w x $h)"
}

function GenerateWhiteFill([int]$w, [int]$h, [int]$radius, [string]$outputPath) {
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

    # Pure white gradient with glossy upper half
    $rect = New-Object System.Drawing.Rectangle(0, 0, $w, $h)
    $brushGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(255, 255, 255, 255),
        [System.Drawing.Color]::FromArgb(255, 230, 230, 230),
        [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
    )
    $g.FillPath($brushGrad, $path)
    $brushGrad.Dispose()

    # Top specular gloss highlight line
    $penGloss = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(240, 255, 255, 255), 1.0)
    $g.DrawLine($penGloss, $radius, 1, $w - $radius, 1)
    $penGloss.Dispose()

    $path.Dispose()
    $g.Dispose()

    $bmp.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "Generated fill: $outputPath ($w x $h)"
}

$assetDir = "Assets\_Assets\Assets"

# 1. Universal clean tracks
GenerateTrack 882 22 5 "$assetDir\RiskGauge_Bg.png"
GenerateTrack 882 22 5 "$assetDir\RiskGauge_Bg-1.png"
GenerateTrack 882 22 5 "$assetDir\RiskGauge_Bg-2.png"
GenerateTrack 260 34 8 "$assetDir\Comp_Camp_RiskGaugeTrack.png"
GenerateTrack 296 16 4 "$assetDir\Comp_Combat_HPTrack.png"

# 2. Universal clean pure-white fills
GenerateWhiteFill 886 14 3 "$assetDir\Fill_100.png"
GenerateWhiteFill 220 20 5 "$assetDir\Comp_Camp_RiskGaugeFill.png"
GenerateWhiteFill 296 14 3 "$assetDir\Comp_Combat_HPRealFill.png"
GenerateWhiteFill 296 14 3 "$assetDir\Comp_Combat_HPGhostFill.png"

Write-Output "All progress bar assets successfully updated!"
