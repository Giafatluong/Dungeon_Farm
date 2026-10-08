using UnityEngine;
using System.Collections.Generic;

public class ProgressionManager : MonoBehaviour
{
    private static ProgressionManager _instance;
    public static ProgressionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ProgressionManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("ProgressionManager_Runtime");
                    _instance = go.AddComponent<ProgressionManager>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Permanent Stats (From Statues)")]
    public int permanentATK = 0;
    public int permanentDEF = 0;
    public int permanentSpeed = 0;
    public int permanentMaxHP = 0;

    [Header("Meta Progression Unlocks")]
    public List<ItemData> unlockedSeeds = new List<ItemData>();
    public List<ItemData> unlockedItems = new List<ItemData>();
    public List<RecipeData> unlockedRecipes = new List<RecipeData>();
    public HashSet<string> completedOfferings = new HashSet<string>();
    public HashSet<string> discoveredRecipes = new HashSet<string>();

    [Header("Run State")]
    public bool runActive = false;
    public int currentFloor = 1;
    public int currentStage = 0;

    [Header("Blessing System (Phước Lành & Hướng Build)")]
    public BlessingType activeBlessing = BlessingType.None;
    public HashSet<BlessingType> unlockedBlessings = new HashSet<BlessingType>();

    [Header("Floor Progression")]
    public int highestUnlockedFloor = 1;
    public List<int> defeatedBossFloors = new List<int>();
    public int selectedFloor = 1;

