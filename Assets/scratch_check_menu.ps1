$path = "Assets\_Scenes\Base.unity"
$lines = [System.IO.File]::ReadAllLines($path)
for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match 'm_Name: MainMenuCanvas' -or $lines[$i] -match 'm_Name: MainMenu_Root' -or $lines[$i] -match 'm_EditorClassIdentifier: Assembly-CSharp::MainMenuUI') {
        for ($j = [Math]::Max(0, $i - 10); $j -lt [Math]::Min($lines.Length, $i + 40); $j++) {
            Write-Output "$($j): $($lines[$j])"
        }
        Write-Output "-------------------------"
    }
}

