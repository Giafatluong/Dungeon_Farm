using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// In-world interactive Event Altar / Shrine spawned during Dungeon Event Stages.
/// Instead of automatically popping up the event modal, the player must approach
/// or interact with this station (via [E], [Space], [Enter], or mouse click) to activate the event.
/// This allows the player to inspect their stats, manage hunger, and view their backpack before deciding to interact.
/// </summary>
[DisallowMultipleComponent]
public class DungeonEventInteractable : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
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
        if (promptCanvasGroup != null) promptCanvasGroup.alpha = 0f;
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

        // 2. Click Collider (Generous size covering full altar sprite and immediate vicinity)
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(2.8f, 3.2f);
        boxCollider.offset = new Vector2(0f, 0.4f);

        // 3. Floating Prompt Canvas
        BuildPromptUI();
    }

    public void BuildPromptUI()
    {
        if (promptCanvas != null)
        {
            promptCanvas.sortingOrder = 150;
            return;
        }

        // Check if an EventPromptCanvas already exists in scene
        promptCanvas = GetComponentInChildren<Canvas>(true);
        if (promptCanvas == null)
        {
            GameObject existingCanvasGO = GameObject.Find("EventPromptCanvas");
            if (existingCanvasGO != null) promptCanvas = existingCanvasGO.GetComponent<Canvas>();
        }

        if (promptCanvas == null)
        {
            Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == "EventPromptCanvas")
                {
                    promptCanvas = all[i];
                    break;
                }
            }
        }

        if (promptCanvas != null)
        {
            promptCanvas.sortingOrder = 150;
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
                    UIButtonHoverGlow.Ensure(promptClickButton);
                }
                return;
            }
        }

        Debug.LogWarning("[DungeonEventInteractable] EventPromptCanvas not found in scene!");
    }

    public void Setup(DungeonEvent evt, PlayerStats player, ItemContainer backpack, System.Action onComplete)
    {
        currentEvent = evt;
        playerStats = player;
        backpackContainer = backpack;
        onCompleteCallback = onComplete;
        isAwaitingInteraction = true;
        isHovered = false;

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
            promptCanvas.sortingOrder = 150;
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
        HandleMouseInteraction();

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

    private void HandleMouseInteraction()
    {
        if (!isAwaitingInteraction) return;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 mousePos = Input.mousePosition;
        Vector3 mouseWorld3D = mainCam.ScreenToWorldPoint(mousePos);
        Vector2 mouseWorld = new Vector2(mouseWorld3D.x, mouseWorld3D.y);

        bool overCollider = boxCollider != null && boxCollider.OverlapPoint(mouseWorld);
        bool overPrompt = promptPanelRect != null && promptCanvas != null && promptCanvas.gameObject.activeInHierarchy &&
                          RectTransformUtility.RectangleContainsScreenPoint(promptPanelRect, mousePos, null);

        // Update hover state dynamically
        isHovered = overCollider || overPrompt;

        // Direct mouse click check (works reliably in all scenes regardless of raycaster config)
        if (Input.GetMouseButtonDown(0))
        {
            if (UIEscapeManager.IsAnyModalUIOpen(includeInventory: true, includeMainMenu: true))
                return;

            if (CombatUI.Instance != null && CombatUI.Instance.foodPanel != null && CombatUI.Instance.foodPanel.activeSelf)
                return;

            if (overCollider || overPrompt)
            {
                TriggerInteraction();
            }
        }
    }

    private void AnimateVisuals()
    {
        // 1. Altar subtle mystical float & pulse
        float bob = Mathf.Sin(Time.time * 3f) * 0.05f;
        transform.position = new Vector3(baseWorldPosition.x, baseWorldPosition.y + bob, baseWorldPosition.z);

        Vector3 targetScale = isHovered ? new Vector3(1.12f, 1.12f, 1f) : Vector3.one;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 12f);

        if (altarRenderer != null)
        {
            float pulse = (Mathf.Sin(Time.time * 2.5f) + 1f) * 0.5f;
            Color baseColor = Color.Lerp(new Color(0.92f, 0.85f, 1f), Color.white, pulse);
            if (isHovered)
            {
                // Radiant golden glow on hover to clearly indicate interactability
                altarRenderer.color = Color.Lerp(baseColor, new Color(1f, 0.95f, 0.5f, 1f), 0.7f);
            }
            else
            {
                altarRenderer.color = baseColor;
            }
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
                    promptPanelRect.anchoredPosition = new Vector2(screenPos.x / scale, (screenPos.y + promptBob) / scale);
                }
            }
        }
    }

    #region Unity EventSystem Interfaces (For Physics2DRaycaster & EventSystem dispatch)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isAwaitingInteraction) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (UIEscapeManager.IsAnyModalUIOpen(includeInventory: true, includeMainMenu: true))
            return;

        if (CombatUI.Instance != null && CombatUI.Instance.foodPanel != null && CombatUI.Instance.foodPanel.activeSelf)
            return;

        TriggerInteraction();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isAwaitingInteraction) return;
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
    #endregion

    #region Legacy Monobehaviour Mouse Callbacks
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

        if (UIEscapeManager.IsAnyModalUIOpen(includeInventory: true, includeMainMenu: true))
            return;

        if (CombatUI.Instance != null && CombatUI.Instance.foodPanel != null && CombatUI.Instance.foodPanel.activeSelf)
            return;

        TriggerInteraction();
    }
    #endregion

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

        if (promptCanvas != null)
        {
            promptCanvas.gameObject.SetActive(false);
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

        if (promptCanvas != null)
        {
            promptCanvas.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }
}
