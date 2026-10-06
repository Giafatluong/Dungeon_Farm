using UnityEngine;
using System.Collections.Generic;

public class MerchantEvent : MonoBehaviour
{
    public static MerchantEvent Instance { get; private set; }

    [Header("Merchant State")]
    public bool merchantActive = true;
    public int remainingDays = 5;

    [Header("Calendar Cycle Settings")]
    [Tooltip("How many consecutive days the merchant remains stationed in the dungeon.")]
    public int stayDurationDays = 5;
    [Tooltip("How many days the merchant is away exploring/restocking before returning.")]
    public int cooldownDurationDays = 3;

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        RefreshCalendarStatus();
    }

    /// <summary>
    /// Synchronizes merchant active status and remaining days with the farm calendar (DayManager / PlayerPrefs).
    /// </summary>
    public void RefreshCalendarStatus()
    {
        int currentDay = 1;
        DayManager dm = cachedDayManager != null ? cachedDayManager : DayManager.Instance;
        if (dm != null && dm.currentDay > 0)
        {
            currentDay = dm.currentDay;
        }
        else
        {
            currentDay = PlayerPrefs.GetInt("Farm_CurrentDay", 1);
        }

        int totalCycle = stayDurationDays + cooldownDurationDays;
        if (totalCycle <= 0) totalCycle = 8;

        int cycleDayOffset = (currentDay - 1) % totalCycle;
        if (cycleDayOffset < stayDurationDays)
        {
            merchantActive = true;
            remainingDays = stayDurationDays - cycleDayOffset;
            Debug.Log($"[MerchantEvent] Active! Farm Day: {currentDay} -> {remainingDays} days remaining in current visit.");
        }
        else
        {
            merchantActive = false;
            remainingDays = 0;
            int daysUntilReturn = totalCycle - cycleDayOffset;
            Debug.Log($"[MerchantEvent] Departed on supply run. Farm Day: {currentDay} -> Returns in {daysUntilReturn} days.");
        }

        EnsureDefaultTrades();
    }

    /// <summary>
    /// Checks if a merchant is currently present and active in the dungeon.
    /// </summary>
    public static bool IsMerchantActive()
    {
        if (Instance != null)
        {
            return Instance.merchantActive && Instance.remainingDays > 0;
        }
        MerchantEvent found = FindFirstObjectByType<MerchantEvent>(FindObjectsInactive.Include);
        if (found != null)
        {
            Instance = found;
            return found.merchantActive && found.remainingDays > 0;
        }
        return true; // Default to available if not explicitly placed/depleted
    }

    /// <summary>
    /// Restores or resets the merchant's stay duration (e.g. for a new merchant arrival cycle).
    /// </summary>
    public void ResetMerchant(int days = 5)
    {
        merchantActive = true;
        remainingDays = days;
        recipeTradeCompleted = false;
        EnsureDefaultTrades();
        Debug.Log($"[MerchantEvent] Merchant has arrived for this run! Active for {days} days.");
    }

    public void EnsureDefaultTrades()
    {
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

        // 1. Default seed trades if empty
        if ((seedTrades == null || seedTrades.Length == 0) && wheatSeed != null)
        {
            List<SeedTrade> trades = new();
            if (tomatoSeed != null)
            {
                trades.Add(new SeedTrade
                {
                    tradeName = "Wheat Seed -> Tomato Seed",
                    inputSeed = wheatSeed,
                    inputAmount = 3,
                    outputSeed = tomatoSeed,
                    outputAmount = 1
                });
            }
            if (carrotSeed != null && cornSeed != null)
            {
                trades.Add(new SeedTrade
                {
                    tradeName = "Carrot Seed -> Corn Seed",
                    inputSeed = carrotSeed,
                    inputAmount = 3,
                    outputSeed = cornSeed,
                    outputAmount = 1
                });
            }
            seedTrades = trades.ToArray();
        }

        // 2. Default ration trades if empty
        if ((rationTrades == null || rationTrades.Length == 0) && (carrot != null || wheat != null))
        {
            List<RationTrade> rTrades = new();
            if (freshSalad != null && carrot != null)
            {
                rTrades.Add(new RationTrade
                {
                    tradeName = "Carrot -> Fresh Salad",
                    inputProduce = carrot,
                    inputAmount = 2,
                    outputFood = freshSalad,
                    outputAmount = 1
                });
            }
            if (tomatoSoup != null && wheat != null)
            {
                rTrades.Add(new RationTrade
                {
                    tradeName = "Wheat -> Tomato Soup",
                    inputProduce = wheat,
                    inputAmount = 2,
                    outputFood = tomatoSoup,
                    outputAmount = 1
                });
            }
            rationTrades = rTrades.ToArray();
        }

        // 3. Default recipe if not set or already unlocked in progression
        bool needsNewRecipe = rewardRecipe == null || (ProgressionManager.Instance != null && ProgressionManager.Instance.unlockedRecipes.Contains(rewardRecipe));
        if (needsNewRecipe)
        {
            recipeTradeCompleted = false;
            rewardRecipe = null;
            RecipeData[] allRecipes = Resources.FindObjectsOfTypeAll<RecipeData>();
            for (int i = 0; i < allRecipes.Length; i++)
            {
                if (allRecipes[i] != null && (ProgressionManager.Instance == null || !ProgressionManager.Instance.unlockedRecipes.Contains(allRecipes[i])))
                {
                    rewardRecipe = allRecipes[i];
                    break;
                }
            }

            if (rewardRecipe != null && (requiredItems == null || requiredItems.Length == 0))
            {
                ItemData barterCrop = carrot != null ? carrot : (wheat != null ? wheat : cabbage);
                if (barterCrop != null)
                {
                    requiredItems = new ItemRequirement[]
                    {
                        new ItemRequirement { item = barterCrop, amount = 2 }
                    };
                }
            }
        }
    }

    private DayManager cachedDayManager;

    private void Start()
    {
        cachedDayManager = FindFirstObjectByType<DayManager>();
        if (cachedDayManager != null)
        {
            cachedDayManager.OnNewDay += HandleNewDay;
        }
    }

    private void OnDestroy()
    {
        if (cachedDayManager != null)
        {
            cachedDayManager.OnNewDay -= HandleNewDay;
            cachedDayManager = null;
        }
    }

    private void HandleNewDay()
    {
        RefreshCalendarStatus();
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

        if (GetItemCount(container, trade.inputSeed) < trade.inputAmount)
            return false;

        return HasContainerSpaceFor(container, trade.inputSeed, trade.inputAmount, trade.outputSeed);
    }

    public bool TradeSeed(SeedTrade trade, ItemContainer container)
    {
        if (!merchantActive)
        {
            OnTradeFailed?.Invoke("Merchant has departed.");
            return false;
        }

        if (GetItemCount(container, trade.inputSeed) < trade.inputAmount)
        {
            OnTradeFailed?.Invoke($"Not enough {trade.inputSeed.itemName} (need {trade.inputAmount}) to exchange for {trade.outputSeed.itemName}.");
            return false;
        }

        if (!HasContainerSpaceFor(container, trade.inputSeed, trade.inputAmount, trade.outputSeed))
        {
            OnTradeFailed?.Invoke("Backpack is completely full! Cannot receive new seeds.");
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

        if (GetItemCount(container, trade.inputProduce) < trade.inputAmount)
            return false;

        return HasContainerSpaceFor(container, trade.inputProduce, trade.inputAmount, trade.outputFood);
    }

    public bool TradeRation(RationTrade trade, ItemContainer container)
    {
        if (!merchantActive)
        {
            OnTradeFailed?.Invoke("Merchant has departed.");
            return false;
        }

        if (GetItemCount(container, trade.inputProduce) < trade.inputAmount)
        {
            OnTradeFailed?.Invoke($"Not enough {trade.inputProduce.itemName} (need {trade.inputAmount}) for rations.");
            return false;
        }

        if (!HasContainerSpaceFor(container, trade.inputProduce, trade.inputAmount, trade.outputFood))
        {
            OnTradeFailed?.Invoke("Backpack is completely full! Cannot receive new rations.");
            return false;
        }

        container.RemoveItem(trade.inputProduce, trade.inputAmount);
        container.AddItem(trade.outputFood, trade.outputAmount);

        Debug.Log($"Ration trade successful: {trade.inputAmount}x {trade.inputProduce.itemName} -> {trade.outputAmount}x {trade.outputFood.itemName}!");
        OnRationTradeSuccess?.Invoke(trade);
        return true;
    }

    private bool HasContainerSpaceFor(ItemContainer container, ItemData inputItem, int inputAmount, ItemData outputItem)
    {
        if (container == null || outputItem == null) return false;

        if (container.CanAddItem(outputItem)) return true;

        if (inputItem != null && inputAmount > 0)
        {
            for (int i = 0; i < container.maxSlots; i++)
            {
                if (container.itemSlots[i] != null && container.itemSlots[i].itemData == inputItem)
                {
                    if (container.itemSlots[i].amount <= inputAmount)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private int GetItemCount(ItemContainer container, ItemData item)
    {
        if (container == null || item == null) return 0;
        return container.GetItemCount(item);
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
