using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-world interactive Event Altar / Shrine spawned during Dungeon Event Stages.
/// Instead of automatically popping up the event modal, the player must approach
/// or interact with this station (via [E], [Space], [Enter], or mouse click) to activate the event.
/// This allows the player to inspect their stats, manage hunger, and view their backpack before deciding to interact.
/// </summary>
[DisallowMultipleComponent]
public class DungeonEventInteractable : MonoBehaviour
{
    public static DungeonEventInteractable Instance { get; private set; }

    [Header("Event Data & Context")]
    private DungeonEvent currentEvent;
    private PlayerStats playerStats;
    private ItemContainer backpackContainer;
    private System.Action onCompleteCallback;

    private bool isAwaitingInteraction = false;
    private bool isHovered = false;

    [Header("Visual Components")]
    [SerializeField] private SpriteRenderer altarRenderer;
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private Canvas promptCanvas;
    [SerializeField] private RectTransform promptPanelRect;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private TextMeshProUGUI promptTitleText;
    [SerializeField] private TextMeshProUGUI promptHintText;
    [SerializeField] private Button promptClickButton;

    private Vector3 baseWorldPosition = new Vector3(1.8f, -0.06f, 0f);
    private Camera mainCam;

    public static DungeonEventInteractable EnsureInstance()
    {
        if (Instance != null) return Instance;

        Instance = FindFirstObjectByType<DungeonEventInteractable>(FindObjectsInactive.Include);
        if (Instance != null) return Instance;

        GameObject go = new GameObject("DungeonEventStation");
        Instance = go.AddComponent<DungeonEventInteractable>();
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

        EnsureVisualComponents();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (promptCanvas != null) Destroy(promptCanvas.gameObject);
    }

