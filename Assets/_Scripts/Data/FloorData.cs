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
}