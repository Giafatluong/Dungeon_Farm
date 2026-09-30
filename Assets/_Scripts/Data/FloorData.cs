using UnityEngine;

[CreateAssetMenu(fileName = "New Floor Data", menuName = "Dungeon/Floor Data")]
public class FloorData : ScriptableObject
{
    [Header("Floor Information")]
    public string floorName = "Floor 1";
    public int floorNumber = 1;

    [Header("Level Design - Thứ tự các Stage do bạn tự sắp xếp")]
    [Tooltip("Danh sách các Stage/Wave được xếp lần lượt theo đúng bài tập Level Design của bạn")]
    public WaveData[] waves;
}