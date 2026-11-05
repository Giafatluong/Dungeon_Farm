using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interactive, Step-by-Step Combat Tutorial Overlay.
/// Guides the player during their first dungeon combat encounter:
/// Phase 1: 3 AP (Action Points) & Eating Food for Speed Buff (Points to [EAT] button).
/// Phase 2: Selecting Targets & Attacking (Points to [ATTACK] button & Enemy).
/// Phase 3: Enemy Intent Reading & Defending (Guarantees enemy Attack intent, points to [DEFEND] / [END TURN]).
/// Phase 4: Enemy Turn & Block verification (Observes shield absorbing incoming attack).
/// Phase 5: Counterattack & Victory (Points to [ATTACK] to finish off the enemy).
/// </summary>
public class CombatTutorialGuide : MonoBehaviour
{
    public static CombatTutorialGuide Instance { get; private set; }

    public enum CombatPhase
    {
        None,
        OverviewAndEat,
        AttackTarget,
        DefendAndEndTurn,
        EnemyTurnWatch,
        FinishTheFight,
        Finished
    }

    public enum CombatAction
    {
        None,
        Attack,
        Defend,
        Eat,
        EndTurn
    }

    public CombatPhase CurrentPhase => currentPhase;
    public bool HasPlayerDefended => hasPlayerDefendedInTutorial;
    public bool IsTutorialActive => currentPhase != CombatPhase.None && currentPhase != CombatPhase.Finished;

    public bool IsActionAllowed(CombatAction action)
    {
        if (!IsTutorialActive) return true;

        switch (currentPhase)
        {
            case CombatPhase.OverviewAndEat:
                if (action == CombatAction.Eat) return true;
                // Safety net: if player has already eaten this turn, or has no edible food, allow Attack to avoid soft-lock
                if (combatManager != null && (combatManager.hasEatenThisTurn || !HasAnyEdibleFood()))
                    return action == CombatAction.Attack;
                return false;

            case CombatPhase.AttackTarget:
                return action == CombatAction.Attack;

            case CombatPhase.DefendAndEndTurn:
                if (!hasPlayerDefendedInTutorial)
                {
                    if (action == CombatAction.Defend) return true;
                    // Safety net: if player has 0 AP or already defended, allow EndTurn so they can progress
                    if (combatManager != null && combatManager.hasDefendedThisTurn) return action == CombatAction.EndTurn;
                    if (combatUI != null && combatUI.playerStats != null && combatUI.playerStats.currentAP <= 0 && action == CombatAction.EndTurn) return true;
                    return false;
                }
                else
                {
                    return action == CombatAction.EndTurn;
                }

            case CombatPhase.EnemyTurnWatch:
                return false;

            case CombatPhase.FinishTheFight:
                // Allow all actions freely in final phase to finish off the enemy
                return true;

            default:
                return true;
        }
    }

    private CombatUI combatUI;
    private CombatManager combatManager;
    private CombatPhase currentPhase = CombatPhase.None;

    public const string CombatTutorialDoneKey = "HarvestHollow_CombatTutorial_Completed";

    [Header("Runtime State")]
    private bool hasPlayerDefendedInTutorial = false;
    private EnemyStats currentTutorialEnemy;

    [Header("UI Elements")]
    private GameObject overlayRootGO;
    private RectTransform bannerCardRT;
    private TextMeshProUGUI bannerTitleText;
    private TextMeshProUGUI bannerBodyText;
    private Button skipButton;

    private GameObject pointerGO;
    private RectTransform pointerRT;
    private TextMeshProUGUI pointerBadgeText;
    private RectTransform currentPointedTarget;

    private GameObject enemyIntentPillGO;
    private RectTransform enemyIntentPillRT;
    private TextMeshProUGUI enemyIntentPillText;

    public static void EnsureInitialized(CombatUI ui)
    {
        if (ui == null) return;

        // Only run if not yet completed
        if (PlayerPrefs.GetInt(CombatTutorialDoneKey, 0) == 1) return;

        // Ensure combat tutorial only runs on Floor 0 (the dedicated tutorial floor)
        int currentFloor = 1;
        if (WaveManager.Instance != null)
        {
            currentFloor = WaveManager.Instance.CurrentFloorNumber;
        }
        else if (ProgressionManager.Instance != null)
        {
            currentFloor = ProgressionManager.Instance.currentFloor;
        }
        if (currentFloor != 0) return;

        if (Instance == null)
        {
            GameObject guideGO = new GameObject("[CombatTutorialGuide]");
            guideGO.transform.SetParent(ui.transform, false);
            Instance = guideGO.AddComponent<CombatTutorialGuide>();
            Instance.Initialize(ui);
        }
        else
        {
            Instance.Initialize(ui);
        }
    }

