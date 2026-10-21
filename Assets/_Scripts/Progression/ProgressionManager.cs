using UnityEngine;
using System.Collections.Generic;

public class ProgressionManager : MonoBehaviour
{
    private static ProgressionManager _instance;
    private static bool isQuitting = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        isQuitting = false;
    }

    public static ProgressionManager Instance
    {
        get
        {
            if (isQuitting) return _instance;

            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ProgressionManager>();
                if (_instance == null && !isQuitting && Application.isPlaying)
                {
                    GameObject go = new GameObject("ProgressionManager_Runtime");
                    _instance = go.AddComponent<ProgressionManager>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    [Header("Permanent Stats (From Statues)")]
    public int permanentATK = 0;
    public int permanentDEF = 0;
    public int permanentSpeed = 0;
    public int permanentMaxHP = 0;

    [Header("Meta Progression Unlocks")]
    public List<ItemData> unlockedSeeds = new List<ItemData>();
    public List<ItemData> unlockedItems = new List<ItemData>();
    public List<RecipeData> unlockedRecipes = new List<RecipeData>();
    public HashSet<string> completedOfferings = new HashSet<string>();
    public HashSet<string> discoveredRecipes = new HashSet<string>();

    [Header("Run State")]
    public bool runActive = false;
    public int currentFloor = 1;
    public int currentStage = 0;

    [Header("Blessing System (Blessings & Build Paths)")]
    public BlessingType activeBlessing = BlessingType.None;
    public HashSet<BlessingType> unlockedBlessings = new HashSet<BlessingType>();

    [Header("Floor Progression")]
    public int highestUnlockedFloor = 1;
    public List<int> defeatedBossFloors = new List<int>();
    public int selectedFloor = 1;
    [Tooltip("Days required for Boss to respawn after defeat")]
    public int defaultBossResetDays = 3;

    public event System.Action OnProgressionChanged;
    public event System.Action<bool> OnRunEnded; // true: victory, false: defeat
    public event System.Action<RecipeData> OnRecipeDiscovered;
    public event System.Action<BlessingType> OnBlessingChanged;
    public event System.Action<BlessingType> OnBlessingUnlocked;

    private void Awake()
    {
        isQuitting = false;
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        LoadFloorProgression();
        LoadDiscoveredRecipes();
        LoadUnlockedBlessings();
        LoadPermanentStats();
        InitializeDefaultUnlocks();
        CheckAllBlessingUnlocks();
    }

    private void InitializeDefaultUnlocks()
    {
        if (unlockedSeeds == null) unlockedSeeds = new List<ItemData>();
        if (unlockedItems == null) unlockedItems = new List<ItemData>();
        if (unlockedRecipes == null) unlockedRecipes = new List<RecipeData>();

        // 1. Automatically unlock all seeds in the system
        ItemData[] allItems = GameAssetHelper.LoadAll<ItemData>();
        for (int i = 0; i < allItems.Length; i++)
        {
            ItemData it = allItems[i];
            if (it == null) continue;
            string nameLower = it.name.ToLower();
            if (nameLower.Contains("seed") || it.itemType == ItemData.ItemType.Seed)
            {
                if (!unlockedSeeds.Contains(it)) unlockedSeeds.Add(it);
            }
            else
            {
                if (!unlockedItems.Contains(it)) unlockedItems.Add(it);
            }
        }

        // 2. Automatically unlock all recipes in the system
        RecipeData[] allRecipes = GameAssetHelper.LoadAll<RecipeData>();
        for (int i = 0; i < allRecipes.Length; i++)
        {
            RecipeData r = allRecipes[i];
            if (r != null && !unlockedRecipes.Contains(r))
            {
                unlockedRecipes.Add(r);
            }
        }
    }

    /// <summary>
    /// Reset all game progression (New Game): Reset permanent stats, dungeon floors, recipes, and blessings to defaults.
    /// </summary>
    public void ResetAllProgression()
    {
        permanentATK = 0;
        permanentDEF = 0;
        permanentSpeed = 0;
        permanentMaxHP = 0;

        highestUnlockedFloor = 1;
        selectedFloor = 1;
        currentFloor = 1;
        currentStage = 0;
        runActive = false;

        if (defeatedBossFloors != null) defeatedBossFloors.Clear();
        if (completedOfferings != null) completedOfferings.Clear();
        if (offeringContributions != null) offeringContributions.Clear();
        if (discoveredRecipes != null) discoveredRecipes.Clear();
        if (unlockedBlessings != null) unlockedBlessings.Clear();
        activeBlessing = BlessingType.None;

        if (unlockedSeeds != null) unlockedSeeds.Clear();
        if (unlockedItems != null) unlockedItems.Clear();
        if (unlockedRecipes != null) unlockedRecipes.Clear();

        InitializeDefaultUnlocks();
        CheckAllBlessingUnlocks();

        SavePermanentStats();
        SaveFloorProgression();
        SaveDiscoveredRecipes();
        SaveUnlockedBlessings();

        OnProgressionChanged?.Invoke();
        OnBlessingChanged?.Invoke(BlessingType.None);
        Debug.Log("[ProgressionManager] All progression data has been completely reset to New Game defaults.");
    }

    /// <summary>
    /// Returns the minimum Dungeon floor required for a seed to drop randomly.
    /// Floor 1: Wheat, Carrot
    /// Floor 2: Cabbage, Cucumber, Tomato, Corn
    /// Floor 3+: Chilli and advanced seeds
    /// </summary>
    public static int GetSeedMinFloor(ItemData seed)
    {
        if (seed == null) return 1;
        string n = seed.name.ToLower();
        if (n.Contains("wheat") || n.Contains("carrot")) return 1;
        if (n.Contains("cabbage") || n.Contains("cucumber") || n.Contains("tomato") || n.Contains("corn")) return 2;
        if (n.Contains("chilli") || n.Contains("chili")) return 3;
        return 1;
    }

    #region Discovered Recipes
    public bool IsRecipeDiscovered(RecipeData recipe)
    {
        if (recipe == null || discoveredRecipes == null) return false;
        if (discoveredRecipes.Contains(recipe.DisplayName)) return true;
        if (!string.IsNullOrEmpty(recipe.recipeName) && discoveredRecipes.Contains(recipe.recipeName)) return true;
        return discoveredRecipes.Contains(recipe.name);
    }

    public bool DiscoverRecipe(RecipeData recipe)
    {
        if (recipe == null) return false;
        if (discoveredRecipes == null) discoveredRecipes = new HashSet<string>();

        string key = recipe.DisplayName;
        if (!IsRecipeDiscovered(recipe))
        {
            discoveredRecipes.Add(key);
            SaveDiscoveredRecipes();
            OnRecipeDiscovered?.Invoke(recipe);
            OnProgressionChanged?.Invoke();
            Debug.Log($"[ProgressionManager] Discovered new recipe: {key}!");
            return true; // First time discovered
        }
        return false;
    }

    public void SaveDiscoveredRecipes()
    {
        if (discoveredRecipes != null)
        {
            PlayerPrefs.SetString("Discovered_Recipes", string.Join(";", discoveredRecipes));
            PlayerPrefs.Save();
        }
    }

    public void LoadDiscoveredRecipes()
    {
        if (discoveredRecipes == null) discoveredRecipes = new HashSet<string>();
        discoveredRecipes.Clear();

        string saved = PlayerPrefs.GetString("Discovered_Recipes", "");
        if (!string.IsNullOrEmpty(saved))
        {
            string[] items = saved.Split(';');
            for (int i = 0; i < items.Length; i++)
            {
                string r = items[i].Trim();
                if (!string.IsNullOrEmpty(r))
                {
                    discoveredRecipes.Add(r);
                }
            }
        }
    }
    #endregion

    public bool IsItemUnlocked(ItemData item)
    {
        if (item == null) return false;
        if (unlockedSeeds != null && unlockedSeeds.Contains(item)) return true;
        if (unlockedItems != null && unlockedItems.Contains(item)) return true;
        return false;
    }

    public void UnlockItem(ItemData item)
    {
        if (item == null) return;
        if (unlockedItems == null) unlockedItems = new List<ItemData>();
        if (!unlockedItems.Contains(item))
        {
            unlockedItems.Add(item);
            Debug.Log("Unlocked new item: " + item.itemName);
            OnProgressionChanged?.Invoke();
        }
    }

    public void StartRun(int floor = 1)
    {
        runActive = true;
        currentFloor = floor > 0 ? floor : 1;
        selectedFloor = currentFloor;
        currentStage = 0;
        Debug.Log("Starting Dungeon Run - Floor " + currentFloor);
    }

    public void AddPermanentStat(Offering.RewardStat stat, int amount)
    {
        switch (stat)
        {
            case Offering.RewardStat.ATK:
                permanentATK += amount;
                break;
            case Offering.RewardStat.DEF:
                permanentDEF += amount;
                break;
            case Offering.RewardStat.Speed:
                permanentSpeed += amount;
                break;
        }

        Debug.Log($"Permanently increased {stat} by {amount}!");
        SavePermanentStats();
        CheckAllBlessingUnlocks();
        OnProgressionChanged?.Invoke();
    }

    public void SavePermanentStats()
    {
        PlayerPrefs.SetInt("Progression_PermanentATK", permanentATK);
        PlayerPrefs.SetInt("Progression_PermanentDEF", permanentDEF);
        PlayerPrefs.SetInt("Progression_PermanentSpeed", permanentSpeed);
        PlayerPrefs.SetInt("Progression_PermanentMaxHP", permanentMaxHP);

        if (completedOfferings != null)
        {
            PlayerPrefs.SetString("Progression_CompletedOfferings", string.Join(",", completedOfferings));
        }

        if (offeringContributions != null && offeringContributions.Count > 0)
        {
            List<string> entries = new List<string>();
            foreach (var kvp in offeringContributions)
            {
                entries.Add($"{kvp.Key}:{kvp.Value}");
            }
            PlayerPrefs.SetString("Progression_OfferingContributions", string.Join(";", entries));
        }
        else
        {
            PlayerPrefs.SetString("Progression_OfferingContributions", "");
        }

        PlayerPrefs.Save();
    }

    public void LoadPermanentStats()
    {
        permanentATK = PlayerPrefs.GetInt("Progression_PermanentATK", 0);
        permanentDEF = PlayerPrefs.GetInt("Progression_PermanentDEF", 0);
        permanentSpeed = PlayerPrefs.GetInt("Progression_PermanentSpeed", 0);
        permanentMaxHP = PlayerPrefs.GetInt("Progression_PermanentMaxHP", 0);

        if (completedOfferings == null) completedOfferings = new HashSet<string>();
        completedOfferings.Clear();

        string savedCompleted = PlayerPrefs.GetString("Progression_CompletedOfferings", "");
        if (!string.IsNullOrEmpty(savedCompleted))
        {
            string[] items = savedCompleted.Split(',');
            for (int i = 0; i < items.Length; i++)
            {
                string key = items[i].Trim();
                if (!string.IsNullOrEmpty(key)) completedOfferings.Add(key);
            }
        }

        if (offeringContributions != null) offeringContributions.Clear();
        string savedContribs = PlayerPrefs.GetString("Progression_OfferingContributions", "");
        if (!string.IsNullOrEmpty(savedContribs))
        {
            string[] pairs = savedContribs.Split(';');
            for (int i = 0; i < pairs.Length; i++)
            {
                string pair = pairs[i].Trim();
                if (string.IsNullOrEmpty(pair)) continue;
                string[] parts = pair.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int val))
                {
                    offeringContributions[parts[0]] = val;
                }
            }
        }
    }

    public void UnlockRecipe(RecipeData recipe)
    {
        if (recipe == null) return;
        if (!unlockedRecipes.Contains(recipe))
        {
            unlockedRecipes.Add(recipe);
            Debug.Log("Unlocked new recipe: " + recipe.recipeName);
            OnProgressionChanged?.Invoke();
        }
    }

    public void UnlockSeed(ItemData seedItem)
    {
        if (seedItem == null) return;
        if (!unlockedSeeds.Contains(seedItem))
        {
            unlockedSeeds.Add(seedItem);
            Debug.Log("Unlocked new seed: " + seedItem.itemName);
            OnProgressionChanged?.Invoke();
        }
    }

    public bool IsOfferingCompleted(string offeringID)
    {
        if (string.IsNullOrEmpty(offeringID)) return false;
        return completedOfferings.Contains(offeringID);
    }

    public void CompleteOffering(string offeringID)
    {
        if (string.IsNullOrEmpty(offeringID)) return;
        completedOfferings.Add(offeringID);
        SavePermanentStats();
        CheckAllBlessingUnlocks();
        OnProgressionChanged?.Invoke();
    }

    private readonly Dictionary<string, int> offeringContributions = new Dictionary<string, int>();

    public int GetOfferingProgress(string offeringKey, int requirementIndex)
    {
        if (string.IsNullOrEmpty(offeringKey)) return 0;
        string key = $"{offeringKey}_req_{requirementIndex}";
        return offeringContributions.TryGetValue(key, out int amount) ? amount : 0;
    }

    public void SetOfferingProgress(string offeringKey, int requirementIndex, int amount)
    {
        if (string.IsNullOrEmpty(offeringKey)) return;
        string key = $"{offeringKey}_req_{requirementIndex}";
        offeringContributions[key] = Mathf.Max(0, amount);
        SavePermanentStats();
        OnProgressionChanged?.Invoke();
    }

    public void AddOfferingProgress(string offeringKey, int requirementIndex, int amount)
    {
        if (string.IsNullOrEmpty(offeringKey) || amount <= 0) return;
        string key = $"{offeringKey}_req_{requirementIndex}";
        int current = offeringContributions.TryGetValue(key, out int val) ? val : 0;
        offeringContributions[key] = current + amount;
        SavePermanentStats();
        OnProgressionChanged?.Invoke();
    }

    #region Blessing System (Divine Blessings & Build Paths)
    /// <summary>
    /// On initial floors (Floor 1-2, highestUnlockedFloor <= 2), player has novice protection
    /// and the Divine Statue grants "Divine Protection" (KeepLootOnDefeat) to avoid dropping loot upon defeat.
    /// After unlocking Floor 3 (highestUnlockedFloor > 2), novice protection ends.
    /// Players can then choose Blessings at the Divine Statue.
    /// </summary>
    public bool IsStarterProtectionActive()
    {
        return highestUnlockedFloor <= 2;
    }

    /// <summary>
    /// Checks if player currently has this blessing.
    /// KeepLootOnDefeat is automatically active during starter protection.
    /// </summary>
    public bool HasBlessing(BlessingType blessing)
    {
        if (blessing == BlessingType.KeepLootOnDefeat)
        {
            return activeBlessing == BlessingType.KeepLootOnDefeat || IsStarterProtectionActive();
        }
        return activeBlessing == blessing;
    }

    public void SetBlessing(BlessingType blessing)
    {
        if (blessing != BlessingType.None && !IsBlessingUnlocked(blessing))
        {
            Debug.LogWarning($"[ProgressionManager] Blessing {blessing} is not unlocked yet!");
            return;
        }

        activeBlessing = blessing;
        Debug.Log($"[ProgressionManager] Received Divine Blessing: {blessing}");
        OnBlessingChanged?.Invoke(activeBlessing);
    }

    /// <summary>
    /// Clears the active run blessing (at day end, defeat, or dungeon completion).
    /// </summary>
    public void ClearRunBlessing()
    {
        if (activeBlessing != BlessingType.None)
        {
            Debug.Log($"[ProgressionManager] Blessing {activeBlessing} expired.");
            activeBlessing = BlessingType.None;
            OnBlessingChanged?.Invoke(activeBlessing);
        }
    }

    /// <summary>
    /// Checks if blessing is unlocked.
    /// Unlocks by: Dungeon Floor OR Statue Offering OR Dungeon Events.
    /// </summary>
    public bool IsBlessingUnlocked(BlessingType blessing)
    {
        if (blessing == BlessingType.None) return false;
        // Divine protection is always unlocked from the start
        if (blessing == BlessingType.KeepLootOnDefeat) return true;
        if (unlockedBlessings != null && unlockedBlessings.Contains(blessing)) return true;

        bool shouldUnlock = false;
        switch (blessing)
        {
            case BlessingType.RawVitality:
                // Complete any 1 crop offering at the Statue
                shouldUnlock = completedOfferings != null && completedOfferings.Count >= 1;
                break;
            case BlessingType.BastionStrike:
                // Clear Floor 1 (Floor 2+ unlocked) OR completed DEF offering
                shouldUnlock = highestUnlockedFloor >= 2 || permanentDEF > 0;
                break;
            case BlessingType.SwiftMomentum:
                // Clear Floor 1 (Floor 2+ unlocked) OR completed Speed offering
                shouldUnlock = highestUnlockedFloor >= 2 || permanentSpeed > 0;
                break;
            case BlessingType.StarvingFury:
                // Clear Floor 1 (Floor 2+ unlocked) OR completed ATK offering
                shouldUnlock = highestUnlockedFloor >= 2 || permanentATK > 0;
                break;
            case BlessingType.GluttonousAegis:
                // Defeat Floor 2 Boss and clear Floor 2 (Floor 3+ unlocked)
                shouldUnlock = highestUnlockedFloor >= 3;
                break;
            case BlessingType.BitterDecay:
                // Defeat Floor 2 Boss and clear Floor 2 (Floor 3+ unlocked)
                shouldUnlock = highestUnlockedFloor >= 3;
                break;
            case BlessingType.EndlessFeast:
                // Complete 2 or more offerings OR clear Floor 3 (Floor 4+ unlocked)
                shouldUnlock = (completedOfferings != null && completedOfferings.Count >= 2) || highestUnlockedFloor >= 4;
                break;
        }

        if (shouldUnlock)
        {
            UnlockBlessing(blessing);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Permanently unlocks a blessing.
    /// </summary>
    public bool UnlockBlessing(BlessingType blessing)
    {
        if (blessing == BlessingType.None) return false;
        if (unlockedBlessings == null) unlockedBlessings = new HashSet<BlessingType>();

        if (!unlockedBlessings.Contains(blessing))
        {
            unlockedBlessings.Add(blessing);
            SaveUnlockedBlessings();
            Debug.Log($"<color=#55FF88>[ProgressionManager] NEW BLESSING UNLOCKED: {blessing}!</color>");
            OnBlessingUnlocked?.Invoke(blessing);
            OnProgressionChanged?.Invoke();
            return true;
        }
        return false;
    }

    public void CheckAllBlessingUnlocks()
    {
        foreach (BlessingType type in System.Enum.GetValues(typeof(BlessingType)))
        {
            if (type != BlessingType.None)
            {
                IsBlessingUnlocked(type);
            }
        }
    }

    public void SaveUnlockedBlessings()
    {
        if (unlockedBlessings != null)
        {
            List<int> ids = new List<int>();
            foreach (var b in unlockedBlessings) ids.Add((int)b);
            PlayerPrefs.SetString("Unlocked_Blessings", string.Join(",", ids));
            PlayerPrefs.Save();
        }
    }

    public void LoadUnlockedBlessings()
    {
        if (unlockedBlessings == null) unlockedBlessings = new HashSet<BlessingType>();
        unlockedBlessings.Clear();
        unlockedBlessings.Add(BlessingType.KeepLootOnDefeat); // Always unlock basic protection by default

        string saved = PlayerPrefs.GetString("Unlocked_Blessings", "");
        if (!string.IsNullOrEmpty(saved))
        {
            string[] parts = saved.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i].Trim(), out int val))
                {
                    unlockedBlessings.Add((BlessingType)val);
                }
            }
        }
    }
    #endregion

    public void HandlePlayerDeath(ItemContainer inventory)
    {
        HandlePlayerDeathWithoutReload(inventory);

        if (!DungeonSummaryUI.HasConfirmedReturn)
        {
            DungeonSummaryUI summary = DungeonSummaryUI.EnsureInstance();
            if (summary != null)
            {
                summary.Show(DungeonSummaryUI.ExpeditionResult.Defeat, () =>
                {
                    if (SceneTransitionManager.Instance != null)
                    {
                        SceneTransitionManager.Instance.LoadBase();
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
                    }
                });
                return;
            }
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadBase();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
        }
    }

    public void HandlePlayerDeathWithoutReload(ItemContainer inventory)
    {
        Debug.Log("Player died! Run ended.");
        runActive = false;

        bool keepLoot = HasBlessing(BlessingType.KeepLootOnDefeat);
        if (inventory != null)
        {
            if (keepLoot)
            {
                Debug.Log("[ProgressionManager] 'Divine Protection' blessing triggered! Retaining all items in inventory upon defeat.");
            }
            else
            {
                ClearRunLoot(inventory);
            }
        }

        // On defeat, clear temporary buffs
        ClearRunBlessing();
        PlayerStats.Instance?.ClearTemporaryBuffs();

        OnRunEnded?.Invoke(false);
    }

    public void CompleteRun()
    {
        Debug.Log("Dungeon Run completed successfully!");
        runActive = false;
        RecordBossDefeated(currentFloor);

        // On dungeon completion, clear temporary buffs
        ClearRunBlessing();
        PlayerStats.Instance?.ClearTemporaryBuffs();

        OnRunEnded?.Invoke(true);

        if (!DungeonSummaryUI.HasConfirmedReturn)
        {
            DungeonSummaryUI summary = DungeonSummaryUI.EnsureInstance();
            if (summary != null)
            {
                summary.Show(DungeonSummaryUI.ExpeditionResult.Victory, () =>
                {
                    if (SceneTransitionManager.Instance != null)
                    {
                        SceneTransitionManager.Instance.LoadBase();
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
                    }
                });
                return;
            }
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadBase();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
        }
    }

    public void RetreatRun()
    {
        Debug.Log("Player retreated safely from the dungeon! Keeping all loot.");
        runActive = false;

        // On safe retreat: preserve 100% of loot in inventory
        // Clear temporary run blessings
        ClearRunBlessing();
        PlayerStats.Instance?.ClearTemporaryBuffs();

        OnRunEnded?.Invoke(false);

        if (!DungeonSummaryUI.HasConfirmedReturn)
        {
            DungeonSummaryUI summary = DungeonSummaryUI.EnsureInstance();
            if (summary != null)
            {
                summary.Show(DungeonSummaryUI.ExpeditionResult.Retreat, () =>
                {
                    if (SceneTransitionManager.Instance != null)
                    {
                        SceneTransitionManager.Instance.LoadBase();
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
                    }
                });
                return;
            }
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadBase();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
        }
    }

    #region Floor Progression Management
    public bool IsFloorUnlocked(int floor)
    {
        if (floor <= 1) return true;
        // Floor N requires defeating the boss of floor N - 1
        return (defeatedBossFloors != null && defeatedBossFloors.Contains(floor - 1)) || highestUnlockedFloor >= floor;
    }

    public bool IsBossDefeated(int floor)
    {
        return defeatedBossFloors != null && defeatedBossFloors.Contains(floor);
    }

    /// <summary>
    /// Checks if the floor Boss has respawned.
    /// If boss was never defeated -> Always available (true).
    /// If defeated -> Must wait until cooldown expires (defaultBossResetDays).
    /// </summary>
    public bool IsBossAvailable(int floor)
    {
        if (!IsBossDefeated(floor)) return true;

        int currentDay = DayManager.Instance != null ? DayManager.Instance.currentDay : 1;
        int nextAvailableDay = PlayerPrefs.GetInt($"Boss_Floor_{floor}_NextAvailableDay", 0);

        return currentDay >= nextAvailableDay;
    }

    /// <summary>
    /// Returns days remaining until the boss respawns. 0 if available or never defeated.
    /// </summary>
    public int GetDaysUntilBossReset(int floor)
    {
        if (IsBossAvailable(floor)) return 0;

        int currentDay = DayManager.Instance != null ? DayManager.Instance.currentDay : 1;
        int nextAvailableDay = PlayerPrefs.GetInt($"Boss_Floor_{floor}_NextAvailableDay", 0);

        return Mathf.Max(0, nextAvailableDay - currentDay);
    }

    /// <summary>
    /// Forces boss to respawn immediately (for testing or special mechanics).
    /// </summary>
    public void ResetBossCooldown(int floor)
    {
        PlayerPrefs.SetInt($"Boss_Floor_{floor}_NextAvailableDay", 0);
        PlayerPrefs.Save();
        OnProgressionChanged?.Invoke();
    }

    public bool RecordBossDefeated(int floor)
    {
        if (floor < 1) floor = 1;
        if (defeatedBossFloors == null) defeatedBossFloors = new List<int>();

        bool isFirstTime = !defeatedBossFloors.Contains(floor);
        if (isFirstTime)
        {
            defeatedBossFloors.Add(floor);
        }

        // Set next respawn day for the Boss
        int currentDay = DayManager.Instance != null ? DayManager.Instance.currentDay : 1;
        int nextAvailableDay = currentDay + defaultBossResetDays;
        PlayerPrefs.SetInt($"Boss_Floor_{floor}_DefeatedDay", currentDay);
        PlayerPrefs.SetInt($"Boss_Floor_{floor}_NextAvailableDay", nextAvailableDay);

        bool changed = isFirstTime;
        if (floor + 1 > highestUnlockedFloor)
        {
            highestUnlockedFloor = floor + 1;
            changed = true;
        }

        SaveFloorProgression();
        if (changed)
        {
            CheckAllBlessingUnlocks();
        }

        Debug.Log($"[ProgressionManager] Boss of Floor {floor} defeated on Day {currentDay}! (FirstTimeClear: {isFirstTime}) Floor {floor + 1} unlocked. Boss will reset on Day {nextAvailableDay} (Cooldown: {defaultBossResetDays} days).");
        OnProgressionChanged?.Invoke();

        return isFirstTime;
    }

    public bool HasClaimedFirstClearStats(int floor)
    {
        return PlayerPrefs.GetInt($"Floor_{floor}_FirstClearStatsClaimed", 0) == 1;
    }

    public void MarkFirstClearStatsClaimed(int floor)
    {
        PlayerPrefs.SetInt($"Floor_{floor}_FirstClearStatsClaimed", 1);
        PlayerPrefs.Save();
    }

    public void ApplyPermanentStatBonus(int atk, int def, int hp, int spd)
    {
        if (atk > 0) permanentATK += atk;
        if (def > 0) permanentDEF += def;
        if (hp > 0) permanentMaxHP += hp;
        if (spd > 0) permanentSpeed += spd;
        SavePermanentStats();
        OnProgressionChanged?.Invoke();
    }

    public void UnlockFloor(int floor)
    {
        if (floor > highestUnlockedFloor)
        {
            highestUnlockedFloor = floor;
            SaveFloorProgression();
            CheckAllBlessingUnlocks();
            OnProgressionChanged?.Invoke();
        }
    }

    public void SaveFloorProgression()
    {
        PlayerPrefs.SetInt("Dungeon_HighestUnlockedFloor", highestUnlockedFloor);
        if (defeatedBossFloors != null)
        {
            PlayerPrefs.SetString("Dungeon_DefeatedBossFloors", string.Join(",", defeatedBossFloors));
        }
        PlayerPrefs.Save();
    }

    public void LoadFloorProgression()
    {
        highestUnlockedFloor = PlayerPrefs.GetInt("Dungeon_HighestUnlockedFloor", 1);
        if (highestUnlockedFloor < 1) highestUnlockedFloor = 1;

        if (defeatedBossFloors == null) defeatedBossFloors = new List<int>();
        defeatedBossFloors.Clear();

        string savedBosses = PlayerPrefs.GetString("Dungeon_DefeatedBossFloors", "");
        if (!string.IsNullOrEmpty(savedBosses))
        {
            string[] parts = savedBosses.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i].Trim(), out int f) && !defeatedBossFloors.Contains(f))
                {
                    defeatedBossFloors.Add(f);
                }
            }
        }
    }

    #endregion

    private void ClearRunLoot(ItemContainer inventory)
    {
        if (inventory == null || inventory.itemSlots == null) return;
        for (int i = 0; i < inventory.itemSlots.Length; i++)
        {
            if (inventory.itemSlots[i] != null)
            {
                inventory.itemSlots[i].itemData = null;
                inventory.itemSlots[i].amount = 0;
            }
        }
        InventoryButton.selectedItem = null;
        inventory.NotifyChange();
    }
}

