using UnityEngine;
using System.Collections.Generic;

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    [Header("Permanent Stats (Tăng vĩnh viễn từ Tượng)")]
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

        // Khởi tạo hạt giống cơ bản nếu danh sách còn rỗng
        InitializeDefaultUnlocks();
    }

    private void InitializeDefaultUnlocks()
    {
        if (unlockedSeeds == null) unlockedSeeds = new List<ItemData>();
        if (unlockedItems == null) unlockedItems = new List<ItemData>();

        if (unlockedSeeds.Count == 0)
        {
            // Tải các hạt giống mặc định cơ bản
            ItemData[] allSeeds = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var seed in allSeeds)
            {
                if (seed != null && seed.itemType == ItemData.ItemType.Seed && !seed.isRare)
                {
                    if (seed.name.Contains("Wheat") || seed.name.Contains("Carrot"))
                    {
                        if (!unlockedSeeds.Contains(seed)) unlockedSeeds.Add(seed);
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
            Debug.Log("Mở khóa vật phẩm mới: " + item.itemName);
            OnProgressionChanged?.Invoke();
        }
    }

    public void StartRun(int floor = 1)
    {
        runActive = true;
        currentFloor = floor;
        currentStage = 0;
        Debug.Log("Bắt đầu Run Dungeon - Tầng " + currentFloor);
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

        Debug.Log($"Đã tăng vĩnh viễn {stat} thêm {amount}!");
        OnProgressionChanged?.Invoke();
    }

    public void UnlockRecipe(RecipeData recipe)
    {
        if (recipe == null) return;
        if (!unlockedRecipes.Contains(recipe))
        {
            unlockedRecipes.Add(recipe);
            Debug.Log("Mở khóa công thức mới: " + recipe.recipeName);
            OnProgressionChanged?.Invoke();
        }
    }

    public void UnlockSeed(ItemData seedItem)
    {
        if (seedItem == null) return;
        if (!unlockedSeeds.Contains(seedItem))
        {
            unlockedSeeds.Add(seedItem);
            Debug.Log("Mở khóa hạt giống mới: " + seedItem.itemName);
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

    public void HandlePlayerDeath(ItemContainer inventory)
    {
        HandlePlayerDeathWithoutReload(inventory);

        // Quay về Base
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadBase();
        }
    }

    public void HandlePlayerDeathWithoutReload(ItemContainer inventory)
    {
        Debug.Log("Người chơi tử trận! Kết thúc Run.");
        runActive = false;

        // Theo GDD: Mất food và loot trong run, giữ lại Seed, Recipe, Permanent Stats
        if (inventory != null)
        {
            ClearRunLoot(inventory);
        }

        OnRunEnded?.Invoke(false);
    }

    public void CompleteRun()
    {
        Debug.Log("Hoàn thành Run Dungeon thành công!");
        runActive = false;
        OnRunEnded?.Invoke(true);

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadBase();
        }
    }

    private void ClearRunLoot(ItemContainer inventory)
    {
        // Khi chết trong run, làm rỗng các món ăn/loot mang theo
        for (int i = 0; i < inventory.itemSlots.Length; i++)
        {
            inventory.itemSlots[i].itemData = null;
            inventory.itemSlots[i].amount = 0;
        }
        InventoryButton.selectedItem = null;
    }
}
