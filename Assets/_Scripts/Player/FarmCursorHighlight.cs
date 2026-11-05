using UnityEngine;

/// <summary>
/// World-class farming UX enhancement:
/// 1. Tile Cursor Highlight: Renders a snappy, color-coded grid bracket around the hovered farm plot.
///    - Gold pulse for Ready-to-harvest crops
///    - Emerald green for valid Planting spots (when a seed is selected)
///    - Sky cyan for growing crops
///    - Soft rose/red when plot is out of reach
/// 2. Crop Inspect Tooltip: Triggers ItemTooltipUI when hovering crops.
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

    private FarmPlot currentHoveredPlot;
    private float hoverTimer;
    private float currentHighlightAlpha;

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
    }

    private void OnDisable()
    {
        DismissHighlights();
        if (highlightGO != null) highlightGO.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (highlightGO != null) Destroy(highlightGO);
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
    }
}
