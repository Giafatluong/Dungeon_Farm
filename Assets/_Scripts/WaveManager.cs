using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    [Header("Level Design - Floor Configuration")]
    [Tooltip("FloorData being used (if set, game runs the exact sequence of stages you designed)")]
    [SerializeField] private FloorData floorData;
    [Tooltip("List of all custom designed floors (Floor 1, Floor 2, Floor 3...)")]
    [SerializeField] private FloorData[] allFloors;

    [Header("Positions & Core")]
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private Transform[] combatPositions;
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private HealthBarManager healthBarManager;
    [SerializeField] private EnemyTargetSelector targetSelector;

    [Header("Encounter Managers")]
    public Camp camp;
    [SerializeField] private MerchantEvent merchantEvent;
    [SerializeField] private ProgressController progressController;
    [SerializeField] private FloorGenerator floorGenerator;

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

        if (floorGenerator == null)
            floorGenerator = GetComponent<FloorGenerator>() ?? FindFirstObjectByType<FloorGenerator>();

        // 1. Priority 1: Hand-crafted FloorData (Level Design)
        if (activeFloor != null && activeFloor.waves != null && activeFloor.waves.Length > 0)
        {
            // Each wave in FloorData is kept 1:1 (Random waves are single dynamic waves, not expanded areas)
            runtimeWaves = (WaveData[])activeFloor.waves.Clone();
            Debug.Log($"[WaveManager] Running custom designed floor: '{activeFloor.floorName ?? activeFloor.name}' with {runtimeWaves.Length} stages.");
        }
        else if (floorGenerator != null)
        {
            // 2. Fallback: Procedurally generate floor using FloorGenerator
            runtimeWaves = floorGenerator.GenerateFloor();
            Debug.Log($"[WaveManager] Procedurally generated random floor using FloorGenerator with {runtimeWaves?.Length ?? 0} stages.");
        }

        if (runtimeWaves == null || runtimeWaves.Length == 0)
        {
            Debug.LogWarning("[WaveManager] Floor has no stages/waves configured.");
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

        float configuredDelay = CurrentWave != null
            ? CurrentWave.transitionDelay
            : 0f;
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

        // If this single wave is a Random Wave, dynamically calculate current score and pick a random encounter type
        if (effectiveType == WaveData.WaveType.Random)
        {
            int minScore = wave.minTargetScore;
            int maxScore = wave.maxTargetScore;
            if (minScore == 0 && maxScore == 0 && floorGenerator != null)
            {
                minScore = floorGenerator.minTargetScore;
                maxScore = floorGenerator.maxTargetScore;
            }

            if (floorGenerator != null)
            {
                effectiveType = floorGenerator.DetermineRandomWaveType(currentFloorScore, lastResolvedType, minScore, maxScore);
            }
            else
            {
                effectiveType = WaveData.WaveType.Combat;
            }

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
        Debug.Log("[WaveManager] Encountered Merchant Event.");
        if (merchantEvent != null && merchantEvent.merchantActive)
        {
            merchantEvent.gameObject.SetActive(true);
        }
        else
        {
            Continue();
        }
    }

    private void HandleRewardStage()
    {
        Debug.Log("[WaveManager] Reached Reward Stage!");
        string rewardDesc = "You found a treasure chest in the reward room!\nReceive recovery items to bolster your strength.";

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

        if (runtimeWaves != null)
        {
            for (int i = 0; i < runtimeWaves.Length; i++)
            {
                if (runtimeWaves[i] != null && (runtimeWaves[i].waveType == WaveData.WaveType.Combat || runtimeWaves[i].waveType == WaveData.WaveType.Boss))
                {
                    GameObject[] pool = runtimeWaves[i].GetEnemiesToSpawn();
                    if (pool != null && pool.Length > 0)
                    {
                        return runtimeWaves[i];
                    }
                }
            }
        }

        FloorData floor = GetActiveFloor();
        if (floor != null && floor.waves != null)
        {
            for (int i = 0; i < floor.waves.Length; i++)
            {
                if (floor.waves[i] != null && floor.waves[i].waveType == WaveData.WaveType.Combat)
                {
                    GameObject[] pool = floor.waves[i].GetEnemiesToSpawn();
                    if (pool != null && pool.Length > 0)
                    {
                        return floor.waves[i];
                    }
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

            Vector3 finalPos = combatPositions[i].position;
            Vector3 startPos = finalPos + new Vector3(5.5f, 0f, 0f);

            GameObject enemyObject = Instantiate(
                enemiesToSpawn[i],
                startPos,
                Quaternion.identity
            );

            StartCoroutine(AnimateEnemySlideIn(enemyObject, startPos, finalPos, 0.75f));

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