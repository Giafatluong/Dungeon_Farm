using UnityEngine;

public class Test : MonoBehaviour
{
    public FarmPlot farmPlot;
    public DayManager dayManager;
    public PlayerStats playerStats;
    public FoodData foodData;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            farmPlot.Planted();
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            dayManager.NextDay();
        }
        // if (Input.GetKeyDown(KeyCode.E))
        // {
        //     ItemData selectedItem = InventoryButton.selectedItem;
        //     if(selectedItem.itemType != ItemData.ItemType.Food)
        //     {
        //         Debug.Log("khong phai do an");
        //         return;
        //     }

        //     playerStats.Eat(selectedItem);
        // }
        if (Input.GetKeyDown(KeyCode.R))
        {
            playerStats.ReduceHunger(20);
            Debug.Log("Current hunger : " + playerStats.currentHunger);
        }
    }
}