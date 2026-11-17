using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// In-world interactive Ancient Exit Teleport Gate / Monument spawned at the end of the final wave of a dungeon floor.
/// Instead of automatically popping up the victory modal right after defeating the boss/final wave,
/// the player moves forward a distance and encounters this glowing teleport gate in the chamber.
/// Interacting with this gate (via mouse click or [E] / [Space] / [Enter]) opens the Floor Victory dialog,
/// giving the player the choice to teleport out (Return to Farm Base) or proceed deeper into the Next Floor.
/// </summary>
[DisallowMultipleComponent]
public class DungeonExitInteractable : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public static DungeonExitInteractable Instance { get; private set; }

    [Header("Floor Context")]
    private FloorData currentFloor;
    private int currentFloorNumber = 1;
    private ItemContainer backpackContainer;
    private System.Action onInteractCallback;

    private bool isAwaitingInteraction = false;
    private bool isHovered = false;

    [Header("Visual Components")]
    [SerializeField] private SpriteRenderer exitRenderer;
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private Canvas promptCanvas;
    [SerializeField] private RectTransform promptPanelRect;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private TextMeshProUGUI promptTitleText;
    [SerializeField] private TextMeshProUGUI promptHintText;
    [SerializeField] private Button promptClickButton;

    private Vector3 baseWorldPosition = new Vector3(2.0f, -0.06f, 0f);
    private Camera mainCam;

    public static DungeonExitInteractable EnsureInstance()
    {
        if (Instance != null) return Instance;

        Instance = FindFirstObjectByType<DungeonExitInteractable>(FindObjectsInactive.Include);
        if (Instance != null) return Instance;

        GameObject go = new GameObject("DungeonExitPortal");
        Instance = go.AddComponent<DungeonExitInteractable>();
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
        // 1. Portal / Monolith SpriteRenderer
        if (exitRenderer == null)
        {
            exitRenderer = GetComponent<SpriteRenderer>();
            if (exitRenderer == null) exitRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        exitRenderer.sortingOrder = 20;

        if (exitRenderer.sprite == null)
        {
            exitRenderer.sprite = DungeonUIAssetHelper.GetExitPortalSprite();
        }

        // 2. Click Collider (Generous size covering full teleport gate / pillar)
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(2.8f, 3.6f);
        boxCollider.offset = new Vector2(0f, 0.6f);

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

        // Check if an ExitPromptCanvas already exists in scene
        promptCanvas = GetComponentInChildren<Canvas>(true);
        if (promptCanvas == null)
        {
            GameObject existingCanvasGO = GameObject.Find("DungeonExitPromptCanvas");
            if (existingCanvasGO != null) promptCanvas = existingCanvasGO.GetComponent<Canvas>();
        }

        if (promptCanvas == null)
        {
            Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == "DungeonExitPromptCanvas")
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
                ApplyPromptGOTYStyling(panelT.gameObject);
                return;
            }
        }

        Debug.LogWarning("[DungeonExitInteractable] DungeonExitPromptCanvas not found in scene!");
    }

    private void ApplyPromptGOTYStyling(GameObject panelGO)
    {
        if (panelGO == null) return;

        if (promptPanelRect != null)
        {
            promptPanelRect.sizeDelta = new Vector2(420f, 96f);
        }

        Image bgImage = panelGO.GetComponent<Image>();
        Outline outline = panelGO.GetComponent<Outline>();
        bool hasGotyPrompt = DungeonUIAssetHelper.ApplyCompSprite(bgImage, "Comp_Dungeon_ExitPromptBase", sliced: true, Color.white);
        if (!hasGotyPrompt && bgImage != null)
        {
            Sprite frameSprite = DungeonUIAssetHelper.GetFrameSprite();
            if (frameSprite != null)
            {
                bgImage.sprite = frameSprite;
                bgImage.type = Image.Type.Sliced;
            }
            bgImage.color = new Color(0.08f, 0.14f, 0.20f, 0.95f);
        }
        if (outline != null)
        {
            outline.enabled = !hasGotyPrompt;
        }

        VerticalLayoutGroup vLayout = panelGO.GetComponent<VerticalLayoutGroup>();
        if (vLayout != null)
        {
            vLayout.padding = new RectOffset(26, 26, 14, 24);
            vLayout.spacing = 4f;
        }

        if (promptTitleText != null)
        {
            DungeonUIAssetHelper.ApplyFont(promptTitleText, withOutline: true);
            promptTitleText.enableAutoSizing = true;
            promptTitleText.fontSizeMin = 13f;
            promptTitleText.fontSizeMax = 18f;
            promptTitleText.textWrappingMode = TextWrappingModes.NoWrap;
            promptTitleText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (promptHintText != null)
        {
            DungeonUIAssetHelper.ApplyFont(promptHintText);
            promptHintText.enableAutoSizing = true;
            promptHintText.fontSizeMin = 10.5f;
            promptHintText.fontSizeMax = 13.5f;
            promptHintText.textWrappingMode = TextWrappingModes.NoWrap;
            promptHintText.overflowMode = TextOverflowModes.Ellipsis;
        }
    }

    public void Setup(FloorData floor, int floorNumber, ItemContainer backpack, System.Action onInteract)
    {
        currentFloor = floor;
        currentFloorNumber = floorNumber;
        backpackContainer = backpack;
        onInteractCallback = onInteract;
        isAwaitingInteraction = true;
        isHovered = false;

        EnsureVisualComponents();

        // Position Portal at Position_1 or center of stage
        Transform pos1 = GameObject.Find("Position_1")?.transform;
        if (pos1 != null)
        {
            baseWorldPosition = pos1.position;
        }
        else
        {
            baseWorldPosition = new Vector3(2.0f, -0.06f, 0f);
        }

        transform.position = baseWorldPosition;
        transform.localScale = Vector3.one;
        gameObject.SetActive(true);

        if (exitRenderer != null)
        {
            exitRenderer.enabled = true;
            if (exitRenderer.sprite == null)
            {
                exitRenderer.sprite = DungeonUIAssetHelper.GetExitPortalSprite();
            }
            exitRenderer.color = Color.white;
            exitRenderer.sortingOrder = 20;
        }

        if (promptCanvas != null)
        {
            promptCanvas.sortingOrder = 150;
            promptCanvas.gameObject.SetActive(true);
        }

        if (promptTitleText != null)
        {
            promptTitleText.text = $"<b><color=#FFDF00>[E]</color> CỔNG DỊCH CHUYỂN TẦNG {floorNumber}</b>";
        }

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 1f;
            promptCanvasGroup.interactable = true;
            promptCanvasGroup.blocksRaycasts = true;
        }

        Debug.Log($"[DungeonExitInteractable] Exit Portal active for Floor {floorNumber} at {transform.position}. Awaiting player interaction.");
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

        // Direct mouse click check
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
        // 1. Subtle mystical float & pulse
        float bob = Mathf.Sin(Time.time * 2.5f) * 0.05f;
        transform.position = new Vector3(baseWorldPosition.x, baseWorldPosition.y + bob, baseWorldPosition.z);

        Vector3 targetScale = isHovered ? new Vector3(1.12f, 1.12f, 1f) : Vector3.one;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 12f);

        if (exitRenderer != null)
        {
            float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
            Color baseColor = Color.Lerp(new Color(0.75f, 0.95f, 1f), Color.white, pulse);
            if (isHovered)
            {
                // Radiant golden-cyan celestial highlight when hovered
                exitRenderer.color = Color.Lerp(baseColor, new Color(1f, 0.95f, 0.45f, 1f), 0.75f);
            }
            else
            {
                exitRenderer.color = baseColor;
            }
        }

        // 2. Position floating prompt above the portal in screen space
        if (promptPanelRect != null && promptCanvas != null)
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 worldPromptPos = transform.position + new Vector3(0f, 2.7f, 0f);
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

    #region Unity EventSystem Interfaces
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

        Debug.Log($"[DungeonExitInteractable] Player activated Exit Teleport Portal! Opening Floor Victory & Travel options.");

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

        if (onInteractCallback != null)
        {
            onInteractCallback.Invoke();
        }
        else
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.HandleFloorVictory();
            }
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
