using UnityEngine;

public class DungeonEntrance : MonoBehaviour
{
    [SerializeField] private DungeonEntranceUI dungeonEntranceUI;

    private bool playerInRange;
    private Collider2D col;
    private Transform playerTransform;

    private void Start()
    {
        col = GetComponent<Collider2D>();
        if (dungeonEntranceUI == null)
        {
            dungeonEntranceUI = FindFirstObjectByType<DungeonEntranceUI>(FindObjectsInactive.Include);
        }
        FindPlayer();
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
        }

        if (playerTransform != null)
        {
            Vector3 center = col != null ? col.bounds.center : transform.position;
            float dist = Vector2.Distance(center, playerTransform.position);
            playerInRange = dist <= 2.2f;
        }

        if (!playerInRange)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            OpenDungeonUI();
        }
    }

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        if (playerInRange)
        {
            OpenDungeonUI();
        }
    }

    private void OpenDungeonUI()
    {
        if (KitchenUI.Instance != null && KitchenUI.Instance.IsOpen) return;
        if (ChestUI.Instance != null && ChestUI.Instance.IsOpen) return;
        if (StatueUI.Instance != null && StatueUI.Instance.IsOpen) return;

        if (dungeonEntranceUI == null)
        {
            dungeonEntranceUI = FindFirstObjectByType<DungeonEntranceUI>(FindObjectsInactive.Include);
            if (dungeonEntranceUI == null)
            {
                Debug.Log("Dungeon Entrance UI is NULL");
                return;
            }
        }

        dungeonEntranceUI.Open();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Press E to enter Dungeon");
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