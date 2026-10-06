using UnityEngine;

[CreateAssetMenu(fileName = "New Wave Data", menuName = "Dungeon/Wave Data")]
public class WaveData : ScriptableObject
{
    public enum WaveType
    {
        Combat = 0,
        Reward = 1,
        Camp = 2,
        Event = 3,
        Boss = 4,
        Random = 5, // Procedural Random Area
        Merchant = 6 // Dedicated Wandering Merchant Encounter
    }

    [Header("Encounter Type")]
    public WaveType waveType = WaveType.Combat;

    [Header("Random Wave (Dynamic Score Balance)")]
    [Tooltip("Target floor score minimum according to GDD (Combat=+1, Event=-1, Reward=-2, Camp=0)")]
    public int minTargetScore = 0;
    [Tooltip("Target floor score maximum according to GDD")]
    public int maxTargetScore = 4;
    [HideInInspector] public bool isRandomArea = false;

    [Header("Single Combat Wave - Fixed vs Random Enemies")]
    [Tooltip("Unchecked: Fixed enemies in enemyPrefabs.\nChecked: Random enemies picked from randomEnemyPool.")]
    public bool isRandom = false;

    [Header("Fixed Enemies (For isRandom = false)")]
    [Tooltip("List of fixed enemy prefabs to spawn (up to 3 enemies for 3 positions)")]
    public GameObject[] enemyPrefabs;

    [Header("Random Enemies (For isRandom = true)")]
    [Tooltip("Pool of enemy prefabs to randomly draw from")]
    public GameObject[] randomEnemyPool;
    [Range(1, 3)] public int minRandomEnemies = 1;
    [Range(1, 3)] public int maxRandomEnemies = 3;

    [Header("Reward Wave Settings")]
    [Tooltip("Optional custom items dropped by the treasure chest in this reward wave.")]
    public ItemSlot[] customRewardLoot;

    [Header("Transition")]
    public float transitionDelay = 1f;

    /// <summary>
    /// Returns the array of enemy prefabs to spawn (handles both fixed and randomized encounters).
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

        // If randomized
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