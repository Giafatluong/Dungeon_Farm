using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DungeonEventChoice
{
    [Header("Choice Presentation")]
    [Tooltip("Label displayed on button, e.g. '[Trade] Barter Crops'")]
    public string choiceLabel = "[Choice]";
    [Tooltip("Details of cost and gain, e.g. 'Give 2x Carrot -> Receive 1x Spicy Corn & 10 HP'")]
    public string choiceDetails = "";

    [Header("Requirements / Costs")]
    public ItemData requiredItem;
    public int requiredItemAmount = 0;
    public int hpCost = 0;
    public int hungerCost = 0;

    [Header("Guaranteed Rewards")]
    public ItemData rewardItem;
    public int rewardItemAmount = 0;
    public int hpRecovery = 0;
    public int hungerRecovery = 0;
    public int atkBuff = 0;
    public int defBuff = 0;
    public int speedBuff = 0;
    public int buffDuration = 4;

    [Header("Blessing Rewards (Phước Lành Thần Linh)")]
    public BlessingType grantedBlessing = BlessingType.None;
    public bool grantRandomBlessing = false;

    [Header("Gamble / Risk Option")]
    [Tooltip("0 = no gamble (100% guaranteed). 0.7 = 70% success chance.")]
    [Range(0f, 1f)] public float gambleSuccessChance = 0f;
    public int failureDamage = 0;
    [TextArea(2, 3)]
    public string failureNarrative = "";

    [Header("Outcome Feedback")]
    [TextArea(2, 4)]
    public string outcomeNarrative = "You completed the encounter.";
    public bool isLeaveChoice = false;
    public bool opensMerchantShop = false;

    public bool CanPlayerSelect(PlayerStats player, ItemContainer backpack, out string reason)
    {
        reason = string.Empty;

        if (player == null) return false;

        // Check HP cost
        if (hpCost > 0 && player.currentHealth <= hpCost)
        {
            reason = $"Requires > {hpCost} HP (Too dangerous)";
            return false;
        }

        // Check Hunger cost
        if (hungerCost > 0 && player.currentHunger < hungerCost)
        {
            reason = $"Requires {hungerCost} Fullness (You are too hungry)";
            return false;
        }

        // Check Item requirement
        if (requiredItem != null && requiredItemAmount > 0)
        {
            int currentCount = GetItemCount(backpack, requiredItem);
            if (currentCount < requiredItemAmount)
            {
                reason = $"Missing {requiredItem.itemName} ({currentCount}/{requiredItemAmount})";
                return false;
            }
        }

        return true;
    }

    private int GetItemCount(ItemContainer container, ItemData item)
    {
        if (container == null || item == null) return 0;
        int count = 0;
        for (int i = 0; i < container.itemSlots.Length; i++)
        {
            if (container.itemSlots[i] != null && container.itemSlots[i].itemData == item)
            {
                count += container.itemSlots[i].amount;
            }
        }
        return count;
    }
}

[System.Serializable]
public class DungeonEvent
{
    public string eventID = "Event";
    public string eventTitle = "Unknown Encounter";
    public string eventCategory = "EVENT"; // MERCHANT, SHRINE, FOUNTAIN, MYSTERY, NPC
    [TextArea(3, 6)]
    public string narrativeStory = "";
    public List<DungeonEventChoice> choices = new();
}
