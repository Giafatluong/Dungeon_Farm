using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;
    public Vector3 offset = new Vector3(0, 1.5f, -10f);
    public bool isCinematicOverride = false;

    private Transform playerTransform;
    private Transform camTransform;
    private float cachedTargetZ = -10f;
    private float nextSearchTime = 0f;

    private void Awake()
    {
        camTransform = transform;
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
        cachedTargetZ = offset.z;
    }

    private void Start()
    {
        ResolvePlayer();
    }

    private void ResolvePlayer()
    {
        if (player == null)
        {
            PlayerMovement pm = PlayerMovement.Instance != null ? PlayerMovement.Instance : FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                player = pm.gameObject;
                playerTransform = pm.transform;
            }
        }
        else if (playerTransform == null)
        {
            playerTransform = player.transform;
        }
    }

    private void FixedUpdate()
    {
        if (isCinematicOverride) return;

        if (playerTransform == null)
        {
            if (PlayerMovement.Instance != null)
            {
                player = PlayerMovement.Instance.gameObject;
                playerTransform = PlayerMovement.Instance.transform;
            }
            else if (Time.time >= nextSearchTime)
            {
                nextSearchTime = Time.time + 1.0f;
                ResolvePlayer();
            }
        }

        if (playerTransform != null)
        {
            Vector3 pPos = playerTransform.position;
            camTransform.position = new Vector3(pPos.x + offset.x, pPos.y + offset.y, cachedTargetZ);
        }
    }
}
