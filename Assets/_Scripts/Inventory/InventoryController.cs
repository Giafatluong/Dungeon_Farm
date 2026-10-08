using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private InventoryPanel inventoryPanel;
    [SerializeField] private CurrentItemPanel currentItemPanel;
    public bool isInteractable = true;

    private void Awake()
    {
        if (inventoryPanel == null)
            inventoryPanel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
        if (currentItemPanel == null)
            currentItemPanel = FindFirstObjectByType<CurrentItemPanel>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        isInteractable = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            SwapInventoryPanel();
        }
    }

    private void SwapInventoryPanel()
    {
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

    private void CloseInventoryPanel()
    {
        if (inventoryPanel != null)
            inventoryPanel.gameObject.SetActive(false);

        if (currentItemPanel != null)
            currentItemPanel.gameObject.SetActive(true);

        isInteractable = true;
    }

    private void OpenInventoryPanel()
    {
        if (inventoryPanel != null)
            inventoryPanel.gameObject.SetActive(true);

        if (currentItemPanel != null)
            currentItemPanel.gameObject.SetActive(false);

        isInteractable = false;
    }
}