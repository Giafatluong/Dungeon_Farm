
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
        if (MainMenuUI.Instance != null && MainMenuUI.Instance.IsOpen) return true;
        if (DayTransitionUI.Instance != null && DayTransitionUI.Instance.IsOpen) return true;
        if (KitchenUI.Instance != null && KitchenUI.Instance.IsOpen) return true;
        if (ChestUI.Instance != null && ChestUI.Instance.IsOpen) return true;
        if (HomeChest.Instance != null && HomeChest.Instance.isOpen) return true;
        if (StatueUI.Instance != null && StatueUI.Instance.IsOpen) return true;
        if (DungeonEntranceUI.Instance != null && DungeonEntranceUI.Instance.IsOpen) return true;
        if (RewardUI.Instance != null && RewardUI.Instance.IsOpen) return true;
        if (LootUI.Instance != null && LootUI.Instance.IsOpen) return true;
        if (CampUI.Instance != null && CampUI.Instance.IsOpen) return true;
        if (DungeonEventUI.Instance != null && DungeonEventUI.Instance.IsOpen) return true;
        if (FloorVictoryUI.Instance != null && FloorVictoryUI.Instance.IsOpen) return true;
        if (MerchantUI.Instance != null && MerchantUI.Instance.IsOpen) return true;
        return false;
    }

    private void InteractFarmPlot()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;
        Vector2 mousePosition = mainCam.ScreenToWorldPoint(Input.mousePosition);

        Collider2D[] hits = (farmPlotLayer.value != 0)
            ? Physics2D.OverlapPointAll(mousePosition, farmPlotLayer)
            : Physics2D.OverlapPointAll(mousePosition);

        if (hits == null || hits.Length == 0)
            return;

        FarmPlot farmPlot = null;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null)
            {
                farmPlot = hits[i].GetComponent<FarmPlot>();
                if (farmPlot != null) break;
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

            Collider2D bedCol = currentBeds[i].GetComponent<Collider2D>();
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