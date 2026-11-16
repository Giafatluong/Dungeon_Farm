$assetFiles = Get-ChildItem -Path 'Assets\_Assets\ScriptableObjects' -Recurse -Filter '*.asset'

$results = @()
foreach ($f in $assetFiles) {
    $lines = Get-Content $f.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        # Look for name, description, title fields
        if ($line -match '^\s*(itemName|description|blessingName|dialogue|title|enemyName|recipeName|effectDescription):\s*(.*)$') {
            $field = $matches[1]
            $val = $matches[2]
            if ($val.Length -gt 1) {
                try {
                    $decoded = [System.Text.RegularExpressions.Regex]::Unescape($val)
                } catch {
                    $decoded = $val
                }
                $results += [PSCustomObject]@{
                    File = $f.Name
                    Field = $field
                    Val = $decoded
                }
            }
        }
    }
}

$vnRegex = '[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ]'
$nonVn = $results | Where-Object {
    $_.Val -match '[a-zA-Z]{3,}' -and $_.Val -notmatch $vnRegex
}

Write-Output ("Found ScriptableObjects with non-Vietnamese text: " + $nonVn.Count)
foreach ($item in $nonVn) {
    Write-Output ($item.File + " (" + $item.Field + "): " + $item.Val)
}
