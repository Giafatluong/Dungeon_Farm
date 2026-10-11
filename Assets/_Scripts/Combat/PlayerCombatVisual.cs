using System.Collections;
using UnityEngine;

/// <summary>
/// Manages combat visual effects (lunge, hurt flash, defend, heal) and animations for Player.
/// Ensures Player faces right toward enemies.
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
    private Vector3 originalScale = Vector3.one;
    private Color originalColor = Color.white;
    private Coroutine currentVisualRoutine;
    private bool isMovingTransition = false;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();

        originalPosition = transform.position;
        originalScale = transform.localScale;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 10);
        }

        SetIdle();
    }

    private void Start()
    {
        SetIdle();

        if (playerStats != null)
        {
            playerStats.OnPlayerDamaged += OnDamaged;
            playerStats.OnPlayerDefended += OnDefended;
            playerStats.OnPlayerHealed += OnHealed;
            playerStats.OnPlayerDeath += OnDeath;
        }

        CombatManager combatManager = CombatManager.Instance ?? FindFirstObjectByType<CombatManager>();
        if (combatManager != null)
        {
            combatManager.OnPlayerAttackAction += OnAttack;
            combatManager.OnCombatStarted += SetIdle;
            combatManager.OnCombatEnded += SetIdle;
            combatManager.OnTurnChanged += HandleTurnChanged;
        }
    }

    private void Update()
    {
        // Trong suốt thời gian chiến đấu (ngoại trừ lúc chuyển tầng có move transition),
        // ép Player luôn luôn duy trì ở trạng thái Idle, không chạy tại chỗ
        if (!isMovingTransition)
        {
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.SetFloat("LastHorizontal", 1f);
                animator.SetFloat("LastVertical", 0f);
                animator.SetFloat("Horizontal", 0f);
                animator.SetFloat("Vertical", 0f);
                animator.SetFloat("Speed", 0f);

                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName("Idle"))
                {
                    animator.Play("Idle", 0);
                }
            }
        }
    }

    private void HandleTurnChanged(CombatManager.Turn turn)
    {
        SetIdle();
    }

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

        try
        {
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

                transform.position = originalPosition;
                yield return null;
            }
        }
        finally
        {
            isMovingTransition = false;
            transform.position = originalPosition;
            SetIdle();
            currentVisualRoutine = null;
            onComplete?.Invoke();
        }
    }

    public void FaceRight()
    {
        SetIdle();
    }

    public void SetIdle()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
        }

        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetFloat("LastHorizontal", 1f);
            animator.SetFloat("LastVertical", 0f);
            animator.SetFloat("Horizontal", 0f);
            animator.SetFloat("Vertical", 0f);
            animator.SetFloat("Speed", 0f);

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!stateInfo.IsName("Idle"))
            {
                animator.Play("Idle", 0, 0f);
            }
        }
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

        CombatManager combatManager = CombatManager.Instance ?? FindFirstObjectByType<CombatManager>();
        if (combatManager != null)
        {
            combatManager.OnPlayerAttackAction -= OnAttack;
            combatManager.OnCombatStarted -= SetIdle;
            combatManager.OnCombatEnded -= SetIdle;
            combatManager.OnTurnChanged -= HandleTurnChanged;
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

        Vector3 stretchScale = new Vector3(originalScale.x * 1.15f, originalScale.y * 0.88f, originalScale.z);
        Vector3 impactScale = new Vector3(originalScale.x * 0.90f, originalScale.y * 1.12f, originalScale.z);

        // Lunge forward with horizontal stretch
        float t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float factor = t / halfDuration;
            transform.position = Vector3.Lerp(startPos, targetPos, factor);
            transform.localScale = Vector3.Lerp(originalScale, stretchScale, factor);
            yield return null;
        }

        // Momentary impact punch
        transform.position = targetPos;
        transform.localScale = impactScale;

        // Return with elastic recovery
        t = 0f;
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float factor = t / halfDuration;
            transform.position = Vector3.Lerp(targetPos, startPos, factor);
            transform.localScale = Vector3.Lerp(impactScale, originalScale, factor);
            yield return null;
        }

        transform.position = startPos;
        transform.localScale = originalScale;
        SetIdle();
    }

    private IEnumerator HurtFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.35f, 0.35f, 1f);
        }

        Vector3 startPos = originalPosition;
        Vector3 recoilPos = startPos + Vector3.left * 0.14f; // Recoil backwards away from enemy
        Vector3 squashScale = new Vector3(originalScale.x * 1.12f, originalScale.y * 0.85f, originalScale.z);

        float p1 = 0.05f;
        float elapsed = 0f;
        while (elapsed < p1)
        {
            elapsed += Time.deltaTime;
            float factor = elapsed / p1;
            transform.position = Vector3.Lerp(startPos, recoilPos, factor);
            transform.localScale = Vector3.Lerp(originalScale, squashScale, factor);
            yield return null;
        }

        float p2 = 0.08f;
        elapsed = 0f;
        while (elapsed < p2)
        {
            elapsed += Time.deltaTime;
            float factor = elapsed / p2;
            transform.position = Vector3.Lerp(recoilPos, startPos, factor);
            transform.localScale = Vector3.Lerp(squashScale, originalScale, factor);
            yield return null;
        }

        transform.position = startPos;
        transform.localScale = originalScale;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        SetIdle();
    }

    private IEnumerator DefendPulseRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.45f, 0.85f, 1f, 1f);
            yield return new WaitForSeconds(0.25f);
            spriteRenderer.color = originalColor;
        }
        SetIdle();
    }

    private IEnumerator HealPulseRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.45f, 1f, 0.6f, 1f);
            yield return new WaitForSeconds(0.25f);
            spriteRenderer.color = originalColor;
        }
        SetIdle();
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
