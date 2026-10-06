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
