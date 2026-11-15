using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class InventoryButton : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemAmount;
    [SerializeField] private Image slotBackground;

    [Header("Slot References (Serialized for Prefab Compatibility)")]
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Sprite selectedSlotSprite;
    [SerializeField] private Sprite hoverSlotSprite;
    [SerializeField] private Sprite lockedSlotSprite;

    public ItemData currentItem;

    // Slot-specific selection tracking to prevent duplicate item conflicts
    public static InventoryButton selectedButton { get; private set; }
    public static ItemContainer selectedContainer { get; private set; }
    public static int selectedSlotIndex { get; private set; } = -1;

    private static ItemData _selectedItem;
    public static ItemData selectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            if (value == null)
            {
                selectedButton = null;
                selectedContainer = null;
                selectedSlotIndex = -1;
                OnItemSelected?.Invoke(null);
            }
        }
    }

    public static event Action<ItemData> OnItemSelected;

    private ItemContainer itemContainer;
    private int slotIndex = -1;
    private bool isHovered = false;
    private bool isLocked = false;

    private GameObject dragIcon;
    private Canvas canvas;
    private Color defaultBgColor = Color.white;
    private bool defaultColorSaved = false;
    private Outline slotOutline;

    private static float lastGlobalSelectTime = -1f;

    // Accessible externally (ChestUI uses container/index for cross-container drops)
    public ItemContainer SlotContainer => itemContainer;
    public int SlotIndex => slotIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        selectedButton = null;
        selectedContainer = null;
        selectedSlotIndex = -1;
        _selectedItem = null;
        OnItemSelected = null;
        lastGlobalSelectTime = -1f;
    }

    private void Awake()
    {
        EnsureComponents();

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            // Transition.None prevents Unity Selectable from fighting with our custom hover/selection colors
            btn.transition = Selectable.Transition.None;
            // OnPointerClick already handles click events (including Shift+Click for transfers).
            // Removing btn.onClick listener prevents double-invocation that causes instant deselection!
            btn.onClick.RemoveAllListeners();
        }

        FixTextLayout();

        if (currentItem == null)
        {
            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.gameObject.SetActive(false);
            }
            if (itemAmount != null)
            {
                itemAmount.gameObject.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {
        EnsureComponents();
        OnItemSelected += HandleItemSelected;
        UpdateHighlight();
    }

    private void OnDisable()
    {
        OnItemSelected -= HandleItemSelected;
        isHovered = false;
        transform.localScale = Vector3.one;
        if (slotOutline != null)
        {
            slotOutline.enabled = false;
        }
        ItemTooltipUI.Instance?.Hide();
        if (dragIcon != null)
        {
            Destroy(dragIcon);
        }
    }

    private void EnsureComponents()
    {
        EnsureIcon();
        if (itemAmount == null)
        {
            itemAmount = GetComponentInChildren<TextMeshProUGUI>(true);
        }
        if (slotBackground == null)
        {
            slotBackground = GetComponent<Image>();
            if (slotBackground == null)
            {
                Transform bgT = transform.Find("Background");
                if (bgT != null) slotBackground = bgT.GetComponent<Image>();
            }
            if (slotBackground == null)
            {
                slotBackground = GetComponentInChildren<Image>(true);
            }
        }

        EnsureDefaultColor();
        EnsureOutline();

        // Clean up any lingering selection frames from previous versions
        Transform oldFrame = transform.Find("Selection_Border_Highlight");
        if (oldFrame != null)
        {
            Destroy(oldFrame.gameObject);
        }
    }

    private void EnsureDefaultColor()
    {
        if (!defaultColorSaved && slotBackground != null)
        {
            defaultBgColor = slotBackground.color;
            if (defaultBgColor.a < 0.05f) defaultBgColor = Color.white;
            defaultColorSaved = true;
        }
    }

    private void EnsureOutline()
    {
        if (slotOutline == null && slotBackground != null)
        {
            slotOutline = slotBackground.GetComponent<Outline>();
            if (slotOutline == null)
            {
                slotOutline = slotBackground.gameObject.AddComponent<Outline>();
            }
            slotOutline.useGraphicAlpha = true;
            slotOutline.effectDistance = new Vector2(3f, -3f);
            slotOutline.enabled = false;
        }
    }

    private void EnsureIcon()
    {
        if (itemIcon != null) return;
        Transform iconT = transform.Find("Icon");
        if (iconT == null) iconT = transform.Find("ItemIcon");
        if (iconT == null) iconT = transform.Find("Image");
        if (iconT != null) itemIcon = iconT.GetComponent<Image>();
    }

    private void FixTextLayout()
    {
        if (itemAmount != null)
        {
            itemAmount.transform.SetAsLastSibling();
        }
    }

    public void SetSlotData(ItemContainer container, int index)
    {
        itemContainer = container;
        slotIndex = index;
        UpdateHighlight();
    }

    public void SetCanvas(Canvas targetCanvas)
    {
        canvas = targetCanvas;
    }

    private void HandleItemSelected(ItemData selected)
    {
        UpdateHighlight();
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        UpdateHighlight();
    }

    public bool IsThisSlotSelected()
    {
        if (currentItem == null) return false;
        if (selectedButton != null && selectedButton == this) return true;
        if (selectedContainer != null && itemContainer != null &&
            selectedContainer == itemContainer && selectedSlotIndex == slotIndex && selectedSlotIndex >= 0)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Updates visuals purely via Outline and background tint (similar to OfferingSlot).
    /// Does NOT swap sprites or use separate hover assets, keeping size and inner cavity identical.
    /// </summary>
    public void UpdateHighlight()
    {
        EnsureComponents();
        if (slotBackground == null) return;

        bool isSelected = IsThisSlotSelected();

        // Kích thước 1.0x cố định tuyệt đối, không đổi kích thước khi hover hay click
        transform.localScale = Vector3.one;
        if (itemIcon != null)
        {
            itemIcon.transform.localScale = Vector3.one;
        }

        if (isLocked)
        {
            if (slotOutline != null) slotOutline.enabled = false;
            slotBackground.color = new Color(0.6f, 0.6f, 0.6f, 0.85f);
            return;
        }

        if (isSelected)
        {
            // Trạng thái Đang Chọn: Viền vàng hổ phách nổi bật (3.5px) + nền ánh vàng ấm nhẹ
            if (slotOutline != null)
            {
                slotOutline.enabled = true;
                slotOutline.effectColor = isHovered 
                    ? new Color(1.0f, 1.0f, 0.50f, 1.0f)   // Hover khi đang chọn: vàng sáng chói
                    : new Color(1.0f, 0.82f, 0.25f, 1.0f);  // Đang chọn: vàng hổ phách
                slotOutline.effectDistance = new Vector2(3.5f, -3.5f);
            }
            slotBackground.color = isHovered 
                ? new Color(1.0f, 0.98f, 0.82f, 1.0f) 
                : new Color(1.0f, 0.95f, 0.78f, 1.0f);
        }
        else if (isHovered)
        {
            // Trạng thái Hover: Viền vàng kim rực rỡ (3px) + Nền slot sáng bừng lên rõ rệt tạo cảm giác chuột đang ở đó
            if (slotOutline != null)
            {
                slotOutline.enabled = true;
                slotOutline.effectColor = new Color(1.0f, 0.95f, 0.42f, 1.0f); // Vàng kim sáng rực rõ nét
                slotOutline.effectDistance = new Vector2(3f, -3f);
            }
            Color brightBg = new Color(
                Mathf.Min(1f, defaultBgColor.r * 1.25f + 0.15f),
                Mathf.Min(1f, defaultBgColor.g * 1.25f + 0.15f),
                Mathf.Min(1f, defaultBgColor.b * 1.25f + 0.10f),
                defaultBgColor.a
            );
            slotBackground.color = Color.Lerp(defaultBgColor, brightBg, 0.85f);
        }
        else
        {
            // Trạng thái Bình thường: Tắt viền, màu nền gốc mặc định
            if (slotOutline != null)
            {
                slotOutline.enabled = false;
            }
            slotBackground.color = defaultBgColor;
        }
    }

    public void BindComponents(Image icon, TextMeshProUGUI amount)
    {
        itemIcon = icon;
        itemAmount = amount;
        FixTextLayout();
    }

    public void SetItem(ItemData itemData, int amount)
    {
        currentItem = itemData;

        EnsureIcon();

        if (itemAmount == null)
        {
            itemAmount = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(true);
            itemIcon.sprite = itemData.itemIcon;
            itemIcon.color = Color.white;
        }

        if (itemAmount != null)
        {
            if (itemData.isStackable)
            {
                FixTextLayout();
                itemAmount.gameObject.SetActive(true);
                itemAmount.text = amount.ToString();
            }
            else
            {
                itemAmount.gameObject.SetActive(false);
            }
        }

        if (IsThisSlotSelected())
        {
            _selectedItem = itemData;
        }

        UpdateHighlight();
    }

    public void ClearItem()
    {
        if (IsThisSlotSelected())
        {
            ClearSelection();
        }

        currentItem = null;
        ItemTooltipUI.Instance?.Hide();

        EnsureIcon();
        if (itemAmount == null)
        {
            itemAmount = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }

        if (itemAmount != null)
        {
            itemAmount.gameObject.SetActive(false);
        }

        UpdateHighlight();
    }

    public static void ClearSelection()
    {
        selectedButton = null;
        selectedContainer = null;
        selectedSlotIndex = -1;
        _selectedItem = null;
        OnItemSelected?.Invoke(null);
    }

    public void SelectItem()
    {
        if (currentItem == null)
        {
            // Clicking an empty slot clears active selection
            if (selectedButton != null || selectedSlotIndex >= 0 || _selectedItem != null)
            {
                ClearSelection();
            }
            return;
        }

        if (Time.unscaledTime - lastGlobalSelectTime < 0.08f)
            return;
        lastGlobalSelectTime = Time.unscaledTime;

        if (IsThisSlotSelected())
        {
            // Click already selected slot again -> Deselect
            ClearSelection();
        }
        else
        {
            // Select this specific slot instance
            selectedButton = this;
            selectedContainer = itemContainer;
            selectedSlotIndex = slotIndex;
            _selectedItem = currentItem;
            OnItemSelected?.Invoke(currentItem);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Shift + LeftClick quick transfer to Chest or Loot UI (standard muscle memory)
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (ChestUI.Instance != null && ChestUI.Instance.IsOpen && itemContainer != null)
                {
                    ChestUI.QuickTransfer(itemContainer, slotIndex);
                    return;
                }
                else if (LootUI.Instance != null && LootUI.Instance.IsOpen && itemContainer != null)
                {
                    LootUI.QuickTransfer(itemContainer, slotIndex);
                    return;
                }
            }

            SelectItem();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (ChestUI.Instance != null && ChestUI.Instance.IsOpen && itemContainer != null)
            {
                ChestUI.QuickTransfer(itemContainer, slotIndex);
            }
            else if (LootUI.Instance != null && LootUI.Instance.IsOpen && itemContainer != null)
            {
                LootUI.QuickTransfer(itemContainer, slotIndex);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateHighlight();
        if (currentItem != null && dragIcon == null)
        {
            ItemTooltipUI.EnsureInstance()?.Show(currentItem, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateHighlight();
        ItemTooltipUI.Instance?.Hide();
    }

    private ScrollRect parentScrollRect;

    public void OnBeginDrag(PointerEventData eventData)
    {
        ItemTooltipUI.Instance?.Hide();

        if (currentItem == null || itemIcon == null || itemIcon.sprite == null)
        {
            if (parentScrollRect == null) parentScrollRect = GetComponentInParent<ScrollRect>();
            if (parentScrollRect != null) parentScrollRect.OnBeginDrag(eventData);
            return;
        }

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
            canvas = DungeonUIAssetHelper.GetMainScreenCanvas();

        if (canvas == null)
            return;

        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(canvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        Image image = dragIcon.AddComponent<Image>();
        image.sprite = itemIcon.sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        RectTransform dragRect = dragIcon.GetComponent<RectTransform>();
        RectTransform iconRect = itemIcon.GetComponent<RectTransform>();

        if (dragRect != null && iconRect != null)
        {
            dragRect.sizeDelta = iconRect.rect.size;
        }

        dragIcon.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon == null)
        {
            if (parentScrollRect != null) parentScrollRect.OnDrag(eventData);
            return;
        }

        dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
        }
        else if (parentScrollRect != null)
        {
            parentScrollRect.OnEndDrag(eventData);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventoryButton draggedButton =
            eventData.pointerDrag?.GetComponent<InventoryButton>();

        if (draggedButton == null || draggedButton == this)
            return;

        if (draggedButton.SlotContainer == null || itemContainer == null)
            return;

        // Same container -> swap or merge stack
        if (draggedButton.SlotContainer == itemContainer)
        {
            bool wasDraggedSelected = draggedButton.IsThisSlotSelected();
            bool wasTargetSelected = this.IsThisSlotSelected();

            itemContainer.SwapSlots(draggedButton.SlotIndex, slotIndex);

            // Follow selection smoothly across dragged slot
            if (wasDraggedSelected)
            {
                selectedSlotIndex = slotIndex;
                selectedButton = this;
            }
            else if (wasTargetSelected)
            {
                selectedSlotIndex = draggedButton.SlotIndex;
                selectedButton = draggedButton;
            }
            OnItemSelected?.Invoke(_selectedItem);
        }
        else
        {
            // Different container -> cross-container transfer (Chest <-> Backpack)
            ChestUI.TransferItem(
                draggedButton.SlotContainer, draggedButton.SlotIndex,
                itemContainer, slotIndex
            );
        }
    }
}
