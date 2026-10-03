using UnityEngine;
using System.Collections.Generic;

public class RewardChest : MonoBehaviour
{
    #region Inspector Fields & Config
    [Header("Chest Presentation")]
    public string chestTitle = "Ancient Dungeon Chest";
    [TextArea(2, 3)]
    public string chestDescription = "You discovered an ancient treasure chest! Collect rations and supplies from your unlocked crops and seeds.";

    [Header("Reward Pools (Only Unlocked Items will Drop)")]
    [Tooltip("Seeds pool - only unlocked seeds will be chosen")]
    public List<ItemData> seedPool = new();
    [Tooltip("Foods pool - only unlocked foods/rations will be chosen")]
    public List<ItemData> foodPool = new();
    [Tooltip("Harvested crops pool - only unlocked crops will be chosen")]
    public List<ItemData> cropPool = new();

    [Header("Current Chest Contents")]
    public List<ItemSlot> currentChestLoot = new();
    public bool isChestOpened { get; private set; } = false;

    public event System.Action<ItemSlot> OnItemClaimed;
    public event System.Action OnChestEmptied;
    #endregion

    #region Reward Generation
    private void Awake()
    {
        InitializeDefaultPoolsIfEmpty();
    }

    public void InitializeDefaultPoolsIfEmpty()
    {
        if (seedPool == null) seedPool = new List<ItemData>();
        if (foodPool == null) foodPool = new List<ItemData>();
        if (cropPool == null) cropPool = new List<ItemData>();

        if (seedPool.Count == 0 || foodPool.Count == 0)
        {
            ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            for (int i = 0; i < allItems.Length; i++)
            {
                ItemData item = allItems[i];
                if (item == null) continue;
                string nameLower = item.name.ToLower();
                if (nameLower.Contains("seed") || item.itemName.ToLower().Contains("seed"))
                {
                    if (!seedPool.Contains(item)) seedPool.Add(item);
                }
                else if (item is FoodData)
                {
                    if (!foodPool.Contains(item)) foodPool.Add(item);
                }
                else
                {
                    if (!cropPool.Contains(item)) cropPool.Add(item);
                }
            }
        }
    }

    public List<ItemSlot> GenerateChestLoot(WaveData wave, int floorNumber)
    {
        InitializeDefaultPoolsIfEmpty();

        currentChestLoot.Clear();
        isChestOpened = true;

        // 1. Check if wave has custom specific reward loot
        if (wave != null && wave.customRewardLoot != null && wave.customRewardLoot.Length > 0)
        {
            foreach (var slot in wave.customRewardLoot)
            {
                if (slot != null && slot.itemData != null && slot.amount > 0)
                {
                    // Strict rule: Only include if player has unlocked this item!
                    if (IsItemUnlocked(slot.itemData))
                    {
                        currentChestLoot.Add(new ItemSlot { itemData = slot.itemData, amount = slot.amount });
                    }
                }
            }

            if (currentChestLoot.Count > 0)
            {
                return currentChestLoot;
            }
        }

        // 2. Filter pools: ONLY items the player has ALREADY UNLOCKED!
        List<ItemData> unlockedSeeds = GetUnlockedItemsFromPool(seedPool);
        List<ItemData> unlockedFoods = GetUnlockedItemsFromPool(foodPool);
        List<ItemData> unlockedCrops = GetUnlockedItemsFromPool(cropPool);

        // Slot 1: Unlocked Seed (2 to 4)
        if (unlockedSeeds.Count > 0)
        {
            ItemData seed = unlockedSeeds[Random.Range(0, unlockedSeeds.Count)];
            int seedAmount = Random.Range(2, 4 + Mathf.Clamp(floorNumber, 1, 3));
            currentChestLoot.Add(new ItemSlot { itemData = seed, amount = seedAmount });
        }

        // Slot 2: Unlocked Food Ration (1 to 2)
        if (unlockedFoods.Count > 0)
        {
            ItemData food = unlockedFoods[Random.Range(0, unlockedFoods.Count)];
            int foodAmount = Random.Range(1, 3);
            currentChestLoot.Add(new ItemSlot { itemData = food, amount = foodAmount });
        }

        // Slot 3: Unlocked Crop / Material (2 to 4)
        if (unlockedCrops.Count > 0)
        {
            ItemData crop = unlockedCrops[Random.Range(0, unlockedCrops.Count)];
            int cropAmount = Random.Range(2, 5);
            currentChestLoot.Add(new ItemSlot { itemData = crop, amount = cropAmount });
        }
        else if (unlockedFoods.Count > 1)
        {
            // Fallback to second unlocked food
            ItemData extraFood = unlockedFoods[Random.Range(0, unlockedFoods.Count)];
            currentChestLoot.Add(new ItemSlot { itemData = extraFood, amount = Random.Range(1, 2) });
        }

        return currentChestLoot;
    }

