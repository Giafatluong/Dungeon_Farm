using UnityEngine;

[CreateAssetMenu(fileName = "New Wave Data", menuName = "Dungeon/Wave Data")]
public class WaveData : ScriptableObject
{
    public enum WaveType
    {
        Combat,
        Reward,
        Camp,
        Event,
        Boss
    }

    public WaveType waveType;

    [Header("Random")]
    public bool isRandom;

    [Header("Combat")]
    public GameObject[] enemyPrefabs;

    [Header("Transition")]
    public float transitionDelay = 1f;
}