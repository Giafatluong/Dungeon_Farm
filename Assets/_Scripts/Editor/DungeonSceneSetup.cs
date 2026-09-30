#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;

[InitializeOnLoad]
public static class DungeonSceneSetup
{
    private const string INVENTORY_ASSET_PATH = "Assets/_Assets/ScriptableObjects/Inventory.asset";
    private const string FLOOR01_ASSET_PATH = "Assets/_Assets/ScriptableObjects/Floor/Floor_01.asset";
    private const string HEALTHBAR_PREFAB_PATH = "Assets/_Assets/_Prefabs/HealthBar.prefab";
    private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";
    private const string AUTO_SETUP_KEY = "DungeonCombatSetup_Auto_V3";

    static DungeonSceneSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SessionState.GetBool(AUTO_SETUP_KEY, false))
            {
                SessionState.SetBool(AUTO_SETUP_KEY, true);
                SetupFullDungeonScene(silent: true);
            }
        };
    }

    [MenuItem("Tools/Combat Setup/Setup Full Dungeon Scene", false, 10)]
    public static void ManualSetupDungeonScene()
    {
        SetupFullDungeonScene(silent: false);
    }

    public static void SetupFullDungeonScene(bool silent = false)
    {
        // 1. Mở Scene Dungeon nếu chưa mở
        if (EditorSceneManager.GetActiveScene().name != "Dungeon")
        {
            if (!silent)
            {
                if (!EditorUtility.DisplayDialog("Mở Scene Dungeon", "Bạn có muốn mở scene Dungeon để cấu hình full combat không?", "Mở Dungeon", "Hủy"))
                {
                    return;
                }
            }
            EditorSceneManager.OpenScene("Assets/_Scenes/Dungeon.unity");
        }

        ItemContainer inventory = AssetDatabase.LoadAssetAtPath<ItemContainer>(INVENTORY_ASSET_PATH);
        FloorData floor01 = AssetDatabase.LoadAssetAtPath<FloorData>(FLOOR01_ASSET_PATH);
        GameObject healthBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HEALTHBAR_PREFAB_PATH);
        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);

        // 2. Cấu hình Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            mainCam = camGO.GetComponent<Camera>();
            camGO.tag = "MainCamera";
        }
        mainCam.transform.position = new Vector3(0, 0, -10);
        mainCam.orthographic = true;
        mainCam.orthographicSize = 5f;
        mainCam.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 1f);

        // Thêm Physics2DRaycaster để click vào enemy hoạt động chuẩn xác với UI
        UnityEngine.EventSystems.Physics2DRaycaster raycaster = mainCam.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
        if (raycaster == null) raycaster = mainCam.gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();

        // 3. Tạo / Cấu hình Player trong Dungeon (đứng phía bên trái, hướng về quái bên phải)
        GameObject playerGO = GameObject.Find("PlayerCombat");
        if (playerGO == null) playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null)
        {
            playerGO = new GameObject("PlayerCombat");
            Undo.RegisterCreatedObjectUndo(playerGO, "Create PlayerCombat");
        }
        playerGO.name = "PlayerCombat";
        playerGO.tag = "Player";
        playerGO.layer = 6;
        playerGO.transform.position = new Vector3(-3.5f, 0f, 0f);
        playerGO.transform.localScale = Vector3.one;

        // SpriteRenderer
        SpriteRenderer sr = playerGO.GetComponent<SpriteRenderer>();
        if (sr == null) sr = playerGO.AddComponent<SpriteRenderer>();
        sr.flipX = false;
        sr.sortingOrder = 10;

        // Gán material URP Lit
        Material litMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        if (litMat != null) sr.sharedMaterial = litMat;

        // Gán sprite thực tế từ bộ Kenmi Cute Fantasy RPG Player
        string playerSpritePath = "Assets/Kenmi/Cute Fantasy RPG - 16x16 top down pixel art asset pack/Sprites/Player/Player.png";
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(playerSpritePath);
        Sprite playerSprite = null;
        if (subAssets != null)
        {
            foreach (var asset in subAssets)
            {
                if (asset is Sprite sp && (sp.name == "Player_0" || sp.name.StartsWith("Player_")))
                {
                    playerSprite = sp;
                    break;
                }
            }
        }
        if (playerSprite != null) sr.sprite = playerSprite;

        // Animator
        Animator anim = playerGO.GetComponent<Animator>();
        if (anim == null) anim = playerGO.AddComponent<Animator>();
        RuntimeAnimatorController animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Assets/Animations/Player/Player.controller");
        if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;

        // PlayerCombatVisual
        PlayerCombatVisual visual = playerGO.GetComponent<PlayerCombatVisual>();
        if (visual == null) visual = playerGO.AddComponent<PlayerCombatVisual>();
        visual.FaceRight();

        // BoxCollider2D
        BoxCollider2D pCol = playerGO.GetComponent<BoxCollider2D>();
        if (pCol == null) pCol = playerGO.AddComponent<BoxCollider2D>();
        pCol.size = new Vector2(1f, 1.5f);
        pCol.isTrigger = false;

        // PlayerStats
        PlayerStats playerStats = playerGO.GetComponent<PlayerStats>();
        if (playerStats == null) playerStats = playerGO.AddComponent<PlayerStats>();
        playerStats.maxHealth = 20;
        playerStats.currentHealth = 20;
        playerStats.ATK = 5;
        playerStats.DEF = 0;
        playerStats.speed = 10;
        playerStats.maxAP = 3;
        playerStats.currentAP = 3;
        playerStats.maxHunger = 100;
        playerStats.currentHunger = 100;
        playerStats.itemContainer = inventory;

        // 4. Tạo DungeonManager & Core Systems
        GameObject dmGO = GameObject.Find("DungeonManager");
        if (dmGO == null)
        {
            dmGO = new GameObject("DungeonManager");
            Undo.RegisterCreatedObjectUndo(dmGO, "Create DungeonManager");
        }

        WaveManager waveManager = dmGO.GetComponent<WaveManager>();
        if (waveManager == null) waveManager = dmGO.AddComponent<WaveManager>();

        CombatManager combatManager = dmGO.GetComponent<CombatManager>();
        if (combatManager == null) combatManager = dmGO.AddComponent<CombatManager>();

        TurnManager turnManager = dmGO.GetComponent<TurnManager>();
        if (turnManager == null) turnManager = dmGO.AddComponent<TurnManager>();

        SpeedManager speedManager = dmGO.GetComponent<SpeedManager>();
        if (speedManager == null) speedManager = dmGO.AddComponent<SpeedManager>();

        EnemyTargetSelector targetSelector = dmGO.GetComponent<EnemyTargetSelector>();
        if (targetSelector == null) targetSelector = dmGO.AddComponent<EnemyTargetSelector>();

        HealthBarManager healthBarManager = dmGO.GetComponent<HealthBarManager>();
        if (healthBarManager == null) healthBarManager = dmGO.AddComponent<HealthBarManager>();

        FloorGenerator floorGenerator = dmGO.GetComponent<FloorGenerator>();
        if (floorGenerator == null) floorGenerator = dmGO.AddComponent<FloorGenerator>();

        // 5. Tạo Camp & MerchantEvent
        GameObject campGO = GameObject.Find("Camp");
        if (campGO == null)
        {
            campGO = new GameObject("Camp");
            campGO.transform.SetParent(dmGO.transform);
        }
        Camp camp = campGO.GetComponent<Camp>();
        if (camp == null) camp = campGO.AddComponent<Camp>();

        GameObject merchantGO = GameObject.Find("MerchantEvent");
        if (merchantGO == null)
        {
            merchantGO = new GameObject("MerchantEvent");
            merchantGO.transform.SetParent(dmGO.transform);
        }
        MerchantEvent merchantEvent = merchantGO.GetComponent<MerchantEvent>();
        if (merchantEvent == null) merchantEvent = merchantGO.AddComponent<MerchantEvent>();

        // 6. Spawn Point & Combat Positions
        GameObject spawnPointGO = GameObject.Find("EnemySpawnPoint");
        if (spawnPointGO == null)
        {
            spawnPointGO = new GameObject("EnemySpawnPoint");
            spawnPointGO.transform.position = new Vector3(8f, 0f, 0f);
            spawnPointGO.transform.SetParent(dmGO.transform);
        }

        GameObject posRoot = GameObject.Find("CombatPositions");
        if (posRoot == null)
        {
            posRoot = new GameObject("CombatPositions");
            posRoot.transform.SetParent(dmGO.transform);
        }

        Transform[] combatPositions = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            string posName = $"Position_{i + 1}";
            Transform pos = posRoot.transform.Find(posName);
            if (pos == null)
            {
                GameObject pG = new GameObject(posName);
                pG.transform.SetParent(posRoot.transform);
                pos = pG.transform;
            }
            pos.position = new Vector3(2.5f + (i * 2.2f), 0f, 0f);
            combatPositions[i] = pos;
        }

        // 7. Tạo HealthBarCanvas (World Space Canvas)
        GameObject hbCanvasGO = GameObject.Find("HealthBarCanvas");
        if (hbCanvasGO == null)
        {
            hbCanvasGO = new GameObject("HealthBarCanvas");
            Canvas c = hbCanvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.worldCamera = mainCam;
            hbCanvasGO.AddComponent<CanvasScaler>();
            hbCanvasGO.AddComponent<GraphicRaycaster>();
        }

        // 8. Tạo EventSystem (với InputSystemUIInputModule)
        GameObject eventSysGO = GameObject.Find("EventSystem");
        if (eventSysGO == null)
        {
            eventSysGO = new GameObject("EventSystem");
            eventSysGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            InputSystemUIInputModule inputMod = eventSysGO.AddComponent<InputSystemUIInputModule>();
            if (inputActions != null)
            {
                inputMod.actionsAsset = inputActions;
            }
        }
        else
        {
            InputSystemUIInputModule inputMod = eventSysGO.GetComponent<InputSystemUIInputModule>();
            if (inputMod == null) inputMod = eventSysGO.AddComponent<InputSystemUIInputModule>();
            if (inputActions != null) inputMod.actionsAsset = inputActions;
        }

        // 9. Tạo Combat Canvas & Full Combat UI Hierarchy
        GameObject combatCanvasGO = GameObject.Find("CombatCanvas");
        if (combatCanvasGO == null)
        {
            combatCanvasGO = new GameObject("CombatCanvas");
        }

        Canvas canvas = combatCanvasGO.GetComponent<Canvas>();
        if (canvas == null) canvas = combatCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = combatCanvasGO.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = combatCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (combatCanvasGO.GetComponent<GraphicRaycaster>() == null)
        {
            combatCanvasGO.AddComponent<GraphicRaycaster>();
        }

        CombatUI combatUI = combatCanvasGO.GetComponent<CombatUI>();
        if (combatUI == null) combatUI = combatCanvasGO.AddComponent<CombatUI>();

        // Xây dựng cây UI hoàn chỉnh
        BuildCombatUIHierarchy(combatCanvasGO, combatUI, playerGO.transform);

        // 10. Gán tham chiếu bằng SerializedObject
        SerializedObject soHB = new SerializedObject(healthBarManager);
        soHB.FindProperty("healthBarPrefab").objectReferenceValue = healthBarPrefab;
        soHB.FindProperty("healthBarCanvas").objectReferenceValue = hbCanvasGO.transform;
        soHB.FindProperty("playerStats").objectReferenceValue = playerStats;
        soHB.ApplyModifiedProperties();

        SerializedObject soSpeed = new SerializedObject(speedManager);
        soSpeed.FindProperty("playerStats").objectReferenceValue = playerStats;
        soSpeed.ApplyModifiedProperties();

        SerializedObject soCombat = new SerializedObject(combatManager);
        soCombat.FindProperty("playerStats").objectReferenceValue = playerStats;
        soCombat.FindProperty("targetSelector").objectReferenceValue = targetSelector;
        soCombat.FindProperty("speedManager").objectReferenceValue = speedManager;
        soCombat.FindProperty("turnManager").objectReferenceValue = turnManager;
        soCombat.FindProperty("healthBarManager").objectReferenceValue = healthBarManager;
        soCombat.FindProperty("waveManager").objectReferenceValue = waveManager;
        soCombat.ApplyModifiedProperties();

        SerializedObject soTurn = new SerializedObject(turnManager);
        soTurn.FindProperty("speedManager").objectReferenceValue = speedManager;
        soTurn.FindProperty("combatManager").objectReferenceValue = combatManager;
        soTurn.ApplyModifiedProperties();

        SerializedObject soWave = new SerializedObject(waveManager);
        soWave.FindProperty("enemySpawnPoint").objectReferenceValue = spawnPointGO.transform;
        soWave.FindProperty("combatManager").objectReferenceValue = combatManager;
        soWave.FindProperty("healthBarManager").objectReferenceValue = healthBarManager;
        soWave.FindProperty("targetSelector").objectReferenceValue = targetSelector;
        soWave.FindProperty("camp").objectReferenceValue = camp;
        soWave.FindProperty("merchantEvent").objectReferenceValue = merchantEvent;
        soWave.FindProperty("floorGenerator").objectReferenceValue = floorGenerator;

        SerializedProperty propPositions = soWave.FindProperty("combatPositions");
        propPositions.arraySize = 3;
        for (int i = 0; i < 3; i++)
        {
            propPositions.GetArrayElementAtIndex(i).objectReferenceValue = combatPositions[i];
        }

        if (floor01 != null)
        {
            soWave.FindProperty("floorData").objectReferenceValue = floor01;
        }
        soWave.ApplyModifiedProperties();

        SerializedObject soCamp = new SerializedObject(camp);
        soCamp.FindProperty("playerStats").objectReferenceValue = playerStats;
        soCamp.FindProperty("itemContainer").objectReferenceValue = inventory;
        soCamp.FindProperty("waveManager").objectReferenceValue = waveManager;
        soCamp.ApplyModifiedProperties();

        // Gán references cho CombatUI bằng SerializedObject
        SerializedObject soUI = new SerializedObject(combatUI);
        soUI.FindProperty("combatManager").objectReferenceValue = combatManager;
        soUI.FindProperty("playerStats").objectReferenceValue = playerStats;
        soUI.FindProperty("turnManager").objectReferenceValue = turnManager;
        soUI.FindProperty("targetSelector").objectReferenceValue = targetSelector;
        soUI.FindProperty("waveManager").objectReferenceValue = waveManager;
        soUI.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("✅ [Combat Setup] Đã cấu hình FULL COMBAT thành công trong scene Dungeon!");
    }

    private static void BuildCombatUIHierarchy(GameObject canvasGO, CombatUI ui, Transform playerTransform)
    {
        // 1. Top Turn Banner
        GameObject turnBannerGO = GetOrCreateChild(canvasGO, "TurnBanner");
        RectTransform bannerRT = turnBannerGO.GetComponent<RectTransform>();
        bannerRT.anchorMin = new Vector2(0.5f, 1f);
        bannerRT.anchorMax = new Vector2(0.5f, 1f);
        bannerRT.pivot = new Vector2(0.5f, 1f);
        bannerRT.anchoredPosition = new Vector2(0, -20);
        bannerRT.sizeDelta = new Vector2(600, 60);

        Image bannerBg = turnBannerGO.GetComponent<Image>();
        if (bannerBg == null) bannerBg = turnBannerGO.AddComponent<Image>();
        bannerBg.color = new Color(0.1f, 0.2f, 0.35f, 0.85f);
        ui.turnBannerBg = bannerBg;

        GameObject turnTextGO = GetOrCreateChild(turnBannerGO, "Text");
        FillRect(turnTextGO);
        TextMeshProUGUI turnText = turnTextGO.GetComponent<TextMeshProUGUI>();
        if (turnText == null) turnText = turnTextGO.AddComponent<TextMeshProUGUI>();
        turnText.fontSize = 26;
        turnText.fontStyle = FontStyles.Bold;
        turnText.alignment = TextAlignmentOptions.Center;
        turnText.text = "LƯỢT CỦA BẠN (PLAYER TURN)";
        turnText.color = new Color(0.2f, 0.9f, 1f);
        ui.turnBannerText = turnText;

        // 2. Combat Log & Stage Progress
        GameObject logGO = GetOrCreateChild(canvasGO, "CombatLog");
        RectTransform logRT = logGO.GetComponent<RectTransform>();
        logRT.anchorMin = new Vector2(0.5f, 1f);
        logRT.anchorMax = new Vector2(0.5f, 1f);
        logRT.pivot = new Vector2(0.5f, 1f);
        logRT.anchoredPosition = new Vector2(0, -90);
        logRT.sizeDelta = new Vector2(750, 45);

        Image logBg = logGO.GetComponent<Image>();
        if (logBg == null) logBg = logGO.AddComponent<Image>();
        logBg.color = new Color(0.05f, 0.05f, 0.08f, 0.75f);

        GameObject logTextGO = GetOrCreateChild(logGO, "Text");
        FillRect(logTextGO);
        TextMeshProUGUI logText = logTextGO.GetComponent<TextMeshProUGUI>();
        if (logText == null) logText = logTextGO.AddComponent<TextMeshProUGUI>();
        logText.fontSize = 18;
        logText.alignment = TextAlignmentOptions.Center;
        logText.text = "⚔️ Trận chiến bắt đầu! Hãy chọn hành động.";
        logText.color = Color.white;
        ui.combatLogText = logText;

        GameObject stageTextGO = GetOrCreateChild(canvasGO, "StageProgress");
        RectTransform stageRT = stageTextGO.GetComponent<RectTransform>();
        stageRT.anchorMin = new Vector2(0, 1);
        stageRT.anchorMax = new Vector2(0, 1);
        stageRT.pivot = new Vector2(0, 1);
        stageRT.anchoredPosition = new Vector2(30, -25);
        stageRT.sizeDelta = new Vector2(200, 40);
        TextMeshProUGUI stageText = stageTextGO.GetComponent<TextMeshProUGUI>();
        if (stageText == null) stageText = stageTextGO.AddComponent<TextMeshProUGUI>();
        stageText.fontSize = 24;
        stageText.fontStyle = FontStyles.Bold;
        stageText.text = "🚩 Tầng 1 - Stage 1";
        stageText.color = new Color(1f, 0.85f, 0.3f);
        ui.stageProgressText = stageText;

        // 3. Target Info HUD (Top Right)
        GameObject targetHUD = GetOrCreateChild(canvasGO, "TargetInfoPanel");
        RectTransform targetRT = targetHUD.GetComponent<RectTransform>();
        targetRT.anchorMin = new Vector2(1, 1);
        targetRT.anchorMax = new Vector2(1, 1);
        targetRT.pivot = new Vector2(1, 1);
        targetRT.anchoredPosition = new Vector2(-30, -25);
        targetRT.sizeDelta = new Vector2(340, 120);

        Image targetBg = targetHUD.GetComponent<Image>();
        if (targetBg == null) targetBg = targetHUD.AddComponent<Image>();
        targetBg.color = new Color(0.12f, 0.08f, 0.15f, 0.9f);
        ui.targetInfoPanel = targetHUD;

        GameObject tNameGO = GetOrCreateChild(targetHUD, "Name");
        RectTransform tNameRT = tNameGO.GetComponent<RectTransform>();
        tNameRT.anchorMin = new Vector2(0, 0.65f);
        tNameRT.anchorMax = new Vector2(1, 1);
        tNameRT.offsetMin = new Vector2(15, 0);
        tNameRT.offsetMax = new Vector2(-15, 0);
        TextMeshProUGUI tName = tNameGO.GetComponent<TextMeshProUGUI>();
        if (tName == null) tName = tNameGO.AddComponent<TextMeshProUGUI>();
        tName.fontSize = 20;
        tName.fontStyle = FontStyles.Bold;
        tName.text = "🎯 Slime";
        tName.color = new Color(1f, 0.4f, 0.4f);
        ui.targetNameText = tName;

        GameObject tHpBarGO = GetOrCreateChild(targetHUD, "HPBar");
        RectTransform tHpBarRT = tHpBarGO.GetComponent<RectTransform>();
        tHpBarRT.anchorMin = new Vector2(0, 0.35f);
        tHpBarRT.anchorMax = new Vector2(1, 0.65f);
        tHpBarRT.offsetMin = new Vector2(15, 3);
        tHpBarRT.offsetMax = new Vector2(-15, -3);
        Image tHpBg = tHpBarGO.GetComponent<Image>();
        if (tHpBg == null) tHpBg = tHpBarGO.AddComponent<Image>();
        tHpBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        GameObject tHpFillGO = GetOrCreateChild(tHpBarGO, "Fill");
        FillRect(tHpFillGO);
        Image tHpFill = tHpFillGO.GetComponent<Image>();
        if (tHpFill == null) tHpFill = tHpFillGO.AddComponent<Image>();
        tHpFill.type = Image.Type.Filled;
        tHpFill.fillMethod = Image.FillMethod.Horizontal;
        tHpFill.fillAmount = 1f;
        tHpFill.color = new Color(0.85f, 0.15f, 0.15f, 1f);
        ui.targetHpFill = tHpFill;

        GameObject tHpTextGO = GetOrCreateChild(tHpBarGO, "Text");
        FillRect(tHpTextGO);
        TextMeshProUGUI tHpText = tHpTextGO.GetComponent<TextMeshProUGUI>();
        if (tHpText == null) tHpText = tHpTextGO.AddComponent<TextMeshProUGUI>();
        tHpText.fontSize = 14;
        tHpText.alignment = TextAlignmentOptions.Center;
        tHpText.text = "HP: 12 / 12";
        tHpText.color = Color.white;
        ui.targetHpText = tHpText;

        GameObject tIntentGO = GetOrCreateChild(targetHUD, "Intent");
        RectTransform tIntentRT = tIntentGO.GetComponent<RectTransform>();
        tIntentRT.anchorMin = new Vector2(0, 0);
        tIntentRT.anchorMax = new Vector2(1, 0.35f);
        tIntentRT.offsetMin = new Vector2(15, 0);
        tIntentRT.offsetMax = new Vector2(-15, 0);
        TextMeshProUGUI tIntent = tIntentGO.GetComponent<TextMeshProUGUI>();
        if (tIntent == null) tIntent = tIntentGO.AddComponent<TextMeshProUGUI>();
        tIntent.fontSize = 15;
        tIntent.text = "Ý định: ⚔️ Tấn công (4 DMG)";
        tIntent.color = new Color(1f, 0.85f, 0.4f);
        ui.targetIntentText = tIntent;

        // 4. Player HUD (Bottom Left)
        GameObject playerHUD = GetOrCreateChild(canvasGO, "PlayerStatusHUD");
        RectTransform pHudRT = playerHUD.GetComponent<RectTransform>();
        pHudRT.anchorMin = new Vector2(0, 0);
        pHudRT.anchorMax = new Vector2(0, 0);
        pHudRT.pivot = new Vector2(0, 0);
        pHudRT.anchoredPosition = new Vector2(30, 25);
        pHudRT.sizeDelta = new Vector2(460, 210);

        Image pHudBg = playerHUD.GetComponent<Image>();
        if (pHudBg == null) pHudBg = playerHUD.AddComponent<Image>();
        pHudBg.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);

        // Title Player
        GameObject pTitleGO = GetOrCreateChild(playerHUD, "Title");
        RectTransform pTitleRT = pTitleGO.GetComponent<RectTransform>();
        pTitleRT.anchorMin = new Vector2(0, 0.82f);
        pTitleRT.anchorMax = new Vector2(1, 1);
        pTitleRT.offsetMin = new Vector2(20, 0);
        pTitleRT.offsetMax = new Vector2(-20, 0);
        TextMeshProUGUI pTitle = pTitleGO.GetComponent<TextMeshProUGUI>();
        if (pTitle == null) pTitle = pTitleGO.AddComponent<TextMeshProUGUI>();
        pTitle.fontSize = 20;
        pTitle.fontStyle = FontStyles.Bold;
        pTitle.text = "DŨNG SĨ (PLAYER)";
        pTitle.color = new Color(0.3f, 0.85f, 1f);
        ui.playerNameText = pTitle;

        // HP Bar
        GameObject pHpBarGO = GetOrCreateChild(playerHUD, "HPBar");
        RectTransform pHpBarRT = pHpBarGO.GetComponent<RectTransform>();
        pHpBarRT.anchorMin = new Vector2(0, 0.62f);
        pHpBarRT.anchorMax = new Vector2(1, 0.80f);
        pHpBarRT.offsetMin = new Vector2(20, 2);
        pHpBarRT.offsetMax = new Vector2(-20, -2);
        Image pHpBg = pHpBarGO.GetComponent<Image>();
        if (pHpBg == null) pHpBg = pHpBarGO.AddComponent<Image>();
        pHpBg.color = new Color(0.25f, 0.1f, 0.1f, 1f);

        GameObject pHpFillGO = GetOrCreateChild(pHpBarGO, "Fill");
        FillRect(pHpFillGO);
        Image pHpFill = pHpFillGO.GetComponent<Image>();
        if (pHpFill == null) pHpFill = pHpFillGO.AddComponent<Image>();
        pHpFill.type = Image.Type.Filled;
        pHpFill.fillMethod = Image.FillMethod.Horizontal;
        pHpFill.fillAmount = 1f;
        pHpFill.color = new Color(0.2f, 0.8f, 0.3f, 1f);
        ui.hpFillImage = pHpFill;

        GameObject pHpTextGO = GetOrCreateChild(pHpBarGO, "Text");
        FillRect(pHpTextGO);
        TextMeshProUGUI pHpText = pHpTextGO.GetComponent<TextMeshProUGUI>();
        if (pHpText == null) pHpText = pHpTextGO.AddComponent<TextMeshProUGUI>();
        pHpText.fontSize = 15;
        pHpText.alignment = TextAlignmentOptions.Center;
        pHpText.text = "HP: 20 / 20";
        pHpText.color = Color.white;
        ui.hpText = pHpText;

        // Hunger Bar
        GameObject pHungerBarGO = GetOrCreateChild(playerHUD, "HungerBar");
        RectTransform pHungerBarRT = pHungerBarGO.GetComponent<RectTransform>();
        pHungerBarRT.anchorMin = new Vector2(0, 0.44f);
        pHungerBarRT.anchorMax = new Vector2(1, 0.60f);
        pHungerBarRT.offsetMin = new Vector2(20, 2);
        pHungerBarRT.offsetMax = new Vector2(-20, -2);
        Image pHungerBg = pHungerBarGO.GetComponent<Image>();
        if (pHungerBg == null) pHungerBg = pHungerBarGO.AddComponent<Image>();
        pHungerBg.color = new Color(0.25f, 0.18f, 0.08f, 1f);

        GameObject pHungerFillGO = GetOrCreateChild(pHungerBarGO, "Fill");
        FillRect(pHungerFillGO);
        Image pHungerFill = pHungerFillGO.GetComponent<Image>();
        if (pHungerFill == null) pHungerFill = pHungerFillGO.AddComponent<Image>();
        pHungerFill.type = Image.Type.Filled;
        pHungerFill.fillMethod = Image.FillMethod.Horizontal;
        pHungerFill.fillAmount = 1f;
        pHungerFill.color = new Color(0.95f, 0.65f, 0.15f, 1f);
        ui.hungerFillImage = pHungerFill;

        GameObject pHungerTextGO = GetOrCreateChild(pHungerBarGO, "Text");
        FillRect(pHungerTextGO);
        TextMeshProUGUI pHungerText = pHungerTextGO.GetComponent<TextMeshProUGUI>();
        if (pHungerText == null) pHungerText = pHungerTextGO.AddComponent<TextMeshProUGUI>();
        pHungerText.fontSize = 14;
        pHungerText.alignment = TextAlignmentOptions.Center;
        pHungerText.text = "🍗 Độ no: 100 / 100";
        pHungerText.color = Color.white;
        ui.hungerText = pHungerText;

        // AP Indicator
        GameObject apGO = GetOrCreateChild(playerHUD, "APText");
        RectTransform apRT = apGO.GetComponent<RectTransform>();
        apRT.anchorMin = new Vector2(0, 0.26f);
        apRT.anchorMax = new Vector2(1, 0.42f);
        apRT.offsetMin = new Vector2(20, 0);
        apRT.offsetMax = new Vector2(-20, 0);
        TextMeshProUGUI apText = apGO.GetComponent<TextMeshProUGUI>();
        if (apText == null) apText = apGO.AddComponent<TextMeshProUGUI>();
        apText.fontSize = 18;
        apText.fontStyle = FontStyles.Bold;
        apText.text = "⚡ AP: 3 / 3";
        apText.color = new Color(1f, 0.95f, 0.2f);
        ui.apText = apText;

        // Stats Display
        GameObject statsGO = GetOrCreateChild(playerHUD, "StatsText");
        RectTransform statsRT = statsGO.GetComponent<RectTransform>();
        statsRT.anchorMin = new Vector2(0, 0.12f);
        statsRT.anchorMax = new Vector2(1, 0.26f);
        statsRT.offsetMin = new Vector2(20, 0);
        statsRT.offsetMax = new Vector2(-20, 0);
        TextMeshProUGUI statsText = statsGO.GetComponent<TextMeshProUGUI>();
        if (statsText == null) statsText = statsGO.AddComponent<TextMeshProUGUI>();
        statsText.fontSize = 15;
        statsText.text = "⚔️ ATK: 5    🛡️ DEF: 0    💨 SPD: 10";
        statsText.color = new Color(0.85f, 0.9f, 1f);
        ui.statsText = statsText;

        // Buffs Display
        GameObject buffsGO = GetOrCreateChild(playerHUD, "BuffsText");
        RectTransform buffsRT = buffsGO.GetComponent<RectTransform>();
        buffsRT.anchorMin = new Vector2(0, 0);
        buffsRT.anchorMax = new Vector2(1, 0.12f);
        buffsRT.offsetMin = new Vector2(20, 0);
        buffsRT.offsetMax = new Vector2(-20, 0);
        TextMeshProUGUI buffsText = buffsGO.GetComponent<TextMeshProUGUI>();
        if (buffsText == null) buffsText = buffsGO.AddComponent<TextMeshProUGUI>();
        buffsText.fontSize = 13;
        buffsText.text = "Hiệu ứng: Không có";
        buffsText.color = new Color(0.7f, 1f, 0.7f);
        ui.buffsText = buffsText;

        // 5. Action Buttons Bar (Bottom Center / Right)
        GameObject actionsGO = GetOrCreateChild(canvasGO, "ActionBar");
        RectTransform actRT = actionsGO.GetComponent<RectTransform>();
        actRT.anchorMin = new Vector2(0.5f, 0);
        actRT.anchorMax = new Vector2(0.5f, 0);
        actRT.pivot = new Vector2(0.5f, 0);
        actRT.anchoredPosition = new Vector2(120, 25);
        actRT.sizeDelta = new Vector2(760, 90);

        Image actBg = actionsGO.GetComponent<Image>();
        if (actBg == null) actBg = actionsGO.AddComponent<Image>();
        actBg.color = new Color(0.06f, 0.08f, 0.12f, 0.9f);

        ui.attackButton = CreateActionButton(actionsGO, "BtnAttack", "⚔️ TẤN CÔNG (1)", new Vector2(-270, 0), new Color(0.75f, 0.2f, 0.2f));
        ui.defendButton = CreateActionButton(actionsGO, "BtnDefend", "🛡️ PHÒNG THỦ (2)", new Vector2(-90, 0), new Color(0.2f, 0.45f, 0.75f));
        ui.eatButton = CreateActionButton(actionsGO, "BtnEat", "🍖 ĂN UỐNG (3)", new Vector2(90, 0), new Color(0.2f, 0.65f, 0.35f));
        ui.endTurnButton = CreateActionButton(actionsGO, "BtnEndTurn", "⏭️ HẾT LƯỢT (Space)", new Vector2(270, 0), new Color(0.6f, 0.5f, 0.2f));

        // 6. Food Panel (Modal)
        GameObject foodModal = GetOrCreateChild(canvasGO, "FoodSelectionPanel");
        RectTransform foodRT = foodModal.GetComponent<RectTransform>();
        foodRT.anchorMin = new Vector2(0.5f, 0.5f);
        foodRT.anchorMax = new Vector2(0.5f, 0.5f);
        foodRT.pivot = new Vector2(0.5f, 0.5f);
        foodRT.sizeDelta = new Vector2(440, 420);

        Image foodBg = foodModal.GetComponent<Image>();
        if (foodBg == null) foodBg = foodModal.AddComponent<Image>();
        foodBg.color = new Color(0.1f, 0.12f, 0.16f, 0.97f);
        ui.foodPanel = foodModal;

        GameObject foodTitleGO = GetOrCreateChild(foodModal, "Title");
        RectTransform foodTitleRT = foodTitleGO.GetComponent<RectTransform>();
        foodTitleRT.anchorMin = new Vector2(0, 0.85f);
        foodTitleRT.anchorMax = new Vector2(1, 1);
        foodTitleRT.offsetMin = Vector2.zero;
        foodTitleRT.offsetMax = Vector2.zero;
        TextMeshProUGUI foodTitle = foodTitleGO.GetComponent<TextMeshProUGUI>();
        if (foodTitle == null) foodTitle = foodTitleGO.AddComponent<TextMeshProUGUI>();
        foodTitle.fontSize = 20;
        foodTitle.fontStyle = FontStyles.Bold;
        foodTitle.alignment = TextAlignmentOptions.Center;
        foodTitle.text = "🍖 CHỌN MÓN ĂN ĐỂ HỒI PHỤC";
        foodTitle.color = new Color(1f, 0.85f, 0.3f);

        GameObject scrollGO = GetOrCreateChild(foodModal, "ListContainer");
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.05f, 0.18f);
        scrollRT.anchorMax = new Vector2(0.95f, 0.84f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;
        VerticalLayoutGroup vlg = scrollGO.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = scrollGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        ui.foodListContainer = scrollGO.transform;

        GameObject noFoodGO = GetOrCreateChild(foodModal, "NoFoodText");
        FillRect(noFoodGO);
        TextMeshProUGUI noFood = noFoodGO.GetComponent<TextMeshProUGUI>();
        if (noFood == null) noFood = noFoodGO.AddComponent<TextMeshProUGUI>();
        noFood.fontSize = 16;
        noFood.alignment = TextAlignmentOptions.Center;
        noFood.text = "Không có thức ăn trong ba lô!";
        noFood.color = new Color(0.8f, 0.8f, 0.8f);
        ui.noFoodText = noFood;

        GameObject closeFoodBtn = GetOrCreateChild(foodModal, "CloseBtn");
        RectTransform cBtnRT = closeFoodBtn.GetComponent<RectTransform>();
        cBtnRT.anchorMin = new Vector2(0.5f, 0);
        cBtnRT.anchorMax = new Vector2(0.5f, 0);
        cBtnRT.pivot = new Vector2(0.5f, 0);
        cBtnRT.anchoredPosition = new Vector2(0, 15);
        cBtnRT.sizeDelta = new Vector2(140, 40);
        Image cBtnImg = closeFoodBtn.GetComponent<Image>();
        if (cBtnImg == null) cBtnImg = closeFoodBtn.AddComponent<Image>();
        cBtnImg.color = new Color(0.4f, 0.2f, 0.2f);
        Button cBtn = closeFoodBtn.GetComponent<Button>();
        if (cBtn == null) cBtn = closeFoodBtn.AddComponent<Button>();
        ui.closeFoodPanelButton = cBtn;

        GameObject cTextGO = GetOrCreateChild(closeFoodBtn, "Text");
        FillRect(cTextGO);
        TextMeshProUGUI cText = cTextGO.GetComponent<TextMeshProUGUI>();
        if (cText == null) cText = cTextGO.AddComponent<TextMeshProUGUI>();
        cText.fontSize = 16;
        cText.alignment = TextAlignmentOptions.Center;
        cText.text = "ĐÓNG";
        cText.color = Color.white;

        // 7. Victory Panel
        ui.victoryPanel = CreateModalPanel(canvasGO, "VictoryPanel", "🏆 CHIẾN THẮNG HẦM NGỤC!", "Bạn đã dọn sạch hầm ngục!\nChiến lợi phẩm đã được bảo toàn an toàn.", "TRỞ VỀ NÔNG TRẠI (BASE)", out ui.victoryText, out ui.victoryReturnButton, new Color(0.15f, 0.6f, 0.3f));

        // 8. Defeat Panel
        ui.defeatPanel = CreateModalPanel(canvasGO, "DefeatPanel", "💀 BẠN ĐÃ TỬ TRẬN!", "Bạn đã bị quái vật đánh bại trong hầm ngục.\nToàn bộ thức ăn và chiến lợi phẩm trong lượt này đã bị rơi mất.", "TRỞ VỀ NÔNG TRẠI (BASE)", out ui.defeatText, out ui.defeatReturnButton, new Color(0.7f, 0.2f, 0.2f));

        // 9. Reward Panel
        ui.rewardPanel = CreateModalPanel(canvasGO, "RewardPanel", "🎁 PHÒNG THƯỞNG KHO BÁU!", "Bạn mở rương kho báu và nhận được nhiều phần quà giá trị!", "TIẾP TỤC", out ui.rewardDescriptionText, out ui.rewardClaimButton, new Color(0.85f, 0.65f, 0.15f));

        // 10. Target Reticle (World Space Indicator)
        GameObject reticleGO = GameObject.Find("TargetReticle");
        if (reticleGO == null)
        {
            reticleGO = new GameObject("TargetReticle");
            SpriteRenderer reticleSR = reticleGO.AddComponent<SpriteRenderer>();
            reticleSR.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            reticleSR.sortingOrder = 20;

            string[] arrowGuids = AssetDatabase.FindAssets("t:Sprite Arrow");
            if (arrowGuids.Length > 0)
            {
                reticleSR.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(arrowGuids[0]));
            }
        }
        ui.targetReticle = reticleGO.transform;
        reticleGO.SetActive(false);
    }

    private static Button CreateActionButton(GameObject parent, string name, string text, Vector2 pos, Color btnColor)
    {
        GameObject btnGO = GetOrCreateChild(parent, name);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(170, 58);

        Image img = btnGO.GetComponent<Image>();
        if (img == null) img = btnGO.AddComponent<Image>();
        img.color = btnColor;

        Button btn = btnGO.GetComponent<Button>();
        if (btn == null) btn = btnGO.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor = btnColor;
        cb.highlightedColor = btnColor * 1.25f;
        cb.pressedColor = btnColor * 0.8f;
        cb.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        btn.colors = cb;

        GameObject textGO = GetOrCreateChild(btnGO, "Text");
        FillRect(textGO);
        TextMeshProUGUI label = textGO.GetComponent<TextMeshProUGUI>();
        if (label == null) label = textGO.AddComponent<TextMeshProUGUI>();
        label.fontSize = 15;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.text = text;
        label.color = Color.white;

        return btn;
    }

    private static GameObject CreateModalPanel(GameObject canvasGO, string name, string title, string body, string btnText, out TextMeshProUGUI bodyText, out Button actionBtn, Color btnColor)
    {
        GameObject modal = GetOrCreateChild(canvasGO, name);
        FillRect(modal);

        Image backdrop = modal.GetComponent<Image>();
        if (backdrop == null) backdrop = modal.AddComponent<Image>();
        backdrop.color = new Color(0, 0, 0, 0.78f);

        GameObject box = GetOrCreateChild(modal, "Box");
        RectTransform boxRT = box.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(580, 320);

        Image boxBg = box.GetComponent<Image>();
        if (boxBg == null) boxBg = box.AddComponent<Image>();
        boxBg.color = new Color(0.1f, 0.12f, 0.18f, 0.98f);

        // Title
        GameObject titleGO = GetOrCreateChild(box, "Title");
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.75f);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;
        TextMeshProUGUI t = titleGO.GetComponent<TextMeshProUGUI>();
        if (t == null) t = titleGO.AddComponent<TextMeshProUGUI>();
        t.fontSize = 24;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.text = title;
        t.color = Color.white;

        // Body
        GameObject bodyGO = GetOrCreateChild(box, "Body");
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = new Vector2(0.08f, 0.28f);
        bodyRT.anchorMax = new Vector2(0.92f, 0.75f);
        bodyRT.offsetMin = Vector2.zero;
        bodyRT.offsetMax = Vector2.zero;
        bodyText = bodyGO.GetComponent<TextMeshProUGUI>();
        if (bodyText == null) bodyText = bodyGO.AddComponent<TextMeshProUGUI>();
        bodyText.fontSize = 17;
        bodyText.alignment = TextAlignmentOptions.Center;
        bodyText.text = body;
        bodyText.color = new Color(0.85f, 0.85f, 0.9f);

        // Button
        GameObject bGO = GetOrCreateChild(box, "ActionBtn");
        RectTransform bRT = bGO.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 0);
        bRT.anchorMax = new Vector2(0.5f, 0);
        bRT.pivot = new Vector2(0.5f, 0);
        bRT.anchoredPosition = new Vector2(0, 25);
        bRT.sizeDelta = new Vector2(260, 52);

        Image bImg = bGO.GetComponent<Image>();
        if (bImg == null) bImg = bGO.AddComponent<Image>();
        bImg.color = btnColor;

        actionBtn = bGO.GetComponent<Button>();
        if (actionBtn == null) actionBtn = bGO.AddComponent<Button>();

        GameObject btGO = GetOrCreateChild(bGO, "Text");
        FillRect(btGO);
        TextMeshProUGUI bt = btGO.GetComponent<TextMeshProUGUI>();
        if (bt == null) bt = btGO.AddComponent<TextMeshProUGUI>();
        bt.fontSize = 16;
        bt.fontStyle = FontStyles.Bold;
        bt.alignment = TextAlignmentOptions.Center;
        bt.text = btnText;
        bt.color = Color.white;

        modal.SetActive(false);
        return modal;
    }

    private static GameObject GetOrCreateChild(GameObject parent, string name)
    {
        Transform child = parent.transform.Find(name);
        if (child != null) return child.gameObject;

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static void FillRect(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
