using System.Collections.Generic;
using UnityEngine;

public class Statue : MonoBehaviour, IInteractable
{
    [Header("List of Offerings")]
    public List<Offering> offerings = new List<Offering>();
    public ItemContainer playerInventory;

    [Header("Interaction Settings")]
    [Tooltip("Maximum distance from the statue collider for the player to interact")]
    public float interactDistance = 1.8f;

    public event System.Action<Offering> OnOfferingSuccess;
    public event System.Action<string> OnOfferingFailed;

    private bool playerInRange = false;
    private Transform playerTransform;
    private Collider2D col;
    private UnityEngine.Tilemaps.Tilemap tilemap;

    private void Start()
    {
        col = GetComponent<Collider2D>();
        tilemap = GetComponent<UnityEngine.Tilemaps.Tilemap>();

        // Ensure collider tightly bounds the statue visual instead of a giant zone
        if (col is BoxCollider2D boxCol)
        {
            if (boxCol.size.x > 2.5f || boxCol.size.y > 3.5f)
            {
                boxCol.size = new Vector2(2f, 2.5f);
                boxCol.offset = new Vector2(40.5f, 11.25f);
            }
        }

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
        return new Vector3(40.5f, 11.25f, 0f);
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
        }

        if (playerTransform != null)
        {
            Vector2 statuePoint = col != null ? (Vector2)col.ClosestPoint(playerTransform.position) : (Vector2)GetStatueCenter();
            float dist = Vector2.Distance(statuePoint, playerTransform.position);
            bool wasInRange = playerInRange;
            playerInRange = (dist <= interactDistance);

            if (!wasInRange && playerInRange)
            {
                Debug.Log("[Statue] Near the Statue! Click on it or press E to make offerings.");
            }

            if (wasInRange && !playerInRange)
            {
                if (StatueUI.Instance != null && StatueUI.Instance.IsOpen)
                {
                    StatueUI.Instance.Close();
                }
            }
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current == null || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                Vector2 mouseWorld = Camera.main != null ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) : Vector2.zero;
                if (col != null && col.OverlapPoint(mouseWorld) && playerInRange)
                {
                    Interact();
                }
            }
        }
    }

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        if (playerInRange)
        {
            Interact();
        }
    }

    public void Interact()
    {
        EnsureReferences();

        if (StatueUI.Instance != null)
        {
            if (StatueUI.Instance.IsOpen)
            {
                StatueUI.Instance.Close();
            }
            else
            {
                StatueUI.Instance.Open(this, offerings, playerInventory);
            }
        }
        else
        {
            TryOfferAny();
        }
    }

    public bool CanOffer(Offering offering, ItemContainer container)
    {
        if (offering == null || container == null) return false;

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
            OnOfferingFailed?.Invoke("Not enough items or offering already completed.");
            return false;
        }

        for (int i = 0; i < offering.requiredItems.Length; i++)
        {
            ItemRequirement req = offering.requiredItems[i];
            if (req.item != null && req.amount > 0)
            {
                container.RemoveItem(req.item, req.amount);
            }
        }

        offering.completed = true;
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.CompleteOffering(offering.offeringKey);
            ProgressionManager.Instance.AddPermanentStat(offering.rewardStat, offering.rewardAmount);
        }

        Debug.Log($"Offering successful: {offering.offeringName}! Received +{offering.rewardAmount} {offering.rewardStat} permanently.");
        OnOfferingSuccess?.Invoke(offering);
        return true;
    }

    public bool TryOfferAny()
    {
        if (playerInventory == null)
        {
            Debug.LogWarning("[Statue] Player inventory is not assigned.");
            return false;
        }

        for (int i = 0; i < offerings.Count; i++)
        {
            if (CanOffer(offerings[i], playerInventory))
            {
                return MakeOffering(offerings[i], playerInventory);
            }
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== [STATUE OF OFFERINGS] Insufficient items for any offering ===");
        for (int i = 0; i < offerings.Count; i++)
        {
            Offering off = offerings[i];
            if (off == null) continue;
            bool isDone = off.completed || (ProgressionManager.Instance != null && ProgressionManager.Instance.IsOfferingCompleted(off.offeringKey));
            if (isDone)
            {
                sb.AppendLine($"- {off.offeringName}: [COMPLETED]");
                continue;
            }

            sb.Append($"- {off.offeringName}: ");
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
    }

    private void OnTriggerExit2D(Collider2D other)
    {
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size + Vector3.one * (interactDistance * 2f));
        }
        else
        {
            Gizmos.DrawWireSphere(GetStatueCenter(), interactDistance);
        }
    }
}
