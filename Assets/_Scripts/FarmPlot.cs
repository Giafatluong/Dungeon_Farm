using System.Collections.Generic;
using UnityEngine;

public class FarmPlot : MonoBehaviour
{
    public enum State
    {
        Empty,
        Planted,
        Ready
    }

    public State currentState;
    public CropData currentCrop;

    public int currentGrowthDay;
    public int daysInCurrentStage;
    public int currentStage;

    public SpriteRenderer soilRenderer;
    public SpriteRenderer cropRenderer;
    [SerializeField] private Sprite defaultSoilSprite;
    public ItemDropSpawner itemDropSpawner;
    public ItemContainer itemContainer;

    public List<CropData> availableCrops;

    private void Awake()
    {
        if (cropRenderer == null)
        {
            cropRenderer = GetComponent<SpriteRenderer>();
            if (cropRenderer == null) cropRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (soilRenderer != null && defaultSoilSprite != null)
        {
            soilRenderer.sprite = defaultSoilSprite;
        }

        if (itemDropSpawner == null)
        {
            itemDropSpawner = GetComponent<ItemDropSpawner>();
            if (itemDropSpawner == null) itemDropSpawner = gameObject.AddComponent<ItemDropSpawner>();
        }
    }

    public bool Planted()
    {
        if (currentState != State.Empty)
            return false;

        ItemData selectedItem = InventoryButton.selectedItem;
        if (selectedItem == null)
        {
            DamagePopupManager.ShowText(transform.position + Vector3.up * 0.8f, "Hãy chọn hạt giống!", Color.yellow, 3.5f);
            return false;
        }

        if (itemContainer == null)
        {
            if (_cachedPlayerStats == null) _cachedPlayerStats = FindFirstObjectByType<PlayerStats>();
            if (_cachedPlayerStats != null) itemContainer = _cachedPlayerStats.itemContainer;
        }

        if (itemContainer == null || !itemContainer.HasItem(selectedItem))
        {
            DamagePopupManager.ShowText(transform.position + Vector3.up * 0.8f, "Hết hạt giống trong túi!", new Color(1f, 0.4f, 0.4f), 3.5f);
            return false;
        }

        EnsureAvailableCrops();

        CropData selectedCrop = null;
        if (availableCrops != null)
        {
            for (int i = 0; i < availableCrops.Count; i++)
            {
                if (availableCrops[i] != null && ItemContainer.IsItemMatch(availableCrops[i].seedItem, selectedItem))
                {
                    selectedCrop = availableCrops[i];
                    break;
                }
            }
        }

        if (selectedCrop == null)
        {
            DamagePopupManager.ShowText(transform.position + Vector3.up * 0.8f, "Vật phẩm này không phải hạt giống!", new Color(1f, 0.4f, 0.4f), 3.5f);
            return false;
        }

        itemContainer.RemoveItem(selectedItem, 1);

        currentState = State.Planted;
        currentGrowthDay = 0;
        currentStage = 0;
        daysInCurrentStage = 0;

        currentCrop = selectedCrop;

        if (cropRenderer == null)
        {
            cropRenderer = GetComponent<SpriteRenderer>();
            if (cropRenderer == null)
            {
                cropRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (cropRenderer != null && currentCrop.growthStages != null && currentCrop.growthStages.Length > 0)
        {
            cropRenderer.sprite = currentCrop.growthStages[currentStage];
        }

        SavePlotData();
        return true;
    }

    private static List<CropData> _cachedCrops;
    private static PlayerStats _cachedPlayerStats;

    public void EnsureAvailableCrops()
    {
        if (availableCrops != null && availableCrops.Count > 0) return;

        if (_cachedCrops == null || _cachedCrops.Count == 0)
        {
            _cachedCrops = new List<CropData>();
            CropData[] found = GameAssetHelper.LoadAll<CropData>();
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null && !_cachedCrops.Contains(found[i]))
                {
                    _cachedCrops.Add(found[i]);
                }
            }
        }
        availableCrops = _cachedCrops;
    }

    public void Harvest()
    {
        if (currentState != State.Ready || currentCrop == null)
            return;

        if (itemDropSpawner == null)
        {
            itemDropSpawner = GetComponent<ItemDropSpawner>();
            if (itemDropSpawner == null) itemDropSpawner = gameObject.AddComponent<ItemDropSpawner>();
        }

        int harvestCount = Mathf.Max(1, currentCrop.harvestAmount);
        if (currentCrop.producedItem != null)
        {
            if (itemDropSpawner != null)
            {
                itemDropSpawner.SpawnItem(
                    currentCrop.producedItem,
                    harvestCount
                );
            }
            else
            {
                if (itemContainer == null)
                {
                    if (_cachedPlayerStats == null) _cachedPlayerStats = FindFirstObjectByType<PlayerStats>();
                    if (_cachedPlayerStats != null) itemContainer = _cachedPlayerStats.itemContainer;
                }
                if (itemContainer != null)
                {
                    itemContainer.AddItem(currentCrop.producedItem, harvestCount);
                }
            }

            DamagePopupManager.ShowText(transform.position + Vector3.up * 0.8f, $"Thu hoạch +{harvestCount} {currentCrop.cropName}!", new Color(1f, 0.85f, 0.2f), 4.2f);
        }

        SetDefault();
    }

    private void SetDefault()
    {
        currentState = State.Empty;
        currentCrop = null;

        currentGrowthDay = 0;
        currentStage = 0;
        daysInCurrentStage = 0;

        if (cropRenderer != null)
        {
            cropRenderer.sprite = null;
        }

        if (soilRenderer != null && defaultSoilSprite != null)
        {
            soilRenderer.sprite = defaultSoilSprite;
        }

        SavePlotData();
    }

    private int _lastAdvancedDay = -1;

    public void NextDay()
    {
        int curDay = (DayManager.Instance != null) ? DayManager.Instance.GetCurrentDay() : PlayerPrefs.GetInt("Farm_CurrentDay", 1);
        if (_lastAdvancedDay == curDay)
            return; // Đã tăng trưởng cho ngày này rồi, không cộng dồn trùng lặp

        _lastAdvancedDay = curDay;

        NextDayInternal();
        UpdateCropVisual();
        SavePlotData(false);
    }

    private void NextDayInternal()
    {
        if (currentState == State.Empty || currentState == State.Ready || currentCrop == null)
        {
            return;
        }

        currentGrowthDay++;
        daysInCurrentStage++;

        // growthTime là số ngày để phát triển của TỪNG giai đoạn
        int effectiveGrowthTime = Mathf.Max(1, currentCrop.growthTime);

        // Sau khi tích lũy ĐỦ số ngày cho giai đoạn hiện tại (daysInCurrentStage >= effectiveGrowthTime), mới chuyển qua stage tiếp theo
        if (daysInCurrentStage >= effectiveGrowthTime)
        {
            daysInCurrentStage = 0; // Reset số ngày tích lũy cho giai đoạn mới

            int maxStage = (currentCrop.growthStages != null && currentCrop.growthStages.Length > 0)
                ? currentCrop.growthStages.Length - 1
                : 0;

            if (currentStage < maxStage)
            {
                currentStage++;
            }

            // Khi đã đạt đến stage tối đa (quả/bông đã lớn hoàn toàn) -> chuyển sang trạng thái Ready (Chín / Thu hoạch được)
            if (currentStage >= maxStage)
            {
                currentStage = maxStage;
                currentState = State.Ready;
            }
        }
    }

    public void UpdateCropVisual()
    {
        if (cropRenderer == null)
        {
            cropRenderer = GetComponent<SpriteRenderer>();
            if (cropRenderer == null)
            {
                cropRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (cropRenderer != null)
        {
            if (currentState == State.Empty || currentCrop == null)
            {
                cropRenderer.sprite = null;
            }
            else if (currentCrop.growthStages != null && currentCrop.growthStages.Length > 0)
            {
                cropRenderer.enabled = true;
                int stageIdx = Mathf.Clamp(currentStage, 0, currentCrop.growthStages.Length - 1);
                cropRenderer.sprite = currentCrop.growthStages[stageIdx];
            }
        }

        if (soilRenderer != null && defaultSoilSprite != null)
        {
            soilRenderer.sprite = defaultSoilSprite;
        }
    }

    public DayManager dayManager;

    private void OnEnable()
    {
        SubscribeToDayManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromDayManager();
    }

    private void SubscribeToDayManager()
    {
        DayManager targetDM = (DayManager.Instance != null) ? DayManager.Instance : dayManager;
        if (targetDM == null)
        {
            targetDM = FindFirstObjectByType<DayManager>();
        }

        if (targetDM != null)
        {
            dayManager = targetDM;
            targetDM.OnNewDay -= NextDay;
            targetDM.OnNewDay += NextDay;
        }
    }

    private void UnsubscribeFromDayManager()
    {
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnNewDay -= NextDay;
        }
        if (dayManager != null)
        {
            dayManager.OnNewDay -= NextDay;
        }
    }

    private void Start()
    {
        SubscribeToDayManager();

        if (itemDropSpawner == null)
        {
            itemDropSpawner = GetComponent<ItemDropSpawner>();
        }

        if (itemContainer == null)
        {
            if (_cachedPlayerStats == null) _cachedPlayerStats = FindFirstObjectByType<PlayerStats>();
            if (_cachedPlayerStats != null) itemContainer = _cachedPlayerStats.itemContainer;
        }

        LoadPlotData();
    }

    private void OnDestroy()
    {
        UnsubscribeFromDayManager();
    }

    #region Persistence
    public string GetPlotSaveKey()
    {
        Vector3 pos = transform.position;
        return $"FarmPlot_{Mathf.RoundToInt(pos.x * 100f)}_{Mathf.RoundToInt(pos.y * 100f)}";
    }

    public void SavePlotData(bool flush = true)
    {
        string key = GetPlotSaveKey();
        if (currentState == State.Empty || currentCrop == null)
        {
            PlayerPrefs.DeleteKey(key + "_State");
            PlayerPrefs.DeleteKey(key + "_Crop");
            PlayerPrefs.DeleteKey(key + "_GrowthDay");
            PlayerPrefs.DeleteKey(key + "_Stage");
            PlayerPrefs.DeleteKey(key + "_DaysInStage");
            PlayerPrefs.DeleteKey(key + "_SavedDay");
            if (flush) PlayerPrefs.Save();
            return;
        }

        int curDay = dayManager != null ? dayManager.GetCurrentDay() : PlayerPrefs.GetInt("Farm_CurrentDay", 1);
        PlayerPrefs.SetInt(key + "_State", (int)currentState);
        PlayerPrefs.SetString(key + "_Crop", currentCrop.name);
        PlayerPrefs.SetInt(key + "_GrowthDay", currentGrowthDay);
        PlayerPrefs.SetInt(key + "_Stage", currentStage);
        PlayerPrefs.SetInt(key + "_DaysInStage", daysInCurrentStage);
        PlayerPrefs.SetInt(key + "_SavedDay", curDay);
        if (flush) PlayerPrefs.Save();
    }

    public void LoadPlotData()
    {
        string key = GetPlotSaveKey();
        if (!PlayerPrefs.HasKey(key + "_State"))
        {
            UpdateCropVisual();
            return;
        }

        int stateInt = PlayerPrefs.GetInt(key + "_State", (int)State.Empty);
        if (stateInt == (int)State.Empty)
        {
            SetDefault();
            return;
        }

        string cropName = PlayerPrefs.GetString(key + "_Crop", "");
        if (string.IsNullOrEmpty(cropName)) return;

        EnsureAvailableCrops();
        CropData foundCrop = null;
        if (availableCrops != null)
        {
            for (int i = 0; i < availableCrops.Count; i++)
            {
                if (availableCrops[i] != null && (availableCrops[i].name.Equals(cropName, System.StringComparison.OrdinalIgnoreCase) || availableCrops[i].cropName.Equals(cropName, System.StringComparison.OrdinalIgnoreCase)))
                {
                    foundCrop = availableCrops[i];
                    break;
                }
            }
        }

        if (foundCrop == null) return;

        currentState = (State)stateInt;
        currentCrop = foundCrop;
        currentGrowthDay = PlayerPrefs.GetInt(key + "_GrowthDay", 0);
        currentStage = PlayerPrefs.GetInt(key + "_Stage", 0);
        daysInCurrentStage = PlayerPrefs.GetInt(key + "_DaysInStage", 0);

        int curDay = (DayManager.Instance != null) ? DayManager.Instance.GetCurrentDay() : (dayManager != null ? dayManager.GetCurrentDay() : PlayerPrefs.GetInt("Farm_CurrentDay", 1));
        int savedDay = PlayerPrefs.GetInt(key + "_SavedDay", curDay);
        int daysPassed = Mathf.Max(0, curDay - savedDay);

        _lastAdvancedDay = curDay;

        if (daysPassed > 0)
        {
            for (int i = 0; i < daysPassed; i++)
            {
                NextDayInternal();
            }
            UpdateCropVisual();
            SavePlotData(false);
        }
        else
        {
            UpdateCropVisual();
        }
    }
    #endregion
}