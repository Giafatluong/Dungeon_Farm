using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;
    public Vector3 offset = new Vector3(0,1.5f, 0);

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
            transform.position = player.transform.position + offset;
        }
    }
}
