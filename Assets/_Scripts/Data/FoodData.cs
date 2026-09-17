using UnityEngine;

[CreateAssetMenu(fileName = "New Food Data", menuName = "Food Data")]
public class FoodData : ItemData
{
    public int hungerValue;
    public int healthValue;

    public Foodbuff[] foodBuff;

    public enum BuffDurationType
    {
        Turn,
        Combat,
        Floor
    }
}

[System.Serializable]
public class Foodbuff
{
    public StatEffectData buffs;
    public FoodData.BuffDurationType buffDurationType;
    public int buffDuration;
    public int buffValue;
}