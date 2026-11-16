$path = "Assets/_Assets/_Prefabs/KitchenCanvas.prefab"
$content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

# Replace FARMSTEAD KITCHEN STATION (BẾP NẤU GIA ĐÌNH & SỔ TAY ẨM THỰC)
$oldText = "FARMSTEAD KITCHEN STATION (BẾP NẤU GIA ĐÌNH & SỔ TAY ẨM THỰC)"
$newText = "BẾP NẤU GIA ĐÌNH & SỔ TAY ẨM THỰC"
if ($content.Contains($oldText)) {
    $content = $content.Replace($oldText, $newText)
    Write-Host "Replaced kitchen title in prefab"
} else {
    Write-Host "Old text not found literally, searching..."
}

# Replace Hồi 60 HP & +45 Độ No
$oldSub = "Hồi 60 HP & +45 Độ No | Cần: Cà chua x2"
$newSub = "Hồi 60 Máu & +45 Độ No | Cần: Cà chua x2"
if ($content.Contains($oldSub)) {
    $content = $content.Replace($oldSub, $newSub)
    Write-Host "Replaced recipe subtitle in prefab"
}

# Replace BỎ NGUYÊN LIỆU VÀO NỒI (TỐI ĐA 4)
$oldMax = "BỎ NGUYÊN LIỆU VÀO NỒI (TỐI ĐA 4)"
$newMax = "BỎ NGUYÊN LIỆU VÀO NỒI"
if ($content.Contains($oldMax)) {
    $content = $content.Replace($oldMax, $newMax)
    Write-Host "Replaced max ingredients subtitle in prefab"
}

[System.IO.File]::WriteAllText($path, $content, [System.Text.Encoding]::UTF8)
