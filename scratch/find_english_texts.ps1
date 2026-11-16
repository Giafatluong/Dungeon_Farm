$csv = Import-Csv 'scratch\all_ui_texts.csv'

# Common Vietnamese vowels with diacritics
$vnRegex = '[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ]'

$nonVn = @()
foreach ($row in $csv) {
    # If the text has letters but NO Vietnamese characters, it might be English!
    if ($row.Text -match '[a-zA-Z]{3,}' -and $row.Text -notmatch $vnRegex) {
        # Ignore things like TMP tags only or pure codes
        $cleaned = $row.Text -replace '<[^>]+>', '' -replace '\{[^}]+\}', ''
        if ($cleaned -match '[a-zA-Z]{3,}') {
            $nonVn += $row
        }
    }
}

Write-Output ("Found potentially English UI texts: " + $nonVn.Count)
foreach ($item in $nonVn) {
    Write-Output ($item.File + ":" + $item.Line + " -> " + $item.Text)
}
