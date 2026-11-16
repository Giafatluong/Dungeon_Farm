$ErrorActionPreference = "Stop"

function Replace-InFile {
    param (
        [string]$Path,
        [hashtable]$Replacements
    )
    $content = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    $modified = $false
    foreach ($k in $Replacements.Keys) {
        if ($content.Contains($k)) {
            $content = $content.Replace($k, $Replacements[$k])
            $modified = $true
            Write-Host "[$Path] Replaced: $k"
        } else {
            Write-Host "[$Path] Note: Target string not found literally: $k"
        }
    }
    if ($modified) {
        [System.IO.File]::WriteAllText($Path, $content, [System.Text.Encoding]::UTF8)
        Write-Host "[$Path] Successfully saved changes."
    }
}

Write-Host "=== 1. Updating KitchenCanvas.prefab ==="
$kitchenCanvasMap = @{
    "FARMSTEAD KITCHEN STATION (B\u1EBEP N\u1EA4U GIA \u0110\xCCNH & S\u1ED4 TAY \u1EA8M TH\u1EF0C)" = "B\u1EBEP N\u1EA4U GIA \u0110\xCCNH & S\u1ED4 TAY \u1EA8M TH\u1EF0C"
    "H\u1ED3i 60 HP & +45 \u0110\u1ED9 No | C\u1EA7n: C\u00E0 chua x2" = "H\u1ED3i 60 M\u00E1u & +45 \u0110\u1ED9 No | C\u1EA7n: C\u00E0 chua x2"
    "B\u1ECE NGUY\xCAN LI\u1EC6U V\xC0O N\u1ED2I (T\u1ED0I \u0110A 4)" = "B\u1ECE NGUY\xCAN LI\u1EC6U V\xC0O N\u1ED2I"
}
Replace-InFile -Path "Assets/_Assets/_Prefabs/KitchenCanvas.prefab" -Replacements $kitchenCanvasMap

