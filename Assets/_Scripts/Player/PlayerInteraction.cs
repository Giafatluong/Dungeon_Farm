
using UnityEngine;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    public List<FarmPlot> currentFarmPlots = new List<FarmPlot>();
    public List<BedInteraction> currentBeds = new List<BedInteraction>();
    public InventoryController inventoryController;

    public LayerMask farmPlotLayer;

    private static readonly Collider2D[] overlapBuffer = new Collider2D[16];

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out FarmPlot farmPlot) && !currentFarmPlots.Contains(farmPlot))
        {
            currentFarmPlots.Add(farmPlot);
        }

        if (other.TryGetComponent(out BedInteraction bed) && !currentBeds.Contains(bed))
        {
            currentBeds.Add(bed);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out FarmPlot farmPlot))
        {
            currentFarmPlots.Remove(farmPlot);
        }

        if (other.TryGetComponent(out BedInteraction bed))
        {
            currentBeds.Remove(bed);
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

        // Floating "[E] ..." prompt above the player
        if (GetComponent<InteractionPromptUI>() == null)
        {
            gameObject.AddComponent<InteractionPromptUI>();
        }
    }

    /// <summary>
    /// True when the player is free to interact with world objects (no modal UI / inventory open).
    /// </summary>
    public bool CanInteractNow()
    {
        if (IsAnyUIOpen()) return false;
        if (inventoryController != null && (!inventoryController.isInteractable || inventoryController.IsInventoryOpen)) return false;
        return true;
    }

    private void Update()
    {
        if (IsAnyUIOpen()) return;
        if (inventoryController != null && !inventoryController.isInteractable) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            InteractFarmPlot();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (IsAnyUIOpen()) return;

            // Prioritize chest when in range - never sleep while interacting with chest
            if (HomeChest.Instance != null && HomeChest.Instance.playerInRange)
                return;

            // Prioritize kitchen cooking station when in range
            if (KitchenStation.Instance != null && KitchenStation.Instance.playerInRange)
                return;

            // Prioritize statue when in range
            if (Statue.Instance != null && Statue.Instance.playerInRange)
                return;

            // Prioritize dungeon entrance when in range
            if (DungeonEntrance.Instance != null && DungeonEntrance.Instance.playerInRange)
                return;

            InteractBed();
        }
    }

    public bool IsAnyUIOpen()
    {
        return UIEscapeManager.IsAnyModalUIOpen(includeInventory: false, includeMainMenu: true);
    }

    private void InteractFarmPlot()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;
        Vector2 mousePosition = mainCam.ScreenToWorldPoint(Input.mousePosition);

        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.useTriggers = true;
        if (farmPlotLayer.value != 0)
        {
            filter.SetLayerMask(farmPlotLayer);
        }

        int hitCount = Physics2D.OverlapPoint(mousePosition, filter, overlapBuffer);
        if (hitCount <= 0)
            return;

        FarmPlot farmPlot = null;
        for (int i = 0; i < hitCount; i++)
        {
            if (overlapBuffer[i] != null && overlapBuffer[i].TryGetComponent(out farmPlot))
            {
                break;
            }
        }

        if (farmPlot == null)
            return;

        for (int i = currentFarmPlots.Count - 1; i >= 0; i--)
        {
            if (currentFarmPlots[i] == null) currentFarmPlots.RemoveAt(i);
        }

        bool isNearPlot = currentFarmPlots.Contains(farmPlot) || Vector2.Distance(transform.position, farmPlot.transform.position) <= 2.5f;
        if (!isNearPlot)
            return;

        if (farmPlot.currentState == FarmPlot.State.Empty)
        {
            farmPlot.Planted();
        }
        else if (farmPlot.currentState == FarmPlot.State.Ready)
        {
            farmPlot.Harvest();
        }
        else if (farmPlot.currentState == FarmPlot.State.Planted && farmPlot.currentCrop != null)
        {
            int totalStages = (farmPlot.currentCrop.growthStages != null && farmPlot.currentCrop.growthStages.Length > 0)
                ? farmPlot.currentCrop.growthStages.Length
                : 1;
            int stageNum = farmPlot.currentStage + 1;
            int daysNeeded = Mathf.Max(1, farmPlot.currentCrop.growthTime) - farmPlot.daysInCurrentStage;
            string progressMsg = $"{farmPlot.currentCrop.cropName}: Stage {stageNum}/{totalStages} (~{daysNeeded} days left)";
            DamagePopupManager.ShowText(farmPlot.transform.position + Vector3.up * 0.8f, progressMsg, new Color(0.4f, 0.85f, 1f), 3.5f);
        }
    }

        private void InteractBed()
    {
        if (currentBeds.Count == 0)
        {
            BedInteraction[] allBeds = FindObjectsByType<BedInteraction>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < allBeds.Length; i++)
            {
                if (allBeds[i] != null && allBeds[i].IsPlayerInRange(transform.position))
                {
                    currentBeds.Add(allBeds[i]);
                    break;
                }
            }
        }

        if (currentBeds.Count == 0)
            return;

        BedInteraction nearestBed = GetNearestBed();

        if (nearestBed == null)
            return;

        if (!nearestBed.IsPlayerInRange(transform.position))
        {
            currentBeds.Remove(nearestBed);
            return;
        }

        nearestBed.Interact();
    }

    public BedInteraction GetNearestBed()
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

            Collider2D bedCol = currentBeds[i].CachedCollider;
            Vector2 bedPoint = (bedCol != null) ? bedCol.ClosestPoint(transform.position) : (Vector2)currentBeds[i].transform.position;

            float distance = Vector2.Distance(transform.position, bedPoint);

            // Clean up if far away (e.g. missed trigger exit)
            if (distance > currentBeds[i].maxInteractDistance * 1.5f)
            {
                currentBeds.RemoveAt(i);
                continue;
            }

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestBed = currentBeds[i];
            }
        }

        return nearestBed;
    }
}