using System.Collections;
using UnityEngine;

/// <summary>
/// Manages combat visual animations and motions for Enemies (lunges, strikes, attack anims, defend pulses, and hurt flashes).
/// Ensures that attack animations completely finish before combat flow advances to the next turn.
/// </summary>
public class EnemyCombatVisual : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyStats enemyStats;

    [Header("Motion Settings")]
    [SerializeField] private float lungeDistance = 1.1f;
    [SerializeField] private float lungeDuration = 0.45f;
    [SerializeField] private float anticipationDuration = 0.15f;
    [SerializeField] private float recoveryDuration = 0.25f;

    private Vector3 originalPosition;
    private Vector3 originalScale = Vector3.one;
    private Color originalColor = Color.white;
    private Coroutine activeVisualRoutine;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        if (enemyStats == null) enemyStats = GetComponent<EnemyStats>();

        originalPosition = transform.position;
        originalScale = transform.localScale;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 10);
        }
    }

    private void Start()
    {
        // Re-record original position in case WaveManager repositioned the enemy on spawn
        originalPosition = transform.position;
        originalScale = transform.localScale;
    }

    /// <summary>
    /// Updates the baseline position when the enemy is repositioned by wave formation.
    /// </summary>
    public void SetOriginalPosition(Vector3 newPos)
    {
        originalPosition = newPos;
        transform.position = newPos;
    }

    #region Attack Animation & Motion
    /// <summary>
    /// Plays full attack animation and motion (wind-up, lunge toward player, strike with impact callback, and return).
    /// Calling coroutine must yield return on this routine to ensure the turn does not advance until it finishes.
    /// </summary>
    public IEnumerator PlayAttackRoutine(Transform playerTarget, System.Action onImpact)
    {
        if (activeVisualRoutine != null)
        {
            StopCoroutine(activeVisualRoutine);
        }

        // Face player (leftwards)
        FaceDirection(Vector3.left);

        // 1. Wind-up / Anticipation (pulls back slightly to the right with slight squash)
        float elapsed = 0f;
        Vector3 windUpPos = originalPosition + Vector3.right * 0.18f;
        Vector3 windUpScale = new Vector3(originalScale.x * 0.9f, originalScale.y * 1.15f, originalScale.z);

        while (elapsed < anticipationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / anticipationDuration);
            transform.position = Vector3.Lerp(originalPosition, windUpPos, t);
            transform.localScale = Vector3.Lerp(originalScale, windUpScale, t);
            yield return null;
        }

        // 2. Trigger Animator attack state if available
        TryPlayAnimatorState("SkeletonBow_Attack", "Attack");

        // 3. Powerful Lunge forward toward player (to the left)
        Vector3 targetPos = playerTarget != null
            ? originalPosition + (playerTarget.position - originalPosition).normalized * lungeDistance
            : originalPosition + Vector3.left * lungeDistance;

        elapsed = 0f;
        float strikeTime = lungeDuration * 0.45f;
        Vector3 stretchScale = new Vector3(originalScale.x * 1.15f, originalScale.y * 0.9f, originalScale.z);

        while (elapsed < strikeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / strikeTime);
            // Ease-in fast strike
            float smoothT = t * t;
            transform.position = Vector3.Lerp(windUpPos, targetPos, smoothT);
            transform.localScale = Vector3.Lerp(windUpScale, stretchScale, smoothT);
            yield return null;
        }

        transform.position = targetPos;

        // 4. Impact moment! Trigger damage, player flinch, and weapon flash
        onImpact?.Invoke();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 1f, 0.6f, 1f); // Bright strike flash
        }

        // Hold at impact point briefly for impact weight
        yield return new WaitForSeconds(0.08f);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // 5. Recovery: Return smoothly back to original combat station
        elapsed = 0f;
        while (elapsed < recoveryDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / recoveryDuration);
            float smoothT = 1f - Mathf.Pow(1f - t, 2f); // Smooth ease-out
            transform.position = Vector3.Lerp(targetPos, originalPosition, smoothT);
            transform.localScale = Vector3.Lerp(stretchScale, originalScale, smoothT);
            yield return null;
        }

        // 6. Reset to baseline
        transform.position = originalPosition;
        transform.localScale = originalScale;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        // Return animator to idle
        TryPlayAnimatorState("SkeletonBow_Idle", "Skeleton-Idle", "Idle");
        activeVisualRoutine = null;
    }
    #endregion

    #region Defend, Buff & Debuff Routines
    /// <summary>
    /// Plays a defensive stance pulse (flashes blue, expands defensive scale, shakes).
    /// </summary>
    public IEnumerator PlayDefendRoutine(System.Action onApply)
    {
        if (activeVisualRoutine != null) StopCoroutine(activeVisualRoutine);

        float duration = 0.5f;
        float elapsed = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.4f, 0.75f, 1f, 1f);
        }

        onApply?.Invoke();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.Sin((elapsed / duration) * Mathf.PI);
            transform.localScale = originalScale * (1f + pulse * 0.12f);
            yield return null;
        }

        transform.localScale = originalScale;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        activeVisualRoutine = null;
    }

    /// <summary>
    /// Plays an energizing buff pulse (golden flash, hops upward, expands).
    /// </summary>
    public IEnumerator PlayBuffRoutine(System.Action onApply)
    {
        if (activeVisualRoutine != null) StopCoroutine(activeVisualRoutine);

        float duration = 0.55f;
        float elapsed = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.9f, 0.35f, 1f);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float hop = Mathf.Sin(t * Mathf.PI) * 0.25f;
            transform.position = originalPosition + new Vector3(0f, hop, 0f);
            yield return null;
        }

        onApply?.Invoke();

        transform.position = originalPosition;
        transform.localScale = originalScale;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        activeVisualRoutine = null;
    }

    /// <summary>
    /// Plays a sinister debuff casting pulse (dark magenta aura, tremors toward player).
    /// </summary>
    public IEnumerator PlayDebuffRoutine(Transform playerTarget, System.Action onApply)
    {
        if (activeVisualRoutine != null) StopCoroutine(activeVisualRoutine);

        float duration = 0.55f;
        float elapsed = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.85f, 0.35f, 0.95f, 1f);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float shakeX = Random.Range(-0.06f, 0.06f);
            float shakeY = Random.Range(-0.04f, 0.04f);
            transform.position = originalPosition + new Vector3(shakeX, shakeY, 0f);
            yield return null;
        }

        onApply?.Invoke();

        transform.position = originalPosition;
        transform.localScale = originalScale;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        activeVisualRoutine = null;
    }
    #endregion

    #region Hurt & Death Feedback
    public void PlayHurtFlash()
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(HurtFlashRoutine());
        }
    }

    private IEnumerator HurtFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.35f, 0.35f, 1f);
        }

        Vector3 startPos = originalPosition;
        for (int i = 0; i < 3; i++)
        {
            transform.position = startPos + new Vector3(Random.Range(-0.06f, 0.06f), Random.Range(-0.04f, 0.04f), 0f);
            yield return new WaitForSeconds(0.035f);
        }

        transform.position = startPos;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    public void PlayDeathEffect()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        TryPlayAnimatorState("SkeletonBow_dead", "Skeleton_dead", "Dead");
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(FadeOutRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        float elapsed = 0f;
        float duration = 0.45f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (spriteRenderer != null)
            {
                float a = Mathf.Lerp(1f, 0f, elapsed / duration);
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, a);
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    public void PlayHitAnimation() => PlayHurtFlash();

    public void PlayDeathAnimation() => PlayDeathEffect();

    public void PlayAttackAnimation()
    {
        if (gameObject.activeInHierarchy)
        {
            PlayerStats player = FindFirstObjectByType<PlayerStats>();
            StartCoroutine(PlayAttackRoutine(player != null ? player.transform : null, null));
        }
    }
    #endregion

    #region Helpers
    private void FaceDirection(Vector3 dir)
    {
        if (spriteRenderer != null && dir.x != 0f)
        {
            spriteRenderer.flipX = dir.x > 0f;
        }
    }

    private bool TryPlayAnimatorState(params string[] stateNames)
    {
        if (animator == null || !animator.isActiveAndEnabled) return false;

        for (int i = 0; i < stateNames.Length; i++)
        {
            string stateName = stateNames[i];
            if (animator.HasState(0, Animator.StringToHash(stateName)))
            {
                animator.Play(stateName);
                return true;
            }
        }

        return false;
    }
    #endregion
}
