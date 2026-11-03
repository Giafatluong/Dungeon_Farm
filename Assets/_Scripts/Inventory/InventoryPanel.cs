using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class InventoryPanel : MonoBehaviour
{
    public static InventoryPanel Instance { get; private set; }

    [SerializeField] private ItemContainer itemContainer;
    [SerializeField] private List<InventoryButton> inventoryButtons;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CurrentItemPanel currentItemPanel;

    private GameObject statsBarGO;
    private TextMeshProUGUI statsBarText;

    public ItemContainer ItemContainer => itemContainer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void EnsureItemContainer()
    {
        if (itemContainer == null)
        {
            if (PlayerStats.Instance != null && PlayerStats.Instance.itemContainer != null)
            {
                itemContainer = PlayerStats.Instance.itemContainer;
            }
            else
            {
                PlayerStats ps = FindFirstObjectByType<PlayerStats>();
                if (ps != null && ps.itemContainer != null) itemContainer = ps.itemContainer;
            }
        }
    }

    private void EnsureButtons()
    {
        if (inventoryButtons == null || inventoryButtons.Count == 0)
        {
            inventoryButtons = new List<InventoryButton>(GetComponentsInChildren<InventoryButton>(true));
        }
    }

    private void Start()
    {
        EnsureItemContainer();
        EnsureButtons();

        for (int i = 0; i < inventoryButtons.Count; i++)
        {
            if (inventoryButtons[i] == null) continue;
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
        EnsureItemContainer();
        EnsureButtons();

        if (itemContainer != null)
        {
            itemContainer.OnInventoryChange -= RefreshInventory;
            itemContainer.OnInventoryChange += RefreshInventory;
        }

        // Always refresh all slot buttons immediately when inventory panel opens
        RefreshInventory();
        RemoveStatsBar();
    }

    private void OnDisable()
    {
        if (itemContainer != null)
        {
            itemContainer.OnInventoryChange -= RefreshInventory;
        }

        RemoveStatsBar();

        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.isInteractable = true;
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

    private void RemoveStatsBar()
    {
        if (statsBarGO != null)
        {
            Destroy(statsBarGO);
            statsBarGO = null;
        }
    }

    public void SetInventoryButton()
    {
        EnsureItemContainer();
        EnsureButtons();
        if (inventoryButtons == null || itemContainer == null || itemContainer.itemSlots == null)
            return;

        for (int i = 0; i < inventoryButtons.Count; i++)
        {
            if (inventoryButtons[i] == null) continue;

            if (i < itemContainer.itemSlots.Length &&
                itemContainer.itemSlots[i] != null &&
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