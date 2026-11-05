using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Professional rectangular Item Hover Tooltip styled with original pixel-art Figma GOTY assets.
/// Automatically changes frame style, border colors, badges and corner accents based on Item Rarity:
/// - Bình thường (Common)
/// - Hiếm (Rare)
/// - Cực hiếm / Bảo vật (Epic / Boss)
/// </summary>
public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance { get; private set; }

    [Header("UI Hierarchy")]
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private RectTransform tooltipRT;
    [SerializeField] private CanvasGroup tooltipCG;
    [SerializeField] private Image outerFrameImg;
    [SerializeField] private Image innerBgImg;

    [Header("Header Elements")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image itemIconBox;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemRarityBadge;
    [SerializeField] private TextMeshProUGUI itemTypeBadge;

    [Header("Content Elements")]
    [SerializeField] private Image divider1;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Image divider2;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Corner Accents")]
    [SerializeField] private Image[] cornerAccents;

    private Canvas parentCanvas;
    private RectTransform canvasRT;
    private ItemData currentItem;
    private bool isVisible = false;

    // Smooth animation & gliding state
    private float currentAlpha = 0f;
    private float currentScale = 0.94f;
    private Vector2 currentPos;
    private bool isFirstFrame = true;

    private static readonly Color CommonBorder = new Color(0.47f, 0.44f, 0.42f, 1f);     // #78716C
    private static readonly Color CommonBg = new Color(0.09f, 0.08f, 0.07f, 0.98f);       // #171412
    private static readonly Color CommonText = new Color(0.96f, 0.96f, 0.95f, 1f);       // #F5F5F4
    private static readonly Color CommonBadge = new Color(0.66f, 0.63f, 0.62f, 1f);      // #A8A29E

    private static readonly Color RareBorder = new Color(0.23f, 0.51f, 0.96f, 1f);       // #3B82F6
    private static readonly Color RareBg = new Color(0.05f, 0.08f, 0.16f, 0.98f);        // #0D1429
    private static readonly Color RareText = new Color(0.58f, 0.77f, 0.99f, 1f);        // #93C5FD
    private static readonly Color RareBadge = new Color(0.38f, 0.65f, 0.98f, 1f);       // #60A5FA

    private static readonly Color EpicBorder = new Color(0.96f, 0.62f, 0.04f, 1f);       // #F59E0B
    private static readonly Color EpicBg = new Color(0.15f, 0.08f, 0.02f, 0.98f);        // #261405
    private static readonly Color EpicText = new Color(0.99f, 0.90f, 0.54f, 1f);        // #FDE68A
    private static readonly Color EpicBadge = new Color(0.99f, 0.83f, 0.30f, 1f);       // #FCD34D

    public static ItemTooltipUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        Instance = FindFirstObjectByType<ItemTooltipUI>(FindObjectsInactive.Include);
        if (Instance != null) return Instance;

        // Auto-generate UI Hierarchy inside high-sorting Canvas
        GameObject rootGO = new GameObject("[ItemTooltipUI]");
        DontDestroyOnLoad(rootGO);

        Canvas c = rootGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 9999; // Topmost
        CanvasScaler cs = rootGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        // Never intercept mouse clicks
        CanvasGroup cg = rootGO.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        Instance = rootGO.AddComponent<ItemTooltipUI>();
        Instance.parentCanvas = c;
        Instance.canvasRT = rootGO.GetComponent<RectTransform>();
        Instance.BuildUIHierarchy(rootGO.transform);

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
        DontDestroyOnLoad(gameObject);

        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        if (canvasRT == null && parentCanvas != null) canvasRT = parentCanvas.GetComponent<RectTransform>();

        if (tooltipRoot == null)
        {
            BuildUIHierarchy(transform);
        }
        else if (tooltipCG == null)
        {
            tooltipCG = tooltipRoot.GetComponent<CanvasGroup>();
            if (tooltipCG == null) tooltipCG = tooltipRoot.AddComponent<CanvasGroup>();
        }

        Hide(true);
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Hide(immediate: true);
    }

    private void Start()
    {
        Hide(true);
    }

    private void Update()
    {
        if (tooltipRoot == null) return;
        if (!isVisible && currentAlpha <= 0.005f && !tooltipRoot.activeSelf) return;

        float dt = Time.unscaledDeltaTime;

        // 1. Smooth Fade & Scale Spring Animation
        if (isVisible)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, 1f, dt * 10f); // Fast yet creamy ~100ms fade-in
            currentScale = Mathf.Lerp(currentScale, 1f, 1f - Mathf.Exp(-26f * dt));
        }
        else
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, 0f, dt * 14f); // Crisp ~70ms fade-out
            currentScale = Mathf.Lerp(currentScale, 0.94f, 1f - Mathf.Exp(-26f * dt));

            if (currentAlpha <= 0.005f)
            {
                currentAlpha = 0f;
                if (tooltipRoot.activeSelf) tooltipRoot.SetActive(false);
            }
        }

        if (tooltipCG != null) tooltipCG.alpha = currentAlpha;
        if (tooltipRT != null) tooltipRT.localScale = new Vector3(currentScale, currentScale, 1f);

        // 2. Smooth Fluid Position Following (Damped / Lerp Follow)
        if (tooltipRoot.activeSelf && tooltipRT != null)
        {
            Vector2 targetPos = CalculateTargetPosition();
            if (isFirstFrame)
            {
                currentPos = targetPos;
                isFirstFrame = false;
            }
            else
            {
                // Smooth glide following the cursor
                currentPos = Vector2.Lerp(currentPos, targetPos, 1f - Mathf.Exp(-32f * dt));
            }

            tooltipRT.anchoredPosition = currentPos;
        }
    }

    private Vector2 CalculateTargetPosition()
    {
        if (canvasRT == null)
        {
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null) canvasRT = parentCanvas.GetComponent<RectTransform>();
        }

        Vector2 mousePos = Input.mousePosition;

        if (canvasRT != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, mousePos, null, out Vector2 localPoint))
        {
            float width = tooltipRT != null ? tooltipRT.rect.width : 280f;
            float height = tooltipRT != null ? tooltipRT.rect.height : 230f;
            if (width < 50f) width = 280f;
            if (height < 50f) height = 230f;

            float minX = -canvasRT.rect.width * canvasRT.pivot.x + 12f;
            float maxX = canvasRT.rect.width * (1f - canvasRT.pivot.x) - 12f;
            float minY = -canvasRT.rect.height * canvasRT.pivot.y + 12f;
            float maxY = canvasRT.rect.height * (1f - canvasRT.pivot.y) - 12f;

            float targetX = localPoint.x + 18f;
            float targetY = localPoint.y - 10f;

            // Flip horizontally if overflow
            if (targetX + width > maxX)
            {
                targetX = localPoint.x - width - 18f;
            }
            // Flip vertically if overflow
            if (targetY - height < minY)
            {
                targetY = localPoint.y + height + 10f;
            }

            targetX = Mathf.Clamp(targetX, minX, maxX - width);
            targetY = Mathf.Clamp(targetY, minY + height, maxY);

            return new Vector2(targetX, targetY);
        }
        else
        {
            return mousePos;
        }
    }

    public void Show(ItemData item, Vector3? anchorPos = null)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        EnsureUIReady();

        bool wasHidden = !isVisible || tooltipRoot == null || !tooltipRoot.activeSelf || currentAlpha <= 0.05f;
        currentItem = item;
        isVisible = true;

        PopulateData(item);
        ApplyRarityStyling(item.GetRarity());

        if (tooltipRoot != null)
        {
            if (!tooltipRoot.activeSelf) tooltipRoot.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRT);

            Vector2 targetPos = CalculateTargetPosition();
            if (wasHidden)
            {
                currentPos = targetPos;
                currentAlpha = 0f;
                currentScale = 0.93f;
                isFirstFrame = true;
                if (tooltipRT != null)
                {
                    tooltipRT.anchoredPosition = currentPos;
                    tooltipRT.localScale = new Vector3(currentScale, currentScale, 1f);
                }
                if (tooltipCG != null) tooltipCG.alpha = 0f;
            }
            else
            {
                // Item switch subtle punch
                currentScale = 0.97f;
            }
        }
    }

    public void Hide(bool immediate = false)
    {
        isVisible = false;
        currentItem = null;

        if (immediate)
        {
            currentAlpha = 0f;
            currentScale = 0.94f;
            if (tooltipCG != null) tooltipCG.alpha = 0f;
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
        }
    }

    private void PopulateData(ItemData item)
    {
        ItemData.ItemRarity rarity = item.GetRarity();

        // 1. Icon
        if (itemIcon != null)
        {
            itemIcon.sprite = item.itemIcon;
            itemIcon.gameObject.SetActive(itemIcon.sprite != null);
        }

        // 2. Title
        string starPrefix = rarity == ItemData.ItemRarity.Epic ? "★ " : "";
        string displayName = !string.IsNullOrEmpty(item.itemName) ? item.itemName : item.name;
        if (itemNameText != null)
        {
            itemNameText.text = $"{starPrefix}{displayName}";
        }

        // 3. Badges
        if (itemRarityBadge != null)
        {
            itemRarityBadge.text = rarity switch
            {
                ItemData.ItemRarity.Common => "BÌNH THƯỜNG",
                ItemData.ItemRarity.Rare => "HIẾM",
                ItemData.ItemRarity.Epic => "BẢO VẬT BOSS",
                _ => "VẬT PHẨM"
            };
        }

        if (itemTypeBadge != null)
        {
            itemTypeBadge.text = item.itemType switch
            {
                ItemData.ItemType.Produce => "NÔNG SẢN",
                ItemData.ItemType.Food => "MÓN ĂN",
                ItemData.ItemType.Seed => "HẠT GIỐNG",
                _ => (rarity == ItemData.ItemRarity.Epic ? "BẢO VẬT" : (item.name.Contains("Monster") || item.name.Contains("Slime") || item.name.Contains("Bone") || item.name.Contains("Wing") || item.name.Contains("Spore") || item.name.Contains("Talon") ? "NGUYÊN LIỆU QUÁI" : "VẬT PHẨM"))
            };
        }

        // 4. Stats & Effects
        StringBuilder sb = new StringBuilder();

        if (item is FoodData food)
        {
            if (food.healthValue > 0)
                sb.AppendLine($"<color=#CBD5E1>Hồi phục Máu:</color>  <color=#FF6B6B><b>+{food.healthValue} HP</b></color>");
            if (food.hungerValue > 0)
                sb.AppendLine($"<color=#CBD5E1>Hồi phục Độ No:</color>  <color=#FFD043><b>+{food.hungerValue} Độ No</b></color>");

            if (food.foodBuff != null && food.foodBuff.Length > 0)
            {
                for (int i = 0; i < food.foodBuff.Length; i++)
                {
                    Foodbuff b = food.foodBuff[i];
                    if (b != null && b.buffs != null)
                    {
                        string statName = !string.IsNullOrEmpty(b.buffs.buffName) ? b.buffs.buffName : b.buffs.buffType.ToString();
                        string sign = b.buffValue > 0 ? "+" : "";
                        string durType = b.buffDurationType switch
                        {
                            FoodData.BuffDurationType.Turn => "Lượt",
                            FoodData.BuffDurationType.Combat => "Trận",
                            _ => "Tầng"
                        };
                        sb.AppendLine($"<color=#CBD5E1>Hiệu ứng Buff:</color>  <color=#50D8FF><b>{sign}{b.buffValue} {statName} ({b.buffDuration} {durType})</b></color>");
                    }
                }
            }
        }
        else if (item.itemType == ItemData.ItemType.Produce)
        {
            sb.AppendLine("<color=#CBD5E1>Hồi phục ăn thô:</color>  <color=#FF6B6B><b>+6 HP</b></color> <color=#94A3B8>|</color> <color=#FFD043><b>+15 Độ No</b></color>");
            sb.AppendLine("<color=#CBD5E1>Công dụng chính:</color>  <color=#F1F5F9>Nấu ăn & Dâng lễ Tượng</color>");
        }
        else if (item.itemType == ItemData.ItemType.Seed)
        {
            sb.AppendLine("<color=#CBD5E1>Loại hạt giống:</color>  <color=#38BDF8><b>Trồng tại Nông Trại</b></color>");
            sb.AppendLine("<color=#CBD5E1>Sản lượng vụ:</color>  <color=#4ADE80><b>~3 Nông Sản / Lần thu</b></color>");
        }
        else if (rarity == ItemData.ItemRarity.Epic)
        {
            string bossSource = (displayName.Contains("Vương Miện") || displayName.Contains("Slime")) ? "Slime Chúa (Boss T2)" :
                                (displayName.Contains("Hoa Hồng") || displayName.Contains("Xương")) ? "Vua Xương Cổ Đại (Boss T1)" :
                                (displayName.Contains("Hắc Huyết") || displayName.Contains("Tinh Thể")) ? "Kỵ Sĩ Bay Hắc Ám (Boss T3)" : "Trùm Hầm Ngục";
            sb.AppendLine($"<color=#CBD5E1>Nguồn gốc rơi:</color>  <color=#F87171><b>{bossSource}</b></color>");
            sb.AppendLine("<color=#CBD5E1>Tỉ lệ rơi:</color>  <color=#4ADE80><b>100% Chắc Chắn</b></color>");
            sb.AppendLine("<color=#CBD5E1>Công dụng cốt lõi:</color>  <color=#FDE047><b>Chặn Cổng Bậc 5/10 Tượng</b></color>");
        }

        if (statsText != null)
        {
            statsText.text = sb.ToString().TrimEnd();
        }

        // 5. Description / Lore
        if (descriptionText != null)
        {
            if (!string.IsNullOrEmpty(item.description))
            {
                descriptionText.text = item.description;
            }
            else
            {
                descriptionText.text = rarity switch
                {
                    ItemData.ItemRarity.Epic => "Bảo vật thần thánh thu thập từ hầm ngục. Bắt buộc phải có để dâng lễ phá vỡ phong ấn Tượng Nữ Thần.",
                    ItemData.ItemRarity.Rare => "Món ăn/nông sản giàu dinh dưỡng, mang lại sức mạnh vượt trội cho chuyến đi hầm ngục.",
                    _ => "Nông sản khởi đầu được thu hoạch từ vụ mùa nông trại. Dùng để sinh tồn và chế biến các món súp bổ dưỡng."
                };
            }
        }
    }

    private void ApplyRarityStyling(ItemData.ItemRarity rarity)
    {
        Color borderColor = rarity switch
        {
            ItemData.ItemRarity.Common => CommonBorder,
            ItemData.ItemRarity.Rare => RareBorder,
            ItemData.ItemRarity.Epic => EpicBorder,
            _ => CommonBorder
        };

        Color bgColor = rarity switch
        {
            ItemData.ItemRarity.Common => CommonBg,
            ItemData.ItemRarity.Rare => RareBg,
            ItemData.ItemRarity.Epic => EpicBg,
            _ => CommonBg
        };

        Color textColor = rarity switch
        {
            ItemData.ItemRarity.Common => CommonText,
            ItemData.ItemRarity.Rare => RareText,
            ItemData.ItemRarity.Epic => EpicText,
            _ => CommonText
        };

        Color badgeColor = rarity switch
        {
            ItemData.ItemRarity.Common => CommonBadge,
            ItemData.ItemRarity.Rare => RareBadge,
            ItemData.ItemRarity.Epic => EpicBadge,
            _ => CommonBadge
        };

        if (outerFrameImg != null)
        {
            DungeonUIAssetHelper.StyleSlicedFrame(outerFrameImg, borderColor);
        }

        if (innerBgImg != null)
        {
            innerBgImg.color = bgColor;
        }

        if (itemNameText != null)
        {
            itemNameText.color = textColor;
        }

        if (itemRarityBadge != null)
        {
            itemRarityBadge.color = badgeColor;
        }

        if (divider1 != null)
        {
            divider1.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0.45f);
        }

        if (divider2 != null)
        {
            divider2.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0.25f);
        }

        if (cornerAccents != null)
        {
            for (int i = 0; i < cornerAccents.Length; i++)
            {
                if (cornerAccents[i] != null)
                {
                    cornerAccents[i].color = badgeColor;
                }
            }
        }
    }

    private void EnsureUIReady()
    {
        if (tooltipRoot == null)
        {
            BuildUIHierarchy(transform);
        }
    }

    private void BuildUIHierarchy(Transform parent)
    {
        // 1. Root Tooltip Object
        tooltipRoot = new GameObject("TooltipBox", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(CanvasGroup));
        tooltipRoot.transform.SetParent(parent, false);

        tooltipCG = tooltipRoot.GetComponent<CanvasGroup>();
        tooltipCG.blocksRaycasts = false;
        tooltipCG.interactable = false;
        tooltipCG.alpha = 0f;

        tooltipRT = tooltipRoot.GetComponent<RectTransform>();
        tooltipRT.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRT.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRT.pivot = new Vector2(0f, 1f);
        tooltipRT.sizeDelta = new Vector2(280f, 0f);

        ContentSizeFitter csf = tooltipRoot.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        VerticalLayoutGroup vlg = tooltipRoot.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 8f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 2. Outer Frame Image (Sliced UI_Frame from Figma GOTY assets)
        outerFrameImg = tooltipRoot.AddComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(outerFrameImg, CommonBorder);

        // 3. Inner Dark Background
        GameObject bgObj = new GameObject("InnerBackground", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(tooltipRoot.transform, false);
        bgObj.transform.SetAsFirstSibling();
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = new Vector2(4, 4);
        bgRT.offsetMax = new Vector2(-4, -4);
        innerBgImg = bgObj.GetComponent<Image>();
        innerBgImg.color = CommonBg;
        innerBgImg.raycastTarget = false;

        // 4. Header Row (Horizontal Layout)
        GameObject headerGO = new GameObject("HeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        headerGO.transform.SetParent(tooltipRoot.transform, false);
        LayoutElement headerLE = headerGO.GetComponent<LayoutElement>();
        headerLE.minHeight = 44f;
        headerLE.preferredHeight = 44f;

        HorizontalLayoutGroup headerHLG = headerGO.GetComponent<HorizontalLayoutGroup>();
        headerHLG.spacing = 10f;
        headerHLG.childControlWidth = false;
        headerHLG.childControlHeight = true;
        headerHLG.childForceExpandWidth = false;
        headerHLG.childForceExpandHeight = true;

        // 4a. Icon Box
        GameObject iconBoxGO = new GameObject("IconBox", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        iconBoxGO.transform.SetParent(headerGO.transform, false);
        iconBoxGO.GetComponent<RectTransform>().sizeDelta = new Vector2(44, 44);
        LayoutElement iconBoxLE = iconBoxGO.GetComponent<LayoutElement>();
        iconBoxLE.minWidth = 44f;
        iconBoxLE.preferredWidth = 44f;
        itemIconBox = iconBoxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(itemIconBox, new Color(0.12f, 0.12f, 0.14f, 1f));

        // 4b. Icon
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(iconBoxGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = new Vector2(4, 4);
        iconRT.offsetMax = new Vector2(-4, -4);
        itemIcon = iconGO.GetComponent<Image>();
        itemIcon.preserveAspect = true;
        itemIcon.raycastTarget = false;

        // 4c. Title & Badge Column
        GameObject titleColGO = new GameObject("TitleColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        titleColGO.transform.SetParent(headerGO.transform, false);
        LayoutElement titleColLE = titleColGO.GetComponent<LayoutElement>();
        titleColLE.minWidth = 190f;
        titleColLE.flexibleWidth = 1f;

        VerticalLayoutGroup titleVLG = titleColGO.GetComponent<VerticalLayoutGroup>();
        titleVLG.spacing = 3f;
        titleVLG.childControlWidth = true;
        titleVLG.childControlHeight = false;

        // Title text
        GameObject nameTextGO = new GameObject("ItemName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameTextGO.transform.SetParent(titleColGO.transform, false);
        itemNameText = nameTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(itemNameText);
        itemNameText.fontSize = 13;
        itemNameText.fontStyle = FontStyles.Bold;
        itemNameText.text = "Item Name";

        // Badges Row
        GameObject badgeRowGO = new GameObject("BadgesRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        badgeRowGO.transform.SetParent(titleColGO.transform, false);
        HorizontalLayoutGroup badgeHLG = badgeRowGO.GetComponent<HorizontalLayoutGroup>();
        badgeHLG.spacing = 6f;
        badgeHLG.childControlWidth = false;
        badgeHLG.childControlHeight = true;

        GameObject rarityGO = new GameObject("RarityBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        rarityGO.transform.SetParent(badgeRowGO.transform, false);
        itemRarityBadge = rarityGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(itemRarityBadge);
        itemRarityBadge.fontSize = 10;
        itemRarityBadge.fontStyle = FontStyles.Bold;
        itemRarityBadge.text = "[BÌNH THƯỜNG]";

        GameObject typeGO = new GameObject("TypeBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        typeGO.transform.SetParent(badgeRowGO.transform, false);
        itemTypeBadge = typeGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(itemTypeBadge);
        itemTypeBadge.fontSize = 10;
        itemTypeBadge.color = new Color(0.6f, 0.6f, 0.65f, 1f);
        itemTypeBadge.text = "• NÔNG SẢN";

        // 5. Divider 1
        GameObject div1GO = new GameObject("Divider1", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        div1GO.transform.SetParent(tooltipRoot.transform, false);
        LayoutElement div1LE = div1GO.GetComponent<LayoutElement>();
        div1LE.minHeight = 1f;
        div1LE.preferredHeight = 1f;
        divider1 = div1GO.GetComponent<Image>();
        divider1.color = new Color(1f, 1f, 1f, 0.2f);

        // 6. Stats Section
        GameObject statsGO = new GameObject("StatsSection", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        statsGO.transform.SetParent(tooltipRoot.transform, false);
        statsText = statsGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(statsText, withOutline: true);
        statsText.fontSize = 11.5f;
        statsText.lineSpacing = 15f;
        statsText.text = "Stats info...";

        // 7. Divider 2
        GameObject div2GO = new GameObject("Divider2", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        div2GO.transform.SetParent(tooltipRoot.transform, false);
        LayoutElement div2LE = div2GO.GetComponent<LayoutElement>();
        div2LE.minHeight = 1f;
        div2LE.preferredHeight = 1f;
        divider2 = div2GO.GetComponent<Image>();
        divider2.color = new Color(1f, 1f, 1f, 0.12f);

        // 8. Description Box
        GameObject descGO = new GameObject("DescriptionText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        descGO.transform.SetParent(tooltipRoot.transform, false);
        descriptionText = descGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(descriptionText, withOutline: true);
        descriptionText.fontSize = 12f;
        descriptionText.fontStyle = FontStyles.Italic;
        descriptionText.color = new Color(0.95f, 0.96f, 0.98f, 1f);
        descriptionText.lineSpacing = 12f;
        descriptionText.text = "Lore description...";

        // 9. Corner Accents (4 rectangular corner brackets)
        cornerAccents = new Image[4];
        Vector2[] cornerPivots = new Vector2[]
        {
            new Vector2(0f, 1f), // Top-Left
            new Vector2(1f, 1f), // Top-Right
            new Vector2(0f, 0f), // Bottom-Left
            new Vector2(1f, 0f)  // Bottom-Right
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject cornerGO = new GameObject($"Corner_{i}", typeof(RectTransform), typeof(Image));
            cornerGO.transform.SetParent(tooltipRoot.transform, false);
            RectTransform crt = cornerGO.GetComponent<RectTransform>();
            crt.anchorMin = cornerPivots[i];
            crt.anchorMax = cornerPivots[i];
            crt.pivot = cornerPivots[i];
            crt.sizeDelta = new Vector2(4f, 4f);
            Image cImg = cornerGO.GetComponent<Image>();
            cImg.color = CommonBadge;
            cImg.raycastTarget = false;
            cornerAccents[i] = cImg;
        }

        tooltipRoot.SetActive(false);
    }
}

