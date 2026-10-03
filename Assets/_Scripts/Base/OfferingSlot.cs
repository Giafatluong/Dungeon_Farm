using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// A single item requirement slot inside an Offering Card.
/// Implements IDropHandler so players can drag items from their inventory and drop them here.
/// Also supports clicking to quickly contribute matching items from backpack.
/// </summary>
public class OfferingSlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private GameObject checkmarkObj;
    [SerializeField] private Outline outline;

    // Slot Data
    private Offering parentOffering;
    private int reqIndex;
    private ItemData requiredItem;
    private int requiredAmount;
    private int currentContributed;
    private bool isCompleted;

    public bool IsRequirementFulfilled => currentContributed >= requiredAmount;
    public ItemData RequiredItem => requiredItem;
    public int CurrentContributed => currentContributed;
    public int RequiredAmount => requiredAmount;

    public void Setup(Offering offering, int requirementIndex, ItemRequirement requirement)
    {
        parentOffering = offering;
        reqIndex = requirementIndex;
        requiredItem = requirement.item;
        requiredAmount = requirement.amount;

        // Check if overall offering is completed
        bool offeringDone = parentOffering.completed ||
            (ProgressionManager.Instance != null && ProgressionManager.Instance.IsOfferingCompleted(parentOffering.offeringKey));

        // Read current contribution progress
        if (offeringDone)
        {
            currentContributed = requiredAmount;
            isCompleted = true;
        }
        else if (ProgressionManager.Instance != null)
        {
            currentContributed = ProgressionManager.Instance.GetOfferingProgress(parentOffering.offeringKey, reqIndex);
            isCompleted = currentContributed >= requiredAmount;
        }
        else
        {
            currentContributed = 0;
            isCompleted = false;
        }

        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        isCompleted = currentContributed >= requiredAmount ||
            (parentOffering != null && parentOffering.completed) ||
            (ProgressionManager.Instance != null && parentOffering != null && ProgressionManager.Instance.IsOfferingCompleted(parentOffering.offeringKey));

        if (itemIcon != null)
        {
            if (requiredItem != null && requiredItem.itemIcon != null)
            {
                itemIcon.gameObject.SetActive(true);
                itemIcon.sprite = requiredItem.itemIcon;
                itemIcon.color = isCompleted ? Color.white : new Color(1f, 1f, 1f, 0.85f);
            }
            else
            {
                itemIcon.gameObject.SetActive(false);
            }
        }

        if (countText != null)
        {
            if (isCompleted)
            {
                countText.text = $"<color=#55FF88>{requiredAmount}/{requiredAmount}</color>";
            }
            else
            {
                string curColor = currentContributed > 0 ? "#FFD700" : "#CCCCCC";
                countText.text = $"<color={curColor}>{currentContributed}</color>/{requiredAmount}";
            }
        }

        if (itemNameText != null && requiredItem != null)
        {
            itemNameText.text = requiredItem.itemName;
            itemNameText.color = isCompleted ? new Color(0.35f, 1f, 0.55f) : new Color(0.85f, 0.88f, 0.95f);
        }

        if (checkmarkObj != null)
        {
            checkmarkObj.SetActive(isCompleted);
        }

        if (slotBackground != null)
        {
            if (isCompleted)
            {
                slotBackground.color = new Color(0.12f, 0.28f, 0.18f, 0.95f);
            }
            else if (currentContributed > 0)
            {
                slotBackground.color = new Color(0.24f, 0.22f, 0.14f, 0.95f);
            }
            else
            {
                slotBackground.color = new Color(0.14f, 0.16f, 0.22f, 0.95f);
            }
        }

        if (outline != null)
        {
            outline.effectColor = isCompleted ? new Color(0.3f, 0.9f, 0.4f, 0.8f) : new Color(0.5f, 0.45f, 0.3f, 0.6f);
        }
    }

    /// <summary>
    /// Drag and Drop item from inventory slot onto this requirement slot.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        InventoryButton draggedButton = eventData.pointerDrag.GetComponent<InventoryButton>();
        if (draggedButton == null) return;

        TryContributeFromDrag(draggedButton);
    }

    private void TryContributeFromDrag(InventoryButton dragged)
    {
        if (parentOffering == null || requiredItem == null) return;

        if (isCompleted)
        {
            StatueUI.Instance?.SetFeedback("<color=#88FF88>This requirement has already been satisfied!</color>");
            return;
        }

        if (dragged.currentItem == null) return;

        if (dragged.currentItem != requiredItem)
        {
            StatueUI.Instance?.SetFeedback($"<color=#FF6666>The Gods reject this item! Requires {requiredItem.itemName}.</color>");
            return;
        }

        int needed = requiredAmount - currentContributed;
        if (needed <= 0) return;

        ItemContainer container = dragged.SlotContainer;
        int slotIndex = dragged.SlotIndex;
        if (container == null || slotIndex < 0 || slotIndex >= container.itemSlots.Length) return;

        ItemSlot sourceSlot = container.itemSlots[slotIndex];
        if (sourceSlot == null || sourceSlot.itemData != requiredItem || sourceSlot.amount <= 0) return;

        int deposit = Mathf.Min(sourceSlot.amount, needed);
        if (deposit <= 0) return;

        // Deduct from player container slot
        sourceSlot.amount -= deposit;
        if (sourceSlot.amount <= 0)
        {
            sourceSlot.itemData = null;
            sourceSlot.amount = 0;
        }
        container.NotifyChange();

        // Apply contribution
        ApplyContribution(deposit);
    }

    /// <summary>
    /// Click directly on the requirement slot to contribute matching items from backpack.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isCompleted || parentOffering == null || requiredItem == null) return;

        ItemContainer backpack = StatueUI.Instance?.PlayerBackpack;
        if (backpack == null) return;

        int needed = requiredAmount - currentContributed;
        if (needed <= 0) return;

        int available = GetTotalItemCount(backpack, requiredItem);
        if (available <= 0)
        {
            StatueUI.Instance?.SetFeedback($"<color=#FFAA55>You don't have {requiredItem.itemName} in your backpack.</color>");
            return;
        }

        int deposit = Mathf.Min(available, needed);
        backpack.RemoveItem(requiredItem, deposit);
        backpack.NotifyChange();

        ApplyContribution(deposit);
    }

    public void ApplyContribution(int amount)
    {
        currentContributed += amount;

        if (ProgressionManager.Instance != null && parentOffering != null)
        {
            ProgressionManager.Instance.SetOfferingProgress(parentOffering.offeringKey, reqIndex, currentContributed);
        }

        UpdateVisuals();

        if (StatueUI.Instance != null)
        {
            StatueUI.Instance.SetFeedback($"<color=#FFD700>Offered {amount}x {requiredItem.itemName}! ({currentContributed}/{requiredAmount})</color>");
            StatueUI.Instance.CheckOfferingCompletion(parentOffering);
        }
    }

    private int GetTotalItemCount(ItemContainer container, ItemData item)
    {
        if (container == null || item == null) return 0;
        int count = 0;
        for (int i = 0; i < container.itemSlots.Length; i++)
        {
            if (container.itemSlots[i].itemData == item)
            {
                count += container.itemSlots[i].amount;
            }
        }
        return count;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (outline != null && !isCompleted)
        {
            outline.effectColor = new Color(1f, 0.85f, 0.35f, 1f);
        }
        transform.localScale = Vector3.one * 1.05f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (outline != null)
        {
            outline.effectColor = isCompleted ? new Color(0.3f, 0.9f, 0.4f, 0.8f) : new Color(0.5f, 0.45f, 0.3f, 0.6f);
        }
        transform.localScale = Vector3.one;
    }
}
