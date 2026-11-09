$scenePath = "Assets\_Scenes\Dungeon.unity"
$lines = Get-Content $scenePath

for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'm_Name:\s*Player\b') {
        Write-Host "Player at line ${i}:"
        for ($k = $i - 10; $k -le $i + 30; $k++) {
            Write-Host "$k : $($lines[$k])"
        }
    }
}

