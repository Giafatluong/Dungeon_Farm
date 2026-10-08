using UnityEngine;
using System.Collections.Generic;

public class CurrentItemPanel : MonoBehaviour
{
    [SerializeField] private InventoryPanel inventoryPanel;
    [SerializeField] private List<InventoryButton> inventoryButtons;

    private void Awake()
    {
        if (inventoryPanel == null)
        {
            inventoryPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
        }
    }

    private void Start()
    {
        SetCurrentItem();
    }

    private void OnEnable()
    {
        if (inventoryPanel == null)
        {
            inventoryPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
        }

        if (inventoryPanel != null && inventoryPanel.ItemContainer != null)
        {
            inventoryPanel.ItemContainer.OnInventoryChange += SetCurrentItem;
        }
    }

    private void OnDisable()
    {
        if (inventoryPanel != null && inventoryPanel.ItemContainer != null)
        {
            inventoryPanel.ItemContainer.OnInventoryChange -= SetCurrentItem;
        }
    }

    public void SetCurrentItem()
    {
        if (inventoryPanel == null || inventoryPanel.ItemContainer == null || inventoryButtons == null)
            return;

        int count = Mathf.Min(11, inventoryButtons.Count);
        for (int i = 0; i < count; i++)
        {
            if (inventoryButtons[i] == null) continue;

            if (i < inventoryPanel.ItemContainer.itemSlots.Length &&
                inventoryPanel.ItemContainer.itemSlots[i] != null &&
                inventoryPanel.ItemContainer.itemSlots[i].itemData != null)
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