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
    public int currentGrowthDay; //ngay hien tai cua giai doan phat trien
    public int daysInCurrentStage; //so ngay da qua cua giai doan hien tai
    public int currentStage; //giai doan hien tai cua cay
    public SpriteRenderer cropRenderer;
    // public SpriteRenderer soilRenderer;
    public ItemContainer itemContainer;

    [SerializeField] private InventoryController inventoryController;
    public List<CropData> availableCrops;

    public bool Planted()
    {
        if (currentState != State.Empty) return false;
        ItemData selectedItem = InventoryButton.selectedItem;

        if (selectedItem == null) return false;
        CropData selectedCrop = availableCrops.Find(crop => crop.seedItem == selectedItem);

        if (selectedCrop == null) return false;

        itemContainer.RemoveItem(selectedItem, 1);

        currentState = State.Planted;
        currentGrowthDay = 0;
        currentStage = 0;
        daysInCurrentStage = 0;

        currentCrop = selectedCrop;
        cropRenderer.sprite = currentCrop.growthStages[currentStage];

        return true;
    }
    public void Harvest()
    {
        if(currentState != State.Ready) return;

        GetComponent<ItemDropSpawner>().SpawnItem(
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
        if (currentState == State.Empty || currentState == State.Ready)
            return;

        currentGrowthDay++;
        daysInCurrentStage++;

        if (daysInCurrentStage >= currentCrop.growthTime)
        {
            daysInCurrentStage = 0;

            if (currentStage < currentCrop.growthStages.Length - 1)
            {
                currentStage++;
                cropRenderer.sprite = currentCrop.growthStages[currentStage];

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
        dayManager.OnNewDay += NextDay;
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
        if(dayManager != null)
        {
            dayManager.OnNewDay -= NextDay;
        }
    }
}