Write-Host "=== 2. Updating Base.unity ==="
$baseMap = @{
    "FARMSTEAD KITCHEN STATION (B\u1EBEP N\u1EA4U GIA \u0110\xCCNH & S\u1ED4 TAY \u1EA8M TH\u1EF0C)" = "B\u1EBEP N\u1EA4U GIA \u0110\xCCNH & S\u1ED4 TAY \u1EA8M TH\u1EF0C"
    "H\u1ED3i 60 HP & +45 \u0110\u1ED9 No | C\u1EA7n: C\u00E0 chua x2" = "H\u1ED3i 60 M\u00E1u & +45 \u0110\u1ED9 No | C\u1EA7n: C\u00E0 chua x2"
    "B\u1ECE NGUY\xCAN LI\u1EC6U V\xC0O N\u1ED2I (T\u1ED0I \u0110A 4)" = "B\u1ECE NGUY\xCAN LI\u1EC6U V\xC0O N\u1ED2I"
    "'Mode: [ WINDOWED ]'" = "'Ch\u1EBF \u0110\u1ED9: [ C\u1EECA S\u1ED4 ]'"
    "'Sound: [ ON 100% ]'" = "'\xC2m Thanh: [ B\u1EACT 100% ]'"
    "'[*] DAWN - DAY 01'" = "'[*] B\xCCNH MINH - NG\xC0Y 01'"
    "START NEW DAY  (Space)" = "B\u1EACT \u0110\u1EA6U NG\xC0Y M\u1EDAI"
    "BACK TO MENU" = "QUAY L\u1EA0I MENU"
    "'[ HOW TO PLAY & CONTROLS ]'" = "'[ H\u01AF\u1EDANG D\u1EAAN & \u0110I\u1EC0U KHI\u1EC2N ]'"
    "'[ SETTINGS ]'" = "'[ C\xC0I \u0110\u1EB6T ]'"
    "'[ DUNGEON ]'" = "'[ H\u1EA6M NG\u1EE5C ]'"
    "<b><color=#FFCC44>[NO BLESSING CHOSEN]</color></b> Choose a blessing to" = "<b><color=#FFCC44>[CH\u01AFA CH\u1ECCN PH\u01AF\u1EDAC L\xC0NH]</color></b> H\xE3y ch\u1ECDn mét ph\u01B0\u1EDBc l\xE0nh cho chuy\u1EBFn \u0111i"
    "Change Build" = "\u0110\u1ED5i Ph\u01B0\u1EDBc L\xE0nh"
    "Drag and drop items from your backpack or click requirement slots to offer." = "K\xE9o th\u1EA3 v\u1EADt ph\u1EA9m t\u1EEB ba l\xF4 ho\u1EB7c nh\u1EA5p v\xE0o \xF4 y\xEAu c\u1EA7u \u0111\u1EC3 d\xE2ng l\u1EC5."
    "DIVINE OFFERINGS LIST" = "DANH S\xC1CH L\u1EC4 V\u1EACT C\u1ED4 X\u01AFA"
    "PLAYER INVENTORY" = "T\xDAI \u0110\u1ED2 NH\xC2N V\u1EACT"
    "<b>Blessed Permanent Stats:</b>   <color=#FF7777>ATK: 10 (+0)</color>" = "<b>Ch\u1EC9 S\u1ED1 V\u0129nh Vi\u1EC5n:</b>   <color=#FF7777>C\xF4ng: 10 (+0)</color>"
    "A peaceful night has passed. New changes have arrived across the farm and`n    the world outside." = "M\u1ED9t \u0111\xEAm y\xEAn b\xECnh \u0111\xE3 tr\xF4i qua. Nh\u1EEFng \u0111i\u1EC1u má»›i m\u1EBB \u0111ang ch\u1EDD \u0111\xF3n tr\xEAn n\xF4ng tr\u1EA1i v\xE0 h\u1EA7m ng\u1EE5c."
    "A peaceful night has passed. New changes have arrived across the farm and`r`n    the world outside." = "M\u1ED9t \u0111\xEAm y\xEAn b\xECnh \u0111\xE3 tr\xF4i qua. Nh\u1EEFng \u0111i\u1EC1u má»›i m\u1EBB \u0111ang ch\u1EDD \u0111\xF3n tr\xEAn n\xF4ng tr\u1EA1i v\xE0 h\u1EA7m ng\u1EE5c."
}
Replace-InFile -Path "Assets/_Scenes/Base.unity" -Replacements $baseMap

