using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;
    public Vector3 offset = new Vector3(0, 1.5f, -10f);

    private void Awake()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = new Vector3(0, 1, 0);
        }

        if (Mathf.Approximately(offset.z, 0f))
        {
            offset.z = -10f;
        }
    }

    private void Start()
    {
        if (player == null)
        {
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) player = pm.gameObject;
        }
    }

    private float nextSearchTime = 0f;

    private void FixedUpdate()
    {
        if (player == null)
        {
            if (Time.time >= nextSearchTime)
            {
                nextSearchTime = Time.time + 1.0f;
                PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
                if (pm != null) player = pm.gameObject;
            }
        }

        if (player != null)
        {
            float targetZ = !Mathf.Approximately(offset.z, 0f) ? offset.z : -10f;
            transform.position = new Vector3(player.transform.position.x + offset.x, player.transform.position.y + offset.y, targetZ);
        }
    }
}
