using UnityEngine;
using System.Collections.Generic;

public class DungeonEventManager : MonoBehaviour
{
    #region Singleton & Events
    public static DungeonEventManager Instance { get; private set; }

    [Header("Event Configuration")]
    [SerializeField] private List<DungeonEvent> customEvents = new();

    private readonly List<DungeonEvent> runtimeEvents = new();
    private int lastEventIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeDefaultEvents();
    }
    #endregion

    #region Event Templates Setup
    public void InitializeDefaultEvents()
    {
        runtimeEvents.Clear();

        if (customEvents != null && customEvents.Count > 0)
        {
            runtimeEvents.AddRange(customEvents);
            return;
        }

        // Dynamically find sample items in project for default event trades
        ItemData wheatSeed = null, carrotSeed = null, tomatoSeed = null, cornSeed = null;
        ItemData wheat = null, carrot = null, cabbage = null;
        FoodData tomatoSoup = null, freshSalad = null;

        ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
        for (int i = 0; i < allItems.Length; i++)
        {
            ItemData it = allItems[i];
            if (it == null) continue;
            string n = it.name.ToLower();
            if (n.Contains("wheatseed")) wheatSeed = it;
            else if (n.Contains("carrotseed")) carrotSeed = it;
            else if (n.Contains("tomatoseed")) tomatoSeed = it;
            else if (n.Contains("cornseed")) cornSeed = it;
            else if (n == "wheat") wheat = it;
            else if (n == "carrot") carrot = it;
            else if (n == "cabbage") cabbage = it;
            else if (it is FoodData fd)
            {
                if (n.Contains("soup")) tomatoSoup = fd;
                else if (n.Contains("salad")) freshSalad = fd;
            }
        }

        // 1. EVENT: The Wandering Merchant
        DungeonEvent merchantEvt = new()
        {
            eventID = "Event_Merchant",
            eventTitle = "The Wandering Merchant",
            eventCategory = "MERCHANT",
            narrativeStory = "A hooded merchant sits beside a glowing lantern amidst the dungeon rubble.\n\"Greetings, traveler. Deep in these halls, seeds and rations are worth more than gold. Shall we trade?\""
        };
        merchantEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Open Full Shop]",
            choiceDetails = "Browse rare recipes, exchange seeds, and buy travel rations",
            opensMerchantShop = true,
            outcomeNarrative = "The merchant unrolls a thick velvet cloth revealing rare cooking recipes and seeds."
        });
        merchantEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Barter Seeds]",
            choiceDetails = "Give 2x Wheat Seed -> Receive 1x Tomato Seed & 1x Corn Seed",
            requiredItem = wheatSeed,
            requiredItemAmount = 2,
            rewardItem = tomatoSeed != null ? tomatoSeed : carrotSeed,
            rewardItemAmount = 2,
            outcomeNarrative = "The merchant inspects your seeds with approval.\n\"A fair exchange! Cultivate these well on your farm surface.\""
        });
        merchantEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Buy Hot Rations]",
            choiceDetails = "Give 2x Harvested Crops -> Receive warm food & recover 25 HP",
            requiredItem = carrot != null ? carrot : wheat,
            requiredItemAmount = 2,
            rewardItem = freshSalad != null ? freshSalad : cabbage,
            rewardItemAmount = 1,
            hpRecovery = 25,
            hungerRecovery = 15,
            outcomeNarrative = "The merchant hands you a steaming meal wrapped in fresh parchment.\nYour vitality returns as you enjoy the warm food."
        });
        merchantEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Leave]",
            choiceDetails = "Politely decline and continue forward",
            isLeaveChoice = true,
            outcomeNarrative = "\"May fortune smile upon your blade, traveler.\" The merchant nods as you step away."
        });
        runtimeEvents.Add(merchantEvt);

        // 2. EVENT: The Ancient Altar (Shrine)
        DungeonEvent shrineEvt = new()
        {
            eventID = "Event_Shrine",
            eventTitle = "The Ancient Altar",
            eventCategory = "SHRINE",
            narrativeStory = "Carved from black obsidian, an ancient altar hums with forgotten runic magic.\nAn inscription reads: 'Give unto the stone, and the dungeon spirits shall heed your call.'"
        };
        shrineEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Blood Offering]",
            choiceDetails = "Sacrifice 12 HP -> Receive Ancient Blessing (+3 ATK, +2 DEF)",
            hpCost = 12,
            atkBuff = 3,
            defBuff = 2,
            buffDuration = 5,
            outcomeNarrative = "Crimson droplets touch the runes. A surge of spectral power courses into your veins!\nYour attacks and defense are empowered for the next 5 turns!"
        });
        shrineEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Pious Prayer]",
            choiceDetails = "Spend 10 Fullness in quiet meditation -> Restore 35 HP",
            hungerCost = 10,
            hpRecovery = 35,
            outcomeNarrative = "You kneel in reverent stillness. A soothing warmth washes over your wounds, sealing your cuts."
        });
        shrineEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Leave]",
            choiceDetails = "Bow respectfully and step past the altar",
            isLeaveChoice = true,
            outcomeNarrative = "You decide not to tamper with ancient relics and continue your expedition."
        });
        runtimeEvents.Add(shrineEvt);

        // 3. EVENT: Fountain of Life
        DungeonEvent fountainEvt = new()
        {
            eventID = "Event_Fountain",
            eventTitle = "Fountain of Life",
            eventCategory = "SANCTUARY",
            narrativeStory = "Hidden behind ivy-covered pillars, a tranquil spring bubbles with luminescent azure water.\nThe air smells sweet and pure, untouched by the dungeon's corruption."
        };
        fountainEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Drink Deeply]",
            choiceDetails = "Drink the pure spring water -> Restore 45 HP and 30 Fullness",
            hpRecovery = 45,
            hungerRecovery = 30,
            outcomeNarrative = "The refreshing water invigorates your body and clears your mind!\nYou feel fully revitalized and ready for battle."
        });
        fountainEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Bathe Weapon]",
            choiceDetails = "Submerge your weapon in the holy waters -> +4 ATK Buff",
            atkBuff = 4,
            buffDuration = 6,
            outcomeNarrative = "The blade gleams with a luminous edge! Your strikes will hit with tremendous force."
        });
        fountainEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Leave]",
            choiceDetails = "Take a breath and continue journey",
            isLeaveChoice = true,
            outcomeNarrative = "You take a quiet breath in the peaceful alcove before venturing deeper into the darkness."
        });
        runtimeEvents.Add(fountainEvt);

        // 4. EVENT: The Cursed Relic (Gambler's Mystery)
        DungeonEvent relicEvt = new()
        {
            eventID = "Event_Relic",
            eventTitle = "The Cursed Relic",
            eventCategory = "MYSTERY",
            narrativeStory = "You come across an ornate jeweled chest pulsing with a sinister purple glow.\nA latch holds the lid closed, but dark vapor seeps through the cracks."
        };
        relicEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Pry Open Chest]",
            choiceDetails = "70% Chance: Gain valuable supplies / 30% Chance: Trap explodes (-15 HP)",
            gambleSuccessChance = 0.70f,
            rewardItem = tomatoSoup != null ? tomatoSoup : freshSalad,
            rewardItemAmount = 2,
            failureDamage = 15,
            failureNarrative = "TRAP TRIGGERED! The chest snaps open, spraying noxious poison gas! You suffer 15 damage!",
            outcomeNarrative = "SUCCESS! You disarm the mechanism just in time! Inside, you find valuable preserved supplies."
        });
        relicEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Exorcise with Care]",
            choiceDetails = "Carefully neutralize runes using 12 Fullness -> Safely receive 1x Crop",
            hungerCost = 12,
            rewardItem = carrot != null ? carrot : wheat,
            rewardItemAmount = 2,
            outcomeNarrative = "With patient focus, you dismantle the curse safely and retrieve the contents inside."
        });
        relicEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Avoid Relic]",
            choiceDetails = "Too risky, walk away",
            isLeaveChoice = true,
            outcomeNarrative = "Wisdom prevails. You leave the suspicious chest undisturbed."
        });
        runtimeEvents.Add(relicEvt);

        // 5. EVENT: The Lost Forager (NPC Encounter)
        DungeonEvent foragerEvt = new()
        {
            eventID = "Event_Forager",
            eventTitle = "The Lost Forager",
            eventCategory = "ENCOUNTER",
            narrativeStory = "A weary adventurer sits huddled against the stone wall, clutching an empty knapsack.\n\"Please... I lost my supplies to goblins on the upper floor. Do you have any spare food?\""
        };
        foragerEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Share Food]",
            choiceDetails = "Give 1x Crop to the forager -> In return, he gives his secret seed pouch",
            requiredItem = carrot != null ? carrot : (wheat != null ? wheat : cabbage),
            requiredItemAmount = 1,
            rewardItem = cornSeed != null ? cornSeed : wheatSeed,
            rewardItemAmount = 3,
            outcomeNarrative = "Tears fill the adventurer's eyes. \"Thank you, friend! Take this seed pouch I saved—it will serve you better!\""
        });
        foragerEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Provide Directions]",
            choiceDetails = "Point him toward the Campfire sanctuary -> Gain +2 Speed from goodwill",
            speedBuff = 2,
            buffDuration = 5,
            outcomeNarrative = "\"A camp nearby? Bless you!\" The adventurer picks up his gear and hurries toward safety."
        });
        foragerEvt.choices.Add(new DungeonEventChoice
        {
            choiceLabel = "[Keep Walking]",
            choiceDetails = "Ignore him and move on",
            isLeaveChoice = true,
            outcomeNarrative = "You avert your eyes and press forward into the dungeon."
        });
        runtimeEvents.Add(foragerEvt);
    }
    #endregion

    #region Event Resolution
    public DungeonEvent GetRandomEvent()
    {
        if (runtimeEvents == null || runtimeEvents.Count == 0)
        {
            InitializeDefaultEvents();
        }

        if (runtimeEvents == null || runtimeEvents.Count == 0) return null;

        int pick = Random.Range(0, runtimeEvents.Count);
        if (pick == lastEventIndex && runtimeEvents.Count > 1)
        {
            pick = (pick + 1) % runtimeEvents.Count;
        }

        lastEventIndex = pick;
        return runtimeEvents[pick];
    }

    public bool ExecuteChoice(
        DungeonEventChoice choice,
        PlayerStats player,
        ItemContainer backpack,
        out string resultNarrative,
        out bool isSuccess)
    {
        resultNarrative = choice.outcomeNarrative;
        isSuccess = true;

        if (choice.isLeaveChoice)
        {
            return true;
        }

        // Deduct costs
        if (choice.hpCost > 0 && player != null)
        {
            player.TakeDamage(choice.hpCost);
        }

        if (choice.hungerCost > 0 && player != null)
        {
            player.currentHunger = Mathf.Max(0, player.currentHunger - choice.hungerCost);
        }

        if (choice.requiredItem != null && choice.requiredItemAmount > 0 && backpack != null)
        {
            backpack.RemoveItem(choice.requiredItem, choice.requiredItemAmount);
        }

        // Handle Gamble / Risk
        if (choice.gambleSuccessChance > 0f)
        {
            float roll = Random.value;
            if (roll > choice.gambleSuccessChance)
            {
                // Failed gamble
                isSuccess = false;
                if (choice.failureDamage > 0 && player != null)
                {
                    player.TakeDamage(choice.failureDamage);
                }
                resultNarrative = choice.failureNarrative;
                return true;
            }
        }

        // Grant Recoveries
        if (choice.hpRecovery > 0 && player != null)
        {
            player.currentHealth = Mathf.Min(player.maxHealth, player.currentHealth + choice.hpRecovery);
        }

        if (choice.hungerRecovery > 0 && player != null)
        {
            player.currentHunger = Mathf.Min(player.maxHunger, player.currentHunger + choice.hungerRecovery);
        }

        // Grant Buffs
        if (player != null)
        {
            StatEffectData atkEffect = null, defEffect = null, spdEffect = null;
            StatEffectData[] allEffects = Resources.FindObjectsOfTypeAll<StatEffectData>();
            for (int i = 0; i < allEffects.Length; i++)
            {
                if (allEffects[i] == null) continue;
                string n = allEffects[i].name.ToLower();
                if (n.Contains("increaseatk")) atkEffect = allEffects[i];
                else if (n.Contains("increasedef")) defEffect = allEffects[i];
                else if (n.Contains("increasespeed")) spdEffect = allEffects[i];
            }

            if (choice.atkBuff > 0 && atkEffect != null)
            {
                player.AddBuff(atkEffect, choice.atkBuff, choice.buffDuration, FoodData.BuffDurationType.Turn);
            }
            if (choice.defBuff > 0 && defEffect != null)
            {
                player.AddBuff(defEffect, choice.defBuff, choice.buffDuration, FoodData.BuffDurationType.Turn);
            }
            if (choice.speedBuff > 0 && spdEffect != null)
            {
                player.AddBuff(spdEffect, choice.speedBuff, choice.buffDuration, FoodData.BuffDurationType.Turn);
            }
        }

        // Grant Reward Item
        if (choice.rewardItem != null && choice.rewardItemAmount > 0)
        {
            if (backpack == null)
            {
                Debug.LogWarning("[DungeonEventManager] Backpack is null — reward item could not be granted.");
                resultNarrative += "\n<color=#FF9900>(Reward lost: no backpack found!)</color>";
            }
            else if (!backpack.CanAddItem(choice.rewardItem))
            {
                Debug.LogWarning($"[DungeonEventManager] Backpack is full — '{choice.rewardItem.itemName}' could not be added.");
                resultNarrative += $"\n<color=#FF9900>(Inventory full! Could not receive {choice.rewardItem.itemName} x{choice.rewardItemAmount}.)</color>";
            }
            else
            {
                backpack.AddItem(choice.rewardItem, choice.rewardItemAmount);
                if (choice.rewardItem.name.ToLower().Contains("seed") && ProgressionManager.Instance != null)
                {
                    ProgressionManager.Instance.UnlockSeed(choice.rewardItem);
                }
                Debug.Log($"[DungeonEventManager] Granted reward: {choice.rewardItem.itemName} x{choice.rewardItemAmount}");
            }
        }

        return true;
    }
    #endregion
}
