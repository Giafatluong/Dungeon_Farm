using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private RecipeData _cachedCompletedRecipe;
    private string _cachedFoodClaimLabel;

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
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    #region UI Construction
    public bool BindSceneReferences()
    {
        if (canvas == null)
        {
            Transform t = transform.Find("InteractionPromptCanvas");
            if (t != null) canvas = t.GetComponent<Canvas>();
            else
            {
                Canvas[] all = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].name == "InteractionPromptCanvas")
                    {
                        canvas = all[i];
                        break;
                    }
                }
            }
        }

        if (canvas == null) return false;

        Transform canvasT = canvas.transform;
        Transform promptT = canvasT.Find("Prompt");
        if (promptT != null)
        {
            panelRect = promptT.GetComponent<RectTransform>();
            canvasGroup = promptT.GetComponent<CanvasGroup>();
            labelText = promptT.Find("Label")?.GetComponent<TextMeshProUGUI>();
        }
        return panelRect != null && canvasGroup != null;
    }

    public void BuildUI()
    {
        if (BindSceneReferences()) return;
        Debug.LogWarning("[InteractionPromptUI] InteractionPromptCanvas not found in scene or parent.");
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
            string kLabel;
            if (kitchen.hasCompletedFood)
            {
                if (_cachedCompletedRecipe != kitchen.completedRecipe)
                {
                    _cachedCompletedRecipe = kitchen.completedRecipe;
                    _cachedFoodClaimLabel = $"Nhận: {kitchen.completedRecipe.DisplayName}";
                }
                kLabel = _cachedFoodClaimLabel;
            }
            else
            {
                kLabel = kitchen.isCooking ? "Đang nấu..." : kitchenLabel;
            }
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
