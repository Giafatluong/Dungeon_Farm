
using UnityEngine;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    public List<FarmPlot> currentFarmPlots = new List<FarmPlot>();
    public List<BedInteraction> currentBeds = new List<BedInteraction>();
    public InventoryController inventoryController;

    public LayerMask farmPlotLayer;

    private void OnTriggerEnter2D(Collider2D other)
    {
        FarmPlot farmPlot = other.GetComponent<FarmPlot>();

        if (farmPlot != null && !currentFarmPlots.Contains(farmPlot))
        {
            currentFarmPlots.Add(farmPlot);
        }

        BedInteraction bed = other.GetComponent<BedInteraction>();

        if (bed != null && !currentBeds.Contains(bed))
        {
            currentBeds.Add(bed);
            Debug.Log("Di chuyen vao giuong");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        FarmPlot farmPlot = other.GetComponent<FarmPlot>();

        if (farmPlot != null && currentFarmPlots.Contains(farmPlot))
        {
            currentFarmPlots.Remove(farmPlot);
        }

        BedInteraction bed = other.GetComponent<BedInteraction>();

        if (bed != null && currentBeds.Contains(bed))
        {
            currentBeds.Remove(bed);
            Debug.Log("Da di chuyen ra khoi giuong");
        }
    }

    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
        if (inventoryController == null)
        {
            inventoryController = FindFirstObjectByType<InventoryController>();
        }
    }

    private void Update()
    {
        if (inventoryController != null && !inventoryController.isInteractable) return;
        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            InteractFarmPlot();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            InteractBed();
        }
    }

    private void InteractFarmPlot()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;
        Vector2 mousePosition = mainCam.ScreenToWorldPoint(Input.mousePosition);
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

    private void InteractBed()
    {
        if (currentBeds.Count == 0)
            return;

        BedInteraction nearestBed = GetNearestBed();

        if (nearestBed == null)
            return;

        nearestBed.Interact();
    }

    private BedInteraction GetNearestBed()
    {
        BedInteraction nearestBed = null;
        float nearestDistance = Mathf.Infinity;

        for (int i = currentBeds.Count - 1; i >= 0; i--)
        {
            if (currentBeds[i] == null)
            {
                currentBeds.RemoveAt(i);
                continue;
            }

            float distance = Vector2.Distance(
                transform.position,
                currentBeds[i].transform.position
            );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestBed = currentBeds[i];
            }
        }

        return nearestBed;
    }
}