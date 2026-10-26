using UnityEngine;
using System.Collections.Generic;

public class MerchantEvent : MonoBehaviour
{
    public static MerchantEvent Instance { get; private set; }

    [System.Serializable]
    public class FloorMerchantStatus
    {
        public int floorNumber;
        public bool hasMet;
        public int remainingDays = 5;
        public int cooldownDays = 0;
        public bool isPresent => hasMet ? (remainingDays > 0 && cooldownDays <= 0) : true;
    }

    public const int DefaultStayDuration = 5;
    public const int DefaultCooldownDuration = 3;

    [Header("Merchant State (Current Floor Dynamic Properties)")]
    public bool merchantActive
    {
        get => IsMerchantActiveForFloor(GetCurrentOrSelectedFloor());
        set { }
    }

    public int remainingDays
    {
        get => GetRemainingDaysForFloor(GetCurrentOrSelectedFloor());
        set { }
    }

    public int merchantCooldown
    {
        get => GetCooldownForFloor(GetCurrentOrSelectedFloor());
        set { }
    }

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

    public static int GetCurrentOrSelectedFloor()
    {
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.currentFloor > 0)
            return ProgressionManager.Instance.currentFloor;
        return 1;
    }

    public static FloorMerchantStatus GetFloorMerchantStatus(int floor)
    {
        if (floor <= 0) floor = 1;
        FloorMerchantStatus status = new FloorMerchantStatus();
        status.floorNumber = floor;
        status.hasMet = PlayerPrefs.GetInt($"Merchant_Floor_{floor}_HasMet", 0) == 1;
        status.remainingDays = PlayerPrefs.GetInt($"Merchant_Floor_{floor}_RemainingDays", DefaultStayDuration);
        status.cooldownDays = PlayerPrefs.GetInt($"Merchant_Floor_{floor}_CooldownDays", 0);
        return status;
    }

    public static void SaveFloorMerchantStatus(FloorMerchantStatus status)
    {
        if (status == null) return;
        PlayerPrefs.SetInt($"Merchant_Floor_{status.floorNumber}_HasMet", status.hasMet ? 1 : 0);
        PlayerPrefs.SetInt($"Merchant_Floor_{status.floorNumber}_RemainingDays", status.remainingDays);
        PlayerPrefs.SetInt($"Merchant_Floor_{status.floorNumber}_CooldownDays", status.cooldownDays);
        PlayerPrefs.Save();
    }

    public static void RecordMerchantEncounter(int floor)
    {
        if (floor <= 0) floor = 1;
        FloorMerchantStatus status = GetFloorMerchantStatus(floor);
        if (!status.hasMet)
        {
            status.hasMet = true;
            if (status.remainingDays <= 0) status.remainingDays = DefaultStayDuration;
            status.cooldownDays = 0;
            SaveFloorMerchantStatus(status);
            Debug.Log($"[MerchantEvent] First encounter with Merchant on Floor {floor}! Registered with {status.remainingDays} days remaining.");
        }
    }

    public static bool HasMetMerchant(int floor)
    {
        if (floor <= 0) floor = 1;
        return PlayerPrefs.GetInt($"Merchant_Floor_{floor}_HasMet", 0) == 1;
    }

    public static bool HasMetAnyMerchant()
    {
        int maxFloor = GetMaxFloorsToCheck();
        for (int f = 1; f <= maxFloor; f++)
        {
            if (HasMetMerchant(f)) return true;
        }
        return false;
    }

    public static List<FloorMerchantStatus> GetAllDiscoveredMerchants()
    {
        List<FloorMerchantStatus> list = new List<FloorMerchantStatus>();
        int maxFloor = GetMaxFloorsToCheck();
        for (int f = 1; f <= maxFloor; f++)
        {
            if (HasMetMerchant(f))
            {
                list.Add(GetFloorMerchantStatus(f));
            }
        }
        return list;
    }

    public static int GetMaxFloorsToCheck()
    {
        int maxUnlocked = ProgressionManager.Instance != null ? ProgressionManager.Instance.highestUnlockedFloor : 1;
        return Mathf.Max(5, maxUnlocked + 1);
    }

    public static bool IsMerchantActiveForFloor(int floor)
    {
        if (floor <= 0) floor = 1;
        FloorMerchantStatus status = GetFloorMerchantStatus(floor);
        // When unmet: always available for player to discover (remaining days do not decrease)
        if (!status.hasMet) return true;
        // When already met: active if days remain and not in cooldown
        return status.remainingDays > 0 && status.cooldownDays <= 0;
    }

    public static int GetRemainingDaysForFloor(int floor)
    {
        if (floor <= 0) floor = 1;
        FloorMerchantStatus status = GetFloorMerchantStatus(floor);
        return status.remainingDays;
    }

    public static int GetCooldownForFloor(int floor)
    {
        if (floor <= 0) floor = 1;
        FloorMerchantStatus status = GetFloorMerchantStatus(floor);
        return status.cooldownDays;
    }

    public static void AdvanceCalendarStatus(int newDay)
    {
        int lastProcessedDay = PlayerPrefs.GetInt("Merchant_LastProcessedDay", 0);
        if (lastProcessedDay == 0)
        {
            PlayerPrefs.SetInt("Merchant_LastProcessedDay", newDay);
            PlayerPrefs.Save();
            return;
        }

        if (newDay <= lastProcessedDay) return;

        int daysAdvanced = newDay - lastProcessedDay;
        PlayerPrefs.SetInt("Merchant_LastProcessedDay", newDay);
        PlayerPrefs.Save();

        int maxFloor = GetMaxFloorsToCheck();
        for (int f = 1; f <= maxFloor; f++)
        {
            if (!HasMetMerchant(f))
            {
                // IMPORTANT: "when merchant is unmet, days do not decrement"
                continue;
            }

            FloorMerchantStatus status = GetFloorMerchantStatus(f);
            for (int d = 0; d < daysAdvanced; d++)
            {
                if (status.remainingDays > 0)
                {
                    status.remainingDays--;
                    if (status.remainingDays <= 0)
                    {
                        status.cooldownDays = DefaultCooldownDuration;
                    }
                }
                else if (status.cooldownDays > 0)
                {
                    status.cooldownDays--;
                    if (status.cooldownDays <= 0)
                    {
                        status.remainingDays = DefaultStayDuration;
                    }
                }
            }
            SaveFloorMerchantStatus(status);
        }
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

        AdvanceCalendarStatus(currentDay);
        EnsureDefaultTrades();
    }

    /// <summary>
    /// Checks if a merchant is currently present and active in the dungeon on the current floor.
    /// </summary>
    public static bool IsMerchantActive()
    {
        int floor = GetCurrentOrSelectedFloor();
        return IsMerchantActiveForFloor(floor);
    }

    /// <summary>
    /// Ends merchant visit duration (time expired).
    /// Transitions into cooldown status.
    /// </summary>
    public void ExpireMerchant()
    {
        int floor = GetCurrentOrSelectedFloor();
        FloorMerchantStatus status = GetFloorMerchantStatus(floor);
        status.remainingDays = 0;
        status.cooldownDays = cooldownDurationDays > 0 ? cooldownDurationDays : DefaultCooldownDuration;
        SaveFloorMerchantStatus(status);
        Debug.Log($"[MerchantEvent] Merchant on Floor {floor} expired! Cooldown set to {status.cooldownDays} days.");
    }

    /// <summary>
    /// Decrements cooldown days. When reaching 0, merchant reappears normally.
    /// </summary>
    public void DecrementCooldown(int amount = 1)
    {
        int floor = GetCurrentOrSelectedFloor();
        FloorMerchantStatus status = GetFloorMerchantStatus(floor);
        if (status.cooldownDays > 0)
        {
            status.cooldownDays = Mathf.Max(0, status.cooldownDays - amount);
            if (status.cooldownDays == 0)
            {
                status.remainingDays = stayDurationDays > 0 ? stayDurationDays : DefaultStayDuration;
            }
            SaveFloorMerchantStatus(status);
            Debug.Log($"[MerchantEvent] Floor {floor} Cooldown decremented to {status.cooldownDays}.");
        }
    }

    /// <summary>
    /// Restores or resets the merchant's stay duration (e.g. for a new merchant arrival cycle).
    /// </summary>
    public void ResetMerchant(int days = 5)
    {
        int floor = GetCurrentOrSelectedFloor();
        FloorMerchantStatus status = GetFloorMerchantStatus(floor);
        status.remainingDays = days;
        status.cooldownDays = 0;
        status.hasMet = true;
        SaveFloorMerchantStatus(status);
        EnsureDefaultTrades();
        Debug.Log($"[MerchantEvent] Merchant on Floor {floor} has arrived! Active for {days} days.");
    }

    public void EnsureDefaultTrades()
    {
        ItemData wheatSeed = null, carrotSeed = null, tomatoSeed = null, cornSeed = null, cucumberSeed = null, cabbageSeed = null, chilliSeed = null;
        ItemData wheat = null, carrot = null, cabbage = null, cucumber = null, tomato = null, corn = null, chilli = null;
        FoodData tomatoSoup = null, carrotSoup = null, cabbageSalad = null, cucumberSalad = null, cornButter = null;

        ItemData[] allItems = GameAssetHelper.LoadAll<ItemData>();
        for (int i = 0; i < allItems.Length; i++)
        {
            ItemData it = allItems[i];
            if (it == null) continue;
            string n = it.name.ToLower();
            string iname = it.itemName != null ? it.itemName.ToLower() : "";

            if (it.itemType == ItemData.ItemType.Seed || n.Contains("seed") || iname.Contains("seed"))
            {
                if (n.Contains("wheat") || iname.Contains("wheat")) wheatSeed = it;
                else if (n.Contains("carrot") || iname.Contains("carrot")) carrotSeed = it;
                else if (n.Contains("tomato") || iname.Contains("tomato")) tomatoSeed = it;
                else if (n.Contains("corn") || iname.Contains("corn")) cornSeed = it;
                else if (n.Contains("cucumber") || iname.Contains("cucumber")) cucumberSeed = it;
                else if (n.Contains("cabbage") || iname.Contains("cabbage")) cabbageSeed = it;
                else if (n.Contains("chilli") || iname.Contains("chilli") || n.Contains("chili")) chilliSeed = it;
            }
            else if (it is FoodData fd)
            {
                if (n.Contains("tomato") && n.Contains("soup")) tomatoSoup = fd;
                else if (n.Contains("carrot")) carrotSoup = fd;
                else if (n.Contains("cabbage") && n.Contains("salad")) cabbageSalad = fd;
                else if (n.Contains("cucumber") && n.Contains("salad")) cucumberSalad = fd;
                else if (n.Contains("corn")) cornButter = fd;
                else if (tomatoSoup == null && n.Contains("soup")) tomatoSoup = fd;
            }
            else
            {
                if (n.Contains("wheat") || iname.Contains("wheat")) wheat = it;
                else if (n.Contains("carrot") || iname.Contains("carrot")) carrot = it;
                else if (n.Contains("cabbage") || iname.Contains("cabbage")) cabbage = it;
                else if (n.Contains("cucumber") || iname.Contains("cucumber")) cucumber = it;
                else if (n.Contains("tomato") || iname.Contains("tomato")) tomato = it;
                else if (n.Contains("corn") || iname.Contains("corn")) corn = it;
                else if (n.Contains("chilli") || iname.Contains("chilli") || n.Contains("chili")) chilli = it;
            }
        }

        // Fallbacks for food items
        if (carrotSoup == null) carrotSoup = tomatoSoup;
        if (cabbageSalad == null) cabbageSalad = tomatoSoup;
        if (cucumberSalad == null) cucumberSalad = cabbageSalad ?? tomatoSoup;
        if (cornButter == null) cornButter = tomatoSoup;

        // 1. Default seed trades if empty or broken
        bool needsSeedTrades = seedTrades == null || seedTrades.Length == 0;
        if (!needsSeedTrades)
        {
            for (int i = 0; i < seedTrades.Length; i++)
            {
                if (seedTrades[i] == null || seedTrades[i].inputSeed == null || seedTrades[i].outputSeed == null)
                {
                    needsSeedTrades = true;
                    break;
                }
            }
        }

        if (needsSeedTrades)
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
            if (cucumberSeed != null && cabbageSeed != null)
            {
                trades.Add(new SeedTrade
                {
                    tradeName = "Cucumber Seed -> Cabbage Seed",
                    inputSeed = cucumberSeed,
                    inputAmount = 3,
                    outputSeed = cabbageSeed,
                    outputAmount = 1
                });
            }
            seedTrades = trades.ToArray();
        }

        // 2. Default ration trades if empty or broken
        bool needsRationTrades = rationTrades == null || rationTrades.Length == 0;
        if (!needsRationTrades)
        {
            for (int i = 0; i < rationTrades.Length; i++)
            {
                if (rationTrades[i] == null || rationTrades[i].inputProduce == null || rationTrades[i].outputFood == null)
                {
                    needsRationTrades = true;
                    break;
                }
            }
        }

        if (needsRationTrades)
        {
            List<RationTrade> rTrades = new();
            if (tomatoSoup != null && tomato != null)
            {
                rTrades.Add(new RationTrade
                {
                    tradeName = "Tomato -> Tomato Soup",
                    inputProduce = tomato,
                    inputAmount = 2,
                    outputFood = tomatoSoup,
                    outputAmount = 1
                });
            }
            if (carrotSoup != null && carrot != null)
            {
                rTrades.Add(new RationTrade
                {
                    tradeName = "Carrot -> Carrot Soup",
                    inputProduce = carrot,
                    inputAmount = 2,
                    outputFood = carrotSoup,
                    outputAmount = 1
                });
            }
            if (cabbageSalad != null && cabbage != null)
            {
                rTrades.Add(new RationTrade
                {
                    tradeName = "Cabbage -> Cabbage Salad",
                    inputProduce = cabbage,
                    inputAmount = 2,
                    outputFood = cabbageSalad,
                    outputAmount = 1
                });
            }
            if (cucumberSalad != null && cucumber != null)
            {
                rTrades.Add(new RationTrade
                {
                    tradeName = "Cucumber -> Cucumber Salad",
                    inputProduce = cucumber,
                    inputAmount = 2,
                    outputFood = cucumberSalad,
                    outputAmount = 1
                });
            }
            if (cornButter != null && corn != null)
            {
                rTrades.Add(new RationTrade
                {
                    tradeName = "Corn -> Corn Butter",
                    inputProduce = corn,
                    inputAmount = 2,
                    outputFood = cornButter,
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
        if (Instance == this) Instance = null;
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
