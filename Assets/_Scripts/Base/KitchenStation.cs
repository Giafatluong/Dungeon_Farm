using UnityEngine;
using TMPro;

/// <summary>
/// Dedicated Kitchen Cooking Station placed at Base.
/// Allows player to approach, view prompt, and press E or Click to open KitchenUI.
/// </summary>
public class KitchenStation : MonoBehaviour, IInteractable
{
    public static KitchenStation Instance { get; private set; }

    [Header("Interaction Settings")]
    public float interactDistance = 1.6f;
    public bool playerInRange = false;
    public bool isOpen
    {
        get => KitchenUI.Instance != null && KitchenUI.Instance.IsOpen;
        set { }
    }

    [Header("Containers")]
    public ItemContainer backpackContainer;
    public ItemContainer chestContainer;

    private Transform playerTransform;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private GameObject promptObject;
    private TextMeshPro promptText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SetupVisuals();
        SetupCollider();
        SetupPrompt();
        FindPlayer();
        FindContainers();
    }

    private void SetupVisuals()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Clean up any old prep table props if this script is attached to a campfire
        Transform prep = transform.Find("PrepTable");
        if (prep != null)
        {
            Destroy(prep.gameObject);
        }

        if (spriteRenderer.sprite == null)
        {
            Sprite campfireSprite = FindSprite("Campfire_Anim_1")
                                 ?? FindSprite("Campfire_Anim_0")
                                 ?? FindSprite("Furnace_Anim_0");

            if (campfireSprite != null)
            {
                spriteRenderer.sprite = campfireSprite;
            }
        }
    }

    private void SetupCollider()
    {
        col = GetComponent<Collider2D>();
        if (col == null)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.2f, 1.2f);
            box.offset = Vector2.zero;
            box.isTrigger = true;
            col = box;
        }
    }

    private void SetupPrompt()
    {
        if (promptObject != null) return;

        promptObject = new GameObject("KitchenPrompt");
        promptObject.transform.SetParent(transform, false);
        promptObject.transform.localPosition = new Vector3(0, 1.15f, 0);

        promptText = promptObject.AddComponent<TextMeshPro>();
        promptText.text = "<color=#FFD700>[E]</color> Nau An (Cooking)";
        promptText.fontSize = 3.6f;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontStyle = FontStyles.Bold;
        promptText.color = Color.white;

        TMP_FontAsset font = DungeonUIAssetHelper.GetFontAsset();
        if (font != null)
        {
            promptText.font = font;
        }

        promptObject.SetActive(false);
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void FindContainers()
    {
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
            HomeChest homeChest = HomeChest.Instance ?? FindFirstObjectByType<HomeChest>(FindObjectsInactive.Include);
            if (homeChest != null)
            {
                chestContainer = homeChest.chestContainer;
            }
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
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            bool wasInRange = playerInRange;
            playerInRange = (dist <= interactDistance);

            if (promptObject != null)
            {
                promptObject.SetActive(playerInRange && !isOpen);
                if (playerInRange)
                {
                    float bob = Mathf.Sin(Time.time * 4f) * 0.06f;
                    promptObject.transform.localPosition = new Vector3(0, 1.35f + bob, 0);
                }
            }

            if (wasInRange && !playerInRange && isOpen)
            {
                CloseKitchen();
            }
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (ChestUI.Instance != null && ChestUI.Instance.IsOpen) return;
            if (StatueUI.Instance != null && StatueUI.Instance.IsOpen) return;

            if (HomeChest.Instance != null && HomeChest.Instance.playerInRange)
            {
                float distToKitchen = Vector2.Distance(transform.position, playerTransform.position);
                float distToChest = Vector2.Distance(HomeChest.Instance.transform.position, playerTransform.position);
                if (distToChest < distToKitchen) return;
            }

            Interact();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseKitchen();
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
            CloseKitchen();
        }
        else
        {
            OpenKitchen();
        }
    }

    public void OpenKitchen()
    {
        // Close chest or statue if open
        if (ChestUI.Instance != null && ChestUI.Instance.IsOpen)
        {
            if (HomeChest.Instance != null) HomeChest.Instance.CloseChest();
            else ChestUI.Instance.Close();
        }
        if (StatueUI.Instance != null && StatueUI.Instance.IsOpen)
        {
            StatueUI.Instance.Close();
        }

        FindContainers();

        if (promptObject != null)
        {
            promptObject.SetActive(false);
        }

        KitchenUI.Instance.Open(this, backpackContainer, chestContainer);
        Debug.Log("[KitchenStation] Opened Kitchen UI at Base.");
    }

    public void CloseKitchen()
    {
        if (KitchenUI.Instance != null && KitchenUI.Instance.IsOpen)
        {
            KitchenUI.Instance.Close();
        }
    }

    public void OnUIClosed()
    {
        // UI closed callback
    }

    private Sprite FindSprite(string spriteName)
    {
        Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].name.Equals(spriteName, System.StringComparison.OrdinalIgnoreCase))
            {
                return sprites[i];
            }
        }
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && sprites[i].name.IndexOf(spriteName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return sprites[i];
            }
        }
        return null;
    }
}
