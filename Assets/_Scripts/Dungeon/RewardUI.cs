using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI for the Treasure Vault / Reward Chest room in the Dungeon.
/// Displays unlocked procedural loot, allows individual or batch claiming,
/// and displays real-time backpack inventory status.
/// Styled using project fantasy pixel art assets (UI_Frame, UI_Slot, Golden Chest).
/// </summary>
public class RewardUI : MonoBehaviour
{
    #region Singleton & References
    private static RewardUI _instance;
    public static RewardUI Instance => _instance;

    public static RewardUI EnsureInstance()
    {
        if (_instance != null) return _instance;

        RewardUI found = FindFirstObjectByType<RewardUI>(FindObjectsInactive.Include);
        if (found != null)
        {
            _instance = found;
            return _instance;
        }

        Canvas canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        if (canvas != null)
        {
            GameObject go = new("RewardUI");
            go.transform.SetParent(canvas.transform, false);
            _instance = go.AddComponent<RewardUI>();
            _instance.canvas = canvas;
            return _instance;
        }

        return null;
    }

    [Header("UI Panels")]
    [SerializeField] public GameObject rewardPanel;
    [SerializeField] public RectTransform chestItemsContainer;
    [SerializeField] public RectTransform backpackSlotsParent;
    [SerializeField] public Canvas canvas;
    [SerializeField] public GameObject slotPrefab;

    [Header("Header Graphics & Labels")]
    [SerializeField] public Image chestHeaderIcon;
    [SerializeField] public TextMeshProUGUI titleText;
    [SerializeField] public TextMeshProUGUI subtitleText;
    [SerializeField] public TextMeshProUGUI leftHeaderTitle;
    [SerializeField] public TextMeshProUGUI backpackHeaderTitle;
    [SerializeField] public TextMeshProUGUI hintText;

    [Header("Action Buttons")]
    [SerializeField] public Button lootAllButton;
    [SerializeField] public Button continueButton;

    private RewardChest currentChest;
    private ItemContainer backpackContainer;
    private System.Action onCompleteCallback;

    private readonly List<GameObject> activeRewardRows = new();
    private readonly List<InventoryButton> backpackSlots = new();
    private GameObject emptyNoticeGO;

