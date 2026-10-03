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

        InitializeDefaultUnlocks();
    }

    private void InitializeDefaultUnlocks()
    {
        if (unlockedSeeds == null) unlockedSeeds = new List<ItemData>();
        if (unlockedItems == null) unlockedItems = new List<ItemData>();
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
        currentFloor = floor;
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
        OnRunEnded?.Invoke(true);

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadBase();
        }
    }

    private void ClearRunLoot(ItemContainer inventory)
    {
        for (int i = 0; i < inventory.itemSlots.Length; i++)
        {
            inventory.itemSlots[i].itemData = null;
            inventory.itemSlots[i].amount = 0;
        }
        InventoryButton.selectedItem = null;
    }
}