    public event System.Action OnProgressionChanged;
    public event System.Action<bool> OnRunEnded; // true: victory, false: defeat
    public event System.Action<RecipeData> OnRecipeDiscovered;
    public event System.Action<BlessingType> OnBlessingChanged;
    public event System.Action<BlessingType> OnBlessingUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadFloorProgression();
        LoadDiscoveredRecipes();
        LoadUnlockedBlessings();
        InitializeDefaultUnlocks();
        CheckAllBlessingUnlocks();
    }

    private void InitializeDefaultUnlocks()
    {
        if (unlockedSeeds == null) unlockedSeeds = new List<ItemData>();
        if (unlockedItems == null) unlockedItems = new List<ItemData>();
        if (unlockedRecipes == null) unlockedRecipes = new List<RecipeData>();

        // 1. Tự động mở khóa sẵn TẤT CẢ các hạt giống trong hệ thống
        ItemData[] allItems = GameAssetHelper.LoadAll<ItemData>();
        for (int i = 0; i < allItems.Length; i++)
        {
            ItemData it = allItems[i];
            if (it == null) continue;
            string nameLower = it.name.ToLower();
            if (nameLower.Contains("seed") || it.itemType == ItemData.ItemType.Seed)
            {
                if (!unlockedSeeds.Contains(it)) unlockedSeeds.Add(it);
            }
            else
            {
                if (!unlockedItems.Contains(it)) unlockedItems.Add(it);
            }
        }

        // 2. Tự động mở khóa sẵn TẤT CẢ các công thức trong hệ thống
        RecipeData[] allRecipes = GameAssetHelper.LoadAll<RecipeData>();
        for (int i = 0; i < allRecipes.Length; i++)
        {
            RecipeData r = allRecipes[i];
            if (r != null && !unlockedRecipes.Contains(r))
            {
                unlockedRecipes.Add(r);
            }
        }
    }

    /// <summary>
    /// Trả về tầng tối thiểu trong Dungeon để hạt giống có thể rơi ngẫu nhiên
    /// Floor 1: Wheat, Carrot
    /// Floor 2: Cabbage, Cucumber, Tomato, Corn
    /// Floor 3+: Chilli và các hạt giống cao cấp
    /// </summary>
    public static int GetSeedMinFloor(ItemData seed)
    {
        if (seed == null) return 1;
        string n = seed.name.ToLower();
        if (n.Contains("wheat") || n.Contains("carrot")) return 1;
        if (n.Contains("cabbage") || n.Contains("cucumber") || n.Contains("tomato") || n.Contains("corn")) return 2;
        if (n.Contains("chilli") || n.Contains("chili")) return 3;
        return 1;
    }

    #region Discovered Recipes (Sổ tay món ăn đã đoán mò thành công)
    public bool IsRecipeDiscovered(RecipeData recipe)
    {
        if (recipe == null) return false;
        string key = !string.IsNullOrEmpty(recipe.recipeName) ? recipe.recipeName : recipe.name;
        return discoveredRecipes != null && discoveredRecipes.Contains(key);
    }

    public bool DiscoverRecipe(RecipeData recipe)
    {
        if (recipe == null) return false;
        if (discoveredRecipes == null) discoveredRecipes = new HashSet<string>();

        string key = !string.IsNullOrEmpty(recipe.recipeName) ? recipe.recipeName : recipe.name;
        if (!discoveredRecipes.Contains(key))
        {
            discoveredRecipes.Add(key);
            SaveDiscoveredRecipes();
            OnRecipeDiscovered?.Invoke(recipe);
            OnProgressionChanged?.Invoke();
            Debug.Log($"[ProgressionManager] Khám phá món ăn mới: {key}!");
            return true; // Lần đầu tiên phát hiện
        }
        return false;
    }

    public void SaveDiscoveredRecipes()
    {
        if (discoveredRecipes != null)
        {
            PlayerPrefs.SetString("Discovered_Recipes", string.Join(";", discoveredRecipes));
            PlayerPrefs.Save();
        }
    }

    public void LoadDiscoveredRecipes()
    {
        if (discoveredRecipes == null) discoveredRecipes = new HashSet<string>();
        discoveredRecipes.Clear();

        string saved = PlayerPrefs.GetString("Discovered_Recipes", "");
        if (!string.IsNullOrEmpty(saved))
        {
            string[] items = saved.Split(';');
            for (int i = 0; i < items.Length; i++)
            {
                string r = items[i].Trim();
                if (!string.IsNullOrEmpty(r))
                {
                    discoveredRecipes.Add(r);
                }
            }
        }
    }
    #endregion

    public bool IsItemUnlocked(ItemData item)
    {
        if (item == null) return false;
        if (unlockedSeeds != null && unlockedSeeds.Contains(item)) return true;
        if (unlockedItems != null && unlockedItems.Contains(item)) return true;
        return false;
    }

    public void UnlockItem(ItemData item)
    {
        if (item == null) return;
        if (unlockedItems == null) unlockedItems = new List<ItemData>();
        if (!unlockedItems.Contains(item))
        {
            unlockedItems.Add(item);
            Debug.Log("Unlocked new item: " + item.itemName);
            OnProgressionChanged?.Invoke();
        }
    }

    public void StartRun(int floor = 1)
    {
        runActive = true;
        currentFloor = floor > 0 ? floor : 1;
        selectedFloor = currentFloor;
        currentStage = 0;
        Debug.Log("Starting Dungeon Run - Floor " + currentFloor);
    }

    public void AddPermanentStat(Offering.RewardStat stat, int amount)
    {
        switch (stat)
        {
            case Offering.RewardStat.ATK:
                permanentATK += amount;
                break;
            case Offering.RewardStat.DEF:
                permanentDEF += amount;
                break;
            case Offering.RewardStat.Speed:
                permanentSpeed += amount;
                break;
        }

        Debug.Log($"Permanently increased {stat} by {amount}!");
        CheckAllBlessingUnlocks();
        OnProgressionChanged?.Invoke();
    }

    public void UnlockRecipe(RecipeData recipe)
    {
        if (recipe == null) return;
        if (!unlockedRecipes.Contains(recipe))
        {
            unlockedRecipes.Add(recipe);
            Debug.Log("Unlocked new recipe: " + recipe.recipeName);
            OnProgressionChanged?.Invoke();
        }
    }

    public void UnlockSeed(ItemData seedItem)
    {
        if (seedItem == null) return;
        if (!unlockedSeeds.Contains(seedItem))
        {
            unlockedSeeds.Add(seedItem);
            Debug.Log("Unlocked new seed: " + seedItem.itemName);
            OnProgressionChanged?.Invoke();
        }
    }

    public bool IsOfferingCompleted(string offeringID)
    {
        if (string.IsNullOrEmpty(offeringID)) return false;
        return completedOfferings.Contains(offeringID);
    }

    public void CompleteOffering(string offeringID)
    {
        if (string.IsNullOrEmpty(offeringID)) return;
        completedOfferings.Add(offeringID);
        CheckAllBlessingUnlocks();
        OnProgressionChanged?.Invoke();
    }

    private readonly Dictionary<string, int> offeringContributions = new Dictionary<string, int>();

    public int GetOfferingProgress(string offeringKey, int requirementIndex)
    {
        if (string.IsNullOrEmpty(offeringKey)) return 0;
        string key = $"{offeringKey}_req_{requirementIndex}";
        return offeringContributions.TryGetValue(key, out int amount) ? amount : 0;
    }

    public void SetOfferingProgress(string offeringKey, int requirementIndex, int amount)
    {
        if (string.IsNullOrEmpty(offeringKey)) return;
        string key = $"{offeringKey}_req_{requirementIndex}";
        offeringContributions[key] = Mathf.Max(0, amount);
        OnProgressionChanged?.Invoke();
    }

    public void AddOfferingProgress(string offeringKey, int requirementIndex, int amount)
    {
        if (string.IsNullOrEmpty(offeringKey) || amount <= 0) return;
        string key = $"{offeringKey}_req_{requirementIndex}";
        int current = offeringContributions.TryGetValue(key, out int val) ? val : 0;
        offeringContributions[key] = current + amount;
        OnProgressionChanged?.Invoke();
    }

    #region Blessing System (Phước Lành Thần Linh & Hướng Build)
    /// <summary>
    /// Ở các tầng đầu tiên (Floor 1-2, highestUnlockedFloor <= 2), người chơi là tân thủ
    /// và được Tượng Thần tự động ban phước "Bảo Hộ Thần Linh" (KeepLootOnDefeat) không rơi đồ khi thua.
    /// Sau khi mở khóa tầng 3 (highestUnlockedFloor > 2), hiệu ứng tân thủ tự động kết thúc.
    /// Từ đó người chơi có thể tự chọn Phước Lành tại Tượng Thần (buff không rơi đồ vẫn có thể xuất hiện trong danh sách).
    /// </summary>
    public bool IsStarterProtectionActive()
    {
        return highestUnlockedFloor <= 2;
    }

    /// <summary>
    /// Kiểm tra người chơi có đang sở hữu loại Phước Lành này không.
    /// Nếu là KeepLootOnDefeat thì tự động có hiệu lực khi đang trong giai đoạn tân thủ.
    /// </summary>
    public bool HasBlessing(BlessingType blessing)
    {
        if (blessing == BlessingType.KeepLootOnDefeat)
        {
            return activeBlessing == BlessingType.KeepLootOnDefeat || IsStarterProtectionActive();
        }
        return activeBlessing == blessing;
    }

    public void SetBlessing(BlessingType blessing)
    {
        if (blessing != BlessingType.None && !IsBlessingUnlocked(blessing))
        {
            Debug.LogWarning($"[ProgressionManager] Phước Lành {blessing} chưa được mở khóa!");
            return;
        }

        activeBlessing = blessing;
        Debug.Log($"[ProgressionManager] Đã nhận Phước Lành Thần Linh: {blessing}");
        OnBlessingChanged?.Invoke(activeBlessing);
    }

    /// <summary>
    /// Xóa Phước Lành hiện tại (khi qua ngày, khi thua trận hoặc khi hoàn thành Dungeon).
    /// </summary>
    public void ClearRunBlessing()
    {
        if (activeBlessing != BlessingType.None)
        {
            Debug.Log($"[ProgressionManager] Phước Lành {activeBlessing} đã kết thúc.");
            activeBlessing = BlessingType.None;
            OnBlessingChanged?.Invoke(activeBlessing);
        }
    }

    /// <summary>
    /// Kiểm tra xem Phước Lành đã được mở khóa hay chưa.
    /// Kết hợp: Mở khóa theo Tầng Dungeon HOẶC Hiến tế tại Tượng Thần HOẶC Sự kiện Dungeon.
    /// </summary>
    public bool IsBlessingUnlocked(BlessingType blessing)
    {
        if (blessing == BlessingType.None) return false;
        // Bảo hộ thần linh luôn mở khóa sẵn từ đầu
        if (blessing == BlessingType.KeepLootOnDefeat) return true;
        if (unlockedBlessings != null && unlockedBlessings.Contains(blessing)) return true;

        bool shouldUnlock = false;
        switch (blessing)
        {
            case BlessingType.RawVitality:
                // Hoàn thành bất kỳ 1 Cúng Dường nông sản nào tại Tượng Thần
                shouldUnlock = completedOfferings != null && completedOfferings.Count >= 1;
                break;
            case BlessingType.BastionStrike:
                // Vượt qua Tầng 1 (mở Tầng 2+) HOẶC đã có cúng dường tăng DEF
                shouldUnlock = highestUnlockedFloor >= 2 || permanentDEF > 0;
                break;
            case BlessingType.SwiftMomentum:
                // Vượt qua Tầng 1 (mở Tầng 2+) HOẶC đã có cúng dường tăng Tốc Độ
                shouldUnlock = highestUnlockedFloor >= 2 || permanentSpeed > 0;
                break;
            case BlessingType.StarvingFury:
                // Vượt qua Tầng 1 (mở Tầng 2+) HOẶC đã có cúng dường tăng ATK
                shouldUnlock = highestUnlockedFloor >= 2 || permanentATK > 0;
                break;
            case BlessingType.GluttonousAegis:
                // Đánh bại Boss Tầng 2 và vượt qua Tầng 2 (mở Tầng 3+)
                shouldUnlock = highestUnlockedFloor >= 3;
                break;
            case BlessingType.BitterDecay:
                // Đánh bại Boss Tầng 2 và vượt qua Tầng 2 (mở Tầng 3+)
                shouldUnlock = highestUnlockedFloor >= 3;
                break;
            case BlessingType.EndlessFeast:
                // Hoàn thành từ 2 Cúng Dường trở lên HOẶC vượt qua Tầng 3 (mở Tầng 4+)
                shouldUnlock = (completedOfferings != null && completedOfferings.Count >= 2) || highestUnlockedFloor >= 4;
                break;
        }

        if (shouldUnlock)
        {
            UnlockBlessing(blessing);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Mở khóa vĩnh viễn một loại Phước Lành.
    /// </summary>
    public bool UnlockBlessing(BlessingType blessing)
    {
        if (blessing == BlessingType.None) return false;
        if (unlockedBlessings == null) unlockedBlessings = new HashSet<BlessingType>();

        if (!unlockedBlessings.Contains(blessing))
        {
            unlockedBlessings.Add(blessing);
            SaveUnlockedBlessings();
            Debug.Log($"<color=#55FF88>[ProgressionManager] ĐÃ MỞ KHÓA PHƯỚC LÀNH MỚI: {blessing}!</color>");
            OnBlessingUnlocked?.Invoke(blessing);
            OnProgressionChanged?.Invoke();
            return true;
        }
        return false;
    }

    public void CheckAllBlessingUnlocks()
    {
        foreach (BlessingType type in System.Enum.GetValues(typeof(BlessingType)))
        {
            if (type != BlessingType.None)
            {
                IsBlessingUnlocked(type);
            }
        }
    }

    public void SaveUnlockedBlessings()
    {
        if (unlockedBlessings != null)
        {
            List<int> ids = new List<int>();
            foreach (var b in unlockedBlessings) ids.Add((int)b);
            PlayerPrefs.SetString("Unlocked_Blessings", string.Join(",", ids));
            PlayerPrefs.Save();
        }
    }

    public void LoadUnlockedBlessings()
    {
        if (unlockedBlessings == null) unlockedBlessings = new HashSet<BlessingType>();
        unlockedBlessings.Clear();
        unlockedBlessings.Add(BlessingType.KeepLootOnDefeat); // Luôn mở khóa sẵn buff bảo hộ cơ bản

        string saved = PlayerPrefs.GetString("Unlocked_Blessings", "");
        if (!string.IsNullOrEmpty(saved))
        {
            string[] parts = saved.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i].Trim(), out int val))
                {
                    unlockedBlessings.Add((BlessingType)val);
                }
            }
        }
    }
    #endregion

    public void HandlePlayerDeath(ItemContainer inventory)
    {
        HandlePlayerDeathWithoutReload(inventory);

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadBase();
        }
    }

    public void HandlePlayerDeathWithoutReload(ItemContainer inventory)
    {
        Debug.Log("Player died! Run ended.");
        runActive = false;

        bool keepLoot = HasBlessing(BlessingType.KeepLootOnDefeat);
        if (inventory != null)
        {
            if (keepLoot)
            {
                Debug.Log("[ProgressionManager] Phước Lành 'Bảo Hộ Thần Linh' đã kích hoạt! Giữ lại toàn bộ vật phẩm trong túi đồ khi tử trận.");
            }
            else
            {
                ClearRunLoot(inventory);
            }
        }

        // Khi thất bại, mất buff tạm thời
        ClearRunBlessing();

        OnRunEnded?.Invoke(false);
    }

    public void CompleteRun()
    {
        Debug.Log("Dungeon Run completed successfully!");
        runActive = false;
        RecordBossDefeated(currentFloor);

        // Khi hoàn thành Dungeon, mất buff tạm thời
        ClearRunBlessing();

        OnRunEnded?.Invoke(true);

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadBase();
        }
    }

    #region Floor Progression Management
    public bool IsFloorUnlocked(int floor)
    {
        if (floor <= 1) return true;
        // Floor N requires defeating the boss of floor N - 1
        return (defeatedBossFloors != null && defeatedBossFloors.Contains(floor - 1)) || highestUnlockedFloor >= floor;
    }

    public bool IsBossDefeated(int floor)
    {
        return defeatedBossFloors != null && defeatedBossFloors.Contains(floor);
    }

    public void RecordBossDefeated(int floor)
    {
        if (floor < 1) floor = 1;
        if (defeatedBossFloors == null) defeatedBossFloors = new List<int>();

        bool changed = false;
        if (!defeatedBossFloors.Contains(floor))
        {
            defeatedBossFloors.Add(floor);
            changed = true;
        }

        if (floor + 1 > highestUnlockedFloor)
        {
            highestUnlockedFloor = floor + 1;
            changed = true;
        }

        if (changed)
        {
            SaveFloorProgression();
            CheckAllBlessingUnlocks();
            Debug.Log($"[ProgressionManager] Boss of Floor {floor} defeated! Floor {floor + 1} is now unlocked! (Highest: {highestUnlockedFloor})");
            OnProgressionChanged?.Invoke();
        }
    }

    public void UnlockFloor(int floor)
    {
        if (floor > highestUnlockedFloor)
        {
            highestUnlockedFloor = floor;
            SaveFloorProgression();
            CheckAllBlessingUnlocks();
            OnProgressionChanged?.Invoke();
        }
    }

    public void SaveFloorProgression()
    {
        PlayerPrefs.SetInt("Dungeon_HighestUnlockedFloor", highestUnlockedFloor);
        if (defeatedBossFloors != null)
        {
            PlayerPrefs.SetString("Dungeon_DefeatedBossFloors", string.Join(",", defeatedBossFloors));
        }
        PlayerPrefs.Save();
    }

    public void LoadFloorProgression()
    {
        highestUnlockedFloor = PlayerPrefs.GetInt("Dungeon_HighestUnlockedFloor", 1);
        if (highestUnlockedFloor < 1) highestUnlockedFloor = 1;

        if (defeatedBossFloors == null) defeatedBossFloors = new List<int>();
        defeatedBossFloors.Clear();

        string savedBosses = PlayerPrefs.GetString("Dungeon_DefeatedBossFloors", "");
        if (!string.IsNullOrEmpty(savedBosses))
        {
            string[] parts = savedBosses.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i].Trim(), out int f) && !defeatedBossFloors.Contains(f))
                {
                    defeatedBossFloors.Add(f);
                }
            }
        }
    }

    #endregion

    private void ClearRunLoot(ItemContainer inventory)
    {
        if (inventory == null || inventory.itemSlots == null) return;
        for (int i = 0; i < inventory.itemSlots.Length; i++)
        {
            if (inventory.itemSlots[i] != null)
            {
                inventory.itemSlots[i].itemData = null;
                inventory.itemSlots[i].amount = 0;
            }
        }
        InventoryButton.selectedItem = null;
        inventory.NotifyChange();
    }
}

