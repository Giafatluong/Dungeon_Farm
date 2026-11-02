using UnityEngine;

/// <summary>
/// Central manager for Escape key handling across all game UIs.
/// Ensures that pressing Escape closes open modals first, and prevents
/// MainMenu from accidentally opening in the same frame when another UI closes.
/// </summary>
public static class UIEscapeManager
{
    private static int _lastConsumedFrame = -1;

    /// <summary>
    /// Call this whenever any UI consumes the Escape key to close itself.
    /// </summary>
    public static void ConsumeEscape()
    {
        _lastConsumedFrame = Time.frameCount;
    }

    /// <summary>
    /// Checks if Escape was already consumed in the current frame by any UI.
    /// </summary>
    public static bool IsEscapeConsumedThisFrame()
    {
        return _lastConsumedFrame == Time.frameCount;
    }

    /// <summary>
    /// Checks if any modal or menu UI other than MainMenu is currently open across the entire game.
    /// </summary>
    public static bool IsAnyOtherUIOpen()
    {
        if (IsEscapeConsumedThisFrame()) return true;
        return IsAnyModalUIOpen(includeInventory: true, includeMainMenu: false);
    }

    /// <summary>
    /// Unified zero-allocation check for whether any blocking modal/menu UI is currently open.
    /// </summary>
    public static bool IsAnyModalUIOpen(bool includeInventory = true, bool includeMainMenu = true)
    {
        if (includeMainMenu && MainMenuUI.Instance != null && MainMenuUI.Instance.IsOpen) return true;

        // Base & Farm Modals
        if (ChestUI.Instance != null && ChestUI.Instance.IsOpen) return true;
        if (HomeChest.Instance != null && HomeChest.Instance.isOpen) return true;
        if (KitchenUI.Instance != null && KitchenUI.Instance.IsOpen) return true;
        if (KitchenStation.Instance != null && KitchenStation.Instance.isOpen) return true;
        if (StatueUI.Instance != null && StatueUI.Instance.IsOpen) return true;
        if (DungeonEntranceUI.Instance != null && DungeonEntranceUI.Instance.IsOpen) return true;
        if (includeInventory && InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen) return true;
        if (DayTransitionUI.Instance != null && DayTransitionUI.Instance.IsOpen) return true;

        // Dungeon & Combat Modals
        if (RewardUI.Instance != null && RewardUI.Instance.IsOpen) return true;
        if (MerchantUI.Instance != null && MerchantUI.Instance.IsOpen) return true;
        if (LootUI.Instance != null && LootUI.Instance.IsOpen) return true;
        if (FloorVictoryUI.Instance != null && FloorVictoryUI.Instance.IsOpen) return true;
        if (DungeonEventUI.Instance != null && DungeonEventUI.Instance.IsOpen) return true;
        if (DungeonSummaryUI.Instance != null && DungeonSummaryUI.Instance.IsOpen) return true;
        if (CampUI.Instance != null && CampUI.Instance.IsOpen) return true;
        if (CombatUI.Instance != null && CombatUI.Instance.foodPanel != null && CombatUI.Instance.foodPanel.activeSelf) return true;

        return false;
    }

    private static GameObject _cachedHealthBarCanvas;
    private static GameObject _cachedMiddleBar;
    private static CurrentItemPanel _cachedHotbar;

    /// <summary>
    /// Closes all active modal windows across the scene to guarantee zero UI overlap.
    /// </summary>
    public static void CloseAllModals(bool includeInventory = true, string exceptModal = null)
    {
        if (exceptModal != "Chest")
        {
            if (ChestUI.Instance != null && ChestUI.Instance.IsOpen) ChestUI.Instance.Close();
            if (HomeChest.Instance != null && HomeChest.Instance.isOpen) HomeChest.Instance.CloseChest();
        }

        if (exceptModal != "Kitchen")
        {
            if (KitchenUI.Instance != null && KitchenUI.Instance.IsOpen) KitchenUI.Instance.Close();
            if (KitchenStation.Instance != null && KitchenStation.Instance.isOpen) KitchenStation.Instance.CloseKitchen();
        }

        if (exceptModal != "Statue")
        {
            if (StatueUI.Instance != null && StatueUI.Instance.IsOpen) StatueUI.Instance.Close();
        }

        if (exceptModal != "DungeonEntrance")
        {
            if (DungeonEntranceUI.Instance != null && DungeonEntranceUI.Instance.IsOpen) DungeonEntranceUI.Instance.Close();
        }

        if (includeInventory && exceptModal != "Inventory")
        {
            if (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen)
            {
                InventoryController.Instance.CloseInventoryPanel();
            }
        }

        if (ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.Hide(immediate: true);
        }
    }

    /// <summary>
    /// Notifies the manager that a modal UI has opened. Closes conflicting modals and hides HUD to prevent overlap.
    /// </summary>
    public static void NotifyModalOpened(string modalName)
    {
        CloseAllModals(includeInventory: true, exceptModal: modalName);

        // Hide HUD hotbar and status bars so they never overlap with the modal window
        SetHUDVisible(false);
    }

    /// <summary>
    /// Notifies the manager that a modal UI has closed. Restores HUD if no other modals remain open.
    /// </summary>
    public static void NotifyModalClosed(string modalName)
    {
        if (ItemTooltipUI.Instance != null)
        {
            ItemTooltipUI.Instance.Hide(immediate: true);
        }

        // If no more modals are active, restore HUD
        if (!IsAnyModalUIOpen(includeInventory: true, includeMainMenu: true))
        {
            SetHUDVisible(true);
        }
    }

    /// <summary>
    /// Toggles visibility of background exploration HUD (Hotbar, HealthBar, MiddleBar).
    /// </summary>
    public static void SetHUDVisible(bool visible)
    {
        // Don't show base HUD during combat!
        if (visible && CombatManager.Instance != null && (CombatManager.Instance.isCombatActive || CombatManager.Instance.isPreparingCombat))
            return;

        if (_cachedHotbar == null)
            _cachedHotbar = Object.FindFirstObjectByType<CurrentItemPanel>(FindObjectsInactive.Include);
        if (_cachedHotbar != null)
            _cachedHotbar.gameObject.SetActive(visible);

        if (_cachedHealthBarCanvas == null)
        {
            _cachedHealthBarCanvas = GameObject.Find("HealthBarCanvas");
            if (_cachedHealthBarCanvas == null) _cachedHealthBarCanvas = GameObject.Find("HealthBarcanvas");
        }
        if (_cachedHealthBarCanvas != null)
            _cachedHealthBarCanvas.SetActive(visible);

        if (_cachedMiddleBar == null)
            _cachedMiddleBar = GameObject.Find("MiddleBar");
        if (_cachedMiddleBar != null)
            _cachedMiddleBar.SetActive(visible);
    }
}
