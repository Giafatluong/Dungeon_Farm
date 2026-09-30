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

    // Cho phép đọc từ bên ngoài (ChestUI cần biết container/index khi drop cross-container)
    public ItemContainer SlotContainer => itemContainer;
    public int SlotIndex => slotIndex;

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
        // Dọn dẹp drag icon nếu UI bị ẩn hoặc đóng giữa chừng khi đang kéo
        if (dragIcon != null)
        {
            Destroy(dragIcon);
        }
    }

    public void SetItem(ItemData itemData, int amount)
    {
        currentItem = itemData;

        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(true);
            itemIcon.sprite = itemData.itemIcon;
        }

        if (itemAmount != null)
        {
            if (itemData.isStackable)
            {
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null || itemIcon == null || itemIcon.sprite == null)
            return;

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
            return;

        dragIcon.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
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

        // Cùng container → swap hoặc gộp stack
        if (draggedButton.SlotContainer == itemContainer)
        {
            itemContainer.SwapSlots(draggedButton.SlotIndex, slotIndex);
        }
        else
        {
            // Khác container → cross-container transfer (Chest ↔ Backpack)
            ChestUI.TransferItem(
                draggedButton.SlotContainer, draggedButton.SlotIndex,
                itemContainer, slotIndex
            );
        }
    }
}