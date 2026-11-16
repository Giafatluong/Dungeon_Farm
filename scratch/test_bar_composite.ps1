Add-Type -AssemblyName System.Drawing

function CreateCompositedBar([string]$trackPath, [string]$fillPath, [float]$percent, [System.Drawing.Color]$tintColor) {
    $track = [System.Drawing.Bitmap]::FromFile($trackPath)
    $fill = [System.Drawing.Bitmap]::FromFile($fillPath)
    
    $result = New-Object System.Drawing.Bitmap($track.Width, $track.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($result)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    
    # Draw track
    $g.DrawImage($track, 0, 0, $track.Width, $track.Height)
    
    # Draw fill if percent > 0
    if ($percent -gt 0.001) {
        $fillW = [int](($track.Width - 4) * $percent)
        $fillH = $track.Height - 4
        if ($fillW -gt 0) {
            # Create tinted fill
            $tintedFill = New-Object System.Drawing.Bitmap($fill.Width, $fill.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $tg = [System.Drawing.Graphics]::FromImage($tintedFill)
            $tg.DrawImage($fill, 0, 0, $fill.Width, $fill.Height)
            $tg.Dispose()
            
            # Color matrix multiplication
            $cm = New-Object System.Drawing.Imaging.ColorMatrix
            $cm.Matrix00 = $tintColor.R / 255.0
            $cm.Matrix11 = $tintColor.G / 255.0
            $cm.Matrix22 = $tintColor.B / 255.0
            $cm.Matrix33 = $tintColor.A / 255.0
            $cm.Matrix44 = 1.0
            
            $ia = New-Object System.Drawing.Imaging.ImageAttributes
            $ia.SetColorMatrix($cm, [System.Drawing.Imaging.ColorMatrixFlag]::Default, [System.Drawing.Imaging.ColorAdjustType]::Bitmap)
            
            $destRect = New-Object System.Drawing.Rectangle(2, 2, $fillW, $fillH)
            $srcRect = New-Object System.Drawing.Rectangle(0, 0, [int]($fill.Width * $percent), $fill.Height)
            $g.DrawImage($tintedFill, $destRect, $srcRect.X, $srcRect.Y, $srcRect.Width, $srcRect.Height, [System.Drawing.GraphicsUnit]::Pixel, $ia)
            $ia.Dispose()
            $tintedFill.Dispose()
        }
    }
    
    $g.Dispose()
    $track.Dispose()
    $fill.Dispose()
    return $result
}

$destDir = "C:\Users\dohuu\.gemini\antigravity\brain\4bbe6c3d-df68-496b-a43d-564474b886e4"

# 1. Zero percent (Empty! Absolutely NO bar, pure clean trough)
$b0 = CreateCompositedBar "scratch\test_track.png" "scratch\test_fill.png" 0.0 ([System.Drawing.Color]::FromArgb(255, 60, 220, 80))
$b0.Save("$destDir\preview_bar_0pct.png", [System.Drawing.Imaging.ImageFormat]::Png)
$b0.Dispose()

# 2. HP Full 100% (Green)
$b100 = CreateCompositedBar "scratch\test_track.png" "scratch\test_fill.png" 1.0 ([System.Drawing.Color]::FromArgb(255, 60, 220, 80))
$b100.Save("$destDir\preview_bar_100pct_hp.png", [System.Drawing.Imaging.ImageFormat]::Png)
$b100.Dispose()

# 3. Camp Ambush 35% (Amber/Orange)
$b35 = CreateCompositedBar "scratch\test_track.png" "scratch\test_fill.png" 0.35 ([System.Drawing.Color]::FromArgb(255, 255, 165, 2))
$b35.Save("$destDir\preview_bar_35pct_ambush.png", [System.Drawing.Imaging.ImageFormat]::Png)
$b35.Dispose()

# 4. Critical Ambush / Low HP 75% (Red)
$b75 = CreateCompositedBar "scratch\test_track.png" "scratch\test_fill.png" 0.75 ([System.Drawing.Color]::FromArgb(255, 245, 60, 60))
$b75.Save("$destDir\preview_bar_75pct_red.png", [System.Drawing.Imaging.ImageFormat]::Png)
$b75.Dispose()

Write-Output 'Previews generated successfully!'
