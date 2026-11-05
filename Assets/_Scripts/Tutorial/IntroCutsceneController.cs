using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dedicated World Intro Screen Controller.
/// Plays an atmospheric, standalone dark intro screen with storytelling narration lines
/// appearing purely in the center upon starting a new game, before seamlessly fading into gameplay.
/// Supports clicking (or Space / Enter) to advance sentences, or ESC to skip entirely.
/// </summary>
public class IntroCutsceneController : MonoBehaviour
{
    public static IntroCutsceneController Instance { get; private set; }

    [Header("Intro Settings")]
    [SerializeField] private bool playOnStart = true;

    private Camera mainCam;
    private CameraController camController;
    private PlayerMovement playerMovement;

    private GameObject cutsceneCanvasGO;
    private CanvasGroup canvasGroup;
    private Image fullscreenOverlay;
    private TextMeshProUGUI centerNarrationText;
    private TextMeshProUGUI skipPromptText;

    private bool isCutscenePlaying = false;
    public bool IsPlaying => isCutscenePlaying || activeCutsceneRoutine != null;
    private bool advanceRequested = false;
    private Coroutine activeCutsceneRoutine;

    private const string CutscenePrefKey = "HarvestHollow_IntroCutscene_Played";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        FindReferences();
        TutorialManager.Instance?.EnsureTutorialCropsReady();

        // Check if cutscene should play on new game (only in Base scene)
        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isBase = string.IsNullOrEmpty(activeScene) || activeScene == "Base";
        bool isTutorialDone = (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialCompleted)
                           || PlayerPrefs.GetInt(TutorialManager.TutorialDoneKey, 0) == 1
                           || PlayerPrefs.GetInt("Farm_CurrentDay", 1) > 1
                           || PlayerPrefs.GetInt("Dungeon_HighestUnlockedFloor", 1) > 1;

        bool isMenuOpen = MainMenuUI.Instance != null && MainMenuUI.Instance.IsOpen;
        bool shouldPlay = playOnStart && isBase && !isTutorialDone && !isMenuOpen && PlayerPrefs.GetInt(CutscenePrefKey, 0) == 0;

