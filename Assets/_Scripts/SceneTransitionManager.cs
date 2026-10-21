using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager _instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SceneTransitionManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public void LoadDungeon(int floor = -1)
    {
        if (ProgressionManager.Instance != null)
        {
            int targetFloor = floor > 0 ? floor : ProgressionManager.Instance.currentFloor;
            if (targetFloor <= 0) targetFloor = 1;
            ProgressionManager.Instance.StartRun(targetFloor);
        }
        SceneManager.LoadScene("Dungeon");
    }

    public void LoadBase()
    {
        // When player leaves Dungeon for Base, all buffs (turn, combat, floor) are cleared:
        PlayerStats.Instance?.ClearTemporaryBuffs();

        // If exiting to Base while expedition is active (day hasn't advanced), automatically advance 1 day:
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.runActive)
        {
            ProgressionManager.Instance.runActive = false;
            ProgressionManager.Instance.ClearRunBlessing();
            DungeonSummaryUI.AdvanceDayOnReturnStatic();
        }

        SceneManager.LoadScene("Base");
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        SceneManager.LoadScene(sceneName);
    }
}