$lines = Get-Content 'Assets\_Scripts\Base\KitchenUI.cs'
$vnRegex = '[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴÈÉẸẺẼÊỀẾỆỂỄÌÍỊỈĨÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠÙÚỤỦŨƯỪỨỰỬỮỲÝỴỶỸĐ]'

$english = @()
for ($i = 0; $i -lt $lines.Count; $i++) {
    $l = $lines[$i]
    if ($l -match '\".*?\"') {
        $matches = [regex]::Matches($l, '\"(.*?)\"')
        foreach ($m in $matches) {
            $val = $m.Groups[1].Value
            if ($val.Length -gt 2 -and $val -notmatch '^[0-9\s#<>=/:+\-.,()_]+$' -and $val -notmatch 'Horizontal|Vertical|Untagged|Default|Comp_|RiskGauge|Btn_|Item|Slot|Panel|Button|Text|Image|Transform|Slider|Canvas') {
                try { $dec = [System.Text.RegularExpressions.Regex]::Unescape($val) } catch { $dec = $val }
                if ($dec -match '[a-zA-Z]{3,}' -and $dec -notmatch $vnRegex) {
                    $english += ($i + 1).ToString() + ': ' + $l.Trim()
                }
            }
        }
    }
}

Write-Output ("Potential English in KitchenUI.cs: " + $english.Count)
$english | Select-Object -First 30
