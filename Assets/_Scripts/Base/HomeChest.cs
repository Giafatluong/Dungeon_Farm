using UnityEngine;

public class HomeChest : MonoBehaviour, IInteractable
{
    public static HomeChest Instance { get; private set; }

    [Header("Containers")]
    public ItemContainer chestContainer;
    public ItemContainer backpackContainer;

    [Header("Interaction")]
    public bool playerInRange;
    public bool isOpen;

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
            Interact();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChest();
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
        isOpen = true;
        Debug.Log("[HomeChest] Đã mở Rương ở nhà. Các vật phẩm cất trong rương được bảo vệ an toàn 100%!");
        OnChestToggled?.Invoke(true);

        if (chestContainer == null)
            Debug.LogWarning("[HomeChest] chestContainer chưa được gán ScriptableObject (HomeChest.asset)!");
        if (backpackContainer == null)
            Debug.LogWarning("[HomeChest] backpackContainer chưa được gán ScriptableObject (Inventory.asset)!");

        if (ChestUI.Instance != null && chestContainer != null && backpackContainer != null)
        {
            ChestUI.Instance.Open(chestContainer, backpackContainer);
        }
        else if (ChestUI.Instance == null)
        {
            Debug.LogWarning("[HomeChest] Không tìm thấy ChestUI trong scene!");
        }
    }

    public void CloseChest()
    {
        isOpen = false;
        Debug.Log("[HomeChest] Đã đóng Rương ở nhà.");
        OnChestToggled?.Invoke(false);

        // Đóng giao diện ChestUI
        if (ChestUI.Instance != null)
        {
            ChestUI.Instance.Close();
        }
    }

    // Chuyển vật phẩm từ Ba lô vào Rương
    public bool StoreItem(ItemData item, int amount)
    {
        if (chestContainer == null || backpackContainer == null) return false;
        if (item == null || amount <= 0) return false;

        if (!backpackContainer.HasItem(item)) return false;

        // Kiểm tra xem rương còn chỗ chứa không trước khi trừ trong ba lô
        if (!chestContainer.CanAddItem(item))
        {
            Debug.LogWarning("[HomeChest] Rương đã đầy, không thể cất thêm đồ!");
            return false;
        }

        // Thêm vào rương và trừ trong ba lô
        chestContainer.AddItem(item, amount);
        backpackContainer.RemoveItem(item, amount);

        Debug.Log($"[HomeChest] Đã cất {amount}x {item.itemName} vào Rương.");
        OnChestContentsChanged?.Invoke();
        return true;
    }

    // Lấy vật phẩm từ Rương ra Ba lô
    public bool RetrieveItem(ItemData item, int amount)
    {
        if (chestContainer == null || backpackContainer == null) return false;
        if (item == null || amount <= 0) return false;

        if (!chestContainer.HasItem(item)) return false;

        // Kiểm tra xem ba lô còn chỗ chứa không trước khi trừ trong rương
        if (!backpackContainer.CanAddItem(item))
        {
            Debug.LogWarning("[HomeChest] Ba lô đã đầy, không thể lấy thêm đồ!");
            return false;
        }

        backpackContainer.AddItem(item, amount);
        chestContainer.RemoveItem(item, amount);

        Debug.Log($"[HomeChest] Đã lấy {amount}x {item.itemName} từ Rương ra Ba lô.");
        OnChestContentsChanged?.Invoke();
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("[HomeChest] Đến gần Rương nhà (Home Chest). Nhấn E để cất/lấy đồ.");
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
