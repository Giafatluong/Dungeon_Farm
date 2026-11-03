using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cinematic Intro Cutscene Controller.
/// Plays a sweeping cinematic camera glide with letterbox black bars and poetic narration
/// upon starting a new game, then smoothly transitions into the player tutorial.
/// Supports clicking (or Space / Enter) to instantly skip to the next sentence, or ESC to skip entirely.
/// </summary>
public class IntroCutsceneController : MonoBehaviour
{
    public static IntroCutsceneController Instance { get; private set; }

    [Header("Cinematic Settings")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float panSpeed = 7.0f;

    private Camera mainCam;
    private CameraController camController;
    private PlayerMovement playerMovement;

    private GameObject cutsceneCanvasGO;
    private RectTransform topBar;
    private RectTransform bottomBar;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI skipPromptText;

    private bool isCutscenePlaying = false;
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
        if (playOnStart && (string.IsNullOrEmpty(activeScene) || activeScene == "Base") && PlayerPrefs.GetInt(CutscenePrefKey, 0) == 0)
        {
            PlayIntroCutscene();
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
    /// Starts the cinematic intro cutscene sequence.
    /// </summary>
    public void PlayIntroCutscene()
    {
        if (isCutscenePlaying) return;

        FindReferences();
        EnsureUIConstructed();

        if (activeCutsceneRoutine != null) StopCoroutine(activeCutsceneRoutine);
        activeCutsceneRoutine = StartCoroutine(CutsceneSequenceRoutine());
    }

    public void SkipCutscene()
    {
        if (!isCutscenePlaying) return;

        if (activeCutsceneRoutine != null)
        {
            StopCoroutine(activeCutsceneRoutine);
            activeCutsceneRoutine = null;
        }

        EndCutscene();
    }

    private IEnumerator CutsceneSequenceRoutine()
    {
        isCutscenePlaying = true;
        advanceRequested = false;
        PlayerPrefs.SetInt(CutscenePrefKey, 1);
        PlayerPrefs.Save();

        // Lock player controls
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // Take over camera
        if (camController != null)
        {
            camController.isCinematicOverride = true;
        }

        // 1. Slide in Cinematic Letterbox Bars
        if (cutsceneCanvasGO != null) cutsceneCanvasGO.SetActive(true);
        yield return StartCoroutine(AnimateBars(0f, 85f, 0.35f));

        if (skipPromptText != null)
        {
            skipPromptText.text = "[ Click / Space: Câu tiếp  |  ESC: Bỏ qua ]";
            skipPromptText.gameObject.SetActive(true);
        }

        // Camera Waypoints:
        // Waypoint 1: Goddess Statue
        Vector3 statuePos = new Vector3(40.5f, 10.9f, -10f);
        Statue statue = Statue.Instance != null ? Statue.Instance : FindFirstObjectByType<Statue>();
        if (statue != null) statuePos = statue.GetStatueCenter() + new Vector3(0, 0, -10f);

        // Waypoint 2: Base Farm Plots & Ripe Crops
        Vector3 farmCenter = new Vector3(2.5f, 0.5f, -10f);

        // Waypoint 3: Player Bed
        Vector3 playerPos = playerMovement != null ? playerMovement.transform.position + new Vector3(0, 1.2f, -10f) : new Vector3(12.0f, 0.5f, -10f);

        // SHOT 1: Goddess Statue
        if (mainCam != null) mainCam.transform.position = statuePos;
        yield return StartCoroutine(PlayShotRoutine(statuePos, "Nơi hầm ngục và nông trại đan xen...", 2.2f));

        // SHOT 2: Glide to Farm Plots
        yield return StartCoroutine(PlayShotRoutine(farmCenter, "Ở thế giới này... Nông sản chính là sinh mệnh và vũ khí duy nhất.", 2.5f));

        // SHOT 3: Glide to Player Bed
        yield return StartCoroutine(PlayShotRoutine(playerPos, "Hãy thức tỉnh, người lữ hành...", 2.0f));

        // Finish Cutscene smoothly
        EndCutscene();
    }

    private IEnumerator PlayShotRoutine(Vector3 targetCamPos, string text, float autoHoldDuration)
    {
        advanceRequested = false;

        Vector3 startCamPos = mainCam != null ? mainCam.transform.position : targetCamPos;
        float distance = Vector3.Distance(startCamPos, targetCamPos);
        // Snappy smooth glide: capped between 0.3s and 1.1s
        float glideDuration = distance > 0.1f ? Mathf.Clamp(distance / Mathf.Max(1f, panSpeed), 0.35f, 1.1f) : 0f;
        float glideElapsed = 0f;

        // Subtitle text fade-in
        if (subtitleText != null)
        {
            subtitleText.text = text;
            subtitleText.color = new Color(1f, 1f, 1f, 0f);
        }

        float textFadeTime = 0.18f;
        float textElapsed = 0f;
        float holdElapsed = 0f;

        // Run glide and text reveal concurrently, allowing click to advance immediately
        while (!advanceRequested)
        {
            float dt = Time.deltaTime;

            // Camera movement
            if (glideDuration > 0f && glideElapsed < glideDuration)
            {
                glideElapsed += dt;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(glideElapsed / glideDuration));
                if (mainCam != null) mainCam.transform.position = Vector3.Lerp(startCamPos, targetCamPos, t);
            }
            else
            {
                if (mainCam != null) mainCam.transform.position = targetCamPos;
            }

            // Subtitle fade
            if (textElapsed < textFadeTime)
            {
                textElapsed += dt;
                float alpha = Mathf.Clamp01(textElapsed / textFadeTime);
                if (subtitleText != null) subtitleText.color = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                if (subtitleText != null) subtitleText.color = Color.white;
                holdElapsed += dt;
                if (holdElapsed >= autoHoldDuration)
                {
                    break; // Time-based auto advance
                }
            }

            yield return null;
        }

        // Snap camera to target waypoint on advance
        if (mainCam != null) mainCam.transform.position = targetCamPos;
        if (subtitleText != null) subtitleText.color = Color.white;

        advanceRequested = false;
    }

    private void EndCutscene()
    {
        isCutscenePlaying = false;
        advanceRequested = false;

        // Hide subtitles and skip prompt
        if (subtitleText != null) subtitleText.text = "";
        if (skipPromptText != null) skipPromptText.gameObject.SetActive(false);

        // Retract cinematic letterbox bars quickly (0.3s)
        StartCoroutine(AnimateBars(85f, 0f, 0.3f, () =>
        {
            if (cutsceneCanvasGO != null) cutsceneCanvasGO.SetActive(false);
        }));

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

        // Notify Tutorial Manager to kick off Step 1
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnIntroCutsceneFinished();
        }
    }

