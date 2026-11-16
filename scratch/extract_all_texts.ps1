$files = Get-ChildItem -Path 'Assets\_Scripts' -Recurse -Filter '*.cs'

$results = @()

foreach ($f in $files) {
    $lines = Get-Content $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '\.text\s*=\s*[\$]?\"([^\"\r\n]+)\"') {
            $extracted = $matches[1]
            if ($extracted.Length -gt 1 -and $extracted -notmatch '^[0-9\s/\:\+\-\%\.\,\(\)\{\}\<\>\=\#\*_]+$') {
                $results += [PSCustomObject]@{
                    File = $f.Name
                    Line = ($i + 1)
                    Text = $extracted
                    Full = $line.Trim()
                }
            }
        }
    }
}

Write-Output ("Total text assignments found: " + $results.Count)
$results | Export-Csv -Path 'scratch\all_ui_texts.csv' -NoTypeInformation -Encoding UTF8
Write-Output "Exported to scratch\all_ui_texts.csv"
