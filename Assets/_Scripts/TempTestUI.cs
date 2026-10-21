using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// TEMPORARY GAMEPLAY UI (DEV/FALLBACK HUD, QUICK ACTION BUTTONS, SMART PROMPTS & TEST PANEL)
/// =========================================================================================
/// Includes:
/// 1. REAL-TIME HUD: Health bar (HP), Hunger bar, Day/Phase Clock, Active Buffs.
/// 2. SCREEN SHORTCUT BUTTONS:
///    - [Bag]: Open/close inventory
///    - [Eat / Consume]: Eat currently held food item (restores HP & Hunger)
///    - [Sleep]: Sleep to transition to next day
///    - [Dungeon Portal]: Open dungeon floor selection panel
/// 3. SMART INTERACTION BUTTONS (Context-sensitive when near interactables):
///    - Near Bed      -> [Sleep (E)]
///    - Near Chest    -> [Open Chest (E)]
///    - Near Kitchen  -> [Cook Food (E)]
///    - Near Statue   -> [Make Offering (E)]
///    - Near FarmPlot -> [Harvest] or [Plant Seed]
/// 4. TOAST NOTIFICATION SYSTEM:
///    - New day banner, hunger warnings, cooking results, harvest notifications, offering buffs.
/// 5. TEST & CHEAT PANEL (F1 or `): Quick stat adjustment, time scale, dev shortcuts.
/// 
/// WHEN OFFICIAL GRAPHICAL UI IS COMPLETE:
/// - Simply delete this file (TempTestUI.cs and TempTestUI.cs.meta).
/// - The project will remain 100% clean with no missing component errors.
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
    private bool showDevPanel = false; // Toggle cheat menu via F1
    private int selectedDevTab = 0;
    private readonly string[] devTabs = new string[] { "[*] Player", "[*] Day/Time", "[*] Farm", "[*] Bag", "[*] Stats" };
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
        ShowToast($"[*] GOOD MORNING: DAY {day} HAS BEGUN!\nCrops have grown by 1 day.", new Color(1f, 0.9f, 0.3f), 4.5f);
    }

    private void HandlePlayerDeath()
    {
        ShowToast("[!] YOU HAVE COLLAPSED!", new Color(1f, 0.3f, 0.3f), 4f);
    }

    private void HandleOfferingSuccess(Offering offering)
    {
        ShowToast("[*] OFFERING ACCEPTED BY THE SHRINE!\nYou have received a permanent blessing.", new Color(0.4f, 1f, 0.5f), 4f);
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
        // Toggle Dev Cheats panel shortcut
        if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.BackQuote))
        {
            showDevPanel = !showDevPanel;
        }

        // Hunger warning check
        if (cachedPlayerStats == null) cachedPlayerStats = PlayerStats.Instance;
        if (cachedPlayerStats != null && cachedPlayerStats.maxHunger > 0)
        {
            if (cachedPlayerStats.currentHunger <= 0)
            {
                if (Time.time >= nextHungerWarningTime)
                {
                    nextHungerWarningTime = Time.time + 10f;
                    ShowToast("[!] YOU ARE STARVING (Hunger = 0)! Eat food immediately.", new Color(1f, 0.4f, 0.2f), 3.5f);
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

        // Shortcut to proceed into dungeon floor 1 when at entrance
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

        // Clean up expired toasts
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

        // 1. MAIN STATUS BAR (PLAYER HUD: HP, HUNGER, DAY, BUFFS) - TOP LEFT
        DrawPlayerHUD();

        // 2. QUICK ACTION BUTTONS - TOP RIGHT
        DrawQuickActionButtons();

        // 3. CONTEXTUAL INTERACTION PROMPT (WHEN NEAR CHEST, KITCHEN, BED, STATUE, PLOT) - BOTTOM CENTER
        DrawContextualInteractionPrompt();

        // 4. TOAST NOTIFICATIONS SYSTEM - TOP CENTER
        DrawToastNotifications();

        // 5. DEV CHEAT WINDOW (PRESS F1 TO TOGGLE)
        if (showDevPanel)
        {
            devWindowRect = GUI.Window(888123, devWindowRect, DrawDevPanelWindow, "DEV TEST & CHEAT MENU (F1 Toggle)", boxStyle);
        }
    }

    #region 1. Player HUD (Health, Hunger, Day, Buffs)
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
        GUILayout.Label($"<b>DAY {day}</b> | {GetTimeOfDayLabel()}", headerStyle);

        if (cachedPlayerStats != null)
        {
            DrawProgressBar("Health (HP)", cachedPlayerStats.currentHealth, cachedPlayerStats.maxHealth, barFillHp);
            DrawProgressBar("Hunger", cachedPlayerStats.currentHunger, cachedPlayerStats.maxHunger, barFillHunger);

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
                            FoodData.BuffDurationType.Turn => "turns",
                            FoodData.BuffDurationType.Combat => "battles",
                            FoodData.BuffDurationType.Floor => "floors",
                            _ => b.buffDurationType.ToString()
                        };
                        string color = b.buffValue >= 0 ? "#68D391" : "#FC8181";
                        GUILayout.Label($"* <color={color}><b>{b.buff.buffName}</b> {sign}{b.buffValue} ({b.remainingDuration} {durUnit})</color>", labelStyle);
                    }
                }
            }
        }
        else
        {
            GUILayout.Label("<i>(Loading player data...)</i>", labelStyle);
        }

        GUILayout.EndArea();
    }

    private string GetTimeOfDayLabel()
    {
        // If time-of-day cycle exists
        return "Daytime";
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

    #region 2. Action Buttons (Screen Action Buttons)
    private void DrawQuickActionButtons()
    {
        float barWidth = 230;
        Rect areaRect = new Rect(Screen.width - barWidth - 12, 12, barWidth, 180);

        GUILayout.BeginArea(areaRect);

        // 1. Open Backpack Button
        if (GUILayout.Button("Backpack (Key I)", btnAction, GUILayout.Height(34)))
        {
            ToggleInventory();
        }

        // 2. Eat / Use currently held item (or first food in bag)
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
                ? $"Eat [{selected.itemName}]"
                : $"Hold [{selected.itemName}]";

            if (GUILayout.Button(itemActionName, btnInteract, GUILayout.Height(34)))
            {
                TryEatSelectedItem(selected);
            }
        }

        // 3. Sleep button to advance day
        if (GUILayout.Button("Sleep (Advance Day)", btnNormal, GUILayout.Height(30)))
        {
            if (cachedDayManager == null) cachedDayManager = DayManager.Instance;
            if (cachedDayManager != null)
            {
                cachedDayManager.Sleep();
                ShowToast("You went to bed... Advancing to a new day!", new Color(0.7f, 0.85f, 1f));
            }
        }

        // 4. Enter Dungeon / Select Floor button
        if (GUILayout.Button("Dungeon Portal", btnNormal, GUILayout.Height(30)))
        {
            OpenDungeonEntrance();
        }

        // 5. Toggle Cheat Menu button
        if (GUILayout.Button(showDevPanel ? "Close Cheat Menu (F1)" : "Cheat Menu (F1)", btnNormal, GUILayout.Height(26)))
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
            ShowToast("YOU ARE FULL! Cannot eat more right now.", new Color(1f, 0.8f, 0.3f), 2.5f);
            return;
        }

        bool ate = cachedPlayerStats.Eat(item);
        if (ate)
        {
            ShowToast($"Ate {item.itemName}! Restored vitality.", new Color(0.4f, 1f, 0.4f), 3f);
        }
        else
        {
            ShowToast($"Cannot eat {item.itemName}! (Item is not edible or not in inventory)", new Color(1f, 0.4f, 0.4f), 3f);
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

    #region 3. Contextual Interaction Prompt (Interaction Hints & Buttons When Near)
    private void DrawContextualInteractionPrompt()
    {
        // Determine entity closest to player
        string promptText = null;
        System.Action interactAction = null;

        // 0. Check Dungeon Entrance Antechamber (Safe Entrance Room)
        WaveManager waveManager = WaveManager.Instance != null ? WaveManager.Instance : FindFirstObjectByType<WaveManager>();
        if (waveManager != null && waveManager.isAtEntrance)
        {
            promptText = "[SPACE / ENTER] ADVANCE TO STAGE 1 OF DUNGEON";
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

        // 1. Check Storage Chest
        if (promptText == null)
        {
            if (cachedChest == null) cachedChest = HomeChest.Instance != null ? HomeChest.Instance : FindFirstObjectByType<HomeChest>();
            if (cachedChest != null && cachedChest.playerInRange && !cachedChest.isOpen)
            {
                promptText = "[E] OPEN STORAGE CHEST";
                interactAction = () => cachedChest.Interact();
            }
        }

        // 2. Check Kitchen Station
        if (promptText == null)
        {
            if (cachedKitchen == null) cachedKitchen = KitchenStation.Instance != null ? KitchenStation.Instance : FindFirstObjectByType<KitchenStation>();
            if (cachedKitchen != null && cachedKitchen.playerInRange && !cachedKitchen.isOpen)
            {
                promptText = "[E] COOK (KITCHEN)";
                interactAction = () => cachedKitchen.Interact();
            }
        }

        // 3. Check Bed
        if (promptText == null)
        {
            if (cachedInteraction == null) cachedInteraction = FindFirstObjectByType<PlayerInteraction>();
            if (cachedInteraction != null && cachedInteraction.currentBeds != null && cachedInteraction.currentBeds.Count > 0)
            {
                BedInteraction nearestBed = cachedInteraction.GetNearestBed();
                Transform playerT = cachedPlayerStats != null ? cachedPlayerStats.transform : null;
                if (nearestBed != null && playerT != null && nearestBed.IsPlayerInRange(playerT.position))
                {
                    promptText = "[E] SLEEP TO ADVANCE DAY";
                    interactAction = () =>
                    {
                        nearestBed.Interact();
                    };
                }
            }
        }

        // 4. Check Divine Statue
        if (promptText == null)
        {
            if (cachedStatue == null) cachedStatue = FindFirstObjectByType<Statue>();
            Transform playerT = cachedPlayerStats != null ? cachedPlayerStats.transform : null;
            if (cachedStatue != null && playerT != null)
            {
                float dist = Vector2.Distance(cachedStatue.transform.position, playerT.position);
                if (dist <= cachedStatue.interactDistance)
                {
                    promptText = "[E] MAKE OFFERING AT STATUE";
                    interactAction = () => cachedStatue.Interact();
                }
            }
        }

        // 5. Check Farm Plot
        if (promptText == null && cachedInteraction != null && cachedInteraction.currentFarmPlots != null && cachedInteraction.currentFarmPlots.Count > 0)
        {
            FarmPlot nearestPlot = cachedInteraction.currentFarmPlots[0];
            if (nearestPlot != null)
            {
                if (nearestPlot.currentState == FarmPlot.State.Ready)
                {
                    promptText = "HARVEST CROP";
                    interactAction = () => nearestPlot.Harvest();
                }
                else if (nearestPlot.currentState == FarmPlot.State.Empty)
                {
                    ItemData seed = InventoryButton.selectedItem;
                    if (seed != null && seed.itemType == ItemData.ItemType.Seed)
                    {
                        promptText = $"PLANT SEED [{seed.itemName}]";
                        interactAction = () => nearestPlot.Planted();
                    }
                    else
                    {
                        promptText = "EMPTY PLOT (Select a seed in inventory to plant)";
                    }
                }
            }
        }

        // Draw prominent interaction prompt button at bottom center of screen
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

        // Tab selector
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
        GUILayout.Label("<color=#888888><i>F1 or top-corner button to close this menu.</i></color>", labelStyle);
        if (GUILayout.Button("Close", btnNormal, GUILayout.Width(65)))
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

        GUILayout.Label("<b>HEALTH & HUNGER:</b>", headerStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+25 HP", btnNormal, GUILayout.Height(26))) cachedPlayerStats.Heal(25);
        if (GUILayout.Button("Full HP", btnNormal, GUILayout.Height(26))) cachedPlayerStats.Heal(cachedPlayerStats.maxHealth);
        if (GUILayout.Button("-25 HP", btnNormal, GUILayout.Height(26))) cachedPlayerStats.TakeDamage(25);
        if (GUILayout.Button("0 HP", btnNormal, GUILayout.Height(26))) cachedPlayerStats.TakeDamage(cachedPlayerStats.currentHealth);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+30 Hunger", btnNormal, GUILayout.Height(26))) cachedPlayerStats.AddHunger(30);
        if (GUILayout.Button("Full Hunger", btnNormal, GUILayout.Height(26))) cachedPlayerStats.AddHunger(cachedPlayerStats.maxHunger);
        if (GUILayout.Button("-30 Hunger", btnNormal, GUILayout.Height(26))) cachedPlayerStats.ReduceHunger(30);
        if (GUILayout.Button("0 Hunger", btnNormal, GUILayout.Height(26))) cachedPlayerStats.ReduceHunger(cachedPlayerStats.currentHunger);
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        godMode = GUILayout.Toggle(godMode, " God Mode (Invincible)", GUILayout.Width(170));
        infiniteHunger = GUILayout.Toggle(infiniteHunger, " Infinite Hunger", GUILayout.Width(170));
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        int buffCount = cachedPlayerStats.activeBuffs != null ? cachedPlayerStats.activeBuffs.Count : 0;
        GUILayout.Label($"<b>ACTIVE BUFFS ({buffCount}):</b>", headerStyle);
        if (buffCount > 0)
        {
            for (int i = 0; i < cachedPlayerStats.activeBuffs.Count; i++)
            {
                ActiveBuff b = cachedPlayerStats.activeBuffs[i];
                if (b != null && b.buff != null)
                {
                    string sign = b.buffValue > 0 ? "+" : "";
                    GUILayout.Label($" * {b.buff.buffName} {sign}{b.buffValue} ({b.remainingDuration} {b.buffDurationType})", labelStyle);
                }
            }
            if (GUILayout.Button("Clear All Buffs", btnNormal, GUILayout.Height(26)))
            {
                cachedPlayerStats.ClearTemporaryBuffs();
                ShowToast("Cleared all temporary buffs!", Color.gray);
            }
        }
        else
        {
            GUILayout.Label("<i>(No active buffs)</i>", labelStyle);
        }
    }

    private void DrawDevTimeTab()
    {
        if (cachedDayManager == null) cachedDayManager = DayManager.Instance;
        if (cachedDayManager == null) return;

        GUILayout.Label($"<b>CURRENT DAY: DAY {cachedDayManager.currentDay}</b>", headerStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Advance 1 Day", btnNormal, GUILayout.Height(32))) cachedDayManager.AdvanceDay();
        if (GUILayout.Button("Sleep (Next Day)", btnNormal, GUILayout.Height(32))) cachedDayManager.Sleep();
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("<b>GAME SPEED (TIME SCALE):</b>", labelStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("0x (Pause)", btnNormal, GUILayout.Height(28))) Time.timeScale = 0f;
        if (GUILayout.Button("1x (Normal)", btnNormal, GUILayout.Height(28))) Time.timeScale = 1.0f;
        if (GUILayout.Button("2x (Fast)", btnNormal, GUILayout.Height(28))) Time.timeScale = 2.0f;
        if (GUILayout.Button("5x (Hyper)", btnNormal, GUILayout.Height(28))) Time.timeScale = 5.0f;
        GUILayout.EndHorizontal();
    }

    private void DrawDevFarmTab()
    {
        FarmPlot[] plots = FindObjectsByType<FarmPlot>(FindObjectsSortMode.None);
        GUILayout.Label($"<b>FARM PLOTS ({plots.Length} Plots):</b>", headerStyle);

        if (GUILayout.Button("Grow All Crops (+1 Day)", btnNormal, GUILayout.Height(30)))
        {
            for (int i = 0; i < plots.Length; i++)
            {
                if (plots[i] != null && plots[i].currentState == FarmPlot.State.Planted) plots[i].NextDay();
            }
            ShowToast("All crops progressed by 1 day!", Color.green);
        }

        if (GUILayout.Button("Instantly Ripen All Crops", btnNormal, GUILayout.Height(30)))
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
            ShowToast("All planted crops are fully ripe!", Color.yellow);
        }

        if (GUILayout.Button("Harvest All Ripe Crops", btnNormal, GUILayout.Height(30)))
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

        GUILayout.Label("<b>QUICK GIVE ITEMS (SLIDER CONTROL):</b>", labelStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Give Amount: <b>x{devGiveAmount}</b>", labelStyle, GUILayout.Width(130));
        devGiveAmount = Mathf.RoundToInt(GUILayout.HorizontalSlider(devGiveAmount, 1, 99));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button($"+{devGiveAmount} Seeds", btnNormal, GUILayout.Height(28))) GiveItemsOfType(bag, ItemData.ItemType.Seed, devGiveAmount);
        if (GUILayout.Button($"+{devGiveAmount} Food", btnNormal, GUILayout.Height(28))) GiveItemsOfType(bag, ItemData.ItemType.Food, devGiveAmount);
        if (GUILayout.Button($"+{devGiveAmount} Produce", btnNormal, GUILayout.Height(28))) GiveItemsOfType(bag, ItemData.ItemType.Produce, devGiveAmount);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        if (bag != null && bag.itemSlots != null)
        {
            // Always show vertical scrollbar (alwaysShowVertical = true) to prevent overflow
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
        ShowToast($"Granted x{count} of each {type}!", Color.cyan);
    }

    private void DrawDevStatsTab()
    {
        ProgressionManager prog = ProgressionManager.Instance;
        if (prog != null)
        {
            GUILayout.Label($"<b>PERMANENT STATS:</b> HP: +{prog.permanentMaxHP} | ATK: +{prog.permanentATK} | DEF: +{prog.permanentDEF}", labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+5 ATK", btnNormal, GUILayout.Height(26))) { prog.permanentATK += 5; cachedPlayerStats?.RefreshStatsFromProgression(); }
            if (GUILayout.Button("+5 DEF", btnNormal, GUILayout.Height(26))) { prog.permanentDEF += 5; cachedPlayerStats?.RefreshStatsFromProgression(); }
            if (GUILayout.Button("+50 Max HP", btnNormal, GUILayout.Height(26))) { prog.permanentMaxHP += 50; cachedPlayerStats?.RefreshStatsFromProgression(); }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        GUILayout.Label("<b>TEST DUNGEON EXPEDITION SUMMARY:</b>", labelStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Summary (Victory)", btnNormal, GUILayout.Height(28)))
        {
            DungeonSummaryUI.EnsureInstance()?.Show(DungeonSummaryUI.ExpeditionResult.Victory);
        }
        if (GUILayout.Button("Summary (Retreat)", btnNormal, GUILayout.Height(28)))
        {
            DungeonSummaryUI.EnsureInstance()?.Show(DungeonSummaryUI.ExpeditionResult.Retreat);
        }
        if (GUILayout.Button("Summary (Defeat)", btnNormal, GUILayout.Height(28)))
        {
            DungeonSummaryUI.EnsureInstance()?.Show(DungeonSummaryUI.ExpeditionResult.Defeat);
        }
        GUILayout.EndHorizontal();
    }
    #endregion
}
