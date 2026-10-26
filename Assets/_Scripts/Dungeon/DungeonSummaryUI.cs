using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dungeon Expedition Summary UI.
/// Automatically shown when player leaves the Dungeon (Victory, Safe Retreat at Camp, or Defeat).
/// - Detailed statistics on floor, stages cleared, Boss status, active blessing, HP/Fullness.
/// - Displays all loot secured in backpack with clean scrollview.
/// - Clear notice regarding protection (Divine Protection / Starter Protection) or lost loot.
/// - "Return to Farm" button completes the run and transitions back to Base.
/// </summary>
public class DungeonSummaryUI : MonoBehaviour
{
    public enum ExpeditionResult
    {
        Victory,    // Defeated Boss or cleared floor
        Retreat,    // Safely retreated at Camp
        Defeat      // Depleted HP / Fallen in battle
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
    [SerializeField] public GameObject summaryPanel;
    [SerializeField] public Canvas canvas;
    [SerializeField] public Transform itemsGridContainer;
    [SerializeField] public ScrollRect itemsScrollRect;

    [Header("Header Elements")]
    [SerializeField] public Image headerBannerImg;
    [SerializeField] public Image headerIcon;
    [SerializeField] public TextMeshProUGUI titleText;
    [SerializeField] public TextMeshProUGUI subtitleText;

    [Header("Left Stats Elements")]
    [SerializeField] public TextMeshProUGUI statFloorText;
    [SerializeField] public TextMeshProUGUI statStagesText;
    [SerializeField] public TextMeshProUGUI statBossText;
    [SerializeField] public TextMeshProUGUI statHealthText;
    [SerializeField] public TextMeshProUGUI statBlessingText;
    [SerializeField] public TextMeshProUGUI statDayTransitionText;
    [SerializeField] public TextMeshProUGUI specialNoticeText;

    [Header("Right Loot Header")]
    [SerializeField] public TextMeshProUGUI lootHeaderTitle;

    [Header("Controls")]
    [SerializeField] public Button returnToBaseButton;
    [SerializeField] public TextMeshProUGUI returnButtonText;

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

        if (returnToBaseButton != null)
        {
            returnToBaseButton.onClick.RemoveListener(OnReturnClicked);
            returnToBaseButton.onClick.AddListener(OnReturnClicked);
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
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UIEscapeManager.ConsumeEscape();
                OnReturnClicked();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                OnReturnClicked();
            }
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Shows the expedition summary modal with corresponding result.
    /// </summary>
    public void Show(ExpeditionResult result, Action onConfirmReturn = null)
    {
        EnsureUIBuilt();
        if (summaryPanel == null)
        {
            onConfirmReturn?.Invoke();
            return;
        }

        HasConfirmedReturn = false;
        currentResult = result;
        onConfirmedReturnCallback = onConfirmReturn;

        summaryPanel.SetActive(true);
        summaryPanel.transform.SetAsLastSibling();

        if (CombatUI.Instance != null && CombatUI.Instance.gameObject != null)
        {
            if (CombatUI.Instance.GetComponent<Canvas>() == null && (canvas == null || CombatUI.Instance.gameObject != canvas.gameObject))
            {
                CombatUI.Instance.gameObject.SetActive(false);
            }
            else
            {
                if (CombatUI.Instance.targetInfoPanel != null) CombatUI.Instance.targetInfoPanel.SetActive(false);
                if (CombatUI.Instance.foodPanel != null) CombatUI.Instance.foodPanel.SetActive(false);
                if (CombatUI.Instance.victoryPanel != null) CombatUI.Instance.victoryPanel.SetActive(false);
                if (CombatUI.Instance.defeatPanel != null) CombatUI.Instance.defeatPanel.SetActive(false);
                if (CombatUI.Instance.rewardPanel != null) CombatUI.Instance.rewardPanel.SetActive(false);
                CombatUI.Instance.enabled = false;
            }
        }
        if (CampUI.Instance != null)
        {
            CampUI.Instance.HideCampScreen();
        }
        if (FloorVictoryUI.Instance != null)
        {
            FloorVictoryUI.Instance.Close();
        }

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
            statFloorText.text = $"<b>[Exploration Floor]</b> <color=#FFE066>Floor {floorNum}</color>";
        }

        if (statStagesText != null)
        {
            statStagesText.text = $"<b>[Stages Cleared]</b> <color=#A0E8AF>{stagesCleared}/{totalStages} Stages</color>";
        }

