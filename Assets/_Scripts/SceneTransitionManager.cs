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
        SceneManager.LoadScene("Base");
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        SceneManager.LoadScene(sceneName);
    }
}