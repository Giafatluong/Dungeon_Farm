using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private FloorGenerator floorGenerator;
    #endregion

    #region Properties & State
    private WaveData[] runtimeWaves;
    private int currentWaveIndex = -1;
    private bool isTransitioning;
    private int currentFloorScore = 0;
    private WaveData.WaveType lastResolvedType = (WaveData.WaveType)(-1);
    private FloorData cachedActiveFloor;

    // Zero-allocation cached lists & runtime resource tracking
    private readonly List<WaveData> runtimeInstantiatedWaves = new List<WaveData>();
    private readonly List<int> _cachedMerchantCandidateIndices = new List<int>(8);
    private readonly List<EnemyStats> _cachedValidEnemies = new List<EnemyStats>(4);

    public FloorData GetActiveFloor()
    {
        if (cachedActiveFloor != null) return cachedActiveFloor;

        int currentFloorNum = ProgressionManager.Instance != null ? ProgressionManager.Instance.currentFloor : 1;

        if (allFloors != null && allFloors.Length > 0)
        {
            for (int i = 0; i < allFloors.Length; i++)
            {
                if (allFloors[i] != null && allFloors[i].floorNumber == currentFloorNum)
                {
                    cachedActiveFloor = allFloors[i];
                    return cachedActiveFloor;
                }
            }
            int floorIndex = Mathf.Clamp(currentFloorNum - 1, 0, allFloors.Length - 1);
            if (allFloors[floorIndex] != null)
            {
                cachedActiveFloor = allFloors[floorIndex];
                return cachedActiveFloor;
            }
        }

        if (floorData != null)
        {
            cachedActiveFloor = floorData;
            return cachedActiveFloor;
        }

        FloorData[] loadedFloors = GameAssetHelper.LoadAll<FloorData>();
        for (int i = 0; i < loadedFloors.Length; i++)
        {
            if (loadedFloors[i] != null && loadedFloors[i].floorNumber == currentFloorNum)
            {
                cachedActiveFloor = loadedFloors[i];
                return cachedActiveFloor;
            }
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
    public static WaveManager Instance { get; private set; }

    [Header("Entrance Room")]
    [Tooltip("Whether the player is currently in the safe entrance antechamber before wave 1.")]
    [SerializeField] private bool _isAtEntrance = false;
    public bool isAtEntrance => _isAtEntrance;
    public bool IsAtEntrance => _isAtEntrance;

    private void Awake()
    {
        Instance = this;
        cachedActiveFloor = null;
        if (combatManager == null) combatManager = GetComponent<CombatManager>();
        if (healthBarManager == null) healthBarManager = GetComponent<HealthBarManager>();
        if (targetSelector == null) targetSelector = GetComponent<EnemyTargetSelector>();
        if (floorGenerator == null) floorGenerator = GetComponent<FloorGenerator>();
        if (rewardChest == null) rewardChest = GetComponentInChildren<RewardChest>(true) ?? FindFirstObjectByType<RewardChest>(FindObjectsInactive.Include);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        for (int i = 0; i < runtimeInstantiatedWaves.Count; i++)
        {
            if (runtimeInstantiatedWaves[i] != null)
            {
                Destroy(runtimeInstantiatedWaves[i]);
            }
        }
        runtimeInstantiatedWaves.Clear();
    }

    private void Start()
    {
        if (ProgressionManager.Instance != null && !ProgressionManager.Instance.runActive)
        {
            int floorToRun = ProgressionManager.Instance.currentFloor > 0 ? ProgressionManager.Instance.currentFloor : 1;
            ProgressionManager.Instance.StartRun(floorToRun);
        }

        SetupFloorAndEnterEntrance();
    }
    #endregion

    #region Floor & Stage Navigation
    public void SetupFloorAndEnterEntrance()
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

        // Hand-crafted or procedural runtime waves established
        ApplyRoguelikeMerchantEncounter();

        // Safe entrance room buffer with right arrow UI
        EnterEntranceRoom();
    }

    public void EnterEntranceRoom()
    {
        _isAtEntrance = true;
        currentWaveIndex = -1;

        int floorNum = ProgressionManager.Instance != null ? ProgressionManager.Instance.currentFloor : 1;
        int totalStages = GetTotalStages();

        if (CombatUI.Instance != null)
        {
            CombatUI.Instance.RefreshAll();
            CombatUI.Instance.LogMessage($"Chào mừng đến Hầm Ngục Tầng {floorNum}! Nhấn nút mũi tên bên phải để tiến vào ải đầu tiên.");
        }

        DungeonEntranceArrowUI arrowUI = DungeonEntranceArrowUI.EnsureInstance();
        if (arrowUI != null)
        {
            arrowUI.Show(floorNum, totalStages, ProceedFromEntrance);
        }
    }

    public void ProceedFromEntrance()
    {
        if (!_isAtEntrance || isTransitioning)
            return;

        _isAtEntrance = false;

        DungeonEntranceArrowUI.Instance?.Hide();

        StartCoroutine(TransitionFromEntranceToFirstWave());
    }

    private IEnumerator TransitionFromEntranceToFirstWave()
    {
        isTransitioning = true;

        float moveDuration = 1.4f;

        // Player walking forward animation between entrance and stage 1
        PlayerCombatVisual playerVisual = FindFirstObjectByType<PlayerCombatVisual>();
        if (playerVisual != null)
        {
            playerVisual.PlayMoveTransition(moveDuration);
        }

        if (CombatUI.Instance != null)
        {
            CombatUI.Instance.LogMessage("Đang tiến vào ải đầu tiên...");
        }

        yield return new WaitForSeconds(moveDuration);

        currentWaveIndex = 0;
        UpdateProgressBar();
        StartWave();

        isTransitioning = false;
    }

    public void StartFirstWave()
    {
        SetupFloorAndEnterEntrance();
    }

    #region Roguelike Merchant Encounter
    [Header("Roguelike Merchant Encounter")]
    [Tooltip("Probability (0.0 to 1.0) that a dedicated Wandering Merchant stage will spawn in this run if the floor doesn't already have one.")]
    [Range(0f, 1f)]
    [SerializeField] private float roguelikeMerchantSpawnChance = 0.65f;
    [Tooltip("If true, dynamically places a Merchant stage at a randomized mid-floor stage on each expedition run.")]
    [SerializeField] private bool enableDynamicRoguelikeMerchant = true;

    private void ApplyRoguelikeMerchantEncounter()
    {
        if (runtimeWaves == null || runtimeWaves.Length < 3)
            return;

        // Synchronize merchant calendar state with current Farm Day (never hardcode reset to 5)
        if (merchantEvent == null) merchantEvent = FindFirstObjectByType<MerchantEvent>(FindObjectsInactive.Include);
        if (merchantEvent != null)
        {
            merchantEvent.RefreshCalendarStatus();
        }

        if (!enableDynamicRoguelikeMerchant)
            return;

        // Check if floor already explicitly contains a Merchant wave
        for (int i = 0; i < runtimeWaves.Length; i++)
        {
            if (runtimeWaves[i] != null && runtimeWaves[i].waveType == WaveData.WaveType.Merchant)
            {
                Debug.Log($"[WaveManager] Floor already has a designated Merchant stage at Stage {i + 1}.");
                return;
            }
        }

        // If merchant has departed on supply run, do not roll dynamic merchant stage
        if (merchantEvent != null && !merchantEvent.merchantActive)
        {
            Debug.Log("[WaveManager] Merchant is currently departed on supply cooldown. No dynamic merchant stage for this run.");
            return;
        }

        // Roll chance for Merchant to appear in this run
        if (Random.value <= roguelikeMerchantSpawnChance)
        {
            // Pick a random middle stage (exclude stage 0 and final boss stage)
            _cachedMerchantCandidateIndices.Clear();
            for (int i = 1; i < runtimeWaves.Length - 1; i++)
            {
                if (runtimeWaves[i] == null) continue;
                if (runtimeWaves[i].waveType == WaveData.WaveType.Combat || runtimeWaves[i].waveType == WaveData.WaveType.Random)
                {
                    _cachedMerchantCandidateIndices.Add(i);
                }
            }

            if (_cachedMerchantCandidateIndices.Count > 0)
            {
                int chosenIndex = _cachedMerchantCandidateIndices[Random.Range(0, _cachedMerchantCandidateIndices.Count)];
                WaveData merchantWave = ScriptableObject.CreateInstance<WaveData>();
                runtimeInstantiatedWaves.Add(merchantWave);
                merchantWave.name = $"Roguelike_Merchant_Stage_{chosenIndex + 1}";
                merchantWave.waveType = WaveData.WaveType.Merchant;
                merchantWave.enemyPrefabs = new GameObject[0];
                merchantWave.transitionDelay = 1.8f;
                runtimeWaves[chosenIndex] = merchantWave;
                Debug.Log($"[WaveManager] Roguelike Roll: Wandering Merchant successfully stationed at Stage {chosenIndex + 1}/{runtimeWaves.Length} for this run!");
            }
        }
        else
        {
            Debug.Log("[WaveManager] Roguelike Roll: Merchant is wandering elsewhere for this expedition.");
        }
    }
    #endregion

    public void Continue()
    {
        if (isTransitioning)
            return;

        FloorData activeFloor = GetActiveFloor();
        int totalStages = runtimeWaves != null ? runtimeWaves.Length : (activeFloor != null && activeFloor.waves != null ? activeFloor.waves.Length : 0);

        if (currentWaveIndex >= totalStages - 1)
        {
            Debug.Log("[WaveManager] Completed entire Floor! Opening Grand Floor Victory Rewards.");
            HandleFloorVictory();
            return;
        }

        StartCoroutine(NextWave());
    }

    private void HandleFloorVictory()
    {
        FloorData activeFloor = GetActiveFloor();
        int floorNum = activeFloor != null ? activeFloor.floorNumber : (ProgressionManager.Instance != null ? ProgressionManager.Instance.currentFloor : 1);

        bool isFirstTime = false;
        if (ProgressionManager.Instance != null)
        {
            isFirstTime = ProgressionManager.Instance.RecordBossDefeated(floorNum);
        }

        PlayerStats pStats = (combatManager != null && combatManager.playerStats != null) ? combatManager.playerStats : FindFirstObjectByType<PlayerStats>();
        ItemContainer backpack = pStats != null ? pStats.itemContainer : null;

        FloorVictoryUI victoryUI = FloorVictoryUI.EnsureInstance();
        if (victoryUI != null)
        {
            victoryUI.Open(activeFloor, floorNum, backpack, isFirstTime,
                onReturnToBase: () =>
                {
                    if (ProgressionManager.Instance != null)
                    {
                        ProgressionManager.Instance.CompleteRun();
                    }
                    else if (SceneTransitionManager.Instance != null)
                    {
                        SceneTransitionManager.Instance.LoadBase();
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
                    }
                },
                onNextFloor: () =>
                {
                    ProceedToNextFloor(floorNum + 1);
                });
        }
        else
        {
            if (CombatUI.Instance != null)
            {
                CombatUI.Instance.ShowVictoryScreen("DUNGEON VICTORY!\n\nYou cleared all stages and defeated the Boss!\nAll loot and crops have been secured.");
            }
            else if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.CompleteRun();
            }
        }
    }

    public void ProceedToNextFloor(int nextFloorNum)
    {
        Debug.Log($"[WaveManager] Transitioning directly to next floor: Floor {nextFloorNum}");
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.currentFloor = nextFloorNum;
            ProgressionManager.Instance.UnlockFloor(nextFloorNum);
        }

        cachedActiveFloor = null;
        currentWaveIndex = -1;
        currentFloorScore = 0;
        lastResolvedType = (WaveData.WaveType)(-1);

        SetupFloorAndEnterEntrance();
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

            case WaveData.WaveType.Merchant:
                HandleMerchantStage();
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

    private void HandleMerchantStage()
    {
        Debug.Log("[WaveManager] Reached Merchant Stage. Opening Wandering Merchant Encounter!");

        if (merchantEvent == null)
        {
            merchantEvent = FindFirstObjectByType<MerchantEvent>(FindObjectsInactive.Include);
            if (merchantEvent == null)
            {
                GameObject mGO = new GameObject("MerchantEvent");
                merchantEvent = mGO.AddComponent<MerchantEvent>();
            }
        }

        PlayerStats ps = (combatManager != null && combatManager.playerStats != null)
            ? combatManager.playerStats
            : FindFirstObjectByType<PlayerStats>();
        ItemContainer backpack = ps != null ? ps.itemContainer : null;

        bool isMerchantActive = merchantEvent != null && merchantEvent.merchantActive && merchantEvent.remainingDays > 0;

        if (CombatUI.Instance != null && CombatUI.Instance.turnBannerText != null)
        {
            if (isMerchantActive)
            {
                CombatUI.Instance.turnBannerText.text = $"MERCHANT - WANDERING TRADER ({merchantEvent.remainingDays}D LEFT)";
                CombatUI.Instance.turnBannerText.color = new Color(1f, 0.85f, 0.35f);
                if (CombatUI.Instance.turnBannerBg != null)
                {
                    CombatUI.Instance.turnBannerBg.color = new Color(0.32f, 0.22f, 0.08f, 0.9f);
                }
            }
            else
            {
                CombatUI.Instance.turnBannerText.text = "MERCHANT POST - DEPARTED (SAFE REST)";
                CombatUI.Instance.turnBannerText.color = new Color(0.65f, 0.82f, 1f);
                if (CombatUI.Instance.turnBannerBg != null)
                {
                    CombatUI.Instance.turnBannerBg.color = new Color(0.12f, 0.20f, 0.28f, 0.9f);
                }
            }
        }

        MerchantUI merchantUI = MerchantUI.EnsureInstance();
        if (merchantUI != null)
        {
            merchantUI.Open(merchantEvent, backpack, onContinue: () =>
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
        _cachedValidEnemies.Clear();

        for (int i = 0; i < enemyCount; i++)
        {
            if (enemiesToSpawn[i] == null || combatPositions[i] == null)
                continue;

            EnemyStats prefabStats = enemiesToSpawn[i].GetComponent<EnemyStats>();
            float yOff = prefabStats != null ? prefabStats.GetSpawnYOffset() : 0f;

            Vector3 finalPos = combatPositions[i].position + new Vector3(0f, yOff, 0f);
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
            _cachedValidEnemies.Add(stats);
        }

        EnemyStats[] enemyArray = _cachedValidEnemies.ToArray();

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
            EnemyCombatVisual visual = enemyObj.GetComponent<EnemyCombatVisual>() ?? enemyObj.AddComponent<EnemyCombatVisual>();
            visual.SetOriginalPosition(toPos);

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
        if (CombatUI.Instance != null)
        {
            CombatUI.Instance.UpdateStageProgress();
        }
    }

    public int GetCurrentWaveIndex()
    {
        return currentWaveIndex;
    }

    public int GetTotalStages()
    {
        if (runtimeWaves != null && runtimeWaves.Length > 0)
            return runtimeWaves.Length;

        FloorData floor = GetActiveFloor();
        if (floor != null && floor.waves != null)
            return floor.waves.Length;

        return 0;
    }
    #endregion
}