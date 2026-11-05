using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// World-class farming UX enhancement:
/// 1. Tile Cursor Highlight: Renders a snappy, color-coded grid bracket around the hovered farm plot.
///    - Gold pulse for Ready-to-harvest crops
///    - Emerald green for valid Planting spots (when a seed is selected)
///    - Sky cyan for growing crops
///    - Soft rose/red when plot is out of reach
/// 2. Crop Inspect Tooltip: Shows crop name, growth progress dots [● ● ○], and days remaining on hover.
/// </summary>
[DisallowMultipleComponent]
public class FarmCursorHighlight : MonoBehaviour
{
    public static FarmCursorHighlight Instance { get; private set; }

    private PlayerInteraction playerInteraction;
    private Camera mainCam;

    // Visual Bracket Highlight
    private GameObject highlightGO;
    private SpriteRenderer highlightRenderer;
    private static Sprite cachedBracketSprite;

    // Crop Hover Tooltip UI (ScreenSpaceOverlay)
    private GameObject tooltipCanvasGO;
    private RectTransform tooltipPanelRT;
    private CanvasGroup tooltipCanvasGroup;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI statusText;
    private Image panelBorder;

    private FarmPlot currentHoveredPlot;
    private bool isPlotNear;
    private float hoverTimer;
    private float currentHighlightAlpha;
    private float currentTooltipAlpha;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        playerInteraction = GetComponent<PlayerInteraction>();
        if (playerInteraction == null) playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        mainCam = Camera.main;

