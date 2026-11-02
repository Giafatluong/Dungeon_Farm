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

        RemoveStatsBar();
    }

    private void OnDisable()
    {
        if (itemContainer != null)
        {
            itemContainer.OnInventoryChange -= RefreshInventory;
        }

        RemoveStatsBar();
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