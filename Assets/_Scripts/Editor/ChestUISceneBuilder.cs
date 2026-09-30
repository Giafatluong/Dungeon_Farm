#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

[InitializeOnLoad]
public static class ChestUISceneBuilder
{
    private const string UI_FRAME_PATH = "Assets/_Assets/Ui_Inventory/UI_Frame.png";
    private const string SLOT_PREFAB_PATH = "Assets/_Assets/_Prefabs/inventorySlot.prefab";
    private const string HOME_CHEST_ASSET_PATH = "Assets/_Assets/ScriptableObjects/HomeChest.asset";
    private const string INVENTORY_ASSET_PATH = "Assets/_Assets/ScriptableObjects/Inventory.asset";

    static ChestUISceneBuilder()
    {
        EditorApplication.delayCall += AutoCheckAndBuild;
    }

    private static void AutoCheckAndBuild()
    {
        if (Application.isPlaying) return;

        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "Base") return;

        GameObject canvasGO = GameObject.Find("Canvas");
        if (canvasGO != null)
        {
            Transform existingChestPanel = canvasGO.transform.Find("ChestPanel");
            bool needsRebuild = false;

            if (existingChestPanel == null)
            {
                needsRebuild = true;
            }
            else
            {
                // Kiểm tra xem có phải layout nằm ngang chuẩn (size 600x290) không
                Transform chestFrame = existingChestPanel.Find("Chest_Frame");
                if (chestFrame == null)
                {
                    needsRebuild = true;
                }
                else
                {
                    RectTransform rt = chestFrame as RectTransform;
                    if (rt == null || rt.sizeDelta.x < 550) // Nếu là layout cũ (370x320) -> rebuild lại nằm ngang
                    {
                        needsRebuild = true;
                    }
                }
            }

            if (needsRebuild)
            {
                Debug.Log("[ChestUISceneBuilder] Đang thiết lập lại UI Rương nằm ngang chuẩn theo Inventory...");
                BuildChestUIInternal(false);
            }
        }
    }

    [MenuItem("Tools/Tạo UI Rương Nằm Ngang (Setup Chest UI in Scene)", false, 10)]
    public static void BuildChestUIMenu()
    {
        BuildChestUIInternal(true);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = 5; // UI Layer
        return go;
    }

    public static void BuildChestUIInternal(bool showDialog)
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != "Base")
        {
            if (showDialog)
            {
                if (EditorUtility.DisplayDialog("Mở Scene Base", "Chức năng này cần thực hiện trên scene Base. Bạn có muốn mở scene Base không?", "Mở Base", "Hủy"))
                {
                    EditorSceneManager.OpenScene("Assets/_Scenes/Base.unity");
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ChestUISceneBuilder] Không tìm thấy Canvas trong scene Base!");
            return;
        }

        Sprite uiFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(UI_FRAME_PATH);
        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SLOT_PREFAB_PATH);
        ItemContainer homeChestContainer = AssetDatabase.LoadAssetAtPath<ItemContainer>(HOME_CHEST_ASSET_PATH);
        ItemContainer inventoryContainer = AssetDatabase.LoadAssetAtPath<ItemContainer>(INVENTORY_ASSET_PATH);
        TMP_FontAsset fontAsset = TMP_Settings.defaultFontAsset;

        // Xóa ChestPanel cũ nếu có để tạo lại mới 100% sạch sẽ
        Transform existing = canvas.transform.Find("ChestPanel");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // 1. Tạo Root ChestPanel (Phủ kín màn hình)
        GameObject chestPanelGO = CreateUIObject("ChestPanel", canvas.transform);
        Undo.RegisterCreatedObjectUndo(chestPanelGO, "Setup ChestPanel");

        RectTransform panelRect = (RectTransform)chestPanelGO.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRect.localScale = Vector3.one;

        // Nền mờ tối chắn click xuyên ra ngoài
        Image panelImage = chestPanelGO.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.75f);
        panelImage.raycastTarget = true;

        // Component ChestUI
        ChestUI chestUI = chestPanelGO.AddComponent<ChestUI>();

        // 2. KHUNG RƯƠNG (NẰM NGANG PHÍA TRÊN: Y = 165, SIZE: 600x290)
        GameObject chestFrameGO = CreateUIObject("Chest_Frame", chestPanelGO.transform);
        RectTransform chestFrameRect = (RectTransform)chestFrameGO.transform;
        chestFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
        chestFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
        chestFrameRect.anchoredPosition = new Vector2(0, 165);
        chestFrameRect.sizeDelta = new Vector2(600, 290);
        chestFrameRect.localScale = Vector3.one;

        Image chestFrameImage = chestFrameGO.AddComponent<Image>();
        if (uiFrameSprite != null)
        {
            chestFrameImage.sprite = uiFrameSprite;
            chestFrameImage.type = Image.Type.Simple; // Giống hệt inventory gốc
        }
        chestFrameImage.color = Color.white;

        // GridLayoutGroup trên Khung Rương (Thông số giống hệt inventory của người chơi)
        GridLayoutGroup chestGLG = chestFrameGO.AddComponent<GridLayoutGroup>();
        chestGLG.padding = new RectOffset(24, 0, 123, 0);
        chestGLG.cellSize = new Vector2(50, 50);
        chestGLG.spacing = new Vector2(0.01f, 0f);
        chestGLG.startCorner = GridLayoutGroup.Corner.UpperLeft;
        chestGLG.startAxis = GridLayoutGroup.Axis.Horizontal;
        chestGLG.childAlignment = TextAnchor.UpperLeft;
        chestGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        chestGLG.constraintCount = 11;

        // Tiêu đề Rương (Dùng LayoutElement ignoreLayout để không bị GridLayoutGroup ép vào ô slot)
        GameObject chestTitleGO = CreateUIObject("Title", chestFrameGO.transform);
        RectTransform chestTitleRect = (RectTransform)chestTitleGO.transform;
        chestTitleRect.anchorMin = new Vector2(0.5f, 1f);
        chestTitleRect.anchorMax = new Vector2(0.5f, 1f);
        chestTitleRect.anchoredPosition = new Vector2(0, -35);
        chestTitleRect.sizeDelta = new Vector2(400, 32);
        chestTitleRect.localScale = Vector3.one;
        LayoutElement chestTitleLE = chestTitleGO.AddComponent<LayoutElement>();
        chestTitleLE.ignoreLayout = true;

        TextMeshProUGUI chestTitleTMP = chestTitleGO.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) chestTitleTMP.font = fontAsset;
        chestTitleTMP.text = "📦 RƯƠNG NHÀ (CHEST)";
        chestTitleTMP.fontSize = 20;
        chestTitleTMP.fontStyle = FontStyles.Bold;
        chestTitleTMP.alignment = TextAlignmentOptions.Center;
        chestTitleTMP.color = new Color(1f, 0.85f, 0.35f, 1f);

        // Phụ đề Rương
        GameObject chestSubGO = CreateUIObject("Subtitle", chestFrameGO.transform);
        RectTransform chestSubRect = (RectTransform)chestSubGO.transform;
        chestSubRect.anchorMin = new Vector2(0.5f, 1f);
        chestSubRect.anchorMax = new Vector2(0.5f, 1f);
        chestSubRect.anchoredPosition = new Vector2(0, -68);
        chestSubRect.sizeDelta = new Vector2(400, 20);
        chestSubRect.localScale = Vector3.one;
        LayoutElement chestSubLE = chestSubGO.AddComponent<LayoutElement>();
        chestSubLE.ignoreLayout = true;

        TextMeshProUGUI chestSubTMP = chestSubGO.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) chestSubTMP.font = fontAsset;
        chestSubTMP.text = "33 Ô CHỨA • BẢO VỆ AN TOÀN 100% KHI VÀO DUNGEON";
        chestSubTMP.fontSize = 11;
        chestSubTMP.alignment = TextAlignmentOptions.Center;
        chestSubTMP.color = new Color(0.45f, 0.95f, 0.5f, 1f);

        // Nút tắt X ở góc phải trên khung Rương
        GameObject closeCornerBtnGO = CreateUIObject("CloseCornerBtn", chestFrameGO.transform);
        RectTransform closeCornerRect = (RectTransform)closeCornerBtnGO.transform;
        closeCornerRect.anchorMin = new Vector2(1f, 1f);
        closeCornerRect.anchorMax = new Vector2(1f, 1f);
        closeCornerRect.anchoredPosition = new Vector2(-28, -28);
        closeCornerRect.sizeDelta = new Vector2(36, 36);
        closeCornerRect.localScale = Vector3.one;
        LayoutElement closeCornerLE = closeCornerBtnGO.AddComponent<LayoutElement>();
        closeCornerLE.ignoreLayout = true;

        Button closeCornerBtn = closeCornerBtnGO.AddComponent<Button>();
        Image closeCornerImg = closeCornerBtnGO.AddComponent<Image>();
        closeCornerImg.color = new Color(0.85f, 0.25f, 0.25f, 0.9f);
        GameObject closeCornerTxtGO = CreateUIObject("Text", closeCornerBtnGO.transform);
        RectTransform closeCornerTxtRect = (RectTransform)closeCornerTxtGO.transform;
        closeCornerTxtRect.anchorMin = Vector2.zero;
        closeCornerTxtRect.anchorMax = Vector2.one;
        closeCornerTxtRect.offsetMin = Vector2.zero;
        closeCornerTxtRect.offsetMax = Vector2.zero;
        TextMeshProUGUI closeCornerTMP = closeCornerTxtGO.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) closeCornerTMP.font = fontAsset;
        closeCornerTMP.text = "✕";
        closeCornerTMP.fontSize = 18;
        closeCornerTMP.fontStyle = FontStyles.Bold;
        closeCornerTMP.alignment = TextAlignmentOptions.Center;
        closeCornerTMP.color = Color.white;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(closeCornerBtn.onClick, chestUI.Close);

        // Tạo sẵn 33 Slot cho Rương trong Editor để nhìn thấy trực quan
        int chestSlotCount = (homeChestContainer != null) ? homeChestContainer.maxSlots : 33;
        for (int i = 0; i < chestSlotCount; i++)
        {
            GameObject slotObj;
            if (slotPrefab != null)
            {
                slotObj = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, chestFrameGO.transform);
            }
            else
            {
                slotObj = CreateUIObject($"inventorySlot ({i})", chestFrameGO.transform);
            }
            slotObj.name = $"inventorySlot ({i})";
            slotObj.transform.localScale = Vector3.one;
        }

        // 3. KHUNG BA LÔ (NẰM NGANG PHÍA DƯỚI: Y = -155, SIZE: 600x290)
        GameObject bpFrameGO = CreateUIObject("Backpack_Frame", chestPanelGO.transform);
        RectTransform bpFrameRect = (RectTransform)bpFrameGO.transform;
        bpFrameRect.anchorMin = new Vector2(0.5f, 0.5f);
        bpFrameRect.anchorMax = new Vector2(0.5f, 0.5f);
        bpFrameRect.anchoredPosition = new Vector2(0, -155);
        bpFrameRect.sizeDelta = new Vector2(600, 290);
        bpFrameRect.localScale = Vector3.one;

        Image bpFrameImage = bpFrameGO.AddComponent<Image>();
        if (uiFrameSprite != null)
        {
            bpFrameImage.sprite = uiFrameSprite;
            bpFrameImage.type = Image.Type.Simple; // Giống hệt inventory gốc
        }
        bpFrameImage.color = Color.white;

        // GridLayoutGroup trên Khung Ba lô (Giống hệt inventory)
        GridLayoutGroup bpGLG = bpFrameGO.AddComponent<GridLayoutGroup>();
        bpGLG.padding = new RectOffset(24, 0, 123, 0);
        bpGLG.cellSize = new Vector2(50, 50);
        bpGLG.spacing = new Vector2(0.01f, 0f);
        bpGLG.startCorner = GridLayoutGroup.Corner.UpperLeft;
        bpGLG.startAxis = GridLayoutGroup.Axis.Horizontal;
        bpGLG.childAlignment = TextAnchor.UpperLeft;
        bpGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        bpGLG.constraintCount = 11;

        // Tiêu đề Ba lô
        GameObject bpTitleGO = CreateUIObject("Title", bpFrameGO.transform);
        RectTransform bpTitleRect = (RectTransform)bpTitleGO.transform;
        bpTitleRect.anchorMin = new Vector2(0.5f, 1f);
        bpTitleRect.anchorMax = new Vector2(0.5f, 1f);
        bpTitleRect.anchoredPosition = new Vector2(0, -35);
        bpTitleRect.sizeDelta = new Vector2(400, 32);
        bpTitleRect.localScale = Vector3.one;
        LayoutElement bpTitleLE = bpTitleGO.AddComponent<LayoutElement>();
        bpTitleLE.ignoreLayout = true;

        TextMeshProUGUI bpTitleTMP = bpTitleGO.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) bpTitleTMP.font = fontAsset;
        bpTitleTMP.text = "🎒 BA LÔ (INVENTORY)";
        bpTitleTMP.fontSize = 20;
        bpTitleTMP.fontStyle = FontStyles.Bold;
        bpTitleTMP.alignment = TextAlignmentOptions.Center;
        bpTitleTMP.color = new Color(0.35f, 0.85f, 1f, 1f);

        // Phụ đề Ba lô
        GameObject bpSubGO = CreateUIObject("Subtitle", bpFrameGO.transform);
        RectTransform bpSubRect = (RectTransform)bpSubGO.transform;
        bpSubRect.anchorMin = new Vector2(0.5f, 1f);
        bpSubRect.anchorMax = new Vector2(0.5f, 1f);
        bpSubRect.anchoredPosition = new Vector2(0, -68);
        bpSubRect.sizeDelta = new Vector2(400, 20);
        bpSubRect.localScale = Vector3.one;
        LayoutElement bpSubLE = bpSubGO.AddComponent<LayoutElement>();
        bpSubLE.ignoreLayout = true;

        TextMeshProUGUI bpSubTMP = bpSubGO.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) bpSubTMP.font = fontAsset;
        bpSubTMP.text = "33 Ô CHỨA • TÚI ĐỒ MANG THEO";
        bpSubTMP.fontSize = 11;
        bpSubTMP.alignment = TextAlignmentOptions.Center;
        bpSubTMP.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        // Tạo sẵn 33 Slot cho Ba lô trong Editor
        int bpSlotCount = (inventoryContainer != null) ? inventoryContainer.maxSlots : 33;
        for (int i = 0; i < bpSlotCount; i++)
        {
            GameObject slotObj;
            if (slotPrefab != null)
            {
                slotObj = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, bpFrameGO.transform);
            }
            else
            {
                slotObj = CreateUIObject($"inventorySlot ({i})", bpFrameGO.transform);
            }
            slotObj.name = $"inventorySlot ({i})";
            slotObj.transform.localScale = Vector3.one;
        }

        // 4. Nút Đóng & Hướng dẫn ở thanh giữa (Y = 5)
        GameObject middleBarGO = CreateUIObject("MiddleBar", chestPanelGO.transform);
        RectTransform middleBarRect = (RectTransform)middleBarGO.transform;
        middleBarRect.anchorMin = new Vector2(0.5f, 0.5f);
        middleBarRect.anchorMax = new Vector2(0.5f, 0.5f);
        middleBarRect.anchoredPosition = new Vector2(0, 5);
        middleBarRect.sizeDelta = new Vector2(600, 30);
        middleBarRect.localScale = Vector3.one;

        // Nút Đóng ở giữa
        GameObject closeBtnGO = CreateUIObject("CloseButton", middleBarGO.transform);
        RectTransform closeRect = (RectTransform)closeBtnGO.transform;
        closeRect.anchorMin = new Vector2(0.5f, 0.5f);
        closeRect.anchorMax = new Vector2(0.5f, 0.5f);
        closeRect.anchoredPosition = new Vector2(0, 0);
        closeRect.sizeDelta = new Vector2(170, 32);
        closeRect.localScale = Vector3.one;

        Image closeImg = closeBtnGO.AddComponent<Image>();
        if (uiFrameSprite != null)
        {
            closeImg.sprite = uiFrameSprite;
            closeImg.type = Image.Type.Simple;
        }
        closeImg.color = new Color(0.9f, 0.35f, 0.35f, 1f);

        Button closeBtn = closeBtnGO.AddComponent<Button>();
        GameObject closeTxtGO = CreateUIObject("Text (TMP)", closeBtnGO.transform);
        RectTransform closeTxtRect = (RectTransform)closeTxtGO.transform;
        closeTxtRect.anchorMin = Vector2.zero;
        closeTxtRect.anchorMax = Vector2.one;
        closeTxtRect.offsetMin = Vector2.zero;
        closeTxtRect.offsetMax = Vector2.zero;
        TextMeshProUGUI closeTMP = closeTxtGO.AddComponent<TextMeshProUGUI>();
        if (fontAsset != null) closeTMP.font = fontAsset;
        closeTMP.text = "❌ ĐÓNG (ESC / E)";
        closeTMP.fontSize = 14;
        closeTMP.fontStyle = FontStyles.Bold;
        closeTMP.alignment = TextAlignmentOptions.Center;
        closeTMP.color = Color.white;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(closeBtn.onClick, chestUI.Close);

        // 5. Kết nối SerializedField cho ChestUI
        SerializedObject soChestUI = new SerializedObject(chestUI);
        soChestUI.FindProperty("chestSlotsParent").objectReferenceValue = chestFrameGO.transform;
        soChestUI.FindProperty("backpackSlotsParent").objectReferenceValue = bpFrameGO.transform;
        soChestUI.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
        soChestUI.FindProperty("canvas").objectReferenceValue = canvas;
        soChestUI.FindProperty("chestPanel").objectReferenceValue = chestPanelGO;
        soChestUI.ApplyModifiedProperties();

        // 6. Cấu hình HomeChest trên Chest_Anim_0
        GameObject chestWorldGO = GameObject.Find("Chest_Anim_0");
        if (chestWorldGO == null) chestWorldGO = GameObject.Find("HomeChest");
        if (chestWorldGO != null)
        {
            HomeChest hc = chestWorldGO.GetComponent<HomeChest>() ?? chestWorldGO.AddComponent<HomeChest>();
            SerializedObject soHC = new SerializedObject(hc);
            if (homeChestContainer != null)
                soHC.FindProperty("chestContainer").objectReferenceValue = homeChestContainer;
            if (inventoryContainer != null)
                soHC.FindProperty("backpackContainer").objectReferenceValue = inventoryContainer;
            soHC.ApplyModifiedProperties();
        }

        // Mặc định ẩn ChestPanel
        chestPanelGO.SetActive(false);

        // Lưu scene
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Debug.Log("[ChestUISceneBuilder] ✅ Đã tạo thành công UI Rương Nằm Ngang chuẩn khớp 100% với UI Inventory trong Scene Base!");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Thành công", "Đã tạo UI Rương Nằm Ngang hoàn chỉnh!\n\n- Khung Rương (trên) và Ba lô (dưới) đều có kích thước 600x290 nằm ngang chuẩn pixel art.\n- Tọa độ và GridLayoutGroup đồng bộ 100% với Inventory người chơi, các slot không còn bị lệch.\n- Đã tạo sẵn 33 slot trực quan trong scene và lưu thành công.", "OK");
        }
    }
}
#endif
