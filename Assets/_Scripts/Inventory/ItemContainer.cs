using UnityEngine;

[System.Serializable]
public class ItemSlot
{
    public ItemData itemData;
    public int amount;
}

[CreateAssetMenu(fileName = "New Item Container", menuName = "Item/Item Container")]
public class ItemContainer : ScriptableObject
{
    public int maxSlots = 20;
    public ItemSlot[] itemSlots;

    public event System.Action OnInventoryChange;

    private void OnEnable()
    {
        OnInventoryChange = null;

        if (itemSlots == null || itemSlots.Length != maxSlots)
        {
            if (itemSlots == null)
            {
                itemSlots = new ItemSlot[maxSlots];
            }
            else
            {
                System.Array.Resize(ref itemSlots, maxSlots);
            }
        }

        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] == null)
            {
                itemSlots[i] = new ItemSlot();
            }
        }
    }

    private void TriggerChange()
    {
        if (OnInventoryChange == null) return;

        var invocationList = OnInventoryChange.GetInvocationList();
        for (int i = 0; i < invocationList.Length; i++)
        {
            var d = invocationList[i];
            try
            {
                if (d.Target is UnityEngine.Object uObj && uObj == null)
                {
                    OnInventoryChange -= (System.Action)d;
                    continue;
                }
                ((System.Action)d).Invoke();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ItemContainer] Handled listener error: {ex.Message}");
            }
        }
    }

    public bool AddItem(ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0)
            return false;

        int limit = itemSlots != null ? itemSlots.Length : maxSlots;

        if (itemData.isStackable)
        {
            for (int i = 0; i < limit; i++)
            {
                if (itemSlots[i] != null && IsItemMatch(itemSlots[i].itemData, itemData))
                {
                    itemSlots[i].amount += amount;
                    TriggerChange();
                    return true;
                }
            }

            int slot = GetEmptySlot();

            if (slot != -1)
            {
                if (itemSlots[slot] == null) itemSlots[slot] = new ItemSlot();
                itemSlots[slot].itemData = itemData;
                itemSlots[slot].amount = amount;
                TriggerChange();
                return true;
            }

            return false;
        }
        else
        {
            bool anyAdded = false;
            for (int i = 0; i < amount; i++)
            {
                int slot = GetEmptySlot();

                if (slot != -1)
                {
                    if (itemSlots[slot] == null) itemSlots[slot] = new ItemSlot();
                    itemSlots[slot].itemData = itemData;
                    itemSlots[slot].amount = 1;
                    anyAdded = true;
                }
            }

            if (anyAdded)
            {
                TriggerChange();
            }
            return anyAdded;
        }
    }

    public static bool IsItemMatch(ItemData a, ItemData b)
    {
        if (a == null || b == null) return false;
        if (ReferenceEquals(a, b) || a == b) return true;
        if (!string.IsNullOrEmpty(a.itemName) && !string.IsNullOrEmpty(b.itemName) && a.itemName.Equals(b.itemName, System.StringComparison.OrdinalIgnoreCase)) return true;
        return a.name.Equals(b.name, System.StringComparison.OrdinalIgnoreCase);
    }

    public void RemoveItem(ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0)
            return;

        bool changed = false;
        int limit = itemSlots != null ? itemSlots.Length : maxSlots;

        for (int i = 0; i < limit; i++)
        {
            if (itemSlots[i] != null && IsItemMatch(itemSlots[i].itemData, itemData) && itemSlots[i].amount > 0)
            {
                if (itemSlots[i].amount > amount)
                {
                    itemSlots[i].amount -= amount;
                    changed = true;
                    amount = 0;
                    break;
                }
                else
                {
                    amount -= itemSlots[i].amount;
                    itemSlots[i].itemData = null;
                    itemSlots[i].amount = 0;
                    changed = true;

                    if (amount <= 0)
                        break;
                }
            }
        }

        if (changed)
        {
            if (!HasItem(itemData) && InventoryButton.selectedItem == itemData)
            {
                InventoryButton.selectedItem = null;
            }
            TriggerChange();
        }
    }

    public int GetItemCount(ItemData itemData)
    {
        if (itemData == null || itemSlots == null) return 0;
        int total = 0;
        int limit = itemSlots.Length;
        for (int i = 0; i < limit; i++)
        {
            if (itemSlots[i] != null && IsItemMatch(itemSlots[i].itemData, itemData) && itemSlots[i].amount > 0)
            {
                total += itemSlots[i].amount;
            }
        }
        return total;
    }

    public bool HasItem(ItemData itemData)
    {
        return HasItem(itemData, 1);
    }

    public bool HasItem(ItemData itemData, int requiredAmount)
    {
        if (itemData == null || requiredAmount <= 0)
            return false;

        int limit = itemSlots != null ? itemSlots.Length : maxSlots;
        if (requiredAmount == 1)
        {
            for (int i = 0; i < limit; i++)
            {
                if (itemSlots[i] != null &&
                    IsItemMatch(itemSlots[i].itemData, itemData) &&
                    itemSlots[i].amount > 0)
                {
                    return true;
                }
            }
            return false;
        }

        return GetItemCount(itemData) >= requiredAmount;
    }

    public int GetEmptySlot()
    {
        int limit = itemSlots != null ? itemSlots.Length : maxSlots;
        for (int i = 0; i < limit; i++)
        {
            if (itemSlots[i] == null || itemSlots[i].itemData == null)
            {
                return i;
            }
        }

        return -1;
    }

    public bool CanAddItem(ItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0) return false;
        if (itemData.isStackable && HasItem(itemData)) return true;

        int emptyCount = 0;
        int limit = itemSlots != null ? itemSlots.Length : maxSlots;
        for (int i = 0; i < limit; i++)
        {
            if (itemSlots[i] == null || itemSlots[i].itemData == null)
            {
                emptyCount++;
                if (itemData.isStackable || emptyCount >= amount) return true;
            }
        }
        return false;
    }

    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= itemSlots.Length)
            return;

        if (indexB < 0 || indexB >= itemSlots.Length)
            return;

        if (indexA == indexB)
            return;

        if (itemSlots[indexA] == null) itemSlots[indexA] = new ItemSlot();
        if (itemSlots[indexB] == null) itemSlots[indexB] = new ItemSlot();

        // If same item and stackable -> Merge stack into slot B instead of swapping
        if (itemSlots[indexA].itemData != null &&
            IsItemMatch(itemSlots[indexA].itemData, itemSlots[indexB].itemData) &&
            itemSlots[indexA].itemData.isStackable)
        {
            itemSlots[indexB].amount += itemSlots[indexA].amount;
            itemSlots[indexA].itemData = null;
            itemSlots[indexA].amount = 0;

            TriggerChange();
            return;
        }

        ItemData tempItem = itemSlots[indexA].itemData;
        int tempAmount = itemSlots[indexA].amount;

        itemSlots[indexA].itemData = itemSlots[indexB].itemData;
        itemSlots[indexA].amount = itemSlots[indexB].amount;

        itemSlots[indexB].itemData = tempItem;
        itemSlots[indexB].amount = tempAmount;

        TriggerChange();
    }

    /// <summary>
    /// Notify listeners when slot contents are modified externally (e.g. cross-container transfer)
    /// </summary>
    public void NotifyChange()
    {
        TriggerChange();
    }

    /// <summary>
    /// Clears all slots in this container
    /// </summary>
    public void Clear()
    {
        if (itemSlots != null)
        {
            for (int i = 0; i < itemSlots.Length; i++)
            {
                if (itemSlots[i] != null)
                {
                    itemSlots[i].itemData = null;
                    itemSlots[i].amount = 0;
                }
            }
        }
        TriggerChange();
    }
}