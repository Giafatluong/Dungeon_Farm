using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    [SerializeField] private CombatManager combatManager;

    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int LastHorizontalHash = Animator.StringToHash("LastHorizontal");
    private static readonly int LastVerticalHash = Animator.StringToHash("LastVertical");

    private bool cachedIsDungeonScene = false;
    private InventoryPanel inventoryPanel;

    public static PlayerMovement Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        Instance = this;
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null)
        {
            anim = GetComponent<Animator>();
            if (anim == null) anim = GetComponentInChildren<Animator>();
        }
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        if (spriteRenderer != null) spriteRenderer.sortingOrder = 3;

        UpdateSceneCache();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateSceneCache();
        inventoryPanel = null;
    }

    private void UpdateSceneCache()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        cachedIsDungeonScene = activeScene.Equals("Dungeon", System.StringComparison.OrdinalIgnoreCase);
    }

    public bool IsInCombat()
    {
        CombatManager cm = combatManager != null ? combatManager : CombatManager.Instance;
        if (cm != null && (cm.isCombatActive || cm.isPreparingCombat))
        {
            return true;
        }

        return cachedIsDungeonScene;
    }

    private float nextInvSearchTime = 0f;

    private bool IsAnyUIOpen()
    {
        if (inventoryPanel == null && Time.time >= nextInvSearchTime)
        {
            nextInvSearchTime = Time.time + 1.0f;
            inventoryPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
        }
        if (inventoryPanel != null && inventoryPanel.gameObject.activeSelf) return true;

        if (KitchenUI.Instance != null && KitchenUI.Instance.IsOpen) return true;
        if (ChestUI.Instance != null && ChestUI.Instance.IsOpen) return true;
        if (MerchantUI.Instance != null && MerchantUI.Instance.IsOpen) return true;
        if (StatueUI.Instance != null && StatueUI.Instance.IsOpen) return true;
        if (RewardUI.Instance != null && RewardUI.Instance.IsOpen) return true;
        if (LootUI.Instance != null && LootUI.Instance.IsOpen) return true;
        if (CampUI.Instance != null && CampUI.Instance.IsOpen) return true;
        if (DungeonEventUI.Instance != null && DungeonEventUI.Instance.IsOpen) return true;
        if (FloorVictoryUI.Instance != null && FloorVictoryUI.Instance.IsOpen) return true;
        if (DungeonEntranceUI.Instance != null && DungeonEntranceUI.Instance.IsOpen) return true;
        if (DayTransitionUI.Instance != null && DayTransitionUI.Instance.IsOpen) return true;
        return false;
    }

    private void Update()
    {
        if (IsInCombat() || IsAnyUIOpen())
        {
            movement = Vector2.zero;
            if (anim != null)
            {
                anim.SetFloat(HorizontalHash, 0f);
                anim.SetFloat(VerticalHash, 0f);
                anim.SetFloat(SpeedHash, 0f);
                anim.SetFloat(LastHorizontalHash, 1f);
                anim.SetFloat(LastVerticalHash, 0f);
            }
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        movement = movement.normalized;

        if (anim != null)
        {
            anim.SetFloat(HorizontalHash, movement.x);
            anim.SetFloat(VerticalHash, movement.y);
            anim.SetFloat(SpeedHash, movement.sqrMagnitude);

            if (movement != Vector2.zero)
            {
                anim.SetFloat(LastHorizontalHash, movement.x);
                anim.SetFloat(LastVerticalHash, movement.y);
            }
        }

        if (spriteRenderer != null)
        {
            if (movement.x > 0)
            {
                spriteRenderer.flipX = false;
            }
            else if (movement.x < 0)
            {
                spriteRenderer.flipX = true;
            }
        }
    }

    public float GetCurrentMoveSpeed()
    {
        float bonus = 0f;
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.permanentSpeed > 0)
        {
            bonus += ProgressionManager.Instance.permanentSpeed * 0.4f;
        }
        return speed + bonus;
    }

    private void FixedUpdate()
    {
        if (IsInCombat() || IsAnyUIOpen())
        {
            return;
        }

        if (rb != null)
        {
            rb.MovePosition(rb.position + movement * GetCurrentMoveSpeed() * Time.fixedDeltaTime);
        }
    }
}