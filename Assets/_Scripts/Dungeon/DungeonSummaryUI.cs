using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện Tổng Kết Chuyến Thám Hiểm (Dungeon Expedition Summary UI).
/// Tự động hiển thị khi người chơi thoát khỏi Dungeon (Chiến thắng, Rút lui an toàn tại Camp, hoặc Tử trận).
/// - Thống kê chi tiết tầng, chặng vượt qua, trạng thái Boss, phước lành, lượng máu/độ no.
/// - Hiển thị toàn bộ chiến lợi phẩm mang về trong ba lô với ScrollView + Thanh cuộn slider dọc đẹp mắt, chống tràn.
/// - Thông báo rõ ràng về bảo hộ rơi đồ (KeepLootOnDefeat / Tân thủ) hoặc vật phẩm đã bảo toàn.
/// - Nút "Trở Về Nông Trại" hoàn tất chuyến đi và chuyển cảnh an toàn về Base.
/// </summary>
public class DungeonSummaryUI : MonoBehaviour
{
    public enum ExpeditionResult
    {
        Victory,    // Tiêu diệt Boss hoặc hoàn thành tầng
        Retreat,    // Chủ động rút lui an toàn tại Camp
        Defeat      // Hết máu / Tử trận
    }

    #region Singleton & Factory
    private static DungeonSummaryUI _instance;
    public static DungeonSummaryUI Instance => _instance;
    public static bool HasConfirmedReturn { get; set; } = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        HasConfirmedReturn = false;
    }

    public static DungeonSummaryUI EnsureInstance()
    {
        if (_instance != null)
        {
            _instance.EnsureUIBuilt();
            return _instance;
        }

        DungeonSummaryUI found = FindFirstObjectByType<DungeonSummaryUI>(FindObjectsInactive.Include);
        if (found != null)
        {
            _instance = found;
            _instance.EnsureUIBuilt();
            return _instance;
        }

        Canvas c = DungeonUIAssetHelper.GetMainScreenCanvas();
        if (c != null)
        {
            GameObject go = new("DungeonSummaryUI_Manager", typeof(DungeonSummaryUI));
            go.transform.SetParent(c.transform, false);
            _instance = go.GetComponent<DungeonSummaryUI>();
            _instance.canvas = c;
            _instance.EnsureUIBuilt();
            return _instance;
        }

        return null;
    }
    #endregion

    #region UI Elements
    [Header("Panels")]
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Transform itemsGridContainer;
    [SerializeField] private ScrollRect itemsScrollRect;

    [Header("Header Elements")]
    [SerializeField] private Image headerBannerImg;
    [SerializeField] private Image headerIcon;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Left Stats Elements")]
    [SerializeField] private TextMeshProUGUI statFloorText;
    [SerializeField] private TextMeshProUGUI statStagesText;
    [SerializeField] private TextMeshProUGUI statBossText;
    [SerializeField] private TextMeshProUGUI statHealthText;
    [SerializeField] private TextMeshProUGUI statBlessingText;
    [SerializeField] private TextMeshProUGUI statDayTransitionText;
    [SerializeField] private TextMeshProUGUI specialNoticeText;

    [Header("Right Loot Header")]
    [SerializeField] private TextMeshProUGUI lootHeaderTitle;

    [Header("Controls")]
    [SerializeField] private Button returnToBaseButton;
    [SerializeField] private TextMeshProUGUI returnButtonText;

    private readonly List<GameObject> activeItemCards = new();
    private GameObject emptyLootNoticeGO;
    private Action onConfirmedReturnCallback;
    private ExpeditionResult currentResult = ExpeditionResult.Retreat;

    public bool IsOpen => summaryPanel != null && summaryPanel.activeSelf;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        EnsureUIBuilt();

        if (summaryPanel != null)
        {
            summaryPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void Update()
    {
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                OnReturnClicked();
            }
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Hiển thị bảng tổng kết chuyến thám hiểm với kết quả tương ứng.
    /// </summary>
    public void Show(ExpeditionResult result, Action onConfirmReturn = null)
    {
        EnsureUIBuilt();
        if (summaryPanel == null) return;

        HasConfirmedReturn = false;
        currentResult = result;
        onConfirmedReturnCallback = onConfirmReturn;

        summaryPanel.SetActive(true);
        summaryPanel.transform.SetAsLastSibling();

        PopulateExpeditionData(result);

        if (itemsScrollRect != null)
        {
            itemsScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void Close()
    {
        if (summaryPanel != null)
        {
            summaryPanel.SetActive(false);
        }
    }
    #endregion

    #region Data Population
    private void PopulateExpeditionData(ExpeditionResult result)
    {
        // 1. Gather Systems Data
        int floorNum = 1;
        int stagesCleared = 0;
        int totalStages = 5;
        bool bossDefeated = false;

        WaveManager wm = WaveManager.Instance != null ? WaveManager.Instance : FindFirstObjectByType<WaveManager>();
        ProgressionManager pm = ProgressionManager.Instance;
        PlayerStats ps = PlayerStats.Instance != null ? PlayerStats.Instance : FindFirstObjectByType<PlayerStats>();

        if (wm != null)
        {
            FloorData activeFloor = wm.GetActiveFloor();
            floorNum = activeFloor != null ? activeFloor.floorNumber : (pm != null ? pm.currentFloor : 1);
            stagesCleared = Mathf.Max(0, wm.GetCurrentWaveIndex());
            totalStages = wm.GetTotalStages();
        }
        else if (pm != null)
        {
            floorNum = pm.currentFloor;
        }

        if (pm != null)
        {
            bossDefeated = pm.IsBossDefeated(floorNum);
        }

        if (result == ExpeditionResult.Victory)
        {
            bossDefeated = true;
            stagesCleared = totalStages;
        }

        // 2. Style Header
        ApplyHeaderTheme(result, floorNum);

        // 3. Stats Column
        if (statFloorText != null)
        {
            statFloorText.text = $"🏰 <b>Tầng Thám Hiểm:</b> <color=#FFE066>Tầng {floorNum}</color>";
        }

        if (statStagesText != null)
        {
            statStagesText.text = $"⚔️ <b>Chặng Đã Vượt:</b> <color=#A0E8AF>{stagesCleared}/{totalStages} Chặng</color>";
        }

        if (statBossText != null)
        {
            if (bossDefeated)
            {
                statBossText.text = "👑 <b>Thủ Lĩnh (Boss):</b> <color=#FFD700>ĐÃ TIÊU DIỆT 🏆</color>";
            }
            else if (result == ExpeditionResult.Retreat)
            {
                statBossText.text = "👑 <b>Thủ Lĩnh (Boss):</b> <color=#88CCFF>Rút lui an toàn</color>";
            }
            else
            {
                statBossText.text = "👑 <b>Thủ Lĩnh (Boss):</b> <color=#FF7777>Chưa đánh bại</color>";
            }
        }

        if (statHealthText != null && ps != null)
        {
            statHealthText.text = $"❤️ <b>Sinh Lực:</b> {ps.currentHealth}/{ps.maxHealth} HP   |   🍖 <b>Độ No:</b> {ps.currentHunger}/{ps.maxHunger}";
        }
        else if (statHealthText != null)
        {
            statHealthText.text = "❤️ <b>Sinh Lực:</b> Bình an";
        }

        if (statBlessingText != null)
        {
            if (pm != null && pm.activeBlessing != BlessingType.None)
            {
                BlessingInfo info = BlessingDatabase.GetBlessingInfo(pm.activeBlessing);
                string bName = info != null ? info.title : pm.activeBlessing.ToString();
                statBlessingText.text = $"✨ <b>Phước Lành:</b> <color=#88FFDD>{bName}</color>";
            }
            else
            {
                statBlessingText.text = "✨ <b>Phước Lành:</b> <color=#AAAAAA>Không kích hoạt</color>";
            }
        }

        // 4. Day Transition Notice
        if (statDayTransitionText != null)
        {
            int curDay = DayManager.Instance != null ? DayManager.Instance.currentDay : PlayerPrefs.GetInt("Farm_CurrentDay", 1);
            int nextDay = curDay + 1;
            statDayTransitionText.text = $"📅 <b>Thời Gian:</b> <color=#FFE066>Sang Ngày Mới ({nextDay})</color> <i>(+1 Ngày)</i>";
        }

        // 4. Special Protection Notice
        if (specialNoticeText != null)
        {
            bool hasKeepLoot = pm != null && pm.HasBlessing(BlessingType.KeepLootOnDefeat);
            bool isStarter = pm != null && pm.IsStarterProtectionActive();

            if (result == ExpeditionResult.Victory)
            {
                specialNoticeText.text = "<color=#66FF88>🌟 HOÀN THÀNH XUẤT SẮC!</color>\nToàn bộ chiến lợi phẩm, kho báu hoàng kim và nông sản đã được chuyển an toàn về trang trại.";
            }
            else if (result == ExpeditionResult.Retreat)
            {
                specialNoticeText.text = "<color=#66E0FF>🏕️ RÚT LUI CHIẾN THUẬT AN TOÀN!</color>\nBạn đã nghỉ ngơi tại Doanh Trại và trở về căn cứ bình an vô sự. Giữ nguyên 100% tài sản trong túi đồ.";
            }
            else // Defeat
            {
                if (hasKeepLoot || isStarter)
                {
                    string reason = isStarter ? "Bảo Hộ Tân Thủ Thần Linh" : "Phước Lành Bảo Hộ";
                    specialNoticeText.text = $"<color=#66FF88>🛡️ {reason} ĐÃ KÍCH HOẠT!</color>\nDù tử trận nhưng bạn được che chở: <b>Giữ nguyên 100% vật phẩm trong ba lô</b> mang về nhà!";
                }
                else
                {
                    specialNoticeText.text = "<color=#FF6666>⚠️ BẠN ĐÃ TỬ TRẬN TRONG DUNGEON!</color>\nBạn đã kiệt sức trước lũ quái vật. Hãy chuẩn bị thêm lương thực và phước lành trước chuyến đi kế tiếp.";
                }
            }
        }

        // 5. Populate Loot Grid
        PopulateLootGrid(ps != null ? ps.itemContainer : null);
    }

    private void ApplyHeaderTheme(ExpeditionResult result, int floor)
    {
        Color bannerColor;
        string t;
        string sub;
        Sprite iconSp = null;

        switch (result)
        {
            case ExpeditionResult.Victory:
                bannerColor = new Color(0.28f, 0.22f, 0.08f, 1f);
                t = $"🏆 TỔNG KẾT: CHIẾN THẮNG TẦNG {floor}!";
                sub = "Bạn đã chinh phục thành công toàn bộ quái vật và đánh bại Chúa Tể Ngục Tối!";
                iconSp = DungeonUIAssetHelper.GetChestSprite();
                break;

            case ExpeditionResult.Retreat:
                bannerColor = new Color(0.08f, 0.24f, 0.28f, 1f);
                t = $"🏕️ TỔNG KẾT: RÚT LUI AN TOÀN - TẦNG {floor}";
                sub = "Bạn đã chủ động rút lui từ Doanh Trại, bảo toàn vẹn nguyên toàn bộ tài nguyên.";
                iconSp = DungeonUIAssetHelper.GetTentSprite();
                break;

            case ExpeditionResult.Defeat:
            default:
                bannerColor = new Color(0.30f, 0.10f, 0.10f, 1f);
                t = $"💀 TỔNG KẾT: TỬ TRẬN TẠI TẦNG {floor}";
                sub = "Chuyến phiêu lưu phải dừng lại trước hiểm nguy trong lòng đất. Hãy rèn luyện và phục thù!";
                iconSp = DungeonUIAssetHelper.GetBossSprite();
                break;
        }

        if (headerBannerImg != null)
        {
            DungeonUIAssetHelper.PlaqueStyle pStyle = result switch
            {
                ExpeditionResult.Victory => DungeonUIAssetHelper.PlaqueStyle.Victory,
                ExpeditionResult.Retreat => DungeonUIAssetHelper.PlaqueStyle.Gold,
                _ => DungeonUIAssetHelper.PlaqueStyle.Defeat
            };
            DungeonUIAssetHelper.StylePlaque(headerBannerImg, pStyle);
        }

        if (titleText != null)
        {
            titleText.text = t;
            titleText.color = result == ExpeditionResult.Victory ? new Color(1f, 0.88f, 0.35f) : (result == ExpeditionResult.Retreat ? new Color(0.6f, 0.95f, 1f) : new Color(1f, 0.65f, 0.65f));
        }

        if (subtitleText != null)
        {
            subtitleText.text = sub;
        }

        if (headerIcon != null && iconSp != null)
        {
            headerIcon.sprite = iconSp;
            headerIcon.gameObject.SetActive(true);
        }
    }

    private void PopulateLootGrid(ItemContainer container)
    {
        for (int i = 0; i < activeItemCards.Count; i++)
        {
            if (activeItemCards[i] != null) Destroy(activeItemCards[i]);
        }
        activeItemCards.Clear();

        if (itemsGridContainer == null) return;

        List<ItemSlot> validSlots = new();
        if (container != null && container.itemSlots != null)
        {
            for (int i = 0; i < container.itemSlots.Length; i++)
            {
                ItemSlot slot = container.itemSlots[i];
                if (slot != null && slot.itemData != null && slot.amount > 0)
                {
                    validSlots.Add(slot);
                }
            }
        }

        if (lootHeaderTitle != null)
        {
            lootHeaderTitle.text = $"CHIẾN LỢI PHẨM MANG VỀ ({validSlots.Count} MÓN ĐỒ)";
        }

        if (validSlots.Count == 0)
        {
            if (emptyLootNoticeGO == null)
            {
                emptyLootNoticeGO = new GameObject("EmptyLootNotice", typeof(RectTransform), typeof(TextMeshProUGUI));
                emptyLootNoticeGO.transform.SetParent(itemsGridContainer, false);
                RectTransform emptyRT = emptyLootNoticeGO.GetComponent<RectTransform>();
                emptyRT.sizeDelta = new Vector2(440, 60);

                TextMeshProUGUI tmp = emptyLootNoticeGO.GetComponent<TextMeshProUGUI>();
                DungeonUIAssetHelper.ApplyFont(tmp);
                tmp.fontSize = 13.5f;
                tmp.color = new Color(0.7f, 0.75f, 0.85f);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.text = "<i>Không có vật phẩm nào trong túi đồ chuyến này.</i>";
            }
            emptyLootNoticeGO.SetActive(true);
            return;
        }

        if (emptyLootNoticeGO != null)
        {
            emptyLootNoticeGO.SetActive(false);
        }

        for (int i = 0; i < validSlots.Count; i++)
        {
            ItemSlot slot = validSlots[i];
            GameObject card = CreateLootCard(slot.itemData, slot.amount);
            if (card != null)
            {
                activeItemCards.Add(card);
            }
        }
    }

    private GameObject CreateLootCard(ItemData item, int amount)
    {
        if (item == null || itemsGridContainer == null) return null;

        // Card Container
        GameObject cardGO = new($"Item_{item.itemName}", typeof(RectTransform), typeof(Image));
        cardGO.transform.SetParent(itemsGridContainer, false);
        RectTransform rt = cardGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(210, 52);

        Image bg = cardGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(bg, DungeonUIAssetHelper.PanelStyle.Wood);

        // Slot Frame for Icon
        GameObject slotFrameGO = new("IconSlot", typeof(RectTransform), typeof(Image));
        slotFrameGO.transform.SetParent(cardGO.transform, false);
        RectTransform sfRT = slotFrameGO.GetComponent<RectTransform>();
        sfRT.anchorMin = new Vector2(0, 0.5f);
        sfRT.anchorMax = new Vector2(0, 0.5f);
        sfRT.pivot = new Vector2(0, 0.5f);
        sfRT.anchoredPosition = new Vector2(8, 0);
        sfRT.sizeDelta = new Vector2(38, 38);
        Image sfImg = slotFrameGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(sfImg, false);

        // Icon Graphic
        if (item.itemIcon != null)
        {
            GameObject iconGO = new("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(slotFrameGO.transform, false);
            RectTransform iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.sizeDelta = new Vector2(-6, -6);
            Image iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = item.itemIcon;
            iconImg.preserveAspect = true;
        }

        // Title and Amount
        GameObject labelGO = new("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(cardGO.transform, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.offsetMin = new Vector2(52, 4);
        labelRT.offsetMax = new Vector2(-8, -4);

        TextMeshProUGUI labelTMP = labelGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(labelTMP);
        labelTMP.fontSize = 11.5f;
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;

        string tag = item.itemType switch
        {
            ItemData.ItemType.Produce => "<color=#88CCFF>[Nông Sản]</color>",
            ItemData.ItemType.Seed => "<color=#88FF88>[Hạt Giống]</color>",
            ItemData.ItemType.Food => "<color=#FFAA44>[Món Ăn]</color>",
            _ => "<color=#E0E0E0>[Vật Phẩm]</color>"
        };

        labelTMP.text = $"<b>{item.itemName}</b>\n{tag} <color=#FFDD44>x{amount}</color>";

        return cardGO;
    }
    #endregion

    #region Action Handlers
    private void OnReturnClicked()
    {
        HasConfirmedReturn = true;
        Close();

        // Tự động chuyển qua 1 ngày và phục hồi thể lực sau chuyến thám hiểm
        AdvanceDayOnReturnStatic();

        if (onConfirmedReturnCallback != null)
        {
            onConfirmedReturnCallback.Invoke();
            onConfirmedReturnCallback = null;
            return;
        }

        // Fallback default: load Base scene
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadBase();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Base");
        }
    }

    /// <summary>
    /// Phương thức dùng chung để tự động qua 1 ngày, cập nhật ruộng lúa và hồi thể lực khi kết thúc chuyến đi.
    /// Có thể gọi trực tiếp hoặc từ bất kỳ hệ thống thoát hầm ngục nào.
    /// </summary>
    public static void AdvanceDayOnReturnStatic()
    {
        // Đảm bảo đánh dấu chuyến đi kết thúc để không bị kích hoạt lặp lại 2 lần khi chuyển scene
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.runActive = false;
            ProgressionManager.Instance.ClearRunBlessing();
        }

        // Chuyển sang ngày mới: Kích hoạt tăng trưởng cây trồng trên ruộng (+1 ngày) và reset boss cooldown
        DayManager dm = DayManager.Instance != null ? DayManager.Instance : FindFirstObjectByType<DayManager>();
        if (dm != null)
        {
            dm.AdvanceDay();
        }
        else
        {
            int curDay = PlayerPrefs.GetInt("Farm_CurrentDay", 1);
            PlayerPrefs.SetInt("Farm_CurrentDay", curDay + 1);
            PlayerPrefs.Save();
        }
        Debug.Log("[DungeonSummaryUI] Đã hoàn tất chuyến thám hiểm: Tự động qua 1 ngày (Cây trồng trên ruộng lớn thêm +1 ngày tuổi)!");

        // 2. Hồi phục thể lực cơ bản khi qua ngày (ngủ nghỉ sau khi trở về)
        PlayerStats ps = PlayerStats.Instance != null ? PlayerStats.Instance : FindFirstObjectByType<PlayerStats>();
        if (ps != null)
        {
            int minHP = Mathf.RoundToInt(ps.maxHealth * 0.5f);
            if (ps.currentHealth < minHP)
            {
                ps.currentHealth = minHP;
            }
            else
            {
                ps.Heal(25);
            }

            if (ps.currentHunger < 35)
            {
                ps.currentHunger = 35;
            }

            ps.ClearTemporaryBuffs();
        }

        // 3. Kết thúc run trạng thái trong ProgressionManager
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.runActive = false;
            ProgressionManager.Instance.ClearRunBlessing();
        }
    }
    #endregion

    #region Procedural UI Building
    private void EnsureUIBuilt()
    {
        if (summaryPanel != null) return;

        if (canvas == null)
        {
            canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        }
        if (canvas == null) return;

        // 1. Dark Fullscreen Overlay
        GameObject overlayGO = new("DungeonSummaryPanel", typeof(RectTransform), typeof(Image));
        overlayGO.transform.SetParent(canvas.transform, false);
        summaryPanel = overlayGO;

        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;

        Image overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0.02f, 0.03f, 0.06f, 0.94f);

        // 2. Main Window Modal Frame (900 x 580)
        GameObject modalGO = new("SummaryModal", typeof(RectTransform), typeof(Image));
        modalGO.transform.SetParent(overlayGO.transform, false);
        RectTransform modalRT = modalGO.GetComponent<RectTransform>();
        modalRT.anchorMin = new Vector2(0.5f, 0.5f);
        modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(900, 580);

        Image modalImg = modalGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(modalImg, DungeonUIAssetHelper.PanelStyle.Wood);

        // 3. Header Section (880 x 76)
        GameObject headerGO = new("Header", typeof(RectTransform), typeof(Image));
        headerGO.transform.SetParent(modalGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = new Vector2(0, -6);
        headerRT.sizeDelta = new Vector2(-12, 74);

        headerBannerImg = headerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StylePlaque(headerBannerImg, DungeonUIAssetHelper.PlaqueStyle.Gold);

        // Header Icon
        GameObject iconGO = new("HeaderIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(headerGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(24, 0);
        iconRT.sizeDelta = new Vector2(50, 50);
        headerIcon = iconGO.GetComponent<Image>();
        headerIcon.preserveAspect = true;

        // Title Text
        GameObject titleGO = new("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(headerGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = new Vector2(86, 24);
        titleRT.offsetMax = new Vector2(-20, -6);
        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.text = "TỔNG KẾT CHUYẾN THÁM HIỂM DUNGEON";

        // Subtitle Text
        GameObject subGO = new("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGO.transform.SetParent(headerGO.transform, false);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 0);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.offsetMin = new Vector2(86, 6);
        subRT.offsetMax = new Vector2(-20, -36);
        subtitleText = subGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(subtitleText);
        subtitleText.fontSize = 12;
        subtitleText.color = new Color(0.85f, 0.88f, 0.95f);
        subtitleText.alignment = TextAlignmentOptions.MidlineLeft;
        subtitleText.text = "Chi tiết tiến độ, số chặng vượt qua và toàn bộ chiến lợi phẩm thu thập được.";

        // 4. Body Area (2 Columns)
        GameObject bodyGO = new("Body", typeof(RectTransform));
        bodyGO.transform.SetParent(modalGO.transform, false);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(12, 64);
        bodyRT.offsetMax = new Vector2(-12, -88);

        // --- Left Column: Statistics & Highlights (Width: 360) ---
        GameObject leftColGO = new("LeftColumn_Stats", typeof(RectTransform), typeof(Image));
        leftColGO.transform.SetParent(bodyGO.transform, false);
        RectTransform leftColRT = leftColGO.GetComponent<RectTransform>();
        leftColRT.anchorMin = new Vector2(0, 0);
        leftColRT.anchorMax = new Vector2(0.42f, 1);
        leftColRT.offsetMin = Vector2.zero;
        leftColRT.offsetMax = new Vector2(-6, 0);
        Image leftColBg = leftColGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(leftColBg, DungeonUIAssetHelper.PanelStyle.Slate);

        // Left Header
        GameObject leftHeaderGO = new("LeftHeader", typeof(RectTransform), typeof(Image));
        leftHeaderGO.transform.SetParent(leftColGO.transform, false);
        RectTransform lhRT = leftHeaderGO.GetComponent<RectTransform>();
        lhRT.anchorMin = new Vector2(0, 1);
        lhRT.anchorMax = new Vector2(1, 1);
        lhRT.pivot = new Vector2(0.5f, 1);
        lhRT.sizeDelta = new Vector2(-12, 30);
        lhRT.anchoredPosition = new Vector2(0, -6);
        DungeonUIAssetHelper.StylePlaque(leftHeaderGO.GetComponent<Image>(), DungeonUIAssetHelper.PlaqueStyle.Wood);

        GameObject lhTxtGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        lhTxtGO.transform.SetParent(leftHeaderGO.transform, false);
        RectTransform lhTxtRT = lhTxtGO.GetComponent<RectTransform>();
        lhTxtRT.anchorMin = Vector2.zero;
        lhTxtRT.anchorMax = Vector2.one;
        lhTxtRT.offsetMin = new Vector2(8, 0);
        lhTxtRT.offsetMax = new Vector2(-8, 0);
        TextMeshProUGUI lhTmp = lhTxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(lhTmp);
        lhTmp.fontSize = 12.5f;
        lhTmp.fontStyle = FontStyles.Bold;
        lhTmp.color = new Color(1f, 0.85f, 0.35f);
        lhTmp.alignment = TextAlignmentOptions.MidlineLeft;
        lhTmp.text = "THỐNG KÊ CHIẾN LƯỢC";

        // Stats List Container
        GameObject statsContentGO = new("StatsList", typeof(RectTransform), typeof(VerticalLayoutGroup));
        statsContentGO.transform.SetParent(leftColGO.transform, false);
        RectTransform scRT = statsContentGO.GetComponent<RectTransform>();
        scRT.anchorMin = new Vector2(0, 0);
        scRT.anchorMax = new Vector2(1, 1);
        scRT.offsetMin = new Vector2(10, 10);
        scRT.offsetMax = new Vector2(-10, -42);

        VerticalLayoutGroup scVLG = statsContentGO.GetComponent<VerticalLayoutGroup>();
        scVLG.spacing = 8;
        scVLG.childControlWidth = true;
        scVLG.childControlHeight = false;
        scVLG.childForceExpandWidth = true;
        scVLG.childForceExpandHeight = false;

        statFloorText = CreateStatLine(statsContentGO.transform, "StatFloor", "🏰 Tầng: 1");
        statStagesText = CreateStatLine(statsContentGO.transform, "StatStages", "⚔️ Chặng: 0/5");
        statBossText = CreateStatLine(statsContentGO.transform, "StatBoss", "👑 Thủ Lĩnh: Chưa gặp");
        statHealthText = CreateStatLine(statsContentGO.transform, "StatHealth", "❤️ Sinh Lực: 100/100 HP");
        statBlessingText = CreateStatLine(statsContentGO.transform, "StatBlessing", "✨ Phước Lành: Không có");
        statDayTransitionText = CreateStatLine(statsContentGO.transform, "StatDay", "📅 Thời Gian: Sang Ngày Mới (+1 Ngày)");

        // Notice Box at Bottom of Left Column
        GameObject noticeBoxGO = new("NoticeBox", typeof(RectTransform), typeof(Image));
        noticeBoxGO.transform.SetParent(statsContentGO.transform, false);
        RectTransform nbRT = noticeBoxGO.GetComponent<RectTransform>();
        nbRT.sizeDelta = new Vector2(0, 110);
        Image nbImg = noticeBoxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(nbImg, DungeonUIAssetHelper.PanelStyle.Wood);

        GameObject noticeTxtGO = new("NoticeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        noticeTxtGO.transform.SetParent(noticeBoxGO.transform, false);
        RectTransform ntRT = noticeTxtGO.GetComponent<RectTransform>();
        ntRT.anchorMin = Vector2.zero;
        ntRT.anchorMax = Vector2.one;
        ntRT.offsetMin = new Vector2(8, 6);
        ntRT.offsetMax = new Vector2(-8, -6);
        specialNoticeText = noticeTxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(specialNoticeText);
        specialNoticeText.fontSize = 11.5f;
        specialNoticeText.color = Color.white;
        specialNoticeText.alignment = TextAlignmentOptions.TopLeft;

        // --- Right Column: Secured Loot with Scrollbar Slider ---
        GameObject rightColGO = new("RightColumn_Loot", typeof(RectTransform), typeof(Image));
        rightColGO.transform.SetParent(bodyGO.transform, false);
        RectTransform rightColRT = rightColGO.GetComponent<RectTransform>();
        rightColRT.anchorMin = new Vector2(0.42f, 0);
        rightColRT.anchorMax = new Vector2(1, 1);
        rightColRT.offsetMin = new Vector2(6, 0);
        rightColRT.offsetMax = Vector2.zero;
        Image rightColBg = rightColGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedPanel(rightColBg, DungeonUIAssetHelper.PanelStyle.Slate);

        // Right Header
        GameObject rightHeaderGO = new("RightHeader", typeof(RectTransform), typeof(Image));
        rightHeaderGO.transform.SetParent(rightColGO.transform, false);
        RectTransform rhRT = rightHeaderGO.GetComponent<RectTransform>();
        rhRT.anchorMin = new Vector2(0, 1);
        rhRT.anchorMax = new Vector2(1, 1);
        rhRT.pivot = new Vector2(0.5f, 1);
        rhRT.sizeDelta = new Vector2(-12, 30);
        rhRT.anchoredPosition = new Vector2(0, -6);
        DungeonUIAssetHelper.StyleSlicedFrame(rightHeaderGO.GetComponent<Image>(), new Color(0.18f, 0.22f, 0.30f, 1f));

        GameObject rhTxtGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        rhTxtGO.transform.SetParent(rightHeaderGO.transform, false);
        RectTransform rhTxtRT = rhTxtGO.GetComponent<RectTransform>();
        rhTxtRT.anchorMin = Vector2.zero;
        rhTxtRT.anchorMax = Vector2.one;
        rhTxtRT.offsetMin = new Vector2(8, 0);
        rhTxtRT.offsetMax = new Vector2(-8, 0);
        lootHeaderTitle = rhTxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(lootHeaderTitle);
        lootHeaderTitle.fontSize = 12.5f;
        lootHeaderTitle.fontStyle = FontStyles.Bold;
        lootHeaderTitle.color = new Color(0.4f, 0.85f, 1f);
        lootHeaderTitle.alignment = TextAlignmentOptions.MidlineLeft;
        lootHeaderTitle.text = "VẬT PHẨM MANG VỀ AN TOÀN";

        // Scroll Area for Loot Items
        GameObject scrollGO = new("LootScrollArea", typeof(RectTransform), typeof(ScrollRect));
        scrollGO.transform.SetParent(rightColGO.transform, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(8, 8);
        scrollRT.offsetMax = new Vector2(-8, -42);

        itemsScrollRect = scrollGO.GetComponent<ScrollRect>();
        itemsScrollRect.horizontal = false;
        itemsScrollRect.vertical = true;
        itemsScrollRect.scrollSensitivity = 25f;
        itemsScrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Viewport with RectMask2D
        GameObject vpGO = new("Viewport", typeof(RectTransform), typeof(RectMask2D));
        vpGO.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = vpGO.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = new Vector2(-16, 0); // Dành 16px cho thanh slider cuộn

        // Content Container with GridLayoutGroup
        GameObject contentGO = new("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(vpGO.transform, false);
        itemsGridContainer = contentGO.transform;
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = Vector2.zero;

        GridLayoutGroup glg = contentGO.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(230, 52);
        glg.spacing = new Vector2(8, 6);
        glg.padding = new RectOffset(6, 6, 6, 6);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        itemsScrollRect.viewport = vpRT;
        itemsScrollRect.content = contentRT;

        // Thanh slider dọc màu xanh lam ngọc sang trọng
        DungeonUIAssetHelper.CreateVerticalScrollbar(scrollGO.transform, itemsScrollRect, new Color(0.25f, 0.75f, 0.85f, 1f));

        // 5. Footer Controls
        GameObject footerGO = new("Footer", typeof(RectTransform));
        footerGO.transform.SetParent(modalGO.transform, false);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0, 0);
        footerRT.anchorMax = new Vector2(1, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.anchoredPosition = new Vector2(0, 12);
        footerRT.sizeDelta = new Vector2(-24, 46);

        // Button [TRỞ VỀ NÔNG TRẠI]
        GameObject btnGO = new("BtnReturnToBase", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(footerGO.transform, false);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0.5f);
        btnRT.anchorMax = new Vector2(1, 0.5f);
        btnRT.pivot = new Vector2(1, 0.5f);
        btnRT.anchoredPosition = Vector2.zero;
        btnRT.sizeDelta = new Vector2(260, 44);

        Image btnImg = btnGO.GetComponent<Image>();
        returnToBaseButton = btnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleTactileButton(returnToBaseButton, btnImg, DungeonUIAssetHelper.ButtonStyle.Victory);
        returnToBaseButton.onClick.AddListener(OnReturnClicked);

        GameObject btnTxtGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTxtGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTxtRT = btnTxtGO.GetComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero;
        btnTxtRT.anchorMax = Vector2.one;
        btnTxtRT.sizeDelta = Vector2.zero;
        returnButtonText = btnTxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(returnButtonText);
        returnButtonText.fontSize = 14;
        returnButtonText.fontStyle = FontStyles.Bold;
        returnButtonText.alignment = TextAlignmentOptions.Center;
        returnButtonText.text = "TRỞ VỀ NÔNG TRẠI  [ ➔ ]";
        returnButtonText.color = Color.white;
    }

    private TextMeshProUGUI CreateStatLine(Transform parent, string name, string defaultText)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 32);

        Image bg = go.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.10f, 0.13f, 0.19f, 0.95f));

        GameObject textGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        RectTransform txtRT = textGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(10, 0);
        txtRT.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.fontSize = 12;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.text = defaultText;

        return tmp;
    }
    #endregion
}
