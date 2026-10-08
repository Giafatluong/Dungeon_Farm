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

    [Header("Seed Exchange")]
    public SeedTrade[] seedTrades;

    [Header("Ration Supplies")]
    public RationTrade[] rationTrades;

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
        EnsureDefaultTrades();
        Debug.Log($"[MerchantEvent] Merchant has arrived for this run! Active for {days} days.");
    }

    public void EnsureDefaultTrades()
    {
        ItemData wheatSeed = null, carrotSeed = null, tomatoSeed = null, cornSeed = null, cucumberSeed = null, cabbageSeed = null, chilliSeed = null;
        ItemData wheat = null, carrot = null, cabbage = null, cucumber = null, tomato = null, corn = null, chilli = null;
        FoodData tomatoSoup = null, freshSalad = null;

        ItemData[] allItems = GameAssetHelper.LoadAll<ItemData>();
        for (int i = 0; i < allItems.Length; i++)
        {
            ItemData it = allItems[i];
            if (it == null) continue;
            string n = it.name.ToLower();
            if (n.Contains("wheatseed")) wheatSeed = it;
            else if (n.Contains("carrotseed")) carrotSeed = it;
            else if (n.Contains("tomatoseed")) tomatoSeed = it;
            else if (n.Contains("cornseed")) cornSeed = it;
            else if (n.Contains("cucumberseed")) cucumberSeed = it;
            else if (n.Contains("cabbageseed")) cabbageSeed = it;
            else if (n.Contains("chilli") && n.Contains("seed")) chilliSeed = it;
            else if (n == "wheat") wheat = it;
            else if (n == "carrot") carrot = it;
            else if (n == "cabbage") cabbage = it;
            else if (n == "cucumber") cucumber = it;
            else if (n == "tomato") tomato = it;
            else if (n == "corn") corn = it;
            else if (n == "chilli" || n == "chili") chilli = it;
            else if (it is FoodData fd)
            {
                if (n.Contains("soup")) tomatoSoup = fd;
                else if (n.Contains("salad")) freshSalad = fd;
            }
        }

        // 1. Default seed trades if empty
        if (seedTrades == null || seedTrades.Length == 0)
        {
            List<SeedTrade> trades = new();
            if (wheatSeed != null && tomatoSeed != null)
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
            if (cabbageSeed != null && cucumberSeed != null)
            {
                trades.Add(new SeedTrade
                {
                    tradeName = "Cabbage Seed -> Cucumber Seed",
                    inputSeed = cabbageSeed,
                    inputAmount = 3,
                    outputSeed = cucumberSeed,
                    outputAmount = 1
                });
            }
            if (cornSeed != null && chilliSeed != null)
            {
                trades.Add(new SeedTrade
                {
                    tradeName = "Corn Seed -> Chilli Seed",
                    inputSeed = cornSeed,
                    inputAmount = 3,
                    outputSeed = chilliSeed,
                    outputAmount = 1
                });
            }
            seedTrades = trades.ToArray();
        }

        // 2. Default ration trades if empty or missing inputProduce
        if (rationTrades == null || rationTrades.Length == 0)
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
            if (freshSalad != null && cabbage != null)
            {
                rTrades.Add(new RationTrade
                {
                    tradeName = "Cabbage -> Fresh Salad",
                    inputProduce = cabbage,
                    inputAmount = 2,
                    outputFood = freshSalad,
                    outputAmount = 1
                });
            }
            rationTrades = rTrades.ToArray();
        }
        else
        {
            // Auto-heal missing references in scene-configured ration trades
            for (int i = 0; i < rationTrades.Length; i++)
            {
                if (rationTrades[i] == null) continue;
                if (rationTrades[i].inputProduce == null)
                {
                    string tn = (rationTrades[i].tradeName ?? "").ToLower();
                    if (tn.Contains("wheat") && wheat != null) rationTrades[i].inputProduce = wheat;
                    else if (tn.Contains("tomato") && tomato != null) rationTrades[i].inputProduce = tomato;
                    else if (tn.Contains("carrot") && carrot != null) rationTrades[i].inputProduce = carrot;
                    else if (tn.Contains("cabbage") && cabbage != null) rationTrades[i].inputProduce = cabbage;
                    else if (tn.Contains("cucumber") && cucumber != null) rationTrades[i].inputProduce = cucumber;
                    else if (tn.Contains("corn") && corn != null) rationTrades[i].inputProduce = corn;
                    else if (wheat != null) rationTrades[i].inputProduce = wheat;
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
