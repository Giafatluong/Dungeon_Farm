using UnityEngine;

[CreateAssetMenu(fileName = "New Offering", menuName = "Statue/Offering Data")]
public class Offering : ScriptableObject
{
    public string offeringKey;
    public string offeringName;

    [Header("Milestone & Category")]
    public string categoryName = "Chiến Binh";
    public int tier = 1; // 1 = Mốc I, 2 = Mốc II, 3 = Mốc III
    public string prerequisiteOfferingKey = "";

    public ItemRequirement[] requiredItems;

    public enum RewardStat
    {
        ATK = 0,
        DEF = 1,
        Speed = 2,
        MaxHP = 3
    }

    public RewardStat rewardStat;
    public int rewardAmount = 1;
    public bool completed;

    public bool IsUnlocked()
    {
        if (string.IsNullOrEmpty(prerequisiteOfferingKey)) return true;
        if (ProgressionManager.Instance == null) return true;
        return ProgressionManager.Instance.IsOfferingCompleted(prerequisiteOfferingKey);
    }
}
