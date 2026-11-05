
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
    private int _lastHoveredFrame = -1;
    private FarmPlot _cachedHoveredPlot;
    private bool _cachedIsNear;

    private void Awake()
    {
        if (currentFarmPlots != null) currentFarmPlots.Clear();
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

        // World-class farming grid bracket highlight & crop hover tooltip
        if (GetComponent<FarmCursorHighlight>() == null)
        {
            gameObject.AddComponent<FarmCursorHighlight>();
        }
    }

    /// <summary>
    /// True when the player is free to interact with world objects (no modal UI / inventory open).
    /// </summary>
    public bool CanInteractNow()
    {
        if (IsAnyUIOpen()) return false;
        if (inventoryController != null && (!inventoryController.isInteractable || inventoryController.IsInventoryOpen)) return false;
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsInputBlocked) return false;
        return true;
    }

    private FarmPlot _lastSweptPlot = null;
    private float _sweepCooldownTimer = 0f;

    private void Update()
    {
        if (IsAnyUIOpen()) return;
        if (inventoryController != null && !inventoryController.isInteractable) return;
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsInputBlocked) return;

        // Continuous sweep farming: single click or drag/hold across plots
        bool pointerOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                             UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        if (!pointerOverUI)
        {
            if (Input.GetMouseButtonDown(0))
            {
                FarmPlot plot = GetHoveredFarmPlot(out bool isNear);
                if (plot != null && isNear)
                {
                    _lastSweptPlot = plot;
                    _sweepCooldownTimer = 0.08f;
                    ExecuteFarmPlotInteraction(plot);
                }
            }
            else if (Input.GetMouseButton(0))
            {
                _sweepCooldownTimer -= Time.deltaTime;
                FarmPlot plot = GetHoveredFarmPlot(out bool isNear);
                if (plot != null && isNear)
                {
                    // Only sweep actionable plots (Ready to harvest or Empty to plant)
                    if (plot.currentState == FarmPlot.State.Ready || plot.currentState == FarmPlot.State.Empty)
                    {
                        if (plot != _lastSweptPlot && _sweepCooldownTimer <= 0f)
                        {
                            _lastSweptPlot = plot;
                            _sweepCooldownTimer = 0.08f;
                            ExecuteFarmPlotInteraction(plot);
                        }
                    }
                }
            }
            else
            {
                _lastSweptPlot = null;
            }
        }
        else
        {
            _lastSweptPlot = null;
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

            // Harvest ready crop if standing in range
            if (currentFarmPlots != null && currentFarmPlots.Count > 0)
            {
                currentFarmPlots.RemoveAll(p => p == null);
                if (TutorialManager.Instance == null || TutorialManager.Instance.IsInteractionAllowed(TutorialManager.TutorialInteraction.HarvestCrop))
                {
                    for (int i = 0; i < currentFarmPlots.Count; i++)
                    {
                        if (currentFarmPlots[i] != null && currentFarmPlots[i].currentState == FarmPlot.State.Ready)
                        {
                            currentFarmPlots[i].Harvest();
                            return;
                        }
                    }
                }
            }

            InteractBed();
        }
    }

    public bool IsAnyUIOpen()
    {
        return UIEscapeManager.IsAnyModalUIOpen(includeInventory: false, includeMainMenu: true);
    }

    public FarmPlot GetHoveredFarmPlot(out bool isNear)
    {
        if (Time.frameCount == _lastHoveredFrame)
        {
            isNear = _cachedIsNear;
            return _cachedHoveredPlot;
        }

        _lastHoveredFrame = Time.frameCount;
        isNear = false;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null)
        {
            _cachedHoveredPlot = null;
            _cachedIsNear = false;
            return null;
        }
        Vector2 mousePosition = mainCam.ScreenToWorldPoint(Input.mousePosition);

        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.useTriggers = true;
        if (farmPlotLayer.value != 0)
        {
            filter.SetLayerMask(farmPlotLayer);
        }

        int hitCount = Physics2D.OverlapPoint(mousePosition, filter, overlapBuffer);
        if (hitCount <= 0)
        {
            _cachedHoveredPlot = null;
            _cachedIsNear = false;
            return null;
        }

        FarmPlot farmPlot = null;
        for (int i = 0; i < hitCount; i++)
        {
            if (overlapBuffer[i] != null && overlapBuffer[i].TryGetComponent(out farmPlot))
            {
                break;
            }
        }

        if (farmPlot == null)
        {
            _cachedHoveredPlot = null;
            _cachedIsNear = false;
            return null;
        }

        for (int i = currentFarmPlots.Count - 1; i >= 0; i--)
        {
            if (currentFarmPlots[i] == null) currentFarmPlots.RemoveAt(i);
        }

        isNear = currentFarmPlots.Contains(farmPlot) || Vector2.Distance(transform.position, farmPlot.transform.position) <= 2.5f;
        _cachedHoveredPlot = farmPlot;
        _cachedIsNear = isNear;
        return farmPlot;
    }

    public bool ExecuteFarmPlotInteraction(FarmPlot farmPlot)
    {
        if (farmPlot == null) return false;

        if (farmPlot.currentState == FarmPlot.State.Empty)
        {
            if (TutorialManager.Instance != null && !TutorialManager.Instance.IsInteractionAllowed(TutorialManager.TutorialInteraction.PlantSeed))
            {
                DamagePopupManager.ShowText(farmPlot.transform.position + Vector3.up * 0.8f, "Chưa đến lúc gieo hạt!", Color.yellow, 1.5f);
                return false;
            }
            return farmPlot.Planted();
        }
        else if (farmPlot.currentState == FarmPlot.State.Ready)
        {
            if (TutorialManager.Instance != null && !TutorialManager.Instance.IsInteractionAllowed(TutorialManager.TutorialInteraction.HarvestCrop))
            {
                DamagePopupManager.ShowText(farmPlot.transform.position + Vector3.up * 0.8f, "Chưa đến lúc thu hoạch!", Color.yellow, 1.5f);
                return false;
            }
            farmPlot.Harvest();
            return true;
        }
        else if (farmPlot.currentState == FarmPlot.State.Planted && farmPlot.currentCrop != null)
        {
            // Không hiển thị popup chữ bay nữa; thông tin cây trồng được hiển thị trực tiếp qua bảng tooltip khi rê chuột vào
            return true;
        }

        return false;
    }

        private void InteractBed()
    {
        if (TutorialManager.Instance != null && !TutorialManager.Instance.IsInteractionAllowed(TutorialManager.TutorialInteraction.BedSleep))
            return;

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