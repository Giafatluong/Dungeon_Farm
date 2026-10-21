using System.Collections.Generic;
using UnityEngine;

public class Statue : MonoBehaviour, IInteractable
{
    #region Inspector Fields & Events
    [Header("List of Offerings")]
    public List<Offering> offerings = new();
    public ItemContainer playerInventory;

    [Header("Interaction Settings")]
    [Tooltip("Maximum distance from the statue collider for the player to interact")]
    public float interactDistance = 2.2f;

    public event System.Action<Offering> OnOfferingSuccess;
    public event System.Action<string> OnOfferingFailed;

    public void NotifyOfferingSuccess(Offering offering)
    {
        OnOfferingSuccess?.Invoke(offering);
    }
    #endregion

    public static Statue Instance { get; private set; }

    #region State & References
    private PlayerStats playerStats;
    public bool playerInRange { get; private set; } = false;
    private Transform playerTransform;
    private Collider2D col;
    private UnityEngine.Tilemaps.Tilemap tilemap;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        col = GetComponent<Collider2D>();
        tilemap = GetComponent<UnityEngine.Tilemaps.Tilemap>();

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

    private float nextPlayerSearchTime = 0f;

    private void Update()
    {
        if (playerTransform == null)
        {
            if (Time.time >= nextPlayerSearchTime)
            {
                nextPlayerSearchTime = Time.time + 0.5f;
                FindPlayer();
            }
        }

        if (playerTransform != null)
        {
            Vector2 statuePoint = col != null ? col.ClosestPoint(playerTransform.position) : (Vector2)GetStatueCenter();
            float dist = Vector2.Distance(statuePoint, playerTransform.position);
            bool wasInRange = playerInRange;
            playerInRange = dist <= interactDistance;

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
            if (DayTransitionUI.Instance != null && DayTransitionUI.Instance.IsOpen) return;
            if (KitchenUI.Instance != null && KitchenUI.Instance.IsOpen) return;
            if (ChestUI.Instance != null && ChestUI.Instance.IsOpen) return;
            Interact();
        }
    }

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        if (playerInRange)
        {
            Interact();
        }
    }
    #endregion

    #region Interaction Handling
    public void Interact()
    {
        EnsureReferences();

        StatueUI ui = StatueUI.EnsureInstance();
        if (ui != null)
        {
            if (ui.IsOpen)
            {
                ui.Close();
            }
            else
            {
                ui.Open(this, offerings, playerInventory);
            }
        }
        else
        {
            TryOfferAny();
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

    private void FindPlayer()
    {
        if (PlayerStats.Instance != null)
        {
            playerStats = PlayerStats.Instance;
            playerTransform = PlayerStats.Instance.transform;
            return;
        }
        if (PlayerMovement.Instance != null)
        {
            playerTransform = PlayerMovement.Instance.transform;
            playerStats = playerTransform.GetComponent<PlayerStats>();
            return;
        }
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerStats = player.GetComponent<PlayerStats>();
        }
    }

    private void EnsureReferences()
    {
        if (playerInventory == null)
        {
            InventoryPanel invPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            if (invPanel != null && invPanel.ItemContainer != null)
            {
                playerInventory = invPanel.ItemContainer;
            }
        }
        if (playerInventory == null)
        {
            PlayerStats ps = FindFirstObjectByType<PlayerStats>();
            if (ps != null && ps.itemContainer != null)
            {
                playerInventory = ps.itemContainer;
            }
        }
        if (playerInventory == null)
        {
            ItemContainer[] containers = GameAssetHelper.LoadAll<ItemContainer>();
            for (int i = 0; i < containers.Length; i++)
            {
                if (containers[i] != null && !containers[i].name.ToLower().Contains("chest") && !containers[i].name.ToLower().Contains("loot"))
                {
                    playerInventory = containers[i];
                    break;
                }
            }
        }
    }
    #endregion

    #region Offering Logic
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

        if (playerStats != null)
        {
            playerStats.RefreshStatsFromProgression();
            DamagePopupManager.ShowText(playerStats.transform.position + Vector3.up * 1.5f, $"+{offering.rewardAmount} {offering.rewardStat} PERMANENT!", new Color(1f, 0.85f, 0.2f), 4.5f);
        }
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

        System.Text.StringBuilder sb = new();
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
        return container.GetItemCount(item);
    }
    #endregion

    #region Gizmos & Debug
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
    #endregion
}