        if (statBossText != null)
        {
            if (bossDefeated)
            {
                statBossText.text = "<b>[Boss Leader]</b> <color=#FFD700>DEFEATED</color>";
            }
            else if (result == ExpeditionResult.Retreat)
            {
                statBossText.text = "<b>[Boss Leader]</b> <color=#88CCFF>Safe Retreat</color>";
            }
            else
            {
                statBossText.text = "<b>[Boss Leader]</b> <color=#FF7777>Undefeated</color>";
            }
        }

        if (statHealthText != null && ps != null)
        {
            statHealthText.text = $"<b>[Vitality]</b> {ps.currentHealth}/{ps.maxHealth} HP   |   <b>[Fullness]</b> {ps.currentHunger}/{ps.maxHunger}";
        }
        else if (statHealthText != null)
        {
            statHealthText.text = "<b>[Vitality]</b> Healthy";
        }

        if (statBlessingText != null)
        {
            if (pm != null && pm.activeBlessing != BlessingType.None)
            {
                BlessingInfo info = BlessingDatabase.GetBlessingInfo(pm.activeBlessing);
                string bName = info != null ? info.title : pm.activeBlessing.ToString();
                statBlessingText.text = $"<b>[Blessing]</b> <color=#88FFDD>{bName}</color>";
            }
            else
            {
                statBlessingText.text = "<b>[Blessing]</b> <color=#AAAAAA>None active</color>";
            }
        }

        // 4. Day Transition Notice
        if (statDayTransitionText != null)
        {
            int curDay = DayManager.Instance != null ? DayManager.Instance.currentDay : PlayerPrefs.GetInt("Farm_CurrentDay", 1);
            int nextDay = curDay + 1;
            statDayTransitionText.text = $"<b>[Time Passed]</b> <color=#FFE066>New Day Arrives ({nextDay})</color> <i>(+1 Day)</i>";
        }

