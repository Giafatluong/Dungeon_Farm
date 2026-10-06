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

    [Tooltip("Explicitly set whether this effect is a debuff. If left false, name heuristics are used as fallback.")]
    public bool isDebuff = false;

    public bool IsDebuff => isDebuff
                         || (buffName != null && (buffName.IndexOf("Giam", System.StringComparison.OrdinalIgnoreCase) >= 0 || buffName.IndexOf("Decrease", System.StringComparison.OrdinalIgnoreCase) >= 0 || buffName.IndexOf("Debuff", System.StringComparison.OrdinalIgnoreCase) >= 0))
                         || (name != null && (name.StartsWith("Decrease", System.StringComparison.OrdinalIgnoreCase) || name.IndexOf("Debuff", System.StringComparison.OrdinalIgnoreCase) >= 0));
}

[System.Serializable]
public class ActiveBuff
{
    public StatEffectData buff;
    public int buffValue;
    public int remainingDuration;
    public FoodData.BuffDurationType buffDurationType;
}