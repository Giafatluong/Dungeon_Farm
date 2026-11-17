using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Controls the Dungeon Entrance portal / gate at Base.
/// Allows the player to approach, view an interaction prompt, and either press [E]
/// or click with mouse to open the Floor Selection UI and enter the dungeon.
/// </summary>
[DisallowMultipleComponent]
public class DungeonEntrance : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private DungeonEntranceUI dungeonEntranceUI;

    public static DungeonEntrance Instance { get; private set; }
    public bool playerInRange { get; private set; }
    private Collider2D col;
    private Transform playerTransform;
    private float nextPlayerSearchTime;
    private bool isHovered = false;

    [Header("Floating Prompt UI")]
    [SerializeField] private Canvas promptCanvas;
    [SerializeField] private RectTransform promptPanelRect;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private TextMeshProUGUI promptTitleText;
    [SerializeField] private TextMeshProUGUI promptHintText;
    [SerializeField] private Button promptClickButton;

    public const string SpawnAtEntranceKey = "SpawnAtDungeonEntrance";

    public static void SetReturnToEntranceFlag()
    {
        PlayerPrefs.SetInt(SpawnAtEntranceKey, 1);
        PlayerPrefs.Save();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (promptCanvas != null)
        {
            if (Application.isPlaying)
                Destroy(promptCanvas.gameObject);
            promptCanvas = null;
        }
    }

    private void Start()
    {
        col = GetComponent<Collider2D>();
        if (dungeonEntranceUI == null)
        {
            dungeonEntranceUI = FindFirstObjectByType<DungeonEntranceUI>(FindObjectsInactive.Include);
        }
        FindPlayer();
        EnsurePromptUI();

        if (PlayerPrefs.GetInt(SpawnAtEntranceKey, 0) == 1)
        {
            PlayerPrefs.SetInt(SpawnAtEntranceKey, 0);
            PlayerPrefs.Save();
            PositionPlayerAtEntrance();
        }
    }

    public void PositionPlayerAtEntrance()
    {
        if (playerTransform == null) FindPlayer();
        if (playerTransform != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(0f, -1.2f, 0f);
            playerTransform.position = spawnPos;

            Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = spawnPos;
                rb.linearVelocity = Vector2.zero;
            }

            if (Camera.main != null)
            {
                CameraController cam = Camera.main.GetComponent<CameraController>();
                Vector3 camOffset = cam != null ? cam.offset : new Vector3(0, 1.5f, -10f);
                Camera.main.transform.position = spawnPos + camOffset;
            }
        }
    }

    private void FindPlayer()
    {
        if (PlayerMovement.Instance != null)
        {
            playerTransform = PlayerMovement.Instance.transform;
            return;
        }
        if (PlayerStats.Instance != null)
        {
            playerTransform = PlayerStats.Instance.transform;
            return;
        }
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null && Time.time >= nextPlayerSearchTime)
        {
            nextPlayerSearchTime = Time.time + 0.5f;
            FindPlayer();
        }

        if (playerTransform != null)
        {
            Vector3 center = col != null ? col.bounds.center : transform.position;
            float dist = Vector2.Distance(center, playerTransform.position);
            playerInRange = dist <= 2.2f;
        }

        HandleMouseInteraction();
        UpdatePromptPositionAndVisibility();

        if (!playerInRange)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            OpenDungeonUI();
        }
    }

    private void HandleMouseInteraction()
    {
        if (Camera.main == null || col == null) return;

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        bool hovered = col.OverlapPoint(mouseWorld);

        if (hovered && !isHovered)
        {
            isHovered = true;
        }
        else if (!hovered && isHovered)
        {
            isHovered = false;
        }

        if (hovered && Input.GetMouseButtonDown(0))
        {
            if (playerInRange)
            {
                OpenDungeonUI();
            }
        }
    }

    public void EnsurePromptUI()
    {
        if (promptCanvas != null) return;

        GameObject canvasGO = GameObject.Find("DungeonEntrancePromptCanvas");
        if (canvasGO != null)
        {
            promptCanvas = canvasGO.GetComponent<Canvas>();
            promptPanelRect = canvasGO.transform.Find("PromptPanel")?.GetComponent<RectTransform>();
            if (promptPanelRect != null)
            {
                promptCanvasGroup = promptPanelRect.GetComponent<CanvasGroup>();
                promptClickButton = promptPanelRect.GetComponent<Button>();
                promptTitleText = promptPanelRect.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                promptHintText = promptPanelRect.Find("HintText")?.GetComponent<TextMeshProUGUI>();

                if (promptClickButton != null && Application.isPlaying)
                {
                    promptClickButton.onClick.RemoveListener(OpenDungeonUI);
                    promptClickButton.onClick.AddListener(OpenDungeonUI);
                }
            }
            if (promptCanvas != null && promptPanelRect != null) return;
        }

        Debug.LogWarning("[DungeonEntrance] DungeonEntrancePromptCanvas not found in scene!");
    }

    private void UpdatePromptPositionAndVisibility()
    {
        if (promptCanvas == null || promptPanelRect == null || promptCanvasGroup == null)
            return;

        bool isModalOpen = (dungeonEntranceUI != null && dungeonEntranceUI.IsOpen) || UIEscapeManager.IsAnyModalUIOpen();
        bool shouldShow = playerInRange && !isModalOpen;

        float targetAlpha = shouldShow ? 1f : 0f;
        promptCanvasGroup.alpha = Mathf.MoveTowards(promptCanvasGroup.alpha, targetAlpha, Time.deltaTime * 6f);
        promptCanvasGroup.blocksRaycasts = shouldShow;

        if (promptCanvasGroup.alpha > 0.01f && Camera.main != null)
        {
            Vector3 worldPos = transform.position + new Vector3(0f, 1.8f, 0f);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            if (promptCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                promptPanelRect.anchoredPosition = new Vector2(screenPos.x, screenPos.y);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (playerInRange)
        {
            OpenDungeonUI();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    private void OnMouseDown()
    {
        if (playerInRange)
        {
            OpenDungeonUI();
        }
    }

    public void OpenDungeonUI()
    {
        if (TutorialManager.Instance != null && !TutorialManager.Instance.IsInteractionAllowed(TutorialManager.TutorialInteraction.DungeonGate))
        {
            DamagePopupManager.ShowText(transform.position + Vector3.up * 1.0f, "Cổng hầm ngục đang khóa! Hãy làm theo hướng dẫn.", Color.yellow, 2.0f);
            return;
        }

        if (UIEscapeManager.IsAnyModalUIOpen()) return;

        // In tutorial: bypass floor selection UI and directly enter Floor 0 ONLY if tutorial is actively at Dungeon Gate
        bool isTutorialActive = (TutorialManager.Instance != null && !TutorialManager.Instance.IsTutorialCompleted && TutorialManager.Instance.CurrentStep == TutorialManager.TutorialStep.DungeonGate)
                             && PlayerPrefs.GetInt(TutorialManager.DungeonTutorialDoneKey, 0) == 0;

        if (isTutorialActive)
        {
            EnterTutorialFloorDirectly();
            return;
        }

        if (dungeonEntranceUI == null)
        {
            dungeonEntranceUI = FindFirstObjectByType<DungeonEntranceUI>(FindObjectsInactive.Include);
            if (dungeonEntranceUI == null)
            {
                Debug.LogWarning("[DungeonEntrance] Dungeon Entrance UI not found.");
                return;
            }
        }

        dungeonEntranceUI.Open();
    }

    private void EnterTutorialFloorDirectly()
    {
        Debug.Log("[DungeonEntrance] In Tutorial: directly entering Floor 0 without floor selection UI.");
        if (dungeonEntranceUI == null)
        {
            dungeonEntranceUI = FindFirstObjectByType<DungeonEntranceUI>(FindObjectsInactive.Include);
        }

        if (dungeonEntranceUI != null)
        {
            dungeonEntranceUI.EnterFloorDirectly(0);
            return;
        }

        TutorialManager.Instance?.NotifyDungeonEntered();

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.currentFloor = 0;
        }

        if (SceneTransitionManager.Instance == null)
        {
            ProgressionManager.Instance?.StartRun(0);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Dungeon");
            return;
        }

        SceneTransitionManager.Instance.LoadDungeon(0);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
