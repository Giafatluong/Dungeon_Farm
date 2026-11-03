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
                if (_instance == null)
                {
                    GameObject go = new GameObject("SceneTransitionManager");
                    _instance = go.AddComponent<SceneTransitionManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    public static bool SpawnAtDungeonEntranceOnBaseLoad = false;
    private string previousSceneName;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        SpawnAtDungeonEntranceOnBaseLoad = false;
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
        previousSceneName = SceneManager.GetActiveScene().name;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;

        // Clean up UI caches and ensure no stale modal windows or tooltips persist across scenes
        UIEscapeManager.ResetSceneCache();
        UIEscapeManager.CloseAllModals(includeInventory: true);
        ItemTooltipUI.Instance?.Hide(immediate: true);

        if (DayTransitionUI.Instance != null)
        {
            DayTransitionUI.Instance.ResetToHiddenState();
        }

        if (scene.name == "Base")
        {
            // Fail-safe: If returning to Base while run was still marked active (e.g. abrupt exit/reload), clean it up
            if (ProgressionManager.Instance != null && ProgressionManager.Instance.runActive)
            {
                ProgressionManager.Instance.runActive = false;
                ProgressionManager.Instance.ClearRunBlessing();
                DungeonSummaryUI.AdvanceDayOnReturnStatic();
            }

            bool cameFromDungeon = SpawnAtDungeonEntranceOnBaseLoad 
                                || (previousSceneName != null && previousSceneName.Equals("Dungeon", System.StringComparison.OrdinalIgnoreCase));

            if (cameFromDungeon)
            {
                SpawnAtDungeonEntranceOnBaseLoad = false;
                PositionPlayerAtDungeonEntrance();
                StartCoroutine(EnsurePlayerAtDungeonEntranceRoutine());
            }
        }

        previousSceneName = scene.name;
    }

    /// <summary>
    /// Positions the player and camera right outside the Dungeon Entrance portal facing towards the farm.
    /// </summary>
    public static void PositionPlayerAtDungeonEntrance()
    {
        DungeonEntrance entrance = DungeonEntrance.Instance != null ? DungeonEntrance.Instance : Object.FindFirstObjectByType<DungeonEntrance>();
        Vector3 spawnPos = entrance != null ? (entrance.transform.position + new Vector3(-1.6f, 0f, 0f)) : new Vector3(38.9f, -6.3f, 0f);

        GameObject player = null;
        if (PlayerMovement.Instance != null) player = PlayerMovement.Instance.gameObject;
        if (player == null && PlayerStats.Instance != null) player = PlayerStats.Instance.gameObject;
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            player.transform.position = spawnPos;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = spawnPos;
                rb.linearVelocity = Vector2.zero;
            }

            Animator anim = player.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetFloat("LastHorizontal", -1f);
                anim.SetFloat("LastVertical", 0f);
            }
        }

        // Instantly position Camera at the dungeon entrance so there is no visual jump from the house
        CameraController cam = Object.FindFirstObjectByType<CameraController>();
        if (cam != null)
        {
            float targetZ = !Mathf.Approximately(cam.offset.z, 0f) ? cam.offset.z : -10f;
            cam.transform.position = new Vector3(spawnPos.x + cam.offset.x, spawnPos.y + cam.offset.y, targetZ);
        }
    }

    private System.Collections.IEnumerator EnsurePlayerAtDungeonEntranceRoutine()
    {
        yield return null;
        PositionPlayerAtDungeonEntrance();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public void LoadDungeon(int floor = -1)
    {
        Time.timeScale = 1f;
        DayTransitionUI.Instance?.ResetToHiddenState();

        FarmPlot.SaveAllPlots();
        PlayerPrefs.Save();

        if (ProgressionManager.Instance != null)
        {
            int targetFloor = floor >= 0 ? floor : ProgressionManager.Instance.currentFloor;
            ProgressionManager.Instance.StartRun(targetFloor);
        }
        SceneManager.LoadScene("Dungeon");
    }

    public void LoadBase()
    {
        Time.timeScale = 1f;
        DayTransitionUI.Instance?.ResetToHiddenState();

        // Mark that next Base scene load should spawn player at Dungeon Entrance
        SpawnAtDungeonEntranceOnBaseLoad = true;

        // When player leaves Dungeon for Base, all buffs (turn, combat, floor) are cleared:
        PlayerStats.Instance?.ClearTemporaryBuffs();

        // If exiting to Base while expedition is active (day hasn't advanced), automatically advance 1 day:
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.runActive)
        {
            ProgressionManager.Instance.runActive = false;
            ProgressionManager.Instance.ClearRunBlessing();
            DungeonSummaryUI.AdvanceDayOnReturnStatic();
        }

        PlayerPrefs.Save();
        SceneManager.LoadScene("Base");
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        Time.timeScale = 1f;
        DayTransitionUI.Instance?.ResetToHiddenState();

        if (SceneManager.GetActiveScene().name == "Base")
        {
            FarmPlot.SaveAllPlots();
        }
        PlayerPrefs.Save();
        SceneManager.LoadScene(sceneName);
    }
}