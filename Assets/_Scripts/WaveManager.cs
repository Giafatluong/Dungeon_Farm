using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private FloorData floorData;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private HealthBarManager healthBarManager;

    private int currentWaveIndex = -1;
    private bool isTransitioning;

    public WaveData CurrentWave
    {
        get
        {
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

        currentWaveIndex = 0;
        StartWave();
    }

    public void Continue()
    {
        if (isTransitioning) return;

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

        float delay = CurrentWave != null ? CurrentWave.transitionDelay : 0f;

        yield return new WaitForSeconds(delay);

        currentWaveIndex++;
        StartWave();

        isTransitioning = false;
    }

    private void StartWave()
    {
        WaveData wave = CurrentWave;

        if (wave == null) return;

        Debug.Log(
            "Start Wave: " +
            currentWaveIndex +
            " | Type: " +
            wave.waveType
        );

        if (wave.waveType == WaveData.WaveType.Combat)
        {
            SpawnEnemies(wave);
            combatManager.StartCombat();
        }
    }

    private void SpawnEnemies(WaveData wave)
    {
        if (wave.enemyPrefabs == null || wave.enemyPrefabs.Length == 0)
        {
            Debug.Log("Wave has no enemy");
            return;
        }

        EnemyStats[] newEnemies = new EnemyStats[wave.enemyPrefabs.Length];

        for (int i = 0; i < wave.enemyPrefabs.Length; i++)
        {
            if (wave.enemyPrefabs[i] == null) continue;

            GameObject enemyObject = Instantiate(
                wave.enemyPrefabs[i],
                enemySpawnPoint.position,
                Quaternion.identity
            );

            EnemyStats enemyStats = enemyObject.GetComponent<EnemyStats>();

            if (enemyStats != null)
            {
                newEnemies[i] = enemyStats;
            }
        }

        combatManager.SetEnemies(newEnemies);

        healthBarManager.SetEnemies(newEnemies);
        healthBarManager.CreateHealthBars();
    }

    public int GetCurrentWaveIndex()
    {
        return currentWaveIndex;
    }
}