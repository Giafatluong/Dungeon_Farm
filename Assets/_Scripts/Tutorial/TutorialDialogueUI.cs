using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Data container for a single tutorial step's instructions.
/// </summary>
public struct TutorialStepInfo
{
    public int stepIndex;
    public int totalSteps;
    public string objectiveTitle;
    public string targetLabel;
    public string actionKeyPrompt;
    public string actionInstruction;
    public string fullDialogue;
    public Vector3? targetPosition;
    public Transform targetTransform;
}

/// <summary>
/// Comprehensive, Crystal-Clear Tutorial Guidance UI.
/// Includes:
/// 1. Persistent HUD Quest Tracker (pinned top-left, shows step index, keys, and live distance/progress).
/// 2. Off-Screen Edge Radar Pointer (shows direction arrow and distance when target is outside camera view).
/// 3. In-Screen Floating Beacon (shows bouncing [E] prompt right above the active target).
/// 4. Sprout Spirit Dialogue Box (narrative context, dismissable with Space/Click without losing indicators).
/// </summary>
public class TutorialDialogueUI : MonoBehaviour
{
    public static TutorialDialogueUI Instance { get; private set; }

    [Header("Master Canvas")]
    private GameObject masterCanvasGO;
    private Canvas masterCanvas;

    [Header("1. HUD Quest Tracker (Top-Left)")]
    private GameObject trackerRootGO;
    private TextMeshProUGUI trackerHeaderText;
    private TextMeshProUGUI trackerTitleText;
    private TextMeshProUGUI trackerInstructionText;
    private TextMeshProUGUI trackerStatusText;

    [Header("2. Off-Screen Edge Pointer (Radar)")]
    private GameObject edgePointerGO;
    private RectTransform edgePointerRT;
    private RectTransform edgeArrowIconRT;
    private TextMeshProUGUI edgeDistanceText;

    [Header("3. In-Screen Target Beacon")]
    private GameObject worldMarkerGO;
    private RectTransform worldMarkerRT;
    private RectTransform worldArrowRT;
    private TextMeshProUGUI worldMarkerKeyText;
    private TextMeshProUGUI worldMarkerDistText;

    [Header("4. Sprout Spirit Dialogue Box (Bottom)")]
    private GameObject dialogueBoxGO;
    private RectTransform dialogueBoxRT;
    private Image avatarImg;
    private TextMeshProUGUI speakerNameText;
    private TextMeshProUGUI objectiveBadgeText;
    private TextMeshProUGUI dialogueBodyText;
    private TextMeshProUGUI continuePromptText;

    // State Tracking
    private TutorialStepInfo currentInfo;
    private bool hasActiveStep = false;
    private bool isDialogueActive = false;
    private Coroutine typewriterRoutine;
    private bool isTyping = false;
    private Camera cachedCam;
    private float nextTextUpdateTime = 0f;

