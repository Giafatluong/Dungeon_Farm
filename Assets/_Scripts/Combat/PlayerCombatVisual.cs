using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý hình ảnh, animation và hiệu ứng chiến đấu (lunge, hurt flash, defend, heal) của Player trong combat.
/// Luôn đảm bảo Player hiển thị rõ ràng, hướng mặt sang phải về phía kẻ địch.
/// </summary>
public class PlayerCombatVisual : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerStats playerStats;

    [Header("Visual Settings")]
    [SerializeField] private float lungeDistance = 1.0f;
    [SerializeField] private float lungeDuration = 0.15f;

    private Vector3 originalPosition;
    private Color originalColor = Color.white;
    private Coroutine currentVisualRoutine;
    private bool isMovingTransition = false;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();

        originalPosition = transform.position;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            // Đảm bảo sorting layer và order luôn nổi trên nền dungeon
            spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 10);

            // Dự phòng: nếu sprite bị null, tự động tìm sprite Player trong bộ tài nguyên
            if (spriteRenderer.sprite == null)
            {
                Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (Sprite s in allSprites)
                {
                    if (s != null && (s.name == "Player_0" || s.name.StartsWith("Player_") || s.name.Contains("PlayerIdle")))
                    {
                        spriteRenderer.sprite = s;
                        break;
                    }
                }
            }
        }

        FaceRight();
    }

    private void Start()
    {
        FaceRight();

        if (playerStats != null)
        {
            playerStats.OnPlayerDamaged += OnDamaged;
            playerStats.OnPlayerDefended += OnDefended;
            playerStats.OnPlayerHealed += OnHealed;
            playerStats.OnPlayerDeath += OnDeath;
        }

        CombatManager combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager != null)
        {
            combatManager.OnPlayerAttackAction += OnAttack;
        }
    }

    private void Update()
    {
        if (isMovingTransition)
            return;

        // Giữ cho Animator luôn phát BlendTree Idle quay sang phải
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetFloat("LastHorizontal", 1f);
            animator.SetFloat("LastVertical", 0f);
            animator.SetFloat("Horizontal", 0f);
            animator.SetFloat("Vertical", 0f);
            animator.SetFloat("Speed", 0f);
        }
    }

    /// <summary>
    /// Kích hoạt animation bước đi/chạy và di chuyển Player tiến về phía trước trong lúc chuyển Wave
    /// Tạo cảm giác người chơi đang di chuyển tiến sâu vào hầm ngục.
    /// </summary>
    public void PlayMoveTransition(float duration, System.Action onComplete = null)
    {
        if (currentVisualRoutine != null)
        {
            StopCoroutine(currentVisualRoutine);
        }
        currentVisualRoutine = StartCoroutine(MoveTransitionRoutine(duration, onComplete));
    }

    private IEnumerator MoveTransitionRoutine(float duration, System.Action onComplete)
    {
        isMovingTransition = true;
        FaceRight();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.SetFloat("LastHorizontal", 1f);
                animator.SetFloat("LastVertical", 0f);
                animator.SetFloat("Horizontal", 1f);
                animator.SetFloat("Vertical", 0f);
                animator.SetFloat("Speed", 1f);
            }

            // Player giữ nguyên vị trí ban đầu (chạy tại chỗ kiểu Capybara Go)
            transform.position = originalPosition;
            yield return null;
        }

        transform.position = originalPosition;
        isMovingTransition = false;

        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetFloat("LastHorizontal", 1f);
            animator.SetFloat("LastVertical", 0f);
            animator.SetFloat("Horizontal", 0f);
            animator.SetFloat("Vertical", 0f);
            animator.SetFloat("Speed", 0f);
        }

        currentVisualRoutine = null;
        onComplete?.Invoke();
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnPlayerDamaged -= OnDamaged;
            playerStats.OnPlayerDefended -= OnDefended;
            playerStats.OnPlayerHealed -= OnHealed;
            playerStats.OnPlayerDeath -= OnDeath;
        }

        CombatManager combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager != null)
        {
            combatManager.OnPlayerAttackAction -= OnAttack;
        }
    }

    /// <summary>
    /// Đảm bảo nhân vật luôn quay mặt sang phải hướng về phía quái vật
    /// </summary>
    public void FaceRight()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
        }

        if (animator != null)
        {
            animator.SetFloat("LastHorizontal", 1f);
            animator.SetFloat("LastVertical", 0f);
            animator.SetFloat("Horizontal", 0f);
            animator.SetFloat("Vertical", 0f);
            animator.SetFloat("Speed", 0f);
        }
    }

    private void OnAttack(EnemyStats target, int damage)
    {
        if (currentVisualRoutine != null) StopCoroutine(currentVisualRoutine);
        currentVisualRoutine = StartCoroutine(AttackLungeRoutine());
    }

    private void OnDamaged(int damage)
    {
        if (currentVisualRoutine != null) StopCoroutine(currentVisualRoutine);
        currentVisualRoutine = StartCoroutine(HurtFlashRoutine());
    }

    private void OnDefended()
    {
        if (currentVisualRoutine != null) StopCoroutine(currentVisualRoutine);
        currentVisualRoutine = StartCoroutine(DefendPulseRoutine());
    }

    private void OnHealed(int amount)
    {
        if (currentVisualRoutine != null) StopCoroutine(currentVisualRoutine);
        currentVisualRoutine = StartCoroutine(HealPulseRoutine());
    }

    private void OnDeath()
    {
        if (currentVisualRoutine != null) StopCoroutine(currentVisualRoutine);
        currentVisualRoutine = StartCoroutine(DeathRoutine());
    }

    private IEnumerator AttackLungeRoutine()
    {
        Vector3 startPos = originalPosition;
        Vector3 targetPos = startPos + new Vector3(lungeDistance, 0f, 0f);
        float halfDuration = lungeDuration * 0.5f;

        // Lao tới trước về phía kẻ địch
        float t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, t / halfDuration);
            yield return null;
        }

        // Lùi về vị trí ban đầu
        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(targetPos, startPos, t / halfDuration);
            yield return null;
        }

        transform.position = startPos;
    }

    private IEnumerator HurtFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.35f, 0.35f, 1f);
        }

        // Rung nhẹ khi trúng đòn
        Vector3 startPos = originalPosition;
        for (int i = 0; i < 4; i++)
        {
            transform.position = startPos + new Vector3(Random.Range(-0.08f, 0.08f), Random.Range(-0.05f, 0.05f), 0f);
            yield return new WaitForSeconds(0.035f);
        }

        transform.position = startPos;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private IEnumerator DefendPulseRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.45f, 0.85f, 1f, 1f);
            yield return new WaitForSeconds(0.25f);
            spriteRenderer.color = originalColor;
        }
    }

    private IEnumerator HealPulseRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.45f, 1f, 0.6f, 1f);
            yield return new WaitForSeconds(0.25f);
            spriteRenderer.color = originalColor;
        }
    }

    private IEnumerator DeathRoutine()
    {
        if (animator != null) animator.enabled = false;

        float elapsed = 0f;
        float duration = 0.8f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
            {
                float a = Mathf.Lerp(1f, 0.2f, elapsed / duration);
                spriteRenderer.color = new Color(0.5f, 0.2f, 0.2f, a);
            }
            yield return null;
        }
    }
}
