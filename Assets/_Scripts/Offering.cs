using UnityEngine;
using System.Collections.Generic;

public class Offering : MonoBehaviour
{
    public ID offeringID;
    public ItemRequirement[] requiredItems;
    public enum RewardStat
    {
        ATK,
        DEF,
        Speed
    }
    public int rewardAmount;
    public bool completed;
}
