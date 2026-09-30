using UnityEngine;

public class MerchantEvent : MonoBehaviour
{
    [Header("Merchant State")]
    public bool merchantActive = true;
    public int remainingDays = 5;

    [Header("Trade Requirements (Công thức)")]
    public ItemRequirement[] requiredItems;
    public RecipeData rewardRecipe;

    [Header("Seed Exchange (Đổi hạt giống cấp thấp lấy cấp cao)")]
    public SeedTrade[] seedTrades;

    public event System.Action<RecipeData> OnTradeSuccess;
    public event System.Action<SeedTrade> OnSeedTradeSuccess;
    public event System.Action<string> OnTradeFailed;

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
        if (!merchantActive) return;

        remainingDays--;
        Debug.Log($"Merchant Event: Còn lại {remainingDays} ngày lưu trú.");

        if (remainingDays <= 0)
        {
            merchantActive = false;
            Debug.Log("Merchant đã rời đi vì hết thời gian lưu trú.");
        }
    }

    public bool CanTrade(ItemContainer container)
    {
        if (!merchantActive) return false;
        if (container == null) return false;
        if (requiredItems == null || requiredItems.Length == 0) return false;

        for (int i = 0; i < requiredItems.Length; i++)
        {
            ItemRequirement req = requiredItems[i];
            if (req.item == null || req.amount <= 0) continue;

            if (GetItemCount(container, req.item) < req.amount)
            {
                return false;
            }
        }

        return true;
    }

    public bool Trade(ItemContainer container)
    {
        if (!merchantActive)
        {
            OnTradeFailed?.Invoke("Thương nhân không còn hoạt động hoặc đã rời đi.");
            return false;
        }

        if (!CanTrade(container))
        {
            OnTradeFailed?.Invoke("Chưa đủ vật phẩm theo yêu cầu của Thương nhân.");
            return false;
        }

        // Trừ vật phẩm
        for (int i = 0; i < requiredItems.Length; i++)
        {
            ItemRequirement req = requiredItems[i];
            if (req.item != null && req.amount > 0)
            {
                container.RemoveItem(req.item, req.amount);
            }
        }

        // Mở khóa công thức
        if (rewardRecipe != null && ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.UnlockRecipe(rewardRecipe);
        }

        Debug.Log("Giao dịch thành công với Thương nhân! Nhận công thức: " + (rewardRecipe != null ? rewardRecipe.recipeName : ""));
        merchantActive = false; // Thương nhân hoàn tất giao dịch và rời đi
        OnTradeSuccess?.Invoke(rewardRecipe);
        return true;
    }

    public bool CanTradeSeed(SeedTrade trade, ItemContainer container)
    {
        if (!merchantActive) return false;
        if (trade == null || container == null) return false;
        if (trade.inputSeed == null || trade.inputAmount <= 0) return false;
        if (trade.outputSeed == null || trade.outputAmount <= 0) return false;

        return GetItemCount(container, trade.inputSeed) >= trade.inputAmount;
    }

    public bool TradeSeed(SeedTrade trade, ItemContainer container)
    {
        if (!merchantActive)
        {
            OnTradeFailed?.Invoke("Thương nhân đã rời đi.");
            return false;
        }

        if (!CanTradeSeed(trade, container))
        {
            OnTradeFailed?.Invoke($"Không đủ {trade.inputSeed.itemName} (cần {trade.inputAmount}) để đổi lấy {trade.outputSeed.itemName}.");
            return false;
        }

        // Trừ hạt giống cấp thấp
        container.RemoveItem(trade.inputSeed, trade.inputAmount);

        // Cộng hạt giống cấp cao
        container.AddItem(trade.outputSeed, trade.outputAmount);

        // Mở khóa hạt giống cấp cao này vào progression (để sau này quái thường cũng có tỷ lệ rớt)
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.UnlockSeed(trade.outputSeed);
        }

        Debug.Log($"Đổi hạt thành công: {trade.inputAmount}x {trade.inputSeed.itemName} ➔ {trade.outputAmount}x {trade.outputSeed.itemName}!");
        OnSeedTradeSuccess?.Invoke(trade);
        return true;
    }

    private int GetItemCount(ItemContainer container, ItemData item)
    {
        if (container == null || item == null) return 0;
        int total = 0;
        for (int i = 0; i < container.itemSlots.Length; i++)
        {
            if (container.itemSlots[i].itemData == item)
            {
                total += container.itemSlots[i].amount;
            }
        }
        return total;
    }
}

[System.Serializable]
public class SeedTrade
{
    public string tradeName;
    public ItemData inputSeed;
    public int inputAmount = 3;
    public ItemData outputSeed;
    public int outputAmount = 1;
}
