using UnityEngine;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    public List<FarmPlot> currentFarmPlots = new List<FarmPlot>();

    public LayerMask farmPlotLayer;

    private void OnTriggerEnter2D(Collider2D other)
    {
        FarmPlot farmPlot = other.GetComponent<FarmPlot>();

        if (farmPlot != null && !currentFarmPlots.Contains(farmPlot))
        {
            currentFarmPlots.Add(farmPlot);
            Debug.Log("Di chuyen vao FarmPlot");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        FarmPlot farmPlot = other.GetComponent<FarmPlot>();

        if (farmPlot != null && currentFarmPlots.Contains(farmPlot))
        {
            currentFarmPlots.Remove(farmPlot);
            Debug.Log("Da di chuyen ra khoi FarmPlot");
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Interact();
        }
    }

    private void Interact()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D hit = Physics2D.OverlapPoint(mousePosition, farmPlotLayer);

        if (hit == null)
            return;

        FarmPlot farmPlot = hit.GetComponent<FarmPlot>();

        if (farmPlot == null)
            return;

        if (!currentFarmPlots.Contains(farmPlot))
            return;

        if (farmPlot.currentState == FarmPlot.State.Empty)
        {
            farmPlot.Planted();
        }
        else if (farmPlot.currentState == FarmPlot.State.Ready)
        {
            farmPlot.Harvest();
        }
    }
}