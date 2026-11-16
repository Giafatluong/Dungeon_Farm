$lines = Get-Content 'Assets\_Scenes\Dungeon.unity'

$inTMP = $false
$currentText = ""
$currentGo = ""
$results = @()

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -match 'm_Name:\s*(.+)$') {
        $currentGo = $matches[1]
    }
    if ($line -match 'm_Script:.*(TextMeshProUGUI|Text)') {
        $inTMP = $true
    }
    if ($inTMP -and $line -match '^\s*m_text:\s*(.*)$') {
        $t = $matches[1]
        $results += [PSCustomObject]@{
            Line = ($i + 1)
            GO = $currentGo
            Text = $t
        }
        $inTMP = $false
    }
    if ($line.StartsWith('--- !u!')) {
        $inTMP = $false
    }
}

$vnRegex = '[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ]'
$englishTMP = $results | Where-Object { 
    $_.Text -match '[a-zA-Z]{3,}' -and $_.Text -notmatch $vnRegex -and $_.Text -notmatch '^(New Text|Text|TextMeshPro|TMP)$'
}

Write-Output ("Found scene texts with potential English: " + $englishTMP.Count)
foreach ($item in $englishTMP) {
    Write-Output ($item.Line.ToString() + ' (' + $item.GO + '): ' + $item.Text)
}
