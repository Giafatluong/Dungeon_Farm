$files = Get-ChildItem -Path 'Assets\_Scripts' -Recurse -Filter '*.cs'
$patterns = @(
    '"Attack"', '"Defend"', '"End Turn"', '"Victory"', '"Defeat"', '"Claim"',
    '"Cancel"', '"Confirm"', '"Close"', '"Buy"', '"Sell"', '"Inventory"',
    '"Next"', '"Back"', '"Return"', '"Wave ', '"Stage ', '"Floor ',
    '"Select ', '"Target"', '"Round ', '"Game Over"', '"You Win"', '"You Died"',
    '"Reward"', '"Level Up"', '"Level '
)

foreach ($f in $files) {
    $matches = Select-String -Path $f.FullName -Pattern $patterns -SimpleMatch
    if ($matches) {
        Write-Output ("=== " + $f.Name + " ===")
        foreach ($m in $matches) {
            # Filter out comments and log statements if possible, or print line
            $trimmed = $m.Line.Trim()
            if (-not ($trimmed.StartsWith('//') -or $trimmed.StartsWith('/*') -or $trimmed.StartsWith('*'))) {
                Write-Output ("  " + $m.LineNumber + ": " + $trimmed)
            }
        }
    }
}
