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
    [SerializeField] public GameObject masterCanvasGO;
    [SerializeField] public Canvas masterCanvas;

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
    public bool IsDialogueActive => isDialogueActive;
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
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);
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
    private static readonly WaitForSeconds _typewriterDelay = new WaitForSeconds(0.015f);

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

        yield return new WaitForSecondsRealtime(0.25f);
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

        for (int i = 1; i <= totalChars; i++)
        {
            if (dialogueBodyText != null)
            {
                dialogueBodyText.maxVisibleCharacters = i;
            }
            yield return _typewriterDelay;
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
    public void EnsureUIConstructed()
    {
        if (masterCanvasGO != null)
        {
            BindSceneReferences();
            return;
        }

        GameObject existingCanvas = GameObject.Find("[TutorialCanvas]");
        if (existingCanvas == null)
        {
            Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < allCanvases.Length; i++)
            {
                if (allCanvases[i] != null && allCanvases[i].name == "[TutorialCanvas]")
                {
                    existingCanvas = allCanvases[i].gameObject;
                    break;
                }
            }
        }

        if (existingCanvas != null)
        {
            masterCanvasGO = existingCanvas;
            masterCanvas = existingCanvas.GetComponent<Canvas>();
            BindSceneReferences();
            return;
        }

        Debug.LogWarning("[TutorialDialogueUI] [TutorialCanvas] not found in scene!");
    }

    public void BindSceneReferences()
    {
        if (masterCanvasGO == null) return;
        Transform root = masterCanvasGO.transform;

        // 1. Tracker
        Transform trackerT = root.Find("QuestTrackerPanel");
        if (trackerT != null)
        {
            trackerRootGO = trackerT.gameObject;
            trackerHeaderText = trackerT.Find("Header")?.GetComponent<TextMeshProUGUI>();
            trackerTitleText = trackerT.Find("Title")?.GetComponent<TextMeshProUGUI>();
            trackerInstructionText = trackerT.Find("Instruction")?.GetComponent<TextMeshProUGUI>();
            trackerStatusText = trackerT.Find("StatusMeter")?.GetComponent<TextMeshProUGUI>();
        }

        // 2. Edge Pointer
        Transform edgeT = root.Find("EdgeRadarPointer");
        if (edgeT != null)
        {
            edgePointerGO = edgeT.gameObject;
            edgePointerRT = edgeT.GetComponent<RectTransform>();
            edgeArrowIconRT = edgeT.Find("ArrowIcon")?.GetComponent<RectTransform>();
            edgeDistanceText = edgeT.Find("DistanceText")?.GetComponent<TextMeshProUGUI>();
        }

        // 3. World Marker
        Transform markerT = root.Find("InScreenWorldMarker");
        if (markerT != null)
        {
            worldMarkerGO = markerT.gameObject;
            worldMarkerRT = markerT.GetComponent<RectTransform>();
            worldArrowRT = markerT.Find("Arrow")?.GetComponent<RectTransform>();
            worldMarkerKeyText = markerT.Find("ActionBadge/ActionText")?.GetComponent<TextMeshProUGUI>();
            worldMarkerDistText = markerT.Find("DistText")?.GetComponent<TextMeshProUGUI>();
        }

        // 4. Dialogue Box
        Transform diagT = root.Find("DialogueBox");
        if (diagT != null)
        {
            dialogueBoxGO = diagT.gameObject;
            dialogueBoxRT = diagT.GetComponent<RectTransform>();
            speakerNameText = diagT.Find("InnerBg/ContentCol/HeaderRow/SpeakerPlaque/SpeakerName")?.GetComponent<TextMeshProUGUI>();
            dialogueBodyText = diagT.Find("InnerBg/ContentCol/BodyText")?.GetComponent<TextMeshProUGUI>();
            objectiveBadgeText = diagT.Find("InnerBg/ContentCol/HeaderRow/ObjectiveBadge")?.GetComponent<TextMeshProUGUI>();
            continuePromptText = diagT.Find("InnerBg/ContinuePrompt")?.GetComponent<TextMeshProUGUI>();
            avatarImg = diagT.Find("InnerBg/AvatarFrame/AvatarImage")?.GetComponent<Image>();
        }
    }

    #endregion
}
