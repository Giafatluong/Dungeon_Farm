using UnityEngine;
using System.Collections.Generic;

public class RewardChest : MonoBehaviour
{
    [Header("Base Reward Pool")]
    [Tooltip("Default loot pool used when the current wave has no custom loot defined.")]
    public ItemSlot[] defaultLootPool;

    [Header("Loot Config")]
    [Range(1, 8)]
    public int minLootItems = 3;
    [Range(1, 8)]
    public int maxLootItems = 5;
    [Range(0f, 1f)]
    public float dropChance = 0.75f;

    [Header("Floor Scaling")]
    [Tooltip("Each floor adds this many bonus loot rolls.")]
    public int bonusRollsPerFloor = 0;

    [Header("Runtime State")]
    public List<ItemSlot> currentChestLoot = new List<ItemSlot>();

    private static ItemData[] _cachedProjectItems;
    private readonly List<ItemData> _cachedCandidates = new List<ItemData>(32);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _cachedProjectItems = null;
    }

    private static ItemData[] GetAllProjectItems()
    {
        if (_cachedProjectItems == null || _cachedProjectItems.Length == 0)
        {
            _cachedProjectItems = GameAssetHelper.LoadAll<ItemData>();
        }
        return _cachedProjectItems;
    }

    /// <summary>
    /// Checks whether an item is an advanced cooked combo dish (crafted in Base Kitchen).
    /// Advanced combo meals should not drop from dungeon chests to keep the farming & cooking progression loop intact.
    /// </summary>
    public static bool IsAdvancedComboFood(ItemData item)
    {
        if (item == null) return false;
        if (item is FoodData fd)
        {
            if (fd.foodBuff != null)
            {
                for (int i = 0; i < fd.foodBuff.Length; i++)
                {
                    var fb = fd.foodBuff[i];
                    if (fb != null && (fb.buffDurationType == FoodData.BuffDurationType.Combat || fb.buffDurationType == FoodData.BuffDurationType.Floor))
                    {
                        return true;
                    }
                }
            }

            string n = item.name.ToLower();
            string iname = item.itemName != null ? item.itemName.ToLower() : "";
            if (n.Contains("salad") || n.Contains("soup") || n.Contains("butter") || n.Contains("spicy") ||
                iname.Contains("salad") || iname.Contains("soup") || iname.Contains("bơ") || iname.Contains("cay"))
            {
                return true;
            }

            if (fd.foodBuff != null && fd.foodBuff.Length > 0)
            {
                for (int b = 0; b < fd.foodBuff.Length; b++)
                {
                    if (fd.foodBuff[b] != null && fd.foodBuff[b].buffValue >= 3)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Checks whether an item is a Boss drop, rare relic, or goddess statue offering trophy.
    /// These items must NEVER drop from regular dungeon chests (they only drop from Boss fights or floor completion rewards).
    /// </summary>
    public static bool IsBossOrRareTrophyItem(ItemData item)
    {
        if (item == null) return false;
        if (item.isRare) return true;
        if (item.GetRarity() == ItemData.ItemRarity.Epic) return true;

        string n = item.name.ToLower();
        string iname = !string.IsNullOrEmpty(item.itemName) ? item.itemName.ToLower() : "";
        if (n.Contains("boss") || iname.Contains("boss") ||
            n.Contains("crown") || iname.Contains("vương miện") ||
            n.Contains("slimecore") || iname.Contains("lõi slime") ||
            n.Contains("ancientcrown") || iname.Contains("hoa hồng cổ") ||
            n.Contains("darkessence") || iname.Contains("tinh thể") ||
            n.Contains("offering") || iname.Contains("hiến tế") ||
            n.Contains("item_monster") || iname.Contains("quái") || iname.Contains("mảnh xương") || iname.Contains("cánh dơi") || iname.Contains("vuốt quỷ"))
        {
            return true;
        }

        return false;
    }

    private void AddItemByName(List<ItemSlot> list, ItemData[] allItems, string targetName, int amount)
    {
        if (allItems == null || list == null) return;
        for (int i = 0; i < allItems.Length; i++)
        {
            if (allItems[i] != null && allItems[i].name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new ItemSlot { itemData = allItems[i], amount = amount });
                return;
            }
        }
    }

    /// <summary>
    /// Generates a list of loot for this reward stage.
    /// </summary>
    public List<ItemSlot> GenerateChestLoot(WaveData waveData, int floorNumber)
    {
        List<ItemSlot> result = new List<ItemSlot>();
        ItemData[] allItems = GetAllProjectItems();

        // 1. Use custom wave loot if specified and non-empty
        if (waveData != null && waveData.customRewardLoot != null && waveData.customRewardLoot.Length > 0)
        {
            foreach (var slot in waveData.customRewardLoot)
            {
                if (slot != null && slot.itemData != null)
                {
                    int amt = slot.amount > 0 ? slot.amount : 1;
                    result.Add(new ItemSlot { itemData = slot.itemData, amount = amt });
                }
            }

            if (result.Count > 0)
            {
                Debug.Log($"[RewardChest] Using custom wave loot: {result.Count} item(s).");
                currentChestLoot = result;
                return result;
            }
        }

        // Dedicated safety rule for Floor 0 (Tutorial): exclusively grant basic starter seeds & crops
        if (floorNumber == 0)
        {
            AddItemByName(result, allItems, "CarrotSeed", 3);
            AddItemByName(result, allItems, "Carrot", 2);
            if (result.Count == 0)
            {
                AddItemByName(result, allItems, "WheatSeed", 3);
                AddItemByName(result, allItems, "Wheat", 2);
            }
            currentChestLoot = result;
            return result;
        }

        // 2. Try configured default pool if valid
        _cachedCandidates.Clear();
        if (defaultLootPool != null && defaultLootPool.Length > 0)
        {
            foreach (var s in defaultLootPool)
            {
                if (s != null && s.itemData != null && !_cachedCandidates.Contains(s.itemData))
                {
                    if (!IsAdvancedComboFood(s.itemData) && !IsBossOrRareTrophyItem(s.itemData))
                    {
                        _cachedCandidates.Add(s.itemData);
                    }
                }
            }
        }

        // 3. Fallback: Automatically gather all available project items (seeds, crops, provisions)
        if (_cachedCandidates.Count == 0)
        {
            for (int i = 0; i < allItems.Length; i++)
            {
                ItemData it = allItems[i];
                if (it == null) continue;

                // Never drop advanced cooked combo dishes from dungeon chests
                if (IsAdvancedComboFood(it)) continue;

                // Never drop boss trophies, crowns, or goddess offerings from dungeon chests
                if (IsBossOrRareTrophyItem(it)) continue;

                // Filter seeds by floor: Higher-tier seeds only drop on deeper floors
                if (it.itemType == ItemData.ItemType.Seed || it.name.ToLower().Contains("seed"))
                {
                    int minFloor = ProgressionManager.GetSeedMinFloor(it);
                    if (floorNumber < minFloor)
                    {
                        continue; // Floor too low, do not drop
                    }
                }

                if (!_cachedCandidates.Contains(it))
                {
                    _cachedCandidates.Add(it);
                }
            }
        }
        else
        {
            // Filter loaded candidateItems if any seeds do not meet the minimum floor requirement, are combo dishes, or are boss trophies
            for (int i = _cachedCandidates.Count - 1; i >= 0; i--)
            {
                ItemData it = _cachedCandidates[i];
                if (it == null || IsAdvancedComboFood(it) || IsBossOrRareTrophyItem(it))
                {
                    _cachedCandidates.RemoveAt(i);
                    continue;
                }

                if (it.itemType == ItemData.ItemType.Seed || it.name.ToLower().Contains("seed"))
                {
                    if (floorNumber < ProgressionManager.GetSeedMinFloor(it))
                    {
                        _cachedCandidates.RemoveAt(i);
                    }
                }
            }
        }

        // If strict floor filter left no candidate items, load basic seeds and raw crops without restriction
        if (_cachedCandidates.Count == 0)
        {
            for (int i = 0; i < allItems.Length; i++)
            {
                if (allItems[i] != null && !IsAdvancedComboFood(allItems[i]) && !IsBossOrRareTrophyItem(allItems[i]) && !_cachedCandidates.Contains(allItems[i]))
                {
                    _cachedCandidates.Add(allItems[i]);
                }
            }
        }

        if (_cachedCandidates.Count == 0)
        {
            Debug.LogWarning("[RewardChest] No items found in project to generate loot!");
            currentChestLoot = result;
            return result;
        }

        List<ItemData> candidateItems = _cachedCandidates;

        // 4. Procedurally roll a balanced assortment of 2 to 4 distinct items
        ShuffleList(candidateItems);
        int minRolls = Mathf.Max(2, minLootItems);
        int maxRolls = Mathf.Max(minRolls + 1, maxLootItems + 1);
        int rolls = Mathf.Clamp(Random.Range(minRolls, maxRolls) + (bonusRollsPerFloor * Mathf.Max(0, floorNumber - 1)), 2, 4);
        int itemsToPick = Mathf.Min(rolls, candidateItems.Count);

        for (int i = 0; i < itemsToPick; i++)
        {
            ItemData it = candidateItems[i];
            if (it == null) continue;

            string n = it.name.ToLower();
            int amount = 1;
            if (n.Contains("seed") || it.itemName.ToLower().Contains("seed"))
            {
                // Seeds drop in stacks of 1 to 3
                amount = Random.Range(1, 4);
            }
            else if (it is FoodData)
            {
                // Raw produce / basic food drops in stacks of 1
                amount = 1;
            }
            else
            {
                // Harvest crops/materials drop in stacks of 1 to 2
                amount = Random.Range(1, 3);
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
