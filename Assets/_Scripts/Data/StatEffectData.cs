using UnityEngine;

[CreateAssetMenu(fileName = "New Stat Effect", menuName = "Buff Data")]
public class StatEffectData : ScriptableObject
{
    public string buffName;
    public enum BuffType
    {
        Attack,
        Defense,
        Speed,
        Health
    }

    public BuffType buffType;

    public enum Stackable
    {
        Yes,
        No
    }

    public Stackable stackable;
}

[System.Serializable]
public class ActiveBuff
{
    public StatEffectData buff;
    public int buffValue;
    public int remainingDuration;
    public FoodData.BuffDurationType buffDurationType;
}