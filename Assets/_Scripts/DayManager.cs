using UnityEngine;
using System.Collections;

public class DayManager : MonoBehaviour
{
    private static DayManager _instance;
    private static bool isQuitting = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        isQuitting = false;
    }

    public static DayManager Instance
    {
        get
        {
            if (isQuitting) return _instance;

            if (_instance == null)
            {
                _instance = FindFirstObjectByType<DayManager>();
                if (_instance == null && !isQuitting && Application.isPlaying)
                {
                    GameObject go = new GameObject("DayManager_Runtime");
                    _instance = go.AddComponent<DayManager>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        if (dayTimerCoroutine != null)
        {
            StopCoroutine(dayTimerCoroutine);
            dayTimerCoroutine = null;
        }
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
                DayTransitionUI transitionUI = DayTransitionUI.Instance ?? FindFirstObjectByType<DayTransitionUI>();
                if (transitionUI == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("DayTransitionUI");
                    transitionUI = go.AddComponent<DayTransitionUI>();
                }

                if (transitionUI != null && !transitionUI.IsOpen)
                {
                    transitionUI.PlayTransition(currentDay, () => AdvanceDay());
                }
                else
                {
                    AdvanceDay();
                }
            }
        }
    }

    /// <summary>
    /// Called when the player interacts with the bed to sleep and advance the day.
    /// Triggers the Morning Report & Day Transition UI sequence.
    /// </summary>
    public void Sleep()
    {
        Debug.Log($"[DayManager] Player is sleeping... Advancing from Day {currentDay}.");
        OnDaySleep?.Invoke();

        DayTransitionUI transitionUI = DayTransitionUI.Instance ?? FindFirstObjectByType<DayTransitionUI>();
        if (transitionUI == null && Application.isPlaying)
        {
            GameObject go = new GameObject("DayTransitionUI");
            transitionUI = go.AddComponent<DayTransitionUI>();
        }

        if (transitionUI != null && !transitionUI.IsOpen)
        {
            transitionUI.PlayTransition(currentDay, () => AdvanceDay());
        }
        else
        {
            AdvanceDay();
        }
    }

    public void AdvanceDay()
    {
        currentDay++;
        PlayerPrefs.SetInt("Farm_CurrentDay", currentDay);
        PlayerPrefs.Save();

        // Notify all subscribers (FarmPlots listen to OnNewDay)
        OnNewDay?.Invoke();

        // Ở nhà các chỉ số không tự động giảm (Hunger/HP giữ nguyên theo yêu cầu thiết kế)

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
