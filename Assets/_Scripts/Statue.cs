using UnityEngine;
using System.Collections.Generic;

public class Statue : MonoBehaviour, IInteractable
{
    [Header("Danh sách các combo dâng lễ")]
    public List<Offering> offerings = new List<Offering>();
    public ItemContainer playerInventory;

    public event System.Action<Offering> OnOfferingSuccess;
    public event System.Action<string> OnOfferingFailed;

    private bool playerInRange = false;

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    public void Interact()
    {
        Debug.Log("Đang tương tác với Tượng dâng lễ...");
        // Thử dâng combo đầu tiên còn khả dụng
        TryOfferAny();
    }

    public bool CanOffer(Offering offering, ItemContainer container)
    {
        if (offering == null || container == null) return false;

        // Kiểm tra đã hoàn thành trước đó chưa (không thể dâng lại combo đã hoàn thành)
        if (offering.completed) return false;
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.IsOfferingCompleted(offering.offeringKey))
        {
            offering.completed = true;
            return false;
        }

        if (offering.requiredItems == null || offering.requiredItems.Length == 0) return false;

        for (int i = 0; i < offering.requiredItems.Length; i++)
        {
            ItemRequirement req = offering.requiredItems[i];
            if (req.item == null || req.amount <= 0) continue;

            if (GetItemCount(container, req.item) < req.amount)
            {
                return false;
            }
        }

        return true;
    }

    public bool MakeOffering(Offering offering, ItemContainer container)
    {
        if (offering == null || container == null) return false;

        if (!CanOffer(offering, container))
        {
            OnOfferingFailed?.Invoke("Chưa đủ lễ vật hoặc combo đã hoàn thành.");
            return false;
        }

        // Trừ vật phẩm
        for (int i = 0; i < offering.requiredItems.Length; i++)
        {
            ItemRequirement req = offering.requiredItems[i];
            if (req.item != null && req.amount > 0)
            {
                container.RemoveItem(req.item, req.amount);
            }
        }

        // Đánh dấu hoàn thành
        offering.completed = true;
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.CompleteOffering(offering.offeringKey);
            ProgressionManager.Instance.AddPermanentStat(offering.rewardStat, offering.rewardAmount);
        }

        Debug.Log($"Dâng lễ thành công combo: {offering.offeringName}! Nhận +{offering.rewardAmount} {offering.rewardStat} vĩnh viễn.");
        OnOfferingSuccess?.Invoke(offering);
        return true;
    }

    public bool TryOfferAny()
    {
        if (playerInventory == null)
        {
            Debug.LogWarning("Túi đồ người chơi chưa được gán trên Tượng.");
            return false;
        }

        for (int i = 0; i < offerings.Count; i++)
        {
            if (CanOffer(offerings[i], playerInventory))
            {
                return MakeOffering(offerings[i], playerInventory);
            }
        }

        Debug.Log("Không có combo nào đủ điều kiện để dâng lễ lúc này.");
        return false;
    }

    private int GetItemCount(ItemContainer container, ItemData item)
    {
        if (container == null || item == null) return 0;
        int total = 0;
        for (int i = 0; i < container.itemSlots.Length; i++)
        {
            if (container.itemSlots[i].itemData == item)
            {
                total += container.itemSlots[i].amount;
            }
        }
        return total;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Đến gần Tượng dâng lễ. Nhấn E để tương tác.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
