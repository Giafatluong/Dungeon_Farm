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

    [Header("Encounter Managers")]
    [SerializeField] private Camp camp;
    [SerializeField] private MerchantEvent merchantEvent;
    [SerializeField] private ProgressController progressController;
    [SerializeField] private FloorGenerator floorGenerator;

    private WaveData[] runtimeWaves;
    private int currentWaveIndex = -1;
    private bool isTransitioning;

    public WaveData CurrentWave
    {
        get
        {
            if (runtimeWaves == null || runtimeWaves.Length == 0)
            {
                if (floorData == null || floorData.waves == null) return null;
                if (currentWaveIndex < 0 || currentWaveIndex >= floorData.waves.Length) return null;
                return floorData.waves[currentWaveIndex];
            }

            if (currentWaveIndex < 0 || currentWaveIndex >= runtimeWaves.Length)
                return null;

            return runtimeWaves[currentWaveIndex];
        }
    }

    private void Start()
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.StartRun(1);
        }

        StartFirstWave();
    }

    public void StartFirstWave()
    {
        // Nếu có FloorGenerator, tạo danh sách wave ngẫu nhiên cân bằng theo GDD
        if (floorGenerator != null)
        {
            runtimeWaves = floorGenerator.GenerateFloor();
        }
        else if (floorData != null && floorData.waves != null)
        {
            runtimeWaves = floorData.waves;
        }

        if (runtimeWaves == null || runtimeWaves.Length == 0)
        {
            Debug.Log("Floor không có Wave/Stage nào.");
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

        int totalStages = runtimeWaves != null ? runtimeWaves.Length : (floorData != null && floorData.waves != null ? floorData.waves.Length : 0);

        if (currentWaveIndex >= totalStages - 1)
        {
            Debug.Log("Hoàn thành toàn bộ Floor!");
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.CompleteRun();
            }
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

        // Theo GDD: Giảm Hunger và giảm buff theo Turn khi di chuyển giữa các Stage
        if (combatManager != null && combatManager.playerStats != null)
        {
            combatManager.playerStats.OnStageTransition();
        }

        // Cập nhật thanh tiến trình Stage trên UI
        UpdateProgressBar();

        StartWave();

        isTransitioning = false;
    }

    private void StartWave()
    {
        WaveData wave = CurrentWave;

        if (wave == null)
            return;

        Debug.Log($"Bắt đầu Stage: {currentWaveIndex + 1} | Loại: {wave.waveType}");

        UpdateProgressBar();

        switch (wave.waveType)
        {
            case WaveData.WaveType.Combat:
                SpawnEnemies(wave, isAmbush: false);
                break;

            case WaveData.WaveType.Boss:
                SpawnEnemies(wave, isAmbush: false);
                break;

            case WaveData.WaveType.Camp:
                HandleCampStage();
                break;

            case WaveData.WaveType.Event:
                HandleEventStage();
                break;

            case WaveData.WaveType.Reward:
                HandleRewardStage();
                break;
        }
    }

    private void HandleCampStage()
    {
        Debug.Log("Đã đến khu cắm trại (Camp). Người chơi có thể nghỉ ngơi, ăn, nấu ăn hoặc tập thể dục.");
        if (camp != null)
        {
            camp.gameObject.SetActive(true);
            camp.waveManager = this;
            if (combatManager != null)
            {
                camp.playerStats = combatManager.playerStats;
                camp.itemContainer = combatManager.playerStats.itemContainer;
            }
        }
    }

    private void HandleEventStage()
    {
        Debug.Log("Gặp sự kiện đặc biệt (Merchant Event).");
        if (merchantEvent != null && merchantEvent.merchantActive)
        {
            merchantEvent.gameObject.SetActive(true);
        }
        else
        {
            // Nếu không có event hoặc merchant đã hết hạn, tự động tiếp tục
            Continue();
        }
    }

    private void HandleRewardStage()
    {
        Debug.Log("Đến phòng thưởng (Reward Stage)!");
        // Tự động chuyển tiếp sau khi nhận thưởng
        Continue();
    }

    public void TriggerAmbush()
    {
        WaveData wave = CurrentWave;
        if (wave != null && wave.enemyPrefabs != null && wave.enemyPrefabs.Length > 0)
        {
            SpawnEnemies(wave, isAmbush: true);
        }
        else
        {
            Debug.Log("Bị Ambush nhưng wave không có quái. Tiếp tục...");
            Continue();
        }
    }

    public void SpawnEnemies(WaveData wave, bool isAmbush = false)
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

        combatManager.StartCombat(isAmbush);
    }

    private void UpdateProgressBar()
    {
        if (progressController == null) return;

        int total = runtimeWaves != null ? runtimeWaves.Length : (floorData != null && floorData.waves != null ? floorData.waves.Length : 10);
        progressController.totalStage = total;
        progressController.currentStage = currentWaveIndex + 1;

        if (progressController.image != null)
        {
            progressController.image.fillAmount = progressController.currentStage / progressController.totalStage;
        }
    }

    public int GetCurrentWaveIndex()
    {
        return currentWaveIndex;
    }
}