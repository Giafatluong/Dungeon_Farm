$replacements = @{
    'Assets\_Assets\ScriptableObjects\Floor\Floor1\Floor_01.asset' = @{ 'floorName: Floor 1' = 'floorName: "H\u1EA7m Ng\u1EE5c C\u1ED5 X\u01B0a (T\u1EA7ng 1)"' }
    'Assets\_Assets\ScriptableObjects\Floor\Floor2\Floor_02.asset' = @{ '\(Floor 2\)' = '(T\u1EA7ng 2)' }
    'Assets\_Assets\ScriptableObjects\Floor\Floor3\Floor_03.asset' = @{ '\(Floor 3\)' = '(T\u1EA7ng 3)' }
    'Assets\_Assets\ScriptableObjects\Floor\Floor4\Floor_04.asset' = @{ '\(Floor 4\)' = '(T\u1EA7ng 4)' }
    'Assets\_Assets\ScriptableObjects\Floor\Floor5\Floor_05.asset' = @{ '\(Floor 5\)' = '(T\u1EA7ng 5)' }
    'Assets\_Assets\ScriptableObjects\Floor\Floor6\Floor_06.asset' = @{ '\(Floor 6\)' = '(T\u1EA7ng 6)' }
    'Assets\_Assets\ScriptableObjects\Floor\Floor7\Floor_07.asset' = @{ '\(Floor 7\)' = '(T\u1EA7ng 7)' }
    'Assets\_Assets\ScriptableObjects\Floor\Floor8\Floor_08.asset' = @{ '\(Floor 8\)' = '(T\u1EA7ng 8)' }
    'Assets\_Assets\ScriptableObjects\Floor\Floor9\Floor_09.asset' = @{ '\(Floor 9\)' = '(T\u1EA7ng 9)' }
    'Assets\_Assets\ScriptableObjects\Floor\Floor10\Floor_10.asset' = @{ '\(Floor 10\)' = '(T\u1EA7ng 10)' }
    'Assets\_Assets\ScriptableObjects\Floor\TestFloor\TestFloor.asset' = @{ '\(Floor 1\)' = '(T\u1EA7ng 1)' }
    'Assets\_Assets\ScriptableObjects\Floor\TutorialFloor\Floor_00_Tutorial.asset' = @{ '\(Tutorial Floor\)' = '(T\u1EA7ng 0)' }
}

foreach ($filePath in $replacements.Keys) {
    if (Test-Path $filePath) {
        $content = [System.IO.File]::ReadAllText($filePath, [System.Text.Encoding]::UTF8)
        $rules = $replacements[$filePath]
        foreach ($pattern in $rules.Keys) {
            $replacement = $rules[$pattern]
            $content = [regex]::Replace($content, $pattern, $replacement)
        }
        [System.IO.File]::WriteAllText($filePath, $content, [System.Text.Encoding]::UTF8)
        Write-Output "Updated $filePath"
    }
}