        // 4. Special Protection Notice
        if (specialNoticeText != null)
        {
            bool hasKeepLoot = pm != null && pm.HasBlessing(BlessingType.KeepLootOnDefeat);
            bool isStarter = pm != null && pm.IsStarterProtectionActive();

            if (result == ExpeditionResult.Victory)
            {
                specialNoticeText.text = "<color=#66FF88>[EXCELLENT EXPEDITION!]</color>\nAll loot, golden treasures, and harvested crops have been safely brought back to your farm.";
            }
            else if (result == ExpeditionResult.Retreat)
            {
                specialNoticeText.text = "<color=#66E0FF>[TACTICAL SAFE RETREAT!]</color>\nYou rested at the Camp and returned home safely. 100% of items in your inventory are preserved.";
            }
            else // Defeat
            {
                if (hasKeepLoot || isStarter)
                {
                    string reason = isStarter ? "Divine Novice Protection" : "Divine Protection Blessing";
                    specialNoticeText.text = $"<color=#66FF88>[{reason} ACTIVATED!]</color>\nThough defeated, divine protection shields you: <b>100% of backpack items were preserved</b> and brought home!";
                }
                else
                {
                    specialNoticeText.text = "<color=#FF6666>[YOU WERE DEFEATED IN THE DUNGEON!]</color>\nYou collapsed before the monsters. Prepare more food and blessings before your next expedition.";
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
        Color titleColor;

        switch (result)
        {
            case ExpeditionResult.Victory:
                bannerColor = new Color(0.24f, 0.18f, 0.10f, 1f); // Dark rich mahogany/gold
                t = $"SUMMARY: FLOOR {floor} VICTORY!";
                sub = "You vanquished all monsters and conquered the Dungeon Boss!";
                iconSp = DungeonUIAssetHelper.GetChestSprite();
                titleColor = new Color(1f, 0.88f, 0.35f, 1f); // Radiant Gold
                break;

            case ExpeditionResult.Retreat:
                bannerColor = new Color(0.10f, 0.20f, 0.26f, 1f); // Deep Twilight Slate
                t = $"SUMMARY: SAFE RETREAT - FLOOR {floor}";
                sub = "You retreated safely from the Camp, preserving all gathered resources.";
                iconSp = DungeonUIAssetHelper.GetTentSprite();
                titleColor = new Color(0.55f, 0.90f, 1f, 1f); // Radiant Cyan
                break;

            case ExpeditionResult.Defeat:
            default:
                bannerColor = new Color(0.26f, 0.10f, 0.10f, 1f); // Deep Crimson Wine
                t = $"SUMMARY: DEFEATED ON FLOOR {floor}";
                sub = "The expedition was cut short by underground perils. Train, prepare, and return stronger!";
                iconSp = DungeonUIAssetHelper.GetBossSprite();
                titleColor = new Color(1f, 0.65f, 0.65f, 1f); // Radiant Coral
                break;
        }

        if (headerBannerImg != null)
        {
            DungeonUIAssetHelper.StylePlaque(headerBannerImg, DungeonUIAssetHelper.PlaqueStyle.Wood);
            headerBannerImg.color = bannerColor;
        }

        if (titleText != null)
        {
            titleText.text = t;
            titleText.color = titleColor;
        }

        if (subtitleText != null)
        {
            subtitleText.text = sub;
            subtitleText.color = new Color(0.85f, 0.88f, 0.95f, 1f);
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
            lootHeaderTitle.text = $"EXPEDITION LOOT SECURED ({validSlots.Count} ITEMS)";
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
                tmp.text = "<i>No items in your backpack this run.</i>";
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
            ItemData.ItemType.Produce => "<color=#88CCFF>[Crop]</color>",
            ItemData.ItemType.Seed => "<color=#88FF88>[Seed]</color>",
            ItemData.ItemType.Food => "<color=#FFAA44>[Food]</color>",
            _ => "<color=#E0E0E0>[Item]</color>"
        };

        labelTMP.text = $"<b>{item.itemName}</b>\n{tag} <color=#FFDD44>x{amount}</color>";

        return cardGO;
    }
    #endregion

    #region Action Handlers
    public void OnReturnClicked()
    {
        HasConfirmedReturn = true;
        Close();

        // Automatically advance 1 day and restore stamina after expedition
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
    /// Shared static helper to advance 1 day, update farm crops, and restore stamina when ending a run.
    /// Can be called directly or from any dungeon exit flow.
    /// </summary>
    public static void AdvanceDayOnReturnStatic()
    {
        // Mark run as inactive to avoid duplicate triggers during scene transition
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.runActive = false;
            ProgressionManager.Instance.ClearRunBlessing();
        }

        // Advance to new day: triggers farm crop growth (+1 day) and resets boss cooldown
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
        Debug.Log("[DungeonSummaryUI] Expedition finished: Automatically advanced 1 day (Farm crops grew +1 day older)!");

        // 2. Restore baseline health and fullness upon day transition
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

        // 3. Complete active run state in ProgressionManager
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
        GameObject overlayGO = new("DungeonSummaryPanel", typeof(RectTransform), typeof(Image), typeof(Canvas), typeof(GraphicRaycaster));
        overlayGO.transform.SetParent(canvas.transform, false);
        summaryPanel = overlayGO;

        Canvas summaryCanvas = overlayGO.GetComponent<Canvas>();
        summaryCanvas.overrideSorting = true;
        summaryCanvas.sortingOrder = 500;

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
        titleText.text = "DUNGEON EXPEDITION SUMMARY";

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
        subtitleText.text = "Overview of progress, stages cleared, and all secured expedition loot.";

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
        lhTmp.text = "EXPEDITION STATS";

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

        statFloorText = CreateStatLine(statsContentGO.transform, "StatFloor", "[Floor] 1");
        statStagesText = CreateStatLine(statsContentGO.transform, "StatStages", "[Stages] 0/5");
        statBossText = CreateStatLine(statsContentGO.transform, "StatBoss", "[Boss] Unmet");
        statHealthText = CreateStatLine(statsContentGO.transform, "StatHealth", "[Vitality] 100/100 HP");
        statBlessingText = CreateStatLine(statsContentGO.transform, "StatBlessing", "[Blessing] None");
        statDayTransitionText = CreateStatLine(statsContentGO.transform, "StatDay", "[Time] New Day (+1 Day)");

        // Notice Box at Bottom of Left Column
        GameObject noticeBoxGO = new("NoticeBox", typeof(RectTransform), typeof(Image));
        noticeBoxGO.transform.SetParent(statsContentGO.transform, false);
        RectTransform nbRT = noticeBoxGO.GetComponent<RectTransform>();
        nbRT.anchorMin = new Vector2(0, 0.5f);
        nbRT.anchorMax = new Vector2(1, 0.5f);
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
        lootHeaderTitle.text = "SAFELY SECURED LOOT";

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
        vpRT.offsetMax = new Vector2(-16, 0); // 16px space for vertical scrollbar

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

        // Vertical scrollbar with sleek cyan styling
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

        // Button [RETURN TO FARM]
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
        returnButtonText.raycastTarget = false;
        DungeonUIAssetHelper.ApplyFont(returnButtonText);
        returnButtonText.fontSize = 14;
        returnButtonText.fontStyle = FontStyles.Bold;
        returnButtonText.alignment = TextAlignmentOptions.Center;
        returnButtonText.text = "RETURN TO FARM  [ -> ]";
        returnButtonText.color = Color.white;
    }

    private TextMeshProUGUI CreateStatLine(Transform parent, string name, string defaultText)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
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
