using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Universal persistence serializer for ItemContainer ScriptableObjects (Backpack & Home Chest).
/// Serializes slots (ItemData reference and amount) into PlayerPrefs.
/// </summary>
public static class ItemContainerPersistence
{
    public const string ChestKeyPrefix = "HomeChest";
    public const string BackpackKeyPrefix = "PlayerBackpack";

    private static Dictionary<string, ItemData> _cachedItemMap;

    // Pre-allocated static cache for slot key strings to avoid GC string allocations on Save/Load
    private static readonly string[] _backpackItemKeys = new string[64];
    private static readonly string[] _backpackAmountKeys = new string[64];
    private static readonly string[] _chestItemKeys = new string[64];
    private static readonly string[] _chestAmountKeys = new string[64];

    static ItemContainerPersistence()
    {
        for (int i = 0; i < 64; i++)
        {
            _backpackItemKeys[i] = $"{BackpackKeyPrefix}_Slot_{i}_Item";
            _backpackAmountKeys[i] = $"{BackpackKeyPrefix}_Slot_{i}_Amount";
            _chestItemKeys[i] = $"{ChestKeyPrefix}_Slot_{i}_Item";
            _chestAmountKeys[i] = $"{ChestKeyPrefix}_Slot_{i}_Amount";
        }
    }

    public static string GetSlotItemKey(string keyPrefix, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < 64)
        {
            if (keyPrefix == BackpackKeyPrefix) return _backpackItemKeys[slotIndex];
            if (keyPrefix == ChestKeyPrefix) return _chestItemKeys[slotIndex];
        }
        return $"{keyPrefix}_Slot_{slotIndex}_Item";
    }

    public static string GetSlotAmountKey(string keyPrefix, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < 64)
        {
            if (keyPrefix == BackpackKeyPrefix) return _backpackAmountKeys[slotIndex];
            if (keyPrefix == ChestKeyPrefix) return _chestAmountKeys[slotIndex];
        }
        return $"{keyPrefix}_Slot_{slotIndex}_Amount";
    }

    /// <summary>
    /// Checks if a saved state exists in PlayerPrefs for the given container prefix.
    /// </summary>
    public static bool HasSavedContainer(string keyPrefix)
    {
        return PlayerPrefs.HasKey($"{keyPrefix}_SlotCount");
    }

    /// <summary>
    /// Saves all slots of an ItemContainer into PlayerPrefs.
    /// </summary>
    public static void SaveContainer(ItemContainer container, string keyPrefix, bool flush = true)
    {
        if (container == null || container.itemSlots == null) return;

        int count = container.itemSlots.Length;
        PlayerPrefs.SetInt($"{keyPrefix}_SlotCount", count);

        for (int i = 0; i < count; i++)
        {
            var slot = container.itemSlots[i];
            string itemKey = GetSlotItemKey(keyPrefix, i);
            string amountKey = GetSlotAmountKey(keyPrefix, i);

            if (slot != null && slot.itemData != null && slot.amount > 0)
            {
                PlayerPrefs.SetString(itemKey, slot.itemData.name);
                PlayerPrefs.SetInt(amountKey, slot.amount);
            }
            else
            {
                PlayerPrefs.DeleteKey(itemKey);
                PlayerPrefs.DeleteKey(amountKey);
            }
        }

        if (flush)
        {
            PlayerPrefs.Save();
        }

        Debug.Log($"[ItemContainerPersistence] Saved {keyPrefix} ({count} slots).");
    }

    /// <summary>
    /// Loads saved slots from PlayerPrefs into the ItemContainer.
    /// If no save exists for this container, leaves it untouched or clears if clearIfMissing is true.
    /// </summary>
    public static bool LoadContainer(ItemContainer container, string keyPrefix, bool clearIfMissing = false)
    {
        if (container == null || container.itemSlots == null) return false;

        if (!PlayerPrefs.HasKey($"{keyPrefix}_SlotCount"))
        {
            if (clearIfMissing)
            {
                container.Clear();
            }
            return false;
        }

        int count = Mathf.Min(container.itemSlots.Length, PlayerPrefs.GetInt($"{keyPrefix}_SlotCount", container.itemSlots.Length));
        Dictionary<string, ItemData> itemMap = GetItemMap();

        for (int i = 0; i < container.itemSlots.Length; i++)
        {
            if (container.itemSlots[i] == null)
            {
                container.itemSlots[i] = new ItemSlot();
            }

            string itemKey = GetSlotItemKey(keyPrefix, i);
            string amountKey = GetSlotAmountKey(keyPrefix, i);

            if (i < count && PlayerPrefs.HasKey(itemKey))
            {
                string itemName = PlayerPrefs.GetString(itemKey, "");
                int amount = PlayerPrefs.GetInt(amountKey, 0);

                if (!string.IsNullOrEmpty(itemName) && amount > 0 && itemMap.TryGetValue(itemName, out ItemData data))
                {
                    container.itemSlots[i].itemData = data;
                    container.itemSlots[i].amount = amount;
                    continue;
                }
            }

            container.itemSlots[i].itemData = null;
            container.itemSlots[i].amount = 0;
        }

        container.NotifyChange();
        Debug.Log($"[ItemContainerPersistence] Loaded {keyPrefix} ({container.itemSlots.Length} slots).");
        return true;
    }

    /// <summary>
    /// Deletes all saved keys for the given container prefix from PlayerPrefs.
    /// </summary>
    public static void ClearContainer(string keyPrefix)
    {
        if (!PlayerPrefs.HasKey($"{keyPrefix}_SlotCount")) return;

        int count = PlayerPrefs.GetInt($"{keyPrefix}_SlotCount", 64);
        for (int i = 0; i < count; i++)
        {
            PlayerPrefs.DeleteKey($"{keyPrefix}_Slot_{i}_Item");
            PlayerPrefs.DeleteKey($"{keyPrefix}_Slot_{i}_Amount");
        }
        PlayerPrefs.DeleteKey($"{keyPrefix}_SlotCount");
    }

    private static Dictionary<string, ItemData> GetItemMap()
    {
        if (_cachedItemMap != null && _cachedItemMap.Count > 0)
        {
            return _cachedItemMap;
        }

        _cachedItemMap = new Dictionary<string, ItemData>(System.StringComparer.OrdinalIgnoreCase);
        ItemData[] allItems = GameAssetHelper.LoadAll<ItemData>();
        if (allItems == null) return _cachedItemMap;

        for (int i = 0; i < allItems.Length; i++)
        {
            ItemData item = allItems[i];
            if (item == null) continue;

            if (!_cachedItemMap.ContainsKey(item.name))
            {
                _cachedItemMap[item.name] = item;
            }
            if (!string.IsNullOrEmpty(item.itemName) && !_cachedItemMap.ContainsKey(item.itemName))
            {
                _cachedItemMap[item.itemName] = item;
            }
        }

        return _cachedItemMap;
    }
}

