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
        if (itemSlots == null || itemSlots.Length != maxSlots)
        {
            itemSlots = new ItemSlot[maxSlots];
        }

        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] == null)
            {
                itemSlots[i] = new ItemSlot();
            }
        }
    }

    public void AddItem(ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0)
            return;

        if (itemData.isStackable)
        {
            if (HasItem(itemData))
            {
                for (int i = 0; i < maxSlots; i++)
                {
                    if (itemSlots[i] != null && itemSlots[i].itemData == itemData)
                    {
                        itemSlots[i].amount += amount;
                        OnInventoryChange?.Invoke();
                        return;
                    }
                }
            }

            int slot = GetEmptySlot();

            if (slot != -1)
            {
                if (itemSlots[slot] == null) itemSlots[slot] = new ItemSlot();
                itemSlots[slot].itemData = itemData;
                itemSlots[slot].amount = amount;
                OnInventoryChange?.Invoke();
                return;
            }
        }
        else
        {
            for (int i = 0; i < amount; i++)
            {
                int slot = GetEmptySlot();

                if (slot != -1)
                {
                    if (itemSlots[slot] == null) itemSlots[slot] = new ItemSlot();
                    itemSlots[slot].itemData = itemData;
                    itemSlots[slot].amount = 1;
                }
            }

            OnInventoryChange?.Invoke();
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

        for (int i = 0; i < maxSlots; i++)
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
            OnInventoryChange?.Invoke();
        }
    }

    public int GetItemCount(ItemData itemData)
    {
        if (itemData == null || itemSlots == null) return 0;
        int total = 0;
        for (int i = 0; i < maxSlots; i++)
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

        if (requiredAmount == 1)
        {
            for (int i = 0; i < maxSlots; i++)
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
        for (int i = 0; i < maxSlots; i++)
        {
            if (itemSlots[i] == null || itemSlots[i].itemData == null)
            {
                return i;
            }
        }

        return -1;
    }

    public bool CanAddItem(ItemData itemData)
    {
        if (itemData == null) return false;
        if (itemData.isStackable && HasItem(itemData)) return true;
        return GetEmptySlot() != -1;
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
            itemSlots[indexA].itemData == itemSlots[indexB].itemData &&
            itemSlots[indexA].itemData.isStackable)
        {
            itemSlots[indexB].amount += itemSlots[indexA].amount;
            itemSlots[indexA].itemData = null;
            itemSlots[indexA].amount = 0;

            OnInventoryChange?.Invoke();
            return;
        }

        ItemData tempItem = itemSlots[indexA].itemData;
        int tempAmount = itemSlots[indexA].amount;

        itemSlots[indexA].itemData = itemSlots[indexB].itemData;
        itemSlots[indexA].amount = itemSlots[indexB].amount;

        itemSlots[indexB].itemData = tempItem;
        itemSlots[indexB].amount = tempAmount;

        OnInventoryChange?.Invoke();
    }

    /// <summary>
    /// Notify listeners when slot contents are modified externally (e.g. cross-container transfer)
    /// </summary>
    public void NotifyChange()
    {
        OnInventoryChange?.Invoke();
    }
}