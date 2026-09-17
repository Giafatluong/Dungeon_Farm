using UnityEngine;

public class ItemPickupRange : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        ItemDrop itemDrop = other.GetComponent<ItemDrop>();

        if (itemDrop != null)
        {
            itemDrop.StartMove(transform.parent.gameObject);
        }
    }
}