        BuildHighlightObject();
        BuildTooltipUI();
    }

    private void OnDisable()
    {
        DismissHighlights();
        if (highlightGO != null) highlightGO.SetActive(false);
        if (tooltipCanvasGO != null) tooltipCanvasGO.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (highlightGO != null) Destroy(highlightGO);
        if (tooltipCanvasGO != null) Destroy(tooltipCanvasGO);
    }

    #region Procedural Sprite & Highlight Setup
    private void BuildHighlightObject()
    {
        if (cachedBracketSprite == null)
        {
            cachedBracketSprite = GenerateBracketSprite();
        }

        highlightGO = new GameObject("[FarmTileHighlight]");
        highlightGO.transform.SetParent(null);
        highlightRenderer = highlightGO.AddComponent<SpriteRenderer>();
        highlightRenderer.sprite = cachedBracketSprite;
        highlightRenderer.sortingOrder = 2; // Above soil & crops (order 0), below player character (order 3)
        highlightRenderer.color = new Color(1f, 1f, 1f, 0f);
        highlightGO.SetActive(false);
    }

    private Sprite GenerateBracketSprite()
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[size * size];
        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        const int borderThick = 4;
        const int cornerLen = 28;
        const int inset = 4;

        Color cornerCol = new Color(1f, 1f, 1f, 1f);
        Color edgeCol = new Color(1f, 1f, 1f, 0.4f);
        Color fillCol = new Color(1f, 1f, 1f, 0.08f);

        for (int y = inset; y < size - inset; y++)
        {
            for (int x = inset; x < size - inset; x++)
            {
                bool isLeft = x < inset + borderThick;
                bool isRight = x >= size - inset - borderThick;
                bool isBottom = y < inset + borderThick;
                bool isTop = y >= size - inset - borderThick;

                bool nearLeftCorner = x < inset + cornerLen;
                bool nearRightCorner = x >= size - inset - cornerLen;
                bool nearBottomCorner = y < inset + cornerLen;
                bool nearTopCorner = y >= size - inset - cornerLen;

                bool isCorner = (isLeft && (nearBottomCorner || nearTopCorner)) ||
                               (isRight && (nearBottomCorner || nearTopCorner)) ||
                               (isBottom && (nearLeftCorner || nearRightCorner)) ||
                               (isTop && (nearLeftCorner || nearRightCorner));

                bool isEdge = isLeft || isRight || isBottom || isTop;

                if (isCorner)
                {
                    pixels[y * size + x] = cornerCol;
                }
                else if (isEdge)
                {
                    pixels[y * size + x] = edgeCol;
                }
                else
                {
                    pixels[y * size + x] = fillCol;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
    #endregion

    #region Tooltip UI Construction
    private void BuildTooltipUI()
    {
        tooltipCanvasGO = new GameObject("[CropInspectTooltipCanvas]");
        tooltipCanvasGO.transform.SetParent(null);

        Canvas canvas = tooltipCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9990;

        CanvasScaler scaler = tooltipCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelGO = new GameObject("TooltipPanel", typeof(RectTransform));
        panelGO.transform.SetParent(tooltipCanvasGO.transform, false);
        tooltipPanelRT = panelGO.GetComponent<RectTransform>();
        tooltipPanelRT.sizeDelta = new Vector2(240f, 64f);
        tooltipPanelRT.pivot = new Vector2(0.5f, 0f); // Anchor bottom-center

        tooltipCanvasGroup = panelGO.AddComponent<CanvasGroup>();
        tooltipCanvasGroup.alpha = 0f;
        tooltipCanvasGroup.blocksRaycasts = false;
        tooltipCanvasGroup.interactable = false;

        // Dark background
        Image bg = panelGO.AddComponent<Image>();
        bg.color = new Color(0.09f, 0.08f, 0.07f, 0.94f);

        // Gold border outline
        Outline outline = panelGO.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.72f, 0.32f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        panelBorder = bg;

        // Vertical Layout
        VerticalLayoutGroup vlg = panelGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 8, 8);
        vlg.spacing = 3f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = panelGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Title text
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform));
        titleGO.transform.SetParent(panelGO.transform, false);
        titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 14;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.92f, 0.45f);

        // Status text
        GameObject statusGO = new GameObject("StatusText", typeof(RectTransform));
        statusGO.transform.SetParent(panelGO.transform, false);
        statusText = statusGO.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 12;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = new Color(0.9f, 0.9f, 0.95f);

        TMP_FontAsset font = DungeonUIAssetHelper.GetFontAsset();
        if (font != null)
        {
            titleText.font = font;
            statusText.font = font;
        }

        tooltipCanvasGO.SetActive(false);
    }
    #endregion

    private void Update()
    {
        if (mainCam == null) mainCam = Camera.main;

        // Hide if any modal UI (chest, dialogue, cooking, main menu, or inventory) is open
        bool isUIBlocking = UIEscapeManager.IsAnyModalUIOpen(includeInventory: true, includeMainMenu: true)
                         || (TutorialManager.Instance != null && TutorialManager.Instance.IsInputBlocked);
        bool pointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                             UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        if (isUIBlocking || pointerOverUI || playerInteraction == null)
        {
            DismissHighlights();
            return;
        }

        // Query hovered plot from PlayerInteraction
        FarmPlot hovered = playerInteraction.GetHoveredFarmPlot(out bool isNear);

        if (hovered != null)
        {
            if (hovered != currentHoveredPlot)
            {
                currentHoveredPlot = hovered;
                hoverTimer = 0f;
            }
            isPlotNear = isNear;
            hoverTimer += Time.deltaTime;

            UpdateBracketVisual(hovered, isNear);
            UpdateTooltipVisual(hovered, isNear);
        }
        else
        {
            currentHoveredPlot = null;
            hoverTimer = 0f;
            DismissHighlights();
        }
    }

    private void UpdateBracketVisual(FarmPlot plot, bool isNear)
    {
        if (highlightGO == null || highlightRenderer == null) return;

        if (!highlightGO.activeSelf) highlightGO.SetActive(true);

        // Snap to plot center
        highlightGO.transform.position = plot.transform.position;

        // Determine color & pulse animation
        Color targetColor;
        float targetScale = 1.02f;

        if (!isNear)
        {
            targetColor = new Color(1f, 0.35f, 0.35f, 0.5f); // Out of reach (Reddish)
        }
        else if (plot.currentState == FarmPlot.State.Ready)
        {
            // Golden Harvest glow with alive pulse
            float pulse = 0.04f * Mathf.Sin(Time.time * 6f);
            targetScale = 1.05f + pulse;
            targetColor = new Color(1f, 0.88f, 0.15f, 0.92f);
        }
        else if (plot.currentState == FarmPlot.State.Empty)
        {
            bool hasSeedSelected = InventoryButton.selectedItem != null &&
                                  (InventoryButton.selectedItem.itemType == ItemData.ItemType.Seed ||
                                   InventoryButton.selectedItem.itemName.ToLower().Contains("seed"));
            if (hasSeedSelected)
            {
                targetColor = new Color(0.32f, 0.98f, 0.48f, 0.88f); // Emerald Planting Green
            }
            else
            {
                targetColor = new Color(1f, 1f, 1f, 0.45f); // Neutral Translucent White
            }
        }
        else // Planted (Growing)
        {
            targetColor = new Color(0.55f, 0.85f, 1f, 0.78f); // Soft Sky Cyan
        }

        currentHighlightAlpha = Mathf.MoveTowards(currentHighlightAlpha, targetColor.a, Time.deltaTime * 10f);
        targetColor.a = currentHighlightAlpha;

        highlightRenderer.color = targetColor;
        highlightGO.transform.localScale = new Vector3(targetScale, targetScale, 1f);
    }

    private void UpdateTooltipVisual(FarmPlot plot, bool isNear)
    {
        // Check if hovering over a planted or harvest-ready crop
        bool isCropPlot = plot != null && plot.currentCrop != null &&
                         (plot.currentState == FarmPlot.State.Planted || plot.currentState == FarmPlot.State.Ready);

        if (isCropPlot && hoverTimer >= 0.04f)
        {
            ItemTooltipUI.EnsureInstance()?.ShowCrop(plot);
        }
        else
        {
            if (ItemTooltipUI.Instance != null && ItemTooltipUI.Instance.IsShowingCrop)
            {
                ItemTooltipUI.Instance.Hide();
            }
        }
    }

    private void DismissHighlights()
    {
        if (highlightRenderer != null)
        {
            currentHighlightAlpha = Mathf.MoveTowards(currentHighlightAlpha, 0f, Time.deltaTime * 14f);
            Color c = highlightRenderer.color;
            c.a = currentHighlightAlpha;
            highlightRenderer.color = c;
            if (currentHighlightAlpha <= 0.01f && highlightGO != null && highlightGO.activeSelf)
            {
                highlightGO.SetActive(false);
            }
        }

        if (ItemTooltipUI.Instance != null && ItemTooltipUI.Instance.IsShowingCrop)
        {
            ItemTooltipUI.Instance.Hide();
        }

        if (tooltipCanvasGroup != null)
        {
            currentTooltipAlpha = Mathf.MoveTowards(currentTooltipAlpha, 0f, Time.deltaTime * 18f);
            tooltipCanvasGroup.alpha = currentTooltipAlpha;
            if (currentTooltipAlpha <= 0.01f && tooltipCanvasGO != null && tooltipCanvasGO.activeSelf)
            {
                tooltipCanvasGO.SetActive(false);
            }
        }
    }
}
