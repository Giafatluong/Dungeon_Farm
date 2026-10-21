using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        // Assign containerRT so bounce animation in Update() works
        if (container != null)
        {
            containerRT = container.GetComponent<RectTransform>();
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

        BuildUIIfNeeded();

        if (titleText != null)
        {
            titleText.text = "PROCEED";
        }

        if (hintText != null)
        {
            hintText.text = totalStages > 0 ? $"Enter Stage 1/{totalStages} [->]" : "Enter Stage 1 [Space / ->]";
        }

        if (container != null)
        {
            if (containerRT == null) containerRT = container.GetComponent<RectTransform>();
            container.SetActive(true);
            container.transform.SetAsLastSibling();
        }
    }

    private void BuildUIIfNeeded()
    {
        if (container != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        }
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas != null && transform.parent != canvas.transform)
        {
            transform.SetParent(canvas.transform, false);
        }

        // 1. Container Object with Button
        container = new GameObject("EntranceArrowContainer", typeof(RectTransform), typeof(Image), typeof(Button));
        container.transform.SetParent(transform, false);
        containerRT = container.GetComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(1f, 0.5f);
        containerRT.anchorMax = new Vector2(1f, 0.5f);
        containerRT.pivot = new Vector2(1f, 0.5f);
        containerRT.sizeDelta = new Vector2(210f, 85f);
        containerRT.anchoredPosition = basePosition;

        Image bg = container.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.12f, 0.16f, 0.24f, 0.95f));

        arrowButton = container.GetComponent<Button>();
        arrowButton.targetGraphic = bg;
        arrowButton.onClick.AddListener(OnArrowClicked);

        // 2. Arrow Icon / TMP
        GameObject arrowGO = new GameObject("ArrowTMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGO.transform.SetParent(container.transform, false);
        RectTransform arrowRT = arrowGO.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1f, 0.5f);
        arrowRT.anchorMax = new Vector2(1f, 0.5f);
        arrowRT.pivot = new Vector2(1f, 0.5f);
        arrowRT.sizeDelta = new Vector2(50f, 50f);
        arrowRT.anchoredPosition = new Vector2(-12f, 0f);

        arrowTMP = arrowGO.GetComponent<TextMeshProUGUI>();
        arrowTMP.text = "->";
        arrowTMP.fontSize = 32f;
        arrowTMP.alignment = TextAlignmentOptions.Center;
        arrowTMP.color = new Color(1f, 0.85f, 0.25f);
        arrowIconRT = arrowRT;

        // 3. Title Text
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(container.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.5f);
        titleRT.anchorMax = new Vector2(0f, 0.5f);
        titleRT.pivot = new Vector2(0f, 0.5f);
        titleRT.anchoredPosition = new Vector2(16f, 14f);
        titleRT.sizeDelta = new Vector2(130f, 30f);

        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        titleText.text = "PROCEED";
        titleText.fontSize = 20f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;

        // 4. Hint Text
        GameObject hintGO = new GameObject("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintGO.transform.SetParent(container.transform, false);
        RectTransform hintRT = hintGO.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0f, 0.5f);
        hintRT.anchorMax = new Vector2(0f, 0.5f);
        hintRT.pivot = new Vector2(0f, 0.5f);
        hintRT.anchoredPosition = new Vector2(16f, -14f);
        hintRT.sizeDelta = new Vector2(130f, 24f);

        hintText = hintGO.GetComponent<TextMeshProUGUI>();
        hintText.text = "Enter Stage 1 [Space / ->]";
        hintText.fontSize = 12f;
        hintText.color = new Color(0.75f, 0.85f, 1f);
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

}
