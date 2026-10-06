using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            GameObject go = new("DungeonEventUI");
            go.transform.SetParent(canvas.transform, false);
            _instance = go.AddComponent<DungeonEventUI>();
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

        EnsureUIBuilt();

        if (eventPanel != null)
        {
            eventPanel.SetActive(false);
        }
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
        for (int i = 0; i < activeChoiceButtons.Count; i++)
        {
            if (activeChoiceButtons[i] != null)
            {
                Destroy(activeChoiceButtons[i]);
            }
        }
        activeChoiceButtons.Clear();

        if (currentEvent == null || currentEvent.choices == null || choicesContainer == null) return;

        for (int i = 0; i < currentEvent.choices.Count; i++)
        {
            int index = i;
            DungeonEventChoice choice = currentEvent.choices[i];
            if (choice == null) continue;

            GameObject btnGO = CreateChoiceButton(choice, index);
            if (btnGO != null)
            {
                btnGO.transform.SetParent(choicesContainer, false);
                activeChoiceButtons.Add(btnGO);
            }
        }
    }

    private GameObject CreateChoiceButton(DungeonEventChoice choice, int index)
    {
        bool canSelect = choice.CanPlayerSelect(currentPlayer, currentBackpack, out string reason);

        // Choice Button with Sliced Frame
        GameObject btnGO = new($"ChoiceBtn_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(700, 52);

        Image img = btnGO.GetComponent<Image>();
        Button btn = btnGO.GetComponent<Button>();
        btn.interactable = canSelect;
        btn.onClick.AddListener(() => OnChoiceSelected(choice));

        if (canSelect)
        {
            Color baseCol = new Color(0.15f, 0.18f, 0.25f, 0.98f);
            DungeonUIAssetHelper.StyleButton(btn, img, baseCol, new Color(0.22f, 0.28f, 0.38f, 1f), new Color(0.12f, 0.14f, 0.20f, 1f));
        }
        else
        {
            Color disabledCol = new Color(0.10f, 0.11f, 0.14f, 0.75f);
            DungeonUIAssetHelper.StyleButton(btn, img, disabledCol);
        }

        // Text Content
        GameObject textGO = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(btnGO.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(16, 4);
        textRT.offsetMax = new Vector2(-16, -4);

        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.fontSize = 13;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        string labelColor = canSelect ? "#FFD700" : "#777777";
        string detailsColor = canSelect ? "#E4EBF2" : "#666666";

        if (canSelect)
        {
            tmp.text = $"<color={labelColor}><b>{choice.choiceLabel}</b></color>  <color={detailsColor}>{choice.choiceDetails}</color>";
        }
        else
        {
            tmp.text = $"<color={labelColor}><b>{choice.choiceLabel}</b></color>  <color={detailsColor}>{choice.choiceDetails}</color>\n<size=11><color=#FF6666>({reason})</color></size>";
        }

        return btnGO;
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

    #region Procedural UI Building
    private Canvas GetCanvas()
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        return canvas;
    }

    private void EnsureUIBuilt()
    {
        if (eventPanel != null) return;

        Canvas parentCanvas = GetCanvas();
        if (parentCanvas == null)
        {
            Debug.LogError("[DungeonEventUI] No Canvas found in scene!");
            return;
        }

        // 1. Fullscreen Dark Overlay
        GameObject overlayGO = new("DungeonEventPanel", typeof(RectTransform), typeof(Image));
        overlayGO.transform.SetParent(parentCanvas.transform, false);
        eventPanel = overlayGO;

        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;

        Image overlayImg = overlayGO.GetComponent<Image>();
        overlayImg.color = new Color(0.04f, 0.05f, 0.08f, 0.88f);

        // 2. Main Parchment Modal (Slay the Spire Style Card with 9-Sliced Frame)
        GameObject modalGO = new("EventCardModal", typeof(RectTransform), typeof(Image));
        modalGO.transform.SetParent(overlayGO.transform, false);

        RectTransform modalRT = modalGO.GetComponent<RectTransform>();
        modalRT.anchorMin = new Vector2(0.5f, 0.5f);
        modalRT.anchorMax = new Vector2(0.5f, 0.5f);
        modalRT.pivot = new Vector2(0.5f, 0.5f);
        modalRT.sizeDelta = new Vector2(780, 540);

        Image modalImg = modalGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(modalImg, new Color(0.10f, 0.12f, 0.17f, 0.98f));

        // 3. Top Banner with Sliced Frame
        GameObject headerGO = new("Header", typeof(RectTransform), typeof(Image));
        headerGO.transform.SetParent(modalGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 74);

        Image headerImg = headerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(headerImg, new Color(0.18f, 0.15f, 0.22f, 1f));

        // Category Badge
        GameObject catGO = new("CategoryText", typeof(RectTransform), typeof(TextMeshProUGUI));
        catGO.transform.SetParent(headerGO.transform, false);
        RectTransform catRT = catGO.GetComponent<RectTransform>();
        catRT.anchorMin = new Vector2(0, 1);
        catRT.anchorMax = new Vector2(1, 1);
        catRT.offsetMin = new Vector2(16, -26);
        catRT.offsetMax = new Vector2(-16, -8);

        categoryText = catGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(categoryText);
        categoryText.fontSize = 11;
        categoryText.fontStyle = FontStyles.Bold;
        categoryText.alignment = TextAlignmentOptions.Center;
        categoryText.color = new Color(1f, 0.85f, 0.35f);
        categoryText.text = "[ ENCOUNTER ]";

        // Title Text
        GameObject titleGO = new("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(headerGO.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = Vector2.one;
        titleRT.offsetMin = new Vector2(16, 8);
        titleRT.offsetMax = new Vector2(-16, -26);

        titleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.text = "Mysterious Encounter";

        // 4. Center Narrative Story Box with Sliced Frame
        GameObject storyBoxGO = new("NarrativeBox", typeof(RectTransform), typeof(Image));
        storyBoxGO.transform.SetParent(modalGO.transform, false);
        RectTransform storyBoxRT = storyBoxGO.GetComponent<RectTransform>();
        storyBoxRT.anchorMin = new Vector2(0, 1);
        storyBoxRT.anchorMax = new Vector2(1, 1);
        storyBoxRT.pivot = new Vector2(0.5f, 1);
        storyBoxRT.anchoredPosition = new Vector2(0, -84);
        storyBoxRT.sizeDelta = new Vector2(-36, 175);

        Image storyBoxImg = storyBoxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(storyBoxImg, new Color(0.06f, 0.08f, 0.11f, 0.95f));

        GameObject narrativeGO = new("NarrativeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        narrativeGO.transform.SetParent(storyBoxGO.transform, false);
        RectTransform narrativeRT = narrativeGO.GetComponent<RectTransform>();
        narrativeRT.anchorMin = Vector2.zero;
        narrativeRT.anchorMax = Vector2.one;
        narrativeRT.offsetMin = new Vector2(16, 12);
        narrativeRT.offsetMax = new Vector2(-16, -12);

        narrativeText = narrativeGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(narrativeText);
        narrativeText.fontSize = 13;
        narrativeText.lineSpacing = 16;
        narrativeText.color = new Color(0.9f, 0.93f, 0.97f);
        narrativeText.alignment = TextAlignmentOptions.TopLeft;
        narrativeText.text = "A mysterious story unfolds before you...";

        // 5. Choices Container (Slay the Spire Vertical Stack)
        GameObject choicesGO = new("ChoicesContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        choicesGO.transform.SetParent(modalGO.transform, false);
        choicesContainer = choicesGO.GetComponent<RectTransform>();
        choicesContainer.anchorMin = new Vector2(0, 0);
        choicesContainer.anchorMax = new Vector2(1, 1);
        choicesContainer.offsetMin = new Vector2(18, 70);
        choicesContainer.offsetMax = new Vector2(-18, -268);

        VerticalLayoutGroup vlg = choicesGO.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        // 6. Footer & Continue Button
        GameObject footerGO = new("Footer", typeof(RectTransform));
        footerGO.transform.SetParent(modalGO.transform, false);
        RectTransform footerRT = footerGO.GetComponent<RectTransform>();
        footerRT.anchorMin = new Vector2(0, 0);
        footerRT.anchorMax = new Vector2(1, 0);
        footerRT.pivot = new Vector2(0.5f, 0);
        footerRT.sizeDelta = new Vector2(0, 60);

        // Status Feedback Text
        GameObject statusGO = new("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGO.transform.SetParent(footerGO.transform, false);
        RectTransform statusRT = statusGO.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0, 0);
        statusRT.anchorMax = new Vector2(0.6f, 1);
        statusRT.offsetMin = new Vector2(18, 4);
        statusRT.offsetMax = new Vector2(-10, -4);

        statusFeedbackText = statusGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(statusFeedbackText);
        statusFeedbackText.fontSize = 12;
        statusFeedbackText.alignment = TextAlignmentOptions.MidlineLeft;
        statusFeedbackText.text = "";

        // Continue Button
        GameObject contGO = new("BtnContinue", typeof(RectTransform), typeof(Image), typeof(Button));
        contGO.transform.SetParent(footerGO.transform, false);
        RectTransform contRT = contGO.GetComponent<RectTransform>();
        contRT.anchorMin = new Vector2(1, 0.5f);
        contRT.anchorMax = new Vector2(1, 0.5f);
        contRT.sizeDelta = new Vector2(210, 40);
        contRT.anchoredPosition = new Vector2(-120, 0);

        Image contImg = contGO.GetComponent<Image>();
        continueButton = contGO.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(continueButton, contImg, new Color(0.2f, 0.55f, 0.35f, 1f), new Color(0.26f, 0.70f, 0.42f, 1f));
        continueButton.onClick.AddListener(OnContinueClicked);

        GameObject contTextGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        contTextGO.transform.SetParent(contGO.transform, false);
        RectTransform contTextRT = contTextGO.GetComponent<RectTransform>();
        contTextRT.anchorMin = Vector2.zero;
        contTextRT.anchorMax = Vector2.one;
        contTextRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI contTMP = contTextGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(contTMP);
        contTMP.fontSize = 13;
        contTMP.fontStyle = FontStyles.Bold;
        contTMP.alignment = TextAlignmentOptions.Center;
        contTMP.color = Color.white;
        contTMP.text = "CONTINUE EXPEDITION >>";
    }
    #endregion
}
