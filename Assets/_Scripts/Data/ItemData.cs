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
    public bool isRare; // Rare seed / item (only drops from Bosses)

    [TextArea(2, 4)]
    public string description;

    public enum ItemRarity
    {
        Common, // Bình thường
        Rare,   // Hiếm (Nông sản T2/T3, Món nấu)
        Epic    // Cực hiếm / Bảo vật Boss
    }

    [Header("Rarity")]
    public ItemRarity rarity = ItemRarity.Common;

    public ItemRarity GetRarity()
    {
        if (rarity != ItemRarity.Common) return rarity;
        if (isRare) return ItemRarity.Epic;
        string n = !string.IsNullOrEmpty(itemName) ? itemName : name;
        if (n.Contains("Boss") || n.Contains("Vương Miện") || n.Contains("Lõi Slime") || n.Contains("Hắc Huyết") ||
            n.Contains("Crown") || n.Contains("Core") || n.Contains("Essence") || n.Contains("Royal") || n.Contains("Spicy tomato"))
            return ItemRarity.Epic;
        if (itemType == ItemType.Food || n.Contains("Salad") || n.Contains("Butter") || n.Contains("Spicy") ||
            n.Contains("Chilli") || n.Contains("Cucumber") || n.Contains("Cabbage") || n.Contains("Corn"))
            return ItemRarity.Rare;
        return ItemRarity.Common;
    }
}
