using UnityEngine;

[CreateAssetMenu(fileName = "New Floor Data", menuName = "Dungeon/Floor Data")]
public class FloorData : ScriptableObject
{
    public string floorName;
    public WaveData[] waves;
}