public enum BlessingType
{
    None = 0,
    KeepLootOnDefeat = 1,   // Bảo Hộ Thần Linh (Không mất đồ khi thua trận trong Dungeon)
    BastionStrike = 2,      // Lấy Thủ Làm Công (+50% DEF vào ATK; Defend nhận thêm 1 AP)
    SwiftMomentum = 3,      // Động Lực Tốc Độ (+4 Tốc độ; Tốc độ cao hơn quái gây thêm sát thương)
    StarvingFury = 4,       // Bụng Rỗng Cuồng Nộ (Độ no < 35% tăng +6 ATK và bạo kích x1.5)
    GluttonousAegis = 5,    // No Căng Phản Đòn (Độ no > 70% nhận +8 DEF và phản 40% sát thương)
    EndlessFeast = 6,       // Đại Tiệc Bất Tận (Buff món ăn x2 thời lượng; món đầu mỗi lượt miễn phí AP)
    BitterDecay = 7,        // Món Đắng Bào Mòn (Đòn đánh găm hiệu ứng Độc và Bào mòn -2 DEF của địch)
    RawVitality = 8         // Nông Dân Cường Tráng (Cho phép ăn nông sản thô hồi +12 HP và +20 Độ no)
}

[System.Serializable]
public class BlessingInfo
{
    public BlessingType type;
    public string title;
    public string description;
    public string iconSymbol;
    public Color color;
    public string unlockRequirement;

