using UnityEngine;

[CreateAssetMenu(fileName = "New Wave Data", menuName = "Dungeon/Wave Data")]
public class WaveData : ScriptableObject
{
    public enum WaveType
    {
        Combat,
        Reward,
        Camp,
        Event,
        Boss
    }

    [Header("Encounter Type")]
    public WaveType waveType = WaveType.Combat;

    [Header("Level Design - Cố định hay Ngẫu nhiên")]
    [Tooltip("Bỏ tích: Màn chơi cố định 100% theo Level Design của bạn.\nTích chọn: Màn chơi ngẫu nhiên quái theo công thức/pool.")]
    public bool isRandom = false;

    [Header("Fixed Enemies (Dành cho isRandom = false)")]
    [Tooltip("Danh sách quái cố định xuất hiện (tối đa 3 quái tương ứng 3 vị trí)")]
    public GameObject[] enemyPrefabs;

    [Header("Random Enemies (Dành cho isRandom = true)")]
    [Tooltip("Pool các loại quái để hệ thống bốc ngẫu nhiên khi vào màn này")]
    public GameObject[] randomEnemyPool;
    [Range(1, 3)] public int minRandomEnemies = 1;
    [Range(1, 3)] public int maxRandomEnemies = 3;

    [Header("Transition")]
    public float transitionDelay = 1f;

    /// <summary>
    /// Lấy danh sách quái thực tế để spawn (xử lý cả cố định lẫn ngẫu nhiên)
    /// </summary>
    public GameObject[] GetEnemiesToSpawn()
    {
        if (!isRandom)
        {
            if (enemyPrefabs != null && enemyPrefabs.Length > 0)
            {
                return (GameObject[])enemyPrefabs.Clone();
            }
            if (randomEnemyPool != null && randomEnemyPool.Length > 0)
            {
                int fallbackCount = Mathf.Clamp(Random.Range(minRandomEnemies, maxRandomEnemies + 1), 1, 3);
                GameObject[] fallbackResult = new GameObject[fallbackCount];
                for (int i = 0; i < fallbackCount; i++)
                {
                    fallbackResult[i] = randomEnemyPool[Random.Range(0, randomEnemyPool.Length)];
                }
                return fallbackResult;
            }
            return new GameObject[0];
        }

        // Nếu là ngẫu nhiên
        GameObject[] sourcePool = (randomEnemyPool != null && randomEnemyPool.Length > 0)
            ? randomEnemyPool
            : enemyPrefabs;

        if (sourcePool == null || sourcePool.Length == 0)
        {
            return new GameObject[0];
        }

        int count = Mathf.Clamp(Random.Range(minRandomEnemies, maxRandomEnemies + 1), 1, 3);
        GameObject[] result = new GameObject[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = sourcePool[Random.Range(0, sourcePool.Length)];
        }
        return result;
    }
}