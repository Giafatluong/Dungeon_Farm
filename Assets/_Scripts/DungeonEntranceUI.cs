using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Dungeon Entrance UI at Base.
/// Displays available dungeon floors in a vertical tower layout (Floor 1 at the bottom, higher floors above).
/// Players must defeat the boss of the lower floor before advancing to the next floor.
/// </summary>
public class DungeonEntranceUI : MonoBehaviour
{
    [SerializeField] private GameObject enterDungeonPanel;

    [Header("Floors Configuration")]
    [SerializeField] private int totalFloors = 5;
    [SerializeField] private FloorDisplayData[] customFloors;

    [System.Serializable]
    public class FloorDisplayData
    {
        public int floorNumber;
        public string floorName;
        public string bossName;
        public string subTitle;
        [TextArea(2, 3)]
        public string description;
    }

    private readonly List<FloorDisplayData> floorList = new List<FloorDisplayData>();
    private int selectedFloor = 1;

    // Runtime UI references
    private GameObject dialogPanel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI detailsText;
    private Button enterButton;
    private TextMeshProUGUI enterButtonText;
    private Button closeButton;
    private readonly Dictionary<int, FloorCardUI> floorCards = new Dictionary<int, FloorCardUI>();
    private Coroutine feedbackFlashCoroutine;

    private class FloorCardUI
    {
        public GameObject rootGO;
        public Button button;
        public Image backgroundImage;
        public TextMeshProUGUI floorBadgeText;
        public TextMeshProUGUI floorNameText;
        public TextMeshProUGUI bossInfoText;
        public TextMeshProUGUI statusBadgeText;
        public Image statusBadgeBg;
        public int floorNumber;
    }

    private void Awake()
    {
        InitializeFloorData();
    }

    private void Start()
    {
        EnsureUIConstructed();
    }

