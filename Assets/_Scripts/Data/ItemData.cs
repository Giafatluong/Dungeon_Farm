using UnityEngine;

[CreateAssetMenu(fileName = "New Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    public GameObject dropPrefab;
    public enum ItemType
    {
        Seed,
        Produce,
        Food,
        Other
    }
    public ItemType itemType;

    public bool isStackable;
}
