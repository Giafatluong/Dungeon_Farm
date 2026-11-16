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
                    if (Application.isPlaying) DontDestroyOnLoad(go);
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
        { 0, "Hầm Ngục Hướng Dẫn" },
        { 1, "Hầm Ngục Cổ Xưa" },
        { 2, "Đầm Lầy Quái Vật" },
        { 3, "Thành Trì Ác Quỷ" },
        { 4, "Hầm Mộ Băng Giá" },
        { 5, "Lò Rèn Hỏa Ngục" },
        { 6, "Địa Đạo Lãng Quên" },
        { 7, "Thánh Điện Huyết Tộc" },
        { 8, "Tàn Tích Hư Vô" },
        { 9, "Vực Sâu Hắc Ám" },
        { 10, "Ngai Vàng Vĩnh Cửu" }
    };

    // Curated Tips Pool
    private static readonly string[] DungeonTips = new string[]
    {
        "Mẹo: Hãy luôn chọn Phước Lành trước khi vào hầm ngục để nhận sức mạnh bổ trợ to lớn!",
        "Mẹo: Thu thập quặng và đá ma thuật để nâng cấp các pho tượng vĩnh viễn tại căn cứ.",
        "Mẹo: Khi xuất hiện vòng đỏ cảnh báo dưới chân, hãy lập tức né tránh kỹ năng của Boss!",
        "Mẹo: Trạm nghỉ cho phép bạn hồi máu an toàn hoặc tiếp tục tiến sâu để nhận thêm thưởng.",
        "Mẹo: Các món ăn nấu tại nhà bếp sẽ cung cấp lượng hồi phục và buff chỉ số vượt trội.",
        "Mẹo: Hãy quản lý điểm thể lực cẩn thận, cân đối giữa tấn công và phòng thủ.",
        "Mẹo: Đánh bại Boss ở tầng cuối mỗi khu vực để mở khóa tầng hầm ngục tiếp theo.",
        "Mẹo: Hành trang đầy? Hãy ưu tiên giữ lại các vật phẩm quý hiếm và đá thăng cấp."
    };

    private static readonly string[] BaseTips = new string[]
    {
        "Mẹo: Đừng quên tưới nước cho các mảnh đất đã gieo hạt để cây trồng phát triển tốt nhất.",
        "Mẹo: Thương nhân tại căn cứ sẽ cập nhật hàng hóa và hạt giống mới mỗi ngày.",
        "Mẹo: Đem nông sản và chiến lợi phẩm về nhà bếp để chế biến các công thức nấu ăn mới.",
        "Mẹo: Dâng lễ tượng thần tại căn cứ để nhận các chỉ số vĩnh viễn và mở khóa phước lành.",
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
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);
        isTransitioning = false;
        EnsureUIConstructed();
        HideImmediate();
    }

    private void Start()
    {
        if (!isTransitioning)
        {
            HideImmediate();
        }
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
            string floorName = FloorNames.TryGetValue(floorNum, out string name) ? name : $"Tầng {floorNum}";

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
                emblemText.text = "[ CHIẾN ĐẤU ]";
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
                subtitleText.text = "[ NÔNG TRẠI YÊN BÌNH ]";
            }

            if (emblemText != null)
            {
                emblemText.text = "[ NÔNG TRẠI ]";
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

        // Lock and dismiss all modals, HUD and input during transition
        UIEscapeManager.CloseAllModals(includeInventory: true);
        UIEscapeManager.SetHUDVisible(false);
        ItemTooltipUI.Instance?.Hide(immediate: true);
        DayTransitionUI.Instance?.ResetToHiddenState();
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        targetCanvas.gameObject.SetActive(true);
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

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

        // Re-ensure UI references after scene load
        EnsureUIConstructed();

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
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutTime);
            }
            yield return null;
        }

        HideImmediate();
        isTransitioning = false;
        UIEscapeManager.SetHUDVisible(true);
        CurrentItemPanel.RefreshHotbar();
    }

    private void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        if (targetCanvas != null)
        {
            targetCanvas.gameObject.SetActive(false);
        }
    }

    #region Scene UI Binding
    public void EnsureUIConstructed()
    {
        if (targetCanvas != null && canvasGroup != null)
        {
            targetCanvas.sortingOrder = 9999;
            BindExistingUIElements();
            return;
        }

        Canvas[] existingCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<Canvas> found = new List<Canvas>();
        for (int i = 0; i < existingCanvases.Length; i++)
        {
            if (existingCanvases[i] != null && existingCanvases[i].name == "[LoadingScreenCanvas]")
            {
                found.Add(existingCanvases[i]);
            }
        }

        for (int i = 0; i < found.Count; i++)
        {
            if (found[i] != null && found[i].transform.childCount > 0)
            {
                if (targetCanvas == null)
                {
                    targetCanvas = found[i];
                    canvasGroup = targetCanvas.GetComponent<CanvasGroup>();
                }
                else
                {
                    // Clean up redundant duplicate canvases in scene
                    Destroy(found[i].gameObject);
                }
            }
        }

        if (targetCanvas == null && found.Count > 0)
        {
            targetCanvas = found[0];
            canvasGroup = targetCanvas.GetComponent<CanvasGroup>();
        }

        if (targetCanvas != null)
        {
            targetCanvas.transform.SetParent(this.transform, false);
            targetCanvas.sortingOrder = 9999;
            BindExistingUIElements();
            return;
        }
    }

    public bool IsActuallyVisible() => isTransitioning && canvasGroup != null && canvasGroup.alpha > 0.05f;

    private void BindExistingUIElements()
    {
        if (targetCanvas == null) return;
        Transform root = targetCanvas.transform;

        if (canvasGroup == null) canvasGroup = targetCanvas.GetComponent<CanvasGroup>();
        if (bgImage == null) bgImage = root.Find("Background")?.GetComponent<Image>();
        if (titleText == null) titleText = (root.Find("HeaderGroup/TitleText") ?? root.Find("HeaderGroup/Title"))?.GetComponent<TextMeshProUGUI>();
        if (subtitleText == null) subtitleText = (root.Find("HeaderGroup/SubtitleText") ?? root.Find("HeaderGroup/Subtitle"))?.GetComponent<TextMeshProUGUI>();
        if (emblemText == null) emblemText = root.Find("CenterEmblemGroup/EmblemText")?.GetComponent<TextMeshProUGUI>();
        if (statusText == null) statusText = root.Find("CenterEmblemGroup/StatusText")?.GetComponent<TextMeshProUGUI>();
        if (progressFillImage == null) progressFillImage = root.Find("ProgressBarGroup/BarFill")?.GetComponent<Image>();
        if (percentText == null) percentText = root.Find("ProgressBarGroup/PercentText")?.GetComponent<TextMeshProUGUI>();
        if (tipHeaderText == null) tipHeaderText = root.Find("TipBox/TipHeader")?.GetComponent<TextMeshProUGUI>();
        if (tipBodyText == null) tipBodyText = root.Find("TipBox/TipBody")?.GetComponent<TextMeshProUGUI>();
    }

    #endregion
}

