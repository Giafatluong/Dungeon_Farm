using UnityEngine;

public class Camp : MonoBehaviour
{
    public float encounterChance;
    public float actionRisk;
    public enum ActionType
    {
        Eat,
        Cook,
        Exercise,
        Continue
    }
    public bool ambushState;
}
