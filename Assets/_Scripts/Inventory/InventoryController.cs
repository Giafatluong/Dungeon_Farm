using UnityEngine;

public class InventoryController : MonoBehaviour
{
    public static InventoryController Instance { get; private set; }

    [SerializeField] private InventoryPanel inventoryPanel;
    [SerializeField] private CurrentItemPanel currentItemPanel;
    public bool isInteractable = true;

    public bool IsInventoryOpen => inventoryPanel != null && inventoryPanel.gameObject.activeSelf;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (inventoryPanel == null)
            inventoryPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
        if (currentItemPanel == null)
            currentItemPanel = FindFirstObjectByType<CurrentItemPanel>(FindObjectsInactive.Include);

        // Ensure Inventory Canvas sorting order is properly elevated above base world and tutorial HUD
        Canvas c = GetComponent<Canvas>();
        if (c != null && c.sortingOrder < 25)
        {
            c.sortingOrder = 25;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        isInteractable = true;
    }

    private void Update()
    {
        // Close inventory on Escape if it is open
        if (IsInventoryOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            UIEscapeManager.ConsumeEscape();
            CloseInventoryPanel();
            return;
        }

        // Toggle inventory with I or B
        if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.B))
        {
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsInputBlocked)
                return;

            // Gate during tutorial (e.g. movement phase must finish before opening inventory)
            if (!IsInventoryOpen && TutorialManager.Instance != null && !TutorialManager.Instance.IsInteractionAllowed(TutorialManager.TutorialInteraction.Inventory))
            {
                PlayerMovement pm = PlayerMovement.Instance;
                Vector3 pos = pm != null ? pm.transform.position + Vector3.up * 1.0f : transform.position;
                DamagePopupManager.ShowText(pos, "Hãy hoàn thành bước hiện tại trước!", new Color(1f, 0.9f, 0.3f), 1.5f);
                return;
            }

            // Do not open inventory if a modal popup (Chest, Kitchen, Statue, Dungeon, etc.) is active
            if (!IsInventoryOpen && IsAnyModalOpen())
                return;

            SwapInventoryPanel();
        }
    }

    public static bool IsAnyModalOpen()
    {
        if (UIEscapeManager.IsAnyModalUIOpen(includeInventory: false, includeMainMenu: true)) return true;
        if (CombatManager.Instance != null && (CombatManager.Instance.isCombatActive || CombatManager.Instance.isPreparingCombat)) return true;
        return false;
    }

    private void EnsurePanels()
    {
        if (inventoryPanel == null)
            inventoryPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
        if (currentItemPanel == null)
            currentItemPanel = FindFirstObjectByType<CurrentItemPanel>(FindObjectsInactive.Include);
    }

    public void SwapInventoryPanel()
    {
        EnsurePanels();
        if (inventoryPanel == null)
            return;

        if (inventoryPanel.gameObject.activeSelf)
        {
            CloseInventoryPanel();
        }
        else
        {
            OpenInventoryPanel();
        }
    }

    public void CloseInventoryPanel()
    {
        EnsurePanels();
        if (inventoryPanel != null)
            inventoryPanel.gameObject.SetActive(false);

        if (currentItemPanel != null)
        {
            currentItemPanel.gameObject.SetActive(true);
            currentItemPanel.SetCurrentItem();
        }

        isInteractable = true;
        UIEscapeManager.NotifyModalClosed("Inventory");
        CurrentItemPanel.RefreshHotbar();
    }

    public void OpenInventoryPanel()
    {
        if (IsAnyModalOpen()) return;

        EnsurePanels();
        if (inventoryPanel != null)
        {
            inventoryPanel.gameObject.SetActive(true);
            inventoryPanel.SetInventoryButton();
        }

        if (currentItemPanel != null)
            currentItemPanel.gameObject.SetActive(false);

        Canvas c = GetComponent<Canvas>();
        if (c != null && c.sortingOrder < 25)
        {
            c.sortingOrder = 25;
        }

        isInteractable = false;
        UIEscapeManager.NotifyModalOpened("Inventory");
    }
}