    private void Update()
    {
        if (enterDungeonPanel != null && enterDungeonPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                EnterSelectedFloor();
            }
        }
    }

    private void InitializeFloorData()
    {
        floorList.Clear();

        if (customFloors != null && customFloors.Length > 0)
        {
            floorList.AddRange(customFloors);
            totalFloors = Mathf.Max(totalFloors, floorList.Count);
            return;
        }

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 1,
            floorName = "FLOOR 1: TRIAL DUNGEON (8 WAVES)",
            bossName = "Skeleton King",
            subTitle = "8 Complete Stages - Difficulty: Balanced",
            description = "Trial floor with 8 Waves: Monsters (Slime/Skeleton/Bowman), Dungeon Events, Traveling Merchant, Treasure Chests, Campfire Cooking & Floor Boss."
        });

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 2,
            floorName = "FLOOR 2: ANCIENT CRYPT",
            bossName = "Immortal Skeleton General",
            subTitle = "Requires Floor 1 Boss - Difficulty: Medium",
            description = "Underground crypt with immortal skeleton warriors and archers. Monsters are faster and more dangerous."
        });

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 3,
            floorName = "FLOOR 3: TOXIC SPORE FOREST",
            bossName = "Spore Demon Fungus",
            subTitle = "Requires Floor 2 Boss - Difficulty: Hard",
            description = "Subterranean toxic mushroom forest. Creatures can self-destruct and emit continuous poison spores."
        });

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 4,
            floorName = "FLOOR 4: MAGMA CHASM",
            bossName = "Abyssal Fire Dragon",
            subTitle = "Requires Floor 3 Boss - Difficulty: Very Hard",
            description = "Boiling magma chasm with scorching heat. Requires abundant recovery food and careful tactics."
        });

        floorList.Add(new FloorDisplayData
        {
            floorNumber = 5,
            floorName = "FLOOR 5: THRONE OF THE ABYSS",
            bossName = "Lord of Darkness",
            subTitle = "Requires Floor 4 Boss - Difficulty: Extreme",
            description = "Highest pinnacle of the thousand-year dungeon. Defeat the Abyssal Lord to uncover ultimate secrets and treasures."
        });
    }

    public void Open()
    {
        EnsureUIConstructed();

        if (enterDungeonPanel != null)
        {
            enterDungeonPanel.SetActive(true);
        }

        // Default to highest unlocked floor or current floor
        if (ProgressionManager.Instance != null)
        {
            int candidate = ProgressionManager.Instance.currentFloor;
            if (candidate > 0 && candidate <= totalFloors && IsFloorUnlocked(candidate))
            {
                selectedFloor = candidate;
            }
            else
            {
                selectedFloor = Mathf.Clamp(ProgressionManager.Instance.highestUnlockedFloor, 1, totalFloors);
            }
        }
        else
        {
            selectedFloor = 1;
        }

        RefreshUI();
    }

    public void Close()
    {
        if (enterDungeonPanel != null)
        {
            enterDungeonPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Backward-compatible method called by existing Unity UI events.
    /// Enters dungeon at the currently selected floor.
    /// </summary>
    public void EnterDungeon()
    {
        EnterSelectedFloor();
    }

    // Cached Color Constants for zero-allocation UI refreshing
    private static readonly Color ColorCardSelected = new Color(1.0f, 0.78f, 0.20f, 1f);
    private static readonly Color ColorCardUnlocked = new Color(0.28f, 0.38f, 0.52f, 0.90f);
    private static readonly Color ColorCardLocked = new Color(0.20f, 0.22f, 0.26f, 0.55f);

    private static readonly Color ColorBadgeSelected = new Color(1.0f, 0.88f, 0.35f);
    private static readonly Color ColorBadgeUnlocked = new Color(0.70f, 0.85f, 1.0f);
    private static readonly Color ColorBadgeLocked = new Color(0.45f, 0.48f, 0.55f);

    private static readonly Color ColorNameUnlocked = new Color(0.92f, 0.94f, 0.98f);
    private static readonly Color ColorNameLocked = new Color(0.55f, 0.58f, 0.65f);

    private static readonly Color ColorBossSelected = new Color(1.0f, 0.85f, 0.50f);
    private static readonly Color ColorBossUnlocked = new Color(0.60f, 0.72f, 0.82f);
    private static readonly Color ColorBossLocked = new Color(0.40f, 0.42f, 0.48f);

    private static readonly Color ColorStatusDefeatedText = new Color(0.40f, 1.0f, 0.45f);
    private static readonly Color ColorStatusDefeatedBg = new Color(0.10f, 0.35f, 0.15f, 0.85f);
    private static readonly Color ColorStatusSelectedText = new Color(1.0f, 0.90f, 0.30f);
    private static readonly Color ColorStatusSelectedBg = new Color(0.40f, 0.28f, 0.05f, 0.95f);
    private static readonly Color ColorStatusUnlockedText = new Color(0.55f, 0.85f, 1.0f);
    private static readonly Color ColorStatusUnlockedBg = new Color(0.12f, 0.25f, 0.42f, 0.85f);
    private static readonly Color ColorStatusLockedText = new Color(0.65f, 0.65f, 0.65f);
    private static readonly Color ColorStatusLockedBg = new Color(0.18f, 0.18f, 0.20f, 0.85f);

    public void EnterSelectedFloor()
    {
        if (!IsFloorUnlocked(selectedFloor))
        {
            ShowWarning($"Floor {selectedFloor} is locked! Defeat Floor {selectedFloor - 1} Boss first.");
            return;
        }

        Close();

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.currentFloor = selectedFloor;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.Log("Scene Transition Manager is NULL, fallback loading Dungeon directly");
            ProgressionManager.Instance?.StartRun(selectedFloor);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Dungeon");
            return;
        }

        SceneTransitionManager.Instance.LoadDungeon(selectedFloor);
    }

    public void SelectFloor(int floor)
    {
        if (!IsFloorUnlocked(floor))
        {
            ShowWarning($"Floor {floor} is locked! Defeat Floor {floor - 1} Boss first.");
            return;
        }

        selectedFloor = floor;
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.currentFloor = selectedFloor;
        }

        RefreshUI();
    }

    public bool IsFloorUnlocked(int floor)
    {
        if (floor <= 1) return true;
        if (ProgressionManager.Instance != null)
        {
            return ProgressionManager.Instance.IsFloorUnlocked(floor);
        }
        return false;
    }

    public bool IsBossDefeated(int floor)
    {
        if (ProgressionManager.Instance != null)
        {
            return ProgressionManager.Instance.IsBossDefeated(floor);
        }
        return false;
    }

    private FloorDisplayData GetFloorData(int floor)
    {
        for (int i = 0; i < floorList.Count; i++)
        {
            if (floorList[i].floorNumber == floor)
                return floorList[i];
        }

        return new FloorDisplayData
        {
            floorNumber = floor,
            floorName = $"FLOOR {floor}",
            bossName = $"Floor {floor} Boss",
            subTitle = $"Floor {floor} of the dungeon",
            description = $"Explore floor {floor} and defeat the boss to proceed."
        };
    }

    private void RefreshUI()
    {
        if (dialogPanel == null) return;

        // Refresh all cards without heap allocations
        foreach (var kvp in floorCards)
        {
            int floorNum = kvp.Key;
            FloorCardUI card = kvp.Value;
            bool isUnlocked = IsFloorUnlocked(floorNum);
            bool isDefeated = IsBossDefeated(floorNum);
            bool isSelected = (floorNum == selectedFloor);

            // Card background border
            card.backgroundImage.color = isSelected ? ColorCardSelected : (isUnlocked ? ColorCardUnlocked : ColorCardLocked);

            // Text colors
            card.floorBadgeText.color = isSelected ? ColorBadgeSelected : (isUnlocked ? ColorBadgeUnlocked : ColorBadgeLocked);
            card.floorNameText.color = isSelected ? Color.white : (isUnlocked ? ColorNameUnlocked : ColorNameLocked);
            card.bossInfoText.color = isSelected ? ColorBossSelected : (isUnlocked ? ColorBossUnlocked : ColorBossLocked);

            // Status Badge
            if (isDefeated)
            {
                card.statusBadgeText.text = "* CLEARED";
                card.statusBadgeText.color = ColorStatusDefeatedText;
                card.statusBadgeBg.color = ColorStatusDefeatedBg;
            }
            else if (isSelected)
            {
                card.statusBadgeText.text = "> SELECTED";
                card.statusBadgeText.color = ColorStatusSelectedText;
                card.statusBadgeBg.color = ColorStatusSelectedBg;
            }
            else if (isUnlocked)
            {
                card.statusBadgeText.text = "READY";
                card.statusBadgeText.color = ColorStatusUnlockedText;
                card.statusBadgeBg.color = ColorStatusUnlockedBg;
            }
            else
            {
                card.statusBadgeText.text = "LOCKED";
                card.statusBadgeText.color = ColorStatusLockedText;
                card.statusBadgeBg.color = ColorStatusLockedBg;
            }
        }

        // Details Panel & Enter button text
        FloorDisplayData selData = GetFloorData(selectedFloor);
        bool selUnlocked = IsFloorUnlocked(selectedFloor);
        bool selDefeated = IsBossDefeated(selectedFloor);

        string statusTag = selDefeated
            ? "<color=#69F0AE>[* Cleared]</color>"
            : (selUnlocked ? "<color=#81D4FA>[Ready]</color>" : "<color=#FF5252>[Locked]</color>");

        detailsText.text = $"<b><color=#FFD54F>{selData.floorName}</color> {statusTag}</b>\n" +
                           $"<size=11><color=#B0BEC5>Boss: <color=#FFE082>{selData.bossName}</color> | {selData.subTitle}</color></size>\n" +
                           $"<size=11><color=#ECEFF1>{selData.description}</color></size>";

        if (enterButtonText != null)
        {
            enterButtonText.text = $"ENTER DUNGEON (FLOOR {selectedFloor})";
        }

        if (enterButton != null)
        {
            enterButton.interactable = selUnlocked;
        }
    }

    private void ShowWarning(string warning)
    {
        if (detailsText == null) return;

        if (feedbackFlashCoroutine != null)
        {
            StopCoroutine(feedbackFlashCoroutine);
        }

        feedbackFlashCoroutine = StartCoroutine(FlashWarningRoutine(warning));
    }

    private System.Collections.IEnumerator FlashWarningRoutine(string warning)
    {
        detailsText.text = $"<b><color=#FF5252>[!] {warning}</color></b>\n<size=11><color=#FF8A80>You need to defeat the previous floor boss before challenging this floor!</color></size>";

        yield return new WaitForSeconds(2.8f);

        RefreshUI();
        feedbackFlashCoroutine = null;
    }

    #region Procedural UI Builder
    private void EnsureUIConstructed()
    {
        if (enterDungeonPanel == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                enterDungeonPanel = new GameObject("EnterDungeonPanel", typeof(RectTransform), typeof(Image));
                enterDungeonPanel.transform.SetParent(canvas.transform, false);
                RectTransform rt = enterDungeonPanel.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                Image bg = enterDungeonPanel.GetComponent<Image>();
                bg.color = new Color(0, 0, 0, 0.65f);
                enterDungeonPanel.SetActive(false);
            }
            else
            {
                return;
            }
        }

        // Check if our modal is already built and populated
        if (dialogPanel != null && floorCards.Count > 0)
        {
            return;
        }

        Transform existingModal = enterDungeonPanel.transform.Find("FloorSelectionModal");
        if (existingModal != null)
        {
            dialogPanel = existingModal.gameObject;
            if (floorCards.Count > 0)
                return;
        }

        // Hide legacy unstyled Yes/No buttons in the scene so they don't overlap
        for (int i = 0; i < enterDungeonPanel.transform.childCount; i++)
        {
            Transform child = enterDungeonPanel.transform.GetChild(i);
            if (child.name != "FloorSelectionModal")
            {
                child.gameObject.SetActive(false);
            }
        }

        BuildModalUI();
    }

    private void BuildModalUI()
    {
        if (floorList.Count == 0)
        {
            InitializeFloorData();
        }

        // 1. Modal Dialog Window
        dialogPanel = new GameObject("FloorSelectionModal", typeof(RectTransform), typeof(Image));
        dialogPanel.transform.SetParent(enterDungeonPanel.transform, false);
        RectTransform dialogRT = dialogPanel.GetComponent<RectTransform>();
        dialogRT.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRT.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRT.pivot = new Vector2(0.5f, 0.5f);
        dialogRT.sizeDelta = new Vector2(560, 620);
        dialogRT.anchoredPosition = Vector2.zero;

        Image dialogBg = dialogPanel.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(dialogBg, new Color(0.10f, 0.12f, 0.18f, 0.98f));

        // 2. Header Title
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(dialogPanel.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -16);
        titleRT.sizeDelta = new Vector2(-40, 32);

        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "SELECT DUNGEON FLOOR";
        titleText.color = new Color(1f, 0.85f, 0.35f);

        // 3. Subtitle / Prompt
        GameObject subtitleGO = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subtitleGO.transform.SetParent(dialogPanel.transform, false);
        RectTransform subRT = subtitleGO.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 1);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.pivot = new Vector2(0.5f, 1);
        subRT.anchoredPosition = new Vector2(0, -48);
        subRT.sizeDelta = new Vector2(-40, 20);

        subtitleText = subtitleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(subtitleText);
        subtitleText.fontSize = 11;
        subtitleText.fontStyle = FontStyles.Italic;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.text = "Defeat the previous floor boss to advance to higher floors";
        subtitleText.color = new Color(0.70f, 0.75f, 0.85f);

        // 4. Decorative divider
        GameObject divGO = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divGO.transform.SetParent(dialogPanel.transform, false);
        RectTransform divRT = divGO.GetComponent<RectTransform>();
        divRT.anchorMin = new Vector2(0.5f, 1);
        divRT.anchorMax = new Vector2(0.5f, 1);
        divRT.pivot = new Vector2(0.5f, 1);
        divRT.anchoredPosition = new Vector2(0, -72);
        divRT.sizeDelta = new Vector2(490, 2);
        divGO.GetComponent<Image>().color = new Color(0.85f, 0.70f, 0.25f, 0.5f);

        // 5. Tower Floors Container
        // Ordered with Floor 1 at the bottom, Floor 2 above, and so on!
        GameObject towerGO = new GameObject("TowerFloorsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        towerGO.transform.SetParent(dialogPanel.transform, false);
        RectTransform towerRT = towerGO.GetComponent<RectTransform>();
        towerRT.anchorMin = new Vector2(0.5f, 1);
        towerRT.anchorMax = new Vector2(0.5f, 1);
        towerRT.pivot = new Vector2(0.5f, 1);
        towerRT.anchoredPosition = new Vector2(0, -80);
        towerRT.sizeDelta = new Vector2(510, 325);

        VerticalLayoutGroup vlg = towerGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(6, 6, 4, 4);
        vlg.spacing = 7;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        floorCards.Clear();

        // Add floors in descending order: 5 at the top, down to 1 at the bottom!
        int numFloors = Mathf.Max(totalFloors, floorList.Count);
        for (int f = numFloors; f >= 1; f--)
        {
            CreateFloorCard(towerGO.transform, f);
        }

        // 6. Selected Floor Details Panel
        GameObject detailsBox = new GameObject("DetailsPanel", typeof(RectTransform), typeof(Image));
        detailsBox.transform.SetParent(dialogPanel.transform, false);
        RectTransform detailsBoxRT = detailsBox.GetComponent<RectTransform>();
        detailsBoxRT.anchorMin = new Vector2(0.5f, 0);
        detailsBoxRT.anchorMax = new Vector2(0.5f, 0);
        detailsBoxRT.pivot = new Vector2(0.5f, 0);
        detailsBoxRT.anchoredPosition = new Vector2(0, 78);
        detailsBoxRT.sizeDelta = new Vector2(500, 78);

        Image detailsBg = detailsBox.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(detailsBg, new Color(0.07f, 0.08f, 0.12f, 0.95f));

        // Boss Preview Box with Slot Frame
        GameObject bossBoxGO = new GameObject("BossPreviewBox", typeof(RectTransform), typeof(Image));
        bossBoxGO.transform.SetParent(detailsBox.transform, false);
        RectTransform bossBoxRT = bossBoxGO.GetComponent<RectTransform>();
        bossBoxRT.anchorMin = new Vector2(0, 0.5f);
        bossBoxRT.anchorMax = new Vector2(0, 0.5f);
        bossBoxRT.pivot = new Vector2(0, 0.5f);
        bossBoxRT.anchoredPosition = new Vector2(10, 0);
        bossBoxRT.sizeDelta = new Vector2(58, 58);
        Image bossBoxBg = bossBoxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(bossBoxBg, new Color(0.24f, 0.28f, 0.38f, 1f));

        GameObject bossIconGO = new GameObject("BossIcon", typeof(RectTransform), typeof(Image));
        bossIconGO.transform.SetParent(bossBoxGO.transform, false);
        RectTransform biRT = bossIconGO.GetComponent<RectTransform>();
        biRT.anchorMin = new Vector2(0.5f, 0.5f);
        biRT.anchorMax = new Vector2(0.5f, 0.5f);
        biRT.sizeDelta = new Vector2(46, 46);
        Image bossIcon = bossIconGO.GetComponent<Image>();
        bossIcon.preserveAspect = true;
        Sprite bSprite = DungeonUIAssetHelper.GetBossSprite();
        if (bSprite != null)
        {
            bossIcon.sprite = bSprite;
        }

        GameObject detailsTextGO = new GameObject("DetailsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        detailsTextGO.transform.SetParent(detailsBox.transform, false);
        RectTransform dtRT = detailsTextGO.GetComponent<RectTransform>();
        dtRT.anchorMin = Vector2.zero;
        dtRT.anchorMax = Vector2.one;
        dtRT.offsetMin = new Vector2(76, 6);
        dtRT.offsetMax = new Vector2(-12, -6);

        detailsText = detailsTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(detailsText);
        detailsText.fontSize = 11.5f;
        detailsText.alignment = TextAlignmentOptions.TopLeft;
        detailsText.textWrappingMode = TextWrappingModes.Normal;
        detailsText.text = "Select a floor to view details.";

        // 7. Footer Action Buttons (Enter Dungeon & Back)
        GameObject footerGO = new GameObject("FooterButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        footerGO.transform.SetParent(dialogPanel.transform, false);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0.5f, 0);
        footerRT.anchorMax = new Vector2(0.5f, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.anchoredPosition = new Vector2(0, 16);
        footerRT.sizeDelta = new Vector2(500, 48);

        HorizontalLayoutGroup hlg = footerGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // "Enter Dungeon" (YES) Button
        GameObject enterBtnGO = new GameObject("BtnEnterDungeon", typeof(RectTransform), typeof(Image), typeof(Button));
        enterBtnGO.transform.SetParent(footerGO.transform, false);
        Image enterImg = enterBtnGO.GetComponent<Image>();
        enterButton = enterBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(enterButton, enterImg, new Color(0.18f, 0.50f, 0.22f, 1f), new Color(0.24f, 0.65f, 0.30f, 1f));
        enterButton.onClick.AddListener(EnterSelectedFloor);

        GameObject enterTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        enterTextGO.transform.SetParent(enterBtnGO.transform, false);
        RectTransform etRT = enterTextGO.GetComponent<RectTransform>();
        etRT.anchorMin = Vector2.zero;
        etRT.anchorMax = Vector2.one;
        etRT.sizeDelta = Vector2.zero;
        enterButtonText = enterTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(enterButtonText);
        enterButtonText.fontSize = 14;
        enterButtonText.fontStyle = FontStyles.Bold;
        enterButtonText.alignment = TextAlignmentOptions.Center;
        enterButtonText.text = "ENTER DUNGEON (FLOOR 1)";
        enterButtonText.color = Color.white;

        // "Back" (NO) Button
        GameObject closeBtnGO = new GameObject("BtnCancel", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnGO.transform.SetParent(footerGO.transform, false);
        Image closeImg = closeBtnGO.GetComponent<Image>();
        closeButton = closeBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(closeButton, closeImg, new Color(0.24f, 0.28f, 0.35f, 1f), new Color(0.32f, 0.38f, 0.48f, 1f));
        closeButton.onClick.AddListener(Close);

        GameObject closeTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTextGO.transform.SetParent(closeBtnGO.transform, false);
        RectTransform ctRT = closeTextGO.GetComponent<RectTransform>();
        ctRT.anchorMin = Vector2.zero;
        ctRT.anchorMax = Vector2.one;
        ctRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI closeText = closeTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(closeText);
        closeText.fontSize = 13;
        closeText.fontStyle = FontStyles.Bold;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.text = "BACK";
        closeText.color = new Color(0.9f, 0.9f, 0.9f);

        // Top-right 'X' close button
        GameObject xBtnGO = new GameObject("BtnCloseX", typeof(RectTransform), typeof(Image), typeof(Button));
        xBtnGO.transform.SetParent(dialogPanel.transform, false);
        RectTransform xRT = xBtnGO.GetComponent<RectTransform>();
        xRT.anchorMin = new Vector2(1, 1);
        xRT.anchorMax = new Vector2(1, 1);
        xRT.pivot = new Vector2(1, 1);
        xRT.anchoredPosition = new Vector2(-12, -12);
        xRT.sizeDelta = new Vector2(28, 28);
        Image xImg = xBtnGO.GetComponent<Image>();
        Button xBtn = xBtnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(xBtn, xImg, new Color(0.25f, 0.28f, 0.35f, 0.9f), new Color(0.85f, 0.25f, 0.25f, 1f));
        xBtn.onClick.AddListener(Close);

        GameObject xTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        xTextGO.transform.SetParent(xBtnGO.transform, false);
        RectTransform xtRT = xTextGO.GetComponent<RectTransform>();
        xtRT.anchorMin = Vector2.zero;
        xtRT.anchorMax = Vector2.one;
        xtRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI xt = xTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(xt);
        xt.fontSize = 14;
        xt.fontStyle = FontStyles.Bold;
        xt.alignment = TextAlignmentOptions.Center;
        xt.text = "X";
        xt.color = Color.white;
    }

    private void CreateFloorCard(Transform parent, int floorNum)
    {
        FloorDisplayData data = GetFloorData(floorNum);

        GameObject cardGO = new GameObject($"FloorCard_{floorNum}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        cardGO.transform.SetParent(parent, false);

        LayoutElement le = cardGO.GetComponent<LayoutElement>();
        le.preferredHeight = 56;
        le.minHeight = 54;

        Image cardBg = cardGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(cardBg, new Color(0.12f, 0.16f, 0.23f, 0.95f));

        Button cardBtn = cardGO.GetComponent<Button>();
        cardBtn.targetGraphic = cardBg;
        ColorBlock cb = cardBtn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        cb.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        cb.selectedColor = Color.white;
        cardBtn.colors = cb;

        int capturedFloor = floorNum;
        cardBtn.onClick.AddListener(() => SelectFloor(capturedFloor));

        // 1. Left Badge (e.g. "FLOOR 1")
        GameObject badgeGO = new GameObject("BadgeBox", typeof(RectTransform), typeof(Image));
        badgeGO.transform.SetParent(cardGO.transform, false);
        RectTransform bRT = badgeGO.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0, 0.5f);
        bRT.anchorMax = new Vector2(0, 0.5f);
        bRT.pivot = new Vector2(0, 0.5f);
        bRT.anchoredPosition = new Vector2(8, 0);
        bRT.sizeDelta = new Vector2(64, 40);
        Image badgeBg = badgeGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlotImage(badgeBg, new Color(0.18f, 0.22f, 0.32f, 1f));

        GameObject bTextGO = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bTextGO.transform.SetParent(badgeGO.transform, false);
        RectTransform btRT = bTextGO.GetComponent<RectTransform>();
        btRT.anchorMin = Vector2.zero;
        btRT.anchorMax = Vector2.one;
        btRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI bText = bTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(bText);
        bText.fontSize = 12;
        bText.fontStyle = FontStyles.Bold;
        bText.alignment = TextAlignmentOptions.Center;
        bText.text = $"FLOOR {floorNum}";
        bText.color = new Color(0.7f, 0.85f, 1f);

        // 2. Middle Content (Floor Name & Boss)
        GameObject contentGO = new GameObject("ContentBox", typeof(RectTransform));
        contentGO.transform.SetParent(cardGO.transform, false);
        RectTransform cRT = contentGO.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 0);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0, 0.5f);
        cRT.offsetMin = new Vector2(80, 4);
        cRT.offsetMax = new Vector2(-140, -4);

        // Floor Name
        GameObject nameGO = new GameObject("FloorName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGO.transform.SetParent(contentGO.transform, false);
        RectTransform nRT = nameGO.GetComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0, 0.5f);
        nRT.anchorMax = new Vector2(1, 1);
        nRT.pivot = new Vector2(0, 0.5f);
        nRT.anchoredPosition = new Vector2(0, 2);
        nRT.sizeDelta = new Vector2(0, 22);
        TextMeshProUGUI nameText = nameGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(nameText);
        nameText.fontSize = 13;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.text = data.floorName;
        nameText.color = Color.white;

        // Boss Info
        GameObject bossGO = new GameObject("BossInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
        bossGO.transform.SetParent(contentGO.transform, false);
        RectTransform bossRT = bossGO.GetComponent<RectTransform>();
        bossRT.anchorMin = new Vector2(0, 0);
        bossRT.anchorMax = new Vector2(1, 0.5f);
        bossRT.pivot = new Vector2(0, 0.5f);
        bossRT.anchoredPosition = new Vector2(0, -2);
        bossRT.sizeDelta = new Vector2(0, 18);
        TextMeshProUGUI bossText = bossGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(bossText);
        bossText.fontSize = 10;
        bossText.alignment = TextAlignmentOptions.Left;
        bossText.text = $"Boss: {data.bossName}";
        bossText.color = new Color(0.6f, 0.72f, 0.82f);

        // 3. Right Status Badge
        GameObject statusBoxGO = new GameObject("StatusBadge", typeof(RectTransform), typeof(Image));
        statusBoxGO.transform.SetParent(cardGO.transform, false);
        RectTransform sRT = statusBoxGO.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(1, 0.5f);
        sRT.anchorMax = new Vector2(1, 0.5f);
        sRT.pivot = new Vector2(1, 0.5f);
        sRT.anchoredPosition = new Vector2(-8, 0);
        sRT.sizeDelta = new Vector2(120, 32);

        Image statusBg = statusBoxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(statusBg, new Color(0.15f, 0.25f, 0.4f, 0.85f));

        GameObject sTextGO = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        sTextGO.transform.SetParent(statusBoxGO.transform, false);
        RectTransform stRT = sTextGO.GetComponent<RectTransform>();
        stRT.anchorMin = Vector2.zero;
        stRT.anchorMax = Vector2.one;
        stRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI sText = sTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(sText);
        sText.fontSize = 11;
        sText.fontStyle = FontStyles.Bold;
        sText.alignment = TextAlignmentOptions.Center;
        sText.text = "READY";
        sText.color = Color.white;

        FloorCardUI cardUI = new FloorCardUI
        {
            rootGO = cardGO,
            button = cardBtn,
            backgroundImage = cardBg,
            floorBadgeText = bText,
            floorNameText = nameText,
            bossInfoText = bossText,
            statusBadgeText = sText,
            statusBadgeBg = statusBg,
            floorNumber = floorNum
        };

        floorCards[floorNum] = cardUI;
    }
    #endregion
}