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
                // Allow opening inventory at all times to check bag
                return interaction == TutorialInteraction.Inventory;

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

        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(activeScene) || activeScene == "Base" || activeScene.Contains("Base"))
        {
            Instance.EnsureTutorialCropsReady();
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
        EnsureTutorialCropsReady();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name == "Base" || scene.name.Contains("Base"))
        {
            EnsureTutorialCropsReady();

            // When returning to Base, re-bind active tutorial step to new scene's live transforms and targets
            if (currentStep > TutorialStep.None && currentStep < TutorialStep.Combat && currentStep != TutorialStep.IntroCutscene)
            {
                SetStep(currentStep);
            }
            else if (currentStep == TutorialStep.Combat)
            {
                if (PlayerPrefs.GetInt(TutorialDoneKey, 0) == 1)
                {
                    currentStep = TutorialStep.Completed;
                }
                else
                {
                    // Returned before completing combat (e.g. retreated) -> guide player back to Dungeon Gate
                    SetStep(TutorialStep.DungeonGate);
                }
            }
        }
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

    private static float _lastEnsureTutorialCropsTime = -1f;

    /// <summary>
    /// Ensures 6 closest farm plots to the player have ripe, mature carrots ready for harvesting,
    /// and grants 3 carrot seeds so the player has enough resources for cooking, planting, and statue offerings.
    /// </summary>
    public void EnsureTutorialCropsReady()
    {
        // Prevent duplicate scene-wide scans within the same fraction of a second
        if (Time.realtimeSinceStartup - _lastEnsureTutorialCropsTime < 0.25f) return;
        _lastEnsureTutorialCropsTime = Time.realtimeSinceStartup;

        // If tutorial has already completed, do not tamper with user's farm
        if (PlayerPrefs.GetInt(TutorialDoneKey, 0) == 1) return;

        // Only prepare starter crops during initial tutorial steps (Intro, Movement, Inventory, HarvestCrop)
        if (currentStep > TutorialStep.HarvestCrop && currentStep != TutorialStep.IntroCutscene && currentStep != TutorialStep.None) return;
        if (PlayerPrefs.GetInt("Tutorial_StarterCropsPrepared", 0) == 1 && currentStep >= TutorialStep.PlantSeed) return;

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

            // Fallback: search available crops directly on scene plots
            if (carrot == null)
            {
                for (int i = 0; i < allPlots.Length; i++)
                {
                    if (allPlots[i] != null && allPlots[i].availableCrops != null)
                    {
                        for (int j = 0; j < allPlots[i].availableCrops.Count; j++)
                        {
                            if (allPlots[i].availableCrops[j] != null && allPlots[i].availableCrops[j].cropName.ToLower().Contains("carrot"))
                            {
                                carrot = allPlots[i].availableCrops[j];
                                break;
                            }
                        }
                    }
                    if (carrot != null) break;
                }
            }

            if (carrot != null)
            {
                // Fixed reference anchor: Eastern entrance of farm plots towards the house / bed
                Vector3 farmEntranceAnchor = new Vector3(12f, 0.5f, 0f);
                System.Array.Sort(allPlots, (a, b) =>
                    Vector3.Distance(a.transform.position, farmEntranceAnchor).CompareTo(Vector3.Distance(b.transform.position, farmEntranceAnchor)));

                int countToPlant = Mathf.Min(6, allPlots.Length);
                for (int i = 0; i < countToPlant; i++)
                {
                    FarmPlot p = allPlots[i];
                    if (p != null)
                    {
                        // Never overwrite a plot that is already planted by the player
                        if (p.currentState == FarmPlot.State.Planted) continue;

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
                PlayerPrefs.SetInt("Tutorial_StarterCropsPrepared", 1);
                PlayerPrefs.Save();

                // Grant 3 Carrot Seeds to player's inventory so they can practice planting
                PlayerStats ps = PlayerStats.Instance != null ? PlayerStats.Instance : Object.FindFirstObjectByType<PlayerStats>();
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

    public int GetItemCount(ItemContainer container, ItemData item)
    {
        if (container == null || item == null || container.itemSlots == null) return 0;
        int count = 0;
        for (int i = 0; i < container.itemSlots.Length; i++)
        {
            var slot = container.itemSlots[i];
            if (slot != null && ItemContainer.IsItemMatch(slot.itemData, item))
            {
                count += slot.amount;
            }
        }
        return count;
    }

    public void EnsureItemInInventory(ItemData item, int minCount)
    {
        if (item == null) return;
        PlayerStats ps = PlayerStats.Instance != null ? PlayerStats.Instance : Object.FindFirstObjectByType<PlayerStats>();
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
                    const float requiredDistance = 2.0f;
                    distanceTraveled += Vector3.Distance(pm.transform.position, lastPlayerPos);
                    if (Mathf.Abs(distanceTraveled - lastReportedProgress) >= 0.1f || distanceTraveled >= requiredDistance)
                    {
                        lastReportedProgress = distanceTraveled;
                        TutorialDialogueUI.Instance?.UpdateDynamicProgress(
                            $"Tiến độ di chuyển: <color=#FFD700><b>{Mathf.Min(requiredDistance, distanceTraveled):F1}m / {requiredDistance:F1}m</b></color>"
                        );
                    }
                    if (distanceTraveled >= requiredDistance)
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
                AdvanceToNextStep();
            }
        }
    }

    public void OnIntroCutsceneFinished()
    {
        EnsureTutorialCropsReady();
        SetStep(TutorialStep.Movement);
    }

    public void SetStep(TutorialStep newStep)
    {
        isAdvancingStep = false;
        currentStep = newStep;
        PlayerPrefs.SetInt(TutorialStepKey, (int)currentStep);
        PlayerPrefs.Save();

        // Always ensure tutorial crops are visible on all early steps
        if (currentStep <= TutorialStep.HarvestCrop)
        {
            EnsureTutorialCropsReady();
        }

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
                                   "Hãy bước vài bước (khoảng 2 mét) quanh sân nông trại để làm quen cơ thể nhé!",
                    targetPosition = null,
                    targetTransform = null
                });
                break;

            case TutorialStep.OpenInventory:
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
                                   "- Bước 1: Chọn <color=#50FA7B><b>[Hạt Giống Cà Rốt]</b></color> trên thanh Hotbar ở đáy màn hình.\n" +
                                   "- Bước 2: Di chuột vào ô đất trống vừa thu hoạch và <color=#FFEA30><b>Click Chuột Trái</b></color> để gieo hạt.\n" +
                                   "- <color=#FFD700><b>Đồng Hồ Sinh Học:</b></color> Khi đi thám hiểm hầm ngục, cây ở nhà vẫn âm thầm lớn lên (cứ mỗi 2 phòng vượt qua = 1 Ngày trôi qua ở Nông Trại). Vượt ải trở về là mùa màng đã chín rộ!",
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
                    objectiveTitle = "Nấu Ăn: Tìm Hiểu Độ No & Tốc Độ",
                    targetLabel = "Bếp Lửa Trại (Kitchen)",
                    actionKeyPrompt = "[E] MỞ BẾP -> [Nấu Ăn]",
                    actionInstruction = "Đến Bếp Lửa trại, nhấn <color=#FFEA30><b>[E]</b></color>, chọn Súp Cà Rốt (tốn 3 Cà Rốt) và bấm <color=#FFA726><b>[Nấu Ăn]</b></color>.",
                    fullDialogue = "Nấu ăn là chìa khóa sinh tồn để chi phối Tốc Độ và Độ No:\n" +
                                   "- Nhấn <color=#FFEA30><b>[E]</b></color> mở Bếp, chọn <color=#FFA726><b>[Súp Cà Rốt]</b></color> (tốn 3 Cà Rốt) và bấm [Nấu Ăn].\n" +
                                   "- <color=#50FA7B><b>Tốc Độ (SPD):</b></color> Ai có SPD cao hơn sẽ RA ĐÒN TRƯỚC! Ăn Súp (+2 SPD) giúp bạn đạt 11 SPD > 10 SPD của quái để giành quyền đi trước.\n" +
                                   "- <color=#FFD700><b>Độ No (Fullness):</b></color> Có 3 mốc (hiển thị bằng icon tròn dưới thanh máu):\n" +
                                   "  + <color=#50FA7B><b>No Nê (30-79%):</b></color> +2 ATK, +1 DEF, +2 SPD!  |  <color=#FF6B6B><b>Đói (<30%):</b></color> -2 ATK, -3 SPD.\n" +
                                   "  + <color=#FFA500><b>Bội Thực (>=80%):</b></color> Hồi máu nhưng bị giảm -4 SPD.\n" +
                                   "Rê chuột vào icon dưới thanh máu để xem chi tiết. Nhớ chuẩn bị thức ăn trước khi xuống hầm ngục nhé!",
                    targetPosition = kPos,
                    targetTransform = kitchen != null ? kitchen.transform : null
                });
                break;

            case TutorialStep.HomeChest:
                // Ensure player has the 4 carrots in backpack before opening chest, so they see them clearly
                CropData cCropChest = GetCarrotCropData();
                if (cCropChest != null && cCropChest.producedItem != null)
                {
                    EnsureItemInInventory(cCropChest.producedItem, 4);
                }

                HomeChest chest = HomeChest.Instance != null ? HomeChest.Instance : Object.FindFirstObjectByType<HomeChest>();
                Vector3 cPos = chest != null ? chest.transform.position : new Vector3(10.985f, -1.701f, 0f);

                TutorialDialogueUI.Instance?.ShowStep(new TutorialStepInfo
                {
                    stepIndex = 6,
                    totalSteps = 8,
                    objectiveTitle = "Rương Nhà: Soạn Ba Lô Đi Chiến Dịch",
                    targetLabel = "Rương Nhà An Toàn",
                    actionKeyPrompt = "[E] MỞ RƯƠNG -> Cất Hạt Giống",
                    actionInstruction = "Đến Rương Nhà cạnh lều, nhấn <color=#FFEA30><b>[E]</b></color> cất các túi Hạt Giống vào để bảo vệ. LƯU Ý: GIỮ LẠI 4 CÀ RỐT & SÚP trong ba lô!",
                    fullDialogue = "QUY TẮC SOẠN BA LÔ TRƯỚC KHI VÀO HẦM NGỤC:\n" +
                                   "- Nếu ngã xuống (HP = 0) trong hầm ngục, bạn sẽ <color=#FF6B6B><b>MẤT TOÀN BỘ VẬT PHẨM TRONG BA LÔ!</b></color>\n" +
                                   "- Hãy mở Rương Nhà và <color=#50FA7B><b>CẤT CÁC TÚI HẠT GIỐNG</b></color> quý vào đây để bảo hiểm an toàn 100%.\n" +
                                   "- <color=#FFEA30><b>ĐẶC BIỆT LƯU Ý - ĐỪNG CẤT HẾT:</b></color>\n" +
                                   "  + <color=#50FA7B><b>Giữ lại ít nhất 4 củ Cà Rốt:</b></color> Để bước tiếp theo dâng lễ Tượng Thần nhận <color=#FF6B6B><b>+1 ATK Vĩnh Viễn!</b></color>\n" +
                                   "  + <color=#50FA7B><b>Giữ lại bát Súp Cà Rốt:</b></color> Để tăng tốc độ và hồi phục khi chiến đấu.\n" +
                                   "Sau khi cất hạt giống xong, nhấn [ESC] đóng rương để tiếp tục sang phía Đông!",
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
                    actionInstruction = "Đến Tượng Thần ở phía Đông, nhấn <color=#FFEA30><b>[E]</b></color> dùng 4 Cà Rốt đã giữ lại để dâng lễ nhận <color=#FF6B6B><b>+1 ATK</b></color> vĩnh viễn.",
                    fullDialogue = "Đây là Tượng Nữ Thần Cổ Đại — nơi bạn dùng nông sản để đổi lấy sức mạnh vĩnh viễn:\n" +
                                   "- Đến trước Tượng Thần ở phía Đông, nhấn phím <color=#FFEA30><b>[E]</b></color> mở giao diện Dâng Lễ.\n" +
                                   "- Chọn Trụ Chiến Binh, bấm nút <color=#FFA726><b>[Quick Offer]</b></color> để hiến tế 4 củ Cà Rốt đã giữ lại trong ba lô.\n" +
                                   "- Nhận ngay <color=#FF6B6B><b>+1 Sức Đánh (ATK) Vĩnh Viễn!</b></color>\n" +
                                   "Tượng Thần sẽ mở thêm nhiều Phước Lành khi bạn dâng lễ nông sản cấp cao sau này (hệ thống tự động nhận diện cả vật phẩm trong Rương Nhà).",
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
                    objectiveTitle = "Cổng Hầm Ngục: Tiến Vào Ải Hướng Dẫn",
                    targetLabel = "Cổng Hầm Ngục (Dungeon Gate)",
                    actionKeyPrompt = "[E] VÀO HẦM NGỤC",
                    actionInstruction = "Đến Cổng Hầm Ngục ngay cạnh Tượng Thần, nhấn <color=#FFEA30><b>[E]</b></color> để tiến thẳng vào Hầm Ngục Hướng Dẫn (Tầng 0).",
                    fullDialogue = "Bạn đã nắm vững toàn bộ chu trình sinh tồn ở Căn cứ!\n" +
                                   "- Bụng no, bát Súp Tốc Độ sẵn sàng trong túi, +1 ATK vĩnh viễn đã kích hoạt và hạt giống đã cất an toàn!\n" +
                                   "- Bước lại Cổng Hầm Ngục ngay cạnh Tượng Thần, nhấn phím <color=#FFEA30><b>[E]</b></color> để tiến thẳng vào Hầm Ngục Hướng Dẫn (Tầng 0)!",
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
            // Fail-safe: If the player stored all carrots into the chest, ensure 4 carrots are in backpack for the statue!
            CropData cCrop = GetCarrotCropData();
            if (cCrop != null && cCrop.producedItem != null)
            {
                ItemData carrotItem = cCrop.producedItem;
                PlayerStats ps = PlayerStats.Instance != null ? PlayerStats.Instance : Object.FindFirstObjectByType<PlayerStats>();
                if (ps != null && ps.itemContainer != null)
                {
                    int inBackpack = GetItemCount(ps.itemContainer, carrotItem);
                    if (inBackpack < 4)
                    {
                        int needed = 4 - inBackpack;
                        if (HomeChest.Instance != null && HomeChest.Instance.chestContainer != null)
                        {
                            int inChest = GetItemCount(HomeChest.Instance.chestContainer, carrotItem);
                            int toTake = Mathf.Min(needed, inChest);
                            if (toTake > 0)
                            {
                                HomeChest.Instance.chestContainer.RemoveItem(carrotItem, toTake);
                                HomeChest.Instance.chestContainer.NotifyChange();
                                HomeChest.Instance.SaveChestData();
                                ps.itemContainer.AddItem(carrotItem, toTake);
                                needed -= toTake;
                            }
                        }
                        if (needed > 0)
                        {
                            ps.itemContainer.AddItem(carrotItem, needed);
                        }
                    }
                }
            }

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
        PlayerPrefs.DeleteKey("Tut_Reward_Intro_Seen");
        PlayerPrefs.DeleteKey("Tut_Event_Intro_Seen");
        PlayerPrefs.DeleteKey("Tut_Merchant_Intro_Seen");
        PlayerPrefs.DeleteKey("Tut_Camp_Intro_Seen");
        PlayerPrefs.Save();
        SetStep(TutorialStep.IntroCutscene);
    }
}
