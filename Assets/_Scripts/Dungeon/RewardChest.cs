using UnityEngine;
using System.Collections.Generic;

public class RewardChest : MonoBehaviour
{
    [Header("Base Reward Pool")]
    [Tooltip("Default loot pool used when the current wave has no custom loot defined.")]
    public ItemSlot[] defaultLootPool;

    [Header("Loot Config")]
    [Range(1, 5)]
    public int minLootItems = 1;
    [Range(1, 5)]
    public int maxLootItems = 3;
    [Range(0f, 1f)]
    public float dropChance = 0.75f;

    [Header("Floor Scaling")]
    [Tooltip("Each floor adds this many bonus loot rolls.")]
    public int bonusRollsPerFloor = 0;

    [Header("Runtime State")]
    public List<ItemSlot> currentChestLoot = new List<ItemSlot>();

    /// <summary>
    /// Generates a list of loot for this reward stage.
    /// </summary>
    public List<ItemSlot> GenerateChestLoot(WaveData waveData, int floorNumber)
    {
        List<ItemSlot> result = new List<ItemSlot>();

        // 1. Use custom wave loot if specified
        if (waveData != null && waveData.customRewardLoot != null && waveData.customRewardLoot.Length > 0)
        {
            foreach (var slot in waveData.customRewardLoot)
            {
                if (slot != null && slot.itemData != null && slot.amount > 0)
                {
                    result.Add(new ItemSlot { itemData = slot.itemData, amount = slot.amount });
                }
            }

            Debug.Log($"[RewardChest] Using custom wave loot: {result.Count} item(s).");
            currentChestLoot = result;
            return result;
        }

        // 2. Fall back to default loot pool
        ItemSlot[] pool = defaultLootPool;
        if (pool == null || pool.Length == 0)
        {
            Debug.LogWarning("[RewardChest] No loot pool configured for this chest.");
            currentChestLoot = result;
            return result;
        }

        int rolls = Mathf.Clamp(Random.Range(minLootItems, maxLootItems + 1) + (bonusRollsPerFloor * (floorNumber - 1)), 1, 10);

        List<ItemSlot> shuffledPool = new List<ItemSlot>(pool);
        ShuffleList(shuffledPool);

        for (int i = 0; i < shuffledPool.Count && result.Count < rolls; i++)
        {
            ItemSlot entry = shuffledPool[i];
            if (entry == null || entry.itemData == null) continue;

            if (Random.value <= dropChance)
            {
                result.Add(new ItemSlot { itemData = entry.itemData, amount = Mathf.Max(1, entry.amount) });
            }
        }

        Debug.Log($"[RewardChest] Generated {result.Count} loot item(s) for Floor {floorNumber}.");
        currentChestLoot = result;
        return result;
    }

    public bool ClaimSingleItem(int index, ItemContainer backpack, out string message)
    {
        message = "";
        if (currentChestLoot == null || index < 0 || index >= currentChestLoot.Count)
        {
            message = "Item not found in chest.";
            return false;
        }

        ItemSlot slot = currentChestLoot[index];
        if (slot == null || slot.itemData == null || slot.amount <= 0)
        {
            message = "Invalid item.";
            return false;
        }

        if (backpack == null)
        {
            message = "No backpack available.";
            return false;
        }

        if (!backpack.CanAddItem(slot.itemData))
        {
            message = $"Backpack is full! Cannot take {slot.itemData.itemName}.";
            return false;
        }

        backpack.AddItem(slot.itemData, slot.amount);
        message = $"{slot.amount}x {slot.itemData.itemName}";
        currentChestLoot.RemoveAt(index);
        return true;
    }

    public int ClaimAllLoot(ItemContainer backpack, out bool isFull)
    {
        isFull = false;
        if (backpack == null || currentChestLoot == null || currentChestLoot.Count == 0)
        {
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

            if (backpack.CanAddItem(slot.itemData))
            {
                backpack.AddItem(slot.itemData, slot.amount);
                currentChestLoot.RemoveAt(i);
                claimedCount++;
            }
            else
            {
                isFull = true;
            }
        }

        return claimedCount;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
}
