using UnityEngine;

[CreateAssetMenu(fileName = "New Floor Data", menuName = "Dungeon/Floor Data")]
public class FloorData : ScriptableObject
{
    [Header("Floor Information")]
    public string floorName = "Floor 1";
    public int floorNumber = 1;

    [Header("Level Design - Stage Sequence")]
    [Tooltip("List of stages/waves sequenced in exact order for this floor")]
    public WaveData[] waves;

    [Header("Floor Clear Special Rewards")]
    [Tooltip("Boss name for victory screen presentation")]
    public string bossName = "Dungeon Boss";
    [Tooltip("Special bonus loot awarded upon clearing this floor")]
    public ItemSlot[] clearRewardItems;
    [Tooltip("Permanent ATK bonus awarded on floor clear")]
    public int bonusPermanentATK = 0;
    [Tooltip("Permanent DEF bonus awarded on floor clear")]
    public int bonusPermanentDEF = 0;
    [Tooltip("Permanent Max HP bonus awarded on floor clear")]
    public int bonusPermanentMaxHP = 0;
    [Tooltip("Permanent Speed bonus awarded on floor clear")]
    public int bonusPermanentSpeed = 0;
    [Tooltip("Custom victory title or congratulations message")]
    public string victoryTitle = "";
    [TextArea(2, 4)]
    [Tooltip("Custom victory subtitle / story summary")]
    public string victoryDescription = "";
}