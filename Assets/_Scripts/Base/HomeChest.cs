using UnityEngine;

public class HomeChest : MonoBehaviour, IInteractable
{
    public static HomeChest Instance { get; private set; }

    [Header("Containers")]
    public ItemContainer chestContainer;
    public ItemContainer backpackContainer;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 1.4f;
    public bool playerInRange;
    private bool _isOpen = false;
    public bool isOpen
    {
        get => _isOpen || (ChestUI.Instance != null && ChestUI.Instance.IsOpen);
        private set => _isOpen = value;
    }

    [Header("Visual & Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

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

        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
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

        if (chestContainer == null)
        {
            ItemContainer[] containers = GameAssetHelper.LoadAll<ItemContainer>();
            for (int i = 0; i < containers.Length; i++)
            {
                if (containers[i] != null && containers[i].name.ToLower().Contains("homechest"))
                {
                    chestContainer = containers[i];
                    break;
                }
            }
        }

        UpdateVisualState(false);
    }

    private void FindPlayer()
    {
        if (PlayerStats.Instance != null)
        {
            playerTransform = PlayerStats.Instance.transform;
            return;
        }
        if (PlayerMovement.Instance != null)
        {
            playerTransform = PlayerMovement.Instance.transform;
            return;
        }
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
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
            Vector3 center = col != null ? col.bounds.center : transform.position;
            float sqrDist = (center - playerTransform.position).sqrMagnitude;
            playerInRange = (sqrDist <= interactDistance * interactDistance);
        }

        if (!playerInRange && isOpen)
        {
            CloseChest();
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (DayTransitionUI.Instance != null && DayTransitionUI.Instance.IsOpen) return;
            if (KitchenUI.Instance != null && KitchenUI.Instance.IsOpen) return;
            if (StatueUI.Instance != null && StatueUI.Instance.IsOpen) return;

            if (KitchenStation.Instance != null && KitchenStation.Instance.playerInRange)
            {
                float distToChest = ((Vector2)(transform.position - playerTransform.position)).sqrMagnitude;
                float distToKitchen = ((Vector2)(KitchenStation.Instance.transform.position - playerTransform.position)).sqrMagnitude;
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

        _isOpen = true;
        UpdateVisualState(true);

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
        _isOpen = false;
        UpdateVisualState(false);

        Debug.Log("[HomeChest] Closed Home Chest.");
        OnChestToggled?.Invoke(false);

        if (ChestUI.Instance != null && ChestUI.Instance.IsOpen)
        {
            ChestUI.Instance.Close();
        }
    }

    private void UpdateVisualState(bool open)
    {
        if (animator != null)
        {
            animator.SetBool("IsInteracted", open);
            animator.SetBool("IsOpen", open);
            animator.Play(open ? "Chest" : "Closed", 0, 0f);
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
