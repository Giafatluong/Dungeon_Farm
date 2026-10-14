using UnityEngine;

public class BedInteraction : MonoBehaviour, IInteractable
{
    [Header("References")]
    public DayManager dayManager;

    private void Start()
    {
        if (dayManager == null)
        {
            dayManager = DayManager.Instance ?? FindFirstObjectByType<DayManager>();
        }
    }

    public void Interact()
    {
        if (DayTransitionUI.Instance != null && DayTransitionUI.Instance.IsOpen)
            return;

        if (ChestUI.Instance != null && ChestUI.Instance.IsOpen)
            return;

        if (HomeChest.Instance != null && (HomeChest.Instance.isOpen || HomeChest.Instance.playerInRange))
            return;

        if (KitchenUI.Instance != null && KitchenUI.Instance.IsOpen)
            return;

        if (dayManager == null)
        {
            dayManager = DayManager.Instance ?? FindFirstObjectByType<DayManager>();
        }

        if (dayManager != null)
        {
            dayManager.Sleep();
        }
        else
        {
            Debug.LogWarning("[BedInteraction] DayManager not found! Cannot sleep.");
        }
    }
}
