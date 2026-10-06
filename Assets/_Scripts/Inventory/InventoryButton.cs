using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class InventoryButton : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemAmount;

    public ItemData currentItem;
    public static ItemData selectedItem;

    public static event Action<ItemData> OnItemSelected;

    private ItemContainer itemContainer;
    private int slotIndex;

    private GameObject dragIcon;
    private Canvas canvas;

    // Accessible externally (ChestUI uses container/index for cross-container drops)
    public ItemContainer SlotContainer => itemContainer;
    public int SlotIndex => slotIndex;

    private void Awake()
    {
        if (itemIcon == null)
        {
            Transform iconT = transform.Find("Icon") ?? transform.Find("ItemIcon") ?? transform.Find("Image");
            if (iconT != null) itemIcon = iconT.GetComponent<Image>();
        }
        if (itemAmount == null)
        {
            itemAmount = GetComponentInChildren<TextMeshProUGUI>(true);
        }
        FixTextLayout();
    }

    private void FixTextLayout()
    {
        if (itemAmount != null)
        {
            RectTransform rt = itemAmount.rectTransform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-3f, 3f);
            rt.sizeDelta = new Vector2(35f, 18f);
            itemAmount.alignment = TextAlignmentOptions.BottomRight;
            itemAmount.characterSpacing = 0f;
            itemAmount.transform.SetAsLastSibling();
        }
    }

    public void SetSlotData(ItemContainer container, int index)
    {
        itemContainer = container;
        slotIndex = index;
    }

    public void SetCanvas(Canvas targetCanvas)
    {
        canvas = targetCanvas;
    }

    private void OnDisable()
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
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

        if (itemIcon == null)
        {
            Transform iconT = transform.Find("Icon") ?? transform.Find("ItemIcon") ?? transform.Find("Image");
            if (iconT != null) itemIcon = iconT.GetComponent<Image>();
        }

        if (itemAmount == null)
        {
            itemAmount = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(true);
            itemIcon.sprite = itemData.itemIcon;
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
    }

    public void ClearItem()
    {
        currentItem = null;

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

    public void SelectItem()
    {
        if (currentItem != null)
        {
            selectedItem = currentItem;
            OnItemSelected?.Invoke(currentItem);
        }
    }

    private ScrollRect parentScrollRect;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null || itemIcon == null || itemIcon.sprite == null)
        {
            if (parentScrollRect == null) parentScrollRect = GetComponentInParent<ScrollRect>();
            if (parentScrollRect != null) parentScrollRect.OnBeginDrag(eventData);
            return;
        }

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();

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
            itemContainer.SwapSlots(draggedButton.SlotIndex, slotIndex);
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