using UnityEngine;
using System.Collections.Generic;

public class InventoryPanel : MonoBehaviour
{
    [SerializeField] private ItemContainer itemContainer;
    [SerializeField] private List<InventoryButton> inventoryButtons;

    public ItemContainer ItemContainer => itemContainer; 
    void Start()
    {
        SetInventoryButton();
    }

    private void OnEnable()
    {
        itemContainer.OnInventoryChange += SetInventoryButton;
    }
    private void OnDisable()
    {
        itemContainer.OnInventoryChange -= SetInventoryButton;       
    }

    public void SetInventoryButton()
    {
        for (int i = 0; i < inventoryButtons.Count; i++)
        {
            if (i < ItemContainer.itemSlots.Length && ItemContainer.itemSlots[i].itemData != null)
            {
                inventoryButtons[i].SetItem(ItemContainer.itemSlots[i].itemData, ItemContainer.itemSlots[i].amount);
            }
            else
            {
                inventoryButtons[i].ClearItem();
            }
        }
    }
}