
using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private FloorData floorData;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private Transform[] combatPositions;
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private HealthBarManager healthBarManager;
    [SerializeField] private EnemyTargetSelector targetSelector;

    private int currentWaveIndex = -1;
    private bool isTransitioning;

    public WaveData CurrentWave
    {
        get
        {
            if (floorData == null)
                return null;

            if (floorData.waves == null)
                return null;

            if (currentWaveIndex < 0 || currentWaveIndex >= floorData.waves.Length)
                return null;

            return floorData.waves[currentWaveIndex];
        }
    }

    private void Start()
    {
        StartFirstWave();
    }

    public void StartFirstWave()
    {
        if (floorData == null)
        {
            Debug.Log("Floor Data is NULL");
            return;
        }

        if (floorData.waves == null || floorData.waves.Length == 0)
        {
            Debug.Log("Floor has no Wave");
            return;
        }

        if (combatManager == null)
        {
            Debug.Log("Combat Manager is NULL");
            return;
        }

        if (targetSelector == null)
        {
            Debug.Log("Target Selector is NULL");
            return;
        }

        currentWaveIndex = 0;
        StartWave();
    }

    public void Continue()
    {
        if (isTransitioning)
            return;

        if (CurrentWave == null)
            return;

        if (currentWaveIndex >= floorData.waves.Length - 1)
        {
            Debug.Log("Floor Complete");
            return;
        }

        StartCoroutine(NextWave());
    }

    private IEnumerator NextWave()
    {
        isTransitioning = true;

        float delay = CurrentWave != null
            ? CurrentWave.transitionDelay
            : 0f;

        yield return new WaitForSeconds(delay);

        currentWaveIndex++;
        StartWave();

        isTransitioning = false;
    }

    private void StartWave()
    {
        WaveData wave = CurrentWave;

        if (wave == null)
            return;

        Debug.Log(
            "Start Wave: " +
            currentWaveIndex +
            " | Type: " +
            wave.waveType
        );

        if (wave.waveType == WaveData.WaveType.Combat)
        {
            SpawnEnemies(wave);
        }
    }

    private void SpawnEnemies(WaveData wave)
    {
        if (wave.enemyPrefabs == null || wave.enemyPrefabs.Length == 0)
        {
            Debug.Log("Wave has no enemy");
            return;
        }

        if (enemySpawnPoint == null)
        {
            Debug.Log("Enemy Spawn Point is NULL");
            return;
        }

        if (combatPositions == null || combatPositions.Length == 0)
        {
            Debug.Log("Combat Positions are NULL or empty");
            return;
        }

        int enemyCount = Mathf.Min(
            wave.enemyPrefabs.Length,
            combatPositions.Length
        );

        EnemyStats[] newEnemies = new EnemyStats[enemyCount];

        for (int i = 0; i < enemyCount; i++)
        {
            if (wave.enemyPrefabs[i] == null)
                continue;

            if (combatPositions[i] == null)
            {
                Debug.Log("Combat Position " + i + " is NULL");
                continue;
            }

            GameObject enemyObject = Instantiate(
                wave.enemyPrefabs[i],
                enemySpawnPoint.position,
                Quaternion.identity
            );

            enemyObject.transform.position = combatPositions[i].position;

            EnemyStats enemyStats = enemyObject.GetComponent<EnemyStats>();

            if (enemyStats == null)
            {
                Debug.Log(
                    "Enemy prefab does not contain EnemyStats: " +
                    wave.enemyPrefabs[i].name
                );

                Destroy(enemyObject);
                continue;
            }

            enemyStats.targetSelector = targetSelector;

            newEnemies[i] = enemyStats;
        }

        combatManager.SetEnemies(newEnemies);

        if (healthBarManager != null)
        {
            healthBarManager.SetEnemies(newEnemies);
            healthBarManager.CreateHealthBars();
        }

        combatManager.StartCombat();
    }

    public int GetCurrentWaveIndex()
    {
        return currentWaveIndex;
    }
}