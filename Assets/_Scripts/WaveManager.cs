using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    [Header("Level Design - Floor Configuration")]
    [Tooltip("FloorData đang được sử dụng (nếu có, game sẽ chạy CHÍNH XÁC theo thứ tự các stage bạn thiết kế)")]
    [SerializeField] private FloorData floorData;
    [Tooltip("Danh sách tất cả các Floor do bạn tự thiết kế (Floor 1, Floor 2, Floor 3...)")]
    [SerializeField] private FloorData[] allFloors;

    [Header("Positions & Core")]
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

    public FloorData GetActiveFloor()
    {
        if (floorData != null) return floorData;

        if (allFloors != null && allFloors.Length > 0)
        {
            int floorIndex = 0;
            if (ProgressionManager.Instance != null)
            {
                floorIndex = Mathf.Clamp(ProgressionManager.Instance.currentFloor - 1, 0, allFloors.Length - 1);
            }
            return allFloors[floorIndex];
        }

        return null;
    }

    public WaveData CurrentWave
    {
        get
        {
            if (runtimeWaves == null || runtimeWaves.Length == 0)
            {
                FloorData activeFloor = GetActiveFloor();
                if (activeFloor == null || activeFloor.waves == null) return null;
                if (currentWaveIndex < 0 || currentWaveIndex >= activeFloor.waves.Length) return null;
                return activeFloor.waves[currentWaveIndex];
            }

            if (currentWaveIndex < 0 || currentWaveIndex >= runtimeWaves.Length)
                return null;

            return runtimeWaves[currentWaveIndex];
        }
    }

    private void Awake()
    {
        if (combatManager == null) combatManager = GetComponent<CombatManager>();
        if (healthBarManager == null) healthBarManager = GetComponent<HealthBarManager>();
        if (targetSelector == null) targetSelector = GetComponent<EnemyTargetSelector>();
        if (floorGenerator == null) floorGenerator = GetComponent<FloorGenerator>();
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
        FloorData activeFloor = GetActiveFloor();

        // 1. Ưu tiên hàng đầu: Chạy theo FloorData do bạn tự sắp xếp (Level Design)
        if (activeFloor != null && activeFloor.waves != null && activeFloor.waves.Length > 0)
        {
            runtimeWaves = activeFloor.waves;
            Debug.Log($"🎮 [WaveManager] Đang chạy Floor do bạn thiết kế: '{activeFloor.floorName ?? activeFloor.name}' với {runtimeWaves.Length} stages.");
        }
        else if (floorGenerator != null)
        {
            // 2. Chỉ khi bạn không gán FloorData nào thì mới sinh tự động hoàn toàn bằng FloorGenerator
            runtimeWaves = floorGenerator.GenerateFloor();
            Debug.Log($"🎲 [WaveManager] Tự động sinh Floor ngẫu nhiên bằng FloorGenerator.");
        }

        if (runtimeWaves == null || runtimeWaves.Length == 0)
        {
            Debug.LogWarning("⚠️ Floor không có Wave/Stage nào.");
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

        FloorData activeFloor = GetActiveFloor();
        int totalStages = runtimeWaves != null ? runtimeWaves.Length : (activeFloor != null && activeFloor.waves != null ? activeFloor.waves.Length : 0);

        if (currentWaveIndex >= totalStages - 1)
        {
            Debug.Log("Hoàn thành toàn bộ Floor!");
            if (CombatUI.Instance != null)
            {
                CombatUI.Instance.ShowVictoryScreen("🏆 CHIẾN THẮNG HẦM NGỤC!\n\nBạn đã dọn sạch toàn bộ các tầng và đánh bại Boss!\nChiến lợi phẩm và hạt giống đã được bảo vệ an toàn.");
            }
            else if (ProgressionManager.Instance != null)
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
        string rewardDesc = "Bạn đã tìm thấy rương kho báu trong phòng thưởng!\nNhận thêm vật phẩm hồi phục sức mạnh.";

        if (CombatUI.Instance != null)
        {
            CombatUI.Instance.ShowRewardScreen(rewardDesc);
        }
        else
        {
            Continue();
        }
    }

    public void TriggerAmbush()
    {
        if (CombatUI.Instance != null)
        {
            CombatUI.Instance.LogMessage("⚠️ CẢNH BÁO: Bị phục kích! Kẻ địch sẽ hành động trước!");
        }

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
        // Lấy danh sách quái (hỗ trợ cả cố định lẫn bốc ngẫu nhiên từ pool)
        GameObject[] enemiesToSpawn = wave.GetEnemiesToSpawn();

        if (enemiesToSpawn == null || enemiesToSpawn.Length == 0)
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
            enemiesToSpawn.Length,
            combatPositions.Length
        );

        EnemyStats[] newEnemies = new EnemyStats[enemyCount];

        for (int i = 0; i < enemyCount; i++)
        {
            if (enemiesToSpawn[i] == null)
                continue;

            if (combatPositions[i] == null)
            {
                Debug.Log("Combat Position " + i + " is NULL");
                continue;
            }

            GameObject enemyObject = Instantiate(
                enemiesToSpawn[i],
                enemySpawnPoint.position,
                Quaternion.identity
            );

            enemyObject.transform.position = combatPositions[i].position;

            EnemyStats enemyStats = enemyObject.GetComponent<EnemyStats>();

            if (enemyStats == null)
            {
                Debug.Log(
                    "Enemy prefab does not contain EnemyStats: " +
                    enemiesToSpawn[i].name
                );

                Destroy(enemyObject);
                continue;
            }

            enemyStats.targetSelector = targetSelector;
            enemyStats.SelectIntent();

            newEnemies[i] = enemyStats;
        }

        // Lọc các kẻ địch hợp lệ
        System.Collections.Generic.List<EnemyStats> validEnemies = new System.Collections.Generic.List<EnemyStats>();
        for (int i = 0; i < newEnemies.Length; i++)
        {
            if (newEnemies[i] != null)
            {
                validEnemies.Add(newEnemies[i]);
            }
        }
        EnemyStats[] enemyArray = validEnemies.ToArray();

        combatManager.SetEnemies(enemyArray);

        if (healthBarManager != null)
        {
            healthBarManager.SetEnemies(enemyArray);
            healthBarManager.CreateHealthBars();
        }

        if (targetSelector != null)
        {
            targetSelector.AutoSelectTarget(enemyArray);
        }

        combatManager.StartCombat(isAmbush);
    }

    private void UpdateProgressBar()
    {
        if (progressController == null) return;

        FloorData activeFloor = GetActiveFloor();
        int total = runtimeWaves != null ? runtimeWaves.Length : (activeFloor != null && activeFloor.waves != null ? activeFloor.waves.Length : 10);
        progressController.totalStage = total;
        progressController.currentStage = currentWaveIndex + 1;

        if (progressController.image != null)
        {
            progressController.image.fillAmount = (float)progressController.currentStage / progressController.totalStage;
        }
    }

    public int GetCurrentWaveIndex()
    {
        return currentWaveIndex;
    }
}