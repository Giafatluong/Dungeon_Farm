using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Individual Blessing / Build Path Card in the Statue Blessing Selection Modal.
/// Fully styled using Figma GOTY Card Assets:
/// - Ornate Tarot Frame Base & Cutout Frame (Aspect Ratio ~ 0.70 matching 687 x 1024)
/// - Handcrafted Element Artworks (Nature, Earth, Void, Bounty, Wind, Fire)
/// - Parchment Title Ribbon & Element Crest Gem
/// - Parchment Description Panel
/// - Interactive Hover Scale & Highlight Border (no select button needed)
/// - Distinct Radiant Gold Border when Activated
/// - Free Blessing Switching anytime
/// </summary>
public class BlessingCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Blessing Type")]
    public BlessingType blessingType;

    [Header("Figma GOTY Card Elements")]
    public Image cardBackground;
    public Image artworkImage;
    public Image frameCutout;
    public Image titleRibbon;
    public Image gemImage;
    public Image descriptionPanel;

    [Header("Text & Controls")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;

    [Header("Select Button (Legacy / Hidden)")]
    public Button selectButton;
    public Image selectButtonBackground;
    public TextMeshProUGUI selectButtonText;

    [Header("Hover & Selection Visuals")]
    public Image borderHighlight;
    public Outline borderOutline;
    public Outline cardOutline;

    [Header("Legacy Fallbacks")]
    public Image iconImage;
    public TextMeshProUGUI iconText;

    private Canvas cardCanvas;
    private GraphicRaycaster cardRaycaster;

    private bool isHovered = false;
    private bool isCurrentActive = false;
    private bool isUnlocked = false;

    private Vector3 defaultScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;
    private const float HOVER_SCALE = 1.06f;
    private const float SCALE_SPEED = 18f;

    // Theme Colors
    private static readonly Color ColorHoverHighlight = new Color(0.12f, 0.90f, 1.0f, 1.0f);     // Bioluminescent Cyan
    private static readonly Color ColorHoverOutline   = new Color(0.02f, 0.35f, 0.50f, 0.90f);    // Deep Cyan Glow
    private static readonly Color ColorActiveBorder   = new Color(1.00f, 0.82f, 0.15f, 1.0f);     // Radiant Sunlight Gold
    private static readonly Color ColorActiveOutline  = new Color(0.70f, 0.40f, 0.05f, 0.95f);    // Warm Gold Shadow
    private static readonly Color ColorActiveHovered  = new Color(1.00f, 0.93f, 0.45f, 1.0f);     // Super Radiant Gold
    private static readonly Color ColorLockedHover    = new Color(0.85f, 0.35f, 0.35f, 0.85f);    // Muted Red/Slate Warning
    private static readonly Color ColorLockedOutline  = new Color(0.20f, 0.08f, 0.08f, 0.80f);

    private System.Action<BlessingType> onSelectCallback;

    private void Awake()
    {
        defaultScale = transform.localScale;
        targetScale = defaultScale;
        EnsureFigmaCardStructure();
    }

    private void OnDisable()
    {
        isHovered = false;
        targetScale = defaultScale;
        transform.localScale = defaultScale;
        if (cardCanvas != null)
        {
            cardCanvas.overrideSorting = false;
        }
    }

    private void Update()
    {
        if (transform.localScale != targetScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * SCALE_SPEED);
            if (Vector3.Distance(transform.localScale, targetScale) < 0.002f)
            {
                transform.localScale = targetScale;
            }
        }
    }

    #region Pointer & Hover Interactions
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = defaultScale * (isUnlocked ? HOVER_SCALE : 1.03f);

        if (cardCanvas != null)
        {
            cardCanvas.overrideSorting = true;
            cardCanvas.sortingOrder = 30; // Float on top of neighboring cards in grid
        }

        UpdateBorderVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = defaultScale;

        if (cardCanvas != null)
        {
            cardCanvas.overrideSorting = false;
        }

        UpdateBorderVisuals();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (!isUnlocked)
        {
            BlessingInfo info = BlessingDatabase.GetBlessingInfo(blessingType);
            string req = info != null && !string.IsNullOrEmpty(info.unlockRequirement) ? info.unlockRequirement : "Chưa đủ điều kiện";
            StatueUI.Instance?.SetFeedback($"<color=#FF7777>Phước lành này chưa mở khóa! (Yêu cầu: {req})</color>");
            return;
        }

        onSelectCallback?.Invoke(blessingType);
    }
    #endregion

    /// <summary>
    /// Constructs and validates the card layout on this card.
    /// Preserves the exact user-designed card sprite (e.g. from scene / project).
    /// </summary>
    public void EnsureFigmaCardStructure()
    {
        // 1. Assign cardBackground Image component
        if (cardBackground == null) cardBackground = GetComponent<Image>();
        if (cardBackground == null) cardBackground = gameObject.AddComponent<Image>();

        // If cardBackground does not have a sprite yet, assign matching blessing sprite
        if (cardBackground.sprite == null)
        {
            Sprite cardSprite = GetBlessingCardSprite(blessingType);
            if (cardSprite != null)
            {
                cardBackground.sprite = cardSprite;
            }
        }

        if (cardBackground != null)
        {
            cardBackground.type = Image.Type.Simple;
            cardBackground.preserveAspect = false;
            cardBackground.raycastTarget = true;
        }

        // Hide legacy redundant layers if present
        string[] redundantLayerNames = new[] {
            "Card_Artwork",
            "Card_Frame_Cutout",
            "Card_Description_Panel",
            "Center_Gem",
            "Card_Title_Ribbon",
            "Icon"
        };
        for (int i = 0; i < redundantLayerNames.Length; i++)
        {
            Transform tr = transform.Find(redundantLayerNames[i]);
            if (tr != null)
            {
                tr.gameObject.SetActive(false);
            }
        }

        // 2. Hide selection button per user instruction (clicking the card itself selects it)
        if (selectButton == null) selectButton = transform.Find("BtnSelect")?.GetComponent<Button>();
        if (selectButton == null) selectButton = GetComponentInChildren<Button>(true);
        if (selectButton != null)
        {
            selectButton.gameObject.SetActive(false);
        }

        // 3. Setup Border Highlight (Sliced outer frame for hover & active states)
        Transform borderTr = transform.Find("Card_Border_Highlight");
        if (borderTr == null)
        {
            GameObject borderGO = new GameObject("Card_Border_Highlight", typeof(RectTransform), typeof(Image), typeof(Outline));
            borderGO.transform.SetParent(transform, false);
            borderTr = borderGO.transform;
        }
        borderTr.SetAsLastSibling();

        if (borderHighlight == null) borderHighlight = borderTr.GetComponent<Image>();
        if (borderOutline == null) borderOutline = borderTr.GetComponent<Outline>();

        if (borderHighlight != null)
        {
            RectTransform bRT = borderHighlight.rectTransform;
            bRT.anchorMin = Vector2.zero;
            bRT.anchorMax = Vector2.one;
            bRT.offsetMin = new Vector2(-4f, -4f);
            bRT.offsetMax = new Vector2(4f, 4f);
            bRT.pivot = new Vector2(0.5f, 0.5f);

            Sprite frameSprite = DungeonUIAssetHelper.GetFrameSprite();
            if (frameSprite != null)
            {
                borderHighlight.sprite = frameSprite;
                borderHighlight.type = Image.Type.Sliced;
                borderHighlight.fillCenter = false; // Hollow center: only frame border is rendered
                borderHighlight.pixelsPerUnitMultiplier = 2.0f;
            }
            borderHighlight.raycastTarget = false;
        }

        if (borderOutline != null)
        {
            borderOutline.effectDistance = new Vector2(2f, -2f);
        }

        // Card outer outline component on root card image
        if (cardOutline == null) cardOutline = GetComponent<Outline>();
        if (cardOutline == null) cardOutline = gameObject.AddComponent<Outline>();
        cardOutline.enabled = false;

        // Nested Canvas & GraphicRaycaster for popping out above other grid items when hovered (authored in scene)
        if (cardCanvas == null) cardCanvas = GetComponent<Canvas>();
        if (cardRaycaster == null) cardRaycaster = GetComponent<GraphicRaycaster>();
        if (cardCanvas != null) cardCanvas.overrideSorting = false;
    }

    private void EnsureCompositeLayers()
    {
        // 1. Artwork Image in upper cutout
        if (artworkImage == null)
        {
            Transform artTr = transform.Find("Card_Artwork");
            if (artTr == null)
            {
                GameObject artGO = new GameObject("Card_Artwork", typeof(RectTransform), typeof(Image));
                artGO.transform.SetParent(transform, false);
                artTr = artGO.transform;
            }
            artworkImage = artTr.GetComponent<Image>();
        }
        if (artworkImage != null)
        {
            artworkImage.gameObject.SetActive(true);
            artworkImage.transform.SetAsFirstSibling();
            if (cardBackground != null) cardBackground.transform.SetAsFirstSibling();
            RectTransform artRT = artworkImage.rectTransform;
            artRT.anchorMin = new Vector2(0.5f, 0.5f);
            artRT.anchorMax = new Vector2(0.5f, 0.5f);
            artRT.pivot = new Vector2(0.5f, 0.5f);
            artRT.anchoredPosition = new Vector2(0f, 38f);
            artRT.sizeDelta = new Vector2(174f, 160f);

            Sprite artSprite = GetBlessingArtwork(blessingType);
            if (artSprite != null)
            {
                artworkImage.sprite = artSprite;
                artworkImage.preserveAspect = true;
            }
            artworkImage.raycastTarget = false;
        }

        // 2. Gem Seal on description top edge
        if (gemImage == null)
        {
            Transform gemTr = transform.Find("Center_Gem");
            if (gemTr == null)
            {
                GameObject gemGO = new GameObject("Center_Gem", typeof(RectTransform), typeof(Image));
                gemGO.transform.SetParent(transform, false);
                gemTr = gemGO.transform;
            }
            gemImage = gemTr.GetComponent<Image>();
        }
        if (gemImage != null)
        {
            gemImage.gameObject.SetActive(true);
            RectTransform gemRT = gemImage.rectTransform;
            gemRT.anchorMin = new Vector2(0.5f, 0.5f);
            gemRT.anchorMax = new Vector2(0.5f, 0.5f);
            gemRT.pivot = new Vector2(0.5f, 0.5f);
            gemRT.anchoredPosition = new Vector2(0f, -48f);
            gemRT.sizeDelta = new Vector2(30f, 30f);

            Sprite gemSprite = GetBlessingGem(blessingType);
            if (gemSprite != null)
            {
                gemImage.sprite = gemSprite;
                gemImage.preserveAspect = true;
            }
            gemImage.raycastTarget = false;
        }
    }

    /// <summary>
    /// Updates border highlight and outline colors according to hover and active state.
    /// </summary>
    public void UpdateBorderVisuals()
    {
        if (borderHighlight == null) return;

        if (isCurrentActive)
        {
            // ACTIVATED BLESSING: Radiant Sunlight Gold Border
            borderHighlight.enabled = true;
            borderHighlight.color = isHovered ? ColorActiveHovered : ColorActiveBorder;

            if (borderOutline != null)
            {
                borderOutline.enabled = true;
                borderOutline.effectColor = ColorActiveOutline;
            }

            if (cardOutline != null)
            {
                cardOutline.enabled = true;
                cardOutline.effectColor = isHovered ? new Color(1f, 0.88f, 0.25f, 0.95f) : new Color(1f, 0.75f, 0.10f, 0.85f);
                cardOutline.effectDistance = new Vector2(2f, 2f);
            }
        }
        else if (isHovered)
        {
            // HOVERED: Luminous Highlight Border
            borderHighlight.enabled = true;
            if (!isUnlocked)
            {
                borderHighlight.color = ColorLockedHover;
                if (borderOutline != null)
                {
                    borderOutline.enabled = true;
                    borderOutline.effectColor = ColorLockedOutline;
                }
                if (cardOutline != null) cardOutline.enabled = false;
            }
            else
            {
                borderHighlight.color = ColorHoverHighlight;
                if (borderOutline != null)
                {
                    borderOutline.enabled = true;
                    borderOutline.effectColor = ColorHoverOutline;
                }
                if (cardOutline != null)
                {
                    cardOutline.enabled = true;
                    cardOutline.effectColor = new Color(0.2f, 0.9f, 1f, 0.85f);
                    cardOutline.effectDistance = new Vector2(2f, 2f);
                }
            }
        }
        else
        {
            // INACTIVE & NOT HOVERED
            borderHighlight.enabled = false;
            if (borderOutline != null) borderOutline.enabled = false;
            if (cardOutline != null) cardOutline.enabled = false;
        }
    }

    /// <summary>
    /// Refreshes the visual state of this card based on player's unlock status and active blessing.
    /// </summary>
    public void UpdateVisuals(System.Action<BlessingType> onSelectAction)
    {
        EnsureFigmaCardStructure();
        onSelectCallback = onSelectAction;

        BlessingInfo info = BlessingDatabase.GetBlessingInfo(blessingType);
        if (info == null) return;

        string title = info.title;
        string desc = info.description;
        string req = info.unlockRequirement;

        isUnlocked = ProgressionManager.Instance != null && ProgressionManager.Instance.IsBlessingUnlocked(blessingType);
        isCurrentActive = isUnlocked && ProgressionManager.Instance != null && ProgressionManager.Instance.activeBlessing == blessingType;

        // Ensure full card sprite is applied (never overwrite an already assigned custom card sprite!)
        if (cardBackground != null && cardBackground.sprite == null)
        {
            Sprite sp = GetBlessingCardSprite(blessingType);
            if (sp != null) cardBackground.sprite = sp;
        }

        // Title text (if element exists)
        if (titleText != null)
        {
            titleText.text = title;
        }

        // Description text (if element exists)
        if (descText != null)
        {
            if (!isUnlocked)
            {
                descText.text = $"{desc}\n<size=7.0><color=#9C3D00><b>Khóa:</b> {req}</color></size>";
            }
            else
            {
                descText.text = desc;
            }
        }

        // Visual Theming by State (Locked, Active, Available)
        if (!isUnlocked)
        {
            // Locked: Muted slate tone
            if (cardBackground != null) cardBackground.color = new Color(0.60f, 0.62f, 0.68f, 1f);
            if (titleText != null) titleText.color = new Color(0.38f, 0.22f, 0.20f);
            if (descText != null) descText.color = new Color(0.35f, 0.25f, 0.20f);
        }
        else if (isCurrentActive)
        {
            // Active: Sunlight Golden Glow
            if (cardBackground != null) cardBackground.color = Color.white;
            if (titleText != null) titleText.color = new Color(0.24f, 0.12f, 0.04f);
            if (descText != null) descText.color = new Color(0.24f, 0.14f, 0.06f);
        }
        else
        {
            // Available: Rich Full Color
            if (cardBackground != null) cardBackground.color = Color.white;
            if (titleText != null) titleText.color = new Color(0.24f, 0.12f, 0.04f);
            if (descText != null) descText.color = new Color(0.24f, 0.14f, 0.06f);
        }

        UpdateBorderVisuals();
    }

    /// <summary>
    /// Loads the custom blessing card sprite corresponding to this blessing type.
    /// </summary>
    public static Sprite GetBlessingCardSprite(BlessingType type)
    {
        // 1. Try Resources.Load with canonical resource paths
        string resPath = type switch
        {
            BlessingType.KeepLootOnDefeat => "BlessingCards/Card_KeepLootOnDefeat",
            BlessingType.BastionStrike    => "BlessingCards/Card_BastionStrike",
            BlessingType.SwiftMomentum   => "BlessingCards/Card_SwiftMomentum",
            BlessingType.StarvingFury     => "BlessingCards/Card_StarvingFury",
            BlessingType.GluttonousAegis  => "BlessingCards/Card_GluttonousAegis",
            BlessingType.EndlessFeast     => "BlessingCards/Card_EndlessFeast",
            BlessingType.BitterDecay      => "BlessingCards/Card_BitterDecay",
            BlessingType.RawVitality      => "BlessingCards/Card_RawVitality",
            _                             => null
        };
        if (!string.IsNullOrEmpty(resPath))
        {
            Sprite sp = Resources.Load<Sprite>(resPath);
            if (sp != null) return sp;
        }

        // 2. In Unity Editor, load directly from Assets/_Assets/Assets/
#if UNITY_EDITOR
        string vnPath = type switch
        {
            BlessingType.KeepLootOnDefeat => "Assets/_Assets/Assets/Bảo hộ thần thánh.png",
            BlessingType.BastionStrike    => "Assets/_Assets/Assets/Thế thủ kiên cố.png",
            BlessingType.SwiftMomentum   => "Assets/_Assets/Assets/Đà Nhanh nhẹn.png",
            BlessingType.StarvingFury     => "Assets/_Assets/Assets/Cơn đói cuồng nộ.png",
            BlessingType.GluttonousAegis  => "Assets/_Assets/Assets/Khiên phàm ăn.png",
            BlessingType.EndlessFeast     => "Assets/_Assets/Assets/đại tiệc vô tận.png",
            BlessingType.BitterDecay      => "Assets/_Assets/Assets/Ăn mòn.png",
            BlessingType.RawVitality      => "Assets/_Assets/Assets/SInh khí nguyên bản.png",
            _                             => null
        };
        if (!string.IsNullOrEmpty(vnPath))
        {
            Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(vnPath);
            if (sp != null) return sp;
        }
#endif

        return null;
    }

    /// <summary>
    /// Maps each BlessingType to its corresponding handcrafted Figma GOTY Card Artwork.
    /// </summary>
    public static Sprite GetBlessingArtwork(BlessingType type)
    {
        string path = type switch
        {
            BlessingType.KeepLootOnDefeat => "UI_Cards/Artworks/Art_01_NatureShield",
            BlessingType.RawVitality      => "UI_Cards/Artworks/Art_02_AncientRoots",
            BlessingType.SwiftMomentum   => "UI_Cards/Artworks/Art_05_SwiftnessBoots",
            BlessingType.BastionStrike    => "UI_Cards/Artworks/Art_03_RustedShield_PureInner",
            BlessingType.BitterDecay      => "UI_Cards/Artworks/Art_03_CursedApple",
            BlessingType.StarvingFury     => "UI_Cards/Artworks/Art_06_FlamingPumpkin",
            BlessingType.GluttonousAegis  => "UI_Cards/Artworks/Art_02_ThornPumpkin_PureInner",
            BlessingType.EndlessFeast     => "UI_Cards/Artworks/Art_04_HarvestBasket",
            _                             => "UI_Cards/Artworks/Art_01_NatureShield"
        };
        return Resources.Load<Sprite>(path);
    }

    /// <summary>
    /// Maps each BlessingType to its corresponding Elemental Jewel Crest.
    /// </summary>
    public static Sprite GetBlessingGem(BlessingType type)
    {
        string path = type switch
        {
            BlessingType.KeepLootOnDefeat => "UI_Cards/Gems/Gem_01_Emerald_Nature",
            BlessingType.RawVitality      => "UI_Cards/Gems/Gem_02_Jade_Earth",
            BlessingType.SwiftMomentum   => "UI_Cards/Gems/Gem_05_Sapphire_Wind",
            BlessingType.BastionStrike    => "UI_Cards/Gems/Gem_02_Jade_Earth",
            BlessingType.BitterDecay      => "UI_Cards/Gems/Gem_03_Amethyst_Void",
            BlessingType.StarvingFury     => "UI_Cards/Gems/Gem_06_Amber_Fire",
            BlessingType.GluttonousAegis  => "UI_Cards/Gems/Gem_04_Topaz_Bounty",
            BlessingType.EndlessFeast     => "UI_Cards/Gems/Gem_04_Topaz_Bounty",
            _                             => "UI_Cards/Gems/Gem_01_Emerald_Nature"
        };
        return Resources.Load<Sprite>(path);
    }
}