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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private Transform playerTransform;
    private Collider2D col;

    private void Start()
    {
        col = GetComponent<Collider2D>();
        FindPlayer();
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

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
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
        isOpen = true;
        Debug.Log("Đã mở Rương ở nhà. Các vật phẩm cất trong rương được bảo vệ an toàn 100%!");
        OnChestToggled?.Invoke(true);
    }

    public void CloseChest()
    {
        isOpen = false;
        Debug.Log("Đã đóng Rương ở nhà.");
        OnChestToggled?.Invoke(false);
    }

    // Chuyển vật phẩm từ Ba lô vào Rương
    public bool StoreItem(ItemData item, int amount)
    {
        if (chestContainer == null || backpackContainer == null) return false;
        if (item == null || amount <= 0) return false;

        if (!backpackContainer.HasItem(item)) return false;

        // Thêm vào rương và trừ trong ba lô
        chestContainer.AddItem(item, amount);
        backpackContainer.RemoveItem(item, amount);

        Debug.Log($"Đã cất {amount}x {item.itemName} vào Rương.");
        OnChestContentsChanged?.Invoke();
        return true;
    }

    // Lấy vật phẩm từ Rương ra Ba lô
    public bool RetrieveItem(ItemData item, int amount)
    {
        if (chestContainer == null || backpackContainer == null) return false;
        if (item == null || amount <= 0) return false;

        if (!chestContainer.HasItem(item)) return false;

        backpackContainer.AddItem(item, amount);
        chestContainer.RemoveItem(item, amount);

        Debug.Log($"Đã lấy {amount}x {item.itemName} từ Rương ra Ba lô.");
        OnChestContentsChanged?.Invoke();
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Đến gần Rương nhà (Home Chest). Nhấn E để cất/lấy đồ.");
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
