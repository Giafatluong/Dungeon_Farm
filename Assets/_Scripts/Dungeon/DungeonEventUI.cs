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
    [SerializeField] public GameObject eventPanel;
    [SerializeField] public RectTransform choicesContainer;
    [SerializeField] public Canvas canvas;

    [Header("Text Labels")]
    [SerializeField] public TextMeshProUGUI categoryText;
    [SerializeField] public TextMeshProUGUI titleText;
    [SerializeField] public TextMeshProUGUI narrativeText;
    [SerializeField] public TextMeshProUGUI statusFeedbackText;

    [Header("Proceed Controls")]
    [SerializeField] public Button continueButton;

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

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            UIEscapeManager.ConsumeEscape();
            OnContinueClicked();
        }
    }
    #endregion

    #region UI Construction
    public void EnsureUIBuilt()
    {
        // UI is authored and bound directly in the scene.
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

        DungeonTutorialController.Instance?.HideBanner();
        DungeonTutorialController.Instance?.HidePointer();

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
        // Clean up any existing choice buttons in container
        if (choicesContainer != null)
        {
            List<Transform> toRemove = new();
            foreach (Transform child in choicesContainer)
            {
                if (child != null) toRemove.Add(child);
            }
            for (int i = 0; i < toRemove.Count; i++)
            {
                toRemove[i].SetParent(null);
                Destroy(toRemove[i].gameObject);
            }
        }
        activeChoiceButtons.Clear();

        if (currentEvent == null || currentEvent.choices == null || choicesContainer == null)
            return;

        // Disable any VerticalLayoutGroup on container to prevent delayed layout override / overlap conflicts
        VerticalLayoutGroup vlg = choicesContainer.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            vlg.enabled = false;
        }

        int uiLayer = choicesContainer.gameObject.layer;
        float paddingTop = 8f;
        float buttonHeight = 44f;
        float spacing = 10f;

        // Create button for each choice in event with rock-solid pixel-perfect positioning
        for (int i = 0; i < currentEvent.choices.Count; i++)
        {
            DungeonEventChoice choice = currentEvent.choices[i];
            if (choice == null) continue;

            string reason = string.Empty;
            bool canSelect = choice.isLeaveChoice || choice.CanPlayerSelect(currentPlayer, currentBackpack, out reason);

            GameObject btnGO = new GameObject($"ChoiceButton_{i}", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            btnGO.layer = uiLayer;
            btnGO.transform.SetParent(choicesContainer, false);

            RectTransform btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0, 1);
            btnRT.anchorMax = new Vector2(1, 1);
            btnRT.pivot = new Vector2(0.5f, 1f);

            float startY = -paddingTop - (i * (buttonHeight + spacing));
            btnRT.anchoredPosition = new Vector2(0, startY);
            btnRT.sizeDelta = new Vector2(-32f, buttonHeight);

            UnityEngine.UI.Image btnImg = btnGO.GetComponent<UnityEngine.UI.Image>();
            UnityEngine.UI.Button btn = btnGO.GetComponent<UnityEngine.UI.Button>();

            if (canSelect)
            {
                DungeonUIAssetHelper.StyleButton(btn, btnImg, new Color(0.16f, 0.20f, 0.30f, 0.95f), new Color(0.26f, 0.32f, 0.45f, 1f));
                btn.interactable = true;
            }
            else
            {
                DungeonUIAssetHelper.StyleButton(btn, btnImg, new Color(0.10f, 0.11f, 0.15f, 0.6f), new Color(0.10f, 0.11f, 0.15f, 0.6f));
                btn.interactable = false;
            }

            Outline outline = btnGO.AddComponent<Outline>();
            outline.effectColor = canSelect ? new Color(0.40f, 0.52f, 0.72f, 0.75f) : new Color(0.2f, 0.2f, 0.25f, 0.4f);
            outline.effectDistance = new Vector2(1, -1);

            // Label text
            GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            textGO.layer = uiLayer;
            textGO.transform.SetParent(btnGO.transform, false);
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            textRT.offsetMin = new Vector2(18, 0);
            textRT.offsetMax = new Vector2(-18, 0);

            TMPro.TextMeshProUGUI label = textGO.GetComponent<TMPro.TextMeshProUGUI>();
            DungeonUIAssetHelper.ApplyFont(label);
            label.fontSize = 12.5f;
            label.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            label.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            label.color = Color.white;

            if (canSelect)
            {
                label.text = string.IsNullOrEmpty(choice.choiceDetails)
                    ? $"<b><color=#FFD070>{choice.choiceLabel}</color></b>"
                    : $"<b><color=#FFD070>{choice.choiceLabel}</color></b>   <size=11.5><color=#D0D8E6>{choice.choiceDetails}</color></size>";
            }
            else
            {
                label.text = $"<b><color=#777777>{choice.choiceLabel}</color></b>   <size=11.5><color=#FF6666><b>[Thiếu: {reason}]</b></color></size>";
            }

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

        // Check if player died from cost/trap damage
        if (currentPlayer != null && currentPlayer.currentHealth <= 0)
        {
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
            narrativeText.text = $"{currentEvent?.narrativeStory}\n\n<color={outcomeColor}><b>Kết quả:</b>\n{outcomeText}</color>";
        }

        if (statusFeedbackText != null)
        {
            statusFeedbackText.text = isSuccess
                ? "<color=#88FF88>Sự kiện đã hoàn tất thành công.</color>"
                : "<color=#FF6666>Rủi ro không như ý muốn!</color>";
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
            CombatUI.Instance.RefreshAll();
        }
    }

    public void OnContinueClicked()
    {
        Close();
    }
    #endregion

}

