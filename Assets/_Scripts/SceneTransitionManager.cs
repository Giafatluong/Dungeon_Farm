using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadDungeon()
    {
        if (ProgressionManager.Instance != null && !ProgressionManager.Instance.runActive)
        {
            ProgressionManager.Instance.StartRun(1);
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