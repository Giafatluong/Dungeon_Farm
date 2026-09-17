using UnityEngine;

public class DayManager : MonoBehaviour
{
    public int currentday = 1;
    public event System.Action OnNewDay;

    public void NextDay()
    {
        currentday++;
        OnNewDay?.Invoke();
    }
}