    private void EnsureVisualComponents()
    {
        // 1. Altar SpriteRenderer
        if (altarRenderer == null)
        {
            altarRenderer = GetComponent<SpriteRenderer>();
            if (altarRenderer == null) altarRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        altarRenderer.sortingOrder = 20;

        // Try to load thematic fantasy shrine/altar sprites
        if (altarRenderer.sprite == null)
        {
            altarRenderer.sprite = DungeonUIAssetHelper.GetAltarSprite();
        }

        // 2. Click Collider
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(2.4f, 2.8f);
        boxCollider.offset = new Vector2(0f, 0.4f);

        // 3. Floating Prompt Canvas
        BuildPromptUI();
    }

    private void BuildPromptUI()
    {
        if (promptCanvas != null) return;

        // Check if an EventPromptCanvas already exists in children
        promptCanvas = GetComponentInChildren<Canvas>(true);
        if (promptCanvas != null)
        {
            Transform panelT = promptCanvas.transform.Find("PromptPanel");
            if (panelT != null)
            {
                promptPanelRect = panelT.GetComponent<RectTransform>();
                promptCanvasGroup = panelT.GetComponent<CanvasGroup>();
                promptTitleText = panelT.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                promptHintText = panelT.Find("HintText")?.GetComponent<TextMeshProUGUI>();
                promptClickButton = panelT.GetComponent<Button>();
                if (promptClickButton != null)
                {
                    promptClickButton.onClick.RemoveListener(TriggerInteraction);
                    promptClickButton.onClick.AddListener(TriggerInteraction);
                }
                return;
            }
        }

        GameObject canvasGO = new GameObject("EventPromptCanvas");
        canvasGO.transform.SetParent(transform, false);
        promptCanvas = canvasGO.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        promptCanvas.sortingOrder = 35; // Above dungeon hud, below top-level modals

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Prompt Panel - anchors must be at bottom-left (0,0) so WorldToScreenPoint directly maps to anchoredPosition!
        GameObject panelGO = new GameObject("PromptPanel", typeof(RectTransform));
        panelGO.transform.SetParent(canvasGO.transform, false);
        promptPanelRect = panelGO.GetComponent<RectTransform>();
        promptPanelRect.anchorMin = Vector2.zero;
        promptPanelRect.anchorMax = Vector2.zero;
        promptPanelRect.sizeDelta = new Vector2(340f, 74f);
        promptPanelRect.pivot = new Vector2(0.5f, 0f);

        promptCanvasGroup = panelGO.AddComponent<CanvasGroup>();
        promptCanvasGroup.alpha = 0f;

        Image bgImage = panelGO.AddComponent<Image>();
        Sprite frameSprite = DungeonUIAssetHelper.GetFrameSprite();
        if (frameSprite != null)
        {
            bgImage.sprite = frameSprite;
            bgImage.type = Image.Type.Sliced;
            bgImage.color = new Color(0.12f, 0.10f, 0.15f, 0.94f);
        }
        else
        {
            bgImage.color = new Color(0.14f, 0.11f, 0.18f, 0.95f);
        }

        Outline outline = panelGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.65f, 1.0f, 0.85f); // Mystical violet highlight
        outline.effectDistance = new Vector2(2f, -2f);

        promptClickButton = panelGO.AddComponent<Button>();
        promptClickButton.targetGraphic = bgImage;
        promptClickButton.onClick.AddListener(TriggerInteraction);

        // Content Layout
        VerticalLayoutGroup vLayout = panelGO.AddComponent<VerticalLayoutGroup>();
        vLayout.padding = new RectOffset(14, 14, 8, 8);
        vLayout.spacing = 3f;
        vLayout.childAlignment = TextAnchor.MiddleCenter;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        // Title row with [E] badge
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform));
        titleGO.transform.SetParent(panelGO.transform, false);
        promptTitleText = titleGO.AddComponent<TextMeshProUGUI>();
        promptTitleText.text = "<b><color=#FFDF00>[E]</color> TƯƠNG TÁC SỰ KIỆN</b>";
        promptTitleText.fontSize = 20f;
        promptTitleText.alignment = TextAlignmentOptions.Center;
        promptTitleText.color = Color.white;
        promptTitleText.raycastTarget = false;
        DungeonUIAssetHelper.ApplyFont(promptTitleText, withOutline: true);

        // Subtitle / Hint
        GameObject hintGO = new GameObject("HintText", typeof(RectTransform));
        hintGO.transform.SetParent(panelGO.transform, false);
        promptHintText = hintGO.AddComponent<TextMeshProUGUI>();
        promptHintText.text = "<size=13><color=#D5C9E5>Bấm <b>[E]</b> hoặc <b>nhấp chuột</b> để kích hoạt</color></size>";
        promptHintText.fontSize = 14f;
        promptHintText.alignment = TextAlignmentOptions.Center;
        promptHintText.color = new Color(0.85f, 0.82f, 0.92f, 1f);
        promptHintText.raycastTarget = false;
        DungeonUIAssetHelper.ApplyFont(promptHintText, withOutline: false);
    }

    public void Setup(DungeonEvent evt, PlayerStats player, ItemContainer backpack, System.Action onComplete)
    {
        currentEvent = evt;
        playerStats = player;
        backpackContainer = backpack;
        onCompleteCallback = onComplete;
        isAwaitingInteraction = true;

        EnsureVisualComponents();

        // Position Altar at Position_1 or center of combat positions
        Transform pos1 = GameObject.Find("Position_1")?.transform;
        if (pos1 != null)
        {
            baseWorldPosition = pos1.position;
        }
        else
        {
            baseWorldPosition = new Vector3(1.8f, -0.06f, 0f);
        }

        transform.position = baseWorldPosition;
        transform.localScale = Vector3.one;
        gameObject.SetActive(true);

        if (altarRenderer != null)
        {
            altarRenderer.enabled = true;
            if (altarRenderer.sprite == null)
            {
                altarRenderer.sprite = DungeonUIAssetHelper.GetAltarSprite();
            }
            altarRenderer.color = Color.white;
            altarRenderer.sortingOrder = 20;
        }

        if (promptCanvas != null)
        {
            promptCanvas.gameObject.SetActive(true);
        }

        if (promptTitleText != null)
        {
            string title = evt != null ? evt.eventTitle : "SỰ KIỆN BÍ ẨN";
            promptTitleText.text = $"<b><color=#FFDF00>[E]</color> {title.ToUpper()}</b>";
        }

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 1f;
            promptCanvasGroup.interactable = true;
            promptCanvasGroup.blocksRaycasts = true;
        }

        Debug.Log($"[DungeonEventInteractable] Event Station ready for '{evt?.eventTitle}' at {transform.position}. Awaiting player interaction.");
    }

    private void Update()
    {
        if (!isAwaitingInteraction) return;

        AnimateVisuals();

        // Ignore hotkey while any modal UI or bag/eating is open
        if (UIEscapeManager.IsAnyModalUIOpen(includeInventory: true, includeMainMenu: true))
            return;

        if (CombatUI.Instance != null && CombatUI.Instance.foodPanel != null && CombatUI.Instance.foodPanel.activeSelf)
            return;

        // Player presses [E], [Space], [Enter] to interact
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            TriggerInteraction();
        }
    }

    private void AnimateVisuals()
    {
        // 1. Altar subtle mystical float & pulse
        float bob = Mathf.Sin(Time.time * 3f) * 0.05f;
        transform.position = new Vector3(baseWorldPosition.x, baseWorldPosition.y + bob, baseWorldPosition.z);

        Vector3 targetScale = isHovered ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 10f);

        if (altarRenderer != null)
        {
            float pulse = (Mathf.Sin(Time.time * 2.5f) + 1f) * 0.5f;
            altarRenderer.color = Color.Lerp(new Color(0.92f, 0.85f, 1f), Color.white, pulse);
        }

        // 2. Position floating prompt above the Altar in screen space
        if (promptPanelRect != null && promptCanvas != null)
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 worldPromptPos = transform.position + new Vector3(0f, 2.3f, 0f);
                Vector3 screenPos = mainCam.WorldToScreenPoint(worldPromptPos);

                if (screenPos.z >= 0f)
                {
                    float scale = promptCanvas.scaleFactor > 0f ? promptCanvas.scaleFactor : 1f;
                    float promptBob = Mathf.Sin(Time.time * 4f) * 4f;
                    promptPanelRect.anchoredPosition = new Vector2(screenPos.x / scale, screenPos.y / scale + promptBob);
                }
            }
        }
    }

    private void OnMouseEnter()
    {
        if (!isAwaitingInteraction) return;
        isHovered = true;
    }

    private void OnMouseExit()
    {
        isHovered = false;
    }

    private void OnMouseDown()
    {
        if (!isAwaitingInteraction) return;

        // If pointer is over other blocking UI, ignore
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        TriggerInteraction();
    }

    public void TriggerInteraction()
    {
        if (!isAwaitingInteraction) return;

        isAwaitingInteraction = false;
        isHovered = false;

        Debug.Log($"[DungeonEventInteractable] Player interacted! Opening Event: {currentEvent?.eventTitle}");

        // Hide prompt banner while modal is open
        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.interactable = false;
            promptCanvasGroup.blocksRaycasts = false;
        }

        DungeonEventUI eventUI = DungeonEventUI.EnsureInstance();
        if (eventUI != null && currentEvent != null)
        {
            eventUI.OpenEvent(currentEvent, playerStats, backpackContainer, onComplete: () =>
            {
                Dismiss();
                onCompleteCallback?.Invoke();
            });
        }
        else
        {
            Dismiss();
            onCompleteCallback?.Invoke();
        }
    }

    public void Dismiss()
    {
        isAwaitingInteraction = false;
        isHovered = false;

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.interactable = false;
            promptCanvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }
}

