using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    #region Inspector Fields & Config
    [Header("Level Design - Floor Configuration")]
    [Tooltip("FloorData being used (if set, game runs the exact sequence of stages you designed)")]
    [SerializeField] private FloorData floorData;
    [Tooltip("List of all custom designed floors (Floor 1, Floor 2, Floor 3...)")]
    [SerializeField] private FloorData[] allFloors;

    [Header("Positions & Core Dependencies")]
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private Transform[] combatPositions;
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private HealthBarManager healthBarManager;
    [SerializeField] private EnemyTargetSelector targetSelector;

    [Header("Encounter Managers")]
    public Camp camp;
    [SerializeField] private MerchantEvent merchantEvent;
    [SerializeField] private RewardChest rewardChest;
    [SerializeField] private ProgressController progressController;
    [SerializeField] private FloorGenerator floorGenerator;
    #endregion

    #region Properties & State
    private WaveData[] runtimeWaves;
    private int currentWaveIndex = -1;
    private bool isTransitioning;
    private int currentFloorScore = 0;
    private WaveData.WaveType lastResolvedType = (WaveData.WaveType)(-1);

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
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (combatManager == null) combatManager = GetComponent<CombatManager>();
        if (healthBarManager == null) healthBarManager = GetComponent<HealthBarManager>();
        if (targetSelector == null) targetSelector = GetComponent<EnemyTargetSelector>();
        if (floorGenerator == null) floorGenerator = GetComponent<FloorGenerator>();
        if (rewardChest == null) rewardChest = GetComponentInChildren<RewardChest>(true) ?? FindFirstObjectByType<RewardChest>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.StartRun(1);
        }

        StartFirstWave();
    }
    #endregion

    #region Floor & Stage Navigation
    public void StartFirstWave()
    {
        FloorData activeFloor = GetActiveFloor();

        if (floorGenerator == null)
            floorGenerator = GetComponent<FloorGenerator>() ?? FindFirstObjectByType<FloorGenerator>();

        // 1. Hand-crafted FloorData
        if (activeFloor != null && activeFloor.waves != null && activeFloor.waves.Length > 0)
        {
            runtimeWaves = (WaveData[])activeFloor.waves.Clone();
            Debug.Log($"[WaveManager] Running custom designed floor: '{activeFloor.floorName ?? activeFloor.name}' with {runtimeWaves.Length} stages.");
        }
        else if (floorGenerator != null)
        {
            // 2. Fallback: Procedurally generated floor
            runtimeWaves = floorGenerator.GenerateFloor();
            Debug.Log($"[WaveManager] Procedurally generated random floor using FloorGenerator with {runtimeWaves?.Length ?? 0} stages.");
        }

        if (runtimeWaves == null || runtimeWaves.Length == 0)
        {
            Debug.LogWarning("[WaveManager] Floor has no stages/waves configured.");
            return;
        }

        if (combatManager == null || targetSelector == null)
        {
            Debug.LogWarning("[WaveManager] CombatManager or TargetSelector is missing.");
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
            Debug.Log("[WaveManager] Completed entire Floor!");
            if (CombatUI.Instance != null)
            {
                CombatUI.Instance.ShowVictoryScreen("DUNGEON VICTORY!\n\nYou cleared all stages and defeated the Boss!\nAll loot and crops have been secured.");
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

        float configuredDelay = CurrentWave != null ? CurrentWave.transitionDelay : 0f;
        float moveDuration = Mathf.Max(configuredDelay, 1.8f);

        // Player walking forward animation between stages
        PlayerCombatVisual playerVisual = FindFirstObjectByType<PlayerCombatVisual>();
        if (playerVisual != null)
        {
            playerVisual.PlayMoveTransition(moveDuration);
        }

        yield return new WaitForSeconds(moveDuration);

        currentWaveIndex++;

        // Reduce hunger and decrement turn buffs on stage transition
        if (combatManager != null && combatManager.playerStats != null)
        {
            combatManager.playerStats.OnStageTransition();
        }

        UpdateProgressBar();
        StartWave();

        isTransitioning = false;
    }

    private void StartWave()
    {
        WaveData wave = CurrentWave;
        if (wave == null)
            return;

        WaveData.WaveType effectiveType = wave.waveType;

        // If this single wave is a Random Wave, dynamically calculate encounter type based on current score
        if (effectiveType == WaveData.WaveType.Random)
        {
            int minScore = wave.minTargetScore;
            int maxScore = wave.maxTargetScore;
            if (minScore == 0 && maxScore == 0 && floorGenerator != null)
            {
                minScore = floorGenerator.minTargetScore;
                maxScore = floorGenerator.maxTargetScore;
            }

            effectiveType = floorGenerator != null
                ? floorGenerator.DetermineRandomWaveType(currentFloorScore, lastResolvedType, minScore, maxScore)
                : WaveData.WaveType.Combat;

            Debug.Log($"[WaveManager] Wave {currentWaveIndex + 1} is Random. Dynamically calculated encounter type: {effectiveType} (Current Score: {currentFloorScore})");
        }

        // Update score balance tracking
        int scoreDelta = floorGenerator != null ? floorGenerator.GetScoreForWaveType(effectiveType) : 0;
        currentFloorScore += scoreDelta;
        lastResolvedType = effectiveType;

        Debug.Log($"[WaveManager] Starting Stage: {currentWaveIndex + 1}/{runtimeWaves.Length} | Type: {effectiveType} | Score Delta: {scoreDelta} | Floor Score: {currentFloorScore}");
        UpdateProgressBar();

        switch (effectiveType)
        {
            case WaveData.WaveType.Combat:
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
    #endregion

    #region Encounter Stage Handlers
    private void HandleCampStage()
    {
        Debug.Log("[WaveManager] Reached Camp. Player can rest, eat, cook, or exercise.");
        if (camp == null) camp = FindFirstObjectByType<Camp>(FindObjectsInactive.Include);
        if (camp == null)
        {
            Debug.LogWarning("[WaveManager] Camp component not found in Scene!");
            return;
        }

        PlayerStats pStats = (combatManager != null && combatManager.playerStats != null) ? combatManager.playerStats : FindFirstObjectByType<PlayerStats>();
        ItemContainer container = pStats != null ? pStats.itemContainer : null;

        CampUI campUI = CampUI.EnsureInstance();
        camp.OpenCamp(pStats, container, this);
        if (campUI != null)
        {
            campUI.ShowCampScreen();
        }
    }

    public void HandlePostAmbushCamp()
    {
        Debug.Log("[WaveManager] Repelled ambush at Camp! Re-opening Camp in mandatory continue mode.");
        isTransitioning = false;

        if (camp == null) camp = FindFirstObjectByType<Camp>(FindObjectsInactive.Include);
        if (camp != null)
        {
            camp.isCampOpen = true;
            camp.mustContinue = true;
            camp.ambushState = false;
        }

        CampUI campUI = CampUI.EnsureInstance();
        if (campUI != null)
        {
            campUI.ShowPostAmbushCampScreen();
        }
    }

    private void HandleEventStage()
    {
        Debug.Log("[WaveManager] Reached Event Stage!");

        DungeonEventManager eventMgr = DungeonEventManager.Instance ?? FindFirstObjectByType<DungeonEventManager>(FindObjectsInactive.Include);
        if (eventMgr == null)
        {
            GameObject emGO = new("DungeonEventManager");
            eventMgr = emGO.AddComponent<DungeonEventManager>();
        }

        DungeonEvent evt = eventMgr.GetRandomEvent();

        if (CombatUI.Instance != null && CombatUI.Instance.turnBannerText != null)
        {
            string title = evt != null ? evt.eventTitle.ToUpper() : "MYSTERIOUS EVENT";
            CombatUI.Instance.turnBannerText.text = $"EVENT - {title}";
            CombatUI.Instance.turnBannerText.color = new Color(0.9f, 0.7f, 1f);
            if (CombatUI.Instance.turnBannerBg != null)
            {
                CombatUI.Instance.turnBannerBg.color = new Color(0.22f, 0.14f, 0.32f, 0.9f);
            }
        }

        PlayerStats ps = (combatManager != null && combatManager.playerStats != null)
            ? combatManager.playerStats
            : FindFirstObjectByType<PlayerStats>();
        ItemContainer backpack = ps != null ? ps.itemContainer : null;

        DungeonEventUI eventUI = DungeonEventUI.EnsureInstance();
        if (eventUI != null && evt != null)
        {
            eventUI.OpenEvent(evt, ps, backpack, onComplete: () =>
            {
                Continue();
            });
        }
        else
        {
            Continue();
        }
    }

    private void HandleRewardStage()
    {
        Debug.Log("[WaveManager] Reached Reward Stage (Treasure Room)!");

        if (CombatUI.Instance != null && CombatUI.Instance.turnBannerText != null)
        {
            CombatUI.Instance.turnBannerText.text = "STAGE REWARD - TREASURE ROOM";
            CombatUI.Instance.turnBannerText.color = new Color(1f, 0.85f, 0.3f);
            if (CombatUI.Instance.turnBannerBg != null)
            {
                CombatUI.Instance.turnBannerBg.color = new Color(0.35f, 0.25f, 0.08f, 0.9f);
            }
        }

        if (rewardChest == null)
        {
            rewardChest = GetComponentInChildren<RewardChest>(true) ?? FindFirstObjectByType<RewardChest>(FindObjectsInactive.Include);
            if (rewardChest == null)
            {
                GameObject chestGO = new GameObject("RewardChest");
                chestGO.transform.SetParent(transform, false);
                rewardChest = chestGO.AddComponent<RewardChest>();
            }
        }

        int floorNum = ProgressionManager.Instance != null ? ProgressionManager.Instance.currentFloor : 1;
        List<ItemSlot> loot = rewardChest != null
            ? rewardChest.GenerateChestLoot(CurrentWave, floorNum)
            : new List<ItemSlot>();

        PlayerStats ps = (combatManager != null && combatManager.playerStats != null)
            ? combatManager.playerStats
            : FindFirstObjectByType<PlayerStats>();
        ItemContainer backpack = ps != null ? ps.itemContainer : null;

        RewardUI rewardUI = RewardUI.EnsureInstance();
        if (rewardUI != null)
        {
            rewardUI.Open(rewardChest, loot, backpack, onComplete: () =>
            {
                Continue();
            });
        }
        else
        {
            Continue();
        }
    }
    #endregion

    #region Combat & Ambush Spawning
    public void TriggerAmbush()
    {
        if (CombatUI.Instance != null)
        {
            CombatUI.Instance.LogMessage("WARNING: Ambushed! Enemies attack first!");
        }

        WaveData wave = CurrentWave;
        GameObject[] enemiesToSpawn = wave != null ? wave.GetEnemiesToSpawn() : null;

        if (enemiesToSpawn == null || enemiesToSpawn.Length == 0)
        {
            wave = GetFallbackCombatWave();
            if (wave != null)
            {
                enemiesToSpawn = wave.GetEnemiesToSpawn();
            }
        }

        if (enemiesToSpawn != null && enemiesToSpawn.Length > 0)
        {
            SpawnEnemies(wave, isAmbush: true);
        }
        else
        {
            Debug.Log("[WaveManager] Ambush triggered but no enemies to spawn. Continuing...");
            Continue();
        }
    }

    private WaveData GetFallbackCombatWave()
    {
        if (floorGenerator != null && floorGenerator.combatWavePool != null && floorGenerator.combatWavePool.Length > 0)
        {
            return floorGenerator.combatWavePool[Random.Range(0, floorGenerator.combatWavePool.Length)];
        }

        WaveData found = FindFirstValidCombatWave(runtimeWaves);
        if (found != null) return found;

        FloorData floor = GetActiveFloor();
        if (floor != null)
        {
            return FindFirstValidCombatWave(floor.waves);
        }

        return null;
    }

    private WaveData FindFirstValidCombatWave(IEnumerable<WaveData> waves)
    {
        if (waves == null) return null;
        foreach (var w in waves)
        {
            if (w == null) continue;
            if (w.waveType == WaveData.WaveType.Combat || w.waveType == WaveData.WaveType.Boss)
            {
                GameObject[] pool = w.GetEnemiesToSpawn();
                if (pool != null && pool.Length > 0)
                {
                    return w;
                }
            }
        }
        return null;
    }

    public void SpawnEnemies(WaveData wave, bool isAmbush = false)
    {
        GameObject[] enemiesToSpawn = wave != null ? wave.GetEnemiesToSpawn() : null;

        if (enemiesToSpawn == null || enemiesToSpawn.Length == 0)
        {
            WaveData fallbackWave = GetFallbackCombatWave();
            if (fallbackWave != null)
            {
                enemiesToSpawn = fallbackWave.GetEnemiesToSpawn();
            }
        }

        if (enemiesToSpawn == null || enemiesToSpawn.Length == 0)
        {
            Debug.Log("Wave has no enemy");
            return;
        }

        if (enemySpawnPoint == null || combatPositions == null || combatPositions.Length == 0)
        {
            Debug.Log("Enemy Spawn Point or Combat Positions missing");
            return;
        }

        int enemyCount = Mathf.Min(enemiesToSpawn.Length, combatPositions.Length);
        List<EnemyStats> validEnemies = new();

        for (int i = 0; i < enemyCount; i++)
        {
            if (enemiesToSpawn[i] == null || combatPositions[i] == null)
                continue;

            Vector3 finalPos = combatPositions[i].position;
            Vector3 startPos = finalPos + new Vector3(5.5f, 0f, 0f);

            GameObject enemyObject = Instantiate(enemiesToSpawn[i], startPos, Quaternion.identity);
            StartCoroutine(AnimateEnemySlideIn(enemyObject, startPos, finalPos, 0.75f));

            EnemyStats stats = enemyObject.GetComponent<EnemyStats>();
            if (stats == null)
            {
                Debug.LogWarning($"Enemy prefab does not contain EnemyStats: {enemiesToSpawn[i].name}");
                Destroy(enemyObject);
                continue;
            }

            stats.targetSelector = targetSelector;
            stats.SelectIntent();
            validEnemies.Add(stats);
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
    #endregion

    #region Enemy Visual Animations
    private IEnumerator AnimateEnemySlideIn(GameObject enemyObj, Vector3 fromPos, Vector3 toPos, float duration)
    {
        if (enemyObj == null) yield break;

        Animator enemyAnim = enemyObj.GetComponent<Animator>();
        if (enemyAnim != null && enemyAnim.isActiveAndEnabled)
        {
            enemyAnim.SetFloat("Horizontal", -1f);
            enemyAnim.SetFloat("Speed", 1f);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (enemyObj == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            enemyObj.transform.position = Vector3.Lerp(fromPos, toPos, smoothT);
            yield return null;
        }

        if (enemyObj != null)
        {
            enemyObj.transform.position = toPos;
            if (enemyAnim != null && enemyAnim.isActiveAndEnabled)
            {
                enemyAnim.SetFloat("Horizontal", 0f);
                enemyAnim.SetFloat("Speed", 0f);
            }
        }
    }
    #endregion

    #region Progress & Helpers
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
    #endregion
}