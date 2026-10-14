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

        // Gán containerRT để animation bounce trong Update() hoạt động
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

        if (titleText != null)
        {
            titleText.text = "TIẾN LÊN";
        }

        if (hintText != null)
        {
            hintText.text = totalStages > 0 ? $"Vào Ải 1/{totalStages} [->]" : "Vào Ải 1 [Space / ->]";
        }

        if (container != null)
        {
            if (containerRT == null) containerRT = container.GetComponent<RectTransform>();
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

}
