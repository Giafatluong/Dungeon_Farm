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
                    if (itemSlots[i].itemData == itemData)
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
            if (itemSlots[i].itemData == itemData)
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
            if (itemSlots[i].itemData == itemData &&
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
            if (itemSlots[i].itemData == null)
            {
                return i;
            }
        }

        return -1;
    }

    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= itemSlots.Length)
            return;

        if (indexB < 0 || indexB >= itemSlots.Length)
            return;

        if (indexA == indexB)
            return;

        ItemData tempItem = itemSlots[indexA].itemData;
        int tempAmount = itemSlots[indexA].amount;

        itemSlots[indexA].itemData = itemSlots[indexB].itemData;
        itemSlots[indexA].amount = itemSlots[indexB].amount;

        itemSlots[indexB].itemData = tempItem;
        itemSlots[indexB].amount = tempAmount;

        OnInventoryChange?.Invoke();
    }
}