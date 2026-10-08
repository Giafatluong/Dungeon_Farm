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

        // 1. Use custom wave loot if specified and non-empty
        if (waveData != null && waveData.customRewardLoot != null && waveData.customRewardLoot.Length > 0)
        {
            foreach (var slot in waveData.customRewardLoot)
            {
                if (slot != null && slot.itemData != null && slot.amount > 0)
                {
                    result.Add(new ItemSlot { itemData = slot.itemData, amount = slot.amount });
                }
            }

            if (result.Count > 0)
            {
                Debug.Log($"[RewardChest] Using custom wave loot: {result.Count} item(s).");
                currentChestLoot = result;
                return result;
            }
        }

        // 2. Try configured default pool if valid
        List<ItemData> candidateItems = new List<ItemData>();
        if (defaultLootPool != null && defaultLootPool.Length > 0)
        {
            foreach (var s in defaultLootPool)
            {
                if (s != null && s.itemData != null && !candidateItems.Contains(s.itemData))
                {
                    candidateItems.Add(s.itemData);
                }
            }
        }

        // 3. Fallback: Automatically gather all available project items (seeds, crops, provisions)
        if (candidateItems.Count == 0)
        {
            ItemData[] allItems = GameAssetHelper.LoadAll<ItemData>();
            for (int i = 0; i < allItems.Length; i++)
            {
                ItemData it = allItems[i];
                if (it == null) continue;

                // Lọc hạt giống theo tầng: Hạt cao cấp chỉ xuất hiện ở tầng cao
                if (it.itemType == ItemData.ItemType.Seed || it.name.ToLower().Contains("seed"))
                {
                    int minFloor = ProgressionManager.GetSeedMinFloor(it);
                    if (floorNumber < minFloor)
                    {
                        continue; // Chưa đủ tầng, không cho rơi
                    }
                }

                if (!candidateItems.Contains(it))
                {
                    candidateItems.Add(it);
                }
            }
        }
        else
        {
            // Lọc danh sách candidateItems đã nạp nếu có hạt chưa đủ điều kiện tầng
            for (int i = candidateItems.Count - 1; i >= 0; i--)
            {
                ItemData it = candidateItems[i];
                if (it != null && (it.itemType == ItemData.ItemType.Seed || it.name.ToLower().Contains("seed")))
                {
                    if (floorNumber < ProgressionManager.GetSeedMinFloor(it))
                    {
                        candidateItems.RemoveAt(i);
                    }
                }
            }
        }

        // If strict floor filter left no candidate items, load all items without restriction as guaranteed fallback
        if (candidateItems.Count == 0)
        {
            ItemData[] allItems = GameAssetHelper.LoadAll<ItemData>();
            for (int i = 0; i < allItems.Length; i++)
            {
                if (allItems[i] != null && !candidateItems.Contains(allItems[i]))
                {
                    candidateItems.Add(allItems[i]);
                }
            }
        }

        if (candidateItems.Count == 0)
        {
            Debug.LogWarning("[RewardChest] No items found in project to generate loot!");
            currentChestLoot = result;
            return result;
        }

        // 4. Procedurally roll a randomized assortment of 2 to 4 distinct items
        ShuffleList(candidateItems);
        int rolls = Mathf.Clamp(Random.Range(2, 5) + (bonusRollsPerFloor * Mathf.Max(0, floorNumber - 1)), 2, 6);
        int itemsToPick = Mathf.Min(rolls, candidateItems.Count);

        for (int i = 0; i < itemsToPick; i++)
        {
            ItemData it = candidateItems[i];
            if (it == null) continue;

            string n = it.name.ToLower();
            int amount = 1;
            if (n.Contains("seed") || it.itemName.ToLower().Contains("seed"))
            {
                // Seeds drop in stacks of 2 to 5
                amount = Random.Range(2, 6);
            }
            else if (it is FoodData)
            {
                // Prepared food drops in stacks of 1 to 2
                amount = Random.Range(1, 3);
            }
            else
            {
                // Harvest crops/materials drop in stacks of 2 to 4
                amount = Random.Range(2, 5);
            }

            result.Add(new ItemSlot { itemData = it, amount = amount });
        }

        Debug.Log($"[RewardChest] Procedurally generated {result.Count} randomized reward items for Floor {floorNumber}.");
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
