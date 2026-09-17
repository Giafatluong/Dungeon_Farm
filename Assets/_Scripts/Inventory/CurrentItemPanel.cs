using UnityEngine;
using System.Collections.Generic;

public class CurrentItemPanel : MonoBehaviour
{
    [SerializeField] private InventoryPanel inventoryPanel;
    [SerializeField] private List<InventoryButton> inventoryButtons;

    private void Start()
    {
        SetCurrentItem();
    }

    private void OnEnable()
    {
        inventoryPanel.ItemContainer.OnInventoryChange += SetCurrentItem;
    }
    private void OnDisable()
    {
        inventoryPanel.ItemContainer.OnInventoryChange -= SetCurrentItem;
    }

    public void SetCurrentItem()
    {
        for (int i = 0; i < 11; i++)
        {
            if (i < inventoryPanel.ItemContainer.itemSlots.Length && inventoryPanel.ItemContainer.itemSlots[i].itemData != null)
            {
                inventoryButtons[i].SetItem(inventoryPanel.ItemContainer.itemSlots[i].itemData, inventoryPanel.ItemContainer.itemSlots[i].amount);
            }
            else
            {
                inventoryButtons[i].ClearItem();
            }
        }
    }
}