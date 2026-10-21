using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private float lungeDuration = 0.44f;
    [SerializeField] private float anticipationDuration = 0.08f;
    [SerializeField] private float impactDuration = 0.24f;
    [SerializeField] private float recoveryDuration = 0.14f;

    [Header("Sprite Orientation")]
    [SerializeField] private bool defaultFacesRight = false;

    [Header("Ranged Combat Settings")]
    [SerializeField] private bool isRanged = false;
    [SerializeField] private Sprite projectileSprite;
    [SerializeField] private float projectileSpeed = 16f;

    public bool IsRanged
    {
        get => isRanged;
        set => isRanged = value;
    }

    public bool IsVisualActionActive { get; private set; }

    private static readonly int[] AttackStateHashes = {
        Animator.StringToHash("SkeletonBow_Attack"),
        Animator.StringToHash("Attack"),
        Animator.StringToHash("Attack1"),
        Animator.StringToHash("Attack_1")
    };
    private static readonly int[] IdleStateHashes = {
        Animator.StringToHash("SkeletonBow_Idle"),
        Animator.StringToHash("Skeleton-Idle"),
        Animator.StringToHash("Slime_Boss"),
        Animator.StringToHash("Melee"),
        Animator.StringToHash("Idle")
    };
    private static readonly int[] HitStateHashes = {
        Animator.StringToHash("Hit"),
        Animator.StringToHash("HIt"),
        Animator.StringToHash("Hurt"),
        Animator.StringToHash("Damage")
    };
    private static readonly int[] DeadStateHashes = {
        Animator.StringToHash("SkeletonBow_dead"),
        Animator.StringToHash("Skeleton_dead"),
        Animator.StringToHash("Dead"),
        Animator.StringToHash("Deah")
    };

    private static readonly WaitForSeconds WaitRelease = new(0.12f);
    private static readonly WaitForSeconds WaitFollowThrough = new(0.14f);
    private static readonly WaitForSeconds WaitDeadFade = new(0.28f);

    private static readonly Queue<GameObject> ProjectilePool = new();
    private static Transform ProjectilePoolRoot;
    private static Sprite _cachedProjectileSprite;

    private static GameObject GetPooledProjectile()
    {
        while (ProjectilePool.Count > 0)
        {
            GameObject p = ProjectilePool.Dequeue();
            if (p != null)
            {
                p.SetActive(true);
                return p;
            }
        }

        if (ProjectilePoolRoot == null)
        {
            GameObject rootGo = new GameObject("[ProjectilePool]");
            Object.DontDestroyOnLoad(rootGo);
            ProjectilePoolRoot = rootGo.transform;
        }

        GameObject proj = new GameObject("Ranged_Projectile");
        proj.transform.SetParent(ProjectilePoolRoot);
        proj.AddComponent<SpriteRenderer>();
        return proj;
    }

    private static void ReturnPooledProjectile(GameObject proj)
    {
        if (proj == null) return;
        proj.SetActive(false);
        if (ProjectilePoolRoot != null)
        {
            proj.transform.SetParent(ProjectilePoolRoot);
        }
        ProjectilePool.Enqueue(proj);
    }

    private Vector3 originalPosition;
    private Vector3 originalScale = Vector3.one;
    private Color originalColor = Color.white;
    private Coroutine activeVisualRoutine;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }
        if (enemyStats == null) enemyStats = GetComponent<EnemyStats>();

        originalPosition = transform.position;
        originalScale = transform.localScale;

        isRanged = CheckIsRanged();
        EnsureProjectileSpriteLoaded();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 10);
            defaultFacesRight = IsSpriteNaturallyFacingRight();
            FaceDirection(Vector3.left);
        }

        TryPlayIdle();
    }

    private void Start()
    {
        // Re-record original position in case WaveManager repositioned the enemy on spawn
        originalPosition = transform.position;
        originalScale = transform.localScale;
        isRanged = CheckIsRanged();
        EnsureProjectileSpriteLoaded();
        FaceDirection(Vector3.left);
        TryPlayIdle();
    }

    /// <summary>
    /// Updates the baseline position when the enemy is repositioned by wave formation.
    /// </summary>
    public void SetOriginalPosition(Vector3 newPos)
    {
        originalPosition = newPos;
        transform.position = newPos;
        isRanged = CheckIsRanged();
        EnsureProjectileSpriteLoaded();
        FaceDirection(Vector3.left);
        TryPlayIdle();
    }

    private void OnDisable()
    {
        IsVisualActionActive = false;
        if (activeVisualRoutine != null)
        {
            StopCoroutine(activeVisualRoutine);
            activeVisualRoutine = null;
        }
    }

    #region Attack Animation & Motion
    /// <summary>
    /// Plays full attack animation and motion.
    /// Ranged monsters remain at their station, aim, shoot projectiles and play ranged attack animations.
    /// Melee monsters wind up and dash to the player.
    /// Calling coroutine must yield return on this routine to ensure the turn does not advance until it finishes.
    /// </summary>
    public IEnumerator PlayAttackRoutine(Transform playerTarget, System.Action onImpact)
    {
        if (activeVisualRoutine != null)
        {
            StopCoroutine(activeVisualRoutine);
        }

        IsVisualActionActive = true;
        Vector3 basePos = originalPosition != Vector3.zero ? originalPosition : transform.position;
        originalPosition = basePos;

        // Face player (leftwards)
        FaceDirection(Vector3.left);

        if (CheckIsRanged())
        {
            yield return StartCoroutine(PlayRangedAttackRoutine(playerTarget, onImpact));
            yield break;
        }

        float total = Mathf.Max(0.35f, lungeDuration);
        float actualAnticipation = Mathf.Max(0.06f, anticipationDuration > 0f ? anticipationDuration : total * 0.18f);
        float actualDash = total * 0.28f;
        float actualImpact = Mathf.Max(0.20f, impactDuration > 0f ? impactDuration : total * 0.32f);
        float actualRecovery = Mathf.Max(0.10f, recoveryDuration > 0f ? recoveryDuration : total * 0.22f);

        try
        {
            // 1. Wind-up / Anticipation (pulls back slightly to the right with slight squash)
            float elapsed = 0f;
            Vector3 windUpPos = basePos + Vector3.right * 0.15f;
            Vector3 windUpScale = new Vector3(originalScale.x * 0.92f, originalScale.y * 1.08f, originalScale.z);

            while (elapsed < actualAnticipation)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / actualAnticipation);
                transform.position = Vector3.Lerp(basePos, windUpPos, t);
                transform.localScale = Vector3.Lerp(originalScale, windUpScale, t);
                yield return null;
            }

            // 2. Dash forward toward player (to the left, stopping right in front of player)
            Vector3 targetPos = playerTarget != null
                ? new Vector3(Mathf.Min(basePos.x - 0.6f, playerTarget.position.x + 0.8f), playerTarget.position.y, basePos.z)
                : basePos + Vector3.left * Mathf.Max(1.2f, lungeDistance);

            elapsed = 0f;
            Vector3 stretchScale = new Vector3(originalScale.x * 1.15f, originalScale.y * 0.90f, originalScale.z);

            while (elapsed < actualDash)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / actualDash);
                // Ease-in fast strike
                float ease = t * t;
                transform.position = Vector3.Lerp(windUpPos, targetPos, ease);
                transform.localScale = Vector3.Lerp(windUpScale, stretchScale, t);
                yield return null;
            }

            transform.position = targetPos;

            // 3. Impact moment! Trigger Animator attack state, deal damage at impact, flash & shake
            TryPlayAttack();
            onImpact?.Invoke();

            if (CombatJuice.Instance != null)
            {
                CombatJuice.Instance.Shake(0.18f, 0.15f);
                CombatJuice.Instance.HitStop(0.03f, 0.06f);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 1f, 0.6f, 1f); // Bright strike flash
            }

            Vector3 impactScale = new Vector3(originalScale.x * 0.90f, originalScale.y * 1.12f, originalScale.z);
            transform.localScale = impactScale;

            // Hold at impact point for attack animation to visibly execute
            elapsed = 0f;
            while (elapsed < actualImpact)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / actualImpact);
                transform.localScale = Vector3.Lerp(impactScale, originalScale, t);
                yield return null;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }

            // 4. Recovery: Return smoothly back to original combat station
            elapsed = 0f;
            while (elapsed < actualRecovery)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / actualRecovery);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(targetPos, basePos, smoothT);
                transform.localScale = Vector3.Lerp(transform.localScale, originalScale, t);
                yield return null;
            }
        }
        finally
        {
            // 5. Reset to baseline
            transform.position = basePos;
            transform.localScale = originalScale;
            FaceDirection(Vector3.left);
            if (spriteRenderer != null) spriteRenderer.color = originalColor;

            // Return animator to idle
            TryPlayIdle();
            activeVisualRoutine = null;
            IsVisualActionActive = false;
        }
    }

    /// <summary>
    /// Ranged attack routine: The monster remains stationary at its combat position,
    /// aims with anticipation recoil, plays authentic ranged attack animation,
    /// shoots projectile straight to the player's position, and smoothly settles back to idle.
    /// </summary>
    private IEnumerator PlayRangedAttackRoutine(Transform playerTarget, System.Action onImpact)
    {
        Vector3 basePos = originalPosition != Vector3.zero ? originalPosition : transform.position;
        originalPosition = basePos;

        FaceDirection(Vector3.left);

        try
        {
            // 1. Draw / Aim Anticipation: Pull back slightly with tension squash on the spot
            float drawDuration = 0.12f;
            float elapsed = 0f;
            Vector3 drawPos = basePos + Vector3.right * 0.12f;
            Vector3 drawScale = new Vector3(originalScale.x * 0.93f, originalScale.y * 1.07f, originalScale.z);

            while (elapsed < drawDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / drawDuration);
                transform.position = Vector3.Lerp(basePos, drawPos, t);
                transform.localScale = Vector3.Lerp(originalScale, drawScale, t);
                yield return null;
            }

            // 2. Release / Shoot! Trigger authentic ranged attack animation (e.g. SkeletonBow_Attack, Attack)
            TryPlayAttack();

            // Subtle forward release kick and light flash on weapon release
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 1f, 0.7f, 1f);
            }

            float recoilDuration = 0.08f;
            elapsed = 0f;
            Vector3 recoilPos = basePos - Vector3.right * 0.08f;
            Vector3 recoilScale = new Vector3(originalScale.x * 1.06f, originalScale.y * 0.94f, originalScale.z);

            while (elapsed < recoilDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / recoilDuration);
                transform.position = Vector3.Lerp(drawPos, recoilPos, t);
                transform.localScale = Vector3.Lerp(drawScale, recoilScale, t);
                yield return null;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }

            // Wait brief release window for arrow/spear release frame in the clip
            yield return WaitRelease;

            // 3. Projectile flight across arena towards player (chest to chest)
            Vector3 origin = spriteRenderer != null ? (spriteRenderer.bounds.center + Vector3.left * 0.35f) : (transform.position + Vector3.up * 0.7f + Vector3.left * 0.35f);
            Vector3 destination;
            SpriteRenderer playerSR = playerTarget != null ? playerTarget.GetComponentInChildren<SpriteRenderer>() : null;
            if (playerSR != null)
            {
                destination = playerSR.bounds.center;
            }
            else if (playerTarget != null)
            {
                destination = playerTarget.position + Vector3.up * 0.7f;
            }
            else
            {
                destination = basePos + Vector3.left * 4.5f + Vector3.up * 0.7f;
            }

            yield return StartCoroutine(LaunchProjectileRoutine(origin, destination, onImpact));

            // 4. Recovery: Settle smoothly back to baseline position and idle scale
            elapsed = 0f;
            float settleDuration = 0.18f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                transform.position = Vector3.Lerp(recoilPos, basePos, smoothT);
                transform.localScale = Vector3.Lerp(transform.localScale, originalScale, t);
                yield return null;
            }

            // Brief follow-through to finish attack animation cleanly before returning to idle
            yield return WaitFollowThrough;
        }
        finally
        {
            transform.position = basePos;
            transform.localScale = originalScale;
            FaceDirection(Vector3.left);
            if (spriteRenderer != null) spriteRenderer.color = originalColor;

            TryPlayIdle();
            activeVisualRoutine = null;
            IsVisualActionActive = false;
        }
    }

    private IEnumerator LaunchProjectileRoutine(Vector3 origin, Vector3 destination, System.Action onImpact)
    {
        EnsureProjectileSpriteLoaded();

        GameObject proj = null;
        try
        {
            proj = GetPooledProjectile();
            proj.transform.position = origin;
            SpriteRenderer sr = proj.GetComponent<SpriteRenderer>();
            sr.sprite = projectileSprite != null ? projectileSprite : _cachedProjectileSprite;
            
            if (spriteRenderer != null)
            {
                sr.sharedMaterial = spriteRenderer.sharedMaterial;
                sr.sortingLayerID = spriteRenderer.sortingLayerID;
                sr.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder + 10, 30);
            }
            else
            {
                sr.sortingOrder = 30; // Above combat characters and arena tiles
            }

            proj.transform.localScale = new Vector3(1.8f, 1.8f, 1f);

            Vector3 dir = (destination - origin).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            proj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[EnemyCombatVisual] Could not setup projectile: {ex.Message}");
        }

        float dist = Vector3.Distance(origin, destination);
        float flightDuration = Mathf.Clamp(dist / Mathf.Max(6f, projectileSpeed), 0.14f, 0.28f);
        float elapsed = 0f;

        try
        {
            while (elapsed < flightDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / flightDuration);
                if (proj != null)
                {
                    proj.transform.position = Vector3.Lerp(origin, destination, t);
                }
                yield return null;
            }
        }
        finally
        {
            if (proj != null)
            {
                ReturnPooledProjectile(proj);
            }
        }

        // Impact moment! Trigger damage callback and impact juice
        onImpact?.Invoke();

        if (CombatJuice.Instance != null)
        {
            CombatJuice.Instance.Shake(0.18f, 0.14f);
            CombatJuice.Instance.HitStop(0.035f, 0.06f);
            CombatJuice.Instance.PlayHitSparks(destination, new Color(1f, 0.45f, 0.2f, 1f), 12);
        }
    }
    #endregion

    #region Defend, Buff & Debuff Routines
    /// <summary>
    /// Plays a defensive stance pulse (flashes blue, expands defensive scale, shakes).
    /// </summary>
    public IEnumerator PlayDefendRoutine(System.Action onApply)
    {
        if (activeVisualRoutine != null) StopCoroutine(activeVisualRoutine);
        IsVisualActionActive = true;

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
        TryPlayIdle();
        activeVisualRoutine = null;
        IsVisualActionActive = false;
    }

    /// <summary>
    /// Plays an energizing buff pulse (golden flash, hops upward, expands).
    /// </summary>
    public IEnumerator PlayBuffRoutine(System.Action onApply)
    {
        if (activeVisualRoutine != null) StopCoroutine(activeVisualRoutine);
        IsVisualActionActive = true;

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
        TryPlayIdle();
        activeVisualRoutine = null;
        IsVisualActionActive = false;
    }

    /// <summary>
    /// Plays a sinister debuff casting pulse (dark magenta aura, tremors toward player).
    /// </summary>
    public IEnumerator PlayDebuffRoutine(Transform playerTarget, System.Action onApply)
    {
        if (activeVisualRoutine != null) StopCoroutine(activeVisualRoutine);
        IsVisualActionActive = true;

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
        TryPlayIdle();
        activeVisualRoutine = null;
        IsVisualActionActive = false;
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
        // Trigger Hit animation if controller has a Hit state
        TryPlayHit();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.35f, 0.35f, 1f);
        }

        Vector3 basePos = originalPosition != Vector3.zero ? originalPosition : transform.position;
        Vector3 baseScale = originalScale != Vector3.zero ? originalScale : transform.localScale;

        // Phase 1: Impact Recoil & Squash (recoil right + squash)
        Vector3 recoilPos = basePos + Vector3.right * 0.16f;
        Vector3 squashScale = new Vector3(baseScale.x * 1.15f, baseScale.y * 0.82f, baseScale.z);
        float p1Duration = 0.05f;
        float elapsed = 0f;

        while (elapsed < p1Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / p1Duration;
            transform.position = Vector3.Lerp(basePos, recoilPos, t);
            transform.localScale = Vector3.Lerp(baseScale, squashScale, t);
            yield return null;
        }

        // Phase 2: Elastic Rebound
        Vector3 reboundPos = basePos - Vector3.right * 0.04f;
        Vector3 stretchScale = new Vector3(baseScale.x * 0.94f, baseScale.y * 1.06f, baseScale.z);
        float p2Duration = 0.06f;
        elapsed = 0f;

        while (elapsed < p2Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / p2Duration;
            transform.position = Vector3.Lerp(recoilPos, reboundPos, t);
            transform.localScale = Vector3.Lerp(squashScale, stretchScale, t);
            yield return null;
        }

        // Phase 3: Settle back to original
        float p3Duration = 0.06f;
        elapsed = 0f;
        while (elapsed < p3Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / p3Duration;
            transform.position = Vector3.Lerp(reboundPos, basePos, t);
            transform.localScale = Vector3.Lerp(stretchScale, baseScale, t);
            yield return null;
        }

        transform.position = basePos;
        transform.localScale = baseScale;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // If monster survives after hit, return to Idle state
        if (enemyStats == null || enemyStats.currentHealth > 0)
        {
            TryPlayIdle();
        }
    }

    public void PlayDeathEffect()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        TryPlayDead();
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
        // Allow the monster to cleanly play death animation before fading out
        yield return WaitDeadFade;

        float elapsed = 0f;
        float duration = 0.38f;
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

    public void PlayAttackAnimation(PlayerStats player = null, System.Action onImpact = null)
    {
        if (gameObject.activeInHierarchy)
        {
            if (player == null) player = FindFirstObjectByType<PlayerStats>();
            if (activeVisualRoutine != null) StopCoroutine(activeVisualRoutine);
            activeVisualRoutine = StartCoroutine(PlayAttackRoutine(player != null ? player.transform : null, onImpact));
        }
        else
        {
            onImpact?.Invoke();
        }
    }
    #endregion

    #region Helpers & Animation State Dispatchers
    public bool TryPlayAttack()
    {
        return TryPlayAnimatorState(AttackStateHashes);
    }

    public bool TryPlayIdle()
    {
        return TryPlayAnimatorState(IdleStateHashes);
    }

    public bool TryPlayHit()
    {
        return TryPlayAnimatorState(HitStateHashes);
    }

    public bool TryPlayDead()
    {
        return TryPlayAnimatorState(DeadStateHashes);
    }

    private bool IsSpriteNaturallyFacingRight()
    {
        string n = gameObject.name.ToLower();
        // Monsters naturally facing right in their source sprite:
        // Need flipX = true to face left towards the player
        if (n.Contains("skeleton") || n.Contains("bowman") || n.Contains("rat") || n.Contains("bat") || n.Contains("slime_boss"))
        {
            return true;
        }

        // Monsters naturally facing left (ME1, ME2, Flying, Range, Flying Rider)
        // or facing forward (Kenmi Slime_0 purple): do not flip (flipX = false)
        return false;
    }

    private void FaceDirection(Vector3 dir)
    {
        if (spriteRenderer != null)
        {
            bool naturallyFacesRight = defaultFacesRight;
            bool wantFaceLeft = dir.x <= 0f;
            spriteRenderer.flipX = naturallyFacesRight ? wantFaceLeft : !wantFaceLeft;
        }
    }

    private bool TryPlayAnimatorState(int[] stateHashes)
    {
        if (animator == null || !animator.isActiveAndEnabled || stateHashes == null) return false;

        for (int i = 0; i < stateHashes.Length; i++)
        {
            int hash = stateHashes[i];
            if (animator.HasState(0, hash))
            {
                animator.Play(hash, 0, 0f);
                return true;
            }
        }

        return false;
    }

    private bool TryPlayAnimatorState(params string[] stateNames)
    {
        if (animator == null || !animator.isActiveAndEnabled || stateNames == null) return false;

        for (int i = 0; i < stateNames.Length; i++)
        {
            string stateName = stateNames[i];
            int hash = Animator.StringToHash(stateName);
            if (animator.HasState(0, hash))
            {
                animator.Play(hash, 0, 0f);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether this enemy is a ranged attacker based on manual flag, EnemyStats, EnemyData, name, or controller clips.
    /// </summary>
    public bool CheckIsRanged()
    {
        if (isRanged) return true;

        if (enemyStats != null)
        {
            if (enemyStats.isRanged) return true;
            if (enemyStats.enemyData != null)
            {
                if (enemyStats.enemyData.isRanged) return true;
                string dName = enemyStats.enemyData.enemyName != null ? enemyStats.enemyData.enemyName.ToLower() : "";
                if (dName.Contains("archer") || dName.Contains("bowman") || dName.Contains("bow") || dName.Contains("range") || dName.Contains("skeleton king") || dName.Contains("skeleton_king") || dName.Contains("ancient skeleton") || dName.Contains("cung") || dName.Contains("x\u01B0\u01A1ng c\u1ED5 \u0111\u1EA1i") || dName.Contains("xuong co dai"))
                {
                    return true;
                }
            }
        }

        string goName = gameObject.name.ToLower();
        if (goName.Contains("range") || goName.Contains("bowman") || goName.Contains("archer") || goName.Contains("skeleton_king") || goName.Contains("skeleton king") || goName.Contains("skeletonbow"))
        {
            return true;
        }

        if (animator != null && animator.isActiveAndEnabled)
        {
            if (animator.HasState(0, Animator.StringToHash("SkeletonBow_Attack")))
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureProjectileSpriteLoaded()
    {
        if (projectileSprite != null) return;
        if (_cachedProjectileSprite != null)
        {
            projectileSprite = _cachedProjectileSprite;
            return;
        }

#if UNITY_EDITOR
        _cachedProjectileSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Fantasy Adventure Pack/Sprites/Proyectiles & Icons/RE_Projectile.png");
#endif
        if (_cachedProjectileSprite == null)
        {
            Sprite[] all = Resources.FindObjectsOfTypeAll<Sprite>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && (all[i].name == "RE_Projectile" || all[i].name.Contains("Projectile")))
                {
                    _cachedProjectileSprite = all[i];
                    break;
                }
            }
        }

        // Procedural arrow sprite fallback in case file is absent in standalone builds
        if (_cachedProjectileSprite == null)
        {
            Texture2D tex = new Texture2D(16, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color shaft = new Color(0.75f, 0.55f, 0.25f, 1f);
            Color head = new Color(0.9f, 0.9f, 0.95f, 1f);
            Color tip = new Color(1f, 1f, 1f, 1f);
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    if (x < 11) tex.SetPixel(x, y, shaft);
                    else if (x < 14) tex.SetPixel(x, y, head);
                    else tex.SetPixel(x, y, tip);
                }
            }
            tex.Apply();
            _cachedProjectileSprite = Sprite.Create(tex, new Rect(0, 0, 16, 4), new Vector2(0.5f, 0.5f), 16f);
        }

        projectileSprite = _cachedProjectileSprite;
    }
    #endregion
}