    public bool IsOpen => rewardPanel != null && rewardPanel.activeSelf;
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
        EnforceBalancedLayout();

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }

        if (lootAllButton != null)
        {
            lootAllButton.onClick.RemoveListener(OnLootAllClicked);
            lootAllButton.onClick.AddListener(OnLootAllClicked);
        }
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            UIEscapeManager.ConsumeEscape();
            Close();
        }
    }

    private void OnDestroy()
    {
        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange -= RefreshBackpackSlots;
        }
        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion

    #region Open & Close
    public void Open(RewardChest chest, List<ItemSlot> loot, ItemContainer backpack, System.Action onComplete, string customTitle = null, string customSubtitle = null)
    {
        EnsureUIBuilt();
        EnforceBalancedLayout();

        if (titleText != null)
        {
            titleText.text = !string.IsNullOrEmpty(customTitle) ? customTitle : "PHÒNG KHO BÁU - PHẦN THƯỞNG CHẶNG";
        }
        if (subtitleText != null)
        {
            subtitleText.text = !string.IsNullOrEmpty(customSubtitle) ? customSubtitle : "Rương kho báu cổ xưa đã được mở khóa! Hãy nhận hạt giống, nông sản quý và quân lương.";
        }
        if (leftHeaderTitle != null)
        {
            leftHeaderTitle.text = !string.IsNullOrEmpty(customTitle) ? customTitle.ToUpper() : "VẬT PHẨM TRONG RƯƠNG";
        }

        currentChest = chest;
        if (currentChest == null)
        {
            currentChest = GetComponent<RewardChest>();
            if (currentChest == null) currentChest = gameObject.AddComponent<RewardChest>();
        }

        if (loot != null && loot.Count > 0)
        {
            currentChest.currentChestLoot = new List<ItemSlot>(loot);
        }
        else if (currentChest.currentChestLoot == null)
        {
            currentChest.currentChestLoot = new List<ItemSlot>();
        }

        backpackContainer = backpack;
        onCompleteCallback = onComplete;

        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange -= RefreshBackpackSlots;
            backpackContainer.OnInventoryChange += RefreshBackpackSlots;
        }

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            rewardPanel.transform.SetAsLastSibling();
        }

        UIEscapeManager.NotifyModalOpened("Reward");

        RefreshAll();

        SetHint("Nhấp <b>[Nhận]</b> để lấy từng món hoặc <b>[NHẬN TẤT CẢ]</b> để cất hết vào ba lô.");
        Debug.Log("[RewardUI] Reward/Loot room opened successfully.");
    }

    public void Close()
    {
        if (backpackContainer != null)
        {
            backpackContainer.OnInventoryChange -= RefreshBackpackSlots;
        }

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }

        DungeonTutorialController.Instance?.HideBanner();
        DungeonTutorialController.Instance?.HidePointer();

        UIEscapeManager.NotifyModalClosed("Reward");
        CurrentItemPanel.RefreshHotbar();

        onCompleteCallback?.Invoke();
        onCompleteCallback = null;
    }
    #endregion

    #region UI Refresh & Rendering
    public void RefreshAll()
    {
        RefreshChestItems();
        BuildBackpackSlots(backpackContainer);
        UpdateBackpackHeader();
        UpdateControlsState();
    }

    private void UpdateBackpackHeader()
    {
        if (backpackHeaderTitle == null || backpackContainer == null || backpackContainer.itemSlots == null) return;
        int occupied = 0;
        int maxSlots = backpackContainer.maxSlots;
        for (int i = 0; i < backpackContainer.itemSlots.Length; i++)
        {
            if (backpackContainer.itemSlots[i] != null && backpackContainer.itemSlots[i].itemData != null)
            {
                occupied++;
            }
        }
        backpackHeaderTitle.text = $"BA LÔ CỦA BẠN <size=11><color=#A0D8EF>({occupied}/{maxSlots} Ô)</color></size>";
    }

    private void RefreshChestItems()
    {
        if (chestItemsContainer == null) return;

        VerticalLayoutGroup vlg = chestItemsContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.spacing = 6f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        // Clean out previous children to avoid overlapping or ghost rows
        for (int i = chestItemsContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = chestItemsContainer.GetChild(i);
            if (child != null && (emptyNoticeGO == null || child.gameObject != emptyNoticeGO))
            {
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }
        activeRewardRows.Clear();

        if (currentChest == null || currentChest.currentChestLoot == null || currentChest.currentChestLoot.Count == 0)
        {
            ShowEmptyChestNotice();
            return;
        }

        if (emptyNoticeGO != null)
        {
            emptyNoticeGO.SetActive(false);
        }

        int validCount = 0;
        for (int i = 0; i < currentChest.currentChestLoot.Count; i++)
        {
            ItemSlot slot = currentChest.currentChestLoot[i];
            if (slot == null || slot.itemData == null || slot.amount <= 0) continue;

            CreateRewardRow(slot, validCount);
            validCount++;
        }

        if (validCount == 0)
        {
            ShowEmptyChestNotice();
        }
        else
        {
            chestItemsContainer.sizeDelta = new Vector2(0, validCount * (52f + 6f) + 12f);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(chestItemsContainer);

        ScrollRect chestSR = chestItemsContainer.GetComponentInParent<ScrollRect>();
        if (chestSR != null)
        {
            chestSR.verticalNormalizedPosition = 1f;
            chestSR.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
    }

    private void ShowEmptyChestNotice()
    {
        if (chestItemsContainer == null) return;

        if (emptyNoticeGO == null)
        {
            emptyNoticeGO = new("EmptyNotice", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            emptyNoticeGO.transform.SetParent(chestItemsContainer, false);
            RectTransform rt = emptyNoticeGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(370, 75);

            LayoutElement le = emptyNoticeGO.GetComponent<LayoutElement>();
            le.minHeight = 75f;
            le.preferredHeight = 75f;
            le.flexibleHeight = 0f;
            le.minWidth = 300f;
            le.flexibleWidth = 1f;

            Image bg = emptyNoticeGO.GetComponent<Image>();
            DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.1f, 0.12f, 0.16f, 0.9f));

            GameObject textGO = new("EmptyLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(emptyNoticeGO.transform, false);
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10, 6);
            textRT.offsetMax = new Vector2(-10, -6);

            TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = textGO.AddComponent<TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(tmp);
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.85f, 0.9f, 0.95f, 0.95f);
            tmp.text = "<b>Tất cả vật phẩm đã được nhận!</b>\n<size=11><color=#88FF88>Rương phần thưởng đã trống.</color> Bấm [TIẾP TỤC] để qua màn.</size>";
        }

        emptyNoticeGO.transform.SetParent(chestItemsContainer, false);
        emptyNoticeGO.SetActive(true);
        emptyNoticeGO.transform.SetAsLastSibling();
    }

    private void EnforceBalancedLayout()
    {
        if (rewardPanel == null) return;

        Transform modalT = rewardPanel.transform.Find("RewardModal");
        if (modalT is RectTransform modalRT)
        {
            modalRT.anchorMin = new Vector2(0.5f, 0.5f);
            modalRT.anchorMax = new Vector2(0.5f, 0.5f);
            modalRT.pivot = new Vector2(0.5f, 0.5f);
            modalRT.anchoredPosition = Vector2.zero;
            modalRT.sizeDelta = new Vector2(960, 580);

            // Body
            Transform bodyT = modalT.Find("Body");
            if (bodyT is RectTransform bodyRT)
            {
                bodyRT.anchorMin = Vector2.zero;
                bodyRT.anchorMax = Vector2.one;
                bodyRT.offsetMin = new Vector2(20, 68); // 68px for action bar
                bodyRT.offsetMax = new Vector2(-20, -84); // 84px below header

                // LeftChestPanel: 50% of modal width
                Transform leftT = bodyT.Find("LeftChestPanel");
                if (leftT is RectTransform leftRT)
                {
                    leftRT.anchorMin = new Vector2(0, 0);
                    leftRT.anchorMax = new Vector2(0.50f, 1);
                    leftRT.pivot = new Vector2(0.5f, 0.5f);
                    leftRT.offsetMin = Vector2.zero;
                    leftRT.offsetMax = new Vector2(-6, 0);

                    Image leftImg = leftT.GetComponent<Image>();
                    if (leftImg != null)
                    {
                        DungeonUIAssetHelper.StyleSlicedFrame(leftImg, new Color(0.12f, 0.14f, 0.20f, 0.95f));
                    }

                    // LeftHeader
                    Transform lhT = leftT.Find("LeftHeader");
                    if (lhT is RectTransform lhRT)
                    {
                        lhRT.anchorMin = new Vector2(0, 1);
                        lhRT.anchorMax = new Vector2(1, 1);
                        lhRT.pivot = new Vector2(0.5f, 1);
                        lhRT.anchoredPosition = new Vector2(0, -6);
                        lhRT.sizeDelta = new Vector2(-16, 32);

                        TextMeshProUGUI lhTMP = lhT.GetComponent<TextMeshProUGUI>();
                        if (lhTMP != null)
                        {
                            lhTMP.alignment = TextAlignmentOptions.Center;
                            lhTMP.fontStyle = FontStyles.Bold;
                            lhTMP.fontSize = 13.5f;
                        }
                    }

                    // ChestScrollArea
                    Transform scrollT = leftT.Find("ChestScrollArea");
                    if (scrollT is RectTransform scrollRT)
                    {
                        scrollRT.anchorMin = Vector2.zero;
                        scrollRT.anchorMax = Vector2.one;
                        scrollRT.pivot = new Vector2(0.5f, 0.5f);
                        scrollRT.offsetMin = new Vector2(8, 8);
                        scrollRT.offsetMax = new Vector2(-8, -42);

                        Transform vpT = scrollT.Find("Viewport");
                        if (vpT is RectTransform vpRT)
                        {
                            vpRT.anchorMin = Vector2.zero;
                            vpRT.anchorMax = Vector2.one;
                            vpRT.pivot = new Vector2(0.5f, 0.5f);
                            vpRT.offsetMin = Vector2.zero;
                            vpRT.offsetMax = new Vector2(-16, 0);
                        }

                        Transform chestSbT = scrollT.Find("VerticalScrollbar");
                        if (chestSbT is RectTransform chestSbRT)
                        {
                            chestSbRT.anchorMin = new Vector2(1, 0);
                            chestSbRT.anchorMax = new Vector2(1, 1);
                            chestSbRT.pivot = new Vector2(1, 0.5f);
                            chestSbRT.anchoredPosition = Vector2.zero;
                            chestSbRT.sizeDelta = new Vector2(12, 0);
                        }
                    }
                }

                // RightBackpackPanel: 50% of modal width
                Transform rightT = bodyT.Find("RightBackpackPanel");
                if (rightT is RectTransform rightRT)
                {
                    rightRT.anchorMin = new Vector2(0.50f, 0);
                    rightRT.anchorMax = new Vector2(1, 1);
                    rightRT.pivot = new Vector2(0.5f, 0.5f);
                    rightRT.offsetMin = new Vector2(6, 0);
                    rightRT.offsetMax = Vector2.zero;

                    Image rightImg = rightT.GetComponent<Image>();
                    if (rightImg != null)
                    {
                        DungeonUIAssetHelper.StyleSlicedFrame(rightImg, new Color(0.12f, 0.14f, 0.20f, 0.95f));
                    }

                    // RightHeader
                    Transform rhT = rightT.Find("RightHeader");
                    if (rhT is RectTransform rhRT)
                    {
                        rhRT.anchorMin = new Vector2(0, 1);
                        rhRT.anchorMax = new Vector2(1, 1);
                        rhRT.pivot = new Vector2(0.5f, 1);
                        rhRT.anchoredPosition = new Vector2(0, -6);
                        rhRT.sizeDelta = new Vector2(-16, 32);

                        TextMeshProUGUI rhTMP = rhT.GetComponent<TextMeshProUGUI>();
                        if (rhTMP != null)
                        {
                            rhTMP.alignment = TextAlignmentOptions.Center;
                            rhTMP.fontStyle = FontStyles.Bold;
                            rhTMP.fontSize = 13.5f;
                        }
                    }

                    // BackpackScrollArea
                    Transform bpScrollT = rightT.Find("BackpackScrollArea");
                    if (bpScrollT is RectTransform bpScrollRT)
                    {
                        bpScrollRT.anchorMin = Vector2.zero;
                        bpScrollRT.anchorMax = Vector2.one;
                        bpScrollRT.pivot = new Vector2(0.5f, 0.5f);
                        bpScrollRT.offsetMin = new Vector2(8, 8);
                        bpScrollRT.offsetMax = new Vector2(-8, -42);

                        Transform bpVpT = bpScrollT.Find("Viewport");
                        if (bpVpT is RectTransform bpVpRT)
                        {
                            bpVpRT.anchorMin = Vector2.zero;
                            bpVpRT.anchorMax = Vector2.one;
                            bpVpRT.pivot = new Vector2(0.5f, 0.5f);
                            bpVpRT.offsetMin = Vector2.zero;
                            bpVpRT.offsetMax = new Vector2(-16, 0);

                            if (bpVpT.GetComponent<RectMask2D>() == null)
                            {
                                bpVpT.gameObject.AddComponent<RectMask2D>();
                            }
                        }

                        Transform bpSbT = bpScrollT.Find("VerticalScrollbar");
                        if (bpSbT is RectTransform bpSbRT)
                        {
                            bpSbRT.anchorMin = new Vector2(1, 0);
                            bpSbRT.anchorMax = new Vector2(1, 1);
                            bpSbRT.pivot = new Vector2(1, 0.5f);
                            bpSbRT.anchoredPosition = Vector2.zero;
                            bpSbRT.sizeDelta = new Vector2(12, 0);
                        }

                        ScrollRect bpSR = bpScrollT.GetComponent<ScrollRect>();
                        if (bpSR != null)
                        {
                            if (bpVpT is RectTransform bpVpRT_cast) bpSR.viewport = bpVpRT_cast;
                            if (backpackSlotsParent != null) bpSR.content = backpackSlotsParent;
                            bpSR.horizontal = false;
                            bpSR.vertical = true;
                            bpSR.movementType = ScrollRect.MovementType.Clamped;
                            bpSR.scrollSensitivity = 25f;
                            bpSR.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                        }
                    }
                }
            }
        }

        // Configure chestItemsContainer layout
        if (chestItemsContainer != null)
        {
            chestItemsContainer.anchorMin = new Vector2(0, 1);
            chestItemsContainer.anchorMax = new Vector2(1, 1);
            chestItemsContainer.pivot = new Vector2(0.5f, 1);
            chestItemsContainer.anchoredPosition = Vector2.zero;
            chestItemsContainer.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vlg = chestItemsContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.padding = new RectOffset(6, 6, 6, 6);
                vlg.spacing = 8f;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
            }

            ContentSizeFitter csf = chestItemsContainer.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        // Configure backpackSlotsParent layout (centered 6 columns)
        if (backpackSlotsParent != null)
        {
            backpackSlotsParent.anchorMin = new Vector2(0, 1);
            backpackSlotsParent.anchorMax = new Vector2(1, 1);
            backpackSlotsParent.pivot = new Vector2(0.5f, 1);
            backpackSlotsParent.anchoredPosition = Vector2.zero;
            backpackSlotsParent.sizeDelta = Vector2.zero;

            GridLayoutGroup glg = backpackSlotsParent.GetComponent<GridLayoutGroup>();
            if (glg != null)
            {
                glg.cellSize = new Vector2(52, 52);
                glg.spacing = new Vector2(6, 6);
                glg.padding = new RectOffset(6, 6, 6, 6);
                glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                glg.constraintCount = 6;
                glg.childAlignment = TextAnchor.UpperCenter;
            }

            ContentSizeFitter csf = backpackSlotsParent.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }
    }

    private void CreateRewardRow(ItemSlot slot, int slotIndex)
    {
        if (slot == null || slot.itemData == null || slot.amount <= 0) return;
        ItemData item = slot.itemData;

        // Container: horizontal row
        GameObject rowGO = new($"RewardItem_{slotIndex}_{item.itemName}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowGO.transform.SetParent(chestItemsContainer, false);

        RectTransform rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0, 1);
        rowRT.anchorMax = new Vector2(1, 1);
        rowRT.pivot = new Vector2(0, 1); // Top-left pivot
        rowRT.anchoredPosition = new Vector2(0, -slotIndex * (52f + 6f));
        rowRT.sizeDelta = new Vector2(0, 52);

        LayoutElement le = rowGO.GetComponent<LayoutElement>();
        le.minHeight = 52f;
        le.preferredHeight = 52f;
        le.flexibleHeight = 0f;
        le.minWidth = 320f;
        le.flexibleWidth = 1f;

        Image bg = rowGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.13f, 0.16f, 0.22f, 0.98f));

        HorizontalLayoutGroup hlg = rowGO.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 5, 5);
        hlg.spacing = 8;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // 1. Slot Box with Icon (Left: 42x42)
        GameObject slotBoxGO = new("SlotBox", typeof(RectTransform), typeof(Image));
        slotBoxGO.transform.SetParent(rowGO.transform, false);
        slotBoxGO.GetComponent<RectTransform>().sizeDelta = new Vector2(42, 42);
        Image slotBoxImg = slotBoxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(slotBoxImg, new Color(0.22f, 0.26f, 0.36f, 1f));

        GameObject iconGO = new("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(slotBoxGO.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 0.5f);
        iconRT.anchorMax = new Vector2(0.5f, 0.5f);
        iconRT.anchoredPosition = Vector2.zero;
        iconRT.sizeDelta = new Vector2(30, 30);
        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.sprite = item.itemIcon;

        // 2. Info Label (Center: 240x42)
        GameObject infoGO = new("Info", typeof(RectTransform), typeof(TextMeshProUGUI));
        infoGO.transform.SetParent(rowGO.transform, false);
        infoGO.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 42);

        TextMeshProUGUI infoTMP = infoGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(infoTMP, withOutline: true);
        infoTMP.fontSize = 11.5f;
        infoTMP.alignment = TextAlignmentOptions.MidlineLeft;
        string tag = GetItemTag(item);
        infoTMP.text = $"<b>{item.itemName}</b> <color=#FFD700>x{slot.amount}</color> {tag}\n<size=10.5>{GetItemDescription(item)}</size>";

        // 3. Take Button (Right: 75x34)
        GameObject btnGO = new("TakeBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(rowGO.transform, false);
        btnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(75, 34);

        Image btnImg = btnGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(btnImg, new Color(0.18f, 0.58f, 0.32f, 1f));

        Button btn = btnGO.GetComponent<Button>();
        ItemSlot slotRef = slot;
        btn.onClick.AddListener(() =>
        {
            btn.interactable = false;
            OnTakeItemClicked(slotRef);
        });

        GameObject btnTextGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnText = btnTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(btnText);
        btnText.fontSize = 11.5f;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = Color.white;
        btnText.text = "NHẬN";

        activeRewardRows.Add(rowGO);
    }

    private string GetItemTag(ItemData item)
    {
        if (item == null) return "";
        string nameLower = item.name.ToLower();
        if (nameLower.Contains("seed") || item.itemName.ToLower().Contains("seed"))
        {
            return "<color=#88FF88>[HẠT GIỐNG]</color>";
        }
        if (item is FoodData)
        {
            return "<color=#FFAA44>[MÓN ĂN]</color>";
        }
        return "<color=#88CCFF>[NÔNG SẢN]</color>";
    }

    private string GetItemDescription(ItemData item)
    {
        if (item is FoodData food)
        {
            return $"<color=#FF6B6B><b>+{food.healthValue} HP</b></color> <color=#94A3B8>|</color> <color=#FFD043><b>+{food.hungerValue} Độ No</b></color>";
        }
        if (item.name.ToLower().Contains("seed") || item.itemName.ToLower().Contains("seed"))
        {
            return "<color=#86EFAC>Trồng tại nông trại để thu hoạch</color>";
        }
        return "<color=#E2E8F0>Tài nguyên quý giá</color>";
    }

    private void UpdateControlsState()
    {
        bool hasItems = currentChest != null && currentChest.currentChestLoot != null && currentChest.currentChestLoot.Count > 0;
        if (lootAllButton != null)
        {
            lootAllButton.interactable = hasItems;
        }
    }
    #endregion

    #region Action Handlers
    private void OnTakeItemClicked(ItemSlot targetSlot)
    {
        if (currentChest == null || backpackContainer == null || targetSlot == null) return;

        // Find the item in the current loot list by reference (safe after RemoveAt shifts indices)
        int foundIndex = -1;
        for (int i = 0; i < currentChest.currentChestLoot.Count; i++)
        {
            if (currentChest.currentChestLoot[i] == targetSlot)
            {
                foundIndex = i;
                break;
            }
        }

        if (foundIndex < 0)
        {
            // Already claimed (double-click race), just refresh
            RefreshAll();
            return;
        }

        bool success = currentChest.ClaimSingleItem(foundIndex, backpackContainer, out string message);
        if (success)
        {
            SetHint($"<color=#88FF88>Đã nhận:</color> {message}");
        }
        else
        {
            SetHint($"<color=#FF7777>{message}</color>");
        }
        RefreshAll();
    }

    public void OnLootAllClicked()
    {
        if (currentChest == null || backpackContainer == null) return;

        int claimed = currentChest.ClaimAllLoot(backpackContainer, out bool isFull);
        if (claimed > 0)
        {
            string msg = $"<color=#88FF88>Đã thu thập {claimed} xấp vật phẩm vào ba lô!</color>";
            if (isFull)
            {
                msg += " <color=#FFCC00>(Ba lô hiện đã đầy!)</color>";
            }
            SetHint(msg);
            RefreshAll();
        }
        else if (isFull)
        {
            SetHint("<color=#FF7777>Ba lô đã đầy! Hãy dọn bớt ô trống trước.</color>");
        }
    }

    public void OnContinueClicked()
    {
        Close();
    }

    private void SetHint(string message)
    {
        if (hintText != null)
        {
            hintText.text = message;
        }
    }
    #endregion

    #region Backpack UI Integration
    private void BuildBackpackSlots(ItemContainer container)
    {
        if (backpackSlotsParent == null || container == null) return;

        EnsureSlotPrefab();
        Canvas currentCanvas = GetCanvas();

        bool allSlotsValid = backpackSlots.Count == container.maxSlots;
        if (allSlotsValid)
        {
            for (int i = 0; i < backpackSlots.Count; i++)
            {
                if (backpackSlots[i] == null)
                {
                    allSlotsValid = false;
                    break;
                }
            }
        }

        if (allSlotsValid)
        {
            for (int i = 0; i < backpackSlots.Count; i++)
            {
                backpackSlots[i].gameObject.SetActive(true);
                backpackSlots[i].SetSlotData(container, i);
                backpackSlots[i].SetCanvas(currentCanvas);
            }
            RefreshBackpackSlots();
            return;
        }

        InventoryButton[] existing = backpackSlotsParent.GetComponentsInChildren<InventoryButton>(true);
        if (existing != null && existing.Length >= container.maxSlots)
        {
            backpackSlots.Clear();
            for (int i = 0; i < container.maxSlots; i++)
            {
                existing[i].gameObject.SetActive(true);
                existing[i].SetSlotData(container, i);
                existing[i].SetCanvas(currentCanvas);
                backpackSlots.Add(existing[i]);
            }
            for (int i = container.maxSlots; i < existing.Length; i++)
            {
                existing[i].gameObject.SetActive(false);
            }
            RefreshBackpackSlots();
            return;
        }

        foreach (var slot in backpackSlots)
        {
            if (slot != null) Destroy(slot.gameObject);
        }
        backpackSlots.Clear();

        if (slotPrefab == null)
        {
            Debug.LogWarning("[RewardUI] slotPrefab is null, cannot build backpack slots!");
            return;
        }

        for (int i = 0; i < container.maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, backpackSlotsParent);
            slotObj.name = $"BackpackSlot_{i}";
            InventoryButton btn = slotObj.GetComponent<InventoryButton>();
            if (btn == null)
            {
                Destroy(slotObj);
                continue;
            }

            btn.SetSlotData(container, i);
            btn.SetCanvas(currentCanvas);
            backpackSlots.Add(btn);
        }

        RefreshBackpackSlots();
        Canvas.ForceUpdateCanvases();
        if (backpackSlotsParent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(backpackSlotsParent);
        }
    }

    private void RefreshBackpackSlots()
    {
        if (backpackContainer == null) return;

        for (int i = 0; i < backpackSlots.Count; i++)
        {
            if (i < backpackContainer.itemSlots.Length &&
                backpackContainer.itemSlots[i] != null &&
                backpackContainer.itemSlots[i].itemData != null)
            {
                backpackSlots[i].SetItem(
                    backpackContainer.itemSlots[i].itemData,
                    backpackContainer.itemSlots[i].amount
                );
            }
            else
            {
                backpackSlots[i].ClearItem();
            }
        }

        UpdateBackpackHeader();
    }

    private void EnsureSlotPrefab()
    {
        if (slotPrefab != null) return;
        slotPrefab = DungeonUIAssetHelper.GetSlotPrefab();
    }

    private Canvas GetCanvas()
    {
        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
            {
                canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
            }
        }
        return canvas;
    }
    #endregion

    #region Procedural UI Building
    public void EnsureUIBuilt()
    {
        // UI is authored and bound directly in the scene.
    }
    #endregion
}