    public System.Action OnDialogueDismissed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureUIConstructed();
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
        cachedCam = null;
        if (scene.name == "Dungeon")
        {
            // In Dungeon scene, the dungeon-specific tutorial (Floor 0 / CombatTutorialGuide / DungeonTutorialController) takes over
            HideDialogue();
        }
    }

    private void Update()
    {
        if (!hasActiveStep) return;

        // Check if full inventory or any modal UI (Chest, Kitchen, Statue, Pause) is open
        bool isMenuOpen = (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen)
                       || UIEscapeManager.IsAnyModalUIOpen();

        if (isMenuOpen)
        {
            // Temporarily hide world markers, edge pointers, and dialogue box so they never obstruct inventory or modal menus
            if (worldMarkerGO != null && worldMarkerGO.activeSelf) worldMarkerGO.SetActive(false);
            if (edgePointerGO != null && edgePointerGO.activeSelf) edgePointerGO.SetActive(false);
            if (dialogueBoxGO != null && dialogueBoxGO.activeSelf) dialogueBoxGO.SetActive(false);
            return;
        }
        else
        {
            // Restore dialogue box if it was active and has not been dismissed
            if (isDialogueActive && dialogueBoxGO != null && !dialogueBoxGO.activeSelf)
            {
                dialogueBoxGO.SetActive(true);
            }
        }

        // Press F1 to toggle/re-open dialogue box anytime
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (isDialogueActive)
            {
                DismissDialogue();
            }
            else
            {
                ReopenDialogue();
            }
        }

        // Dialogue interaction (Space, Return, or Click to advance typewriter or close)
        if (isDialogueActive)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return))
            {
                if (isTyping)
                {
                    // Fast-forward text
                    if (typewriterRoutine != null) StopCoroutine(typewriterRoutine);
                    isTyping = false;
                    if (dialogueBodyText != null)
                    {
                        dialogueBodyText.text = currentInfo.fullDialogue;
                        dialogueBodyText.maxVisibleCharacters = 99999;
                    }
                    if (continuePromptText != null) continuePromptText.gameObject.SetActive(true);
                }
                else
                {
                    DismissDialogue();
                }
            }
        }

        // Update Navigators (Off-screen pointer & In-screen target beacon)
        UpdateNavigators();
    }

    /// <summary>
    /// Displays a complete tutorial step with HUD tracker, navigational pointers, and Sprout dialogue.
    /// </summary>
    public void ShowStep(TutorialStepInfo info)
    {
        EnsureUIConstructed();

        currentInfo = info;
        hasActiveStep = true;

        // 1. Update HUD Quest Tracker
        if (trackerRootGO != null)
        {
            trackerRootGO.SetActive(true);
            if (trackerHeaderText != null)
            {
                trackerHeaderText.text = $"<color=#FFE066>HƯỚNG DẪN TÂN THỦ [{info.stepIndex}/{info.totalSteps}]</color>  <size=11.5><color=#FFE066><b>[F1: Lời dặn]</b></color></size>";
            }
            if (trackerTitleText != null)
            {
                trackerTitleText.text = info.objectiveTitle;
            }
            if (trackerInstructionText != null)
            {
                trackerInstructionText.text = info.actionInstruction;
            }
            if (trackerStatusText != null)
            {
                if (!string.IsNullOrEmpty(info.targetLabel))
                {
                    trackerStatusText.text = $"<color=#50FA7B>Mục tiêu: {info.targetLabel}</color>";
                }
                else
                {
                    trackerStatusText.text = "";
                }
            }
        }

        // 2. Setup World Marker prompt text
        if (worldMarkerKeyText != null)
        {
            worldMarkerKeyText.text = !string.IsNullOrEmpty(info.actionKeyPrompt) ? info.actionKeyPrompt : "[E] TƯƠNG TÁC";
        }

        // 3. Open Sprout Spirit Dialogue Box
        OpenDialogue(info);
    }

    /// <summary>
    /// Legacy compatibility for ShowDialogue callers.
    /// </summary>
    public void ShowDialogue(string objectiveTitle, string dialogueText, Vector3? targetPosition = null, Transform targetObj = null)
    {
        TutorialStepInfo info = new TutorialStepInfo
        {
            stepIndex = 1,
            totalSteps = 7,
            objectiveTitle = objectiveTitle,
            targetLabel = objectiveTitle,
            actionKeyPrompt = "[E] TƯƠNG TÁC",
            actionInstruction = dialogueText,
            fullDialogue = dialogueText,
            targetPosition = targetPosition,
            targetTransform = targetObj
        };
        ShowStep(info);
    }

    /// <summary>
    /// Dynamically updates the progress meter in the HUD Tracker (e.g. distance walked).
    /// </summary>
    public void UpdateDynamicProgress(string statusText)
    {
        if (trackerStatusText != null)
        {
            trackerStatusText.text = statusText;
        }
    }

    /// <summary>
    /// Displays completion fanfare before advancing.
    /// </summary>
    public void ShowCompletionFeedback(System.Action onComplete)
    {
        StartCoroutine(CompletionRoutine(onComplete));
    }

    private IEnumerator CompletionRoutine(System.Action onComplete)
    {
        if (trackerStatusText != null)
        {
            trackerStatusText.text = "<color=#50FA7B><b>HOÀN THÀNH MỤC TIÊU!</b></color>";
        }

        yield return new WaitForSeconds(0.45f);
        onComplete?.Invoke();
    }

    public void HideDialogue()
    {
        hasActiveStep = false;
        isDialogueActive = false;
        if (dialogueBoxGO != null) dialogueBoxGO.SetActive(false);
        if (trackerRootGO != null) trackerRootGO.SetActive(false);
        if (edgePointerGO != null) edgePointerGO.SetActive(false);
        if (worldMarkerGO != null) worldMarkerGO.SetActive(false);
    }

    private void OpenDialogue(TutorialStepInfo info)
    {
        if (dialogueBoxGO == null) return;

        isDialogueActive = true;
        dialogueBoxGO.SetActive(true);

        if (objectiveBadgeText != null)
        {
            objectiveBadgeText.text = $"[ NHIỆM VỤ {info.stepIndex}/{info.totalSteps}: {info.objectiveTitle.ToUpper()} ]";
        }

        if (continuePromptText != null) continuePromptText.gameObject.SetActive(false);

        if (typewriterRoutine != null) StopCoroutine(typewriterRoutine);
        typewriterRoutine = StartCoroutine(TypewriterRoutine(info.fullDialogue));
    }

    private void DismissDialogue()
    {
        isDialogueActive = false;
        if (dialogueBoxGO != null) dialogueBoxGO.SetActive(false);
        OnDialogueDismissed?.Invoke();
    }

    private void ReopenDialogue()
    {
        if (hasActiveStep)
        {
            OpenDialogue(currentInfo);
        }
    }

    private IEnumerator TypewriterRoutine(string fullText)
    {
        isTyping = true;
        if (dialogueBodyText != null)
        {
            dialogueBodyText.text = fullText;
            dialogueBodyText.maxVisibleCharacters = 0;
        }

        yield return null;

        int totalChars = (dialogueBodyText != null && dialogueBodyText.textInfo != null)
            ? dialogueBodyText.textInfo.characterCount
            : fullText.Length;

        float charDelay = 0.015f;
        for (int i = 1; i <= totalChars; i++)
        {
            if (dialogueBodyText != null)
            {
                dialogueBodyText.maxVisibleCharacters = i;
            }
            yield return new WaitForSeconds(charDelay);
        }

        isTyping = false;
        if (dialogueBodyText != null) dialogueBodyText.maxVisibleCharacters = 99999;
        if (continuePromptText != null) continuePromptText.gameObject.SetActive(true);
    }

    private Vector3 GetCurrentTargetWorldPosition()
    {
        // 1. Prioritize explicit target position if provided (handles tilemaps, statue centers, etc.)
        if (currentInfo.targetPosition.HasValue)
        {
            return currentInfo.targetPosition.Value;
        }

        // 2. Dynamically resolve from targetTransform
        if (currentInfo.targetTransform != null)
        {
            Statue statue = currentInfo.targetTransform.GetComponent<Statue>();
            if (statue != null) return statue.GetStatueCenter();

            Collider2D col = currentInfo.targetTransform.GetComponent<Collider2D>();
            if (col != null) return col.bounds.center;

            return currentInfo.targetTransform.position;
        }

        return Vector3.zero;
    }

    private void UpdateNavigators()
    {
        bool hasTarget = currentInfo.targetTransform != null || currentInfo.targetPosition.HasValue;
        if (!hasTarget)
        {
            if (edgePointerGO != null && edgePointerGO.activeSelf) edgePointerGO.SetActive(false);
            if (worldMarkerGO != null && worldMarkerGO.activeSelf) worldMarkerGO.SetActive(false);
            return;
        }

        Vector3 targetWorld = GetCurrentTargetWorldPosition();

        if (cachedCam == null)
        {
            cachedCam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        }
        if (cachedCam == null) return;

        Vector3 playerPos = PlayerMovement.Instance != null ? PlayerMovement.Instance.transform.position : cachedCam.transform.position;
        float distance = Vector3.Distance(playerPos, targetWorld);

        bool shouldUpdateText = Time.time >= nextTextUpdateTime;
        if (shouldUpdateText)
        {
            nextTextUpdateTime = Time.time + 0.1f;
            // Update live distance in HUD tracker
            if (trackerStatusText != null && !string.IsNullOrEmpty(currentInfo.targetLabel))
            {
                trackerStatusText.text = $"<color=#55FF88>{currentInfo.targetLabel}</color>  |  Cách: <color=#FFD700><b>{distance:F1}m</b></color>";
            }
        }

        // Viewport check (0 to 1)
        Vector3 vp = cachedCam.WorldToViewportPoint(targetWorld);
        bool isOnScreen = (vp.z > 0f) && (vp.x >= 0.08f && vp.x <= 0.92f) && (vp.y >= 0.10f && vp.y <= 0.90f);

        RectTransform canvasRT = masterCanvas != null ? masterCanvas.transform as RectTransform : null;
        if (canvasRT == null) return;

        if (isOnScreen)
        {
            // Target is visible on screen: Show In-Screen Floating Beacon
            if (edgePointerGO != null && edgePointerGO.activeSelf) edgePointerGO.SetActive(false);

            if (worldMarkerGO != null)
            {
                if (!worldMarkerGO.activeSelf) worldMarkerGO.SetActive(true);

                // Convert world pos (elevated above target) to canvas local coordinates
                Vector3 markerWorldPos = targetWorld + Vector3.up * 0.75f;
                Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(cachedCam, markerWorldPos);

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenPoint, null, out Vector2 localPoint))
                {
                    // Gentle vertical bobbing
                    float bob = Mathf.Sin(Time.time * 6f) * 6f;
                    worldMarkerRT.anchoredPosition = localPoint + new Vector2(0f, bob);
                }

                if (shouldUpdateText && worldMarkerDistText != null)
                {
                    worldMarkerDistText.text = $"{distance:F1}m";
                }
            }
        }
        else
        {
            // Target is off-screen: Show Off-Screen Edge Radar Pointer
            if (worldMarkerGO != null && worldMarkerGO.activeSelf) worldMarkerGO.SetActive(false);

            if (edgePointerGO != null)
            {
                if (!edgePointerGO.activeSelf) edgePointerGO.SetActive(true);

                // Compute exact 2D direction from camera position to target in world coordinates
                Vector2 dir = new Vector2(targetWorld.x - cachedCam.transform.position.x, targetWorld.y - cachedCam.transform.position.y);
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
                dir.Normalize();

                // Compute arrow rotation angle (pointing directly towards target)
                float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                // Canvas reference bounds & safe margins so badge never clips off-screen
                float marginX = (edgePointerRT.sizeDelta.x * 0.5f) + 24f;
                float marginY = (edgePointerRT.sizeDelta.y * 0.5f) + 24f;

                float maxOffsetX = Mathf.Max(10f, (canvasRT.rect.width * 0.5f) - marginX);
                float maxOffsetY = Mathf.Max(10f, (canvasRT.rect.height * 0.5f) - marginY);

                // Ray-box intersection to clamp to screen border
                float scaleX = Mathf.Abs(maxOffsetX / Mathf.Max(0.0001f, Mathf.Abs(dir.x)));
                float scaleY = Mathf.Abs(maxOffsetY / Mathf.Max(0.0001f, Mathf.Abs(dir.y)));
                float scale = Mathf.Min(scaleX, scaleY);

                // Gentle radar pulse along direction vector
                float pulse = Mathf.Sin(Time.time * 5f) * 4f;
                Vector2 edgePos = dir * (scale + pulse);
                edgePointerRT.anchoredPosition = edgePos;

                // Rotate arrow icon towards target direction
                if (edgeArrowIconRT != null)
                {
                    edgeArrowIconRT.localEulerAngles = new Vector3(0f, 0f, angleDeg);
                }

                if (shouldUpdateText && edgeDistanceText != null)
                {
                    edgeDistanceText.text = $"{currentInfo.targetLabel} ({distance:F1}m)";
                }
            }
        }
    }

    #region UI Construction
    private void EnsureUIConstructed()
    {
        if (masterCanvasGO != null) return;

        masterCanvasGO = new GameObject("[TutorialCanvas]");
        DontDestroyOnLoad(masterCanvasGO);

        masterCanvas = masterCanvasGO.AddComponent<Canvas>();
        masterCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        masterCanvas.sortingOrder = 18; // Above map/sprites (0-3) and health bar (15), below Inventory (25), Chest (21), Kitchen (20)

        CanvasScaler cs = masterCanvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        masterCanvasGO.AddComponent<GraphicRaycaster>();

        TMP_FontAsset font = DungeonUIAssetHelper.GetFontAsset();

        ConstructQuestTracker(font);
        ConstructEdgePointer(font);
        ConstructWorldMarker(font);
        ConstructDialogueBox(font);
    }

    private void ConstructQuestTracker(TMP_FontAsset font)
    {
        // Tracker Panel (Top-Left)
        trackerRootGO = new GameObject("QuestTrackerPanel", typeof(RectTransform), typeof(Image));
        trackerRootGO.transform.SetParent(masterCanvasGO.transform, false);
        RectTransform rt = trackerRootGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(25, -25);
        rt.sizeDelta = new Vector2(430, 165);

        Image frameImg = trackerRootGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(frameImg, new Color(0.18f, 0.45f, 0.35f, 0.95f)); // Emerald frame

        // Inner dark plate
        GameObject plate = new GameObject("DarkPlate", typeof(RectTransform), typeof(Image));
        plate.transform.SetParent(trackerRootGO.transform, false);
        RectTransform plateRT = plate.GetComponent<RectTransform>();
        plateRT.anchorMin = Vector2.zero;
        plateRT.anchorMax = Vector2.one;
        plateRT.offsetMin = new Vector2(4, 4);
        plateRT.offsetMax = new Vector2(-4, -4);
        Image plateImg = plate.GetComponent<Image>();
        plateImg.color = new Color(0.08f, 0.10f, 0.12f, 0.94f);

        // Header text
        GameObject headerGO = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerGO.transform.SetParent(plate.transform, false);
        RectTransform hRT = headerGO.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0, 1);
        hRT.anchorMax = new Vector2(1, 1);
        hRT.pivot = new Vector2(0, 1);
        hRT.anchoredPosition = new Vector2(12, -10);
        hRT.sizeDelta = new Vector2(-24, 22);
        trackerHeaderText = headerGO.GetComponent<TextMeshProUGUI>();
        trackerHeaderText.fontSize = 13.5f;
        trackerHeaderText.fontStyle = FontStyles.Bold;
        trackerHeaderText.color = new Color(1f, 0.92f, 0.30f); // Sunlight gold
        DungeonUIAssetHelper.ApplyFont(trackerHeaderText, withOutline: true);

        // Title text
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(plate.transform, false);
        RectTransform tRT = titleGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0, 1);
        tRT.anchorMax = new Vector2(1, 1);
        tRT.pivot = new Vector2(0, 1);
        tRT.anchoredPosition = new Vector2(12, -34);
        tRT.sizeDelta = new Vector2(-24, 24);
        trackerTitleText = titleGO.GetComponent<TextMeshProUGUI>();
        trackerTitleText.fontSize = 15.5f;
        trackerTitleText.fontStyle = FontStyles.Bold;
        trackerTitleText.color = Color.white;
        DungeonUIAssetHelper.ApplyFont(trackerTitleText, withOutline: true);

        // Instruction text
        GameObject instGO = new GameObject("Instruction", typeof(RectTransform), typeof(TextMeshProUGUI));
        instGO.transform.SetParent(plate.transform, false);
        RectTransform iRT = instGO.GetComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0, 1);
        iRT.anchorMax = new Vector2(1, 1);
        iRT.pivot = new Vector2(0, 1);
        iRT.anchoredPosition = new Vector2(12, -62);
        iRT.sizeDelta = new Vector2(-24, 52);
        trackerInstructionText = instGO.GetComponent<TextMeshProUGUI>();
        trackerInstructionText.fontSize = 13.5f;
        trackerInstructionText.color = Color.white;
        trackerInstructionText.lineSpacing = 0;
        DungeonUIAssetHelper.ApplyFont(trackerInstructionText, withOutline: true);

        // Status / Distance meter text
        GameObject statusGO = new GameObject("StatusMeter", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGO.transform.SetParent(plate.transform, false);
        RectTransform sRT = statusGO.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0, 0);
        sRT.anchorMax = new Vector2(1, 0);
        sRT.pivot = new Vector2(0, 0);
        sRT.anchoredPosition = new Vector2(12, 10);
        sRT.sizeDelta = new Vector2(-24, 20);
        trackerStatusText = statusGO.GetComponent<TextMeshProUGUI>();
        trackerStatusText.fontSize = 13.5f;
        trackerStatusText.fontStyle = FontStyles.Bold;
        trackerStatusText.color = new Color(0.35f, 1f, 0.65f); // Bright neon emerald
        DungeonUIAssetHelper.ApplyFont(trackerStatusText, withOutline: true);

        trackerRootGO.SetActive(false);
    }

    private void ConstructEdgePointer(TMP_FontAsset font)
    {
        edgePointerGO = new GameObject("EdgeRadarPointer", typeof(RectTransform), typeof(Image));
        edgePointerGO.transform.SetParent(masterCanvasGO.transform, false);
        edgePointerRT = edgePointerGO.GetComponent<RectTransform>();
        edgePointerRT.anchorMin = new Vector2(0.5f, 0.5f);
        edgePointerRT.anchorMax = new Vector2(0.5f, 0.5f);
        edgePointerRT.pivot = new Vector2(0.5f, 0.5f);
        edgePointerRT.sizeDelta = new Vector2(240, 42);

        Image img = edgePointerGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(img, new Color(0.85f, 0.55f, 0.15f, 0.95f)); // Amber gold

        // Dark background plate
        GameObject plate = new GameObject("Plate", typeof(RectTransform), typeof(Image));
        plate.transform.SetParent(edgePointerGO.transform, false);
        RectTransform pRT = plate.GetComponent<RectTransform>();
        pRT.anchorMin = Vector2.zero;
        pRT.anchorMax = Vector2.one;
        pRT.offsetMin = new Vector2(3, 3);
        pRT.offsetMax = new Vector2(-3, -3);
        plate.GetComponent<Image>().color = new Color(0.12f, 0.10f, 0.08f, 0.92f);

        // Arrow icon (rotates)
        GameObject arrowGO = new GameObject("ArrowIcon", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGO.transform.SetParent(plate.transform, false);
        edgeArrowIconRT = arrowGO.GetComponent<RectTransform>();
        edgeArrowIconRT.anchorMin = new Vector2(0, 0.5f);
        edgeArrowIconRT.anchorMax = new Vector2(0, 0.5f);
        edgeArrowIconRT.pivot = new Vector2(0.5f, 0.5f);
        edgeArrowIconRT.anchoredPosition = new Vector2(18, 0);
        edgeArrowIconRT.sizeDelta = new Vector2(26, 26);
        TextMeshProUGUI aTMP = arrowGO.GetComponent<TextMeshProUGUI>();
        aTMP.text = ">";
        aTMP.alignment = TextAlignmentOptions.Center;
        aTMP.fontSize = 20;
        aTMP.fontStyle = FontStyles.Bold;
        aTMP.color = new Color(1f, 0.85f, 0.2f);
        if (font != null) aTMP.font = font;

        // Label with name & distance
        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(plate.transform, false);
        RectTransform lRT = labelGO.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0, 0);
        lRT.anchorMax = new Vector2(1, 1);
        lRT.offsetMin = new Vector2(34, 0);
        lRT.offsetMax = new Vector2(-8, 0);
        edgeDistanceText = labelGO.GetComponent<TextMeshProUGUI>();
        edgeDistanceText.alignment = TextAlignmentOptions.Left;
        edgeDistanceText.fontSize = 12;
        edgeDistanceText.fontStyle = FontStyles.Bold;
        edgeDistanceText.color = Color.white;
        if (font != null) edgeDistanceText.font = font;

        edgePointerGO.SetActive(false);
    }

    private void ConstructWorldMarker(TMP_FontAsset font)
    {
        worldMarkerGO = new GameObject("InScreenWorldMarker", typeof(RectTransform));
        worldMarkerGO.transform.SetParent(masterCanvasGO.transform, false);
        worldMarkerRT = worldMarkerGO.GetComponent<RectTransform>();
        worldMarkerRT.anchorMin = new Vector2(0.5f, 0.5f);
        worldMarkerRT.anchorMax = new Vector2(0.5f, 0.5f);
        worldMarkerRT.pivot = new Vector2(0.5f, 0f); // Pivot at the bottom tip of ▼ arrow
        worldMarkerRT.sizeDelta = new Vector2(190, 70);

        // Bouncing arrow
        GameObject arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGO.transform.SetParent(worldMarkerGO.transform, false);
        worldArrowRT = arrowGO.GetComponent<RectTransform>();
        worldArrowRT.anchorMin = new Vector2(0.5f, 0f);
        worldArrowRT.anchorMax = new Vector2(0.5f, 0f);
        worldArrowRT.pivot = new Vector2(0.5f, 0f);
        worldArrowRT.anchoredPosition = new Vector2(0, 0);
        worldArrowRT.sizeDelta = new Vector2(30, 26);
        TextMeshProUGUI arrowTMP = arrowGO.GetComponent<TextMeshProUGUI>();
        arrowTMP.text = "v";
        arrowTMP.fontSize = 22;
        arrowTMP.fontStyle = FontStyles.Bold;
        arrowTMP.alignment = TextAlignmentOptions.Center;
        arrowTMP.color = new Color(1f, 0.85f, 0.2f);
        if (font != null) arrowTMP.font = font;

        // Action badge plaque
        GameObject plaqueGO = new GameObject("ActionBadge", typeof(RectTransform), typeof(Image));
        plaqueGO.transform.SetParent(worldMarkerGO.transform, false);
        RectTransform pRT = plaqueGO.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.5f, 0f);
        pRT.anchorMax = new Vector2(0.5f, 0f);
        pRT.pivot = new Vector2(0.5f, 0f);
        pRT.anchoredPosition = new Vector2(0, 26);
        pRT.sizeDelta = new Vector2(170, 36);
        Image pImg = plaqueGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(pImg, new Color(0.15f, 0.65f, 0.45f, 0.95f)); // Emerald jade

        GameObject plate = new GameObject("DarkPlate", typeof(RectTransform), typeof(Image));
        plate.transform.SetParent(plaqueGO.transform, false);
        RectTransform plRT = plate.GetComponent<RectTransform>();
        plRT.anchorMin = Vector2.zero;
        plRT.anchorMax = Vector2.one;
        plRT.offsetMin = new Vector2(2, 2);
        plRT.offsetMax = new Vector2(-2, -2);
        plate.GetComponent<Image>().color = new Color(0.10f, 0.12f, 0.14f, 0.95f);

        // Action prompt text (e.g. [E] THU HOẠCH)
        GameObject txtGO = new GameObject("ActionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(plate.transform, false);
        RectTransform tRT = txtGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(4, 0);
        tRT.offsetMax = new Vector2(-4, 0);
        worldMarkerKeyText = txtGO.GetComponent<TextMeshProUGUI>();
        worldMarkerKeyText.fontSize = 13;
        worldMarkerKeyText.fontStyle = FontStyles.Bold;
        worldMarkerKeyText.alignment = TextAlignmentOptions.Center;
        worldMarkerKeyText.color = new Color(1f, 0.95f, 0.35f);
        DungeonUIAssetHelper.ApplyFont(worldMarkerKeyText, withOutline: true);

        // Distance text
        GameObject distGO = new GameObject("DistText", typeof(RectTransform), typeof(TextMeshProUGUI));
        distGO.transform.SetParent(worldMarkerGO.transform, false);
        RectTransform dRT = distGO.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0.5f, 0f);
        dRT.anchorMax = new Vector2(0.5f, 0f);
        dRT.pivot = new Vector2(0.5f, 0f);
        dRT.anchoredPosition = new Vector2(0, 64);
        dRT.sizeDelta = new Vector2(80, 16);
        worldMarkerDistText = distGO.GetComponent<TextMeshProUGUI>();
        worldMarkerDistText.fontSize = 12;
        worldMarkerDistText.fontStyle = FontStyles.Bold;
        worldMarkerDistText.alignment = TextAlignmentOptions.Center;
        worldMarkerDistText.color = new Color(0.35f, 1f, 0.65f);
        DungeonUIAssetHelper.ApplyFont(worldMarkerDistText, withOutline: true);

        worldMarkerGO.SetActive(false);
    }

    private void ConstructDialogueBox(TMP_FontAsset font)
    {
        // Dialogue Box Frame (Bottom Center, positioned safely above the Toolbar)
        // Toolbar (currentItemPanel) is at anchoredPosition.y = 85.7 with height 66.5 (top at ~86px).
        // Placing dialogue box at y = 105 ensures the entire toolbar inventory is 100% visible and clickable!
        dialogueBoxGO = new GameObject("DialogueBox", typeof(RectTransform), typeof(Image));
        dialogueBoxGO.transform.SetParent(masterCanvasGO.transform, false);
        dialogueBoxRT = dialogueBoxGO.GetComponent<RectTransform>();
        dialogueBoxRT.anchorMin = new Vector2(0.5f, 0f);
        dialogueBoxRT.anchorMax = new Vector2(0.5f, 0f);
        dialogueBoxRT.pivot = new Vector2(0.5f, 0f);
        dialogueBoxRT.anchoredPosition = new Vector2(0, 105);
        dialogueBoxRT.sizeDelta = new Vector2(820, 175);

        Image boxImg = dialogueBoxGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(boxImg, new Color(0.15f, 0.55f, 0.40f, 1f)); // Emerald jade frame

        // Dark background plate
        GameObject bgGO = new GameObject("InnerBg", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(dialogueBoxGO.transform, false);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = new Vector2(6, 6);
        bgRT.offsetMax = new Vector2(-6, -6);
        Image bgImg = bgGO.GetComponent<Image>();
        bgImg.color = new Color(0.08f, 0.10f, 0.12f, 0.96f);

        // Avatar Frame (Left)
        GameObject avatarFrameGO = new GameObject("AvatarFrame", typeof(RectTransform), typeof(Image));
        avatarFrameGO.transform.SetParent(bgGO.transform, false);
        RectTransform avFrameRT = avatarFrameGO.GetComponent<RectTransform>();
        avFrameRT.anchorMin = new Vector2(0, 0.5f);
        avFrameRT.anchorMax = new Vector2(0, 0.5f);
        avFrameRT.pivot = new Vector2(0, 0.5f);
        avFrameRT.anchoredPosition = new Vector2(14, 0);
        avFrameRT.sizeDelta = new Vector2(96, 96);
        Image avFrameImg = avatarFrameGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(avFrameImg, new Color(0.20f, 0.65f, 0.45f, 1f));

        GameObject avatarGO = new GameObject("AvatarImage", typeof(RectTransform), typeof(Image));
        avatarGO.transform.SetParent(avatarFrameGO.transform, false);
        RectTransform avRT = avatarGO.GetComponent<RectTransform>();
        avRT.anchorMin = Vector2.zero;
        avRT.anchorMax = Vector2.one;
        avRT.offsetMin = new Vector2(4, 4);
        avRT.offsetMax = new Vector2(-4, -4);
        avatarImg = avatarGO.GetComponent<Image>();
        avatarImg.preserveAspect = true;

        Sprite tentSprite = DungeonUIAssetHelper.GetTentSprite();
        if (tentSprite != null) avatarImg.sprite = tentSprite;
        avatarImg.color = new Color(0.55f, 1.0f, 0.70f, 1f);

        // Content Column (Right of Avatar)
        GameObject colGO = new GameObject("ContentCol", typeof(RectTransform));
        colGO.transform.SetParent(bgGO.transform, false);
        RectTransform colRT = colGO.GetComponent<RectTransform>();
        colRT.anchorMin = new Vector2(0, 0);
        colRT.anchorMax = new Vector2(1, 1);
        colRT.offsetMin = new Vector2(125, 10);
        colRT.offsetMax = new Vector2(-16, -10);

        // Header Row (Speaker Name + Objective Badge)
        GameObject headerGO = new GameObject("HeaderRow", typeof(RectTransform));
        headerGO.transform.SetParent(colGO.transform, false);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0, 1);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0, 24);

        // Speaker Name
        GameObject nameGO = new GameObject("SpeakerName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGO.transform.SetParent(headerGO.transform, false);
        RectTransform nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0);
        nameRT.anchorMax = new Vector2(0.32f, 1);
        nameRT.offsetMin = Vector2.zero;
        nameRT.offsetMax = Vector2.zero;
        speakerNameText = nameGO.GetComponent<TextMeshProUGUI>();
        speakerNameText.text = "Tinh Linh Mầm Cây";
        speakerNameText.fontSize = 15.5f;
        speakerNameText.fontStyle = FontStyles.Bold;
        speakerNameText.textWrappingMode = TextWrappingModes.NoWrap;
        speakerNameText.color = new Color(0.35f, 1f, 0.65f, 1f); // Bright neon jade
        DungeonUIAssetHelper.ApplyFont(speakerNameText, withOutline: true);

        // Objective Badge
        GameObject badgeGO = new GameObject("ObjectiveBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeGO.transform.SetParent(headerGO.transform, false);
        RectTransform badgeRT = badgeGO.GetComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0.32f, 0);
        badgeRT.anchorMax = new Vector2(1, 1);
        badgeRT.offsetMin = Vector2.zero;
        badgeRT.offsetMax = Vector2.zero;
        objectiveBadgeText = badgeGO.GetComponent<TextMeshProUGUI>();
        objectiveBadgeText.alignment = TextAlignmentOptions.Right;
        objectiveBadgeText.fontSize = 12.5f;
        objectiveBadgeText.fontStyle = FontStyles.Bold;
        objectiveBadgeText.textWrappingMode = TextWrappingModes.NoWrap;
        objectiveBadgeText.color = new Color(1f, 0.92f, 0.30f, 1f); // Bright sunlight gold
        DungeonUIAssetHelper.ApplyFont(objectiveBadgeText, withOutline: true);

        // Dialogue Body Text
        GameObject bodyGO = new GameObject("BodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyGO.transform.SetParent(colGO.transform, false);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0, 0);
        bodyRT.anchorMax = new Vector2(1, 1);
        bodyRT.offsetMin = new Vector2(0, 22);
        bodyRT.offsetMax = new Vector2(0, -28);
        dialogueBodyText = bodyGO.GetComponent<TextMeshProUGUI>();
        dialogueBodyText.fontSize = 14f;
        dialogueBodyText.color = Color.white;
        dialogueBodyText.lineSpacing = 2.0f;
        DungeonUIAssetHelper.ApplyFont(dialogueBodyText, withOutline: true);

        // Continue Prompt Text
        GameObject promptGO = new GameObject("ContinuePrompt", typeof(RectTransform), typeof(TextMeshProUGUI));
        promptGO.transform.SetParent(colGO.transform, false);
        RectTransform promptRT = promptGO.GetComponent<RectTransform>();
        promptRT.anchorMin = new Vector2(0, 0);
        promptRT.anchorMax = new Vector2(1, 0);
        promptRT.pivot = new Vector2(1, 0);
        promptRT.sizeDelta = new Vector2(0, 20);
        promptRT.anchoredPosition = Vector2.zero;
        continuePromptText = promptGO.GetComponent<TextMeshProUGUI>();
        continuePromptText.alignment = TextAlignmentOptions.Right;
        continuePromptText.fontSize = 12.5f;
        continuePromptText.fontStyle = FontStyles.Bold;
        continuePromptText.color = new Color(1f, 0.90f, 0.35f, 1f); // Bright Gold so players immediately see how to proceed!
        continuePromptText.text = "[ Nhấn Space hoặc Click để tiếp tục ]";
        DungeonUIAssetHelper.ApplyFont(continuePromptText, withOutline: true);

        dialogueBoxGO.SetActive(false);
    }
    #endregion
}
