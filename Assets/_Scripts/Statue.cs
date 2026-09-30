using System.Collections.Generic;
using UnityEngine;

public class Statue : MonoBehaviour, IInteractable
{
    [Header("Danh sách các combo dâng lễ")]
    public List<Offering> offerings = new List<Offering>();
    public ItemContainer playerInventory;

    public event System.Action<Offering> OnOfferingSuccess;
    public event System.Action<string> OnOfferingFailed;

    private bool playerInRange = false;
    private bool isTriggerActive = false;
    private Transform playerTransform;
    private Collider2D col;
    private UnityEngine.Tilemaps.Tilemap tilemap;

    private void Start()
    {
        col = GetComponent<Collider2D>();
        tilemap = GetComponent<UnityEngine.Tilemaps.Tilemap>();
        FindPlayer();
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (playerInventory == null)
        {
            PlayerStats ps = FindFirstObjectByType<PlayerStats>();
            if (ps != null && ps.itemContainer != null)
            {
                playerInventory = ps.itemContainer;
            }
        }
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    public Vector3 GetStatueCenter()
    {
        if (col != null)
        {
            return col.bounds.center;
        }
        if (tilemap != null && tilemap.cellBounds.size.x > 0)
        {
            return tilemap.localBounds.center + transform.position;
        }
        // Tọa độ thực tế của tile Tượng Thần (40, 10)
        return new Vector3(40.5f, 11.5f, 0f);
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
        }

        if (playerTransform != null)
        {
            Vector3 center = GetStatueCenter();
            float dist = Vector2.Distance(center, playerTransform.position);
            bool wasInRange = playerInRange;
            playerInRange = isTriggerActive || (dist <= 3.5f);

            if (!wasInRange && playerInRange)
            {
                Debug.Log("⛩️ [Tượng Thần] Đang đứng gần Tượng Thần! Nhấn phím E để dâng lễ.");
            }
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    public void Interact()
    {
        Debug.Log("⛩️ [Tượng Thần] Nhấn phím E - Đang kiểm tra lễ vật dâng Tượng Thần...");
        EnsureReferences();
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

        // Kiểm tra xem có combo nào đủ điều kiện để dâng lễ không
        for (int i = 0; i < offerings.Count; i++)
        {
            if (CanOffer(offerings[i], playerInventory))
            {
                return MakeOffering(offerings[i], playerInventory);
            }
        }

        // Nếu không đủ điều kiện, hiển thị tiến độ chi tiết từng combo để người chơi biết
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== [TƯỢNG THẦN DÂNG LỄ] Chưa đủ lễ vật để dâng ===");
        for (int i = 0; i < offerings.Count; i++)
        {
            Offering off = offerings[i];
            if (off == null) continue;
            bool isDone = off.completed || (ProgressionManager.Instance != null && ProgressionManager.Instance.IsOfferingCompleted(off.offeringKey));
            if (isDone)
            {
                sb.AppendLine($"• {off.offeringName}: [ĐÃ HOÀN THÀNH]");
                continue;
            }

            sb.Append($"• {off.offeringName}: ");
            if (off.requiredItems != null)
            {
                for (int r = 0; r < off.requiredItems.Length; r++)
                {
                    ItemRequirement req = off.requiredItems[r];
                    if (req.item != null)
                    {
                        int current = GetItemCount(playerInventory, req.item);
                        sb.Append($"{req.item.itemName} ({current}/{req.amount}) ");
                    }
                }
            }
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
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
            isTriggerActive = true;
            playerInRange = true;
            Debug.Log("⛩️ [Tượng Thần] Đã bước vào vùng dâng lễ của Tượng Thần. Nhấn phím E để dâng lễ!");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isTriggerActive = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(GetStatueCenter(), 3.5f);
    }
}
