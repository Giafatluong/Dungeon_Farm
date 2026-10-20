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
    }

    [Header("Day Settings")]
    public int currentDay = 1;

    [Header("References")]
    public PlayerStats playerStats;

    public event System.Action OnNewDay;
    public event System.Action<int> OnDayChanged;
    public event System.Action OnDaySleep;

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

    /// <summary>
    /// Called when the player interacts with the bed to sleep and advance the day.
    /// Triggers the Morning Report & Day Transition UI sequence.
    /// </summary>
    public void Sleep()
    {
        Debug.Log($"[DayManager] Player is sleeping... Advancing from Day {currentDay}.");
        OnDaySleep?.Invoke();

        DayTransitionUI transitionUI = DayTransitionUI.Instance != null ? DayTransitionUI.Instance : FindFirstObjectByType<DayTransitionUI>();
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

        // Safe multicast invocation: an exception in one listener will NEVER crash or block others
        if (OnNewDay != null)
        {
            var invocationList = OnNewDay.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
            {
                var d = invocationList[i];
                try
                {
                    if (d.Target is UnityEngine.Object uObj && uObj == null)
                    {
                        OnNewDay -= (System.Action)d;
                        continue;
                    }
                    ((System.Action)d).Invoke();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[DayManager] Handled exception in OnNewDay listener: {ex.Message}");
                }
            }
        }

        // Direct notification to all active FarmPlots in the current scene to guarantee 100% reliable day advancement
        FarmPlot[] plots = FindObjectsByType<FarmPlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < plots.Length; i++)
        {
            if (plots[i] != null)
            {
                plots[i].NextDay();
            }
        }

        // Flush all saved plot and day data in one batch
        PlayerPrefs.Save();

        // Ở nhà các chỉ số không tự động giảm (Hunger/HP giữ nguyên theo yêu cầu thiết kế)

        // Reset temporary run blessings and food buffs when day advances
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.ClearRunBlessing();
        }
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ClearTemporaryBuffs();
            PlayerStats.Instance.Heal(PlayerStats.Instance.maxHealth);
            PlayerStats.Instance.currentAP = PlayerStats.Instance.maxAP;
        }

        Debug.Log($"[DayManager] Day {currentDay} has begun!");
        OnDayChanged?.Invoke(currentDay);
    }

    public int GetCurrentDay() => currentDay;
}
