using UnityEngine;

public class DungeonEntranceUI : MonoBehaviour
{
    [SerializeField] private GameObject enterDungeonPanel;

    public void Open()
    {
        enterDungeonPanel.SetActive(true);
    }

    public void Close()
    {
        enterDungeonPanel.SetActive(false);
    }

    public void EnterDungeon()
    {
        Close();

        if (SceneTransitionManager.Instance == null)
        {
            Debug.Log("Scene Transition Manager is NULL");
            return;
        }

        SceneTransitionManager.Instance.LoadDungeon();
    }
}