    public static void Cleanup()
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
            Instance = null;
        }
    }

    public void Initialize(CombatUI ui)
    {
        combatUI = ui;
        combatManager = ui.combatManager != null ? ui.combatManager : Object.FindFirstObjectByType<CombatManager>();

        if (combatManager != null)
        {
            combatManager.OnCombatStarted += HandleCombatStarted;
            combatManager.OnCombatEnded += HandleCombatEnded;
            combatManager.OnTurnChanged += HandleTurnChanged;
            combatManager.OnPlayerEatAction += HandlePlayerEat;
            combatManager.OnPlayerAttackAction += HandlePlayerAttack;
            combatManager.OnPlayerDefendAction += HandlePlayerDefend;
            combatManager.OnPlayerEndTurnAction += HandlePlayerEndTurn;
        }

        ConstructUI();

        // Start dormant; tutorial only begins when combat actually starts via HandleCombatStarted
        currentPhase = CombatPhase.None;
        if (overlayRootGO != null) overlayRootGO.SetActive(false);
        if (pointerGO != null) pointerGO.SetActive(false);
        if (enemyIntentPillGO != null) enemyIntentPillGO.SetActive(false);
    }

    private void OnDestroy()
    {
        if (combatManager != null)
        {
            combatManager.OnCombatStarted -= HandleCombatStarted;
            combatManager.OnCombatEnded -= HandleCombatEnded;
            combatManager.OnTurnChanged -= HandleTurnChanged;
            combatManager.OnPlayerEatAction -= HandlePlayerEat;
            combatManager.OnPlayerAttackAction -= HandlePlayerAttack;
            combatManager.OnPlayerDefendAction -= HandlePlayerDefend;
            combatManager.OnPlayerEndTurnAction -= HandlePlayerEndTurn;
        }
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (currentPhase == CombatPhase.None || currentPhase == CombatPhase.Finished) return;

        // ESC to dismiss / skip combat tutorial (do not skip if food panel is active, let food panel close instead)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (combatUI != null && combatUI.foodPanel != null && combatUI.foodPanel.activeSelf)
            {
                return;
            }
            SkipTutorial();
            return;
        }

        // Phase 1 dynamic pointer: track Eat button vs first Food item inside food panel
        if (currentPhase == CombatPhase.OverviewAndEat)
        {
            if (combatUI != null && combatUI.foodPanel != null && combatUI.foodPanel.activeSelf)
            {
                if (combatUI.foodListContainer != null && combatUI.foodListContainer.childCount > 0)
                {
                    Transform firstItem = combatUI.foodListContainer.GetChild(0);
                    if (firstItem != null && firstItem.gameObject.activeInHierarchy)
                    {
                        RectTransform itemRT = firstItem.GetComponent<RectTransform>();
                        if (currentPointedTarget != itemRT)
                        {
                            PointToButton(itemRT, "[CLICK ĂN SÚP]");
                        }
                    }
                }
            }
            else
            {
                RectTransform eatRT = combatUI != null && combatUI.eatButton != null ? combatUI.eatButton.GetComponent<RectTransform>() : null;
                if (currentPointedTarget != eatRT)
                {
                    PointToButton(eatRT, "[EAT] ĂN SÚP (Phím 3)");
                }
            }
        }

        // Animate floating spotlight pointer
        if (pointerGO != null && pointerGO.activeSelf && currentPointedTarget != null && currentPointedTarget.gameObject.activeInHierarchy)
        {
            float bob = Mathf.Sin(Time.time * 6f) * 6f;
            Vector3 targetScreenPos = currentPointedTarget.position + new Vector3(0, 52f + bob, 0);
            pointerRT.position = targetScreenPos;
        }
        else if (pointerGO != null && pointerGO.activeSelf && (currentPointedTarget == null || !currentPointedTarget.gameObject.activeInHierarchy))
        {
            pointerGO.SetActive(false);
        }

        // Update floating Intent badge over active enemy
        if (enemyIntentPillGO != null && enemyIntentPillGO.activeSelf)
        {
            EnemyStats activeEnemy = GetActiveEnemy();
            if (activeEnemy != null && activeEnemy.currentHealth > 0)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 screenPos = cam.WorldToScreenPoint(activeEnemy.transform.position + Vector3.up * 1.35f);
                    enemyIntentPillRT.position = screenPos;
                }
            }
            else
            {
                enemyIntentPillGO.SetActive(false);
            }
        }
    }

    private EnemyStats GetActiveEnemy()
    {
        if (currentTutorialEnemy != null && currentTutorialEnemy.currentHealth > 0)
            return currentTutorialEnemy;

        if (combatUI != null && combatUI.targetSelector != null && combatUI.targetSelector.selectedEnemy != null)
            return combatUI.targetSelector.selectedEnemy;

        if (combatManager != null && combatManager.enemies != null)
        {
            foreach (var e in combatManager.enemies)
            {
                if (e != null && e.currentHealth > 0) return e;
            }
        }
        return null;
    }

    private void EnsureEnemyAttackIntent(bool lockIntent = true)
    {
        if (combatManager == null || combatManager.enemies == null) return;

        foreach (var enemy in combatManager.enemies)
        {
            if (enemy != null && enemy.currentHealth > 0)
            {
                currentTutorialEnemy = enemy;
                enemy.ForceIntent(EnemyData.EnemyIntent.Attack, lockIntent);

                if (combatUI != null && combatUI.targetSelector != null && combatUI.targetSelector.selectedEnemy == enemy)
                {
                    combatUI.UpdateTargetInfo(enemy);
                }
            }
        }
    }

    private void HandleCombatStarted()
    {
        if (PlayerPrefs.GetInt(CombatTutorialDoneKey, 0) == 1) return;

        // Ensure combat tutorial only runs on Floor 0 (the dedicated tutorial floor)
        int currentFloor = 1;
        if (WaveManager.Instance != null)
        {
            currentFloor = WaveManager.Instance.CurrentFloorNumber;
        }
        else if (ProgressionManager.Instance != null)
        {
            currentFloor = ProgressionManager.Instance.currentFloor;
        }
        if (currentFloor != 0) return;

        // Ensure DungeonTutorialController hides any entrance or stage banners
        DungeonTutorialController.Instance?.HideBanner();
        DungeonTutorialController.Instance?.HidePointer();

        EnsureEnemyAttackIntent(lockIntent: true);
        if (currentPhase == CombatPhase.None)
        {
            SetPhase(CombatPhase.OverviewAndEat);
        }
    }

    private bool HasAnyEdibleFood()
    {
        PlayerStats ps = combatUI != null && combatUI.playerStats != null
            ? combatUI.playerStats
            : Object.FindFirstObjectByType<PlayerStats>();
        if (ps == null || ps.itemContainer == null) return false;

        for (int i = 0; i < ps.itemContainer.itemSlots.Length; i++)
        {
            var slot = ps.itemContainer.itemSlots[i];
            if (slot != null && slot.itemData != null && slot.amount > 0)
            {
                if (slot.itemData is FoodData || slot.itemData.itemType == ItemData.ItemType.Food || slot.itemData.itemType == ItemData.ItemType.Produce)
                    return true;
            }
        }
        return false;
    }

    private void EnsurePlayerHasTutorialFood()
    {
        PlayerStats ps = combatUI != null && combatUI.playerStats != null
            ? combatUI.playerStats
            : Object.FindFirstObjectByType<PlayerStats>();

        if (ps == null || ps.itemContainer == null) return;

        // Check if player has any food or produce
        for (int i = 0; i < ps.itemContainer.itemSlots.Length; i++)
        {
            var slot = ps.itemContainer.itemSlots[i];
            if (slot != null && slot.itemData != null && slot.amount > 0)
            {
                if (slot.itemData is FoodData || slot.itemData.itemType == ItemData.ItemType.Food || slot.itemData.itemType == ItemData.ItemType.Produce)
                {
                    return; // Player already has food!
                }
            }
        }

        // If no food in bag, grant Carrot Soup
        FoodData soup = null;
#if UNITY_EDITOR
        soup = UnityEditor.AssetDatabase.LoadAssetAtPath<FoodData>("Assets/_Assets/ScriptableObjects/Food/Data/Combo/Carrot soup.asset");
#endif
        if (soup == null)
        {
            FoodData[] allFoods = GameAssetHelper.LoadAll<FoodData>();
            for (int i = 0; i < allFoods.Length; i++)
            {
                if (allFoods[i] != null && allFoods[i].name.ToLower().Contains("carrot"))
                {
                    soup = allFoods[i];
                    break;
                }
            }
            if (soup == null && allFoods.Length > 0) soup = allFoods[0];
        }

        if (soup != null)
        {
            bool added = ps.itemContainer.AddItem(soup, 1);
            if (!added && ps.itemContainer.itemSlots.Length > 0)
            {
                // Bag was completely full! Replace slot 0 with soup so tutorial is never blocked
                ps.itemContainer.itemSlots[0].itemData = soup;
                ps.itemContainer.itemSlots[0].amount = 1;
            }
        }
    }

    public void SetPhase(CombatPhase phase)
    {
        currentPhase = phase;

        if (overlayRootGO != null) overlayRootGO.SetActive(true);

        switch (currentPhase)
        {
            case CombatPhase.OverviewAndEat:
                EnsurePlayerHasTutorialFood();

                // Reduce hunger to 70 so player has room to consume +25 soup without hitting full barrier
                PlayerStats tutPs = combatUI != null && combatUI.playerStats != null
                    ? combatUI.playerStats
                    : Object.FindFirstObjectByType<PlayerStats>();
                if (tutPs != null)
                {
                    if (tutPs.currentHunger >= tutPs.maxHunger)
                    {
                        tutPs.currentHunger = 70;
                        tutPs.CheckFullnessTierTransition(false);
                    }
                    if (combatUI != null)
                    {
                        combatUI.InvalidateHUDCache();
                        combatUI.UpdatePlayerHUD();
                    }
                }

                if (bannerTitleText != null)
                {
                    bannerTitleText.text = "<color=#FFD700>HƯỚNG DẪN CHIẾN ĐẤU [1/4]: CƠ CHẾ TỐC ĐỘ (SPD) & ĐỘ NO</color>";
                }
                if (bannerBodyText != null)
                {
                    bannerBodyText.text = "1. <b>Tốc Độ (SPD):</b> Ai có SPD cao hơn sẽ ĐI TRƯỚC! Quái vật có <color=#FF6B6B><b>10 SPD</b></color>, bạn có <color=#5599FF><b>9 SPD</b></color> (sẽ bị đánh trước!).\n" +
                                          "2. <b>Độ No:</b> Giữ ở mốc 'No Nê' (30%-79%) để nhận Buff toàn diện (+2 ATK, +1 DEF, +2 SPD).\n" +
                                          "Hãy bấm <color=#FFA500><b>[EAT] (Phím 3)</b></color> ăn Súp Cà Rốt để tăng lên <color=#50FA7B><b>11 SPD</b></color> (> 10 SPD) giành quyền ĐI TRƯỚC!";
                }
                PointToButton(combatUI != null && combatUI.eatButton != null ? combatUI.eatButton.GetComponent<RectTransform>() : null, "[EAT] ĂN SÚP (+2 SPD)");
                if (enemyIntentPillGO != null) enemyIntentPillGO.SetActive(false);
                break;

            case CombatPhase.AttackTarget:
                if (bannerTitleText != null)
                {
                    bannerTitleText.text = "<color=#50FA7B>TỐC ĐỘ ĐÃ ĐẠT 11 SPD > 10 SPD: BẠN ĐÃ ĐI TRƯỚC!</color>";
                }
                if (bannerBodyText != null)
                {
                    bannerBodyText.text = "Xuất sắc! Sau khi ăn Súp, Tốc Độ đạt <color=#50FA7B><b>11 SPD</b></color> (cao hơn 10 SPD của địch), giúp bạn <color=#FFD700><b>GIÀNH QUYỀN ĐI TRƯỚC!</b></color>\n" +
                                          "<color=#50FA7B><b>Thanh Buff Dưới Máu:</b></color> Các biểu tượng tròn hiển thị trạng thái No Nê & Súp Tốc Độ. Hãy rê chuột vào icon để xem chi tiết!\n" +
                                          "Giờ đến lượt bạn, hãy bấm <color=#FF5555><b>[ATTACK] (Phím 1)</b></color> để tung đòn tấn công!";
                }
                PointToButton(combatUI != null && combatUI.attackButton != null ? combatUI.attackButton.GetComponent<RectTransform>() : null, "[ATTACK] TẤN CÔNG (1 AP)");
                if (enemyIntentPillGO != null) enemyIntentPillGO.SetActive(false);
                break;

            case CombatPhase.DefendAndEndTurn:
                EnsureEnemyAttackIntent(lockIntent: true);
                if (bannerTitleText != null)
                {
                    bannerTitleText.text = "<color=#FFD700>HƯỚNG DẪN CHIẾN ĐẤU [3/4]: Ý ĐỊNH & GIÁP ẢO</color>";
                }
                if (bannerBodyText != null)
                {
                    bannerBodyText.text = "Chú ý nhãn đỏ trên đầu quái vật: <b>Ý Định (Intent)</b> của nó là <color=#FF5555><b>Tấn Công (6 DMG)</b></color>! Quái sắp đánh bạn!\n" +
                                          "Hãy bấm <color=#5599FF><b>[DEFEND] (Phím 2)</b></color> để nhận <color=#5599FF><b>+5 Giáp Ảo (Shield)</b></color> hấp thụ toàn bộ đòn đánh trước khi trừ vào máu!";
                }
                PointToButton(combatUI != null && combatUI.defendButton != null ? combatUI.defendButton.GetComponent<RectTransform>() : null, "[DEFEND] THỦ (+5 GIÁP ẢO)");
                if (enemyIntentPillGO != null)
                {
                    enemyIntentPillGO.SetActive(true);
                    if (enemyIntentPillText != null)
                    {
                        enemyIntentPillText.text = "Ý ĐỊNH: TẤN CÔNG (6 DMG)";
                    }
                }
                break;

            case CombatPhase.EnemyTurnWatch:
                if (bannerTitleText != null)
                {
                    bannerTitleText.text = "<color=#FFA500>QUÁI VẬT ĐANG HÀNH ĐỘNG...</color>";
                }
                if (bannerBodyText != null)
                {
                    bannerBodyText.text = "Quan sát quái vật lao đến tấn công: Giáp Ảo (+5 Shield) kết hợp DEF sẽ hấp thụ hoàn toàn đòn đánh (BLOCKED!) mà không mất giọt máu nào!";
                }
                PointToButton(null, "");
                if (enemyIntentPillGO != null) enemyIntentPillGO.SetActive(true);
                break;

            case CombatPhase.FinishTheFight:
                if (enemyIntentPillGO != null) enemyIntentPillGO.SetActive(false);
                if (bannerTitleText != null)
                {
                    bannerTitleText.text = "<color=#00FF7F>HẤP THỤ THÀNH CÔNG! [4/4]: KẾT LIỄU QUÁI VẬT</color>";
                }
                if (bannerBodyText != null)
                {
                    if (hasPlayerDefendedInTutorial)
                    {
                        bannerBodyText.text = "Xuất sắc! Giáp Ảo đã hấp thụ trọn vẹn sát thương (BLOCKED)! Giờ đến lượt bạn hồi đầy 3 AP, hãy bấm <color=#FF5555><b>[ATTACK] (Phím 1)</b></color> để kết liễu quái vật!";
                    }
                    else
                    {
                        bannerBodyText.text = "Quái vật vừa tấn công bạn! Lần sau nhớ dùng [DEFEND] để nhận Giáp Ảo chặn sát thương nhé. Giờ hãy bấm <color=#FF5555><b>[ATTACK] (Phím 1)</b></color> để kết liễu nó!";
                    }
                }
                PointToButton(combatUI != null && combatUI.attackButton != null ? combatUI.attackButton.GetComponent<RectTransform>() : null, "[ATTACK] KẾT LIỄU!");
                break;

            case CombatPhase.Finished:
                DismissGuide();
                break;
        }

        if (combatUI != null)
        {
            combatUI.InvalidateHUDCache();
            combatUI.UpdateActionButtons();
        }
    }

    private void PointToButton(RectTransform targetRT, string label)
    {
        currentPointedTarget = targetRT;
        if (pointerGO == null) return;

        if (targetRT != null)
        {
            pointerGO.SetActive(true);
            if (pointerBadgeText != null) pointerBadgeText.text = label;
        }
        else
        {
            pointerGO.SetActive(false);
        }
    }

    private void HandlePlayerEat(ItemData food)
    {
        if (currentPhase == CombatPhase.OverviewAndEat)
        {
            if (combatUI != null && combatUI.foodPanel != null && combatUI.foodPanel.activeSelf)
            {
                combatUI.foodPanel.SetActive(false);
            }
            SetPhase(CombatPhase.AttackTarget);
        }
    }

    private void HandlePlayerAttack(EnemyStats target, int damage)
    {
        if (currentPhase == CombatPhase.OverviewAndEat || currentPhase == CombatPhase.AttackTarget)
        {
            SetPhase(CombatPhase.DefendAndEndTurn);
        }
        else if (currentPhase == CombatPhase.FinishTheFight)
        {
            if (target != null && target.currentHealth > 0)
            {
                if (bannerBodyText != null)
                {
                    bannerBodyText.text = "Đòn đánh chính xác! Tiếp tục bấm <color=#FF5555><b>[ATTACK]</b></color> để dứt điểm quái vật!";
                }
            }
        }
    }

    private void HandlePlayerDefend()
    {
        if (currentPhase == CombatPhase.DefendAndEndTurn)
        {
            hasPlayerDefendedInTutorial = true;
            if (bannerTitleText != null)
            {
                bannerTitleText.text = "<color=#5599FF>GIÁP ẢO ĐÃ KÍCH HOẠT! (+5 SHIELD)</color>";
            }
            if (bannerBodyText != null)
            {
                bannerBodyText.text = "Giáp Ảo (+5 Shield) đã sẵn sàng! Quan sát biểu tượng Khiên xanh xuất hiện dưới thanh máu của bạn.\n" +
                                      "Bây giờ hãy bấm <color=#FFA500><b>[KẾT THÚC LƯỢT] (Phím Space)</b></color> để nhường lượt cho quái vật đánh vào Giáp Ảo của bạn!";
            }
            PointToButton(combatUI != null && combatUI.endTurnButton != null ? combatUI.endTurnButton.GetComponent<RectTransform>() : null, "[KẾT THÚC LƯỢT] (Space)");

            if (combatUI != null)
            {
                combatUI.InvalidateHUDCache();
                combatUI.UpdateActionButtons();
            }
        }
    }

    private void HandlePlayerEndTurn()
    {
        if (currentPhase == CombatPhase.DefendAndEndTurn)
        {
            SetPhase(CombatPhase.EnemyTurnWatch);
        }
    }

    private void HandleTurnChanged(CombatManager.Turn turn)
    {
        if (turn == CombatManager.Turn.Player)
        {
            if (currentPhase == CombatPhase.EnemyTurnWatch || currentPhase == CombatPhase.DefendAndEndTurn)
            {
                SetPhase(CombatPhase.FinishTheFight);
            }
        }
    }

    private void HandleCombatEnded()
    {
        PlayerPrefs.SetInt(CombatTutorialDoneKey, 1);
        PlayerPrefs.Save();
        TutorialManager.Instance?.NotifyCombatFinished();
        DismissGuide();
    }

    public void SkipTutorial()
    {
        PlayerPrefs.SetInt(CombatTutorialDoneKey, 1);
        PlayerPrefs.Save();
        TutorialManager.Instance?.NotifyCombatFinished();
        DismissGuide();
    }

    private void DismissGuide()
    {
        currentPhase = CombatPhase.Finished;
        if (overlayRootGO != null) overlayRootGO.SetActive(false);
        if (pointerGO != null) pointerGO.SetActive(false);
        if (enemyIntentPillGO != null) enemyIntentPillGO.SetActive(false);
        if (combatUI != null)
        {
            combatUI.InvalidateHUDCache();
            combatUI.UpdateActionButtons();
        }
    }

    #region UI Construction
    private void ConstructUI()
    {
        if (overlayRootGO != null) return;

        overlayRootGO = new GameObject("[CombatTutorialOverlay]", typeof(RectTransform));
        overlayRootGO.transform.SetParent(transform, false);
        RectTransform rootRT = overlayRootGO.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        TMP_FontAsset font = DungeonUIAssetHelper.GetFontAsset();

        // Banner Card (Top-Center below turn banner)
        GameObject cardGO = new GameObject("CombatGuidanceCard", typeof(RectTransform), typeof(Image));
        cardGO.transform.SetParent(overlayRootGO.transform, false);
        bannerCardRT = cardGO.GetComponent<RectTransform>();
        bannerCardRT.anchorMin = new Vector2(0.5f, 1f);
        bannerCardRT.anchorMax = new Vector2(0.5f, 1f);
        bannerCardRT.pivot = new Vector2(0.5f, 1f);
        bannerCardRT.anchoredPosition = new Vector2(0, -90);
        bannerCardRT.sizeDelta = new Vector2(820, 168);

        Image cardFrame = cardGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(cardFrame, new Color(0.85f, 0.55f, 0.18f, 0.98f)); // Golden amber

        // Inner dark plate
        GameObject plate = new GameObject("DarkPlate", typeof(RectTransform), typeof(Image));
        plate.transform.SetParent(cardGO.transform, false);
        RectTransform plRT = plate.GetComponent<RectTransform>();
        plRT.anchorMin = Vector2.zero;
        plRT.anchorMax = Vector2.one;
        plRT.offsetMin = new Vector2(4, 4);
        plRT.offsetMax = new Vector2(-4, -4);
        plate.GetComponent<Image>().color = new Color(0.09f, 0.10f, 0.12f, 0.96f);

        // Title row
        GameObject titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(plate.transform, false);
        RectTransform tRT = titleGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0, 1);
        tRT.anchorMax = new Vector2(1, 1);
        tRT.pivot = new Vector2(0, 1);
        tRT.anchoredPosition = new Vector2(14, -8);
        tRT.sizeDelta = new Vector2(-120, 24);
        bannerTitleText = titleGO.GetComponent<TextMeshProUGUI>();
        bannerTitleText.fontSize = 15.5f;
        bannerTitleText.enableAutoSizing = true;
        bannerTitleText.fontSizeMin = 13f;
        bannerTitleText.fontSizeMax = 15.5f;
        bannerTitleText.fontStyle = FontStyles.Bold;
        bannerTitleText.color = new Color(1f, 0.92f, 0.30f); // Bright sunlight gold
        DungeonUIAssetHelper.ApplyFont(bannerTitleText, withOutline: true);

        // Skip button (Top Right)
        GameObject skipGO = new GameObject("SkipButton", typeof(RectTransform), typeof(Image), typeof(Button));
        skipGO.transform.SetParent(plate.transform, false);
        RectTransform sRT = skipGO.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(1, 1);
        sRT.anchorMax = new Vector2(1, 1);
        sRT.pivot = new Vector2(1, 1);
        sRT.anchoredPosition = new Vector2(-8, -6);
        sRT.sizeDelta = new Vector2(90, 26);
        Image skipImg = skipGO.GetComponent<Image>();
        skipImg.color = new Color(0.15f, 0.17f, 0.22f, 0.95f);
        skipButton = skipGO.GetComponent<Button>();
        skipButton.onClick.AddListener(SkipTutorial);

        GameObject skipTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        skipTxtGO.transform.SetParent(skipGO.transform, false);
        RectTransform stRT = skipTxtGO.GetComponent<RectTransform>();
        stRT.anchorMin = Vector2.zero;
        stRT.anchorMax = Vector2.one;
        stRT.offsetMin = Vector2.zero;
        stRT.offsetMax = Vector2.zero;
        TextMeshProUGUI skipTmp = skipTxtGO.GetComponent<TextMeshProUGUI>();
        skipTmp.text = "[ Bỏ qua ]";
        skipTmp.fontSize = 12;
        skipTmp.fontStyle = FontStyles.Bold;
        skipTmp.alignment = TextAlignmentOptions.Center;
        skipTmp.color = new Color(0.95f, 0.95f, 0.95f);
        DungeonUIAssetHelper.ApplyFont(skipTmp, withOutline: true);

        // Body instruction text (Auto-sizing enabled so multi-point explanations never bleed over borders)
        GameObject bodyGO = new GameObject("BodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyGO.transform.SetParent(plate.transform, false);
        RectTransform bRT = bodyGO.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0, 0);
        bRT.anchorMax = new Vector2(1, 1);
        bRT.offsetMin = new Vector2(16, 8);
        bRT.offsetMax = new Vector2(-16, -34);
        bannerBodyText = bodyGO.GetComponent<TextMeshProUGUI>();
        bannerBodyText.fontSize = 13.5f;
        bannerBodyText.enableAutoSizing = true;
        bannerBodyText.fontSizeMin = 11.5f;
        bannerBodyText.fontSizeMax = 13.5f;
        bannerBodyText.color = Color.white;
        bannerBodyText.lineSpacing = 0;
        DungeonUIAssetHelper.ApplyFont(bannerBodyText, withOutline: true);

        // Floating Spotlight Pointer
        pointerGO = new GameObject("CombatSpotlightPointer", typeof(RectTransform));
        pointerGO.transform.SetParent(overlayRootGO.transform, false);
        pointerRT = pointerGO.GetComponent<RectTransform>();
        pointerRT.sizeDelta = new Vector2(240, 50);

        // Arrow icon pointing down
        GameObject arrowGO = new GameObject("ArrowIcon", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGO.transform.SetParent(pointerGO.transform, false);
        RectTransform aRT = arrowGO.GetComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0.5f, 0f);
        aRT.anchorMax = new Vector2(0.5f, 0f);
        aRT.pivot = new Vector2(0.5f, 0f);
        aRT.anchoredPosition = new Vector2(0, -6);
        aRT.sizeDelta = new Vector2(30, 24);
        TextMeshProUGUI arrowTMP = arrowGO.GetComponent<TextMeshProUGUI>();
        arrowTMP.text = "v";
        arrowTMP.fontSize = 22;
        arrowTMP.fontStyle = FontStyles.Bold;
        arrowTMP.alignment = TextAlignmentOptions.Center;
        arrowTMP.color = new Color(1f, 0.90f, 0.20f);
        DungeonUIAssetHelper.ApplyFont(arrowTMP, withOutline: true);

        // Pointer badge plaque
        GameObject badgeGO = new GameObject("BadgePlaque", typeof(RectTransform), typeof(Image));
        badgeGO.transform.SetParent(pointerGO.transform, false);
        RectTransform bgRT = badgeGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.5f, 0f);
        bgRT.anchorMax = new Vector2(0.5f, 0f);
        bgRT.pivot = new Vector2(0.5f, 0f);
        bgRT.anchoredPosition = new Vector2(0, 16);
        bgRT.sizeDelta = new Vector2(250, 34);
        Image bImg = badgeGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(bImg, new Color(1f, 0.82f, 0.20f, 1f));

        GameObject bPlate = new GameObject("DarkPlate", typeof(RectTransform), typeof(Image));
        bPlate.transform.SetParent(badgeGO.transform, false);
        RectTransform bpRT = bPlate.GetComponent<RectTransform>();
        bpRT.anchorMin = Vector2.zero;
        bpRT.anchorMax = Vector2.one;
        bpRT.offsetMin = new Vector2(2, 2);
        bpRT.offsetMax = new Vector2(-2, -2);
        bPlate.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.09f, 0.98f);

        GameObject pTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        pTxtGO.transform.SetParent(bPlate.transform, false);
        RectTransform ptRT = pTxtGO.GetComponent<RectTransform>();
        ptRT.anchorMin = Vector2.zero;
        ptRT.anchorMax = Vector2.one;
        ptRT.offsetMin = new Vector2(4, 0);
        ptRT.offsetMax = new Vector2(-4, 0);
        pointerBadgeText = pTxtGO.GetComponent<TextMeshProUGUI>();
        pointerBadgeText.fontSize = 13.5f;
        pointerBadgeText.fontStyle = FontStyles.Bold;
        pointerBadgeText.alignment = TextAlignmentOptions.Center;
        pointerBadgeText.color = new Color(1f, 0.95f, 0.30f);
        DungeonUIAssetHelper.ApplyFont(pointerBadgeText, withOutline: true);

        // Floating Enemy Intent Callout Pill (Overhead tag for enemy)
        enemyIntentPillGO = new GameObject("EnemyIntentPill", typeof(RectTransform), typeof(Image));
        enemyIntentPillGO.transform.SetParent(overlayRootGO.transform, false);
        enemyIntentPillRT = enemyIntentPillGO.GetComponent<RectTransform>();
        enemyIntentPillRT.sizeDelta = new Vector2(200, 30);
        Image eipFrame = enemyIntentPillGO.GetComponent<Image>();
        DungeonUIAssetHelper.StyleSlicedFrame(eipFrame, new Color(0.95f, 0.28f, 0.28f, 0.95f)); // Danger crimson frame

        GameObject eipDark = new GameObject("DarkPlate", typeof(RectTransform), typeof(Image));
        eipDark.transform.SetParent(enemyIntentPillGO.transform, false);
        RectTransform edRT = eipDark.GetComponent<RectTransform>();
        edRT.anchorMin = Vector2.zero;
        edRT.anchorMax = Vector2.one;
        edRT.offsetMin = new Vector2(2, 2);
        edRT.offsetMax = new Vector2(-2, -2);
        eipDark.GetComponent<Image>().color = new Color(0.18f, 0.04f, 0.04f, 0.95f);

        GameObject eipTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        eipTxtGO.transform.SetParent(eipDark.transform, false);
        RectTransform etRT = eipTxtGO.GetComponent<RectTransform>();
        etRT.anchorMin = Vector2.zero;
        etRT.anchorMax = Vector2.one;
        etRT.offsetMin = new Vector2(4, 0);
        etRT.offsetMax = new Vector2(-4, 0);
        enemyIntentPillText = eipTxtGO.GetComponent<TextMeshProUGUI>();
        enemyIntentPillText.fontSize = 12.5f;
        enemyIntentPillText.fontStyle = FontStyles.Bold;
        enemyIntentPillText.alignment = TextAlignmentOptions.Center;
        enemyIntentPillText.color = new Color(1f, 0.85f, 0.85f);
        enemyIntentPillText.text = "Ý ĐỊNH: TẤN CÔNG (6 DMG)";
        DungeonUIAssetHelper.ApplyFont(enemyIntentPillText, withOutline: true);

        enemyIntentPillGO.SetActive(false);
        pointerGO.SetActive(false);
        overlayRootGO.SetActive(false);
    }
    #endregion
}
