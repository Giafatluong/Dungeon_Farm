using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI for the Dungeon Safe Entrance / Antechamber room.
/// Displays an animated fantasy Right Arrow button on the right edge of the screen,
/// allowing the player to prepare before stepping into Stage 1.
/// Supports both mouse clicks and keyboard shortcuts (Space / Enter / RightArrow / D).
/// </summary>
public class DungeonEntranceArrowUI : MonoBehaviour
{
    public static DungeonEntranceArrowUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject container;
    [SerializeField] private Button arrowButton;
    [SerializeField] private RectTransform arrowIconRT;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI arrowTMP;

    private System.Action onProceedCallback;
    private bool isProceeding = false;
    private Vector2 basePosition = new Vector2(-28f, 0f);
    private RectTransform containerRT;

    public static DungeonEntranceArrowUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        Instance = FindFirstObjectByType<DungeonEntranceArrowUI>(FindObjectsInactive.Include);
        if (Instance != null) return Instance;

        GameObject go = new GameObject("DungeonEntranceArrowUI_Manager", typeof(RectTransform), typeof(DungeonEntranceArrowUI));
        Instance = go.GetComponent<DungeonEntranceArrowUI>();

        Canvas canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        if (canvas != null)
        {
            go.transform.SetParent(canvas.transform, false);
        }

        Instance.BuildUI();
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

        if (container == null)
        {
            BuildUI();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (container == null || !container.activeSelf || isProceeding)
            return;

        // 1. Idle breathing / bounce animation to beckon the player
        float offsetX = Mathf.Sin(Time.time * 4.5f) * 6f;
        if (containerRT != null)
        {
            containerRT.anchoredPosition = new Vector2(basePosition.x + offsetX, basePosition.y);
        }

        if (arrowTMP != null)
        {
            float alpha = 0.82f + Mathf.PingPong(Time.time * 2f, 0.18f);
            Color c = arrowTMP.color;
            c.a = alpha;
            arrowTMP.color = c;
        }

        // 2. Keyboard shortcuts to quickly advance
        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.RightArrow) ||
            Input.GetKeyDown(KeyCode.D))
        {
            OnArrowClicked();
        }
    }

    public void Show(int floorNumber, int totalStages, System.Action onProceed)
    {
        onProceedCallback = onProceed;
        isProceeding = false;

        if (container == null)
        {
            BuildUI();
        }

        if (titleText != null)
        {
            titleText.text = "TIẾN LÊN";
        }

        if (hintText != null)
        {
            hintText.text = totalStages > 0 ? $"Vào Ải 1/{totalStages} [➔]" : "Vào Ải 1 [Space / ➔]";
        }

        if (container != null)
        {
            container.SetActive(true);
            container.transform.SetAsLastSibling();
        }
    }

    public void Hide()
    {
        if (container != null)
        {
            container.SetActive(false);
        }
        isProceeding = false;
        onProceedCallback = null;
    }

    public void OnArrowClicked()
    {
        if (isProceeding) return;
        isProceeding = true;

        if (container != null)
        {
            container.SetActive(false);
        }

        System.Action cb = onProceedCallback;
        onProceedCallback = null;
        cb?.Invoke();
    }

    private void BuildUI()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null || parentCanvas.renderMode == RenderMode.WorldSpace)
        {
            parentCanvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        }
        if (parentCanvas == null) return;

        // Container / Button Object on the right edge
        GameObject btnGO = new GameObject("RightArrowProceedBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        btnGO.transform.SetParent(parentCanvas.transform, false);
        container = btnGO;
        containerRT = btnGO.GetComponent<RectTransform>();

        containerRT.anchorMin = new Vector2(1f, 0.5f);
        containerRT.anchorMax = new Vector2(1f, 0.5f);
        containerRT.pivot = new Vector2(1f, 0.5f);
        containerRT.sizeDelta = new Vector2(195f, 78f);
        containerRT.anchoredPosition = basePosition;

        Image bgImg = btnGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bgImg, new Color(0.10f, 0.13f, 0.19f, 0.96f));

        Outline outline = btnGO.GetComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.68f, 0.22f, 0.75f);
        outline.effectDistance = new Vector2(2f, -2f);

        arrowButton = btnGO.GetComponent<Button>();
        ColorBlock cb = arrowButton.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.05f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        arrowButton.colors = cb;
        arrowButton.onClick.RemoveAllListeners();
        arrowButton.onClick.AddListener(OnArrowClicked);

        // Left Text Block
        GameObject textBlockGO = new GameObject("TextBlock", typeof(RectTransform));
        textBlockGO.transform.SetParent(btnGO.transform, false);
        RectTransform tbRT = textBlockGO.GetComponent<RectTransform>();
        tbRT.anchorMin = new Vector2(0f, 0f);
        tbRT.anchorMax = new Vector2(0.68f, 1f);
        tbRT.offsetMin = new Vector2(12f, 8f);
        tbRT.offsetMax = new Vector2(-4f, -8f);

        // Title: "TIẾN LÊN"
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(textBlockGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.45f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.sizeDelta = Vector2.zero;
        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 15;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = new Color(1f, 0.88f, 0.35f);
        titleText.text = "TIẾN LÊN";

        // Subtitle: "[Space / ➔]"
        GameObject hintGO = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintGO.transform.SetParent(textBlockGO.transform, false);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0f, 0f);
        hintRT.anchorMax = new Vector2(1f, 0.45f);
        hintRT.sizeDelta = Vector2.zero;
        hintText = hintGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(hintText);
        hintText.fontSize = 11;
        hintText.alignment = TextAlignmentOptions.MidlineLeft;
        hintText.color = new Color(0.72f, 0.84f, 0.98f, 0.9f);
        hintText.text = "Vào Ải 1 [➔]";

        // Right Arrow Block
        GameObject arrowGO = new GameObject("ArrowIcon", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGO.transform.SetParent(btnGO.transform, false);
        arrowIconRT = arrowGO.GetComponent<RectTransform>();
        arrowIconRT.anchorMin = new Vector2(0.68f, 0f);
        arrowIconRT.anchorMax = new Vector2(1f, 1f);
        arrowIconRT.offsetMin = new Vector2(0f, 4f);
        arrowIconRT.offsetMax = new Vector2(-10f, -4f);

        arrowTMP = arrowGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(arrowTMP);
        arrowTMP.fontSize = 28;
        arrowTMP.fontStyle = FontStyles.Bold;
        arrowTMP.alignment = TextAlignmentOptions.Center;
        arrowTMP.color = new Color(1f, 0.92f, 0.45f);
        arrowTMP.text = "➔";

        container.SetActive(false);
    }
}
