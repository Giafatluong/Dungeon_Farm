using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Individual Buff/Debuff icon widget on a health bar.
/// Displays an illustrated icon and bottom-right stack/turn badge.
/// Shows rich Slay the Spire-style tooltip on mouse hover.
/// </summary>
public class BuffIconWidget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image iconImage;
    private TextMeshProUGUI badgeText;
    private BuffDisplayInfo currentInfo;

    private bool isHovered = false;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        EnsureComponents();
    }

    private void EnsureComponents()
    {
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
            if (iconImage == null) iconImage = gameObject.AddComponent<Image>();
        }
        if (iconImage != null)
        {
            iconImage.preserveAspect = true;
        }

        if (badgeText == null)
        {
            Transform existing = transform.Find("BadgeText");
            if (existing != null)
            {
                badgeText = existing.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject badgeGO = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
                badgeGO.transform.SetParent(transform, false);
                badgeText = badgeGO.GetComponent<TextMeshProUGUI>();
            }
        }

        if (badgeText != null)
        {
            RectTransform rt = badgeText.rectTransform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(3f, -2f);
            rt.sizeDelta = new Vector2(26f, 16f);

            DungeonUIAssetHelper.ApplyFont(badgeText, withOutline: true);
            badgeText.fontSize = 11;
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.alignment = TextAlignmentOptions.BottomRight;
            badgeText.textWrappingMode = TextWrappingModes.NoWrap;
            badgeText.overflowMode = TextOverflowModes.Overflow;
            badgeText.color = new Color(1.0f, 0.95f, 0.4f, 1f); // Golden amber
            badgeText.raycastTarget = false;
        }
    }

    public void Setup(BuffDisplayInfo info)
    {
        EnsureComponents();
        currentInfo = info;

        if (iconImage != null && info != null)
        {
            iconImage.sprite = info.icon;
            iconImage.enabled = (info.icon != null);
        }

        if (badgeText != null)
        {
            if (info != null && !string.IsNullOrEmpty(info.stackBadge))
            {
                badgeText.gameObject.SetActive(true);
                badgeText.text = info.stackBadge;
            }
            else
            {
                badgeText.gameObject.SetActive(false);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (currentInfo != null)
        {
            BuffTooltipUI.EnsureInstance()?.Show(currentInfo, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        BuffTooltipUI.Instance?.Hide();
    }

    private void OnDisable()
    {
        if (isHovered)
        {
            isHovered = false;
            BuffTooltipUI.Instance?.Hide();
        }
        transform.localScale = baseScale;
    }

    private void Update()
    {
        Vector3 targetScale = isHovered ? baseScale * 1.18f : baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 14f);
    }
}

