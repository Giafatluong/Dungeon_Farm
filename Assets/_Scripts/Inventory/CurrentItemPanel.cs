using UnityEngine;
using System.Collections.Generic;

public class CurrentItemPanel : MonoBehaviour
{
    public static CurrentItemPanel Instance { get; private set; }

    [SerializeField] private InventoryPanel inventoryPanel;
    [SerializeField] private List<InventoryButton> inventoryButtons;

    private ItemContainer _subscribedContainer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    public static void RefreshHotbar()
    {
        if (Instance != null)
        {
            Instance.SetCurrentItem();
        }
        else
        {
            CurrentItemPanel panel = FindFirstObjectByType<CurrentItemPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                panel.SetCurrentItem();
            }
        }
    }

    private void EnsureInventoryPanel()
    {
        if (inventoryPanel == null)
        {
            inventoryPanel = InventoryPanel.Instance != null ? InventoryPanel.Instance : FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
        }
    }

    public ItemContainer GetItemContainer()
    {
        EnsureInventoryPanel();
        if (inventoryPanel != null && inventoryPanel.ItemContainer != null)
        {
            return inventoryPanel.ItemContainer;
        }

        if (PlayerStats.Instance != null && PlayerStats.Instance.itemContainer != null)
        {
            return PlayerStats.Instance.itemContainer;
        }

        PlayerStats ps = FindFirstObjectByType<PlayerStats>(FindObjectsInactive.Include);
        if (ps != null && ps.itemContainer != null)
        {
            return ps.itemContainer;
        }

        ItemContainer[] containers = GameAssetHelper.LoadAll<ItemContainer>();
        if (containers != null)
        {
            for (int i = 0; i < containers.Length; i++)
            {
                if (containers[i] != null && !containers[i].name.ToLower().Contains("chest") && !containers[i].name.ToLower().Contains("loot"))
                {
                    return containers[i];
                }
            }
        }

        return null;
    }

    private void Awake()
    {
        Instance = this;
        EnsureInventoryPanel();
        EnsureButtons();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        UnsubscribeContainerEvent();
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
        EnsureInventoryPanel();
        EnsureButtons();
        SubscribeContainerEvent();
        SetCurrentItem();
    }

    private void OnEnable()
    {
        EnsureInventoryPanel();
        EnsureButtons();
        SubscribeContainerEvent();
        SetCurrentItem();
    }

    private void OnDisable()
    {
        UnsubscribeContainerEvent();
    }

    private void SubscribeContainerEvent()
    {
        ItemContainer container = GetItemContainer();
        if (container == null) return;

        if (_subscribedContainer != null && _subscribedContainer != container)
        {
            _subscribedContainer.OnInventoryChange -= SetCurrentItem;
        }

        _subscribedContainer = container;
        _subscribedContainer.OnInventoryChange -= SetCurrentItem;
        _subscribedContainer.OnInventoryChange += SetCurrentItem;
    }

    private void UnsubscribeContainerEvent()
    {
        if (_subscribedContainer != null)
        {
            _subscribedContainer.OnInventoryChange -= SetCurrentItem;
            _subscribedContainer = null;
        }
    }

    private void Update()
    {
        // Number keys 1-9 to select items on hotbar when outside combat and no modal/bag is open
        if (CombatManager.Instance != null && (CombatManager.Instance.isCombatActive || CombatManager.Instance.isPreparingCombat))
            return;

        if (InventoryController.IsAnyModalOpen() || (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen))
            return;

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsInputBlocked)
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
        EnsureInventoryPanel();
        EnsureButtons();
        SubscribeContainerEvent();

        ItemContainer container = GetItemContainer();
        if (container == null || inventoryButtons == null)
            return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        int count = Mathf.Min(11, inventoryButtons.Count);
        for (int i = 0; i < count; i++)
        {
            if (inventoryButtons[i] == null) continue;

            inventoryButtons[i].SetSlotData(container, i);
            if (parentCanvas != null)
            {
                inventoryButtons[i].SetCanvas(parentCanvas);
            }

            if (container.itemSlots != null &&
                i < container.itemSlots.Length &&
                container.itemSlots[i] != null &&
                container.itemSlots[i].itemData != null &&
                container.itemSlots[i].amount > 0)
            {
                inventoryButtons[i].SetItem(container.itemSlots[i].itemData, container.itemSlots[i].amount);
            }
            else
            {
                inventoryButtons[i].ClearItem();
            }
        }
    }
}