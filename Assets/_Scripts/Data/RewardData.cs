using UnityEngine;

[System.Serializable]
public class RewardItemEntry
{
    public ItemData item;
    public int minAmount = 1;
    public int maxAmount = 3;
    [Range(0f, 1f)] public float dropChance = 1f;
}
