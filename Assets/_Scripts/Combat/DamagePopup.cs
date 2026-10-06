using System.Collections;
using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private Color textColor;
    private float lifetime = 0.85f;
    private Vector3 moveVector;
    private Vector3 baseScale;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMeshPro>();
        }
    }

    public static DamagePopup Create(Vector3 position, string text, Color color, float size = 5f, bool isCrit = false)
    {
        GameObject go = new GameObject("DamagePopup", typeof(TextMeshPro));
        // Slight random horizontal offset to prevent stacking popups directly on top of each other
        Vector3 spawnPos = position + new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.1f, 0.15f), 0f);
        go.transform.position = spawnPos;

        DamagePopup popup = go.AddComponent<DamagePopup>();
        popup.Init(text, color, size, isCrit);
        return popup;
    }

    public void Init(string text, Color color, float size, bool isCrit)
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();

        TMP_FontAsset font = DungeonUIAssetHelper.GetFontAsset();
        if (font != null) textMesh.font = font;

        textMesh.text = text;
        textMesh.fontSize = isCrit ? size * 1.3f : size;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.color = color;
        textColor = color;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        textMesh.sortingOrder = 1000;

        float randomX = Random.Range(-0.35f, 0.35f);
        float upwardY = Random.Range(1.8f, 2.3f);
        moveVector = new Vector3(randomX, upwardY, 0f);

        baseScale = isCrit ? Vector3.one * 1.2f : Vector3.one;
        transform.localScale = baseScale * 0.4f;

        StartCoroutine(AnimateRoutine(isCrit));
    }

    private IEnumerator AnimateRoutine(bool isCrit)
    {
        float elapsed = 0f;

        // 1. Pop & Bounce scale up
        float popDuration = 0.12f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float punch = isCrit ? Mathf.Lerp(0.4f, 1.4f, t) : Mathf.Lerp(0.4f, 1.25f, t);
            transform.localScale = baseScale * punch;
            transform.position += moveVector * Time.deltaTime;
            yield return null;
        }

        // 2. Settle scale back to baseline
        float settleDuration = 0.08f;
        float settleElapsed = 0f;
        while (settleElapsed < settleDuration)
        {
            settleElapsed += Time.deltaTime;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(settleElapsed / settleDuration);
            float punch = isCrit ? Mathf.Lerp(1.4f, 1.15f, t) : Mathf.Lerp(1.25f, 1.0f, t);
            transform.localScale = baseScale * punch;
            transform.position += moveVector * Time.deltaTime;
            yield return null;
        }

        // 3. Float upward with deceleration and fade out
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            moveVector -= moveVector * (2.8f * Time.deltaTime);
            transform.position += moveVector * Time.deltaTime;

            float fadeStart = lifetime * 0.45f;
            if (elapsed > fadeStart)
            {
                float fadeT = (elapsed - fadeStart) / (lifetime - fadeStart);
                float alpha = Mathf.Clamp01(1f - fadeT);
                textMesh.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
