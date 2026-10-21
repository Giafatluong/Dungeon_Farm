using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private float lungeDuration = 0.44f;

    [Header("Attack Animation & Weapon Overlay")]
    [SerializeField] private SpriteRenderer weaponOverlayRenderer;
    private Sprite[] attackSprites;
    private Sprite[] weaponSprites;
    private Sprite idleRightSprite;
    private bool isAttacking = false;

    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int LastHorizontalHash = Animator.StringToHash("LastHorizontal");
    private static readonly int LastVerticalHash = Animator.StringToHash("LastVertical");
    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly WaitForSeconds PulseWait = new(0.25f);

    private static Sprite[] _cachedAttackSprites;
    private static Sprite[] _cachedWeaponSprites;
    private static Sprite _cachedIdleRightSprite;

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

        EnsureWeaponOverlay();
        LoadAttackSprites();
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

        CombatManager combatManager = CombatManager.Instance != null ? CombatManager.Instance : FindFirstObjectByType<CombatManager>();
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
        // During combat (except when moving transition or attacking), keep Player strictly in Idle state without running in place
        if (!isMovingTransition && !isAttacking && animator != null && animator.isActiveAndEnabled)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash != IdleHash)
            {
                animator.SetFloat(LastHorizontalHash, 1f);
                animator.SetFloat(LastVerticalHash, 0f);
                animator.SetFloat(HorizontalHash, 0f);
                animator.SetFloat(VerticalHash, 0f);
                animator.SetFloat(SpeedHash, 0f);
                animator.Play(IdleHash, 0);
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
                    animator.SetFloat(LastHorizontalHash, 1f);
                    animator.SetFloat(LastVerticalHash, 0f);
                    animator.SetFloat(HorizontalHash, 1f);
                    animator.SetFloat(VerticalHash, 0f);
                    animator.SetFloat(SpeedHash, 1f);
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

    private void EnsureWeaponOverlay()
    {
        if (weaponOverlayRenderer == null)
        {
            Transform child = transform.Find("WeaponOverlay");
            if (child == null)
            {
                GameObject go = new GameObject("WeaponOverlay");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                weaponOverlayRenderer = go.AddComponent<SpriteRenderer>();
            }
            else
            {
                weaponOverlayRenderer = child.GetComponent<SpriteRenderer>();
            }
        }

        if (weaponOverlayRenderer != null && spriteRenderer != null)
        {
            weaponOverlayRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            weaponOverlayRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
            weaponOverlayRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
            weaponOverlayRenderer.sprite = null;
        }
    }

    public void SetIdle()
    {
        isAttacking = false;
        if (weaponOverlayRenderer != null)
        {
            weaponOverlayRenderer.sprite = null;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
            if (idleRightSprite != null)
            {
                spriteRenderer.sprite = idleRightSprite;
            }
        }

        if (animator != null)
        {
            if (!animator.enabled) animator.enabled = true;
            animator.SetFloat(LastHorizontalHash, 1f);
            animator.SetFloat(LastVerticalHash, 0f);
            animator.SetFloat(HorizontalHash, 0f);
            animator.SetFloat(VerticalHash, 0f);
            animator.SetFloat(SpeedHash, 0f);
            animator.Update(0f);
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

        CombatManager combatManager = CombatManager.Instance != null ? CombatManager.Instance : FindFirstObjectByType<CombatManager>();
        if (combatManager != null)
        {
            combatManager.OnPlayerAttackAction -= OnAttack;
            combatManager.OnCombatStarted -= SetIdle;
            combatManager.OnCombatEnded -= SetIdle;
            combatManager.OnTurnChanged -= HandleTurnChanged;
        }
    }

    private void LoadAttackSprites()
    {
        if (_cachedAttackSprites != null && _cachedAttackSprites.Length > 0 &&
            _cachedWeaponSprites != null && _cachedWeaponSprites.Length > 0 &&
            _cachedIdleRightSprite != null)
        {
            attackSprites = _cachedAttackSprites;
            weaponSprites = _cachedWeaponSprites;
            idleRightSprite = _cachedIdleRightSprite;
            return;
        }

#if UNITY_EDITOR
        string path = "Assets/Kenmi/Cute Fantasy RPG - 16x16 top down pixel art asset pack/Sprites/Player/Player_Attack_NoWeapon.png";
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets != null)
        {
            List<Sprite> list = new();
            // Row 4: Frames 16, 17, 18, 19 are the horizontal rightward slash sequence in Player_Attack_NoWeapon
            for (int i = 16; i <= 19; i++)
            {
                string targetName = $"Player_Attack_NoWeapon_{i}";
                for (int j = 0; j < assets.Length; j++)
                {
                    if (assets[j] is Sprite s && s.name == targetName)
                    {
                        list.Add(s);
                        break;
                    }
                }
            }
            if (list.Count > 0) attackSprites = list.ToArray();
        }

        // 2. Load white sword slash sprites from Player_Attack_OnlyWeapon (Row 4: frames 12, 13, 14 horizontal arc)
        string weaponPath = "Assets/Kenmi/Cute Fantasy RPG - 16x16 top down pixel art asset pack/Sprites/Player/Player_Attack_OnlyWeapon.png";
        Object[] weaponAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(weaponPath);
        if (weaponAssets != null)
        {
            List<Sprite> list = new();
            for (int i = 12; i <= 14; i++)
            {
                string targetName = $"Player_Attack_OnlyWeapon_{i}";
                for (int j = 0; j < weaponAssets.Length; j++)
                {
                    if (weaponAssets[j] is Sprite s && s.name == targetName)
                    {
                        list.Add(s);
                        break;
                    }
                }
            }
            if (list.Count > 0) weaponSprites = list.ToArray();
        }

        // 3. Cache Player_6 (Idle facing right) from Player.png to prevent turning forward
        string playerPath = "Assets/Kenmi/Cute Fantasy RPG - 16x16 top down pixel art asset pack/Sprites/Player/Player.png";
        Object[] playerAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(playerPath);
        if (playerAssets != null)
        {
            for (int j = 0; j < playerAssets.Length; j++)
            {
                if (playerAssets[j] is Sprite s && s.name == "Player_6")
                {
                    idleRightSprite = s;
                    break;
                }
            }
        }
#endif
        if (attackSprites == null || attackSprites.Length == 0)
        {
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            if (allSprites != null)
            {
                List<Sprite> list = new();
                for (int i = 16; i <= 19; i++)
                {
                    string targetName = $"Player_Attack_NoWeapon_{i}";
                    for (int j = 0; j < allSprites.Length; j++)
                    {
                        if (allSprites[j] != null && allSprites[j].name == targetName)
                        {
                            list.Add(allSprites[j]);
                            break;
                        }
                    }
                }
                if (list.Count > 0) attackSprites = list.ToArray();
            }
        }

        if (weaponSprites == null || weaponSprites.Length == 0)
        {
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            if (allSprites != null)
            {
                List<Sprite> list = new();
                for (int i = 12; i <= 14; i++)
                {
                    string targetName = $"Player_Attack_OnlyWeapon_{i}";
                    for (int j = 0; j < allSprites.Length; j++)
                    {
                        if (allSprites[j] != null && allSprites[j].name == targetName)
                        {
                            list.Add(allSprites[j]);
                            break;
                        }
                    }
                }
                if (list.Count > 0) weaponSprites = list.ToArray();
            }
        }

        if (idleRightSprite == null)
        {
            Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            if (allSprites != null)
            {
                for (int j = 0; j < allSprites.Length; j++)
                {
                    if (allSprites[j] != null && allSprites[j].name == "Player_6")
                    {
                        idleRightSprite = allSprites[j];
                        break;
                    }
                }
            }
        }

        _cachedAttackSprites = attackSprites;
        _cachedWeaponSprites = weaponSprites;
        _cachedIdleRightSprite = idleRightSprite;
    }

    private void OnAttack(EnemyStats target, int damage)
    {
        if (currentVisualRoutine != null) StopCoroutine(currentVisualRoutine);
        bool isCrit = damage >= 30;
        currentVisualRoutine = StartCoroutine(AttackLungeRoutine(target, damage, isCrit));
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

    private IEnumerator AttackLungeRoutine(EnemyStats target, int damage, bool isCrit)
    {
        isAttacking = true;
        if (animator != null) animator.enabled = false;

        Vector3 startPos = originalPosition;

        // Move close to enemy position before executing the slash
        Vector3 targetPos;
        if (target != null)
        {
            float targetX = Mathf.Max(startPos.x + 0.6f, target.transform.position.x - 0.8f);
            float targetY = target.transform.position.y;
            targetPos = new Vector3(targetX, targetY, startPos.z);
        }
        else
        {
            targetPos = startPos + new Vector3(Mathf.Max(1.2f, lungeDistance), 0f, 0f);
        }

        // Optimized timing: quick, decisive, agile strike (~0.44s total)
        float totalDur = Mathf.Max(0.35f, lungeDuration);
        float windupDuration = totalDur * 0.18f; // ~0.08s
        float dashDuration = totalDur * 0.28f;   // ~0.12s
        float slashDuration = totalDur * 0.27f;  // ~0.12s
        float returnDuration = totalDur * 0.27f; // ~0.12s

        // -------------------------------------------------------------
        // Phase 1: Wind-up & Ready Stance
        // Player lowers center of gravity and raises sword preparing to dash
        // -------------------------------------------------------------
        Vector3 windupPos = startPos - new Vector3(0.12f, 0f, 0f);
        Vector3 windupScale = new Vector3(originalScale.x * 0.92f, originalScale.y * 1.08f, originalScale.z);

        if (attackSprites != null && attackSprites.Length > 0 && spriteRenderer != null)
        {
            spriteRenderer.sprite = attackSprites[0]; // Player_Attack_NoWeapon_16
        }
        if (weaponOverlayRenderer != null && weaponSprites != null && weaponSprites.Length > 0)
        {
            weaponOverlayRenderer.sprite = weaponSprites[0]; // Player_Attack_OnlyWeapon_12
        }

        float t = 0f;
        while (t < windupDuration)
        {
            t += Time.deltaTime;
            float factor = Mathf.Clamp01(t / windupDuration);
            transform.position = Vector3.Lerp(startPos, windupPos, factor);
            transform.localScale = Vector3.Lerp(originalScale, windupScale, factor);
            yield return null;
        }

        // -------------------------------------------------------------
        // Phase 2: Dash to Enemy
        // Dash across the arena close to the enemy
        // -------------------------------------------------------------
        Vector3 stretchScale = new Vector3(originalScale.x * 1.15f, originalScale.y * 0.90f, originalScale.z);

        t = 0f;
        while (t < dashDuration)
        {
            t += Time.deltaTime;
            float factor = Mathf.Clamp01(t / dashDuration);
            // Quadratic ease-in: fast acceleration
            float ease = factor * factor;
            transform.position = Vector3.Lerp(windupPos, targetPos, ease);
            transform.localScale = Vector3.Lerp(windupScale, stretchScale, factor);
            yield return null;
        }

        // -------------------------------------------------------------
        // Phase 3: Horizontal Slash & Impact
        // Reached enemy -> execute horizontal strike cutting across the target!
        // -------------------------------------------------------------
        transform.position = targetPos;
        Vector3 impactScale = new Vector3(originalScale.x * 0.90f, originalScale.y * 1.12f, originalScale.z);
        transform.localScale = impactScale;

        // Perform strong horizontal slash (Player_Attack_NoWeapon_17 + Player_Attack_OnlyWeapon_13)
        if (attackSprites != null && attackSprites.Length > 1 && spriteRenderer != null)
        {
            spriteRenderer.sprite = attackSprites[1];
        }
        if (weaponOverlayRenderer != null && weaponSprites != null && weaponSprites.Length > 1)
        {
            weaponOverlayRenderer.sprite = weaponSprites[1];
        }

        // Trigger damage feedback on enemy at the exact impact frame:
        if (target != null)
        {
            target.TriggerDamageFeedback(damage);
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.PlayAttackAnimationByDamage(target.transform.position, damage, isCrit);
            }
        }
        else if (VFXManager.Instance != null)
        {
            VFXManager.Instance.PlayAttackAnimationByDamage(targetPos, damage, isCrit);
        }

        // Strong hit feel: Camera shake + Hit-stop
        if (CombatJuice.Instance != null)
        {
            float shakeAmt = isCrit || damage >= 30 ? 0.30f : (damage >= 15 ? 0.18f : 0.10f);
            float shakeDur = isCrit || damage >= 30 ? 0.20f : 0.14f;
            CombatJuice.Instance.Shake(shakeAmt, shakeDur);
            CombatJuice.Instance.HitStop(isCrit ? 0.04f : 0.025f, 0.06f);
        }

        float halfSlash = slashDuration * 0.5f;
        t = 0f;
        while (t < halfSlash)
        {
            t += Time.deltaTime;
            float factor = Mathf.Clamp01(t / halfSlash);
            transform.localScale = Vector3.Lerp(impactScale, originalScale, factor * 0.5f);
            yield return null;
        }

        // Transition to follow-through / spark frame
        if (attackSprites != null && attackSprites.Length > 2 && spriteRenderer != null)
        {
            spriteRenderer.sprite = attackSprites[2]; // Player_Attack_NoWeapon_18
        }
        if (weaponOverlayRenderer != null && weaponSprites != null && weaponSprites.Length > 2)
        {
            weaponOverlayRenderer.sprite = weaponSprites[2]; // Player_Attack_OnlyWeapon_14
        }

        t = 0f;
        while (t < halfSlash)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // -------------------------------------------------------------
        // Phase 4: Sheath & Return
        // White blade fades, sheath weapon and glide smoothly back to starting position
        // -------------------------------------------------------------
        if (weaponOverlayRenderer != null)
        {
            weaponOverlayRenderer.sprite = null;
        }

        if (attackSprites != null && attackSprites.Length > 3 && spriteRenderer != null)
        {
            spriteRenderer.sprite = attackSprites[3]; // Player_Attack_NoWeapon_19
        }

        Vector3 currentHoldScale = transform.localScale;
        t = 0f;
        while (t < returnDuration)
        {
            t += Time.deltaTime;
            float factor = Mathf.Clamp01(t / returnDuration);
            float ease = Mathf.SmoothStep(0f, 1f, factor);
            transform.position = Vector3.Lerp(targetPos, startPos, ease);
            transform.localScale = Vector3.Lerp(currentHoldScale, originalScale, factor);
            yield return null;
        }

        // -------------------------------------------------------------
        // Phase 5: Settle back into Idle facing right
        // -------------------------------------------------------------
        transform.position = startPos;
        transform.localScale = originalScale;
        if (weaponOverlayRenderer != null)
        {
            weaponOverlayRenderer.sprite = null;
        }

        if (idleRightSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = idleRightSprite;
        }

        isAttacking = false;
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetFloat(LastHorizontalHash, 1f);
            animator.SetFloat(LastVerticalHash, 0f);
            animator.SetFloat(HorizontalHash, 0f);
            animator.SetFloat(VerticalHash, 0f);
            animator.SetFloat(SpeedHash, 0f);
            animator.Update(0f);
        }

        SetIdle();
        currentVisualRoutine = null;
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
            yield return PulseWait;
            spriteRenderer.color = originalColor;
        }
        SetIdle();
    }

    private IEnumerator HealPulseRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.45f, 1f, 0.6f, 1f);
            yield return PulseWait;
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
