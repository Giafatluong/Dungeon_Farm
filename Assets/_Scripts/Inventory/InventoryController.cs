using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private InventoryPanel inventoryPanel;
    [SerializeField] private CurrentItemPanel currentItemPanel;
    public bool isInteractable;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            SwapInventoryPanel();
        }
    }

    private void SwapInventoryPanel()
    {
        if(inventoryPanel.gameObject.activeSelf)
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
        inventoryPanel.gameObject.SetActive(false);
        currentItemPanel.gameObject.SetActive(true);
        isInteractable = true;
    }
    private void OpenInventoryPanel()
    {
        inventoryPanel.gameObject.SetActive(true);
        currentItemPanel.gameObject.SetActive(false);
        isInteractable = false;
    }
}