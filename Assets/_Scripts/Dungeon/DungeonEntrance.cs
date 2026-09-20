using UnityEngine;

public class DungeonEntrance : MonoBehaviour
{
    [SerializeField] private DungeonEntranceUI dungeonEntranceUI;

    private bool playerInRange;

    private void Update()
    {
        if (!playerInRange)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            OpenDungeonUI();
        }
    }

    private void OpenDungeonUI()
    {
        if (dungeonEntranceUI == null)
        {
            Debug.Log("Dungeon Entrance UI is NULL");
            return;
        }

        dungeonEntranceUI.Open();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Nhấn E để vào Dungeon");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}