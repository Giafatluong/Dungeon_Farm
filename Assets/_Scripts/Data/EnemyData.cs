using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;

    public int maxHealth;
    public int ATK;
    public int DEF;
    public int speed;

    public enum EnemyIntent
    {
        Attack,
        Defend,
        Buff,
        Debuff
    }

    public enum EffectTarget
    {
        Self,
        Ally,
        Player
    }

    [System.Serializable]
    public class IntentEffect
    {
        public StatEffectData effect;
        public EffectTarget target;
        public int value;
        public int duration;
        public FoodData.BuffDurationType durationType;
    }

    public EnemyIntent[] possibleIntents;
    public IntentEffect[] intentEffects;

    [System.Serializable]
    public class DropItemEntry
    {
        public ItemData item;
        [Range(0f, 1f)]
        public float dropChance = 0.5f;
        public int minAmount = 1;
        public int maxAmount = 1;
        [Tooltip("If checked, the player must have unlocked this item via Meta Progression for it to drop")]
        public bool requireUnlock = false;
    }

    [Header("Loot Configuration")]
    [Tooltip("List of item drops, quantities, drop chances, and unlock requirements")]
    public DropItemEntry[] lootDrops;

    [Tooltip("Allow dropping unlocked seeds randomly")]
    public bool dropUnlockedSeeds = true;
    [Range(0f, 1f)]
    public float seedDropChance = 0.35f;

    public System.Collections.Generic.List<ItemSlot> RollDrops()
    {
        var drops = new System.Collections.Generic.List<ItemSlot>();

        if (lootDrops != null)
        {
            for (int i = 0; i < lootDrops.Length; i++)
            {
                var entry = lootDrops[i];
                if (entry == null || entry.item == null) continue;

                if (entry.requireUnlock)
                {
                    if (ProgressionManager.Instance != null && !ProgressionManager.Instance.IsItemUnlocked(entry.item))
                    {
                        continue;
                    }
                }

                if (Random.value <= entry.dropChance)
                {
                    int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                    if (amount > 0)
                    {
                        drops.Add(new ItemSlot { itemData = entry.item, amount = amount });
                    }
                }
            }
        }

        if (dropUnlockedSeeds && ProgressionManager.Instance != null && ProgressionManager.Instance.unlockedSeeds != null)
        {
            var regularSeeds = new System.Collections.Generic.List<ItemData>();
            for (int i = 0; i < ProgressionManager.Instance.unlockedSeeds.Count; i++)
            {
                var s = ProgressionManager.Instance.unlockedSeeds[i];
                if (s != null && !s.isRare)
                {
                    regularSeeds.Add(s);
                }
            }

            if (regularSeeds.Count > 0 && Random.value <= seedDropChance)
            {
                ItemData chosen = regularSeeds[Random.Range(0, regularSeeds.Count)];
                drops.Add(new ItemSlot { itemData = chosen, amount = 1 });
            }
        }

        return drops;
    }
}