$lines = Get-Content 'Assets\_Scenes\Dungeon.unity'
$btnIds = @('1859631995', '945154304', '1221912986', '275609426')

foreach ($id in $btnIds) {
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match ('--- !u!114 &' + $id)) {
            Write-Output ("=== Button ID: " + $id + " at line " + $i + " ===")
            # Look up gameobject ID
            $goId = ""
            for ($k = $i; $k -lt [Math]::Min($lines.Count, $i + 15); $k++) {
                if ($lines[$k] -match 'm_GameObject:\s*\{fileID:\s*(\d+)\}') {
                    $goId = $matches[1]
                    break
                }
            }
            Write-Output ("GameObject ID: " + $goId)
            # Find the GameObject and its RectTransform
            for ($k = 0; $k -lt $lines.Count; $k++) {
                if ($lines[$k] -match ('--- !u!1 &' + $goId)) {
                    Write-Output ("Found GameObject at line " + $k)
                    for ($m = $k; $m -lt $k + 50; $m++) {
                        Write-Output ($lines[$m])
                        if ($lines[$m].StartsWith('--- !u!114')) { break }
                    }
                    break
                }
            }
            break
        }
    }
}
