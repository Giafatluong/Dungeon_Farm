using UnityEngine;

public class MerchantEvent : MonoBehaviour
{
    [Header("Merchant State")]
    public bool merchantActive = true;
    public int remainingDays = 5;

    [Header("Trade Requirements (Recipe)")]
    public ItemRequirement[] requiredItems;
    public RecipeData rewardRecipe;

    [Header("Seed Exchange")]
    public SeedTrade[] seedTrades;

    [Header("Ration Supplies")]
    public RationTrade[] rationTrades;

    [Header("Encounter State")]
    public bool recipeTradeCompleted;

    public event System.Action<RecipeData> OnTradeSuccess;
    public event System.Action<SeedTrade> OnSeedTradeSuccess;
    public event System.Action<RationTrade> OnRationTradeSuccess;
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
        Debug.Log($"Merchant Event: {remainingDays} days remaining.");

        if (remainingDays <= 0)
        {
            merchantActive = false;
            Debug.Log("Merchant has departed as time expired.");
        }
    }

    public bool CanTrade(ItemContainer container)
    {
        if (!merchantActive) return false;
        if (recipeTradeCompleted) return false;
        if (rewardRecipe != null && ProgressionManager.Instance != null && ProgressionManager.Instance.unlockedRecipes.Contains(rewardRecipe)) return false;
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
            OnTradeFailed?.Invoke("Merchant is no longer active or has departed.");
            return false;
        }

        if (!CanTrade(container))
        {
            OnTradeFailed?.Invoke("Not enough items required by the Merchant or recipe already unlocked.");
            return false;
        }

        for (int i = 0; i < requiredItems.Length; i++)
        {
            ItemRequirement req = requiredItems[i];
            if (req.item != null && req.amount > 0)
            {
                container.RemoveItem(req.item, req.amount);
            }
        }

        if (rewardRecipe != null && ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.UnlockRecipe(rewardRecipe);
        }

        Debug.Log("Trade successful with Merchant! Received recipe: " + (rewardRecipe != null ? rewardRecipe.recipeName : ""));
        recipeTradeCompleted = true;
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
            OnTradeFailed?.Invoke("Merchant has departed.");
            return false;
        }

        if (!CanTradeSeed(trade, container))
        {
            OnTradeFailed?.Invoke($"Not enough {trade.inputSeed.itemName} (need {trade.inputAmount}) to exchange for {trade.outputSeed.itemName}.");
            return false;
        }

        container.RemoveItem(trade.inputSeed, trade.inputAmount);
        container.AddItem(trade.outputSeed, trade.outputAmount);

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.UnlockSeed(trade.outputSeed);
        }

        Debug.Log($"Seed trade successful: {trade.inputAmount}x {trade.inputSeed.itemName} -> {trade.outputAmount}x {trade.outputSeed.itemName}!");
        OnSeedTradeSuccess?.Invoke(trade);
        return true;
    }

    public bool CanTradeRation(RationTrade trade, ItemContainer container)
    {
        if (!merchantActive) return false;
        if (trade == null || container == null) return false;
        if (trade.inputProduce == null || trade.inputAmount <= 0) return false;
        if (trade.outputFood == null || trade.outputAmount <= 0) return false;

        return GetItemCount(container, trade.inputProduce) >= trade.inputAmount;
    }

    public bool TradeRation(RationTrade trade, ItemContainer container)
    {
        if (!merchantActive)
        {
            OnTradeFailed?.Invoke("Merchant has departed.");
            return false;
        }

        if (!CanTradeRation(trade, container))
        {
            OnTradeFailed?.Invoke($"Not enough {trade.inputProduce.itemName} (need {trade.inputAmount}) for rations.");
            return false;
        }

        container.RemoveItem(trade.inputProduce, trade.inputAmount);
        container.AddItem(trade.outputFood, trade.outputAmount);

        Debug.Log($"Ration trade successful: {trade.inputAmount}x {trade.inputProduce.itemName} -> {trade.outputAmount}x {trade.outputFood.itemName}!");
        OnRationTradeSuccess?.Invoke(trade);
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

[System.Serializable]
public class RationTrade
{
    public string tradeName;
    public ItemData inputProduce;
    public int inputAmount = 2;
    public ItemData outputFood;
    public int outputAmount = 1;
}
