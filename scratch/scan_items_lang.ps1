$items = Get-ChildItem -Path 'Assets\_Assets\ScriptableObjects' -Recurse -Filter '*.asset'
$vnRegex = '[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ]'

$found = @()
foreach ($item in $items) {
    $lines = Get-Content $item.FullName
    foreach ($l in $lines) {
        if ($l -match '^\s*(itemName|name|description|title):\s*(.+)$') {
            $f = $matches[1]
            $v = $matches[2]
            if ($v.Length -gt 2 -and $v -notmatch '^[\d\s\.\,\-\:\/]+$' -and $v -notmatch $vnRegex -and $v -notmatch 'GUID|fileID|MonoBehaviour') {
                $found += "$($item.Name) ($f): $v"
            }
        }
    }
}

Write-Output ("Items/Assets without Vietnamese: " + $found.Count)
$found | Select-Object -First 30
