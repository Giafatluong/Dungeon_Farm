$items = Get-ChildItem -Path 'Assets\_Assets\ScriptableObjects' -Recurse -Filter '*.asset'
$vnRegex = '[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ]'

$realEnglish = @()
foreach ($item in $items) {
    $lines = Get-Content $item.FullName
    foreach ($l in $lines) {
        if ($l -match '^\s*(itemName|enemyName|blessingName|description|title):\s*(.+)$') {
            $f = $matches[1]
            $v = $matches[2]
            # Try to unescape unicode
            try { $decoded = [System.Text.RegularExpressions.Regex]::Unescape($v) } catch { $decoded = $v }
            if ($decoded -notmatch $vnRegex -and $decoded.Length -gt 2 -and $decoded -match '[a-zA-Z]{3,}') {
                $realEnglish += [PSCustomObject]@{
                    File = $item.FullName
                    Name = $item.Name
                    Field = $f
                    Value = $decoded
                }
            }
        }
    }
}

Write-Output ("Real English Asset Fields: " + $realEnglish.Count)
foreach ($re in $realEnglish) {
    Write-Output ($re.Name + " (" + $re.Field + "): " + $re.Value)
}
