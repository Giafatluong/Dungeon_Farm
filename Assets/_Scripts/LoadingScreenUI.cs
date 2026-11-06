using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// High-end, fantasy-styled procedural Loading Screen UI displayed during scene transitions
/// (between Base Farm and Dungeon floors). Features animated progress bars, live progress percentages,
/// destination info, ambient pulsing emblem, and helpful gameplay tips.
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    private static LoadingScreenUI _instance;
    public static LoadingScreenUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<LoadingScreenUI>(FindObjectsInactive.Include);
                if (_instance == null)
                {
                    GameObject go = new GameObject("[LoadingScreenUI]");
                    _instance = go.AddComponent<LoadingScreenUI>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    public static LoadingScreenUI EnsureInstance() => Instance;

    [Header("UI Elements (Auto constructed if null)")]
    private Canvas targetCanvas;
    private CanvasGroup canvasGroup;
    private Image bgImage;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI emblemText;
    private Image progressFillImage;
    private TextMeshProUGUI percentText;
    private TextMeshProUGUI tipHeaderText;
    private TextMeshProUGUI tipBodyText;

    private bool isTransitioning = false;
    public bool IsTransitioning => isTransitioning;

    // Floor Names Map
    private static readonly Dictionary<int, string> FloorNames = new Dictionary<int, string>
    {
        { 0, "Tutorial Catacombs" },
        { 1, "Ancient Dungeon" },
        { 2, "Monster Swamp" },
        { 3, "Demon Citadel" },
        { 4, "Frozen Crypt" },
        { 5, "Infernal Forge" },
        { 6, "Forgotten Catacombs" },
        { 7, "Blood Sanctum" },
        { 8, "Ruins of the Void" },
        { 9, "Abyssal Depths" },
        { 10, "The Eternal Throne" }
    };

    // Curated Tips Pool
    private static readonly string[] DungeonTips = new string[]
    {
        "Mẹo: Hãy luôn chọn Phước Lành trước khi vào hầm ngục để nhận sức mạnh bổ trợ to lớn!",
        "Mẹo: Thu thập quặng và đá ma thuật để nâng cấp các pho tượng vĩnh viễn tại căn cứ.",
        "Mẹo: Khi xuất hiện vòng đỏ cảnh báo dưới chân, hãy lập tức né tránh kỹ năng của Boss!",
        "Mẹo: Trạm nghỉ (Camp) cho phép bạn hồi máu an toàn hoặc tiếp tục tiến sâu để nhận thêm thưởng.",
        "Mẹo: Các món ăn nấu tại nhà bếp sẽ cung cấp lượng hồi phục và buff chỉ số vượt trội.",
        "Mẹo: Hãy quản lý điểm thể lực (AP) cẩn thận, cân đối giữa tấn công và phòng thủ.",
        "Mẹo: Đánh bại Boss ở tầng cuối mỗi khu vực để mở khóa tầng hầm ngục tiếp theo.",
        "Mẹo: Hành trang đầy? Hãy ưu tiên giữ lại các vật phẩm quý hiếm và đá thăng cấp."
    };

    private static readonly string[] BaseTips = new string[]
    {
        "Mẹo: Đừng quên tưới nước cho các mảnh đất đã gieo hạt để cây trồng phát triển tốt nhất.",
        "Mẹo: Thương nhân tại căn cứ sẽ cập nhật hàng hóa và hạt giống mới mỗi ngày.",
        "Mẹo: Đem nông sản và chiến lợi phẩm về nhà bếp để chế biến các công thức nấu ăn mới.",
        "Mẹo: Nâng cấp tượng thần tại căn cứ để tăng vĩnh viễn ATK, DEF và Max HP cho nhân vật.",
        "Mẹo: Thu hoạch cây trồng kịp thời trước khi bắt đầu chuyến thám hiểm hầm ngục tiếp theo.",
        "Mẹo: Bạn có thể kiểm tra báo cáo buổi sáng để nắm tình trạng nông trại nhanh chóng."
    };

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureUIConstructed();
        HideImmediate();
    }

    /// <summary>
    /// Loads the target scene asynchronously while displaying a smooth, stylish loading screen.
    /// </summary>
    public void LoadScene(string sceneName, int targetFloor = -1, System.Action onBeforeLoad = null, System.Action onAfterLoad = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning($"[LoadingScreenUI] Transition to '{sceneName}' ignored: already transitioning.");
            return;
        }

        StartCoroutine(TransitionRoutine(sceneName, targetFloor, onBeforeLoad, onAfterLoad));
    }

    private IEnumerator TransitionRoutine(string sceneName, int targetFloor, System.Action onBeforeLoad, System.Action onAfterLoad)
    {
        isTransitioning = true;
        Time.timeScale = 1f;

        EnsureUIConstructed();

        // 1. Setup Theme, Titles & Tips based on destination
        bool isGoingToDungeon = sceneName.Equals("Dungeon", System.StringComparison.OrdinalIgnoreCase);

        if (isGoingToDungeon)
        {
            int floorNum = targetFloor >= 0 ? targetFloor : 1;
            string floorName = FloorNames.TryGetValue(floorNum, out string name) ? name : $"Floor {floorNum}";

            if (titleText != null)
            {
                titleText.text = "TIẾN VÀO HẦM NGỤC";
                titleText.color = new Color(0.98f, 0.78f, 0.20f, 1f); // Rich Gold
            }

            if (subtitleText != null)
            {
                subtitleText.text = $"[ TẦNG {floorNum} - {floorName.ToUpper()} ]";
            }

            if (emblemText != null)
            {
                emblemText.text = "[ COMBAT ]";
                emblemText.fontSize = 28;
                emblemText.color = new Color(0.98f, 0.78f, 0.20f, 1f);
            }

            if (progressFillImage != null)
            {
                progressFillImage.color = new Color(0.96f, 0.62f, 0.12f, 1f); // Amber / Gold
            }

            if (tipBodyText != null)
            {
                tipBodyText.text = DungeonTips[Random.Range(0, DungeonTips.Length)];
            }
        }
        else
        {
            if (titleText != null)
            {
                titleText.text = "TRỞ VỀ NÔNG TRẠI";
                titleText.color = new Color(0.48f, 0.88f, 0.60f, 1f); // Soft Emerald Green
            }

            if (subtitleText != null)
            {
                subtitleText.text = "[ NÔNG TRẠI YÊN BÌNH - BASE ]";
            }

            if (emblemText != null)
            {
                emblemText.text = "[ FARM ]";
                emblemText.fontSize = 28;
                emblemText.color = new Color(0.48f, 0.88f, 0.60f, 1f);
            }

            if (progressFillImage != null)
            {
                progressFillImage.color = new Color(0.18f, 0.80f, 0.44f, 1f); // Fresh Emerald
            }

            if (tipBodyText != null)
            {
                tipBodyText.text = BaseTips[Random.Range(0, BaseTips.Length)];
            }
        }

        if (statusText != null) statusText.text = "Đang chuẩn bị dữ liệu...";
        if (progressFillImage != null) progressFillImage.fillAmount = 0f;
        if (percentText != null) percentText.text = "0%";

        targetCanvas.gameObject.SetActive(true);
        canvasGroup.blocksRaycasts = true;

        // 2. Fade in screen (0.22s)
        float elapsed = 0f;
        float fadeInTime = 0.22f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInTime);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 3. Invoke pre-load actions (save data, clear temp buffs, etc.)
        onBeforeLoad?.Invoke();
        yield return null;

        // 4. Begin Async Load with fake & real progress smoothing
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
        if (asyncOp == null)
        {
            Debug.LogError($"[LoadingScreenUI] Failed to load scene '{sceneName}'!");
            HideImmediate();
            isTransitioning = false;
            yield break;
        }

        asyncOp.allowSceneActivation = false;

        float displayProgress = 0f;
        float minDisplayTime = 0.75f; // Ensures visual stability and tip readability
        float timer = 0f;

        while (displayProgress < 1f || timer < minDisplayTime)
        {
            timer += Time.unscaledDeltaTime;

            // asyncOp.progress goes from 0 to 0.9 while loading
            float realNormalized = Mathf.Clamp01(asyncOp.progress / 0.9f);
            float targetProgress = Mathf.Min(realNormalized, timer / minDisplayTime);

            displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, 2.0f * Time.unscaledDeltaTime);

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = displayProgress;
            }

            if (percentText != null)
            {
                percentText.text = $"{Mathf.RoundToInt(displayProgress * 100f)}%";
            }

            // Pulse emblem slightly
            if (emblemText != null)
            {
                float pulse = 1f + 0.08f * Mathf.Sin(timer * 6f);
                emblemText.transform.localScale = Vector3.one * pulse;
            }

            // Update status text
            if (statusText != null)
            {
                if (displayProgress < 0.35f)
                    statusText.text = "Đang nạp tài nguyên & hình ảnh...";
                else if (displayProgress < 0.75f)
                    statusText.text = "Đang khởi tạo môi trường & thực thể...";
                else if (displayProgress < 0.98f)
                    statusText.text = "Đang đồng bộ trạng thái nhân vật...";
                else
                    statusText.text = "Sẵn sàng!";
            }

            if (asyncOp.progress >= 0.9f && timer >= minDisplayTime && displayProgress >= 0.99f)
            {
                break;
            }

            yield return null;
        }

        if (progressFillImage != null) progressFillImage.fillAmount = 1f;
        if (percentText != null) percentText.text = "100%";
        if (statusText != null) statusText.text = "Hoàn tất!";

        yield return new WaitForSecondsRealtime(0.08f);

        // 5. Activate Scene
        asyncOp.allowSceneActivation = true;
        while (!asyncOp.isDone)
        {
            yield return null;
        }

        // 6. Wait 2 frames for scene objects to run Awake & Start
        yield return null;
        yield return new WaitForEndOfFrame();

        // 7. Invoke post-load actions (e.g. positioning player at portal, theme setup)
        onAfterLoad?.Invoke();
        yield return null;

        // 8. Fade out loading screen (0.28s)
        elapsed = 0f;
        float fadeOutTime = 0.28f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutTime);
            yield return null;
        }

        HideImmediate();
        isTransitioning = false;
    }

    private void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        if (targetCanvas != null)
        {
            targetCanvas.gameObject.SetActive(false);
        }
    }

    #region Procedural UI Hierarchy Construction
    public void EnsureUIConstructed()
    {
        if (targetCanvas != null && canvasGroup != null) return;

        // 1. Root Canvas
        GameObject canvasGo = new GameObject("[LoadingScreenCanvas]", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);
        DontDestroyOnLoad(canvasGo);

        targetCanvas = canvasGo.GetComponent<Canvas>();
        targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        targetCanvas.sortingOrder = 99999; // Always on top of all existing UI

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = canvasGo.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        // 2. Fullscreen Background
        GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(canvasGo.transform, false);
        RectTransform bgRT = bgGo.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        bgImage = bgGo.GetComponent<Image>();
        bgImage.color = new Color(0.043f, 0.055f, 0.078f, 1f); // Deep Obsidian Slate (#0B0E14)
        bgImage.raycastTarget = true;

        // Subtle Gold Top & Bottom Accent Lines
        CreateHorizontalLine(canvasGo.transform, 1f, new Vector2(0f, -2f), new Color(0.95f, 0.75f, 0.25f, 0.4f), 3f);
        CreateHorizontalLine(canvasGo.transform, 0f, new Vector2(0f, 2f), new Color(0.95f, 0.75f, 0.25f, 0.4f), 3f);

        // 3. Header Area (Top Centered)
        GameObject headerGo = new GameObject("HeaderGroup", typeof(RectTransform));
        headerGo.transform.SetParent(canvasGo.transform, false);
        RectTransform headerRT = headerGo.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0.5f, 0.75f);
        headerRT.anchorMax = new Vector2(0.5f, 0.75f);
        headerRT.sizeDelta = new Vector2(900f, 160f);
        headerRT.anchoredPosition = Vector2.zero;

        // Diamond deco
        GameObject decoGo = new GameObject("Deco", typeof(RectTransform), typeof(TextMeshProUGUI));
        decoGo.transform.SetParent(headerGo.transform, false);
        RectTransform decoRT = decoGo.GetComponent<RectTransform>();
        decoRT.anchoredPosition = new Vector2(0f, 40f);
        decoRT.sizeDelta = new Vector2(400f, 30f);
        TextMeshProUGUI decoTmp = decoGo.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(decoTmp);
        decoTmp.text = "<color=#F59E0B>~   *   ~</color>";
        decoTmp.fontSize = 20;
        decoTmp.alignment = TextAlignmentOptions.Center;

        // Main Title
        GameObject titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(headerGo.transform, false);
        RectTransform titleRT = titleGo.GetComponent<RectTransform>();
        titleRT.anchoredPosition = new Vector2(0f, 0f);
        titleRT.sizeDelta = new Vector2(850f, 54f);
        titleText = titleGo.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(titleText);
        titleText.text = "TIẾN VÀO HẦM NGỤC";
        titleText.fontSize = 38;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;

        // Subtitle
        GameObject subGo = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGo.transform.SetParent(headerGo.transform, false);
        RectTransform subRT = subGo.GetComponent<RectTransform>();
        subRT.anchoredPosition = new Vector2(0f, -44f);
        subRT.sizeDelta = new Vector2(850f, 36f);
        subtitleText = subGo.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(subtitleText);
        subtitleText.text = "[ TẦNG 1 - ANCIENT DUNGEON ]";
        subtitleText.fontSize = 20;
        subtitleText.color = new Color(0.70f, 0.75f, 0.82f, 1f);
        subtitleText.alignment = TextAlignmentOptions.Center;

        // 4. Center Emblem & Status
        GameObject centerGo = new GameObject("CenterGroup", typeof(RectTransform));
        centerGo.transform.SetParent(canvasGo.transform, false);
        RectTransform centerRT = centerGo.GetComponent<RectTransform>();
        centerRT.anchorMin = new Vector2(0.5f, 0.50f);
        centerRT.anchorMax = new Vector2(0.5f, 0.50f);
        centerRT.sizeDelta = new Vector2(600f, 140f);
        centerRT.anchoredPosition = new Vector2(0f, 10f);

        GameObject emblemGo = new GameObject("Emblem", typeof(RectTransform), typeof(TextMeshProUGUI));
        emblemGo.transform.SetParent(centerGo.transform, false);
        RectTransform emblemRT = emblemGo.GetComponent<RectTransform>();
        emblemRT.anchoredPosition = new Vector2(0f, 30f);
        emblemRT.sizeDelta = new Vector2(100f, 80f);
        emblemText = emblemGo.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(emblemText);
        emblemText.text = "[ DUNGEON ]";
        emblemText.fontSize = 28;
        emblemText.alignment = TextAlignmentOptions.Center;

        GameObject statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGo.transform.SetParent(centerGo.transform, false);
        RectTransform statusRT = statusGo.GetComponent<RectTransform>();
        statusRT.anchoredPosition = new Vector2(0f, -32f);
        statusRT.sizeDelta = new Vector2(600f, 32f);
        statusText = statusGo.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(statusText);
        statusText.text = "Đang chuẩn bị dữ liệu...";
        statusText.fontSize = 17;
        statusText.color = new Color(0.78f, 0.82f, 0.88f, 1f);
        statusText.alignment = TextAlignmentOptions.Center;

        // 5. Progress Bar Group (Width: 680, Height: 24)
        GameObject barGroupGo = new GameObject("ProgressBarGroup", typeof(RectTransform));
        barGroupGo.transform.SetParent(canvasGo.transform, false);
        RectTransform barGroupRT = barGroupGo.GetComponent<RectTransform>();
        barGroupRT.anchorMin = new Vector2(0.5f, 0.32f);
        barGroupRT.anchorMax = new Vector2(0.5f, 0.32f);
        barGroupRT.sizeDelta = new Vector2(680f, 26f);
        barGroupRT.anchoredPosition = Vector2.zero;

        // Bar Dark Frame Background
        GameObject barBgGo = new GameObject("BarBackground", typeof(RectTransform), typeof(Image));
        barBgGo.transform.SetParent(barGroupGo.transform, false);
        RectTransform barBgRT = barBgGo.GetComponent<RectTransform>();
        barBgRT.anchorMin = Vector2.zero;
        barBgRT.anchorMax = Vector2.one;
        barBgRT.sizeDelta = Vector2.zero;
        Image barBg = barBgGo.GetComponent<Image>();
        barBg.color = new Color(0.08f, 0.10f, 0.15f, 1f); // Dark Slate

        // Bar Fill
        GameObject barFillGo = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        barFillGo.transform.SetParent(barGroupGo.transform, false);
        RectTransform barFillRT = barFillGo.GetComponent<RectTransform>();
        barFillRT.anchorMin = new Vector2(0f, 0f);
        barFillRT.anchorMax = new Vector2(1f, 1f);
        barFillRT.sizeDelta = new Vector2(-6f, -6f); // 3px inner margin
        barFillRT.anchoredPosition = Vector2.zero;
        progressFillImage = barFillGo.GetComponent<Image>();
        progressFillImage.type = Image.Type.Filled;
        progressFillImage.fillMethod = Image.FillMethod.Horizontal;
        progressFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressFillImage.fillAmount = 0f;
        progressFillImage.color = new Color(0.96f, 0.62f, 0.12f, 1f);

        // Bar Percentage Text
        GameObject percentGo = new GameObject("PercentText", typeof(RectTransform), typeof(TextMeshProUGUI));
        percentGo.transform.SetParent(barGroupGo.transform, false);
        RectTransform percentRT = percentGo.GetComponent<RectTransform>();
        percentRT.anchoredPosition = new Vector2(0f, 28f);
        percentRT.sizeDelta = new Vector2(200f, 24f);
        percentText = percentGo.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(percentText);
        percentText.text = "0%";
        percentText.fontSize = 16;
        percentText.fontStyle = FontStyles.Bold;
        percentText.color = Color.white;
        percentText.alignment = TextAlignmentOptions.Center;

        // 6. Game Tips Box (Anchored near Bottom)
        GameObject tipBoxGo = new GameObject("TipBox", typeof(RectTransform), typeof(Image));
        tipBoxGo.transform.SetParent(canvasGo.transform, false);
        RectTransform tipBoxRT = tipBoxGo.GetComponent<RectTransform>();
        tipBoxRT.anchorMin = new Vector2(0.5f, 0.14f);
        tipBoxRT.anchorMax = new Vector2(0.5f, 0.14f);
        tipBoxRT.sizeDelta = new Vector2(860f, 84f);
        tipBoxRT.anchoredPosition = Vector2.zero;
        Image tipBg = tipBoxGo.GetComponent<Image>();
        tipBg.color = new Color(0.08f, 0.10f, 0.15f, 0.85f);

        // Tip Header
        GameObject tipHeaderGo = new GameObject("TipHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        tipHeaderGo.transform.SetParent(tipBoxGo.transform, false);
        RectTransform tipHeaderRT = tipHeaderGo.GetComponent<RectTransform>();
        tipHeaderRT.anchoredPosition = new Vector2(0f, 22f);
        tipHeaderRT.sizeDelta = new Vector2(820f, 24f);
        tipHeaderText = tipHeaderGo.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tipHeaderText);
        tipHeaderText.text = "[ MẸO HỮU ÍCH ]";
        tipHeaderText.fontSize = 14;
        tipHeaderText.fontStyle = FontStyles.Bold;
        tipHeaderText.color = new Color(0.96f, 0.62f, 0.12f, 1f);
        tipHeaderText.alignment = TextAlignmentOptions.Center;

        // Tip Body
        GameObject tipBodyGo = new GameObject("TipBody", typeof(RectTransform), typeof(TextMeshProUGUI));
        tipBodyGo.transform.SetParent(tipBoxGo.transform, false);
        RectTransform tipBodyRT = tipBodyGo.GetComponent<RectTransform>();
        tipBodyRT.anchoredPosition = new Vector2(0f, -12f);
        tipBodyRT.sizeDelta = new Vector2(800f, 44f);
        tipBodyText = tipBodyGo.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tipBodyText);
        tipBodyText.text = "Thu thập quặng và đá ma thuật để nâng cấp các pho tượng vĩnh viễn tại căn cứ.";
        tipBodyText.fontSize = 16;
        tipBodyText.color = new Color(0.90f, 0.92f, 0.96f, 1f);
        tipBodyText.alignment = TextAlignmentOptions.Center;
    }

    private void CreateHorizontalLine(Transform parent, float anchorY, Vector2 anchoredPos, Color col, float height)
    {
        GameObject line = new GameObject("AccentLine", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(parent, false);
        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, anchorY);
        rt.anchorMax = new Vector2(1f, anchorY);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = anchoredPos;
        Image img = line.GetComponent<Image>();
        img.color = col;
    }
    #endregion
}

