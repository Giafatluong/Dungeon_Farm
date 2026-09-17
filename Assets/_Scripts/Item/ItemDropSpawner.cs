using UnityEngine;

public class ItemDropSpawner : MonoBehaviour
{
    public GameObject player;
    public ItemContainer itemContainer;

    public void SpawnItem(ItemData itemData, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject dropObject = Instantiate(
                itemData.dropPrefab,
                transform.position,
                Quaternion.identity
            );

            ItemDrop itemDrop = dropObject.GetComponent<ItemDrop>();

            itemDrop.itemData = itemData;
            itemDrop.amount = 1;
            itemDrop.player = player;
            itemDrop.itemContainer = itemContainer;

            itemDrop.PlayDropEffect();
        }
    }
}