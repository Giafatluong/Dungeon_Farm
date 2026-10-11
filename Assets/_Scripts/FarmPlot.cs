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
    }

    public DayManager dayManager;

    private void Start()
    {
        if (dayManager == null)
        {
            dayManager = FindFirstObjectByType<DayManager>();
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
    }

    private void OnDestroy()
    {
        if (dayManager != null)
        {
            dayManager.OnNewDay -= NextDay;
        }
    }
}