    public BlessingInfo(BlessingType type, string title, string description, string iconSymbol, Color color, string unlockRequirement = "")
    {
        this.type = type;
        this.title = title;
        this.description = description;
        this.iconSymbol = iconSymbol;
        this.color = color;
        this.unlockRequirement = unlockRequirement;
    }
}

public static class BlessingDatabase
{
    private static readonly Dictionary<BlessingType, BlessingInfo> blessings = new Dictionary<BlessingType, BlessingInfo>
    {
        {
            BlessingType.KeepLootOnDefeat,
            new BlessingInfo(
                BlessingType.KeepLootOnDefeat,
                "Bảo Hộ Thần Linh",
                "Khi thất bại hoặc tử trận trong Dungeon, giữ lại toàn bộ vật phẩm trong túi đồ mà không bị rơi rụng.",
                "🛡️",
                new Color(0.3f, 0.8f, 1f),
                "Mở khóa sẵn ngay từ đầu."
            )
        },
        {
            BlessingType.BastionStrike,
            new BlessingInfo(
                BlessingType.BastionStrike,
                "Lấy Thủ Làm Công",
                "Cộng thêm 50% chỉ số DEF hiện tại vào ATK. Dùng hành động Phòng Thủ (Defend) sẽ hồi ngay +1 Điểm Hành Động (AP).",
                "⚔️",
                new Color(1f, 0.7f, 0.2f),
                "Vượt qua Tầng 1 Dungeon HOẶC Hoàn thành Cúng Dường DEF tại Tượng Thần."
            )
        },
        {
            BlessingType.SwiftMomentum,
            new BlessingInfo(
                BlessingType.SwiftMomentum,
                "Động Lực Tốc Độ",
                "Tăng vĩnh viễn +4 Tốc độ trong run. Nếu Tốc độ cao hơn quái vật, mỗi đòn đánh gây thêm sát thương chênh lệch tốc độ.",
                "⚡",
                new Color(0.2f, 1f, 0.5f),
                "Vượt qua Tầng 1 Dungeon HOẶC Hoàn thành Cúng Dường Tốc Độ tại Tượng Thần."
            )
        },
        {
            BlessingType.StarvingFury,
            new BlessingInfo(
                BlessingType.StarvingFury,
                "Bụng Rỗng Cuồng Nộ",
                "Khi Độ no dưới 35%, nhận thêm +6 ATK và đòn tấn công có 35% tỷ lệ bạo kích gây 150% sát thương.",
                "🔥",
                new Color(1f, 0.3f, 0.3f),
                "Vượt qua Tầng 1 Dungeon HOẶC Hoàn thành Cúng Dường Sức Mạnh (ATK)."
            )
        },
        {
            BlessingType.GluttonousAegis,
            new BlessingInfo(
                BlessingType.GluttonousAegis,
                "No Căng Phản Đòn",
                "Khi Độ no trên 70%, nhận thêm +8 DEF vững chãi và phản lại 40% sát thương nhận vào cho kẻ địch tấn công.",
                "🍲",
                new Color(0.9f, 0.8f, 0.2f),
                "Đánh bại Boss và Vượt qua Tầng 2 Dungeon."
            )
        },
        {
            BlessingType.EndlessFeast,
            new BlessingInfo(
                BlessingType.EndlessFeast,
                "Đại Tiệc Bất Tận",
                "Hiệu lực của tất cả các món ăn tăng gấp đôi thời lượng. Món ăn đầu tiên trong mỗi lượt không tiêu tốn Điểm Hành Động (AP).",
                "🍗",
                new Color(1f, 0.5f, 0.8f),
                "Hoàn thành 2 Cúng Dường tại Tượng Thần HOẶC Vượt qua Tầng 3 Dungeon."
            )
        },
        {
            BlessingType.BitterDecay,
            new BlessingInfo(
                BlessingType.BitterDecay,
                "Món Đắng Bào Mòn",
                "Mọi đòn đánh thường đều kèm độc dược bào mòn: giảm vĩnh viễn 2 DEF của mục tiêu trong trận đấu và gây thêm 4 sát thương độc.",
                "🧪",
                new Color(0.6f, 0.3f, 0.9f),
                "Đánh bại Boss và Vượt qua Tầng 2 Dungeon."
            )
        },
        {
            BlessingType.RawVitality,
            new BlessingInfo(
                BlessingType.RawVitality,
                "Nông Dân Cường Tráng",
                "Cho phép ăn trực tiếp bất kỳ loại nông sản sống nào (lúa mì, cà rốt, bắp...) ngay trong Dungeon để hồi phục +12 HP và +20 Độ no.",
                "🌾",
                new Color(0.4f, 0.9f, 0.3f),
                "Hoàn thành bất kỳ 1 Cúng Dường / Hiến Tế nào tại Tượng Thần."
            )
        }
    };

