using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI for the Dungeon Safe Entrance / Antechamber room.
/// Uses the scene-authored DungeonEntranceArrowCanvas in Dungeon.unity.
/// Displays an animated fantasy Right Arrow button on the right edge of the screen,
/// allowing the player to prepare before stepping into Stage 1.
/// Supports both mouse clicks and keyboard shortcuts (Space / Enter / RightArrow / D).
/// </summary>
public class DungeonEntranceArrowUI : MonoBehaviour
{
    public static DungeonEntranceArrowUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] public GameObject container;
    [SerializeField] public Button arrowButton;
    [SerializeField] public RectTransform arrowIconRT;
    [SerializeField] public TextMeshProUGUI titleText;
    [SerializeField] public TextMeshProUGUI hintText;
    [SerializeField] public TextMeshProUGUI arrowTMP;

    private System.Action onProceedCallback;
    private bool isProceeding = false;
    private Vector2 basePosition = new Vector2(-28f, 0f);
    private RectTransform containerRT;

    public static DungeonEntranceArrowUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        Instance = FindFirstObjectByType<DungeonEntranceArrowUI>(FindObjectsInactive.Include);
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

        if (container != null)
        {
            containerRT = container.GetComponent<RectTransform>();
        }

        if (arrowButton != null && arrowButton.onClick.GetPersistentEventCount() == 0)
        {
            arrowButton.onClick.AddListener(OnArrowClicked);
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
