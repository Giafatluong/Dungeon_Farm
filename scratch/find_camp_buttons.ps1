$lines = Get-Content 'Assets\_Scenes\Dungeon.unity'
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'm_Name:\s*(.+Button.*|Btn.*)') {
        $name = $matches[1]
        for ($j = [Math]::Max(0, $i - 10); $j -lt [Math]::Min($lines.Count, $i + 25); $j++) {
            if ($lines[$j] -match 'm_SizeDelta:\s*\{x:\s*([^,]+),\s*y:\s*([^}]+)\}') {
                $w = $matches[1]
                $h = $matches[2]
                Write-Output ("Line " + $i + ": " + $name + " -> Size: " + $w + " x " + $h)
                break
            }
        }
    }
}
