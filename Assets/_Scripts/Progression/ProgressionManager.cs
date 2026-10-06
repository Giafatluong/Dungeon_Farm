using UnityEngine;
using System.Collections.Generic;

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

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

    [Header("Run State")]
    public bool runActive = false;
    public int currentFloor = 1;
    public int currentStage = 0;

    [Header("Floor Progression")]
    public int highestUnlockedFloor = 1;
    public List<int> defeatedBossFloors = new List<int>();
    public int selectedFloor = 1;

    public event System.Action OnProgressionChanged;
    public event System.Action<bool> OnRunEnded; // true: victory, false: defeat

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadFloorProgression();
        InitializeDefaultUnlocks();
    }

    private void InitializeDefaultUnlocks()
    {
        if (unlockedSeeds == null) unlockedSeeds = new List<ItemData>();
        if (unlockedItems == null) unlockedItems = new List<ItemData>();

        if (unlockedSeeds.Count == 0)
        {
            ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            for (int i = 0; i < allItems.Length; i++)
            {
                ItemData it = allItems[i];
                if (it == null) continue;
                string nameLower = it.name.ToLower();
                if (nameLower.Contains("wheat") || nameLower.Contains("carrot"))
                {
                    if (nameLower.Contains("seed"))
                    {
                        if (!unlockedSeeds.Contains(it)) unlockedSeeds.Add(it);
                    }
                    else
                    {
                        if (!unlockedItems.Contains(it)) unlockedItems.Add(it);
                    }
                }
            }
        }
    }

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
        OnProgressionChanged?.Invoke();
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
        OnProgressionChanged?.Invoke();
    }

    public void AddOfferingProgress(string offeringKey, int requirementIndex, int amount)
    {
        if (string.IsNullOrEmpty(offeringKey) || amount <= 0) return;
        string key = $"{offeringKey}_req_{requirementIndex}";
        int current = offeringContributions.TryGetValue(key, out int val) ? val : 0;
        offeringContributions[key] = current + amount;
        OnProgressionChanged?.Invoke();
    }

    public void HandlePlayerDeath(ItemContainer inventory)
    {
        HandlePlayerDeathWithoutReload(inventory);

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadBase();
        }
    }

    public void HandlePlayerDeathWithoutReload(ItemContainer inventory)
    {
        Debug.Log("Player died! Run ended.");
        runActive = false;

        if (inventory != null)
        {
            ClearRunLoot(inventory);
        }

        OnRunEnded?.Invoke(false);
    }

    public void CompleteRun()
    {
        Debug.Log("Dungeon Run completed successfully!");
        runActive = false;
        RecordBossDefeated(currentFloor);
        OnRunEnded?.Invoke(true);

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadBase();
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

    public void RecordBossDefeated(int floor)
    {
        if (floor < 1) floor = 1;
        if (defeatedBossFloors == null) defeatedBossFloors = new List<int>();

        bool changed = false;
        if (!defeatedBossFloors.Contains(floor))
        {
            defeatedBossFloors.Add(floor);
            changed = true;
        }

        if (floor + 1 > highestUnlockedFloor)
        {
            highestUnlockedFloor = floor + 1;
            changed = true;
        }

        if (changed)
        {
            SaveFloorProgression();
            Debug.Log($"[ProgressionManager] Boss of Floor {floor} defeated! Floor {floor + 1} is now unlocked! (Highest: {highestUnlockedFloor})");
            OnProgressionChanged?.Invoke();
        }
    }

    public void UnlockFloor(int floor)
    {
        if (floor > highestUnlockedFloor)
        {
            highestUnlockedFloor = floor;
            SaveFloorProgression();
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

    [ContextMenu("Cheat: Unlock All Floors (Up to 5)")]
    public void CheatUnlockAllFloors()
    {
        if (defeatedBossFloors == null) defeatedBossFloors = new List<int>();
        for (int i = 1; i <= 4; i++)
        {
            if (!defeatedBossFloors.Contains(i)) defeatedBossFloors.Add(i);
        }
        highestUnlockedFloor = 5;
        SaveFloorProgression();
        OnProgressionChanged?.Invoke();
        Debug.Log("[ProgressionManager] Cheat: Unlocked all floors up to 5!");
    }

    [ContextMenu("Cheat: Reset Floor Progression")]
    public void ResetFloorProgression()
    {
        if (defeatedBossFloors == null) defeatedBossFloors = new List<int>();
        defeatedBossFloors.Clear();
        highestUnlockedFloor = 1;
        selectedFloor = 1;
        SaveFloorProgression();
        OnProgressionChanged?.Invoke();
        Debug.Log("[ProgressionManager] Reset floor progression back to Floor 1.");
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
