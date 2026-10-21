using TMPro;
using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [Header("Prefabs (Configured in Unity Editor)")]
    [Tooltip("Prefab for normal monster or player damage numbers")]
    public GameObject normalDamagePrefab;

    [Tooltip("Prefab for critical / heavy damage numbers")]
    public GameObject critDamagePrefab;

    [Tooltip("Prefab for healing numbers")]
    public GameObject healPrefab;

    [Tooltip("Prefab for status notification text (Miss, Buff, Shield...)")]
    public GameObject statusTextPrefab;

    public static DamagePopupManager EnsureInstance()
    {
        if (Instance != null) return Instance;
        DamagePopupManager found = FindFirstObjectByType<DamagePopupManager>(FindObjectsInactive.Include);
        if (found != null)
        {
            Instance = found;
            return Instance;
        }
        GameObject go = new GameObject("DamagePopupManager_Runtime");
        Instance = go.AddComponent<DamagePopupManager>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Displays damage popup (automatically chooses Normal or Crit prefab).
    /// </summary>
    public static void ShowDamage(Vector3 worldPos, int amount, bool isPlayer = false, bool isCrit = false, float damagePercent = 0f, bool isLethal = false)
    {
        if (amount <= 0)
        {
            ShowText(worldPos, "BLOCKED", Color.gray);
            return;
        }

        EnsureInstance();

        // Synchronize color and scale according to damage tier
        Color dmgColor;
        float popupSize;

        if (isPlayer)
        {
            dmgColor = new Color(1f, 0.35f, 0.35f);
            popupSize = 4.2f;
        }
        else if (isCrit || isLethal || amount >= 30)
        {
            // Tier 3: Fiery red crit / massive damage
            dmgColor = new Color(1f, 0.25f, 0.18f);
            popupSize = 5.6f;
        }
        else if (amount >= 15)
        {
            // Tier 2: Golden amber heavy damage
            dmgColor = new Color(1f, 0.78f, 0.18f);
            popupSize = 4.8f;
        }
        else
        {
            // Tier 1: Silver bright basic damage
            dmgColor = Color.white;
            popupSize = 4.0f;
        }

        GameObject prefabToSpawn = (isCrit || isLethal || amount >= 30) && Instance != null && Instance.critDamagePrefab != null
            ? Instance.critDamagePrefab
            : (Instance != null ? Instance.normalDamagePrefab : null);

        SpawnPopup(prefabToSpawn, worldPos, $"-{amount}", dmgColor, popupSize);
    }

    /// <summary>
    /// Displays healing popup.
    /// </summary>
    public static void ShowHeal(Vector3 worldPos, int amount)
    {
        if (amount <= 0) return;
        EnsureInstance();

        GameObject prefabToSpawn = Instance != null ? Instance.healPrefab : null;
        SpawnPopup(prefabToSpawn, worldPos, $"+{amount}", new Color(0.3f, 1f, 0.4f), 4.5f);
    }

    /// <summary>
    /// Displays status text (Def, Shield, Miss, Buff...).
    /// </summary>
    public static void ShowText(Vector3 worldPos, string text, Color color = default, float size = 0f)
    {
        EnsureInstance();

        GameObject prefabToSpawn = Instance != null ? Instance.statusTextPrefab : null;
        SpawnPopup(prefabToSpawn, worldPos, text, color, size);
    }

    private static GameObject CreateFallbackPopup(string content, Color color, float size)
    {
        GameObject go = new GameObject("Popup_Fallback", typeof(TextMeshPro));
        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        tmp.text = content;
        tmp.fontSize = size > 0 ? size : 4.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color != default ? color : Color.white;
        tmp.sortingOrder = 50;
        return go;
    }

    private static void SpawnPopup(GameObject prefab, Vector3 worldPos, string content, Color customColor = default, float customSize = 0f)
    {
        // Add slight random offset to prevent multiple damage numbers from overlapping
        Vector3 spawnPos = worldPos + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.1f, 0.15f), 0f);
        GameObject popup = null;

        if (prefab != null)
        {
            popup = Instantiate(prefab, spawnPos, Quaternion.identity);
        }
        else
        {
            popup = CreateFallbackPopup(content, customColor, customSize);
            popup.transform.position = spawnPos;
        }

        TMP_Text tmp = popup.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = content;
            if (customColor != default)
            {
                tmp.color = customColor;
            }
            if (customSize > 0f)
            {
                tmp.fontSize = customSize;
            }
        }

        // Animate bounce, float upward, and fade out
        if (Instance != null)
        {
            Instance.StartCoroutine(AnimatePopupRoutine(popup, tmp));
        }
        else
        {
            Destroy(popup, 0.8f);
        }
    }

    private static System.Collections.IEnumerator AnimatePopupRoutine(GameObject popup, TMP_Text tmp)
    {
        if (popup == null) yield break;

        Transform popupTransform = popup.transform;
        Vector3 startPos = popupTransform.position;
        Vector3 originalScale = popupTransform.localScale;

        // Random bounce velocity (slight left/right variance + upward burst)
        float horizontalSpeed = Random.Range(-0.4f, 0.4f);
        float verticalSpeed = Random.Range(1.8f, 2.3f);
        Vector3 velocity = new Vector3(horizontalSpeed, verticalSpeed, 0f);

        Color startColor = tmp != null ? tmp.color : Color.white;

        float duration = 0.75f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (popup == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 1. Float upward & decelerate
            popupTransform.position += velocity * Time.deltaTime;
            velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * 3.5f);

            // 2. Punch Scale effect: pop quickly at start then settle back to normal
            if (t < 0.2f)
            {
                float punchT = t / 0.2f;
                // Pop up to 1.35x then shrink
                float scaleMultiplier = Mathf.Lerp(0.5f, 1.35f, punchT);
                popupTransform.localScale = originalScale * scaleMultiplier;
            }
            else if (t < 0.35f)
            {
                float settleT = (t - 0.2f) / 0.15f;
                // Settle from 1.35x to 1.0x
                float scaleMultiplier = Mathf.Lerp(1.35f, 1.0f, settleT);
                popupTransform.localScale = originalScale * scaleMultiplier;
            }

            // 3. Fade out in second half of animation
            if (t > 0.45f && tmp != null)
            {
                float fadeT = (t - 0.45f) / (1f - 0.45f);
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, fadeT);
                tmp.color = c;
            }

            yield return null;
        }

        if (popup != null)
        {
            Destroy(popup);
        }
    }
}
