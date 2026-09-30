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

    [SerializeField] private InventoryController inventoryController;

    public List<CropData> availableCrops;

    public bool Planted()
    {
        if (currentState != State.Empty)
            return false;

        ItemData selectedItem = InventoryButton.selectedItem;

        if (selectedItem == null)
            return false;

        int slotIndex = -1;

        for (int i = 0; i < itemContainer.itemSlots.Length; i++)
        {
            if (itemContainer.itemSlots[i].itemData == selectedItem &&
                itemContainer.itemSlots[i].amount > 0)
            {
                slotIndex = i;
                break;
            }
        }

        if (slotIndex == -1)
            return false;

        CropData selectedCrop = availableCrops.Find(
            crop => crop.seedItem == selectedItem
        );

        if (selectedCrop == null)
            return false;

        itemContainer.RemoveItem(selectedItem, 1);

        currentState = State.Planted;
        currentGrowthDay = 0;
        currentStage = 0;
        daysInCurrentStage = 0;

        currentCrop = selectedCrop;

        cropRenderer.sprite =
            currentCrop.growthStages[currentStage];

        return true;
    }

    public void Harvest()
    {
        if (currentState != State.Ready)
            return;

        if (currentCrop == null)
            return;

        itemDropSpawner.SpawnItem(
            currentCrop.producedItem,
            currentCrop.harvestAmount
        );

        SetDefault();
    }

    private void SetDefault()
    {
        currentState = State.Empty;
        currentCrop = null;

        currentGrowthDay = 0;
        currentStage = 0;
        daysInCurrentStage = 0;

        cropRenderer.sprite = null;
    }

    public void NextDay()
    {
        if (currentState == State.Empty ||
            currentState == State.Ready)
        {
            return;
        }

        currentGrowthDay++;
        daysInCurrentStage++;

        if (daysInCurrentStage >= currentCrop.growthTime)
        {
            daysInCurrentStage = 0;

            if (currentStage <
                currentCrop.growthStages.Length - 1)
            {
                currentStage++;

                cropRenderer.sprite =
                    currentCrop.growthStages[currentStage];

                if (currentStage ==
                    currentCrop.growthStages.Length - 1)
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
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            Harvest();
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