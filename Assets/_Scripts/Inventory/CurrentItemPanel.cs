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

    private void Update()
    {
        // Phím tắt 1-9 chọn nhanh vật phẩm trên thanh công cụ khi không ở trong trận chiến và không mở modal/túi đồ
        if (CombatManager.Instance != null && (CombatManager.Instance.isCombatActive || CombatManager.Instance.isPreparingCombat))
            return;

        if (InventoryController.IsAnyModalOpen() || (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen))
            return;

        if (inventoryButtons == null || inventoryButtons.Count == 0) return;

        for (int i = 0; i < Mathf.Min(inventoryButtons.Count, 9); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                if (inventoryButtons[i] != null)
                {
                    inventoryButtons[i].SelectItem();
                }
                break;
            }
        }
    }

    public void SetCurrentItem()
    {
        if (inventoryPanel == null)
        {
            inventoryPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
        }

        if (inventoryPanel == null || inventoryPanel.ItemContainer == null || inventoryButtons == null)
            return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        int count = Mathf.Min(11, inventoryButtons.Count);
        for (int i = 0; i < count; i++)
        {
            if (inventoryButtons[i] == null) continue;

            inventoryButtons[i].SetSlotData(inventoryPanel.ItemContainer, i);
            if (parentCanvas != null)
            {
                inventoryButtons[i].SetCanvas(parentCanvas);
            }

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