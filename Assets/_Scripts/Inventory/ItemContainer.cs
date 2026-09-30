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
            }
        }
        else
        {
            for (int i = 0; i < amount; i++)
            {
                int slot = GetEmptySlot();

                if (slot == -1)
                {
                    return;
                }

                if (itemSlots[slot] == null) itemSlots[slot] = new ItemSlot();
                itemSlots[slot].itemData = itemData;
                itemSlots[slot].amount = 1;
            }

            OnInventoryChange?.Invoke();
        }
    }

    public void RemoveItem(ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0)
            return;

        for (int i = 0; i < maxSlots; i++)
        {
            if (itemSlots[i] != null && itemSlots[i].itemData == itemData)
            {
                itemSlots[i].amount -= amount;

                if (itemSlots[i].amount <= 0)
                {
                    itemSlots[i].itemData = null;
                    itemSlots[i].amount = 0;

                    if (InventoryButton.selectedItem == itemData)
                    {
                        InventoryButton.selectedItem = null;
                    }
                }

                OnInventoryChange?.Invoke();
                return;
            }
        }
    }

    public bool HasItem(ItemData itemData)
    {
        if (itemData == null)
            return false;

        for (int i = 0; i < maxSlots; i++)
        {
            if (itemSlots[i] != null &&
                itemSlots[i].itemData == itemData &&
                itemSlots[i].amount > 0)
            {
                return true;
            }
        }

        return false;
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

        // Nếu cùng loại item và stack được -> Gộp stack vào slot B thay vì hoán đổi
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
    /// Gọi thủ công khi dữ liệu slot bị thay đổi từ bên ngoài (ví dụ: cross-container transfer)
    /// </summary>
    public void NotifyChange()
    {
        OnInventoryChange?.Invoke();
    }
}