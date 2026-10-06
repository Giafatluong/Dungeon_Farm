using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Procedural UI Builder for Camp Encounter.
/// Constructs the Canvas, main panels, action buttons, risk gauge, and modals at runtime
/// if references are not pre-assigned in the Inspector.
/// </summary>
public static class CampUIBuilder
{
    public static void Build(CampUI ui)
    {
        if (ui == null || ui.mainCampPanel != null) return;

        Canvas canvas = ui.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // 1. Main Camp Panel
        ui.mainCampPanel = new GameObject("CampMainPanel", typeof(RectTransform), typeof(Image));
        ui.mainCampPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRT = ui.mainCampPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(760, 520);
        Image panelBg = ui.mainCampPanel.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(panelBg, new Color(0.10f, 0.12f, 0.17f, 0.98f));

        // Camp Icon
        Sprite tentSprite = DungeonUIAssetHelper.GetTentSprite();
        if (tentSprite != null)
        {
            GameObject iconGO = new GameObject("CampIcon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(ui.mainCampPanel.transform, false);
            RectTransform iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 1);
            iconRT.anchorMax = new Vector2(0.5f, 1);
            iconRT.pivot = new Vector2(0.5f, 1);
            iconRT.anchoredPosition = new Vector2(-190, -14);
            iconRT.sizeDelta = new Vector2(38, 38);
            Image iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = tentSprite;
            iconImg.preserveAspect = true;
        }

        // Title
        GameObject titleGO = new GameObject("CampTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(ui.mainCampPanel.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = new Vector2(0, -18);
        titleRT.sizeDelta = new Vector2(-40, 36);
        ui.campTitleText = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(ui.campTitleText);
        ui.campTitleText.fontSize = 22;
        ui.campTitleText.fontStyle = FontStyles.Bold;
        ui.campTitleText.alignment = TextAlignmentOptions.Center;
        ui.campTitleText.text = "CAMP ENCOUNTER";
        ui.campTitleText.color = new Color(1f, 0.85f, 0.35f);

        // Description
        GameObject descGO = new GameObject("CampDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
        descGO.transform.SetParent(ui.mainCampPanel.transform, false);
        RectTransform descRT = descGO.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0, 1);
        descRT.anchorMax = new Vector2(1, 1);
        descRT.pivot = new Vector2(0.5f, 1);
        descRT.anchoredPosition = new Vector2(0, -56);
        descRT.sizeDelta = new Vector2(-60, 48);
        ui.campDescText = descGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(ui.campDescText);
        ui.campDescText.fontSize = 12;
        ui.campDescText.alignment = TextAlignmentOptions.Center;
        ui.campDescText.text = "A warm campfire burns in the dungeon. Rest, eat, or cook to recover.\n<color=#FFAA55>Warning: Activities create noise and aroma that attract wandering monsters!</color>";
        ui.campDescText.color = new Color(0.85f, 0.88f, 0.95f);

        // Ambush Gauge Section
        GameObject riskSection = new GameObject("RiskSection", typeof(RectTransform));
        riskSection.transform.SetParent(ui.mainCampPanel.transform, false);
        RectTransform riskRT = riskSection.GetComponent<RectTransform>();
        riskRT.anchorMin = new Vector2(0.5f, 1);
        riskRT.anchorMax = new Vector2(0.5f, 1);
        riskRT.anchoredPosition = new Vector2(0, -135);
        riskRT.sizeDelta = new Vector2(500, 60);

        GameObject riskTextGO = new GameObject("RiskPercentText", typeof(RectTransform), typeof(TextMeshProUGUI));
        riskTextGO.transform.SetParent(riskSection.transform, false);
        RectTransform riskTextRT = riskTextGO.GetComponent<RectTransform>();
        riskTextRT.anchorMin = new Vector2(0, 1);
        riskTextRT.anchorMax = new Vector2(1, 1);
        riskTextRT.anchoredPosition = new Vector2(0, 0);
        riskTextRT.sizeDelta = new Vector2(0, 20);
        ui.ambushRiskPercentText = riskTextGO.GetComponent<TextMeshProUGUI>();
        ui.ambushRiskPercentText.fontSize = 13;
        ui.ambushRiskPercentText.alignment = TextAlignmentOptions.Center;
        ui.ambushRiskPercentText.text = "Ambush Risk: <b>0%</b>";

        GameObject gaugeBgGO = new GameObject("GaugeBg", typeof(RectTransform), typeof(Image));
        gaugeBgGO.transform.SetParent(riskSection.transform, false);
        RectTransform gaugeBgRT = gaugeBgGO.GetComponent<RectTransform>();
        gaugeBgRT.anchoredPosition = new Vector2(0, -16);
        gaugeBgRT.sizeDelta = new Vector2(460, 14);
        Image gaugeBgImg = gaugeBgGO.GetComponent<Image>();
        gaugeBgImg.color = new Color(0.2f, 0.22f, 0.28f, 1f);

        GameObject gaugeFillGO = new GameObject("GaugeFill", typeof(RectTransform), typeof(Image));
        gaugeFillGO.transform.SetParent(gaugeBgGO.transform, false);
        RectTransform gaugeFillRT = gaugeFillGO.GetComponent<RectTransform>();
        gaugeFillRT.anchorMin = Vector2.zero;
        gaugeFillRT.anchorMax = Vector2.one;
        gaugeFillRT.sizeDelta = Vector2.zero;
        ui.ambushRiskFillImage = gaugeFillGO.GetComponent<Image>();
        ui.ambushRiskFillImage.type = Image.Type.Filled;
        ui.ambushRiskFillImage.fillMethod = Image.FillMethod.Horizontal;
        ui.ambushRiskFillImage.fillAmount = 0f;
        ui.ambushRiskFillImage.color = new Color(0.2f, 0.85f, 0.4f);

        GameObject riskStatusGO = new GameObject("RiskStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        riskStatusGO.transform.SetParent(riskSection.transform, false);
        RectTransform riskStatusRT = riskStatusGO.GetComponent<RectTransform>();
        riskStatusRT.anchoredPosition = new Vector2(0, -38);
        riskStatusRT.sizeDelta = new Vector2(460, 18);
        ui.ambushRiskStatusText = riskStatusGO.GetComponent<TextMeshProUGUI>();
        ui.ambushRiskStatusText.fontSize = 11;
        ui.ambushRiskStatusText.alignment = TextAlignmentOptions.Center;
        ui.ambushRiskStatusText.text = "The campsite is quiet and safe.";
        ui.ambushRiskStatusText.color = new Color(0.7f, 1f, 0.7f);

        // Player Status Section
        GameObject statusGO = new GameObject("PlayerStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGO.transform.SetParent(ui.mainCampPanel.transform, false);
        RectTransform statusRT = statusGO.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0.5f, 1);
        statusRT.anchorMax = new Vector2(0.5f, 1);
        statusRT.anchoredPosition = new Vector2(0, -188);
        statusRT.sizeDelta = new Vector2(600, 22);
        ui.playerStatusText = statusGO.GetComponent<TextMeshProUGUI>();
        ui.playerStatusText.fontSize = 13;
        ui.playerStatusText.alignment = TextAlignmentOptions.Center;
        ui.playerStatusText.text = "HP: 100/100   |   Fullness: 100/100";

        GameObject buffsGO = new GameObject("PlayerBuffsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        buffsGO.transform.SetParent(ui.mainCampPanel.transform, false);
        RectTransform buffsRT = buffsGO.GetComponent<RectTransform>();
        buffsRT.anchorMin = new Vector2(0.5f, 1);
        buffsRT.anchorMax = new Vector2(0.5f, 1);
        buffsRT.anchoredPosition = new Vector2(0, -210);
        buffsRT.sizeDelta = new Vector2(600, 18);
        ui.playerBuffsText = buffsGO.GetComponent<TextMeshProUGUI>();
        ui.playerBuffsText.fontSize = 11;
        ui.playerBuffsText.alignment = TextAlignmentOptions.Center;
        ui.playerBuffsText.text = "Buffs: None";

        // Action Buttons Grid
        GameObject btnGroup = new GameObject("ActionButtons", typeof(RectTransform));
        btnGroup.transform.SetParent(ui.mainCampPanel.transform, false);
        RectTransform btnGroupRT = btnGroup.GetComponent<RectTransform>();
        btnGroupRT.anchorMin = new Vector2(0.5f, 0);
        btnGroupRT.anchorMax = new Vector2(0.5f, 0);
        btnGroupRT.anchoredPosition = new Vector2(0, 140);
        btnGroupRT.sizeDelta = new Vector2(680, 110);

        ui.eatButton = CreateCampButton(btnGroup.transform, "EatBtn", "EAT FOOD\n(+15% Risk)", new Vector2(-230, 26), new Vector2(210, 48), new Color(0.2f, 0.6f, 0.35f));
        ui.cookButton = CreateCampButton(btnGroup.transform, "CookBtn", "COOK RECIPE\n(+20% Risk)", new Vector2(0, 26), new Vector2(210, 48), new Color(0.8f, 0.45f, 0.15f));
        ui.exerciseButton = CreateCampButton(btnGroup.transform, "ExerciseBtn", "EXERCISE (-25 Full)\n(+35% Risk)", new Vector2(230, 26), new Vector2(210, 48), new Color(0.25f, 0.5f, 0.75f));
        ui.restButton = CreateCampButton(btnGroup.transform, "RestBtn", "REST (+25 HP)\n(+10% Risk)", new Vector2(-230, -30), new Vector2(210, 48), new Color(0.2f, 0.55f, 0.6f));
        ui.continueButton = CreateCampButton(btnGroup.transform, "ContinueBtn", "CONTINUE FORWARD\n(Next Stage)", new Vector2(0, -30), new Vector2(210, 48), new Color(0.3f, 0.7f, 0.3f));
        ui.returnHomeButton = CreateCampButton(btnGroup.transform, "ReturnBtn", "RETURN HOME\n(Keep All Loot)", new Vector2(230, -30), new Vector2(210, 48), new Color(0.45f, 0.3f, 0.6f));

        // Ambush Alert Banner
        ui.ambushAlertBanner = new GameObject("AmbushAlertBanner", typeof(RectTransform), typeof(Image));
        ui.ambushAlertBanner.transform.SetParent(ui.mainCampPanel.transform, false);
        RectTransform alertRT = ui.ambushAlertBanner.GetComponent<RectTransform>();
        alertRT.anchorMin = new Vector2(0, 0.5f);
        alertRT.anchorMax = new Vector2(1, 0.5f);
        alertRT.sizeDelta = new Vector2(0, 90);
        alertRT.anchoredPosition = Vector2.zero;
        Image alertImg = ui.ambushAlertBanner.GetComponent<Image>();
        alertImg.color = new Color(0.85f, 0.15f, 0.15f, 0.98f);

        GameObject alertTextGO = new GameObject("AlertText", typeof(RectTransform), typeof(TextMeshProUGUI));
        alertTextGO.transform.SetParent(ui.ambushAlertBanner.transform, false);
        RectTransform alertTextRT = alertTextGO.GetComponent<RectTransform>();
        alertTextRT.anchorMin = Vector2.zero;
        alertTextRT.anchorMax = Vector2.one;
        alertTextRT.sizeDelta = Vector2.zero;
        ui.ambushAlertText = alertTextGO.GetComponent<TextMeshProUGUI>();
        ui.ambushAlertText.fontSize = 20;
        ui.ambushAlertText.fontStyle = FontStyles.Bold;
        ui.ambushAlertText.alignment = TextAlignmentOptions.Center;
        ui.ambushAlertText.text = "AMBUSH! Monsters detected your campsite!";
        ui.ambushAlertText.color = Color.white;
        ui.ambushAlertBanner.SetActive(false);

        // Modals
        BuildFoodModal(ui, canvas.transform);
        BuildCookModal(ui, canvas.transform);
    }

    public static Button CreateCampButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        Button btn = go.GetComponent<Button>();
        DungeonUIAssetHelper.StyleButton(btn, img, color, color * 1.15f, color * 0.85f);

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.fontSize = 11;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = label;
        tmp.color = Color.white;

        return btn;
    }

    public static void BuildFoodModal(CampUI ui, Transform parent)
    {
        ui.foodModal = new GameObject("FoodModal", typeof(RectTransform), typeof(Image));
        ui.foodModal.transform.SetParent(parent, false);
        RectTransform rt = ui.foodModal.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(500, 420);
        Image bg = ui.foodModal.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.10f, 0.12f, 0.18f, 0.98f));

        // Title
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(ui.foodModal.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.anchoredPosition = new Vector2(0, -18);
        titleRT.sizeDelta = new Vector2(0, 30);
        TextMeshProUGUI tmp = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.fontSize = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = "SELECT FOOD TO EAT";

        // Scroll View
        GameObject scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
        scrollGO.transform.SetParent(ui.foodModal.transform, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(15, 60);
        scrollRT.offsetMax = new Vector2(-15, -45);
        Image maskImg = scrollGO.GetComponent<Image>();
        maskImg.color = new Color(0, 0, 0, 0.3f);
        Mask mask = scrollGO.GetComponent<Mask>();
        mask.showMaskGraphic = true;

        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(scrollGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 6;
        vlg.padding = new RectOffset(6, 6, 6, 6);

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect sr = scrollGO.GetComponent<ScrollRect>();
        sr.content = contentRT;
        sr.horizontal = false;
        sr.vertical = true;
        ui.foodListContainer = contentGO.transform;

        // No Food Text
        GameObject noFoodGO = new GameObject("NoFoodText", typeof(RectTransform), typeof(TextMeshProUGUI));
        noFoodGO.transform.SetParent(ui.foodModal.transform, false);
        RectTransform noFoodRT = noFoodGO.GetComponent<RectTransform>();
        noFoodRT.anchoredPosition = new Vector2(0, 10);
        noFoodRT.sizeDelta = new Vector2(400, 60);
        ui.noFoodText = noFoodGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(ui.noFoodText);
        ui.noFoodText.fontSize = 13;
        ui.noFoodText.alignment = TextAlignmentOptions.Center;
        ui.noFoodText.text = "No food in backpack!\n(Cook meals or harvest food from farm)";
        ui.noFoodText.color = new Color(0.7f, 0.7f, 0.7f);

        // Close Button
        ui.closeFoodModalButton = CreateCampButton(ui.foodModal.transform, "CloseBtn", "CLOSE", new Vector2(0, -180), new Vector2(120, 36), new Color(0.4f, 0.4f, 0.45f));
        ui.foodModal.SetActive(false);
    }

    public static void BuildCookModal(CampUI ui, Transform parent)
    {
        ui.cookModal = new GameObject("CookModal", typeof(RectTransform), typeof(Image));
        ui.cookModal.transform.SetParent(parent, false);
        RectTransform rt = ui.cookModal.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(500, 440);
        Image bg = ui.cookModal.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bg, new Color(0.10f, 0.12f, 0.18f, 0.98f));

        // Title
        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(ui.cookModal.transform, false);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.anchoredPosition = new Vector2(0, -18);
        titleRT.sizeDelta = new Vector2(0, 30);
        TextMeshProUGUI tmp = titleGO.GetComponent<TextMeshProUGUI>();
        DungeonUIAssetHelper.ApplyFont(tmp);
        tmp.fontSize = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = "SELECT RECIPE TO COOK";

        // Scroll View
        GameObject scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
        scrollGO.transform.SetParent(ui.cookModal.transform, false);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(15, 60);
        scrollRT.offsetMax = new Vector2(-15, -45);
        Image maskImg = scrollGO.GetComponent<Image>();
        maskImg.color = new Color(0, 0, 0, 0.3f);
        Mask mask = scrollGO.GetComponent<Mask>();
        mask.showMaskGraphic = true;

        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(scrollGO.transform, false);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 6;
        vlg.padding = new RectOffset(6, 6, 6, 6);

        ContentSizeFitter csf = contentGO.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect sr = scrollGO.GetComponent<ScrollRect>();
        sr.content = contentRT;
        sr.horizontal = false;
        sr.vertical = true;
        ui.recipeListContainer = contentGO.transform;

        // No Recipe Text
        GameObject noRecGO = new GameObject("NoRecipeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        noRecGO.transform.SetParent(ui.cookModal.transform, false);
        RectTransform noRecRT = noRecGO.GetComponent<RectTransform>();
        noRecRT.anchoredPosition = new Vector2(0, 10);
        noRecRT.sizeDelta = new Vector2(400, 60);
        ui.noRecipeText = noRecGO.GetComponent<TextMeshProUGUI>();
        ui.noRecipeText.fontSize = 13;
        ui.noRecipeText.alignment = TextAlignmentOptions.Center;
        ui.noRecipeText.text = "No recipes discovered yet!\n(Meet Merchants or offer gifts to unlock recipes)";
        ui.noRecipeText.color = new Color(0.7f, 0.7f, 0.7f);

        // Close Button
        ui.closeCookModalButton = CreateCampButton(ui.cookModal.transform, "CloseBtn", "CLOSE", new Vector2(0, -190), new Vector2(120, 36), new Color(0.4f, 0.4f, 0.45f));
        ui.cookModal.SetActive(false);
    }
}