    private IEnumerator AnimateBars(float fromHeight, float toHeight, float duration, System.Action onComplete = null)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float h = Mathf.Lerp(fromHeight, toHeight, t);

            if (topBar != null) topBar.sizeDelta = new Vector2(0, h);
            if (bottomBar != null) bottomBar.sizeDelta = new Vector2(0, h);

            yield return null;
        }

        if (topBar != null) topBar.sizeDelta = new Vector2(0, toHeight);
        if (bottomBar != null) bottomBar.sizeDelta = new Vector2(0, toHeight);

        onComplete?.Invoke();
    }

    private void EnsureUIConstructed()
    {
        if (cutsceneCanvasGO != null) return;

        cutsceneCanvasGO = new GameObject("[IntroCutsceneCanvas]");
        DontDestroyOnLoad(cutsceneCanvasGO);

        Canvas c = cutsceneCanvasGO.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 9998; // Just below topmost tooltips

        CanvasScaler cs = cutsceneCanvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        CanvasGroup cg = cutsceneCanvasGO.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        // 1. Top Cinematic Bar
        GameObject topBarGO = new GameObject("TopLetterbox", typeof(RectTransform), typeof(Image));
        topBarGO.transform.SetParent(cutsceneCanvasGO.transform, false);
        topBar = topBarGO.GetComponent<RectTransform>();
        topBar.anchorMin = new Vector2(0, 1);
        topBar.anchorMax = new Vector2(1, 1);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.sizeDelta = new Vector2(0, 0);
        topBarGO.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 1f);

        // 2. Bottom Cinematic Bar
        GameObject bottomBarGO = new GameObject("BottomLetterbox", typeof(RectTransform), typeof(Image));
        bottomBarGO.transform.SetParent(cutsceneCanvasGO.transform, false);
        bottomBar = bottomBarGO.GetComponent<RectTransform>();
        bottomBar.anchorMin = new Vector2(0, 0);
        bottomBar.anchorMax = new Vector2(1, 0);
        bottomBar.pivot = new Vector2(0.5f, 0f);
        bottomBar.sizeDelta = new Vector2(0, 0);
        bottomBarGO.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 1f);

        // 3. Subtitle Text
        GameObject subGO = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGO.transform.SetParent(bottomBarGO.transform, false);
        RectTransform subRT = subGO.GetComponent<RectTransform>();
        subRT.anchorMin = Vector2.zero;
        subRT.anchorMax = Vector2.one;
        subRT.offsetMin = new Vector2(40, 10);
        subRT.offsetMax = new Vector2(-40, -10);

        subtitleText = subGO.GetComponent<TextMeshProUGUI>();
        subtitleText.fontSize = 22;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.fontStyle = FontStyles.Bold;
        subtitleText.color = Color.white;
        TMP_FontAsset font = DungeonUIAssetHelper.GetFontAsset();
        if (font != null) subtitleText.font = font;

        // 4. Skip Prompt Text
        GameObject skipGO = new GameObject("SkipPrompt", typeof(RectTransform), typeof(TextMeshProUGUI));
        skipGO.transform.SetParent(topBarGO.transform, false);
        RectTransform skipRT = skipGO.GetComponent<RectTransform>();
        skipRT.anchorMin = new Vector2(1, 0.5f);
        skipRT.anchorMax = new Vector2(1, 0.5f);
        skipRT.pivot = new Vector2(1, 0.5f);
        skipRT.anchoredPosition = new Vector2(-25, 0);

        skipPromptText = skipGO.GetComponent<TextMeshProUGUI>();
        skipPromptText.fontSize = 13;
        skipPromptText.color = new Color(0.7f, 0.7f, 0.75f, 0.8f);
        skipPromptText.alignment = TextAlignmentOptions.Right;
        if (font != null) skipPromptText.font = font;

        cutsceneCanvasGO.SetActive(false);
    }
}
