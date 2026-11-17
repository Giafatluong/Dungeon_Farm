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

    [SerializeField] public Canvas parentCanvas;
    [SerializeField] public RectTransform canvasRT;
    private ItemData currentItem;
    private FarmPlot currentPlot;
    private int currentPlotStage = -1;
    private FarmPlot.State currentPlotState;
    private int currentPlotDaysInStage = -1;
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

    private static readonly Color CropBorder = new Color(0.22f, 0.77f, 0.38f, 1f);       // #37C461 (Emerald)
    private static readonly Color CropBg = new Color(0.04f, 0.11f, 0.07f, 0.98f);         // #0B1C12
    private static readonly Color CropText = new Color(0.72f, 0.96f, 0.78f, 1f);         // #B7F5C7
    private static readonly Color CropBadge = new Color(0.35f, 0.88f, 0.52f, 1f);        // #59E085

    private bool isShowingCrop = false;
    public bool IsShowingCrop => isVisible && isShowingCrop;

    public static ItemTooltipUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        Instance = FindFirstObjectByType<ItemTooltipUI>(FindObjectsInactive.Include);
        if (Instance != null)
        {
            Instance.BindSceneReferences();
            return Instance;
        }

        GameObject canvasGO = GameObject.Find("[ItemTooltipCanvas]") ?? GameObject.Find("ItemTooltipCanvas");
        if (canvasGO != null)
        {
            Instance = canvasGO.GetComponentInChildren<ItemTooltipUI>(true);
            if (Instance != null)
            {
                Instance.BindSceneReferences();
                return Instance;
            }
        }

        return null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);

        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        if (canvasRT == null && parentCanvas != null) canvasRT = parentCanvas.GetComponent<RectTransform>();

        if (tooltipRoot == null)
        {
            Transform existingBox = transform.Find("TooltipBox");
            if (existingBox != null)
            {
                tooltipRoot = existingBox.gameObject;
            }
        }

        BindSceneReferences();
        Hide(true);
    }

    public void BindSceneReferences()
    {
        if (tooltipRoot == null)
        {
            Transform existingBox = transform.Find("TooltipBox");
            if (existingBox != null) tooltipRoot = existingBox.gameObject;
        }
        if (tooltipRoot == null) return;
        Transform root = tooltipRoot.transform;

        tooltipCG = tooltipRoot.GetComponent<CanvasGroup>();
        tooltipRT = tooltipRoot.GetComponent<RectTransform>();
        outerFrameImg = tooltipRoot.GetComponent<Image>();
        innerBgImg = root.Find("InnerBackground")?.GetComponent<Image>();

        Transform header = root.Find("HeaderRow");
        if (header != null)
        {
            itemIconBox = header.Find("IconBox")?.GetComponent<Image>();
            itemIcon = header.Find("IconBox/Icon")?.GetComponent<Image>();
            itemNameText = header.Find("TitleColumn/ItemName")?.GetComponent<TextMeshProUGUI>() ?? header.Find("InfoCol/ItemName")?.GetComponent<TextMeshProUGUI>();
            itemRarityBadge = header.Find("TitleColumn/BadgesRow/RarityBadgeBg/RarityBadge")?.GetComponent<TextMeshProUGUI>() ?? header.Find("InfoCol/BadgeRow/RarityBadge")?.GetComponent<TextMeshProUGUI>();
            itemTypeBadge = header.Find("TitleColumn/BadgesRow/TypeBadge")?.GetComponent<TextMeshProUGUI>() ?? header.Find("InfoCol/BadgeRow/TypeBadge")?.GetComponent<TextMeshProUGUI>();
        }

        divider1 = root.Find("Divider1")?.GetComponent<Image>();
        statsText = root.Find("StatsSection")?.GetComponent<TextMeshProUGUI>() ?? root.Find("StatsText")?.GetComponent<TextMeshProUGUI>();
        divider2 = root.Find("Divider2")?.GetComponent<Image>();
        descriptionText = root.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();

        cornerAccents = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            Transform corner = root.Find($"Corner_{i}");
            if (corner != null) cornerAccents[i] = corner.GetComponent<Image>();
        }
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

        // Optimization: If already displaying this item, skip redundant layout rebuilds
        if (isVisible && !isShowingCrop && currentItem == item && tooltipRoot != null && tooltipRoot.activeSelf)
        {
            return;
        }

        currentPlot = null;
        currentPlotStage = -1;
        currentPlotDaysInStage = -1;

        EnsureUIReady();

        bool wasHidden = !isVisible || tooltipRoot == null || !tooltipRoot.activeSelf || currentAlpha <= 0.05f;
        currentItem = item;
        isShowingCrop = false;
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

    public void ShowCrop(FarmPlot plot)
    {
        if (plot == null || plot.currentCrop == null || plot.currentState == FarmPlot.State.Empty)
        {
            Hide();
            return;
        }

        // Optimization: If already displaying this plot with identical stage/growth, skip heavy rebuild
        if (isVisible && isShowingCrop && currentPlot == plot &&
            currentPlotStage == plot.currentStage &&
            currentPlotState == plot.currentState &&
            currentPlotDaysInStage == plot.daysInCurrentStage &&
            tooltipRoot != null && tooltipRoot.activeSelf)
        {
            return;
        }

        currentPlot = plot;
        currentPlotStage = plot.currentStage;
        currentPlotState = plot.currentState;
        currentPlotDaysInStage = plot.daysInCurrentStage;

        EnsureUIReady();

        bool wasHidden = !isVisible || tooltipRoot == null || !tooltipRoot.activeSelf || currentAlpha <= 0.05f;
        currentItem = null;
        isShowingCrop = true;
        isVisible = true;

        PopulateCropData(plot);

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
                currentScale = 0.97f;
            }
        }
    }

    public void Hide(bool immediate = false)
    {
        isVisible = false;
        currentItem = null;
        isShowingCrop = false;
        currentPlot = null;
        currentPlotStage = -1;
        currentPlotDaysInStage = -1;

        if (immediate)
        {
            currentAlpha = 0f;
            currentScale = 0.94f;
            if (tooltipCG != null) tooltipCG.alpha = 0f;
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
        }
    }

    private void PopulateCropData(FarmPlot plot)
    {
        CropData crop = plot.currentCrop;
        bool isReady = plot.currentState == FarmPlot.State.Ready;

        // 1. Icon
        Sprite iconSp = null;
        if (crop.producedItem != null && crop.producedItem.itemIcon != null)
        {
            iconSp = crop.producedItem.itemIcon;
        }
        else if (crop.growthStages != null && crop.growthStages.Length > 0)
        {
            iconSp = crop.growthStages[crop.growthStages.Length - 1];
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = iconSp;
            itemIcon.gameObject.SetActive(iconSp != null);
        }

        // 2. Title
        string starPrefix = isReady ? "★ " : "";
        string displayName = !string.IsNullOrEmpty(crop.cropName) ? crop.cropName : crop.name;
        if (itemNameText != null)
        {
            itemNameText.text = $"{starPrefix}{displayName}";
        }

        // 3. Badges
        if (itemRarityBadge != null)
        {
            itemRarityBadge.text = isReady ? "CHÍN VÀNG" : "CÂY TRỒNG";
        }

        int totalStages = (crop.growthStages != null && crop.growthStages.Length > 0) ? crop.growthStages.Length : 1;
        int currentStageNum = Mathf.Clamp(plot.currentStage + 1, 1, totalStages);
        if (itemTypeBadge != null)
        {
            itemTypeBadge.text = isReady ? "SẴN SÀNG" : $"GIAI ĐOẠN {currentStageNum}/{totalStages}";
        }

        // 4. Stats & Information
        StringBuilder sb = new StringBuilder();
        int harvestAmt = Mathf.Max(1, crop.harvestAmount);
        string prodName = crop.producedItem != null ? crop.producedItem.itemName : crop.cropName;
        sb.AppendLine($"<color=#CBD5E1>Thu hoạch được:</color>  <color=#FFD043><b>+{harvestAmt} {prodName}</b></color>");

        if (isReady)
        {
            sb.AppendLine("<color=#CBD5E1>Thời gian:</color>  <color=#50FA7B><b>★ Sẵn sàng thu hoạch ngay!</b></color>");
        }
        else
        {
            int maxStage = totalStages - 1;
            int daysRemaining = Mathf.Max(1, (maxStage - plot.currentStage) * Mathf.Max(1, crop.growthTime) - plot.daysInCurrentStage);
            sb.AppendLine($"<color=#CBD5E1>Còn lại:</color>  <color=#77CCFF><b>~{daysRemaining} ngày (lần ngủ)</b></color>");
        }

        string dots = "";
        for (int i = 0; i < totalStages; i++)
        {
            dots += (i < currentStageNum) ? "<color=#50FA7B>●</color> " : "<color=#64748B>○</color> ";
        }
        sb.AppendLine($"<color=#CBD5E1>Sinh trưởng:</color>  [ {dots.Trim()} ]");

        if (statsText != null)
        {
            statsText.text = sb.ToString().TrimEnd();
            statsText.gameObject.SetActive(true);
        }
        if (divider1 != null) divider1.gameObject.SetActive(true);

        // 5. Description / Guide
        if (descriptionText != null)
        {
            if (isReady)
            {
                descriptionText.text = "<color=#50FA7B>Cây đã chín vàng rực rỡ! Click Chuột Trái để thu hoạch nông sản vào ba lô.</color>";
            }
            else
            {
                if (crop.producedItem != null && !string.IsNullOrEmpty(crop.producedItem.description))
                {
                    descriptionText.text = crop.producedItem.description;
                }
                else
                {
                    descriptionText.text = "Cây đang sinh trưởng và lớn lên mỗi ngày. Hãy ngủ đủ giấc hoặc thám hiểm hầm ngục để cây mau chín!";
                }
            }
            descriptionText.gameObject.SetActive(true);
        }
        if (divider2 != null) divider2.gameObject.SetActive(true);

        // 6. Style application
        Color borderColor = isReady ? EpicBorder : CropBorder;
        Color bgColor = isReady ? EpicBg : CropBg;
        Color textColor = isReady ? EpicText : CropText;
        Color badgeColor = isReady ? EpicBadge : CropBadge;

        if (outerFrameImg != null)
        {
            Color gotyTint = isReady ? Color.white : new Color(0.86f, 1f, 0.90f, 1f);
            if (DungeonUIAssetHelper.ApplyCompSprite(outerFrameImg, "Comp_Tooltip_ItemBase", sliced: true, gotyTint))
            {
                if (innerBgImg != null) innerBgImg.gameObject.SetActive(false);
            }
            else
            {
                DungeonUIAssetHelper.StyleSlicedFrame(outerFrameImg, borderColor);
                if (innerBgImg != null) { innerBgImg.gameObject.SetActive(true); innerBgImg.color = bgColor; }
            }
        }
        if (itemIconBox != null) DungeonUIAssetHelper.ApplyCompSprite(itemIconBox, "Comp_Tooltip_IconBox", sliced: true, Color.white);
        if (itemNameText != null) itemNameText.color = textColor;
        if (itemRarityBadge != null) itemRarityBadge.color = badgeColor;
        if (divider1 != null)
        {
            if (!DungeonUIAssetHelper.ApplyCompSprite(divider1, "Comp_Tooltip_Divider", sliced: true, new Color(borderColor.r, borderColor.g, borderColor.b, 0.7f)))
                divider1.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0.45f);
        }
        if (divider2 != null)
        {
            if (!DungeonUIAssetHelper.ApplyCompSprite(divider2, "Comp_Tooltip_Divider", sliced: true, new Color(borderColor.r, borderColor.g, borderColor.b, 0.45f)))
                divider2.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0.25f);
        }
        if (cornerAccents != null)
        {
            for (int i = 0; i < cornerAccents.Length; i++)
            {
                if (cornerAccents[i] != null) cornerAccents[i].color = badgeColor;
            }
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
                sb.AppendLine($"<color=#CBD5E1>Hồi phục Máu:</color>  <color=#FF6B6B><b>+{food.healthValue} Máu</b></color>");
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
            sb.AppendLine("<color=#CBD5E1>Hồi phục ăn thô:</color>  <color=#FF6B6B><b>+6 Máu</b></color> <color=#94A3B8>|</color> <color=#FFD043><b>+15 Độ No</b></color>");
            sb.AppendLine("<color=#CBD5E1>Công dụng chính:</color>  <color=#F1F5F9>Nấu ăn & Dâng lễ Tượng</color>");
        }
        else if (item.itemType == ItemData.ItemType.Seed)
        {
            sb.AppendLine("<color=#CBD5E1>Loại hạt giống:</color>  <color=#38BDF8><b>Trồng tại Nông Trại</b></color>");
            sb.AppendLine("<color=#CBD5E1>Sản lượng vụ:</color>  <color=#4ADE80><b>~3 Nông Sản / Lần thu</b></color>");
        }
        else if (rarity == ItemData.ItemRarity.Epic)
        {
            string bossSource = (displayName.Contains("Vương Miện") || displayName.Contains("Slime")) ? "Slime Chúa (Trùm Tầng 2)" :
                                (displayName.Contains("Hoa Hồng") || displayName.Contains("Xương")) ? "Vua Xương Cổ Đại (Trùm Tầng 1)" :
                                (displayName.Contains("Hắc Huyết") || displayName.Contains("Tinh Thể")) ? "Kỵ Sĩ Bay Hắc Ám (Trùm Tầng 3)" : "Trùm Hầm Ngục";
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
            Color gotyTint = rarity switch
            {
                ItemData.ItemRarity.Rare => new Color(0.86f, 0.94f, 1.0f, 1f),
                _ => Color.white
            };
            if (DungeonUIAssetHelper.ApplyCompSprite(outerFrameImg, "Comp_Tooltip_ItemBase", sliced: true, gotyTint))
            {
                if (innerBgImg != null) innerBgImg.gameObject.SetActive(false);
            }
            else
            {
                DungeonUIAssetHelper.StyleSlicedFrame(outerFrameImg, borderColor);
                if (innerBgImg != null)
                {
                    innerBgImg.gameObject.SetActive(true);
                    innerBgImg.color = bgColor;
                }
            }
        }

        if (itemIconBox != null)
        {
            DungeonUIAssetHelper.ApplyCompSprite(itemIconBox, "Comp_Tooltip_IconBox", sliced: true, Color.white);
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
            if (!DungeonUIAssetHelper.ApplyCompSprite(divider1, "Comp_Tooltip_Divider", sliced: true, new Color(borderColor.r, borderColor.g, borderColor.b, 0.7f)))
                divider1.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0.45f);
        }

        if (divider2 != null)
        {
            if (!DungeonUIAssetHelper.ApplyCompSprite(divider2, "Comp_Tooltip_Divider", sliced: true, new Color(borderColor.r, borderColor.g, borderColor.b, 0.45f)))
                divider2.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0.25f);
        }

        if (cornerAccents != null)
        {
            bool useGotyFrame = outerFrameImg != null && outerFrameImg.sprite != null && outerFrameImg.sprite.name.StartsWith("Comp_");
            for (int i = 0; i < cornerAccents.Length; i++)
            {
                if (cornerAccents[i] != null)
                {
                    cornerAccents[i].gameObject.SetActive(!useGotyFrame);
                    cornerAccents[i].color = badgeColor;
                }
            }
        }
    }

    private void EnsureUIReady()
    {
        if (tooltipRoot == null)
        {
            BindSceneReferences();
        }
    }

    [System.Obsolete("UI hierarchy is now authored directly in the scene/prefab. Use BindSceneReferences instead.")]
    public void BuildUIHierarchy(Transform parent = null)
    {
        BindSceneReferences();
    }
}