public enum BlessingType
{
    None = 0,
    KeepLootOnDefeat = 1,   // Divine Protection (Do not lose inventory loot upon defeat in Dungeon)
    BastionStrike = 2,      // Bastion Strike (+50% DEF added to ATK; Defend grants +1 AP)
    SwiftMomentum = 3,      // Swift Momentum (+4 Speed; higher speed than enemy deals bonus damage)
    StarvingFury = 4,       // Starving Fury (Fullness < 35% gives +6 ATK and 35% crit for 150% dmg)
    GluttonousAegis = 5,    // Gluttonous Aegis (Fullness > 70% gives +8 DEF and reflects 40% dmg)
    EndlessFeast = 6,       // Endless Feast (Food buffs 2x duration; first food each turn costs 0 AP)
    BitterDecay = 7,        // Bitter Decay (Attacks inflict Poison and erode enemy DEF by -2)
    RawVitality = 8         // Raw Vitality (Allows eating raw crops in Dungeon for +12 HP and +20 Fullness)
}

[System.Serializable]
public class BlessingInfo
{
    public BlessingType type;
    public string title;
    public string description;
    public string iconSymbol;
    public Color color;
    public string unlockRequirement;

    public BlessingInfo(BlessingType type, string title, string description, string iconSymbol, Color color, string unlockRequirement = "")
    {
        this.type = type;
        this.title = title;
        this.description = description;
        this.iconSymbol = iconSymbol;
        this.color = color;
        this.unlockRequirement = unlockRequirement;
    }
}

