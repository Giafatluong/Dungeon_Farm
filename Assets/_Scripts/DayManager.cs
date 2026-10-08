using UnityEngine;
using System.Collections;

public class DayManager : MonoBehaviour
{
    private static DayManager _instance;
    public static DayManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DayManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DayManager_Runtime");
                    _instance = go.AddComponent<DayManager>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Day Settings")]
    public int currentDay = 1;
    public float dayDuration = 120f; // seconds per day (real time), 0 = manual only

    [Header("References")]
    public PlayerStats playerStats;

    public event System.Action OnNewDay;
    public event System.Action<int> OnDayChanged;
    public event System.Action OnDaySleep;

    private Coroutine dayTimerCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentDay = PlayerPrefs.GetInt("Farm_CurrentDay", currentDay);
    }

    private void Start()
    {
        if (dayDuration > 0f)
        {
            dayTimerCoroutine = StartCoroutine(DayTimer());
        }
    }

    private IEnumerator DayTimer()
    {
        while (true)
        {
            yield return new WaitForSeconds(dayDuration);

            // Chỉ tự động qua ngày khi người chơi đang ở Base (không trong Dungeon Run)
            if (ProgressionManager.Instance == null || !ProgressionManager.Instance.runActive)
            {
                AdvanceDay();
            }
        }
    }

    /// <summary>
    /// Called when the player interacts with the bed to sleep and advance the day.
    /// </summary>
    public void Sleep()
    {
        Debug.Log($"[DayManager] Player is sleeping... Advancing from Day {currentDay}.");
        OnDaySleep?.Invoke();
        AdvanceDay();
    }

    public void AdvanceDay()
    {
        currentDay++;
        PlayerPrefs.SetInt("Farm_CurrentDay", currentDay);
        PlayerPrefs.Save();

        // Notify all subscribers (FarmPlots listen to OnNewDay)
        OnNewDay?.Invoke();

        // Reduce player hunger slightly each day
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        if (playerStats != null)
        {
            playerStats.ReduceHunger(5);
        }

        // Reset temporary run blessings when day advances
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.ClearRunBlessing();
        }

        Debug.Log($"[DayManager] Day {currentDay} has begun!");
        OnDayChanged?.Invoke(currentDay);
    }

    public int GetCurrentDay() => currentDay;
}
