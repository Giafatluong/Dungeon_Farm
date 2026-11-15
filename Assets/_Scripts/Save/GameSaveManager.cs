using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Master Save System for Harvest & Hollow.
/// Centralizes all auto-save and manual-save triggers:
/// - Sleeping in bed (New Day)
/// - Returning from Dungeon run
/// - Entering Dungeon
/// - Scene transitions
/// - Opening/Closing Main Menu
/// - Exiting the game (Quit button, Alt+F4, window close)
/// - Application pause (mobile / alt-tab)
/// </summary>
public class GameSaveManager : MonoBehaviour
{
    public const string SaveExistsKey = "Game_HasSaveData";
    public const string PlayerSavedPosXKey = "Player_SavedPosX";
    public const string PlayerSavedPosYKey = "Player_SavedPosY";
    public const string PlayerHasSavedPosKey = "Player_HasSavedPos";

    private static GameSaveManager _instance;
    public static GameSaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameSaveManager>();
                if (_instance == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("GameSaveManager");
                    _instance = go.AddComponent<GameSaveManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    public static event System.Action<string> OnGameSaved;

    private static ItemContainer _cachedBackpackContainer;
    private static ItemContainer _cachedChestContainer;
    private static Transform _cachedPlayerTransform;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        OnGameSaved = null;
        _cachedBackpackContainer = null;
        _cachedChestContainer = null;
        _cachedPlayerTransform = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Application.isPlaying && _instance == null)
        {
            _ = Instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[GameSaveManager] Application quitting -> Auto-saving game state...");
        SaveAll("ApplicationQuit");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("[GameSaveManager] Application paused -> Auto-saving game state...");
            SaveAll("ApplicationPause");
        }
    }

    /// <summary>
    /// Checks whether an existing saved game exists on this device.
    /// </summary>
    public static bool HasSaveData()
    {
        return PlayerPrefs.GetInt(SaveExistsKey, 0) == 1
            || PlayerPrefs.HasKey("Farm_CurrentDay")
            || PlayerPrefs.HasKey($"{ItemContainerPersistence.BackpackKeyPrefix}_SlotCount")
            || PlayerPrefs.HasKey($"{ItemContainerPersistence.ChestKeyPrefix}_SlotCount")
            || PlayerPrefs.HasKey("Dungeon_HighestUnlockedFloor");
    }

    /// <summary>
    /// Auto-saves the entire game state across all systems:
    /// Day counter, Farm plots, Player backpack, Home chest, Dungeon progression, Stats, Blessings, Recipes.
    /// </summary>
    public static void SaveAll(string reason = "AutoSave")
    {
        try
        {
            // 1. Mark that a save file exists
            PlayerPrefs.SetInt(SaveExistsKey, 1);

            // 2. Save Day counter
            if (DayManager.Instance != null)
            {
                PlayerPrefs.SetInt("Farm_CurrentDay", DayManager.Instance.currentDay);
            }

            // 3. Save Farm Plots & Player Position (only active in Base scene)
            string activeSceneName = SceneManager.GetActiveScene().name;
            if (activeSceneName == "Base" || activeSceneName.Contains("Base"))
            {
                FarmPlot.SaveAllPlots();
                SavePlayerPosition();
            }

            // 4. Save Player Backpack
            ItemContainer backpack = GetBackpackContainer();
            if (backpack != null)
            {
                ItemContainerPersistence.SaveContainer(backpack, ItemContainerPersistence.BackpackKeyPrefix, flush: false);
            }

            // 5. Save Home Chest
            ItemContainer chest = GetChestContainer();
            if (chest != null)
            {
                ItemContainerPersistence.SaveContainer(chest, ItemContainerPersistence.ChestKeyPrefix, flush: false);
            }

            // 6. Save Progression (Floors, Bosses, Blessings, Permanent stats, Discovered recipes)
            if (ProgressionManager.Instance != null)
            {
                ProgressionManager.Instance.SaveProgression();
            }

            // 7. Flush all PlayerPrefs to disk/registry
            PlayerPrefs.Save();

            Debug.Log($"[GameSaveManager] Successfully saved game state. Reason: '{reason}'. (Day: {PlayerPrefs.GetInt("Farm_CurrentDay", 1)}, Floor: {PlayerPrefs.GetInt("Dungeon_HighestUnlockedFloor", 1)})");
            OnGameSaved?.Invoke(reason);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameSaveManager] Exception while saving game: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Saves the player's current coordinate position in Base.
    /// </summary>
    public static void SavePlayerPosition()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (!activeSceneName.Equals("Base", System.StringComparison.OrdinalIgnoreCase)) return;

        if (_cachedPlayerTransform == null)
        {
            if (PlayerMovement.Instance != null) _cachedPlayerTransform = PlayerMovement.Instance.transform;
            else if (PlayerStats.Instance != null) _cachedPlayerTransform = PlayerStats.Instance.transform;
            else
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _cachedPlayerTransform = player.transform;
            }
        }

        if (_cachedPlayerTransform != null)
        {
            Vector3 pos = _cachedPlayerTransform.position;
            if (!float.IsNaN(pos.x) && !float.IsNaN(pos.y))
            {
                PlayerPrefs.SetFloat(PlayerSavedPosXKey, pos.x);
                PlayerPrefs.SetFloat(PlayerSavedPosYKey, pos.y);
                PlayerPrefs.SetInt(PlayerHasSavedPosKey, 1);
                Debug.Log($"[GameSaveManager] Saved player position at ({pos.x:F2}, {pos.y:F2})");
            }
        }
    }

    /// <summary>
    /// Restores the player's saved coordinate position in Base.
    /// Returns true if a saved position was found and applied.
    /// </summary>
    public static bool RestorePlayerPosition()
    {
        if (SceneTransitionManager.SpawnAtDungeonEntranceOnBaseLoad) return false;

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (!activeSceneName.Equals("Base", System.StringComparison.OrdinalIgnoreCase)) return false;

        if (PlayerPrefs.GetInt(PlayerHasSavedPosKey, 0) == 1)
        {
            float posX = PlayerPrefs.GetFloat(PlayerSavedPosXKey, 12f);
            float posY = PlayerPrefs.GetFloat(PlayerSavedPosYKey, 0.5f);
            Vector3 savedPos = new Vector3(posX, posY, 0f);

            GameObject player = null;
            if (PlayerMovement.Instance != null) player = PlayerMovement.Instance.gameObject;
            if (player == null && PlayerStats.Instance != null) player = PlayerStats.Instance.gameObject;
            if (player == null) player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                player.transform.position = savedPos;
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.position = savedPos;
                    rb.linearVelocity = Vector2.zero;
                }

                // Snap camera over restored player position
                CameraController cam = Object.FindFirstObjectByType<CameraController>();
                if (cam != null)
                {
                    float targetZ = !Mathf.Approximately(cam.offset.z, 0f) ? cam.offset.z : -10f;
                    cam.transform.position = new Vector3(savedPos.x + cam.offset.x, savedPos.y + cam.offset.y, targetZ);
                }

                Debug.Log($"[GameSaveManager] Restored player position to ({posX:F2}, {posY:F2})");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Loads all persistent items into containers (Backpack & Chest).
    /// Called when the game boots or starts.
    /// </summary>
    public static void LoadContainers()
    {
        try
        {
            ItemContainer backpack = GetBackpackContainer();
            if (backpack != null && ItemContainerPersistence.HasSavedContainer(ItemContainerPersistence.BackpackKeyPrefix))
            {
                ItemContainerPersistence.LoadContainer(backpack, ItemContainerPersistence.BackpackKeyPrefix);
            }

            ItemContainer chest = GetChestContainer();
            if (chest != null && ItemContainerPersistence.HasSavedContainer(ItemContainerPersistence.ChestKeyPrefix))
            {
                ItemContainerPersistence.LoadContainer(chest, ItemContainerPersistence.ChestKeyPrefix);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameSaveManager] Exception while loading containers: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Fully resets all game save data for a brand-new playthrough.
    /// Clears PlayerPrefs, wipes backpack & storage chest, resets progression, offerings, crops, and day.
    /// </summary>
    public static void ResetAllGameData()
    {
        Debug.Log("[GameSaveManager] Resetting ALL game data for a completely fresh New Game...");

        // 1. Delete all PlayerPrefs keys
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("Farm_CurrentDay", 1);
        PlayerPrefs.SetInt("Dungeon_HighestUnlockedFloor", 1);
        PlayerPrefs.Save();

        // 2. Clear all ItemContainers in memory
        try
        {
            ItemContainer[] containers = GameAssetHelper.LoadAll<ItemContainer>();
            if (containers != null)
            {
                for (int i = 0; i < containers.Length; i++)
                {
                    if (containers[i] != null)
                    {
                        containers[i].Clear();
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GameSaveManager] Error clearing ItemContainers: {ex.Message}");
        }

        InventoryButton.selectedItem = null;

        // 3. Reset all ProgressionManager progression
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.ResetAllProgression();
        }

        // 4. Reset DayManager to Day 1
        if (DayManager.Instance != null)
        {
            DayManager.Instance.currentDay = 1;
        }

        // 5. Reset all Statue Offerings
        try
        {
            Offering[] offerings = GameAssetHelper.LoadAll<Offering>();
            if (offerings != null)
            {
                for (int i = 0; i < offerings.Length; i++)
                {
                    if (offerings[i] != null)
                    {
                        offerings[i].completed = false;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GameSaveManager] Error resetting Offerings: {ex.Message}");
        }

        // 6. Reset all FarmPlots in current scene
        try
        {
            FarmPlot[] plots = Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (plots != null)
            {
                for (int i = 0; i < plots.Length; i++)
                {
                    if (plots[i] != null)
                    {
                        plots[i].SetDefault();
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GameSaveManager] Error resetting FarmPlots: {ex.Message}");
        }

        // 7. Reset Tutorial progress
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ResetTutorial();
        }

        // 8. Reset player combat stats and saved position
        PlayerPrefs.DeleteKey(PlayerHasSavedPosKey);
        PlayerPrefs.DeleteKey(PlayerSavedPosXKey);
        PlayerPrefs.DeleteKey(PlayerSavedPosYKey);

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ClearTemporaryBuffs();
            PlayerStats.Instance.RefreshStatsFromProgression();
            PlayerStats.Instance.currentHealth = PlayerStats.Instance.maxHealth;
            PlayerStats.Instance.currentHunger = PlayerStats.Instance.maxHunger;
            PlayerStats.Instance.currentAP = PlayerStats.Instance.maxAP;
            PlayerStats.Instance.defendCount = 0;
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Finds the player's Backpack ItemContainer (cached).
    /// </summary>
    public static ItemContainer GetBackpackContainer()
    {
        if (_cachedBackpackContainer != null) return _cachedBackpackContainer;

        if (PlayerStats.Instance != null && PlayerStats.Instance.itemContainer != null)
        {
            _cachedBackpackContainer = PlayerStats.Instance.itemContainer;
            return _cachedBackpackContainer;
        }

        InventoryPanel invPanel = InventoryPanel.Instance != null ? InventoryPanel.Instance : Object.FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
        if (invPanel != null && invPanel.ItemContainer != null)
        {
            _cachedBackpackContainer = invPanel.ItemContainer;
            return _cachedBackpackContainer;
        }

        ItemContainer[] containers = GameAssetHelper.LoadAll<ItemContainer>();
        if (containers != null)
        {
            for (int i = 0; i < containers.Length; i++)
            {
                if (containers[i] != null && !containers[i].name.ToLower().Contains("chest") && !containers[i].name.ToLower().Contains("loot"))
                {
                    _cachedBackpackContainer = containers[i];
                    return _cachedBackpackContainer;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the Home Chest ItemContainer (cached).
    /// </summary>
    public static ItemContainer GetChestContainer()
    {
        if (_cachedChestContainer != null) return _cachedChestContainer;

        if (HomeChest.Instance != null && HomeChest.Instance.chestContainer != null)
        {
            _cachedChestContainer = HomeChest.Instance.chestContainer;
            return _cachedChestContainer;
        }

        ItemContainer[] containers = GameAssetHelper.LoadAll<ItemContainer>();
        if (containers != null)
        {
            for (int i = 0; i < containers.Length; i++)
            {
                if (containers[i] != null && containers[i].name.ToLower().Contains("homechest"))
                {
                    _cachedChestContainer = containers[i];
                    return _cachedChestContainer;
                }
            }
        }

        return null;
    }
}

