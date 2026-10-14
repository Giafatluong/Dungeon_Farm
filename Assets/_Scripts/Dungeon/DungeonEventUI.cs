using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI for Slay the Spire style Mystery / Shrine / Fountain / Encounter events in the Dungeon.
/// Features atmospheric card modal, story narrative, choice cards with cost & gamble odds,
/// outcome feedback, and integration with the Wandering Merchant shop.
/// Styled using project fantasy pixel art assets (UI_Frame, UI_Slot).
/// </summary>
public class DungeonEventUI : MonoBehaviour
{
    #region Singleton & References
    private static DungeonEventUI _instance;
    public static DungeonEventUI Instance => _instance;

    public static DungeonEventUI EnsureInstance()
    {
        if (_instance != null) return _instance;

        DungeonEventUI found = FindFirstObjectByType<DungeonEventUI>(FindObjectsInactive.Include);
        if (found != null)
        {
            _instance = found;
            return _instance;
        }

        Canvas canvas = DungeonUIAssetHelper.GetMainScreenCanvas();
        if (canvas != null)
        {
            GameObject go = new("DungeonEventUI");
            go.transform.SetParent(canvas.transform, false);
            _instance = go.AddComponent<DungeonEventUI>();
            _instance.canvas = canvas;
            return _instance;
        }

        return null;
    }

    [Header("Panels")]
    [SerializeField] private GameObject eventPanel;
    [SerializeField] private RectTransform choicesContainer;
    [SerializeField] private Canvas canvas;

    [Header("Text Labels")]
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI narrativeText;
    [SerializeField] private TextMeshProUGUI statusFeedbackText;

    [Header("Proceed Controls")]
    [SerializeField] private Button continueButton;

    private DungeonEvent currentEvent;
    private PlayerStats currentPlayer;
    private ItemContainer currentBackpack;
    private System.Action onCompleteCallback;

    private readonly List<GameObject> activeChoiceButtons = new();

    public bool IsOpen => eventPanel != null && eventPanel.activeSelf;
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

        if (eventPanel != null)
        {
            eventPanel.SetActive(false);
        }

        // Fix Bug 2: Bind continueButton listener
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion

    #region UI Construction
    public void EnsureUIBuilt()
    {
        if (eventPanel != null) return;

        Canvas parentCanvas = canvas != null ? canvas : DungeonUIAssetHelper.GetMainScreenCanvas();
        if (parentCanvas == null)
        {
            Debug.LogError("[DungeonEventUI] No Canvas found in scene!");
            return;
        }

        // 1. Dark overlay
        GameObject overlayGO = new("DungeonEventPanel", typeof(RectTransform), typeof(Image));
        overlayGO.transform.SetParent(parentCanvas.transform, false);
        eventPanel = overlayGO;

        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;

        Image overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);

        // 2. Main Window Modal
        GameObject modalGO = new("EventModal", typeof(RectTransform), typeof(Image));
        modalGO.transform.SetParent(overlayGO.transform, false);

        RectTransform modalRT = modalGO.GetComponent<RectTransform>();
        modalRT.anchorMin = new Vector2(0.5f, 0.5f);
        modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(760, 520);

        Image modalImg = modalGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(modalImg, new Color(0.12f, 0.13f, 0.18f, 0.98f));

        // 3. Header Banner
        GameObject headerGO = new("Header", typeof(RectTransform), typeof(Image));
        headerGO.transform.SetParent(modalGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 68);

        Image headerImg = headerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(headerImg, new Color(0.20f, 0.15f, 0.28f, 1f));

        // Category Text
        GameObject catGO = new("CategoryText", typeof(RectTransform), typeof(TextMeshProUGUI));
        catGO.transform.SetParent(headerGO.transform, false);
        RectTransform catRT = catGO.GetComponent<RectTransform>();
        catRT.anchorMin = new Vector2(0, 1);
        catRT.anchorMax = new Vector2(1, 1);
        catRT.offsetMin = new Vector2(24, -26);
        catRT.offsetMax = new Vector2(-24, -6);

