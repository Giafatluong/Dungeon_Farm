using UnityEngine;

public class HomeChest : MonoBehaviour, IInteractable
{
    public static HomeChest Instance { get; private set; }

    [Header("Containers")]
    public ItemContainer chestContainer;
    public ItemContainer backpackContainer;

    [Header("Interaction")]
    public bool playerInRange;
    public bool isOpen
    {
        get => ChestUI.Instance != null && ChestUI.Instance.IsOpen;
        set { }
    }

    public event System.Action<bool> OnChestToggled;
    public event System.Action OnChestContentsChanged;

    private Transform playerTransform;
    private Collider2D col;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        col = GetComponent<Collider2D>();
        FindPlayer();

        if (backpackContainer == null)
        {
            InventoryPanel invPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            if (invPanel != null)
            {
                backpackContainer = invPanel.ItemContainer;
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

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
        }

        if (playerTransform != null)
        {
            Vector3 center = col != null ? col.bounds.center : transform.position;
            float dist = Vector2.Distance(center, playerTransform.position);
            playerInRange = (dist <= 2.2f);
        }

        if (!playerInRange && isOpen)
        {
            CloseChest();
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (KitchenUI.Instance != null && KitchenUI.Instance.IsOpen) return;
            if (StatueUI.Instance != null && StatueUI.Instance.IsOpen) return;

            if (KitchenStation.Instance != null && KitchenStation.Instance.playerInRange)
            {
                float distToChest = Vector2.Distance(transform.position, playerTransform.position);
                float distToKitchen = Vector2.Distance(KitchenStation.Instance.transform.position, playerTransform.position);
                if (distToKitchen < distToChest) return;
            }

            Interact();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChest();
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

    public void Interact()
    {
        if (isOpen)
        {
            CloseChest();
        }
        else
        {
            OpenChest();
        }
    }

    public void OpenChest()
    {
        if (KitchenUI.Instance != null && KitchenUI.Instance.IsOpen)
        {
            KitchenUI.Instance.Close();
        }
        if (StatueUI.Instance != null && StatueUI.Instance.IsOpen)
        {
            StatueUI.Instance.Close();
        }

        if (backpackContainer == null)
        {
            InventoryPanel invPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            if (invPanel != null)
            {
                backpackContainer = invPanel.ItemContainer;
            }
        }

        Debug.Log("[HomeChest] Opened Home Chest. Stored items are 100% safe!");
        OnChestToggled?.Invoke(true);

        if (ChestUI.Instance != null && chestContainer != null && backpackContainer != null)
        {
            ChestUI.Instance.Open(chestContainer, backpackContainer);
        }
        else if (ChestUI.Instance == null)
        {
            Debug.LogWarning("[HomeChest] ChestUI not found in scene!");
        }
        else
        {
            if (chestContainer == null) Debug.LogWarning("[HomeChest] chestContainer is not assigned!");
            if (backpackContainer == null) Debug.LogWarning("[HomeChest] backpackContainer is not assigned!");
        }
    }

    public void CloseChest()
    {
        Debug.Log("[HomeChest] Closed Home Chest.");
        OnChestToggled?.Invoke(false);

        if (ChestUI.Instance != null && ChestUI.Instance.IsOpen)
        {
            ChestUI.Instance.Close();
        }
    }

    public bool StoreItem(ItemData item, int amount)
    {
        if (chestContainer == null || backpackContainer == null) return false;
        if (item == null || amount <= 0) return false;

        if (!backpackContainer.HasItem(item)) return false;

        if (!chestContainer.CanAddItem(item))
        {
            Debug.LogWarning("[HomeChest] Chest is full, cannot store more items!");
            return false;
        }

        chestContainer.AddItem(item, amount);
        backpackContainer.RemoveItem(item, amount);

        Debug.Log($"[HomeChest] Stored {amount}x {item.itemName} into Chest.");
        OnChestContentsChanged?.Invoke();
        return true;
    }

    public bool RetrieveItem(ItemData item, int amount)
    {
        if (chestContainer == null || backpackContainer == null) return false;
        if (item == null || amount <= 0) return false;

        if (!chestContainer.HasItem(item)) return false;

        if (!backpackContainer.CanAddItem(item))
        {
            Debug.LogWarning("[HomeChest] Backpack is full, cannot retrieve more items!");
            return false;
        }

        backpackContainer.AddItem(item, amount);
        chestContainer.RemoveItem(item, amount);

        Debug.Log($"[HomeChest] Retrieved {amount}x {item.itemName} from Chest.");
        OnChestContentsChanged?.Invoke();
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("[HomeChest] Near Home Chest. Press E to open.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (isOpen)
            {
                CloseChest();
            }
        }
    }
}