    private List<ItemData> GetUnlockedItemsFromPool(List<ItemData> sourcePool)
    {
        List<ItemData> result = new();
        if (sourcePool == null) return result;

        for (int i = 0; i < sourcePool.Count; i++)
        {
            ItemData item = sourcePool[i];
            if (item != null && IsItemUnlocked(item))
            {
                result.Add(item);
            }
        }

        return result;
    }

    private bool IsItemUnlocked(ItemData item)
    {
        if (item == null) return false;
        if (ProgressionManager.Instance == null) return true;
        return ProgressionManager.Instance.IsItemUnlocked(item);
    }
    #endregion

    #region Claim & Inventory Transfer
    public bool ClaimSingleItem(int index, ItemContainer backpack, out string message)
    {
        message = string.Empty;
        if (backpack == null)
        {
            message = "Backpack not found!";
            return false;
        }

        if (index < 0 || index >= currentChestLoot.Count)
        {
            message = "Invalid item slot.";
            return false;
        }

        ItemSlot slot = currentChestLoot[index];
        if (slot == null || slot.itemData == null || slot.amount <= 0)
        {
            currentChestLoot.RemoveAt(index);
            return false;
        }

        if (!HasSpaceInBackpack(backpack, slot.itemData))
        {
            message = "Backpack is full!";
            return false;
        }

        backpack.AddItem(slot.itemData, slot.amount);
        message = $"Collected {slot.amount}x {slot.itemData.itemName}!";
        OnItemClaimed?.Invoke(slot);
        currentChestLoot.RemoveAt(index);

        if (currentChestLoot.Count == 0)
        {
            OnChestEmptied?.Invoke();
        }

        return true;
    }

    public int ClaimAllLoot(ItemContainer backpack, out bool isBackpackFull)
    {
        isBackpackFull = false;

        if (backpack == null)
        {
            isBackpackFull = true;
            return 0;
        }

        int claimedCount = 0;

        for (int i = currentChestLoot.Count - 1; i >= 0; i--)
        {
            ItemSlot slot = currentChestLoot[i];
            if (slot == null || slot.itemData == null || slot.amount <= 0)
            {
                currentChestLoot.RemoveAt(i);
                continue;
            }

            if (!HasSpaceInBackpack(backpack, slot.itemData))
            {
                isBackpackFull = true;
                continue;
            }

            backpack.AddItem(slot.itemData, slot.amount);
            claimedCount += slot.amount;
            OnItemClaimed?.Invoke(slot);
            currentChestLoot.RemoveAt(i);
        }

        if (currentChestLoot.Count == 0)
        {
            OnChestEmptied?.Invoke();
        }

        return claimedCount;
    }

    private bool HasSpaceInBackpack(ItemContainer backpack, ItemData item)
    {
        if (backpack == null || item == null) return false;
        if (item.isStackable && backpack.HasItem(item)) return true;
        return backpack.GetEmptySlot() != -1;
    }
    #endregion
}
