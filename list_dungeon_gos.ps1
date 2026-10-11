$dungeon = Get-Content 'Assets\_Scenes\Dungeon.unity'

$gameObjects = @{}
$currentId = ""
$currentName = ""
$currentFather = ""

for ($i = 0; $i -lt $dungeon.Count; $i++) {
    $line = $dungeon[$i]
    if ($line -match '--- !u!1 &(\d+)') {
        $currentId = $matches[1]
    }
    if ($line -match 'm_Name:\s*(.*)') {
        if ($currentId -ne "") {
            $gameObjects[$currentId] = $matches[1]
        }
    }
}

Write-Host "Total GameObjects: $($gameObjects.Count)"
foreach ($kv in $gameObjects.GetEnumerator()) {
    Write-Host "$($kv.Key) : $($kv.Value)"
}