public static class BlessingDatabase
{
    private static readonly Dictionary<BlessingType, BlessingInfo> blessings = new Dictionary<BlessingType, BlessingInfo>
    {
        {
            BlessingType.KeepLootOnDefeat,
            new BlessingInfo(
                BlessingType.KeepLootOnDefeat,
                "Divine Protection",
                "When defeated in the Dungeon, keep all items in your inventory without dropping them.",
                "[PROTECTION]",
                new Color(0.3f, 0.8f, 1f),
                "Unlocked by default from the start."
            )
        },
        {
            BlessingType.BastionStrike,
            new BlessingInfo(
                BlessingType.BastionStrike,
                "Bastion Strike",
                "Add 50% of your current DEF to ATK. Using the Defend action restores +1 Action Point (AP).",
                "[BASTION]",
                new Color(1f, 0.7f, 0.2f),
                "Clear Dungeon Floor 1 OR complete DEF Offering at the Statue."
            )
        },
        {
            BlessingType.SwiftMomentum,
            new BlessingInfo(
                BlessingType.SwiftMomentum,
                "Swift Momentum",
                "Gain +4 Speed during dungeon runs. If faster than the enemy, attacks deal extra bonus damage based on speed difference.",
                "[SWIFT]",
                new Color(0.2f, 1f, 0.5f),
                "Clear Dungeon Floor 1 OR complete Speed Offering at the Statue."
            )
        },
        {
            BlessingType.StarvingFury,
            new BlessingInfo(
                BlessingType.StarvingFury,
                "Starving Fury",
                "When Fullness is below 35%, gain +6 ATK and attacks have a 35% chance to critically strike for 150% damage.",
                "[FURY]",
                new Color(1f, 0.3f, 0.3f),
                "Clear Dungeon Floor 1 OR complete ATK Offering at the Statue."
            )
        },
        {
            BlessingType.GluttonousAegis,
            new BlessingInfo(
                BlessingType.GluttonousAegis,
                "Gluttonous Aegis",
                "When Fullness is above 70%, gain +8 DEF and reflect 40% of incoming damage back to the attacker.",
                "[REFLECT]",
                new Color(0.9f, 0.8f, 0.2f),
                "Defeat Boss and clear Dungeon Floor 2."
            )
        },
        {
            BlessingType.EndlessFeast,
            new BlessingInfo(
                BlessingType.EndlessFeast,
                "Endless Feast",
                "All food buff durations are doubled. The first food consumed each turn costs 0 Action Points (AP).",
                "[FEAST]",
                new Color(1f, 0.5f, 0.8f),
                "Complete 2 Offerings at the Statue OR clear Dungeon Floor 3."
            )
        },
        {
            BlessingType.BitterDecay,
            new BlessingInfo(
                BlessingType.BitterDecay,
                "Bitter Decay",
                "All basic attacks inflict corrosive poison: reduce target's DEF by 2 permanently in combat and deal 4 poison damage.",
                "[DECAY]",
                new Color(0.6f, 0.3f, 0.9f),
                "Defeat Boss and clear Dungeon Floor 2."
            )
        },
        {
            BlessingType.RawVitality,
            new BlessingInfo(
                BlessingType.RawVitality,
                "Raw Vitality",
                "Allows eating raw crops (wheat, carrot, corn...) directly in the Dungeon to restore +12 HP and +20 Fullness.",
                "[VITALITY]",
                new Color(0.4f, 0.9f, 0.3f),
                "Complete any crop Offering at the Statue."
            )
        }
    };

    public static BlessingInfo GetBlessingInfo(BlessingType type)
    {
        if (blessings.TryGetValue(type, out var info))
        {
            return info;
        }
        return null;
    }

    /// <summary>
    /// Returns the full list of all 8 blessings (7 build paths + protection)
    /// so players can view and choose the build that best fits their run.
    /// </summary>
    public static List<BlessingInfo> GetAllBlessings()
    {
        return new List<BlessingInfo>(blessings.Values);
    }

    /// <summary>
    /// Returns a list of random blessings for the player to choose from.
    /// Guarantees that Divine Protection is always an eligible choice.
    /// </summary>
    public static List<BlessingInfo> GetRandomChoices(int count = 3)
    {
        List<BlessingInfo> all = new List<BlessingInfo>(blessings.Values);
        for (int i = 0; i < all.Count; i++)
        {
            int r = Random.Range(i, all.Count);
            var temp = all[i];
            all[i] = all[r];
            all[r] = temp;
        }

        int resultCount = Mathf.Clamp(count, 1, all.Count);
        return all.GetRange(0, resultCount);
    }
}
