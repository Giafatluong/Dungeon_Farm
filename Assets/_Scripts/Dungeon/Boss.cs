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
        Debug.Log($"Boss {bossID}: Còn {daysUntilReset} ngày nữa để reset.");

        if (daysUntilReset <= 0)
        {
            isDefeated = false;
            Debug.Log($"Boss {bossID} đã hồi sinh sau thời gian nghỉ ngơi!");
            OnBossReset?.Invoke();
        }
    }

    public void OnBossDefeated(ItemContainer playerContainer)
    {
        isDefeated = true;
        daysUntilReset = resetDaysRequired;

        Debug.Log($"Boss {bossID} đã bị tiêu diệt!");

        // Trao thưởng vật phẩm
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

        // Mở khóa hạt giống hiếm nếu có
        if (rareSeedReward != null && ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.UnlockSeed(rareSeedReward);
        }

        // Hoàn thành Floor / Run
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.CompleteRun();
        }
    }
}