Write-Host "=== 3. Updating Dungeon.unity ==="
$dungeonMap = @{
    "CAMP ENCOUNTER" = "TR\u1EA0M NGH\u1EC8 L\u1EECA TR\u1EA0I"
    "A warm campfire burns in the dungeon. Rest, eat, or cook to recover.`n<color=#FFAA55>Warning:" = "L\u1EEDa tr\u1EA1i \u1EA5m \xE1p s\u01B0\u1EDFi \u1EA5m h\u1EA7m ng\u1EE5c. H\xE3y ngh\u1EC9 ng\u01A1i, \u0103n u\u1ED1ng ho\u1EB7c n\u1EA5u n\u01B0\u1EDBng \u0111\u1EC3 há»“i ph\u1EE5c.`n<color=#FFAA55>C\u1EA3nh b\xE1o:"
    "A warm campfire burns in the dungeon. Rest, eat, or cook to recover.`r`n<color=#FFAA55>Warning:" = "L\u1EEDa tr\u1EA1i \u1EA5m \xE1p s\u01B0\u1EDFi \u1EA5m h\u1EA7m ng\u1EE5c. H\xE3y ngh\u1EC9 ng\u01A1i, \u0103n u\u1ED1ng ho\u1EB7c n\u1EA5u n\u01B0\u1EDBng \u0111\u1EC3 há»“i ph\u1EE5c.`r`n<color=#FFAA55>C\u1EA3nh b\xE1o:"
    "Ambush Risk: <b>0%</b>" = "Nguy c\u01A1 ph\u1EE5c k\xEDch: <b>0%</b>"
    "The campsite is quiet and safe." = "Khu tr\u1EA1i \u0111ang y\xEAn t\u0129nh v\xE0 an to\xE0n."
    "HP: 100/100   |   Fullness: 100/100" = "M\u00E1u: 100/100   |   \u0110\u1ED9 No: 100/100"
    "Buffs: None" = "Hi\u1EC7u \u1EE9ng: Kh\xF4ng c\xF3"
    "EAT FOOD\n(+15% Risk)" = "\u0102N M\xD3N\n(+15% Nguy c\u01A1)"
    "COOK RECIPE\n(+20% Risk)" = "N\u1EA4U N\u01AF\u1EDANG\n(+20% Nguy c\u01A1)"
    "EXERCISE (-25 Full)\n(+35% Risk)" = "T\u1EACP LUY\u1EC6N (-25 No)\n(+35% Nguy c\u01A1)"
    "REST (+25 HP)\n(+10% Risk)" = "NGH\u1EC8 NG\u01A0I (+25 M\u00E1u)\n(+10% Nguy c\u01A1)"
    "CONTINUE FORWARD\n(Next Stage)" = "TI\u1EBEP T\u1EE4C TI\u1EBEN L\xCAN\n(Ch\u1EB7ng Ti\u1EBFp Theo)"
    "RETURN HOME\n(Keep All Loot)" = "TR\u1EDE V\u1EC0 C\u0102N C\u1EE8\n(Gi\u1EEF H\u1EBFt \u0110\u1ED3)"
    "AMBUSH! Monsters detected your campsite!" = "PH\u1EE4C K\xCDCH! Qu\xE1i v\u1EADt \u0111\xE3 ph\xE1t hi\u1EC7n khu tr\u1EA1i!"
    "SELECT FOOD TO EAT" = "CH\u1ECCN M\xD3N \u0110\u1EC2 \u0102N"
    "No food in backpack!\n(Cook meals or harvest food from farm)" = "Kh\xF4ng c\xF3 th\u1EE9c \u0103n trong ba l\xF4!\n(N\u1EA5u m\xF3n ho\u1EB7c thu ho\u1EA1ch t\u1EEB n\xF4ng tr\u1EA1i)"
    "SELECT RECIPE TO COOK" = "CH\u1ECCN C\xD4NG TH\u1EE8C \u0110\u1EC2 N\u1EA4U"
    "No recipes discovered yet!\n(Meet Merchants or offer gifts to unlock recipes)" = "Ch\u01B0a má»Ÿ kh\xF3a c\xF4ng th\u1EE9c n\xE0o!\n(G\u1EB7p Th\u01B0\u01A1ng nh\xE2n ho\u1EB7c D\xE2ng l\u1EC5 T\u01B0\u1EE3ng th\u1EA7n)"
    "TREASURE VAULT - STAGE REWARD" = "KHO B\xC1U - TH\u01AF\u1EDENG V\u01AF\u1EE2T \u1EA2I"
    "An ancient treasure cache has unlocked! Claim seeds, rare crops, and provisions." = "R\u01B0\u01A1ng cá»• x\u01B0a \u0111\xE3 má»Ÿ! Nh\u1EADn h\u1EA1t giá»‘ng, n\xF4ng s\u1EA3n qu\xFD v\xE0 l\u01B0\u01A1ng th\u1EF1c."
    "EXPEDITION GOLDEN TREASURE" = "KHO B\xC1U HO\xC0NG KIM"
    "CHEST CACHE CONTENTS" = "CHI\u1EBEN L\u1EE2I PH\u1EA8M TRONG R\u01AF\u01A0NG"
    "CLAIM ALL" = "NH\u1EACN T\u1EA4T C\u1EA2"
    "Items collected will be safely stored in your backpack." = "V\u1EADt ph\u1EA9m thu th\u1EADp sáº½ \u0111\u01B0\u1EE3c chuy\u1EC3n an to\xE0n v\xE0o ba l\xF4 cá»§a b\u1EA1n."
    "CONTINUE EXPEDITION >>" = "TI\u1EBEP T\u1EE4C TH\xC1M HI\u1EC2M >>"
    "PROCEED" = "TI\u1EBEP T\u1EE4C"
    "'Enter Stage 1 [Space / ->]'" = "'V\xE0o Ch\u1EB7ng 1 [Ph\xEDm C\xE1ch / ->]'"
    "CONTINUE >>" = "TI\u1EBEP T\u1EE4C >>"
    "ANCIENT BLESSINGS UNLOCKED" = "M\u1EDE KH\xD3A PH\u01AF\u1EDAC L\xC0NH C\u1ED4 X\u01AFA"
    "PERMANENT STAT ENHANCEMENT" = "CH\u1EC8 S\u1ED1 V\u0128NH VI\u1EC4N NH\u1EACN \u0110\u01AF\u1EE2C"
    "SAFELY SECURED LOOT" = "V\u1EACT PH\u1EA8M MANG V\u1EC0 AN TO\xC0N"
    "DUNGEON EXPEDITION SUMMARY" = "T\u1ED4NG K\u1EBET CHI\u1EBEN D\u1ECACH H\u1EA6M NG\u1EE5C"
    "EXPEDITION STATS" = "TH\u1ED0NG K\xCA CHI\u1EBEN D\u1ECACH"
    "Overview of progress, stages cleared, and all secured expedition loot." = "T\u1ED5ng quan ti\u1EBFn tr\xECnh, sá»‘ ch\u1EB7ng \u0111\xE3 v\u01B0\u1EE3t v\xE0 chi\u1EBFn l\u1EE3i ph\u1EA9m mang v\u1EC1 an to\xE0n."
    "'NEXT FLOOR [->]'" = "'T\u1EA6NG TI\u1EBEP THEO [->]'"
    "RETURN TO FARM" = "TR\u1EDE V\u1EC0 N\xD4NG TR\u1EA0I"
    "'CONTINUE [->]'" = "'TI\u1EBEP T\u1EE4C [->]'"
    "'RETURN TO FARM  [ -> ]'" = "'TR\u1EDE V\u1EC0 N\xD4NG TR\u1EA0I [ -> ]'"
    "'[Time] New Day (+1 Day)'" = "'[Th\u1EDDi Gian] Ng\xE0y M\u1EDBi (+1 Ng\xE0y)'"
    "'[Stages] 0/5'" = "'[Ch\u1EB7ng] 0/5'"
    "'[Vitality] 100/100 HP'" = "'[Sinh L\u1EF1c] 100/100 HP'"
    "'[Boss] Unmet'" = "'[Th\u1EE7 L\u0129nh] Ch\u01B0a G\u1EB7p'"
    "WANDERING MERCHANT - DUNGEON OUTPOST" = "TH\u01AF\u01A0NG NH\xC2N DU M\u1EE4C - TI\u1EC0N TI\xCAU H\u1EA6M NG\u1EE5C"
    "Welcome traveler! What would you like to barter today?" = "Ch\xE0o má»«ng l\u1EEF kh\xE1ch! H\xF4m nay b\u1EA1n mu\u1ED1n trao \u0111\u1ED5i g\xEC n\xE0o?"
    "A mysterious trader braving the dungeon. Barter seeds, recipes, and survival" = "Th\u01B0\u01A1ng gia b\xED \u1EA9n d\u0169ng c\u1EA3m gi\u1EEFa h\u1EA7m ng\u1EE5c. Trao \u0111\u1ED5i h\u1EA1t giá»‘ng, c\xF4ng th\u1EE9c v\xE0 \u0111á»“ sinh tá»“n"
    "BA L\xD4 NG\u01AF\u1EDCI CH\u01A0I (T\xDAI \u0110\u1ED2)" = "BA L\xD4 NG\u01AF\u1EDCI CH\u01A0I"
    "⏭️ H\u1EBET L\u01AF\u1EE2T (Space)" = "⏭️ H\u1EBET L\u01AF\u1EE2T"
    "\xDD \u0111\u1ECBnh: T\u1EA5n c\xF4ng (4 DMG)" = "\xDD \u0111\u1ECBnh: T\u1EA5n c\xF4ng (4 S\xE1t th\u01B0\u01A1ng)"
    "CHI\u1EBEN L\u1EE2I PH\u1EA8M (R\u01A0I T\u1EEA QU\xC1I)" = "CHI\u1EBEN L\u1EE2I PH\u1EA8M R\u01A0I RA"
}
Replace-InFile -Path "Assets/_Scenes/Dungeon.unity" -Replacements $dungeonMap
