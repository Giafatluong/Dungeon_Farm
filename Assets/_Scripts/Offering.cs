using UnityEngine;

[CreateAssetMenu(fileName = "New Offering", menuName = "Statue/Offering Data")]
public class Offering : ScriptableObject
{
    public string offeringKey;
    public string offeringName;
    public ItemRequirement[] requiredItems;
    public enum RewardStat
    {
        ATK,
        DEF,
        Speed
    }
    public RewardStat rewardStat;
    public int rewardAmount = 1;
    public bool completed;
}
