using UnityEngine;

/// <summary>
/// Data model for representing any status, buff, debuff, or passive blessing
/// in the Slay the Spire-style Buff Bar and Hover Tooltip.
/// </summary>
public class BuffDisplayInfo
{
    public string id;
    public string title;
    public string category; // "[CƯỜNG HÓA]", "[BẤT LỢI]", "[PHÒNG THỦ]", "[TRẠNG THÁI]", "[PHƯỚC LÀNH]"
    public Color categoryColor;
    public Sprite icon;
    public string stackBadge; // Number or text displayed on the icon (e.g. "2", "5", "+3")
    public string durationText; // "⏱ Còn 2 Lượt", "⏱ Hết Trận Này", "⏱ Cả Tầng"
    public string description; // Full detailed tooltip explanation
}

