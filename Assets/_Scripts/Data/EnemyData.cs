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
}