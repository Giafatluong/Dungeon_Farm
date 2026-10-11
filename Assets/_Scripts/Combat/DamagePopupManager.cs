using TMPro;
using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance { get; private set; }

    [Header("Prefabs (Thiết kế trong Unity & kéo thả vào đây)")]
    [Tooltip("Prefab số dame thường của quái hoặc player")]
    public GameObject normalDamagePrefab;

    [Tooltip("Prefab số dame chí mạng / dame to")]
    public GameObject critDamagePrefab;

    [Tooltip("Prefab số hồi máu")]
    public GameObject healPrefab;

    [Tooltip("Prefab text thông báo (Miss, Buff, Shield...)")]
    public GameObject statusTextPrefab;

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
    /// Hiển thị sát thương (Tự động chọn Prefab Chí mạng hoặc Thường theo thiết kế của bạn)
    /// </summary>
    public static void ShowDamage(Vector3 worldPos, int amount, bool isPlayer = false, bool isCrit = false, float damagePercent = 0f, bool isLethal = false)
    {
        if (amount <= 0)
        {
            ShowText(worldPos, "BLOCKED");
            return;
        }

        if (Instance == null) return;

        // Chọn Prefab: nếu chí mạng / kết liễu thì dùng critDamagePrefab, ngược lại dùng normalDamagePrefab
        GameObject prefabToSpawn = (isCrit || isLethal) && Instance.critDamagePrefab != null
            ? Instance.critDamagePrefab
            : Instance.normalDamagePrefab;

        SpawnPopup(prefabToSpawn, worldPos, $"-{amount}");
    }

    /// <summary>
    /// Hiển thị hồi máu
    /// </summary>
    public static void ShowHeal(Vector3 worldPos, int amount)
    {
        if (amount <= 0 || Instance == null) return;

        SpawnPopup(Instance.healPrefab, worldPos, $"+{amount}");
    }

    /// <summary>
    /// Hiển thị text trạng thái (Def, Shield, Miss, Buff...)
    /// </summary>
    public static void ShowText(Vector3 worldPos, string text, Color color = default, float size = 0f)
    {
        if (Instance == null) return;

        SpawnPopup(Instance.statusTextPrefab, worldPos, text);
    }

    private static void SpawnPopup(GameObject prefab, Vector3 worldPos, string content)
    {
        if (prefab == null) return;

        // Thêm độ lệch nhẹ ngẫu nhiên để nhiều số dame không đè khít lên nhau
        Vector3 spawnPos = worldPos + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.1f, 0.15f), 0f);
        GameObject popup = Instantiate(prefab, spawnPos, Quaternion.identity);

        TMP_Text tmp = popup.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = content;
        }

        // Chạy hiệu ứng nảy số, bay lên và mờ dần biến mất
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

        // Vận tốc nảy ngẫu nhiên (hơi lệch trái/phải và bay vút lên)
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

            // 1. Bay lên & chậm dần (Deceleration)
            popupTransform.position += velocity * Time.deltaTime;
            velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * 3.5f);

            // 2. Hiệu ứng Punch Scale: Nảy to cực nhanh lúc đầu (t < 0.2s) rồi đàn hồi về bình thường
            if (t < 0.2f)
            {
                float punchT = t / 0.2f;
                // Phóng to lên 1.35x rồi co lại
                float scaleMultiplier = Mathf.Lerp(0.5f, 1.35f, punchT);
                popupTransform.localScale = originalScale * scaleMultiplier;
            }
            else if (t < 0.35f)
            {
                float settleT = (t - 0.2f) / 0.15f;
                // Đàn hồi từ 1.35x về 1.0x
                float scaleMultiplier = Mathf.Lerp(1.35f, 1.0f, settleT);
                popupTransform.localScale = originalScale * scaleMultiplier;
            }

            // 3. Mờ dần biến mất (Fade out ở nửa sau animation)
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
