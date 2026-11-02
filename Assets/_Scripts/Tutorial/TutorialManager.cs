using UnityEngine;

/// <summary>
/// Master State Machine and Quest Manager for the Harvest & Hollow Tutorial.
/// Organically guides the player through 7 core action milestones:
/// Movement -> Farming -> Cooking -> Safe Chest -> Statue (Boss Gate) -> Dungeon Entrance -> Combat.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    public enum TutorialStep
    {
        None = 0,
        IntroCutscene = 1,
        Movement = 2,
        OpenInventory = 3,
        HarvestCrop = 4,
        PlantSeed = 5,
        Cooking = 6,
        HomeChest = 7,
        GoddessStatue = 8,
        DungeonGate = 9,
        Combat = 10,
        Completed = 11
    }

    public enum TutorialInteraction
    {
        Inventory,
        HarvestCrop,
        PlantSeed,
        Cooking,
        HomeChest,
        GoddessStatue,
        DungeonGate,
        BedSleep
    }

    [Header("Current Progress")]
    [SerializeField] private TutorialStep currentStep = TutorialStep.None;
    public TutorialStep CurrentStep => currentStep;
    public bool IsTutorialCompleted => currentStep == TutorialStep.Completed || PlayerPrefs.GetInt(TutorialDoneKey, 0) == 1;

    public bool IsInteractionAllowed(TutorialInteraction interaction)
    {
        if (IsTutorialCompleted || currentStep == TutorialStep.None || currentStep == TutorialStep.Completed)
            return true;

        switch (currentStep)
        {
            case TutorialStep.IntroCutscene:
                return false;

            case TutorialStep.Movement:
                // Only movement is expected, keep stations and inventory locked
                return false;

            case TutorialStep.OpenInventory:
                return interaction == TutorialInteraction.Inventory;

            case TutorialStep.HarvestCrop:
                return interaction == TutorialInteraction.HarvestCrop || interaction == TutorialInteraction.Inventory;

            case TutorialStep.PlantSeed:
                return interaction == TutorialInteraction.PlantSeed || interaction == TutorialInteraction.HarvestCrop || interaction == TutorialInteraction.Inventory;

            case TutorialStep.Cooking:
                return interaction == TutorialInteraction.Cooking || interaction == TutorialInteraction.HarvestCrop || interaction == TutorialInteraction.PlantSeed || interaction == TutorialInteraction.Inventory;

            case TutorialStep.HomeChest:
                return interaction == TutorialInteraction.HomeChest || interaction == TutorialInteraction.Inventory;

            case TutorialStep.GoddessStatue:
                return interaction == TutorialInteraction.GoddessStatue || interaction == TutorialInteraction.HarvestCrop || interaction == TutorialInteraction.Inventory;

            case TutorialStep.DungeonGate:
                return interaction == TutorialInteraction.DungeonGate || interaction == TutorialInteraction.Inventory;

            case TutorialStep.Combat:
                return true;

            default:
                return true;
        }
    }

    public const string TutorialDoneKey = "HarvestHollow_Tutorial_Completed";
    public const string TutorialStepKey = "HarvestHollow_Tutorial_CurrentStep";
    public const string DungeonTutorialDoneKey = "HarvestHollow_DungeonTutorial_Completed";

    private Vector3 lastPlayerPos;
    private float distanceTraveled = 0f;
    private float lastReportedProgress = -1f;
    private float inventoryOpenedTimer = 0f;
    private int cropsHarvestedInTutorial = 0;
    private const int RequiredHarvestCount = 3;
    private bool isAdvancingStep = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject managerGO = new GameObject("TutorialManager");
            Instance = managerGO.AddComponent<TutorialManager>();
            managerGO.AddComponent<IntroCutsceneController>();
            managerGO.AddComponent<TutorialDialogueUI>();
            DontDestroyOnLoad(managerGO);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Check if tutorial is already finished
        if (PlayerPrefs.GetInt(TutorialDoneKey, 0) == 1)
        {
            currentStep = TutorialStep.Completed;
            return;
        }

        int savedStep = PlayerPrefs.GetInt(TutorialStepKey, (int)TutorialStep.IntroCutscene);
        currentStep = (TutorialStep)savedStep;

        EnsureTutorialCropsReady();

        if (currentStep == TutorialStep.IntroCutscene)
        {
            // Intro Cutscene Controller will call OnIntroCutsceneFinished when done
        }
        else
        {
            SetStep(currentStep);
        }
    }

    /// <summary>
    /// Ensures 6 closest farm plots to the player have ripe, mature carrots ready for harvesting,
    /// and grants 3 carrot seeds so the player has enough resources for cooking, planting, and statue offerings.
    /// </summary>
    public void EnsureTutorialCropsReady()
    {
        // If tutorial has already completed, do not tamper with user's farm
        if (PlayerPrefs.GetInt(TutorialDoneKey, 0) == 1) return;

        // Only plant if player is on intro or early farming steps
        if (currentStep > TutorialStep.PlantSeed && currentStep != TutorialStep.IntroCutscene && currentStep != TutorialStep.None) return;

        FarmPlot[] allPlots = Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (allPlots == null || allPlots.Length == 0) return;

        int readyCount = 0;
        for (int i = 0; i < allPlots.Length; i++)
        {
            if (allPlots[i] != null && allPlots[i].currentState == FarmPlot.State.Ready && allPlots[i].currentCrop != null)
            {
                readyCount++;
            }
        }

        // Prepare 6 plots closest to the house/bed with ripe Carrots
        if (readyCount < 4)
        {
            CropData carrot = null;
            CropData[] allCrops = GameAssetHelper.LoadAll<CropData>();
            for (int i = 0; i < allCrops.Length; i++)
            {
                if (allCrops[i] != null && allCrops[i].cropName.ToLower().Contains("carrot"))
                {
                    carrot = allCrops[i];
                    break;
                }
            }

            if (carrot != null)
            {
                Vector3 playerPos = PlayerMovement.Instance != null ? PlayerMovement.Instance.transform.position : new Vector3(12f, 0.5f, 0f);
                System.Array.Sort(allPlots, (a, b) =>
                    Vector3.Distance(a.transform.position, playerPos).CompareTo(Vector3.Distance(b.transform.position, playerPos)));

                int countToPlant = Mathf.Min(6, allPlots.Length);
                for (int i = 0; i < countToPlant; i++)
                {
                    FarmPlot p = allPlots[i];
                    if (p != null)
                    {
                        p.currentState = FarmPlot.State.Ready;
                        p.currentCrop = carrot;
                        int maxStage = (carrot.growthStages != null && carrot.growthStages.Length > 0) ? carrot.growthStages.Length - 1 : 0;
                        p.currentStage = maxStage;
                        p.currentGrowthDay = carrot.growthTime * maxStage;
                        p.daysInCurrentStage = carrot.growthTime;
                        p.UpdateCropVisual();
                        p.SavePlotData(true);
                    }
                }

                // Grant 3 Carrot Seeds to player's inventory so they can practice planting
                PlayerStats ps = Object.FindFirstObjectByType<PlayerStats>();
                if (ps != null && ps.itemContainer != null && carrot.seedItem != null)
                {
                    if (!ps.itemContainer.HasItem(carrot.seedItem))
                    {
                        ps.itemContainer.AddItem(carrot.seedItem, 3);
                    }
                }
            }
        }
    }

    private FarmPlot GetNearestPlot(FarmPlot.State state)
    {
        FarmPlot[] plots = Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (plots == null || plots.Length == 0) return null;

        Vector3 pPos = PlayerMovement.Instance != null ? PlayerMovement.Instance.transform.position : new Vector3(12f, 0.5f, 0f);
        System.Array.Sort(plots, (a, b) =>
            Vector3.Distance(a.transform.position, pPos).CompareTo(Vector3.Distance(b.transform.position, pPos)));

        for (int i = 0; i < plots.Length; i++)
        {
            if (plots[i] != null && plots[i].currentState == state)
            {
                return plots[i];
            }
        }
        return plots[0];
    }

    public CropData GetCarrotCropData()
    {
        CropData[] allCrops = GameAssetHelper.LoadAll<CropData>();
        for (int i = 0; i < allCrops.Length; i++)
        {
            if (allCrops[i] != null && allCrops[i].cropName.ToLower().Contains("carrot"))
            {
                return allCrops[i];
            }
        }
        return null;
    }

    public void EnsureItemInInventory(ItemData item, int minCount)
    {
        if (item == null) return;
        PlayerStats ps = Object.FindFirstObjectByType<PlayerStats>();
        if (ps != null && ps.itemContainer != null)
        {
            int currentCount = 0;
            for (int i = 0; i < ps.itemContainer.itemSlots.Length; i++)
            {
                var slot = ps.itemContainer.itemSlots[i];
                if (slot != null && slot.itemData == item)
                {
                    currentCount += slot.amount;
                }
            }
            if (currentCount < minCount)
            {
                ps.itemContainer.AddItem(item, minCount - currentCount);
            }
        }
    }

    private void Update()
    {
        if (currentStep == TutorialStep.Completed || currentStep == TutorialStep.None) return;

        // Step 1: Track movement
        if (currentStep == TutorialStep.Movement)
        {
            PlayerMovement pm = PlayerMovement.Instance;
            if (pm != null)
            {
                if (lastPlayerPos != Vector3.zero)
                {
                    distanceTraveled += Vector3.Distance(pm.transform.position, lastPlayerPos);
                    if (Mathf.Abs(distanceTraveled - lastReportedProgress) >= 0.1f || distanceTraveled >= 6.0f)
                    {
                        lastReportedProgress = distanceTraveled;
                        TutorialDialogueUI.Instance?.UpdateDynamicProgress(
                            $"Tiến độ di chuyển: <color=#FFD700><b>{Mathf.Min(6f, distanceTraveled):F1}m / 6.0m</b></color>"
                        );
                    }
                    if (distanceTraveled >= 6.0f)
                    {
                        AdvanceToNextStep();
                    }
                }
                lastPlayerPos = pm.transform.position;
            }
        }
        // Step 2: Track opening inventory
        else if (currentStep == TutorialStep.OpenInventory)
        {
            if (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen)
            {
                inventoryOpenedTimer += Time.deltaTime;
                if (inventoryOpenedTimer >= 1.0f)
                {
                    AdvanceToNextStep();
                }
            }
        }
    }

    public void OnIntroCutsceneFinished()
    {
        SetStep(TutorialStep.Movement);
    }

    public void SetStep(TutorialStep newStep)
    {
        isAdvancingStep = false;
        currentStep = newStep;
        PlayerPrefs.SetInt(TutorialStepKey, (int)currentStep);
        PlayerPrefs.Save();

        switch (currentStep)
        {
            case TutorialStep.Movement:
                distanceTraveled = 0f;
                lastPlayerPos = PlayerMovement.Instance != null ? PlayerMovement.Instance.transform.position : Vector3.zero;
                TutorialDialogueUI.Instance?.ShowStep(new TutorialStepInfo
                {
                    stepIndex = 1,
                    totalSteps = 8,
                    objectiveTitle = "Di Chuyển & Khám Phá Nông Trại",
                    targetLabel = "",
                    actionKeyPrompt = "[WASD] / [Shift] Chạy Nhanh",
                    actionInstruction = "Dùng phím <color=#50FA7B><b>[W][A][S][D]</b></color> để bước đi, giữ <color=#50FA7B><b>[Shift]</b></color> chạy nhanh quanh sân.",
                    fullDialogue = "Chào mừng bạn đến với Harvest & Hollow! Tôi là Tinh Linh Mầm Cây, sẽ đồng hành hướng dẫn bạn.\n" +
                                   "- Dùng các phím <color=#50FA7B><b>[W, A, S, D]</b></color> để di chuyển bước chân đầu tiên quanh nông trại.\n" +
                                   "- Giữ phím <color=#50FA7B><b>[Left Shift]</b></color> khi đi để chạy nhanh (tăng 1.5x tốc độ).\n" +
                                   "Hãy đi dạo một đoạn 6 mét quanh sân nông trại để làm quen cơ thể nhé!",
                    targetPosition = null,
                    targetTransform = null
                });
                break;

            case TutorialStep.OpenInventory:
                inventoryOpenedTimer = 0f;
                TutorialDialogueUI.Instance?.ShowStep(new TutorialStepInfo
                {
                    stepIndex = 2,
                    totalSteps = 8,
                    objectiveTitle = "Mở Ba Lô Hành Trang",
                    targetLabel = "",
                    actionKeyPrompt = "[I] / [B] MỞ TÚI ĐỒ",
                    actionInstruction = "Nhấn phím <color=#FFEA30><b>[I]</b></color> hoặc <color=#FFEA30><b>[B]</b></color> mở Ba lô kiểm tra hành trang và vật phẩm của bạn.",
                    fullDialogue = "Nhấn [I] hoặc [B] để mở Ba lô nhé! Đây là nơi lưu giữ toàn bộ nông sản, hạt giống và vật phẩm bạn thu thập được.\n" +
                                   "- Bạn có thể kéo thả để sắp xếp các vật phẩm trong ba lô.\n" +
                                   "- Các ô ở hàng đầu tiên tương ứng với thanh Hotbar phím tắt nhanh ở đáy màn hình.\n" +
                                   "Sau khi xem xong, nhấn [I] hoặc [ESC] để đóng túi đồ.",
                    targetPosition = null,
                    targetTransform = null
                });
                break;

            case TutorialStep.HarvestCrop:
                EnsureTutorialCropsReady();
                cropsHarvestedInTutorial = 0;

                FarmPlot targetPlot = GetNearestPlot(FarmPlot.State.Ready);
                Vector3 targetPos = targetPlot != null ? targetPlot.transform.position : new Vector3(-0.5f, -2.5f, 0f);

                TutorialDialogueUI.Instance?.ShowStep(new TutorialStepInfo
                {
                    stepIndex = 3,
                    totalSteps = 8,
                    objectiveTitle = "Thu Hoạch Nông Sản Chín Vàng",
                    targetLabel = "Ô Đất Cà Rốt Chín",
                    actionKeyPrompt = "[CLICK CHUỘT TRÁI] THU HOẠCH (0/3)",
                    actionInstruction = "Đi sang ruộng bên trái (xem mũi tên), <color=#FFEA30><b>Click Chuột Trái</b></color> vào 3 luống Cà Rốt chín để thu hoạch đủ 9 củ Cà Rốt.",
                    fullDialogue = "Tại thế giới này không có tiền vàng đâu, nông sản chính là nguồn sức mạnh duy nhất!\n" +
                                   "- Luống đất có Cà Rốt chín vàng rực rỡ ở bên trái đang chờ bạn thu hoạch.\n" +
                                   "- <color=#FFEA30><b>Click Chuột Trái</b></color> trực tiếp vào 3 luống Cà Rốt chín để nhổ 9 củ Cà Rốt tươi vào ba lô nhé!",
                    targetPosition = targetPos,
                    targetTransform = targetPlot != null ? targetPlot.transform : null
                });
                break;

            case TutorialStep.PlantSeed:
                CropData cCrop = GetCarrotCropData();
                if (cCrop != null && cCrop.seedItem != null)
                {
                    EnsureItemInInventory(cCrop.seedItem, 3);
                }

                FarmPlot emptyPlot = GetNearestPlot(FarmPlot.State.Empty);
                Vector3 emptyPos = emptyPlot != null ? emptyPlot.transform.position : new Vector3(-0.5f, -2.5f, 0f);

                TutorialDialogueUI.Instance?.ShowStep(new TutorialStepInfo
                {
                    stepIndex = 4,
                    totalSteps = 8,
                    objectiveTitle = "Gieo Hạt Giống Mới",
                    targetLabel = "Ô Đất Trống",
                    actionKeyPrompt = "Chọn Hạt -> Click Chuột Vào Đất",
                    actionInstruction = "Chọn <color=#50FA7B><b>Hạt Giống Cà Rốt</b></color> trên thanh Hotbar, rồi <color=#FFEA30><b>Click Chuột Trái</b></color> vào ô đất trống để gieo hạt.",
                    fullDialogue = "Đất trống rồi đừng để lãng phí! Quy trình trồng trọt rất đơn giản:\n" +
                                   "- Bước 1: Nhìn xuống thanh Hotbar (11 ô ở đáy màn hình), click hoặc bấm phím số để chọn <color=#50FA7B><b>[Hạt Giống Cà Rốt]</b></color>.\n" +
                                   "- Bước 2: Di chuột vào ô đất trống vừa thu hoạch và <color=#FFEA30><b>Click Chuột Trái</b></color> để gieo hạt xuống đất.\n" +
                                   "Cây sẽ tự động lớn lên khi ngày trôi qua (khi bạn ngủ hoặc đi qua các phòng hầm ngục)!",
                    targetPosition = emptyPos,
                    targetTransform = emptyPlot != null ? emptyPlot.transform : null
                });
                break;

            case TutorialStep.Cooking:
                CropData cCropCook = GetCarrotCropData();
                if (cCropCook != null && cCropCook.producedItem != null)
                {
                    EnsureItemInInventory(cCropCook.producedItem, 3);
                }

                CookingManager.Instance?.EnsureKitchenStationInBase();
                KitchenStation kitchen = KitchenStation.Instance != null ? KitchenStation.Instance : Object.FindFirstObjectByType<KitchenStation>();
                Vector3 kPos = kitchen != null ? kitchen.transform.position : new Vector3(15.365f, -3.413f, 0f);

                TutorialDialogueUI.Instance?.ShowStep(new TutorialStepInfo
                {
                    stepIndex = 5,
                    totalSteps = 8,
                    objectiveTitle = "Nấu Ăn: Chế Biến Súp Tốc Độ",
                    targetLabel = "Bếp Lửa Trại (Kitchen)",
                    actionKeyPrompt = "[E] MỞ BẾP -> [Nấu Ăn]",
                    actionInstruction = "Đến Bếp Lửa lửa trại, nhấn <color=#FFEA30><b>[E]</b></color>, chọn Súp Cà Rốt (tốn 3 Cà Rốt) và bấm <color=#FFA726><b>[Nấu Ăn]</b></color>.",
                    fullDialogue = "Đừng ăn nông sản sống, hãy chế biến thành món ăn hoàn chỉnh!\n" +
                                   "- Đi lại Bếp Lửa Trại phía trên, nhấn phím <color=#FFEA30><b>[E]</b></color> để mở Bếp.\n" +
                                   "- Chọn món <color=#FFA726><b>[Súp Cà Rốt]</b></color> (cần 3 Cà Rốt) và bấm nút <color=#FFA726><b>[Nấu Ăn]</b></color>.\n" +
                                   "- Súp Cà Rốt hồi +18 HP, +25 Độ No và ban tặng <color=#FFEE55><b>Buff Tốc Độ (Speed)</b></color> cực mạnh giúp bạn luôn được đi trước quái vật trong hầm ngục! Giữ bát súp trong ba lô nhé!",
                    targetPosition = kPos,
                    targetTransform = kitchen != null ? kitchen.transform : null
                });
                break;

            case TutorialStep.HomeChest:
                HomeChest chest = HomeChest.Instance != null ? HomeChest.Instance : Object.FindFirstObjectByType<HomeChest>();
                Vector3 cPos = chest != null ? chest.transform.position : new Vector3(10.985f, -1.701f, 0f);

                TutorialDialogueUI.Instance?.ShowStep(new TutorialStepInfo
                {
                    stepIndex = 6,
                    totalSteps = 8,
                    objectiveTitle = "Rương Nhà: Bảo Hiểm An Toàn 100%",
                    targetLabel = "Rương Nhà An Toàn",
                    actionKeyPrompt = "[E] MỞ RƯƠNG -> Cất Đồ",
                    actionInstruction = "Đến Rương Nhà cạnh lều, nhấn <color=#FFEA30><b>[E]</b></color> mở rương cất bớt hạt giống phòng rủi ro khi tử trận.",
                    fullDialogue = "CẢNH BÁO SINH TỬ VỀ HẦM NGỤC:\n" +
                                   "- Nếu bạn ngã xuống (HP = 0) trong hầm ngục, bạn sẽ <color=#FF6B6B><b>MẤT TOÀN BỘ VẬT PHẨM TRONG BA LÔ!</b></color>\n" +
                                   "- Hãy đi lại chiếc Rương Nhà bằng gỗ màu nâu cạnh lều, nhấn phím <color=#FFEA30><b>[E]</b></color> mở rương và cất bớt hạt giống quý vào đây.\n" +
                                   "Tất cả nông sản và hạt giống trong Rương Nhà được bảo hiểm an toàn tuyệt đối 100%! Nhấn [ESC] đóng rương để tiếp tục.",
                    targetPosition = cPos,
                    targetTransform = chest != null ? chest.transform : null
                });
                break;

            case TutorialStep.GoddessStatue:
                CropData cCropStatue = GetCarrotCropData();
                if (cCropStatue != null && cCropStatue.producedItem != null)
                {
                    EnsureItemInInventory(cCropStatue.producedItem, 4);
                }

                Statue statue = Statue.Instance != null ? Statue.Instance : Object.FindFirstObjectByType<Statue>();
                Vector3 sPos = statue != null ? statue.GetStatueCenter() : new Vector3(40.5f, 10.9f, 0f);

                TutorialDialogueUI.Instance?.ShowStep(new TutorialStepInfo
                {
                    stepIndex = 7,
                    totalSteps = 8,
                    objectiveTitle = "Tượng Nữ Thần: Dâng Lễ Tăng Chỉ Số",
                    targetLabel = "Tượng Nữ Thần (Ancient Shrine)",
                    actionKeyPrompt = "[E] DÂNG LỄ (4 Cà Rốt)",
                    actionInstruction = "Đến Tượng Thần ở phía Đông, nhấn <color=#FFEA30><b>[E]</b></color> dâng 4 Cà Rốt nhận <color=#FF6B6B><b>+1 ATK</b></color> vĩnh viễn.",
                    fullDialogue = "Đây là Tượng Nữ Thần Cổ Đại — nơi gia tăng sức mạnh vĩnh viễn cho bạn:\n" +
                                   "- Đến trước Tượng Thần ở phía Đông, nhấn phím <color=#FFEA30><b>[E]</b></color> mở giao diện Dâng Lễ.\n" +
                                   "- Chọn Trụ Chiến Binh, bấm nút <color=#FFA726><b>[Dâng Lễ Nhanh]</b></color> để cúng 4 củ Cà Rốt nhận ngay <color=#FF6B6B><b>+1 Sức Đánh (ATK) Vĩnh Viễn!</b></color>\n" +
                                   "Có 4 trụ cột: HP, ATK, DEF và Speed. Càng dâng lễ bậc cao, bạn càng mở khóa nhiều Phước Lành Thần Thánh!",
                    targetPosition = sPos,
                    targetTransform = statue != null ? statue.transform : null
                });
                break;

            case TutorialStep.DungeonGate:
                DungeonEntrance entrance = DungeonEntrance.Instance != null ? DungeonEntrance.Instance : Object.FindFirstObjectByType<DungeonEntrance>();
                Vector3 dPos = entrance != null ? entrance.transform.position : new Vector3(40.506f, -6.311f, 0f);

                TutorialDialogueUI.Instance?.ShowStep(new TutorialStepInfo
                {
                    stepIndex = 8,
                    totalSteps = 8,
                    objectiveTitle = "Cổng Hầm Ngục: Kích Hoạt Phước Lành",
                    targetLabel = "Cổng Hầm Ngục (Dungeon Gate)",
                    actionKeyPrompt = "[E] VÀO CỔNG -> [Tiến Vào]",
                    actionInstruction = "Đến Cổng Hầm Ngục ngay cạnh Tượng Thần, nhấn <color=#FFEA30><b>[E]</b></color> nhận Phước Lành miễn phí và bấm <color=#50FA7B>[Tiến Vào]</color>.",
                    fullDialogue = "Bạn đã nắm vững toàn bộ chu trình sinh tồn ở Căn cứ!\n" +
                                   "- Bụng no, bát Súp Tốc Độ sẵn sàng trong túi, +1 ATK vĩnh viễn đã kích hoạt và hạt giống đã cất an toàn!\n" +
                                   "- Bước lại Cổng Hầm Ngục ngay cạnh Tượng Thần, nhấn phím <color=#FFEA30><b>[E]</b></color>.\n" +
                                   "- Nhận Phước Lành 'Bảo Hộ Thần Thánh' (bảo vệ đồ miễn phí) và bấm <color=#50FA7B><b>[Tiến Vào]</b></color> để bước vào Hầm Ngục Hướng Dẫn (Ải 0)!",
                    targetPosition = dPos,
                    targetTransform = entrance != null ? entrance.transform : null
                });
                break;

            case TutorialStep.Combat:
                TutorialDialogueUI.Instance?.HideDialogue();
                break;

            case TutorialStep.Completed:
                PlayerPrefs.SetInt(TutorialDoneKey, 1);
                PlayerPrefs.Save();
                TutorialDialogueUI.Instance?.HideDialogue();
                break;
        }
    }

    public void AdvanceToNextStep()
    {
        if (isAdvancingStep) return;
        isAdvancingStep = true;

        int next = (int)currentStep + 1;
        if (next <= (int)TutorialStep.Completed)
        {
            if (TutorialDialogueUI.Instance != null)
            {
                TutorialDialogueUI.Instance.ShowCompletionFeedback(() =>
                {
                    isAdvancingStep = false;
                    SetStep((TutorialStep)next);
                });
            }
            else
            {
                isAdvancingStep = false;
                SetStep((TutorialStep)next);
            }
        }
        else
        {
            isAdvancingStep = false;
        }
    }

    // Callbacks from other game systems:
    public void NotifyCropHarvested()
    {
        if (currentStep == TutorialStep.HarvestCrop)
        {
            cropsHarvestedInTutorial++;
            TutorialDialogueUI.Instance?.UpdateDynamicProgress(
                $"Tiến độ thu hoạch: <color=#FFD700><b>{cropsHarvestedInTutorial} / {RequiredHarvestCount} luống</b></color>"
            );

            if (cropsHarvestedInTutorial >= RequiredHarvestCount)
            {
                AdvanceToNextStep();
                return;
            }

            // Fallback: If no more ready plots exist in the farm, advance to avoid stalling
            FarmPlot[] allPlots = Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int readyPlots = 0;
            if (allPlots != null)
            {
                for (int i = 0; i < allPlots.Length; i++)
                {
                    if (allPlots[i] != null && allPlots[i].currentState == FarmPlot.State.Ready) readyPlots++;
                }
            }
            if (readyPlots == 0)
            {
                AdvanceToNextStep();
            }
        }
    }

    public void NotifyCropPlanted()
    {
        if (currentStep == TutorialStep.PlantSeed)
        {
            AdvanceToNextStep();
        }
    }

    public void NotifyCropHarvestedOrPlanted()
    {
        if (currentStep == TutorialStep.HarvestCrop)
        {
            NotifyCropHarvested();
        }
        else if (currentStep == TutorialStep.PlantSeed)
        {
            NotifyCropPlanted();
        }
    }

    public void NotifyDishCooked()
    {
        if (currentStep == TutorialStep.Cooking)
        {
            AdvanceToNextStep();
        }
    }

    public void NotifyChestUsed()
    {
        if (currentStep == TutorialStep.HomeChest)
        {
            AdvanceToNextStep();
        }
    }

    public void NotifyStatueOfferingDone()
    {
        if (currentStep == TutorialStep.GoddessStatue)
        {
            AdvanceToNextStep();
        }
    }

    public void NotifyDungeonEntered()
    {
        TutorialDialogueUI.Instance?.HideDialogue();
        if (currentStep == TutorialStep.DungeonGate)
        {
            AdvanceToNextStep();
        }
    }

    public void NotifyCombatFinished()
    {
        if (currentStep == TutorialStep.Combat)
        {
            AdvanceToNextStep();
        }
    }

    /// <summary>
    /// Resets the tutorial progress so player can replay from start.
    /// </summary>
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(TutorialDoneKey);
        PlayerPrefs.DeleteKey(TutorialStepKey);
        PlayerPrefs.DeleteKey(DungeonTutorialDoneKey);
        PlayerPrefs.DeleteKey("HarvestHollow_CombatTutorial_Completed");
        PlayerPrefs.DeleteKey("HarvestHollow_IntroCutscene_Played");
        PlayerPrefs.DeleteKey("Tut_Event_Intro_Seen");
        PlayerPrefs.DeleteKey("Tut_Merchant_Intro_Seen");
        PlayerPrefs.DeleteKey("Tut_Camp_Intro_Seen");
        PlayerPrefs.Save();
        SetStep(TutorialStep.IntroCutscene);
    }
}