        if (shouldPlay)
        {
            PlayIntroCutscene();
        }
        else
        {
            isCutscenePlaying = false;
            if (isMenuOpen)
            {
                // MainMenu is open: wait for player to close menu or choose New Game/Continue
                return;
            }

            if (isTutorialDone && TutorialManager.Instance != null)
            {
                TutorialManager.Instance.SkipOrCompleteTutorial();
            }
            else if (TutorialManager.Instance != null && TutorialManager.Instance.CurrentStep == TutorialManager.TutorialStep.IntroCutscene)
            {
                TutorialManager.Instance.OnIntroCutsceneFinished();
            }
        }
    }

    private void Update()
    {
        if (!isCutscenePlaying) return;

        // ESC: Skip cutscene entirely
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SkipCutscene();
            return;
        }

        // Left Click, Space, or Enter: Advance to the next sentence immediately
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            advanceRequested = true;
        }
    }

    private void FindReferences()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null && camController == null) camController = mainCam.GetComponent<CameraController>();
        if (playerMovement == null) playerMovement = PlayerMovement.Instance != null ? PlayerMovement.Instance : FindFirstObjectByType<PlayerMovement>();
    }

    /// <summary>
    /// Starts the standalone world intro screen sequence.
    /// </summary>
    public void PlayIntroCutscene()
    {
        if (activeCutsceneRoutine != null) return;

        FindReferences();
        EnsureUIConstructed();

        activeCutsceneRoutine = StartCoroutine(CutsceneSequenceRoutine());
    }

    public void SkipCutscene()
    {
        if (!isCutscenePlaying && activeCutsceneRoutine == null) return;

        if (activeCutsceneRoutine != null)
        {
            StopCoroutine(activeCutsceneRoutine);
            activeCutsceneRoutine = null;
        }

        EndCutscene(immediate: true);
    }

    private IEnumerator CutsceneSequenceRoutine()
    {
        isCutscenePlaying = true;
        advanceRequested = false;
        PlayerPrefs.SetInt(CutscenePrefKey, 1);
        PlayerPrefs.Save();

        // Lock player controls during intro
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // Snap camera over player bed in the house so world is ready behind the black screen
        Vector3 playerPos = playerMovement != null 
            ? playerMovement.transform.position + new Vector3(0, 1.2f, -10f) 
            : new Vector3(12.0f, 0.5f, -10f);
        if (mainCam != null) mainCam.transform.position = playerPos;

        if (camController != null)
        {
            camController.isCinematicOverride = true;
        }

        // Show dedicated Intro screen
        if (cutsceneCanvasGO != null) cutsceneCanvasGO.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        if (skipPromptText != null)
        {
            skipPromptText.text = "[ Nhấp chuột / Space: Tiếp tục   |   ESC: Bỏ qua ]";
            skipPromptText.gameObject.SetActive(true);
        }

        // Sentence 1
        yield return StartCoroutine(PlaySentenceRoutine("Nơi hầm ngục và nông trại đan xen...", 2.8f));

        // Sentence 2
        yield return StartCoroutine(PlaySentenceRoutine("Ở thế giới này... Nông sản chính là sinh mệnh và vũ khí duy nhất.", 3.2f));

        // Sentence 3
        yield return StartCoroutine(PlaySentenceRoutine("Hãy thức tỉnh, người lữ hành...", 2.8f));

        // End Intro smoothly with fade-out into game world
        EndCutscene(immediate: false);
    }

    private IEnumerator PlaySentenceRoutine(string text, float autoHoldDuration)
    {
        advanceRequested = false;

        if (centerNarrationText != null)
        {
            centerNarrationText.text = text;
            centerNarrationText.color = new Color(0.96f, 0.96f, 0.98f, 0f);
        }

        // 1. Smooth Fade-in text (0.35f)
        float elapsed = 0f;
        float fadeTime = 0.35f;
        while (elapsed < fadeTime && !advanceRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(elapsed / fadeTime);
            if (centerNarrationText != null)
            {
                centerNarrationText.color = new Color(0.96f, 0.96f, 0.98f, a);
            }
            yield return null;
        }
        if (centerNarrationText != null) centerNarrationText.color = new Color(0.96f, 0.96f, 0.98f, 1f);

        // 2. Hold on screen until duration expires or player clicks to advance
        float holdElapsed = 0f;
        while (holdElapsed < autoHoldDuration && !advanceRequested)
        {
            holdElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 3. Smooth Fade-out text (0.25f)
        elapsed = 0f;
        float fadeOutTime = 0.25f;
        while (elapsed < fadeOutTime && !advanceRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(elapsed / fadeOutTime);
            if (centerNarrationText != null)
            {
                centerNarrationText.color = new Color(0.96f, 0.96f, 0.98f, a);
            }
            yield return null;
        }

        if (centerNarrationText != null)
        {
            centerNarrationText.color = new Color(0.96f, 0.96f, 0.98f, 0f);
            centerNarrationText.text = "";
        }

        // Brief dramatic pause between sentences (0.15f)
        if (!advanceRequested)
        {
            yield return new WaitForSecondsRealtime(0.15f);
        }

        advanceRequested = false;
    }

    private void EndCutscene(bool immediate = false)
    {
        isCutscenePlaying = false;
        advanceRequested = false;
        activeCutsceneRoutine = null;

        if (centerNarrationText != null) centerNarrationText.text = "";
        if (skipPromptText != null) skipPromptText.gameObject.SetActive(false);

        if (immediate || canvasGroup == null)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
            if (cutsceneCanvasGO != null) cutsceneCanvasGO.SetActive(false);
            FinishCutsceneCleanup();
        }
        else
        {
            // Smooth cinematic fade-out of the dark screen revealing the player waking up at farm
            StartCoroutine(FadeOutScreenRoutine(0.75f, () =>
            {
                if (cutsceneCanvasGO != null) cutsceneCanvasGO.SetActive(false);
                FinishCutsceneCleanup();
            }));
        }
    }

    private IEnumerator FadeOutScreenRoutine(float duration, System.Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            }
            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        onComplete?.Invoke();
    }

    private void FinishCutsceneCleanup()
    {
        // Restore Camera
        if (camController != null)
        {
            camController.isCinematicOverride = false;
        }

        // Re-enable player movement
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        // Notify Tutorial Manager to begin Step 1 (Movement)
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnIntroCutsceneFinished();
        }
    }

    private void EnsureUIConstructed()
    {
        if (cutsceneCanvasGO != null) return;

        cutsceneCanvasGO = new GameObject("[IntroCutsceneCanvas]");
        DontDestroyOnLoad(cutsceneCanvasGO);

        Canvas c = cutsceneCanvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 9998;

        CanvasScaler cs = cutsceneCanvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        canvasGroup = cutsceneCanvasGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        // 1. Dedicated Fullscreen Dark Background
        GameObject bgGO = new GameObject("FullscreenOverlay", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(cutsceneCanvasGO.transform, false);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        fullscreenOverlay = bgGO.GetComponent<Image>();
        fullscreenOverlay.color = new Color(0.03f, 0.035f, 0.05f, 1f); // Deep Atmospheric Obsidian Black
        fullscreenOverlay.raycastTarget = true;

        // 2. Center Narration Text (Exactly in the middle of the screen)
        GameObject centerTextGO = new GameObject("CenterNarrationText", typeof(RectTransform), typeof(TextMeshProUGUI));
        centerTextGO.transform.SetParent(cutsceneCanvasGO.transform, false);
        RectTransform textRT = centerTextGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = new Vector2(1200f, 240f);

        centerNarrationText = centerTextGO.GetComponent<TextMeshProUGUI>();
        centerNarrationText.fontSize = 32;
        centerNarrationText.alignment = TextAlignmentOptions.Center;
        centerNarrationText.fontStyle = FontStyles.Bold;
        centerNarrationText.color = new Color(0.96f, 0.96f, 0.98f, 1f);
        DungeonUIAssetHelper.ApplyFont(centerNarrationText);

        // 3. Skip Prompt Text (At the bottom of the screen)
        GameObject skipGO = new GameObject("SkipPrompt", typeof(RectTransform), typeof(TextMeshProUGUI));
        skipGO.transform.SetParent(cutsceneCanvasGO.transform, false);
        RectTransform skipRT = skipGO.GetComponent<RectTransform>();
        skipRT.anchorMin = new Vector2(0.5f, 0.06f);
        skipRT.anchorMax = new Vector2(0.5f, 0.06f);
        skipRT.pivot = new Vector2(0.5f, 0.5f);
        skipRT.anchoredPosition = Vector2.zero;
        skipRT.sizeDelta = new Vector2(800f, 32f);

        skipPromptText = skipGO.GetComponent<TextMeshProUGUI>();
        skipPromptText.fontSize = 15;
        skipPromptText.color = new Color(0.6f, 0.65f, 0.75f, 0.7f);
        skipPromptText.alignment = TextAlignmentOptions.Center;
        DungeonUIAssetHelper.ApplyFont(skipPromptText);

        cutsceneCanvasGO.SetActive(false);
    }
}
