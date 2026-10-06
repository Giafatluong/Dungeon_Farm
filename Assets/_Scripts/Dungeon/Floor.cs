using UnityEngine;
using System.Collections.Generic;

public class Floor : MonoBehaviour
{
    public enum StageType
    {
        Combat,
        Camp,
        Event,
        Reward,
        Boss,
        Merchant
    }
    public bool isFixed; 
    public int weughtScore;
    public int floorindex;
    public int stageIndex;
    public List<StageConstraint> constraints;
}
