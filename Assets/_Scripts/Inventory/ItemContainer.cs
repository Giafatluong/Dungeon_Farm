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
            else
            {
                Debug.Log("Inventory Full");
            }
        }
        else
        {
            for (int i = 0; i < amount; i++)
            {
                int slot = GetEmptySlot();

                if (slot == -1)
                {
                    Debug.Log("Inventory Full!");
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
        for (int i = 0; i < maxSlots; i++)
        {
            if (itemSlots[i].itemData == itemData)
            {
                itemSlots[i].amount -= amount;

                if (itemSlots[i].amount <= 0)
                {
                    itemSlots[i].itemData = null;
                    itemSlots[i].amount = 0;
                }
                OnInventoryChange?.Invoke();
                return;
            }
        }

        Debug.Log("Item not found!");
    }

    public bool HasItem(ItemData itemData)
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (itemSlots[i].itemData == itemData) return true;
        }

        return false;
    }

    public int GetEmptySlot()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (itemSlots[i].itemData == null) return i;
        }

        return -1;
    }
}