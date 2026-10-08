using UnityEngine;

public class ItemDropSpawner : MonoBehaviour
{
    public GameObject player;
    public ItemContainer itemContainer;

    private void Start()
    {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                PlayerStats ps = FindFirstObjectByType<PlayerStats>();
                if (ps != null) player = ps.gameObject;
            }
        }

        if (itemContainer == null)
        {
            if (player != null)
            {
                PlayerStats ps = player.GetComponent<PlayerStats>();
                if (ps != null) itemContainer = ps.itemContainer;
            }
            if (itemContainer == null)
            {
                PlayerStats ps = FindFirstObjectByType<PlayerStats>();
                if (ps != null) itemContainer = ps.itemContainer;
            }
        }
    }

    public void SpawnItem(ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0) return;
        EnsureReferences();

        if (itemData.dropPrefab == null)
        {
            if (itemContainer != null)
            {
                itemContainer.AddItem(itemData, amount);
            }
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            GameObject dropObject = Instantiate(
                itemData.dropPrefab,
                transform.position,
                Quaternion.identity
            );

            ItemDrop itemDrop = dropObject.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                itemDrop.itemData = itemData;
                itemDrop.amount = 1;
                itemDrop.player = player;
                itemDrop.itemContainer = itemContainer;
                itemDrop.PlayDropEffect();
            }
            else if (itemContainer != null)
            {
                itemContainer.AddItem(itemData, 1);
            }
        }
    }
}