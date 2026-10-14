using UnityEngine;
using System.Collections.Generic;

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

    public SpriteRenderer cropRenderer;
    public ItemDropSpawner itemDropSpawner;
    public ItemContainer itemContainer;

    public List<CropData> availableCrops;

    public bool Planted()
    {
        if (currentState != State.Empty)
            return false;

        ItemData selectedItem = InventoryButton.selectedItem;
        if (selectedItem == null)
            return false;

        if (itemContainer == null)
        {
            if (_cachedPlayerStats == null) _cachedPlayerStats = FindFirstObjectByType<PlayerStats>();
            if (_cachedPlayerStats != null) itemContainer = _cachedPlayerStats.itemContainer;
        }

        if (itemContainer == null || !itemContainer.HasItem(selectedItem))
            return false;

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
            return false;

        itemContainer.RemoveItem(selectedItem, 1);

        currentState = State.Planted;
        currentGrowthDay = 0;
        currentStage = 0;
        daysInCurrentStage = 0;

        currentCrop = selectedCrop;

        if (cropRenderer == null)
        {
            cropRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
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
        }

        if (itemDropSpawner != null && currentCrop.producedItem != null)
        {
            itemDropSpawner.SpawnItem(
                currentCrop.producedItem,
                currentCrop.harvestAmount
            );
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

        SavePlotData();
    }

    public void NextDay()
    {
        if (currentState == State.Empty || currentState == State.Ready || currentCrop == null)
        {
            return;
        }

        currentGrowthDay++;
        daysInCurrentStage++;

        if (daysInCurrentStage >= currentCrop.growthTime)
        {
            daysInCurrentStage = 0;

            if (currentCrop.growthStages != null && currentStage < currentCrop.growthStages.Length - 1)
            {
                currentStage++;

                if (cropRenderer != null)
                {
                    cropRenderer.sprite = currentCrop.growthStages[currentStage];
                }

                if (currentStage == currentCrop.growthStages.Length - 1)
                {
                    currentState = State.Ready;
                }
            }
        }

        SavePlotData();
    }

    public DayManager dayManager;

    private void Start()
    {
        if (dayManager == null)
        {
            dayManager = DayManager.Instance ?? FindFirstObjectByType<DayManager>();
        }

        if (dayManager != null)
        {
            dayManager.OnNewDay += NextDay;
        }

        if (itemDropSpawner == null)
        {
            itemDropSpawner =
                GetComponent<ItemDropSpawner>();
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
        if (dayManager != null)
        {
            dayManager.OnNewDay -= NextDay;
        }
    }

    #region Persistence
    public string GetPlotSaveKey()
    {
        Vector3 pos = transform.position;
        return $"FarmPlot_{Mathf.RoundToInt(pos.x * 100f)}_{Mathf.RoundToInt(pos.y * 100f)}";
    }

    public void SavePlotData()
    {
        string key = GetPlotSaveKey();
        if (currentState == State.Empty || currentCrop == null)
        {
            PlayerPrefs.DeleteKey(key + "_State");
            PlayerPrefs.DeleteKey(key + "_Crop");
            PlayerPrefs.DeleteKey(key + "_GrowthDay");
            PlayerPrefs.DeleteKey(key + "_Stage");
            PlayerPrefs.DeleteKey(key + "_DaysInStage");
            PlayerPrefs.Save();
            return;
        }

        PlayerPrefs.SetInt(key + "_State", (int)currentState);
        PlayerPrefs.SetString(key + "_Crop", currentCrop.name);
        PlayerPrefs.SetInt(key + "_GrowthDay", currentGrowthDay);
        PlayerPrefs.SetInt(key + "_Stage", currentStage);
        PlayerPrefs.SetInt(key + "_DaysInStage", daysInCurrentStage);
        PlayerPrefs.Save();
    }

    public void LoadPlotData()
    {
        string key = GetPlotSaveKey();
        if (!PlayerPrefs.HasKey(key + "_State"))
        {
            return;
        }

        int stateInt = PlayerPrefs.GetInt(key + "_State", (int)State.Empty);
        if (stateInt == (int)State.Empty) return;

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

        if (cropRenderer == null)
        {
            cropRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        }

        if (cropRenderer != null && currentCrop.growthStages != null && currentCrop.growthStages.Length > 0)
        {
            int stageIdx = Mathf.Clamp(currentStage, 0, currentCrop.growthStages.Length - 1);
            cropRenderer.sprite = currentCrop.growthStages[stageIdx];
        }
    }
    #endregion
}