using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Slay the Spire-style floating Tooltip UI for Buff/Debuff icons.
/// Shows icon, title, category badge, duration pill, and rich description text on hover.
/// </summary>
public class BuffTooltipUI : MonoBehaviour
{
    public static BuffTooltipUI Instance { get; private set; }

    private GameObject rootGO;
    private RectTransform rootRT;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;

    private Image iconImage;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI categoryBadgeText;
    private TextMeshProUGUI durationText;
    private TextMeshProUGUI descText;

    private bool isVisible = false;
    private Vector2 targetScreenPos;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    public static BuffTooltipUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        Canvas mainCanvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        if (mainCanvas == null) mainCanvas = Object.FindFirstObjectByType<Canvas>();

        GameObject go = new GameObject("[BuffTooltipUI]");
        if (mainCanvas != null)
        {
            go.transform.SetParent(mainCanvas.transform, false);
        }
        else
        {
            DontDestroyOnLoad(go);
        }

        Instance = go.AddComponent<BuffTooltipUI>();
        Instance.ConstructUI();
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
        ConstructUI();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Hide();
    }

    private void ConstructUI()
    {
        if (rootGO != null) return;

        // Container card
        rootGO = new GameObject("BuffTooltipCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        rootGO.transform.SetParent(transform, false);

        rootRT = rootGO.GetComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(250, 110);
        rootRT.pivot = new Vector2(0f, 0f);

        canvasGroup = rootGO.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        Image bg = rootGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.08f, 0.10f, 0.16f, 0.96f));

        // Outline border effect
        Outline outline = rootGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.35f, 0.55f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Header Row: Icon (28x28) + Title & Category
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconGO.transform.SetParent(rootGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 1);
        iconRT.anchorMax = new Vector2(0, 1);
        iconRT.pivot = new Vector2(0, 1);
        iconRT.anchoredPosition = new Vector2(12, -12);
        iconRT.sizeDelta = new Vector2(28, 28);
        iconImage = iconGO.GetComponent<Image>();
        iconImage.preserveAspect = true;

        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(rootGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0, 1);
        titleRT.anchoredPosition = new Vector2(48, -10);
        titleRT.sizeDelta = new Vector2(-58, 22);

        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 14;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 11f;
        titleText.fontSizeMax = 14f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;

        // Category Badge & Duration Row
        GameObject subRowGO = new GameObject("SubRow", typeof(RectTransform), typeof(TextMeshProUGUI));
        subRowGO.transform.SetParent(rootGO.transform, false);
        RectTransform subRT = subRowGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 1);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.pivot = new Vector2(0, 1);
        subRT.anchoredPosition = new Vector2(48, -32);
        subRT.sizeDelta = new Vector2(-58, 18);

        durationText = subRowGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(durationText);
        durationText.fontSize = 11;
        durationText.color = new Color(0.75f, 0.85f, 1f, 0.95f);

        // Divider
        GameObject divGO = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divGO.transform.SetParent(rootGO.transform, false);
        RectTransform divRT = divGO.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0, 1);
        divRT.anchorMax = new Vector2(1, 1);
        divRT.pivot = new Vector2(0.5f, 1);
        divRT.anchoredPosition = new Vector2(0, -52);
        divRT.sizeDelta = new Vector2(-24, 1.5f);
        Image divImg = divGO.GetComponent<Image>();
        divImg.color = new Color(0.3f, 0.4f, 0.55f, 0.5f);

        // Description Body
        GameObject descGO = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGO.transform.SetParent(rootGO.transform, false);
        RectTransform descRT = descGO.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0, 0);
        descRT.anchorMax = new Vector2(1, 1);
        descRT.pivot = new Vector2(0, 1);
        descRT.anchoredPosition = new Vector2(12, -58);
        descRT.sizeDelta = new Vector2(-24, -68);

        descText = descGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(descText);
        descText.fontSize = 12;
        descText.enableAutoSizing = true;
        descText.fontSizeMin = 9.5f;
        descText.fontSizeMax = 12f;
        descText.color = new Color(0.85f, 0.90f, 0.95f, 1f);
        descText.textWrappingMode = TextWrappingModes.Normal;

        rootGO.SetActive(false);
    }

    public void Show(BuffDisplayInfo info, Vector3 worldPosition)
    {
        if (info == null) return;
        EnsureInstance();
        ConstructUI();

        if (iconImage != null)
        {
            iconImage.sprite = info.icon;
            iconImage.enabled = (info.icon != null);
        }

        if (titleText != null)
        {
            titleText.text = info.title;
        }

        if (durationText != null)
        {
            string cat = !string.IsNullOrEmpty(info.category) ? $"<color=#{ColorUtility.ToHtmlStringRGB(info.categoryColor)}><b>{info.category}</b></color>  " : "";
            durationText.text = $"{cat}{info.durationText}";
        }

        if (descText != null)
        {
            descText.text = info.description;
        }

        // Auto-size card based on description length
        float cardHeight = Mathf.Clamp(100f + (info.description.Length / 28f) * 16f, 105f, 180f);
        rootRT.sizeDelta = new Vector2(250, cardHeight);

        targetScreenPos = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPosition) : (Vector2)Input.mousePosition;

        isVisible = true;
        if (rootGO != null) rootGO.SetActive(true);
        UpdatePosition();
    }

    public void Hide()
    {
        isVisible = false;
        if (rootGO != null) rootGO.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        if (!isVisible || rootGO == null) return;

        targetScreenPos = Input.mousePosition;
        UpdatePosition();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.unscaledDeltaTime * 12f);
        }
    }

    private void UpdatePosition()
    {
        if (rootRT == null) return;

        // Position slightly offset from mouse/target
        Vector2 pos = targetScreenPos + new Vector2(16f, 16f);

        // Screen edge clamp
        float cardW = rootRT.rect.width;
        float cardH = rootRT.rect.height;

        if (pos.x + cardW > Screen.width - 12f)
        {
            pos.x = targetScreenPos.x - cardW - 16f;
        }
        if (pos.y + cardH > Screen.height - 12f)
        {
            pos.y = targetScreenPos.y - cardH - 16f;
        }
        if (pos.x < 12f) pos.x = 12f;
        if (pos.y < 12f) pos.y = 12f;

        rootRT.position = pos;
    }
}
