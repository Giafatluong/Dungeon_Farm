using UnityEngine;

public class ItemPickupRange : MonoBehaviour
{
    private GameObject cachedTargetPlayer;

    private GameObject GetTargetPlayer()
    {
        if (cachedTargetPlayer == null)
        {
            cachedTargetPlayer = transform.parent != null ? transform.parent.gameObject : gameObject;
        }
        return cachedTargetPlayer;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out ItemDrop itemDrop) && itemDrop.CanStartMove)
        {
            itemDrop.StartMove(GetTargetPlayer());
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent(out ItemDrop itemDrop) && itemDrop.CanStartMove)
        {
            itemDrop.StartMove(GetTargetPlayer());
        }
    }
}