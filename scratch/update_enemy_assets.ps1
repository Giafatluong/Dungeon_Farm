$enemyNames = @{
    'Assets\_Assets\ScriptableObjects\Enemy\Bat.asset' = '"D\u01A1i Hang \u0110\u1ED9ng"'
    'Assets\_Assets\ScriptableObjects\Enemy\Bombschroom.asset' = '"N\u1EA5m Ph\u00E1t N\u1ED5"'
    'Assets\_Assets\ScriptableObjects\Enemy\Boss_Skeleton_King.asset' = '"Vua X\u01B0\u01A1ng C\u1ED5 \u0110\u1EA1i (Tr\u00F9m)"'
    'Assets\_Assets\ScriptableObjects\Enemy\Flying.asset' = '"Ti\u1EC3u Qu\u1EF7 Bay"'
    'Assets\_Assets\ScriptableObjects\Enemy\FlyingRider.asset' = '"K\u1EF5 S\u0129 B\u00F3ng \u0110\u00EAm (Tr\u00F9m)"'
    'Assets\_Assets\ScriptableObjects\Enemy\ME1.asset' = '"Chi\u1EBFn Binh Goblin"'
    'Assets\_Assets\ScriptableObjects\Enemy\ME2.asset' = '"Orc Gi\u00E1p S\u1EAFt"'
    'Assets\_Assets\ScriptableObjects\Enemy\Range.asset' = '"Cung Th\u1EE7 B\u00F3ng \u0110\u00EAm"'
    'Assets\_Assets\ScriptableObjects\Enemy\Rat.asset' = '"Chu\u1ED9t C\u1ED1ng Ng\u1EA7m"'
    'Assets\_Assets\ScriptableObjects\Enemy\Skeleton.asset' = '"B\u1ED9 X\u01B0\u01A1ng C\u1ED5"'
    'Assets\_Assets\ScriptableObjects\Enemy\Skeleton_Bowman.asset' = '"X\u01B0\u01A1ng Cung Th\u1EE7"'
    'Assets\_Assets\ScriptableObjects\Enemy\Slime.asset' = '"Qu\u00E1i Ch\u1EA5t Nh\u1EA7y"'
    'Assets\_Assets\ScriptableObjects\Enemy\SlimeBoss.asset' = '"Vua Ch\u1EA5t Nh\u1EA7y (Tr\u00F9m)"'
}

foreach ($p in $enemyNames.Keys) {
    if (Test-Path $p) {
        $content = [System.IO.File]::ReadAllText($p, [System.Text.Encoding]::UTF8)
        $vnName = $enemyNames[$p]
        $content = [regex]::Replace($content, 'enemyName:\s*.*', "enemyName: $vnName")
        [System.IO.File]::WriteAllText($p, $content, [System.Text.Encoding]::UTF8)
        Write-Output "Updated $p to $vnName"
    }
}