    public static BlessingInfo GetBlessingInfo(BlessingType type)
    {
        if (blessings.TryGetValue(type, out var info))
        {
            return info;
        }
        return null;
    }

    /// <summary>
    /// Trả về toàn bộ danh sách 8 phước lành (7 hướng build + Bảo hộ không rơi đồ)
    /// để người chơi có thể tự do xem và chọn lối chơi phù hợp với từng tầng.
    /// </summary>
    public static List<BlessingInfo> GetAllBlessings()
    {
        return new List<BlessingInfo>(blessings.Values);
    }

    /// <summary>
    /// Lấy danh sách n phước lành ngẫu nhiên để người chơi chọn.
    /// Đảm bảo luôn có buff không rơi đồ (KeepLootOnDefeat) trong danh sách có thể xuất hiện!
    /// </summary>
    public static List<BlessingInfo> GetRandomChoices(int count = 3)
    {
        List<BlessingInfo> all = new List<BlessingInfo>(blessings.Values);
        for (int i = 0; i < all.Count; i++)
        {
            int r = Random.Range(i, all.Count);
            var temp = all[i];
            all[i] = all[r];
            all[r] = temp;
        }

        int resultCount = Mathf.Clamp(count, 1, all.Count);
        return all.GetRange(0, resultCount);
    }
}
