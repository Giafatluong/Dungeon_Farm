using UnityEngine;

public class DungeonEntrance : MonoBehaviour
{
    [SerializeField] private DungeonEntranceUI dungeonEntranceUI;

    public static DungeonEntrance Instance { get; private set; }
    public bool playerInRange { get; private set; }
    private Collider2D col;
    private Transform playerTransform;
    private float nextPlayerSearchTime;

    public const string SpawnAtEntranceKey = "SpawnAtDungeonEntrance";

    public static void SetReturnToEntranceFlag()
    {
        PlayerPrefs.SetInt(SpawnAtEntranceKey, 1);
        PlayerPrefs.Save();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        col = GetComponent<Collider2D>();
        if (dungeonEntranceUI == null)
        {
            dungeonEntranceUI = FindFirstObjectByType<DungeonEntranceUI>(FindObjectsInactive.Include);
        }
        FindPlayer();

        if (PlayerPrefs.GetInt(SpawnAtEntranceKey, 0) == 1)
        {
            PlayerPrefs.SetInt(SpawnAtEntranceKey, 0);
            PlayerPrefs.Save();
            PositionPlayerAtEntrance();
        }
    }

    public void PositionPlayerAtEntrance()
    {
        if (playerTransform == null) FindPlayer();
        if (playerTransform != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(0f, -1.2f, 0f);
            playerTransform.position = spawnPos;

            Rigidbody2D rb = playerTransform.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.position = spawnPos;
                rb.linearVelocity = Vector2.zero;
            }

            if (Camera.main != null)
            {
                CameraController cam = Camera.main.GetComponent<CameraController>();
                Vector3 camOffset = cam != null ? cam.offset : new Vector3(0, 1.5f, -10f);
                Camera.main.transform.position = spawnPos + camOffset;
            }
        }
    }

    private void FindPlayer()
    {
        if (PlayerMovement.Instance != null)
        {
            playerTransform = PlayerMovement.Instance.transform;
            return;
        }
        if (PlayerStats.Instance != null)
        {
            playerTransform = PlayerStats.Instance.transform;
            return;
        }
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null && Time.time >= nextPlayerSearchTime)
        {
            nextPlayerSearchTime = Time.time + 0.5f;
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
        if (TutorialManager.Instance != null && !TutorialManager.Instance.IsInteractionAllowed(TutorialManager.TutorialInteraction.DungeonGate))
            return;

        if (UIEscapeManager.IsAnyModalUIOpen()) return;

        if (dungeonEntranceUI == null)
        {
            dungeonEntranceUI = FindFirstObjectByType<DungeonEntranceUI>(FindObjectsInactive.Include);
            if (dungeonEntranceUI == null)
            {
                Debug.LogWarning("[DungeonEntrance] Dungeon Entrance UI not found.");
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