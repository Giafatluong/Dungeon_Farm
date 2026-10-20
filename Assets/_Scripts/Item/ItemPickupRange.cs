using UnityEngine;

public class ItemPickupRange : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        ItemDrop itemDrop = other.GetComponent<ItemDrop>();

        if (itemDrop != null)
        {
            GameObject targetPlayer = transform.parent != null ? transform.parent.gameObject : gameObject;
            itemDrop.StartMove(targetPlayer);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        ItemDrop itemDrop = other.GetComponent<ItemDrop>();

        if (itemDrop != null && !itemDrop.IsMoving && itemDrop.canCollect)
        {
            GameObject targetPlayer = transform.parent != null ? transform.parent.gameObject : gameObject;
            itemDrop.StartMove(targetPlayer);
        }
    }
}