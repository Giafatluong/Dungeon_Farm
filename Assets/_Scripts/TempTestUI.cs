using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GIAO DIỆN CHƠI GAME TẠM THỜI (TẤT CẢ CÁC UI, NÚT BẤM & SỰ KIỆN CÒN THIẾU TRONG GAME)
/// =========================================================================================
/// Bao gồm đầy đủ:
/// 1. HUD THỜI GIAN THỰC: Thanh Máu (HP), Thanh Độ No (Hunger), Đồng hồ Ngày/Buổi, Buffs đang có.
/// 2. NÚT THAO TÁC TRÊN MÀN HÌNH:
///    - Nút [🎒 Túi Đồ]: Mở/đóng kho đồ
///    - Nút [🍽️ Ăn / Dùng Món]: Ăn ngay món đang cầm trên tay (hồi máu & no)
///    - Nút [🛏️ Đi Ngủ]: Ngủ chuyển sang ngày mới
///    - Nút [🌀 Cổng Hầm Ngục]: Mở bảng chọn tầng Dungeon
/// 3. NÚT TƯƠNG TÁC THÔNG MINH (Ngữ cảnh tự động hiện khi đến gần):
///    - Đến gần Giường  -> Nút [🛏️ Đi Ngủ (E)]
///    - Đến gần Rương   -> Nút [📦 Mở Rương Nhà (E)]
///    - Đến gần Bếp     -> Nút [🍳 Nấu Ăn (E)]
///    - Đến gần Tượng   -> Nút [🗿 Cúng Lễ Vật (E)]
///    - Đến gần Ô Đất   -> Nút [🌾 Thu Hoạch] hoặc [🌱 Gieo Hạt]
/// 4. HỆ THỐNG THÔNG BÁO SỰ KIỆN (Toast Banner):
///    - Sự kiện qua ngày mới, đói bụng, nấu ăn xong, thu hoạch, cúng tượng...
/// 5. BẢNG TEST & CHEAT (Phím F1): Giúp điều chỉnh nhanh stats, tua tốc độ game khi cần.
/// 
/// KHI BẠN LÀM XONG UI ĐỒ HỌA CHÍNH THỨC:
/// - Chỉ cần XÓA FILE NÀY (TempTestUI.cs và TempTestUI.cs.meta).
/// - Dự án sẽ sạch 100%, không bị Missing Component hay bất kỳ lỗi nào!
/// =========================================================================================
/// </summary>
public class TempTestUI : MonoBehaviour
{
    private static TempTestUI _instance;
    private static bool _autoInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        _autoInitialized = false;
    }

    // Auto-spawn disabled by default to avoid overlapping GOTY Canvas UI and IMGUI crashes.
    // To enable, uncomment the line below or place TempTestUI on an empty GameObject in the scene.
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (_autoInitialized) return;
        if (FindFirstObjectByType<TempTestUI>() == null)
        {
            GameObject go = new GameObject("[TempGameUI_Auto]");
            DontDestroyOnLoad(go);
            go.AddComponent<TempTestUI>();
            _autoInitialized = true;
        }
    }

    // UI States
    private bool showDevPanel = false; // Bật tắt menu cheat qua F1
    private int selectedDevTab = 0;
    private readonly string[] devTabs = new string[] { "❤️ Nhân Vật", "☀️ Ngày/Time", "🌾 Nông Trại", "🎒 Túi Đồ", "⭐ Chỉ Số" };
    private Rect devWindowRect = new Rect(20, 60, 480, 440);
    private Vector2 invScrollPos;
    private int devGiveAmount = 10;

    // Cheats
    private bool godMode = false;
    private bool infiniteHunger = false;

    // Toast Notification System
    private class ToastMessage
    {
        public string text;
        public Color color;
        public float expireTime;
    }
    private readonly List<ToastMessage> toasts = new List<ToastMessage>();

    // GUI Styles & Textures
    private bool stylesInit = false;
    private GUIStyle boxStyle;
    private GUIStyle btnNormal;
    private GUIStyle btnAction;
    private GUIStyle btnInteract;
    private GUIStyle tabActive;
    private GUIStyle labelStyle;
    private GUIStyle headerStyle;
    private GUIStyle toastStyle;
    private GUIStyle barBg;
    private GUIStyle barFillHp;
    private GUIStyle barFillHunger;
    private GUIStyle barFillAp;

    private Texture2D texDark;
    private Texture2D texBtn;
    private Texture2D texBtnAction;
    private Texture2D texBtnInteract;
    private Texture2D texActive;
    private Texture2D texBarBg;
    private Texture2D texGreen;
    private Texture2D texOrange;
    private Texture2D texCyan;

    // Cache References
    private PlayerStats cachedPlayerStats;
    private DayManager cachedDayManager;
    private PlayerInteraction cachedInteraction;
    private InventoryController cachedInvController;
    private HomeChest cachedChest;
    private KitchenStation cachedKitchen;
    private Statue cachedStatue;

    private float nextHungerWarningTime = 0f;

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

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        UnhookGameEvents();
    }

    private void Start()
    {
        HookGameEvents();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnhookGameEvents();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshReferences();
        HookGameEvents();
    }

    private void RefreshReferences()
    {
        cachedPlayerStats = PlayerStats.Instance != null ? PlayerStats.Instance : FindFirstObjectByType<PlayerStats>();
        cachedDayManager = DayManager.Instance != null ? DayManager.Instance : FindFirstObjectByType<DayManager>();
        cachedInteraction = FindFirstObjectByType<PlayerInteraction>();
        cachedInvController = FindFirstObjectByType<InventoryController>();
        cachedChest = HomeChest.Instance != null ? HomeChest.Instance : FindFirstObjectByType<HomeChest>();
        cachedKitchen = KitchenStation.Instance != null ? KitchenStation.Instance : FindFirstObjectByType<KitchenStation>();
        cachedStatue = FindFirstObjectByType<Statue>();
    }

    private void HookGameEvents()
    {
        RefreshReferences();
        if (cachedDayManager != null)
        {
            cachedDayManager.OnNewDay -= HandleNewDay;
            cachedDayManager.OnNewDay += HandleNewDay;
        }

        if (cachedPlayerStats != null)
        {
            cachedPlayerStats.OnPlayerDeath -= HandlePlayerDeath;
            cachedPlayerStats.OnPlayerDeath += HandlePlayerDeath;
        }

        if (cachedStatue != null)
        {
            cachedStatue.OnOfferingSuccess -= HandleOfferingSuccess;
            cachedStatue.OnOfferingSuccess += HandleOfferingSuccess;
        }
    }

    private void UnhookGameEvents()
    {
        if (cachedDayManager != null) cachedDayManager.OnNewDay -= HandleNewDay;
        if (cachedPlayerStats != null) cachedPlayerStats.OnPlayerDeath -= HandlePlayerDeath;
        if (cachedStatue != null) cachedStatue.OnOfferingSuccess -= HandleOfferingSuccess;
    }

    private void HandleNewDay()
    {
        int day = cachedDayManager != null ? cachedDayManager.currentDay : 1;
        ShowToast($"☀️ CHÀO BUỔI SÁNG: NGÀY {day} ĐÃ BẮT ĐẦU!\nCây trồng đã tăng trưởng thêm 1 ngày.", new Color(1f, 0.9f, 0.3f), 4.5f);
    }

    private void HandlePlayerDeath()
    {
        ShowToast("💀 BẠN ĐÃ KIỆT SỨC!", new Color(1f, 0.3f, 0.3f), 4f);
    }

    private void HandleOfferingSuccess(Offering offering)
    {
        ShowToast("✨ ĐÃ DÂNG LỄ VẬT CHO TƯỢNG THẦN THÀNH CÔNG!\nBạn nhận được phước lành vĩnh viễn.", new Color(0.4f, 1f, 0.5f), 4f);
    }

    public void ShowToast(string text, Color color, float duration = 3.5f)
    {
        toasts.Add(new ToastMessage
        {
            text = text,
            color = color,
            expireTime = Time.unscaledTime + duration
        });
    }

    private void Update()
    {
        // Phím tắt bật/tắt bảng Dev Cheats
        if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.BackQuote))
        {
            showDevPanel = !showDevPanel;
        }

        // Quét cảnh báo đói
        if (cachedPlayerStats == null) cachedPlayerStats = PlayerStats.Instance;
        if (cachedPlayerStats != null && cachedPlayerStats.maxHunger > 0)
        {
            if (cachedPlayerStats.currentHunger <= 0)
            {
                if (Time.time >= nextHungerWarningTime)
                {
                    nextHungerWarningTime = Time.time + 10f;
                    ShowToast("⚠️ BẠN ĐANG ĐÓI LẢ (Độ no = 0)! Hãy ăn thức ăn ngay.", new Color(1f, 0.4f, 0.2f), 3.5f);
                }
            }
        }

        // Cheats
        if (cachedPlayerStats != null)
        {
            if (godMode && cachedPlayerStats.currentHealth < cachedPlayerStats.maxHealth)
            {
                cachedPlayerStats.currentHealth = cachedPlayerStats.maxHealth;
            }
            if (infiniteHunger && cachedPlayerStats.currentHunger < cachedPlayerStats.maxHunger)
            {
                cachedPlayerStats.currentHunger = cachedPlayerStats.maxHunger;
            }
        }

        // Phím tắt tiến vào Dungeon ải 1 khi ở phòng chờ
        WaveManager wm = WaveManager.Instance;
        if (wm != null && wm.isAtEntrance)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (DungeonEntranceArrowUI.Instance != null)
                {
                    DungeonEntranceArrowUI.Instance.OnArrowClicked();
                }
                else
                {
                    wm.ProceedFromEntrance();
                }
            }
        }

        // Dọn dẹp Toasts hết hạn
        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            if (Time.unscaledTime >= toasts[i].expireTime)
            {
                toasts.RemoveAt(i);
            }
        }
    }

    private void InitStyles()
    {
        if (stylesInit) return;

        texDark = MakeTex(2, 2, new Color(0.10f, 0.11f, 0.15f, 0.92f));
        texBtn = MakeTex(2, 2, new Color(0.20f, 0.23f, 0.30f, 1f));
        texBtnAction = MakeTex(2, 2, new Color(0.18f, 0.48f, 0.78f, 1f));
        texBtnInteract = MakeTex(2, 2, new Color(0.92f, 0.55f, 0.15f, 1f));
        texActive = MakeTex(2, 2, new Color(0.25f, 0.65f, 0.95f, 1f));
        texBarBg = MakeTex(2, 2, new Color(0.06f, 0.06f, 0.08f, 0.95f));
        texGreen = MakeTex(2, 2, new Color(0.22f, 0.82f, 0.35f, 1f));
        texOrange = MakeTex(2, 2, new Color(0.95f, 0.62f, 0.18f, 1f));
        texCyan = MakeTex(2, 2, new Color(0.22f, 0.75f, 0.95f, 1f));

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = texDark, textColor = Color.white },
            padding = new RectOffset(10, 10, 10, 10)
        };

        btnNormal = new GUIStyle(GUI.skin.button)
        {
            normal = { background = texBtn, textColor = Color.white },
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        btnAction = new GUIStyle(GUI.skin.button)
        {
            normal = { background = texBtnAction, textColor = Color.white },
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };

        btnInteract = new GUIStyle(GUI.skin.button)
        {
            normal = { background = texBtnInteract, textColor = Color.black },
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };

        tabActive = new GUIStyle(btnNormal)
        {
            normal = { background = texActive, textColor = Color.white }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = Color.white }
        };

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.85f, 0.2f) }
        };

        toastStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = texDark },
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(16, 16, 8, 8)
        };

        barBg = new GUIStyle { normal = { background = texBarBg } };
        barFillHp = new GUIStyle { normal = { background = texGreen } };
        barFillHunger = new GUIStyle { normal = { background = texOrange } };
        barFillAp = new GUIStyle { normal = { background = texCyan } };

        stylesInit = true;
    }

    private Texture2D MakeTex(int w, int h, Color col)
    {
        Color[] pix = new Color[w * h];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;
        Texture2D res = new Texture2D(w, h);
        res.SetPixels(pix);
        res.Apply();
        return res;
    }

    private void OnGUI()
    {
        InitStyles();
        GUI.depth = -9999;

        // 1. THANH TRẠNG THÁI CHÍNH (PLAYER HUD: HP, HUNGER, NGÀY, BUFFS) - GÓC TRÊN TRÁI
        DrawPlayerHUD();

        // 2. CÁC NÚT THAO TÁC NHANH (ACTION BUTTONS) - GÓC TRÊN PHẢI
        DrawQuickActionButtons();

        // 3. NÚT TƯƠNG TÁC NGỮ CẢNH (KHI ĐẾN GẦN RƯƠNG, BẾP, GIƯỜNG, TƯỢNG, ĐẤT) - GIỮA DƯỚI MÀN HÌNH
        DrawContextualInteractionPrompt();

        // 4. HỆ THỐNG THÔNG BÁO SỰ KIỆN (TOAST NOTIFICATIONS) - GIỮA TRÊN MÀN HÌNH
        DrawToastNotifications();

        // 5. CỬA SỔ DEV CHEAT (BẤM F1 ĐỂ BẬT/TẮT)
        if (showDevPanel)
        {
            devWindowRect = GUI.Window(888123, devWindowRect, DrawDevPanelWindow, "🛠️ MENU TEST & CHEAT (F1 Ẩn/Hiện)", boxStyle);
        }
    }

    #region 1. Player HUD (Máu, Độ No, Ngày, Buffs)
    private void DrawPlayerHUD()
    {
        if (cachedPlayerStats == null) cachedPlayerStats = PlayerStats.Instance;
        if (cachedDayManager == null) cachedDayManager = DayManager.Instance;

        int buffCount = cachedPlayerStats != null && cachedPlayerStats.activeBuffs != null ? cachedPlayerStats.activeBuffs.Count : 0;
        float hudWidth = 270;
        float hudHeight = 115 + Mathf.Max(0, buffCount * 18);
        Rect rect = new Rect(12, 12, hudWidth, hudHeight);

        GUI.Box(rect, GUIContent.none, boxStyle);
        GUILayout.BeginArea(new Rect(rect.x + 8, rect.y + 6, rect.width - 16, rect.height - 12));

        int day = cachedDayManager != null ? cachedDayManager.currentDay : 1;
        GUILayout.Label($"📅 <b>NGÀY {day}</b> | 🕒 {GetTimeOfDayLabel()}", headerStyle);

        if (cachedPlayerStats != null)
        {
            DrawProgressBar("Máu (HP)", cachedPlayerStats.currentHealth, cachedPlayerStats.maxHealth, barFillHp);
            DrawProgressBar("No (Hunger)", cachedPlayerStats.currentHunger, cachedPlayerStats.maxHunger, barFillHunger);

            // Buffs
            if (cachedPlayerStats.activeBuffs != null && cachedPlayerStats.activeBuffs.Count > 0)
            {
                for (int i = 0; i < cachedPlayerStats.activeBuffs.Count; i++)
                {
                    ActiveBuff b = cachedPlayerStats.activeBuffs[i];
                    if (b != null && b.buff != null)
                    {
                        string sign = b.buffValue > 0 ? "+" : "";
                        string durUnit = b.buffDurationType switch
                        {
                            FoodData.BuffDurationType.Turn => "lượt",
                            FoodData.BuffDurationType.Combat => "trận",
                            FoodData.BuffDurationType.Floor => "tầng",
                            _ => b.buffDurationType.ToString()
                        };
                        string color = b.buffValue >= 0 ? "#68D391" : "#FC8181";
                        GUILayout.Label($"✨ <color={color}><b>{b.buff.buffName}</b> {sign}{b.buffValue} ({b.remainingDuration} {durUnit})</color>", labelStyle);
                    }
                }
            }
        }
        else
        {
            GUILayout.Label("<i>(Đang tải dữ liệu nhân vật...)</i>", labelStyle);
        }

        GUILayout.EndArea();
    }

    private string GetTimeOfDayLabel()
    {
        // Nếu có chu kỳ thời gian
        return "Ban Ngày";
    }

    private void DrawProgressBar(string label, int cur, int max, GUIStyle fillStyle)
    {
        if (max <= 0) max = 1;
        float pct = Mathf.Clamp01((float)cur / max);

        Rect barRect = GUILayoutUtility.GetRect(100, 18);
        GUI.Box(barRect, GUIContent.none, barBg);

        Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * pct, barRect.height);
        GUI.Box(fillRect, GUIContent.none, fillStyle);

        GUIStyle centerText = new GUIStyle(labelStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 11,
            normal = { textColor = Color.white }
        };
        GUI.Label(barRect, $"{label}: {cur}/{max}", centerText);
    }
    #endregion

    #region 2. Action Buttons (Nút Thao Tác Màn Hình)
    private void DrawQuickActionButtons()
    {
        float barWidth = 230;
        Rect areaRect = new Rect(Screen.width - barWidth - 12, 12, barWidth, 180);

        GUILayout.BeginArea(areaRect);

        // 1. Nút mở Túi Đồ
        if (GUILayout.Button("🎒 Túi Đồ (Phím I)", btnAction, GUILayout.Height(34)))
        {
            ToggleInventory();
        }

        // 2. Nút Ăn / Dùng vật phẩm đang cầm trên tay (hoặc tự lấy món ăn đầu tiên trong túi nếu chưa chọn)
        ItemData selected = InventoryButton.selectedItem;
        if (selected == null && cachedPlayerStats != null && cachedPlayerStats.itemContainer != null)
        {
            for (int i = 0; i < cachedPlayerStats.itemContainer.itemSlots.Length; i++)
            {
                ItemSlot s = cachedPlayerStats.itemContainer.itemSlots[i];
                if (s != null && s.itemData != null && s.amount > 0)
                {
                    if (s.itemData is FoodData || s.itemData.itemType == ItemData.ItemType.Food || s.itemData.itemType == ItemData.ItemType.Produce)
                    {
                        selected = s.itemData;
                        break;
                    }
                }
            }
        }

        if (selected != null)
        {
            string itemActionName = (selected is FoodData || selected.itemType == ItemData.ItemType.Food || selected.itemType == ItemData.ItemType.Produce)
                ? $"🍽️ Ăn [{selected.itemName}]"
                : $"✋ Cầm [{selected.itemName}]";

            if (GUILayout.Button(itemActionName, btnInteract, GUILayout.Height(34)))
            {
                TryEatSelectedItem(selected);
            }
        }

        // 3. Nút Đi Ngủ để qua ngày
        if (GUILayout.Button("🛏️ Đi Ngủ (Qua Ngày)", btnNormal, GUILayout.Height(30)))
        {
            if (cachedDayManager == null) cachedDayManager = DayManager.Instance;
            if (cachedDayManager != null)
            {
                cachedDayManager.Sleep();
                ShowToast("😴 Bạn đã lên giường ngủ... Đang chuyển sang ngày mới!", new Color(0.7f, 0.85f, 1f));
            }
        }

        // 4. Nút Vào Dungeon / Chọn Tầng
        if (GUILayout.Button("🌀 Cổng Hầm Ngục", btnNormal, GUILayout.Height(30)))
        {
            OpenDungeonEntrance();
        }

        // 5. Nút Bật/Tắt Menu Test Cheat
        if (GUILayout.Button(showDevPanel ? "❌ Đóng Menu Cheat (F1)" : "⚙️ Menu Cheat (F1)", btnNormal, GUILayout.Height(26)))
        {
            showDevPanel = !showDevPanel;
        }

        GUILayout.EndArea();
    }

    private void ToggleInventory()
    {
        if (cachedInvController == null) cachedInvController = FindFirstObjectByType<InventoryController>();
        if (cachedInvController != null)
        {
            InventoryPanel panel = FindFirstObjectByType<InventoryPanel>(FindObjectsInactive.Include);
            if (panel != null)
            {
                bool willOpen = !panel.gameObject.activeSelf;
                panel.gameObject.SetActive(willOpen);
                cachedInvController.isInteractable = !willOpen;
            }
        }
    }

    private void TryEatSelectedItem(ItemData item)
    {
        if (cachedPlayerStats == null) cachedPlayerStats = PlayerStats.Instance;
        if (cachedPlayerStats == null || item == null) return;

        if (cachedPlayerStats.currentHunger >= cachedPlayerStats.maxHunger)
        {
            ShowToast("😋 BẠN ĐÃ NO CĂNG! Không thể ăn thêm lúc này.", new Color(1f, 0.8f, 0.3f), 2.5f);
            return;
        }

        bool ate = cachedPlayerStats.Eat(item);
        if (ate)
        {
            ShowToast($"🍲 Đã ăn {item.itemName}! Hồi phục sinh lực.", new Color(0.4f, 1f, 0.4f), 3f);
        }
        else
        {
            ShowToast($"❌ Không thể ăn {item.itemName}! (Vật phẩm không phải thức ăn hoặc không có trong túi)", new Color(1f, 0.4f, 0.4f), 3f);
        }
    }

    private void OpenDungeonEntrance()
    {
        DungeonEntranceUI entrance = DungeonEntranceUI.Instance != null ? DungeonEntranceUI.Instance : FindFirstObjectByType<DungeonEntranceUI>();
        if (entrance != null)
        {
            entrance.Open();
        }
        else if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadDungeon(1);
        }
    }
    #endregion

    #region 3. Contextual Interaction Prompt (Gợi ý & Nút tương tác khi đến gần)
    private void DrawContextualInteractionPrompt()
    {
        // Xác định đối tượng đang ở gần người chơi nhất
        string promptText = null;
        System.Action interactAction = null;

        // 0. Kiểm tra sảnh chờ vào Dungeon (Antechamber / Safe Entrance Room)
        WaveManager waveManager = WaveManager.Instance != null ? WaveManager.Instance : FindFirstObjectByType<WaveManager>();
        if (waveManager != null && waveManager.isAtEntrance)
        {
            promptText = "⚔️ [SPACE / ENTER] TIẾN VÀO ẢI 1 CỦA HẦM NGỤC";
            interactAction = () =>
            {
                if (DungeonEntranceArrowUI.Instance != null)
                {
                    DungeonEntranceArrowUI.Instance.OnArrowClicked();
                }
                else
                {
                    waveManager.ProceedFromEntrance();
                }
            };
        }

        // 1. Kiểm tra Rương nhà
        if (promptText == null)
        {
            if (cachedChest == null) cachedChest = HomeChest.Instance != null ? HomeChest.Instance : FindFirstObjectByType<HomeChest>();
            if (cachedChest != null && cachedChest.playerInRange && !cachedChest.isOpen)
            {
                promptText = "📦 [E] MỞ RƯƠNG CHỨA ĐỒ";
                interactAction = () => cachedChest.Interact();
            }
        }

        // 2. Kiểm tra Bếp nấu ăn
        if (promptText == null)
        {
            if (cachedKitchen == null) cachedKitchen = KitchenStation.Instance != null ? KitchenStation.Instance : FindFirstObjectByType<KitchenStation>();
            if (cachedKitchen != null && cachedKitchen.playerInRange && !cachedKitchen.isOpen)
            {
                promptText = "🍳 [E] NẤU ĂN (BẾP)";
                interactAction = () => cachedKitchen.Interact();
            }
        }

        // 3. Kiểm tra Giường ngủ
        if (promptText == null)
        {
            if (cachedInteraction == null) cachedInteraction = FindFirstObjectByType<PlayerInteraction>();
            if (cachedInteraction != null && cachedInteraction.currentBeds != null && cachedInteraction.currentBeds.Count > 0)
            {
                BedInteraction nearestBed = cachedInteraction.GetNearestBed();
                Transform playerT = cachedPlayerStats != null ? cachedPlayerStats.transform : null;
                if (nearestBed != null && playerT != null && nearestBed.IsPlayerInRange(playerT.position))
                {
                    promptText = "🛏️ [E] LÊN GIƯỜNG NGỦ ĐỂ QUA NGÀY";
                    interactAction = () =>
                    {
                        nearestBed.Interact();
                    };
                }
            }
        }

        // 4. Kiểm tra Tượng Thần
        if (promptText == null)
        {
            if (cachedStatue == null) cachedStatue = FindFirstObjectByType<Statue>();
            Transform playerT = cachedPlayerStats != null ? cachedPlayerStats.transform : null;
            if (cachedStatue != null && playerT != null)
            {
                float dist = Vector2.Distance(cachedStatue.transform.position, playerT.position);
                if (dist <= cachedStatue.interactDistance)
                {
                    promptText = "🗿 [E] CÚNG TẾ TƯỢNG THẦN";
                    interactAction = () => cachedStatue.Interact();
                }
            }
        }

        // 5. Kiểm tra Ô đất Nông Trại
        if (promptText == null && cachedInteraction != null && cachedInteraction.currentFarmPlots != null && cachedInteraction.currentFarmPlots.Count > 0)
        {
            FarmPlot nearestPlot = cachedInteraction.currentFarmPlots[0];
            if (nearestPlot != null)
            {
                if (nearestPlot.currentState == FarmPlot.State.Ready)
                {
                    promptText = "🌾 THU HOẠCH NÔNG SẢN";
                    interactAction = () => nearestPlot.Harvest();
                }
                else if (nearestPlot.currentState == FarmPlot.State.Empty)
                {
                    ItemData seed = InventoryButton.selectedItem;
                    if (seed != null && seed.itemType == ItemData.ItemType.Seed)
                    {
                        promptText = $"🌱 GIEO HẠT [{seed.itemName}]";
                        interactAction = () => nearestPlot.Planted();
                    }
                    else
                    {
                        promptText = "🌱 Ô ĐẤT TRỐNG (Hãy chọn hạt giống trong túi để trồng)";
                    }
                }
            }
        }

        // Nếu có tương tác, vẽ nút bấm nổi to rõ ràng ở giữa phía dưới màn hình
        if (!string.IsNullOrEmpty(promptText))
        {
            float btnWidth = 380;
            float btnHeight = 44;
            Rect btnRect = new Rect((Screen.width - btnWidth) / 2f, Screen.height - btnHeight - 25, btnWidth, btnHeight);

            if (GUI.Button(btnRect, promptText, btnInteract))
            {
                interactAction?.Invoke();
            }
        }
    }
    #endregion

    #region 4. Toast Notifications System
    private void DrawToastNotifications()
    {
        if (toasts.Count == 0) return;

        float toastWidth = 440;
        float startY = 16;

        for (int i = 0; i < toasts.Count; i++)
        {
            ToastMessage t = toasts[i];
            GUIStyle curToastStyle = new GUIStyle(toastStyle)
            {
                normal = { textColor = t.color }
            };

            Rect toastRect = new Rect((Screen.width - toastWidth) / 2f, startY, toastWidth, 48);
            GUI.Box(toastRect, t.text, curToastStyle);
            startY += 54;
        }
    }
    #endregion

    #region 5. Dev Cheats Panel Window (F1)
    private void DrawDevPanelWindow(int windowID)
    {
        GUILayout.Space(6);

        // Thanh chọn Tab
        GUILayout.BeginHorizontal();
        for (int i = 0; i < devTabs.Length; i++)
        {
            GUIStyle st = (selectedDevTab == i) ? tabActive : btnNormal;
            if (GUILayout.Button(devTabs[i], st, GUILayout.Height(28)))
            {
                selectedDevTab = i;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        switch (selectedDevTab)
        {
            case 0: DrawDevPlayerTab(); break;
            case 1: DrawDevTimeTab(); break;
            case 2: DrawDevFarmTab(); break;
            case 3: DrawDevInventoryTab(); break;
            case 4: DrawDevStatsTab(); break;
        }

        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        GUILayout.Label("<color=#888888><i>F1 hoặc nút trên góc để đóng menu này.</i></color>", labelStyle);
        if (GUILayout.Button("Đóng", btnNormal, GUILayout.Width(65)))
        {
            showDevPanel = false;
        }
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0, 0, 10000, 25));
    }

    private void DrawDevPlayerTab()
    {
        if (cachedPlayerStats == null) cachedPlayerStats = PlayerStats.Instance;
        if (cachedPlayerStats == null) return;

        GUILayout.Label("<b>HỒI MÁU & ĐỘ NO:</b>", headerStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+25 HP", btnNormal, GUILayout.Height(26))) cachedPlayerStats.Heal(25);
        if (GUILayout.Button("Full HP", btnNormal, GUILayout.Height(26))) cachedPlayerStats.Heal(cachedPlayerStats.maxHealth);
        if (GUILayout.Button("-25 HP", btnNormal, GUILayout.Height(26))) cachedPlayerStats.TakeDamage(25);
        if (GUILayout.Button("0 HP", btnNormal, GUILayout.Height(26))) cachedPlayerStats.TakeDamage(cachedPlayerStats.currentHealth);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+30 No", btnNormal, GUILayout.Height(26))) cachedPlayerStats.AddHunger(30);
        if (GUILayout.Button("No Căng", btnNormal, GUILayout.Height(26))) cachedPlayerStats.AddHunger(cachedPlayerStats.maxHunger);
        if (GUILayout.Button("-30 Đói", btnNormal, GUILayout.Height(26))) cachedPlayerStats.ReduceHunger(30);
        if (GUILayout.Button("Đói 0", btnNormal, GUILayout.Height(26))) cachedPlayerStats.ReduceHunger(cachedPlayerStats.currentHunger);
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        godMode = GUILayout.Toggle(godMode, " 🛡️ Bất tử (God Mode)", GUILayout.Width(170));
        infiniteHunger = GUILayout.Toggle(infiniteHunger, " 🍖 Không bao giờ đói", GUILayout.Width(170));
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        int buffCount = cachedPlayerStats.activeBuffs != null ? cachedPlayerStats.activeBuffs.Count : 0;
        GUILayout.Label($"<b>HIỆU ỨNG BUFF ĐANG CÓ ({buffCount}):</b>", headerStyle);
        if (buffCount > 0)
        {
            for (int i = 0; i < cachedPlayerStats.activeBuffs.Count; i++)
            {
                ActiveBuff b = cachedPlayerStats.activeBuffs[i];
                if (b != null && b.buff != null)
                {
                    string sign = b.buffValue > 0 ? "+" : "";
                    GUILayout.Label($" • {b.buff.buffName} {sign}{b.buffValue} ({b.remainingDuration} {b.buffDurationType})", labelStyle);
                }
            }
            if (GUILayout.Button("🧹 Xóa Toàn Bộ Buffs", btnNormal, GUILayout.Height(26)))
            {
                cachedPlayerStats.ClearTemporaryBuffs();
                ShowToast("Đã xóa sạch các hiệu ứng Buff tạm thời!", Color.gray);
            }
        }
        else
        {
            GUILayout.Label("<i>(Không có hiệu ứng buff nào đang kích hoạt)</i>", labelStyle);
        }
    }

    private void DrawDevTimeTab()
    {
        if (cachedDayManager == null) cachedDayManager = DayManager.Instance;
        if (cachedDayManager == null) return;

        GUILayout.Label($"<b>NGÀY HIỆN TẠI: NGÀY {cachedDayManager.currentDay}</b>", headerStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("⏭️ Qua 1 Ngày", btnNormal, GUILayout.Height(32))) cachedDayManager.AdvanceDay();
        if (GUILayout.Button("🛏️ Đi Ngủ (Chuyển ngày)", btnNormal, GUILayout.Height(32))) cachedDayManager.Sleep();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("<b>TỐC ĐỘ GAME (TIME SCALE):</b>", labelStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("⏸️ 0x", btnNormal, GUILayout.Height(28))) Time.timeScale = 0f;
        if (GUILayout.Button("1x (Chuẩn)", btnNormal, GUILayout.Height(28))) Time.timeScale = 1.0f;
        if (GUILayout.Button("2x (Nhanh)", btnNormal, GUILayout.Height(28))) Time.timeScale = 2.0f;
        if (GUILayout.Button("5x (Siêu tốc)", btnNormal, GUILayout.Height(28))) Time.timeScale = 5.0f;
        GUILayout.EndHorizontal();
    }

    private void DrawDevFarmTab()
    {
        FarmPlot[] plots = FindObjectsByType<FarmPlot>(FindObjectsSortMode.None);
        GUILayout.Label($"<b>Ô ĐẤT NÔNG TRẠI ({plots.Length} Ô):</b>", headerStyle);

        if (GUILayout.Button("⚡ Tăng Trưởng Tất Cả Cây (+1 Ngày)", btnNormal, GUILayout.Height(30)))
        {
            for (int i = 0; i < plots.Length; i++)
            {
                if (plots[i] != null && plots[i].currentState == FarmPlot.State.Planted) plots[i].NextDay();
            }
            ShowToast("🌾 Toàn bộ cây đã phát triển thêm 1 ngày!", Color.green);
        }

        if (GUILayout.Button("🌟 Làm Chín Toàn Bộ Cây Trồng Ngay", btnNormal, GUILayout.Height(30)))
        {
            for (int i = 0; i < plots.Length; i++)
            {
                if (plots[i] != null && plots[i].currentState == FarmPlot.State.Planted && plots[i].currentCrop != null)
                {
                    plots[i].currentState = FarmPlot.State.Ready;
                    if (plots[i].currentCrop.growthStages != null && plots[i].currentCrop.growthStages.Length > 0)
                    {
                        plots[i].currentStage = plots[i].currentCrop.growthStages.Length - 1;
                        if (plots[i].cropRenderer != null)
                        {
                            plots[i].cropRenderer.sprite = plots[i].currentCrop.growthStages[plots[i].currentStage];
                        }
                    }
                    plots[i].SavePlotData();
                }
            }
            ShowToast("✨ Toàn bộ cây trồng đã chín vàng!", Color.yellow);
        }

        if (GUILayout.Button("🧺 Thu Hoạch Hết Cây Đã Chín", btnNormal, GUILayout.Height(30)))
        {
            for (int i = 0; i < plots.Length; i++)
            {
                if (plots[i] != null && plots[i].currentState == FarmPlot.State.Ready) plots[i].Harvest();
            }
        }
    }

    private void DrawDevInventoryTab()
    {
        ItemContainer bag = cachedPlayerStats != null ? cachedPlayerStats.itemContainer : null;

        GUILayout.Label("<b>NHẬN NHANH VẬT PHẨM (KÈM THANH SLIDER ĐIỀU CHỈNH):</b>", labelStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Số lượng nhận: <b>x{devGiveAmount}</b>", labelStyle, GUILayout.Width(130));
        devGiveAmount = Mathf.RoundToInt(GUILayout.HorizontalSlider(devGiveAmount, 1, 99));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button($"+{devGiveAmount} Hạt Giống", btnNormal, GUILayout.Height(28))) GiveItemsOfType(bag, ItemData.ItemType.Seed, devGiveAmount);
        if (GUILayout.Button($"+{devGiveAmount} Thức Ăn", btnNormal, GUILayout.Height(28))) GiveItemsOfType(bag, ItemData.ItemType.Food, devGiveAmount);
        if (GUILayout.Button($"+{devGiveAmount} Nông Sản", btnNormal, GUILayout.Height(28))) GiveItemsOfType(bag, ItemData.ItemType.Produce, devGiveAmount);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        if (bag != null && bag.itemSlots != null)
        {
            // Luôn hiển thị thanh cuộn slider dọc (alwaysShowVertical = true) để tránh tràn khi có nhiều đồ
            invScrollPos = GUILayout.BeginScrollView(invScrollPos, false, true, GUILayout.Height(170));
            for (int i = 0; i < bag.itemSlots.Length; i++)
            {
                ItemSlot s = bag.itemSlots[i];
                if (s != null && s.itemData != null && s.amount > 0)
                {
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label($"<b>[{i + 1}]</b> {s.itemData.itemName} ({s.itemData.itemType}) x{s.amount}", labelStyle);
                    if (GUILayout.Button("+5", btnNormal, GUILayout.Width(35), GUILayout.Height(20))) s.amount += 5;
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
        }
    }

    private void GiveItemsOfType(ItemContainer container, ItemData.ItemType type, int count)
    {
        if (container == null) return;
        ItemData[] all = GameAssetHelper.LoadAll<ItemData>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].itemType == type) container.AddItem(all[i], count);
        }
        ShowToast($"🎁 Đã nhận x{count} mỗi loại {type}!", Color.cyan);
    }

    private void DrawDevStatsTab()
    {
        ProgressionManager prog = ProgressionManager.Instance;
        if (prog != null)
        {
            GUILayout.Label($"<b>CHỈ SỐ VĨNH VIỄN:</b> HP: +{prog.permanentMaxHP} | ATK: +{prog.permanentATK} | DEF: +{prog.permanentDEF}", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+5 ATK", btnNormal, GUILayout.Height(26))) { prog.permanentATK += 5; cachedPlayerStats?.RefreshStatsFromProgression(); }
            if (GUILayout.Button("+5 DEF", btnNormal, GUILayout.Height(26))) { prog.permanentDEF += 5; cachedPlayerStats?.RefreshStatsFromProgression(); }
            if (GUILayout.Button("+50 Max HP", btnNormal, GUILayout.Height(26))) { prog.permanentMaxHP += 50; cachedPlayerStats?.RefreshStatsFromProgression(); }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        GUILayout.Label("<b>TEST UI TỔNG KẾT DUNGEON (EXPEDITION SUMMARY):</b>", labelStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("🏆 Tổng Kết (Thắng)", btnNormal, GUILayout.Height(28)))
        {
            DungeonSummaryUI.EnsureInstance()?.Show(DungeonSummaryUI.ExpeditionResult.Victory);
        }
        if (GUILayout.Button("🏕️ Tổng Kết (Rút lui)", btnNormal, GUILayout.Height(28)))
        {
            DungeonSummaryUI.EnsureInstance()?.Show(DungeonSummaryUI.ExpeditionResult.Retreat);
        }
        if (GUILayout.Button("💀 Tổng Kết (Tử trận)", btnNormal, GUILayout.Height(28)))
        {
            DungeonSummaryUI.EnsureInstance()?.Show(DungeonSummaryUI.ExpeditionResult.Defeat);
        }
        GUILayout.EndHorizontal();
    }
    #endregion
}
