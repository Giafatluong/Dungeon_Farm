using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player;
    public Vector3 offset = new Vector3(0,1.5f, 0);

    private void FixedUpdate()
    {
        transform.position = player.transform.position + offset;
    }
}
