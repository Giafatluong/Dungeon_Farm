using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows a small "[E] action" prompt right above the player whenever an object
/// that can be used with the E key (chest, kitchen, statue, dungeon entrance, bed) is in range.
/// The UI is built at runtime, so no prefab / scene setup is required.
/// </summary>
[DisallowMultipleComponent]
public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    [Header("Layout")]
    [Tooltip("Extra world-space offset above the top of the player's sprite")]
    public float headMargin = 0.2f;
    [Tooltip("Fallback offset from player pivot when no SpriteRenderer is found")]
    public Vector3 fallbackWorldOffset = new Vector3(0f, 1.2f, 0f);
    public float fadeSpeed = 12f;
    public float bobAmplitude = 3f;
    public float bobSpeed = 4f;

    [Header("Labels")]
    public string keyLabel = "E";
    public string chestLabel = "Mở rương";
    public string kitchenLabel = "Nấu ăn";
    public string statueLabel = "Dâng lễ vật";
    public string dungeonLabel = "Vào hầm ngục";
    public string bedLabel = "Đi ngủ";

    private PlayerInteraction playerInteraction;
    private Transform playerRoot;
    private SpriteRenderer playerSprite;
    private Camera cam;

    private Canvas canvas;
    private RectTransform panelRect;
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI labelText;

    private string currentLabel;
    private float popScale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        playerInteraction = GetComponent<PlayerInteraction>();
        ResolvePlayer();

        BuildUI();
    }

    private void ResolvePlayer()
    {
        playerRoot = PlayerMovement.Instance != null ? PlayerMovement.Instance.transform : transform;
        playerSprite = playerRoot.GetComponent<SpriteRenderer>();
        if (playerSprite == null) playerSprite = playerRoot.GetComponentInChildren<SpriteRenderer>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (canvas != null) Destroy(canvas.gameObject);
    }

    #region UI Construction
    private void BuildUI()
    {
        // Overlay canvas (ignores player scale / flip)
        GameObject canvasGO = new GameObject("InteractionPromptCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Panel
        GameObject panelGO = new GameObject("Prompt", typeof(RectTransform));
        panelGO.transform.SetParent(canvasGO.transform, false);
        panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = new Vector2(0.5f, 0f);

        canvasGroup = panelGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;   // never block mouse clicks (farm plots etc.)
        canvasGroup.interactable = false;

        Image bg = panelGO.AddComponent<Image>();
        bg.color = new Color(0.09f, 0.07f, 0.06f, 0.88f);
        bg.raycastTarget = false;
        Outline border = panelGO.AddComponent<Outline>();
        border.effectColor = new Color(1f, 0.82f, 0.35f, 0.55f);
        border.effectDistance = new Vector2(1.5f, -1.5f);

        HorizontalLayoutGroup layout = panelGO.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 14, 6, 6);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Key badge "E"
        GameObject keyGO = new GameObject("Key", typeof(RectTransform));
        keyGO.transform.SetParent(panelGO.transform, false);
        Image keyBg = keyGO.AddComponent<Image>();
        keyBg.color = new Color(1f, 0.84f, 0.32f, 1f);
        keyBg.raycastTarget = false;
        Shadow keyShadow = keyGO.AddComponent<Shadow>();
        keyShadow.effectColor = new Color(0.45f, 0.3f, 0.05f, 1f);
        keyShadow.effectDistance = new Vector2(0f, -3f);
        LayoutElement keyLE = keyGO.AddComponent<LayoutElement>();
        keyLE.minWidth = keyLE.preferredWidth = 30f;
        keyLE.minHeight = keyLE.preferredHeight = 30f;

        GameObject keyTextGO = new GameObject("KeyText", typeof(RectTransform));
        keyTextGO.transform.SetParent(keyGO.transform, false);
        RectTransform keyTextRect = keyTextGO.GetComponent<RectTransform>();
        keyTextRect.anchorMin = Vector2.zero;
        keyTextRect.anchorMax = Vector2.one;
        keyTextRect.offsetMin = Vector2.zero;
        keyTextRect.offsetMax = Vector2.zero;
        TextMeshProUGUI keyText = keyTextGO.AddComponent<TextMeshProUGUI>();
        keyText.text = keyLabel;
        keyText.fontSize = 20f;
        keyText.fontStyle = FontStyles.Bold;
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.color = new Color(0.18f, 0.12f, 0.05f, 1f);
        keyText.raycastTarget = false;

        // Action label
        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(panelGO.transform, false);
        labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.fontSize = 20f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.color = new Color(1f, 0.97f, 0.9f, 1f);
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        labelText.raycastTarget = false;
    }
    #endregion

    #region Update
    private void LateUpdate()
    {
        if (panelRect == null) return;
        if (playerRoot == null || (PlayerMovement.Instance != null && playerRoot != PlayerMovement.Instance.transform))
            ResolvePlayer();

        string label = FindPromptLabel();
        bool show = label != null;

        if (show && label != currentLabel)
        {
            currentLabel = label;
            labelText.text = label;
            popScale = 0.8f;
        }

        float target = show ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, fadeSpeed * Time.unscaledDeltaTime);
        if (canvasGroup.alpha <= 0f)
        {
            currentLabel = null;
            return;
        }

        popScale = Mathf.MoveTowards(popScale, 1f, 2.5f * Time.unscaledDeltaTime);
        panelRect.localScale = Vector3.one * popScale;

        PositionPanel();
    }

    private void PositionPanel()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || canvas == null) return;

        Vector3 worldPos;
        if (playerSprite != null && playerSprite.enabled)
        {
            Bounds b = playerSprite.bounds;
            worldPos = new Vector3(b.center.x, b.max.y + headMargin, 0f);
        }
        else
        {
            worldPos = playerRoot.position + fallbackWorldOffset;
        }

        Vector3 screen = cam.WorldToScreenPoint(worldPos);
        if (screen.z < 0f)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        float scale = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        float bob = Mathf.Sin(Time.unscaledTime * bobSpeed) * bobAmplitude;
        panelRect.anchoredPosition = new Vector2(screen.x / scale, screen.y / scale + bob);
    }
    #endregion

    #region Target Detection
    /// <summary>
    /// Returns the label of the nearest E-interactable in range, or null if none.
    /// Mirrors the priority rules used by each interactable's own E handling.
    /// </summary>
    private string FindPromptLabel()
    {
        if (playerInteraction != null && !playerInteraction.CanInteractNow())
            return null;

        Vector2 playerPos = playerInteraction != null ? (Vector2)playerInteraction.transform.position : (Vector2)playerRoot.position;
        float bestSqr = float.MaxValue;
        string best = null;

        HomeChest chest = HomeChest.Instance;
        if (chest != null && chest.playerInRange && !chest.isOpen)
            Consider(chest.transform, chestLabel, playerPos, ref bestSqr, ref best);

        KitchenStation kitchen = KitchenStation.Instance;
        if (kitchen != null && kitchen.playerInRange && !kitchen.isOpen)
        {
            string kLabel = kitchen.hasCompletedFood
                ? $"Nhận: {kitchen.completedRecipe.DisplayName}"
                : (kitchen.isCooking ? "Đang nấu..." : kitchenLabel);
            Consider(kitchen.transform, kLabel, playerPos, ref bestSqr, ref best);
        }

        Statue statue = Statue.Instance;
        if (statue != null && statue.playerInRange)
            Consider(statue.transform, statueLabel, playerPos, ref bestSqr, ref best, statue.GetStatueCenter());

        DungeonEntrance entrance = DungeonEntrance.Instance;
        if (entrance != null && entrance.playerInRange)
            Consider(entrance.transform, dungeonLabel, playerPos, ref bestSqr, ref best);

        if (best != null) return best;

        // Bed only triggers when nothing else is in range (see PlayerInteraction.Update)
        if (playerInteraction != null && playerInteraction.currentBeds.Count > 0)
        {
            BedInteraction bed = playerInteraction.GetNearestBed();
            if (bed != null && bed.IsPlayerInRange(playerPos))
                return bedLabel;
        }

        return null;
    }

    private static void Consider(Transform target, string label, Vector2 playerPos, ref float bestSqr, ref string best, Vector3? overridePos = null)
    {
        if (target == null) return;
        Vector2 pos = overridePos.HasValue ? (Vector2)overridePos.Value : (Vector2)target.position;
        float sqr = (pos - playerPos).sqrMagnitude;
        if (sqr < bestSqr)
        {
            bestSqr = sqr;
            best = label;
        }
    }
    #endregion
}