        categoryText = catGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(categoryText);
        categoryText.fontSize = 12f;
        categoryText.fontStyle = FontStyles.Bold;
        categoryText.alignment = TextAlignmentOptions.MidlineLeft;

        // Title Text
        GameObject titleGO = new("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(headerGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = new Vector2(24, 6);
        titleRT.offsetMax = new Vector2(-24, -28);

        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 19f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.88f, 0.25f);
        titleText.alignment = TextAlignmentOptions.MidlineLeft;

        // 4. Narrative Box
        GameObject narrativeBoxGO = new("NarrativeBox", typeof(RectTransform), typeof(Image));
        narrativeBoxGO.transform.SetParent(modalGO.transform, false);
        RectTransform narrBoxRT = narrativeBoxGO.GetComponent<RectTransform>();
        narrBoxRT.anchorMin = new Vector2(0, 0.52f);
        narrBoxRT.anchorMax = new Vector2(1, 1);
        narrBoxRT.offsetMin = new Vector2(24, 0);
        narrBoxRT.offsetMax = new Vector2(-24, -76);

        Image narrBoxImg = narrativeBoxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(narrBoxImg, new Color(0.07f, 0.08f, 0.12f, 0.95f));

        GameObject narrTextGO = new("NarrativeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        narrTextGO.transform.SetParent(narrativeBoxGO.transform, false);
        RectTransform narrTextRT = narrTextGO.GetComponent<RectTransform>();
        narrTextRT.anchorMin = Vector2.zero;
        narrTextRT.anchorMax = Vector2.one;
        narrTextRT.offsetMin = new Vector2(16, 12);
        narrTextRT.offsetMax = new Vector2(-16, -12);

        narrativeText = narrTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(narrativeText);
        narrativeText.fontSize = 13.5f;
        narrativeText.lineSpacing = 6f;
        narrativeText.color = new Color(0.92f, 0.94f, 0.98f);
        narrativeText.alignment = TextAlignmentOptions.TopLeft;

        // 5. Choices Container
        GameObject choicesGO = new("ChoicesContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        choicesGO.transform.SetParent(modalGO.transform, false);
        choicesContainer = choicesGO.GetComponent<RectTransform>();
        choicesContainer.anchorMin = new Vector2(0, 0);
        choicesContainer.anchorMax = new Vector2(1, 0.50f);
        choicesContainer.offsetMin = new Vector2(24, 56);
        choicesContainer.offsetMax = new Vector2(-24, 0);

        VerticalLayoutGroup vlg = choicesGO.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 6. Footer: StatusFeedbackText & ContinueButton
        GameObject feedbackGO = new("StatusFeedbackText", typeof(RectTransform), typeof(TextMeshProUGUI));
        feedbackGO.transform.SetParent(modalGO.transform, false);
        RectTransform fbRT = feedbackGO.GetComponent<RectTransform>();
        fbRT.anchorMin = new Vector2(0, 0);
        fbRT.anchorMax = new Vector2(0.68f, 0);
        fbRT.offsetMin = new Vector2(24, 12);
        fbRT.offsetMax = new Vector2(0, 48);

        statusFeedbackText = feedbackGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(statusFeedbackText);
        statusFeedbackText.fontSize = 12.5f;
        statusFeedbackText.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject btnGO = new("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(modalGO.transform, false);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1, 0);
        btnRT.anchorMax = new Vector2(1, 0);
        btnRT.pivot = new Vector2(1, 0);
        btnRT.anchoredPosition = new Vector2(-24, 12);
        btnRT.sizeDelta = new Vector2(160, 38);

        Image btnImg = btnGO.GetComponent<Image>();
        continueButton = btnGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(continueButton, btnImg, new Color(0.18f, 0.55f, 0.28f));
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(OnContinueClicked);

        GameObject btnTxtGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTxtGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTxtRT = btnTxtGO.GetComponent<RectTransform>();
        btnTxtRT.anchorMin = Vector2.zero;
        btnTxtRT.anchorMax = Vector2.one;
        btnTxtRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnTxt = btnTxtGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(btnTxt);
        btnTxt.text = "TIẾP TỤC [->]";
        btnTxt.fontSize = 13f;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.alignment = TextAlignmentOptions.Center;
        btnTxt.color = Color.white;
    }
    #endregion

    #region Open & Close
    public void OpenEvent(DungeonEvent evt, PlayerStats player, ItemContainer backpack, System.Action onComplete)
    {
        EnsureUIBuilt();

        currentEvent = evt;
        currentPlayer = player;
        currentBackpack = backpack;
        onCompleteCallback = onComplete;

        if (eventPanel != null)
        {
            eventPanel.SetActive(true);
            eventPanel.transform.SetAsLastSibling();
        }

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }

        if (statusFeedbackText != null)
        {
            statusFeedbackText.text = "";
        }

        DisplayEventContent();
        Debug.Log($"[DungeonEventUI] Opened encounter: '{evt?.eventTitle}'");
    }

    public void Close()
    {
        if (eventPanel != null)
        {
            eventPanel.SetActive(false);
        }

        onCompleteCallback?.Invoke();
        onCompleteCallback = null;
    }
    #endregion

    #region Event Rendering
    private void DisplayEventContent()
    {
        if (currentEvent == null) return;

        if (categoryText != null)
        {
            categoryText.text = $"[ {currentEvent.eventCategory.ToUpper()} ]";
            categoryText.color = GetCategoryColor(currentEvent.eventCategory);
        }

        if (titleText != null)
        {
            titleText.text = currentEvent.eventTitle;
        }

        if (narrativeText != null)
        {
            narrativeText.text = currentEvent.narrativeStory;
        }

        BuildChoiceButtons();
    }

    private Color GetCategoryColor(string category)
    {
        return category switch
        {
            "MERCHANT" => new Color(1f, 0.85f, 0.35f),
            "SHRINE" => new Color(0.85f, 0.55f, 1f),
            "SANCTUARY" => new Color(0.4f, 0.9f, 1f),
            "MYSTERY" => new Color(1f, 0.45f, 0.45f),
            "ENCOUNTER" => new Color(0.55f, 0.95f, 0.65f),
            _ => Color.white
        };
    }

    private void BuildChoiceButtons()
    {
        // Xóa các button cũ
        for (int i = 0; i < activeChoiceButtons.Count; i++)
        {
            if (activeChoiceButtons[i] != null)
            {
                Destroy(activeChoiceButtons[i]);
            }
        }
        activeChoiceButtons.Clear();

        // Fix Bug 3: Tạo button cho từng lựa chọn trong event
        if (currentEvent == null || currentEvent.choices == null || choicesContainer == null)
            return;

        for (int i = 0; i < currentEvent.choices.Count; i++)
        {
            DungeonEventChoice choice = currentEvent.choices[i];
            if (choice == null) continue;

            // Tạo button cho lựa chọn
            GameObject btnGO = new GameObject($"ChoiceButton_{i}", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            btnGO.transform.SetParent(choicesContainer, false);

            RectTransform btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(400, 44);

            UnityEngine.UI.Image btnImg = btnGO.GetComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Button btn = btnGO.GetComponent<UnityEngine.UI.Button>();
            DungeonUIAssetHelper.StyleButton(btn, btnImg, new Color(0.16f, 0.19f, 0.28f, 0.95f), new Color(0.24f, 0.29f, 0.40f, 1f));

            // Label text
            GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            textGO.transform.SetParent(btnGO.transform, false);
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            textRT.offsetMin = new Vector2(14, 4);
            textRT.offsetMax = new Vector2(-14, -4);

            TMPro.TextMeshProUGUI label = textGO.GetComponent<TMPro.TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(label);
            label.fontSize = 12.5f;
            label.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            label.text = string.IsNullOrEmpty(choice.choiceDetails)
                ? $"<b><color=#FFD070>{choice.choiceLabel}</color></b>"
                : $"<b><color=#FFD070>{choice.choiceLabel}</color></b>   <size=11><color=#C8D0DC>{choice.choiceDetails}</color></size>";

            // Bind listener (capture local index)
            DungeonEventChoice capturedChoice = choice;
            btn.onClick.AddListener(() => OnChoiceSelected(capturedChoice));

            activeChoiceButtons.Add(btnGO);
        }
    }
    #endregion

    #region Outcome & Actions
    private void OnChoiceSelected(DungeonEventChoice choice)
    {
        // 1. Check if this choice opens the full Merchant UI
        if (choice.opensMerchantShop)
        {
            MerchantEvent mEvent = Object.FindFirstObjectByType<MerchantEvent>(FindObjectsInactive.Include);
            MerchantUI mUI = MerchantUI.EnsureInstance();
            if (mUI != null)
            {
                if (eventPanel != null) eventPanel.SetActive(false);
                mUI.Open(mEvent, currentBackpack, onContinue: () =>
                {
                    if (eventPanel != null) eventPanel.SetActive(true);
                    ResolveOutcome(choice.outcomeNarrative, isSuccess: true, choice.choiceLabel);
                });
                return;
            }
        }

        if (DungeonEventManager.Instance == null)
        {
            Close();
            return;
        }

        bool executed = DungeonEventManager.Instance.ExecuteChoice(
            choice,
            currentPlayer,
            currentBackpack,
            out string outcomeText,
            out bool isSuccess
        );

        if (!executed)
        {
            Debug.LogWarning("[DungeonEventUI] ExecuteChoice returned false.");
            Close();
            return;
        }

        ResolveOutcome(outcomeText, isSuccess, choice.choiceLabel);
    }

    private void ResolveOutcome(string outcomeText, bool isSuccess, string choiceLabel)
    {
        // 1. Hide active choices
        for (int i = 0; i < activeChoiceButtons.Count; i++)
        {
            if (activeChoiceButtons[i] != null)
            {
                activeChoiceButtons[i].SetActive(false);
            }
        }

        // 2. Update narrative story with outcome
        if (narrativeText != null)
        {
            string outcomeColor = isSuccess ? "#88FF88" : "#FF6666";
            narrativeText.text = $"{currentEvent?.narrativeStory}\n\n<color={outcomeColor}><b>Outcome:</b>\n{outcomeText}</color>";
        }

        if (statusFeedbackText != null)
        {
            statusFeedbackText.text = isSuccess
                ? "<color=#88FF88>Encounter resolved successfully.</color>"
                : "<color=#FF6666>The risk did not pay off!</color>";
        }

        // 3. Reveal Continue button - create one on the fly if it was never built
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
        }
        else
        {
            // Fallback: make the whole panel clickable to close
            Debug.LogWarning("[DungeonEventUI] continueButton is null - using panel click fallback.");
            if (eventPanel != null)
            {
                var clickClose = eventPanel.GetComponent<UnityEngine.UI.Button>();
                if (clickClose == null)
                {
                    clickClose = eventPanel.AddComponent<UnityEngine.UI.Button>();
                }
                clickClose.onClick.RemoveAllListeners();
                clickClose.onClick.AddListener(OnContinueClicked);
            }
        }

        if (CombatUI.Instance != null)
        {
            CombatUI.Instance.LogMessage($"[Event] {choiceLabel} resolved.");
            CombatUI.Instance.RefreshAll();
        }
    }

    private void OnContinueClicked()
    {
        Close();
    }
    #endregion

}

