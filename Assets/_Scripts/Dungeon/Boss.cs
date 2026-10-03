using UnityEngine;

public class Boss : MonoBehaviour
{
    public string bossID = "Boss_Floor_1";
    public int floorIndex = 1;
    public bool isDefeated = false;

    [Header("Reset Logic")]
    public int resetDaysRequired = 3;
    public int daysUntilReset = 0;

    [Header("Rewards")]
    public ItemRequirement[] rewardItems;
    public ItemData rareSeedReward;

    public event System.Action OnBossReset;

    private void Start()
    {
        DayManager dayManager = FindFirstObjectByType<DayManager>();
        if (dayManager != null)
        {
            dayManager.OnNewDay += HandleNewDay;
        }
    }

    private void OnDestroy()
    {
        DayManager dayManager = FindFirstObjectByType<DayManager>();
        if (dayManager != null)
        {
            dayManager.OnNewDay -= HandleNewDay;
        }
    }

    private void HandleNewDay()
    {
        if (!isDefeated) return;

        daysUntilReset--;
        Debug.Log($"Boss {bossID}: {daysUntilReset} days until respawn.");

        if (daysUntilReset <= 0)
        {
            isDefeated = false;
            Debug.Log($"Boss {bossID} has respawned!");
            OnBossReset?.Invoke();
        }
    }

    public void OnBossDefeated(ItemContainer playerContainer)
    {
        isDefeated = true;
        daysUntilReset = resetDaysRequired;

        Debug.Log($"Boss {bossID} defeated!");

        if (playerContainer != null && rewardItems != null)
        {
            for (int i = 0; i < rewardItems.Length; i++)
            {
                if (rewardItems[i].item != null && rewardItems[i].amount > 0)
                {
                    playerContainer.AddItem(rewardItems[i].item, rewardItems[i].amount);
                }
            }
        }

        if (rareSeedReward != null && ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.UnlockSeed(rareSeedReward);
        }

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.CompleteRun();
        }
    }
}
