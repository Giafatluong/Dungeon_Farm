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

    private bool cachedIsDungeonScene = false;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

        UpdateSceneCache();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateSceneCache();
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

    private bool IsAnyUIOpen()
    {
        if (KitchenUI.Instance != null && KitchenUI.Instance.IsOpen) return true;
        if (ChestUI.Instance != null && ChestUI.Instance.IsOpen) return true;
        if (MerchantUI.Instance != null && MerchantUI.Instance.IsOpen) return true;
        if (StatueUI.Instance != null && StatueUI.Instance.IsOpen) return true;
        if (RewardUI.Instance != null && RewardUI.Instance.IsOpen) return true;
        return false;
    }

    private void Update()
    {
        if (IsInCombat() || IsAnyUIOpen())
        {
            movement = Vector2.zero;
            if (anim != null)
            {
                anim.SetFloat("Speed", 0f);
            }
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        movement = movement.normalized;

        if (anim != null)
        {
            anim.SetFloat("Horizontal", movement.x);
            anim.SetFloat("Vertical", movement.y);
            anim.SetFloat("Speed", movement.sqrMagnitude);

            if (movement != Vector2.zero)
            {
                anim.SetFloat("LastHorizontal", movement.x);
                anim.SetFloat("LastVertical", movement.y);
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

    private void FixedUpdate()
    {
        if (IsInCombat() || IsAnyUIOpen())
        {
            return;
        }

        if (rb != null)
        {
            rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);
        }
    }
}