using UnityEngine;
using System.Collections.Generic;

public class InventoryPanel : MonoBehaviour
{
    [SerializeField] private ItemContainer itemContainer;
    [SerializeField] private List<InventoryButton> inventoryButtons;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CurrentItemPanel currentItemPanel;

    public ItemContainer ItemContainer => itemContainer;

    private void Start()
    {
        for (int i = 0; i < inventoryButtons.Count; i++)
        {
            inventoryButtons[i].SetSlotData(itemContainer, i);
            inventoryButtons[i].SetCanvas(canvas);
        }

        SetInventoryButton();

        if (currentItemPanel != null)
        {
            currentItemPanel.SetCurrentItem();
        }
    }

    private void OnEnable()
    {
        if (itemContainer != null)
        {
            itemContainer.OnInventoryChange += RefreshInventory;
        }
    }

    private void OnDisable()
    {
        if (itemContainer != null)
        {
            itemContainer.OnInventoryChange -= RefreshInventory;
        }
    }

    private void RefreshInventory()
    {
        SetInventoryButton();

        if (currentItemPanel != null)
        {
            currentItemPanel.SetCurrentItem();
        }
    }

    public void SetInventoryButton()
    {
        for (int i = 0; i < inventoryButtons.Count; i++)
        {
            if (i < itemContainer.itemSlots.Length &&
                itemContainer.itemSlots[i].itemData != null)
            {
                inventoryButtons[i].SetItem(
                    itemContainer.itemSlots[i].itemData,
                    itemContainer.itemSlots[i].amount
                );
            }
            else
            {
                inventoryButtons[i].ClearItem();
            }
        }
    }
}