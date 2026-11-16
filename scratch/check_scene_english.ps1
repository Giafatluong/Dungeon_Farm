$lines = Get-Content 'Assets\_Scenes\Dungeon.unity'

$results = @()
$currentGo = ""

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -match '^\s*m_Name:\s*(.+)$') {
        $currentGo = $matches[1]
    }
    if ($line -match '^\s*m_text:\s*\"?(.*?)\"?$') {
        $raw = $matches[1]
        if ($raw.Length -gt 1) {
            # Regex unescape
            try {
                $decoded = [System.Text.RegularExpressions.Regex]::Unescape($raw)
            } catch {
                $decoded = $raw
            }
            $results += [PSCustomObject]@{
                Line = ($i + 1)
                GO = $currentGo
                Decoded = $decoded
            }
        }
    }
}

# Find text that contains English words
$englishWords = @(
    '\bAttack\b', '\bDefend\b', '\bTurn\b', '\bVictory\b', '\bDefeat\b', '\bClaim\b',
    '\bCancel\b', '\bConfirm\b', '\bClose\b', '\bBuy\b', '\bSell\b', '\bInventory\b',
    '\bNext\b', '\bBack\b', '\bReturn\b', '\bWave\b', '\bStage\b', '\bFloor\b',
    '\bSelect\b', '\bTarget\b', '\bEnemy\b', '\bReward\b', '\bLevel\b', '\bHealth\b',
    '\bDamage\b', '\bBuff\b', '\bDebuff\b', '\bOptions\b', '\bSettings\b', '\bQuit\b',
    '\bPlay\b', '\bPause\b', '\bResume\b', '\bStart\b', '\bExit\b', '\bLeave\b',
    '\bCost\b', '\bPrice\b', '\bFood\b', '\bEat\b', '\bCook\b', '\bCraft\b',
    '\bStamina\b', '\bHunger\b'
)

$pattern = ($englishWords -join '|')

$found = @()
foreach ($r in $results) {
    if ($r.Decoded -match $pattern) {
        $found += $r
    }
}

Write-Output ("Found scene texts with English words: " + $found.Count)
foreach ($f in $found) {
    Write-Output ($f.Line.ToString() + ' [' + $f.GO + ']: ' + $f.Decoded)
}
