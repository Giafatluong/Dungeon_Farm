
using UnityEngine;

public class BedInteraction : MonoBehaviour
{
    [SerializeField] private DayManager dayManager;

    private bool isSleeping;

    public void Interact()
    {
        if (isSleeping)
            return;

        if (dayManager == null)
        {
            dayManager = FindFirstObjectByType<DayManager>();
        }

        if (dayManager == null)
        {
            Debug.Log("Day Manager is NULL");
            return;
        }

        isSleeping = true;

        dayManager.NextDay();

        Debug.Log("New Day: " + dayManager.currentday);

        isSleeping = false;
    }
}