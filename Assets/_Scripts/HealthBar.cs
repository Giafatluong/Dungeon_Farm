using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI intentText;
    [SerializeField] private Vector3 offset = new(0, 0.9f, 0);
    public EnemyStats enemyTarget;
    public PlayerStats playerTarget;

    private int lastHealth = -1;
    private int lastMaxHealth = -1;
    private EnemyData.EnemyIntent? lastIntent = null;
    private int lastAtk = -1;

    [SerializeField] private Image ghostHealthBar;
    private Coroutine ghostDrainRoutine;

    private void Awake()
    {
        EnsureGhostHealthBar();
    }

    private void OnDisable()
    {
        if (ghostDrainRoutine != null)
        {
            StopCoroutine(ghostDrainRoutine);
            ghostDrainRoutine = null;
        }
    }

    private void EnsureGhostHealthBar()
    {
        // Never execute or instantiate on Prefab Assets in Project folder
        if (!gameObject.scene.IsValid()) return;
        if (ghostHealthBar != null) return;

        // Auto-resolve child healthBar image if not assigned or improperly assigned
        if (healthBar == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].gameObject != gameObject)
                {
                    healthBar = images[i];
                    break;
                }
            }
            if (healthBar == null)
            {
                healthBar = GetComponent<Image>();
            }
        }

        if (healthBar == null || healthBar.transform == null || healthBar.transform.parent == null) return;

        Transform parentTransform = healthBar.transform.parent;

        // Check if GhostHealthBar already exists as sibling
        Transform existing = parentTransform.Find("GhostHealthBar");
        if (existing != null)
        {
            ghostHealthBar = existing.GetComponent<Image>();
            if (ghostHealthBar != null) return;
        }

        // Avoid Instantiate(healthBar.gameObject) because if healthBar.gameObject contains HealthBar script,
        // it triggers an infinite recursion in Awake() causing a StackOverflowException and hard crash.
        // Instead, create a clean GameObject with only RectTransform, CanvasRenderer and Image components.
        GameObject ghostObj = new GameObject("GhostHealthBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        ghostObj.transform.SetParent(parentTransform, false);
        ghostObj.transform.SetSiblingIndex(healthBar.transform.GetSiblingIndex());

        RectTransform ghostRect = ghostObj.GetComponent<RectTransform>();
        RectTransform srcRect = healthBar.rectTransform;
        if (srcRect != null && ghostRect != null)
        {
            ghostRect.anchorMin = srcRect.anchorMin;
            ghostRect.anchorMax = srcRect.anchorMax;
            ghostRect.anchoredPosition = srcRect.anchoredPosition;
            ghostRect.sizeDelta = srcRect.sizeDelta;
            ghostRect.pivot = srcRect.pivot;
            ghostRect.localScale = srcRect.localScale;
            ghostRect.localRotation = srcRect.localRotation;
        }

        ghostHealthBar = ghostObj.GetComponent<Image>();
        if (ghostHealthBar != null)
        {
            ghostHealthBar.sprite = healthBar.sprite;
            ghostHealthBar.type = healthBar.type;
            ghostHealthBar.fillMethod = healthBar.fillMethod;
            ghostHealthBar.fillOrigin = healthBar.fillOrigin;
            ghostHealthBar.fillClockwise = healthBar.fillClockwise;
            ghostHealthBar.color = new Color(1f, 0.88f, 0.4f, 0.95f);
            ghostHealthBar.fillAmount = healthBar.fillAmount;
            ghostHealthBar.raycastTarget = false;
        }
    }

    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (enemyTarget != null && enemyTarget.currentHealth > 0)
        {
            if (enemyTarget.targetSelector != null)
            {
                enemyTarget.targetSelector.SelectEnemy(enemyTarget);
            }
            else
            {
                EnemyTargetSelector sel = FindFirstObjectByType<EnemyTargetSelector>();
                if (sel != null) sel.SelectEnemy(enemyTarget);
            }
        }
    }

    public void SetHealthBar(int currentHealth, int maxHealth)
    {
        EnsureGhostHealthBar();

        if (healthBar != null && maxHealth > 0)
        {
            float targetFill = Mathf.Clamp01((float)currentHealth / maxHealth);
            float previousFill = healthBar.fillAmount;
            healthBar.fillAmount = targetFill;

            if (ghostHealthBar != null)
            {
                if (targetFill < previousFill && gameObject.activeInHierarchy)
                {
                    if (ghostDrainRoutine != null) StopCoroutine(ghostDrainRoutine);
                    ghostDrainRoutine = StartCoroutine(GhostDrainRoutine(targetFill));
                }
                else
                {
                    if (ghostDrainRoutine != null)
                    {
                        StopCoroutine(ghostDrainRoutine);
                        ghostDrainRoutine = null;
                    }
                    ghostHealthBar.fillAmount = targetFill;
                }
            }
        }
    }

    private System.Collections.IEnumerator GhostDrainRoutine(float targetFill)
    {
        yield return new WaitForSeconds(0.2f);
        if (ghostHealthBar == null) yield break;

        float startFill = ghostHealthBar.fillAmount;
        float elapsed = 0f;
        float duration = 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (ghostHealthBar != null)
            {
                ghostHealthBar.fillAmount = Mathf.Lerp(startFill, targetFill, elapsed / duration);
            }
            yield return null;
        }

        if (ghostHealthBar != null)
        {
            ghostHealthBar.fillAmount = targetFill;
        }
        ghostDrainRoutine = null;
    }

    private Collider2D cachedEnemyCollider;

    public void SetEnemyTarget(EnemyStats enemy)
    {
        if (enemyTarget != null)
        {
            enemyTarget.OnIntentChanged -= HandleIntentChanged;
        }

        enemyTarget = enemy;
        playerTarget = null;
        lastHealth = -1;
        lastMaxHealth = -1;
        lastIntent = null;
        lastAtk = -1;
        cachedEnemyCollider = enemy != null ? enemy.GetComponent<Collider2D>() : null;

        if (enemyTarget != null)
        {
            enemyTarget.OnIntentChanged -= HandleIntentChanged;
            enemyTarget.OnIntentChanged += HandleIntentChanged;
        }

        EnsureIntentText();

        if (intentText != null)
        {
            intentText.gameObject.SetActive(true);
        }
    }

    private void HandleIntentChanged(EnemyStats sender, EnemyData.EnemyIntent newIntent)
    {
        if (sender == enemyTarget)
        {
            lastIntent = null; // force immediate re-evaluation in next frame
        }
    }

    private void EnsureIntentText()
    {
        if (intentText != null) return;
        Transform existing = transform.Find("IntentText");
        if (existing != null)
        {
            intentText = existing.GetComponent<TextMeshProUGUI>();
            if (intentText != null) return;
        }

        GameObject go = new GameObject("IntentText", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0, 4f);
        rt.sizeDelta = new Vector2(160, 24);

        intentText = go.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(intentText);
        intentText.fontSize = 13;
        intentText.fontStyle = FontStyles.Bold;
        intentText.alignment = TextAlignmentOptions.Center;
    }

    public void SetPlayerTarget(PlayerStats player)
    {
        if (enemyTarget != null)
        {
            enemyTarget.OnIntentChanged -= HandleIntentChanged;
        }

        playerTarget = player;
        enemyTarget = null;
        cachedEnemyCollider = null;
        lastHealth = -1;
        lastMaxHealth = -1;
        lastIntent = null;
        lastAtk = -1;

        if (intentText != null)
        {
            intentText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (enemyTarget != null)
        {
            enemyTarget.OnIntentChanged -= HandleIntentChanged;
        }
    }

    private void Update()
    {
        // Enemy health bar: If target is null, dead (HP <= 0) or disabled, destroy health bar
        if (playerTarget == null)
        {
            if (enemyTarget == null || enemyTarget.currentHealth <= 0 || !enemyTarget.gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (enemyTarget != null)
        {
            float topY = offset.y;
            if (cachedEnemyCollider != null)
            {
                topY = Mathf.Max(offset.y, (cachedEnemyCollider.bounds.max.y - enemyTarget.transform.position.y) + 0.35f);
            }
            transform.position = enemyTarget.transform.position + new Vector3(offset.x, topY, offset.z);

            int curHp = enemyTarget.currentHealth;
            int maxHp = enemyTarget.enemyData != null ? enemyTarget.enemyData.maxHealth : 1;

            if (curHp != lastHealth || maxHp != lastMaxHealth)
            {
                lastHealth = curHp;
                lastMaxHealth = maxHp;
                SetHealthBar(curHp, maxHp);
            }

            if (intentText != null)
            {
                int curAtk = enemyTarget.GetCurrentATK();
                EnemyData.EnemyIntent curIntent = enemyTarget.currentIntent;

                if (curIntent != lastIntent || curAtk != lastAtk)
                {
                    lastIntent = curIntent;
                    lastAtk = curAtk;

                    switch (curIntent)
                    {
                        case EnemyData.EnemyIntent.Attack:
                            intentText.text = $"<color=#FF5555>ATK {curAtk}</color>";
                            break;
                        case EnemyData.EnemyIntent.Defend:
                            int defVal = (enemyTarget.enemyData != null) ? Mathf.Max(5, enemyTarget.enemyData.DEF) : 5;
                            intentText.text = $"<color=#55AAFF>DEFEND (+{defVal})</color>";
                            break;
                        case EnemyData.EnemyIntent.Buff:
                            intentText.text = "<color=#FFDD44>BUFF</color>";
                            break;
                        case EnemyData.EnemyIntent.Debuff:
                            intentText.text = "<color=#DD55FF>DEBUFF</color>";
                            break;
                        default:
                            intentText.text = $"<color=#FF5555>ATK {curAtk}</color>";
                            break;
                    }
                }
            }
        }

        if (playerTarget != null)
        {
            if (playerTarget.currentHealth <= 0 || !playerTarget.gameObject.activeInHierarchy)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = playerTarget.transform.position + offset;

            int curHp = playerTarget.currentHealth;
            int maxHp = playerTarget.maxHealth;

            if (curHp != lastHealth || maxHp != lastMaxHealth)
            {
                lastHealth = curHp;
                lastMaxHealth = maxHp;
                SetHealthBar(curHp, maxHp);
            }
        }
    }
}