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

        LoadChestData();
        UpdateVisualState(false);
        ChestUI.EnsureInstance();
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
            UIEscapeManager.ConsumeEscape();
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

        LoadChestData();
        _isOpen = true;
        UpdateVisualState(true);

        Debug.Log("[HomeChest] Opened Home Chest. Stored items are 100% safe!");
        OnChestToggled?.Invoke(true);

        ChestUI ui = ChestUI.EnsureInstance();
        if (ui != null && chestContainer != null && backpackContainer != null)
        {
            ui.Open(chestContainer, backpackContainer);
        }
        else if (ui == null)
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
        SaveChestData();
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

        int available = backpackContainer.GetItemCount(item);
        if (available <= 0) return false;
        int toStore = Mathf.Min(amount, available);

        if (!chestContainer.CanAddItem(item))
        {
            Debug.LogWarning("[HomeChest] Chest is full, cannot store more items!");
            return false;
        }

        chestContainer.AddItem(item, toStore);
        backpackContainer.RemoveItem(item, toStore);
        SaveChestData();

        Debug.Log($"[HomeChest] Stored {toStore}x {item.itemName} into Chest.");
        OnChestContentsChanged?.Invoke();
        return true;
    }

    public bool RetrieveItem(ItemData item, int amount)
    {
        if (chestContainer == null || backpackContainer == null) return false;
        if (item == null || amount <= 0) return false;

        int available = chestContainer.GetItemCount(item);
        if (available <= 0) return false;
        int toRetrieve = Mathf.Min(amount, available);

        if (!backpackContainer.CanAddItem(item))
        {
            Debug.LogWarning("[HomeChest] Backpack is full, cannot retrieve more items!");
            return false;
        }

        backpackContainer.AddItem(item, toRetrieve);
        chestContainer.RemoveItem(item, toRetrieve);
        SaveChestData();

        Debug.Log($"[HomeChest] Retrieved {toRetrieve}x {item.itemName} from Chest.");
        OnChestContentsChanged?.Invoke();
        return true;
    }

    #region Chest Persistence
    public void SaveChestData()
    {
        if (chestContainer == null) return;
        SaveContainer(chestContainer);
    }

    public static void SaveContainer(ItemContainer container)
    {
        if (container == null || container.itemSlots == null) return;
        int count = container.itemSlots.Length;
        PlayerPrefs.SetInt("HomeChest_SlotCount", count);

        for (int i = 0; i < count; i++)
        {
            var slot = container.itemSlots[i];
            if (slot != null && slot.itemData != null && slot.amount > 0)
            {
                PlayerPrefs.SetString($"HomeChest_Slot_{i}_Item", slot.itemData.name);
                PlayerPrefs.SetInt($"HomeChest_Slot_{i}_Amount", slot.amount);
            }
            else
            {
                PlayerPrefs.DeleteKey($"HomeChest_Slot_{i}_Item");
                PlayerPrefs.DeleteKey($"HomeChest_Slot_{i}_Amount");
            }
        }
        PlayerPrefs.Save();
        Debug.Log($"[HomeChest] Saved chest contents ({count} slots).");
    }

    public void LoadChestData()
    {
        if (chestContainer == null) return;
        LoadContainer(chestContainer);
    }

    public static void LoadContainer(ItemContainer container)
    {
        if (container == null || container.itemSlots == null) return;
        if (!PlayerPrefs.HasKey("HomeChest_SlotCount"))
        {
            container.Clear();
            return;
        }

        int count = Mathf.Min(container.itemSlots.Length, PlayerPrefs.GetInt("HomeChest_SlotCount", container.itemSlots.Length));

        ItemData[] allItems = GameAssetHelper.LoadAll<ItemData>();
        var itemMap = new System.Collections.Generic.Dictionary<string, ItemData>(System.StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < allItems.Length; i++)
        {
            if (allItems[i] != null)
            {
                if (!itemMap.ContainsKey(allItems[i].name)) itemMap[allItems[i].name] = allItems[i];
                if (!string.IsNullOrEmpty(allItems[i].itemName) && !itemMap.ContainsKey(allItems[i].itemName))
                {
                    itemMap[allItems[i].itemName] = allItems[i];
                }
            }
        }

        for (int i = 0; i < container.itemSlots.Length; i++)
        {
            if (container.itemSlots[i] == null) container.itemSlots[i] = new ItemSlot();

            if (i < count && PlayerPrefs.HasKey($"HomeChest_Slot_{i}_Item"))
            {
                string itemName = PlayerPrefs.GetString($"HomeChest_Slot_{i}_Item", "");
                int amount = PlayerPrefs.GetInt($"HomeChest_Slot_{i}_Amount", 0);
                if (!string.IsNullOrEmpty(itemName) && amount > 0 && itemMap.TryGetValue(itemName, out ItemData data))
                {
                    container.itemSlots[i].itemData = data;
                    container.itemSlots[i].amount = amount;
                    continue;
                }
            }

            container.itemSlots[i].itemData = null;
            container.itemSlots[i].amount = 0;
        }

        container.NotifyChange();
        Debug.Log($"[HomeChest] Loaded chest contents ({container.itemSlots.Length} slots).");
    }
    #endregion

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
