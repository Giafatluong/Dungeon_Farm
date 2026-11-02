using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Slay the Spire-style horizontal Buff/Debuff Bar.
/// Manages and displays a row of illustrated buff widgets under/over a health bar.
/// Supports both Player and Enemy targets with object pooling for zero GC allocations.
/// </summary>
public class BuffBarUI : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float iconSize = 22f;
    [SerializeField] private float spacing = 4f;

    private HorizontalLayoutGroup layoutGroup;
    private RectTransform rectTransform;

    private readonly List<BuffIconWidget> activeWidgets = new();
    private readonly List<BuffIconWidget> widgetPool = new();

    public PlayerStats trackedPlayer;
    public EnemyStats trackedEnemy;

    private int lastBuffHash = -9999;

    private void Awake()
    {
        EnsureLayout();
        AdjustScaleToParent();
    }

    private void OnEnable()
    {
        AdjustScaleToParent();
    }

    /// <summary>
    /// Counteracts non-uniform parent scale (e.g., HealthBar's 0.02, 0.01 scale).
    /// Dynamically enforces an exact 1:1 aspect ratio in world space so all buff badges render as perfect circles.
    /// </summary>
    public void AdjustScaleToParent()
    {
        if (transform.parent != null)
        {
            Vector3 parentLossy = transform.parent.lossyScale;
            float px = Mathf.Abs(parentLossy.x);
            float py = Mathf.Abs(parentLossy.y);

            if (px > 0.00001f && py > 0.00001f)
            {
                float ratio = px / py;
                if (Mathf.Abs(ratio - 1f) > 0.001f)
                {
                    transform.localScale = new Vector3(1f / ratio, 1f, 1f);
                }
                else if (transform.localScale != Vector3.one)
                {
                    transform.localScale = Vector3.one;
                }
            }
        }
    }

    private void EnsureLayout()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) rectTransform = gameObject.AddComponent<RectTransform>();

        if (layoutGroup == null) layoutGroup = GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null) layoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();

        layoutGroup.spacing = spacing;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
    }

    public void SetPlayerTarget(PlayerStats player)
    {
        trackedPlayer = player;
        trackedEnemy = null;
        lastBuffHash = -9999;
        Refresh();
    }

    public void SetEnemyTarget(EnemyStats enemy)
    {
        trackedEnemy = enemy;
        trackedPlayer = null;
        lastBuffHash = -9999;
        Refresh();
    }

    private void LateUpdate()
    {
        AdjustScaleToParent();

        if (trackedPlayer != null)
        {
            int hash = CalculatePlayerHash(trackedPlayer);
            if (hash != lastBuffHash)
            {
                lastBuffHash = hash;
                Refresh();
            }
        }
        else if (trackedEnemy != null)
        {
            int hash = CalculateEnemyHash(trackedEnemy);
            if (hash != lastBuffHash)
            {
                lastBuffHash = hash;
                Refresh();
            }
        }
    }

    private int CalculatePlayerHash(PlayerStats p)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (int)p.GetCurrentFullnessTier();
            hash = hash * 31 + p.currentShield;
            if (p.activeBuffs != null)
            {
                hash = hash * 31 + p.activeBuffs.Count;
                for (int i = 0; i < p.activeBuffs.Count; i++)
                {
                    var b = p.activeBuffs[i];
                    if (b != null)
                    {
                        hash = hash * 31 + b.buffValue * 13 + b.remainingDuration * 7 + (int)b.buffDurationType;
                    }
                }
            }
            if (ProgressionManager.Instance != null)
            {
                hash = hash * 31 + (int)ProgressionManager.Instance.activeBlessing;
            }
            return hash;
        }
    }

    private int CalculateEnemyHash(EnemyStats e)
    {
        unchecked
        {
            int hash = 19;
            hash = hash * 31 + (e.isDefending ? 1 : 0);
            hash = hash * 31 + e.defendBonus;
            hash = hash * 31 + e.defenseReduction;
            if (e.activeBuffs != null)
            {
                hash = hash * 31 + e.activeBuffs.Count;
                for (int i = 0; i < e.activeBuffs.Count; i++)
                {
                    var b = e.activeBuffs[i];
                    if (b != null)
                    {
                        hash = hash * 31 + b.buffValue * 13 + b.remainingDuration * 7 + (int)b.buffDurationType;
                    }
                }
            }
            return hash;
        }
    }

    public void Refresh()
    {
        List<BuffDisplayInfo> displayList = new();

        if (trackedPlayer != null)
        {
            CollectPlayerBuffs(trackedPlayer, displayList);
        }
        else if (trackedEnemy != null)
        {
            CollectEnemyBuffs(trackedEnemy, displayList);
        }

        UpdateWidgets(displayList);
    }

    private void CollectPlayerBuffs(PlayerStats p, List<BuffDisplayInfo> list)
    {
        // 1. Guard / Active Shield (Defend)
        if (p.currentShield > 0)
        {
            int totalShield = p.currentShield;
            list.Add(new BuffDisplayInfo
            {
                id = "player_guard",
                title = $"Khiên Chống Đỡ (+{totalShield} Shield)",
                category = "[PHÒNG HỘ]",
                categoryColor = new Color(0.2f, 0.7f, 1f),
                icon = BuffSpriteFactory.GetSprite("def_buff"),
                stackBadge = $"+{totalShield}",
                durationText = "⏱ Hết Lượt Này",
                description = $"Đang giương khiên phòng thủ: Hấp thụ và chặn đứng <b>{totalShield} sát thương</b> từ đòn đánh tiếp theo của quái vật."
            });
        }

        // 2. Fullness Tier Status
        PlayerStats.FullnessTier tier = p.GetCurrentFullnessTier();
        switch (tier)
        {
            case PlayerStats.FullnessTier.Satiated:
                list.Add(new BuffDisplayInfo
                {
                    id = "fullness_satiated",
                    title = "No Nê (+2 ATK, +1 DEF, +2 SPD)",
                    category = "[CƯỜNG HÓA]",
                    categoryColor = new Color(0.3f, 0.95f, 0.45f),
                    icon = BuffSpriteFactory.GetSprite("fullness_satiated"),
                    stackBadge = "+2",
                    durationText = "⏱ Khi Còn No (30-79%)",
                    description = "Cơ thể tràn đầy năng lượng! Nhận buff toàn diện <b>+2 ATK</b>, <b>+1 DEF</b> và <b>+2 Speed</b> để luôn giành quyền đi trước."
                });
                break;

            case PlayerStats.FullnessTier.Hungry:
                list.Add(new BuffDisplayInfo
                {
                    id = "fullness_hungry",
                    title = "Đói Bụng (-2 ATK, -3 SPD)",
                    category = "[BẤT LỢI]",
                    categoryColor = new Color(1f, 0.4f, 0.25f),
                    icon = BuffSpriteFactory.GetSprite("fullness_hungry"),
                    stackBadge = "-2",
                    durationText = "⏱ Tới Khi Ăn (< 30%)",
                    description = "Cơ thể suy nhược vì đói! Bị giảm <b>-2 ATK</b>, <b>-3 Speed</b>. Mất <b>8 HP</b> mỗi phòng chuyển giao nếu chạm đáy 0 Độ No."
                });
                break;

            case PlayerStats.FullnessTier.Overstuffed:
                list.Add(new BuffDisplayInfo
                {
                    id = "fullness_overstuffed",
                    title = "Bội Thực (+3 DEF, Hồi 2 HP)",
                    category = "[TRẠNG THÁI]",
                    categoryColor = new Color(1f, 0.82f, 0.2f),
                    icon = BuffSpriteFactory.GetSprite("fullness_overstuffed"),
                    stackBadge = "+3",
                    durationText = "⏱ Tới Khi Tiêu Hóa (>= 80%)",
                    description = "Bụng quá no hóa thân thành Pháo Đài: Nhận <b>+3 DEF</b> và tự động hồi <b>+2 HP mỗi lượt</b> & mỗi phòng mới. Bị giảm <b>-4 Speed, -1 ATK</b>."
                });
                break;
        }

        // 3. Active Buffs & Debuffs from Food / Potions
        if (p.activeBuffs != null)
        {
            for (int i = 0; i < p.activeBuffs.Count; i++)
            {
                var ab = p.activeBuffs[i];
                if (ab == null || ab.buff == null) continue;

                bool isDebuff = ab.buff.IsDebuff || ab.buffValue < 0;
                string statName = CombatUI.GetBuffStatLabel(ab.buff);
                string sign = ab.buffValue > 0 ? "+" : "";

                string spriteKey = ab.buff.buffType switch
                {
                    StatEffectData.BuffType.Attack => isDebuff ? "atk_debuff" : "atk_buff",
                    StatEffectData.BuffType.Defense => isDebuff ? "def_debuff" : "def_buff",
                    StatEffectData.BuffType.Speed => isDebuff ? "speed_debuff" : "speed_buff",
                    StatEffectData.BuffType.Health => "health_buff",
                    _ => isDebuff ? "atk_debuff" : "atk_buff"
                };

                string durUnit = ab.buffDurationType switch
                {
                    FoodData.BuffDurationType.Turn => ab.remainingDuration == 1 ? "Lượt" : "Lượt",
                    FoodData.BuffDurationType.Combat => ab.remainingDuration == 1 ? "Trận" : "Trận",
                    FoodData.BuffDurationType.Floor => ab.remainingDuration == 1 ? "Tầng" : "Tầng",
                    _ => ab.buffDurationType.ToString()
                };

                list.Add(new BuffDisplayInfo
                {
                    id = $"player_buff_{i}",
                    title = $"{ab.buff.buffName} ({sign}{ab.buffValue} {statName})",
                    category = isDebuff ? "[BẤT LỢI]" : "[CƯỜNG HÓA]",
                    categoryColor = isDebuff ? new Color(1f, 0.35f, 0.35f) : new Color(0.3f, 0.85f, 1f),
                    icon = BuffSpriteFactory.GetSprite(spriteKey),
                    stackBadge = $"{ab.remainingDuration}",
                    durationText = $"⏱ Còn {ab.remainingDuration} {durUnit}",
                    description = $"Hiệu ứng từ thức ăn: Tăng cường <b>{sign}{ab.buffValue} {statName}</b> trong {ab.remainingDuration} {durUnit} tiếp theo."
                });
            }
        }

        // 4. Divine Blessing
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.activeBlessing != BlessingType.None)
        {
            BlessingType blessing = ProgressionManager.Instance.activeBlessing;
            BlessingInfo bInfo = BlessingDatabase.GetBlessingInfo(blessing);
            string bTitle = bInfo != null ? bInfo.title : blessing.ToString();
            string bDesc = bInfo != null ? bInfo.description : "Phước lành thần thánh bảo hộ.";

            list.Add(new BuffDisplayInfo
            {
                id = "player_blessing",
                title = $"Phước Lành: {bTitle}",
                category = "[PHƯỚC LÀNH]",
                categoryColor = new Color(1f, 0.9f, 0.3f),
                icon = BuffSpriteFactory.GetSprite("blessing"),
                stackBadge = "★",
                durationText = "⏱ Suốt Chuyến Đi",
                description = bDesc
            });
        }
    }

    private void CollectEnemyBuffs(EnemyStats e, List<BuffDisplayInfo> list)
    {
        // 1. Defending Shield
        if (e.isDefending)
        {
            list.Add(new BuffDisplayInfo
            {
                id = "enemy_defend",
                title = $"Phòng Ngự (+{e.defendBonus} DEF)",
                category = "[PHÒNG THỦ]",
                categoryColor = new Color(0.2f, 0.7f, 1f),
                icon = BuffSpriteFactory.GetSprite("def_buff"),
                stackBadge = $"+{e.defendBonus}",
                durationText = "⏱ Hết Lượt Này",
                description = $"Quái vật đang giương thế thủ: Tăng thêm <b>+{e.defendBonus} DEF</b> giảm thiểu sát thương nhận vào trong lượt này."
            });
        }

        // 2. Defense Reduction (Armor Break / Bitter Decay)
        if (e.defenseReduction > 0)
        {
            list.Add(new BuffDisplayInfo
            {
                id = "enemy_corrode",
                title = $"Ăn Mòn Giáp (-{e.defenseReduction} DEF)",
                category = "[BẤT LỢI]",
                categoryColor = new Color(0.85f, 0.3f, 1f),
                icon = BuffSpriteFactory.GetSprite("poison"),
                stackBadge = $"-{e.defenseReduction}",
                durationText = "⏱ Hết Trận",
                description = $"Giáp phòng ngự bị độc tố ăn mòn vĩnh viễn trong trận đấu: Giảm <b>-{e.defenseReduction} DEF</b>, chịu thêm sát thương từ mọi đòn tấn công."
            });
        }

        // 3. Active Buffs & Debuffs on Enemy
        if (e.activeBuffs != null)
        {
            for (int i = 0; i < e.activeBuffs.Count; i++)
            {
                var ab = e.activeBuffs[i];
                if (ab == null || ab.buff == null) continue;

                bool isDebuff = ab.buff.IsDebuff || ab.buffValue < 0;
                string statName = CombatUI.GetBuffStatLabel(ab.buff);
                string sign = ab.buffValue > 0 ? "+" : "";

                string spriteKey = ab.buff.buffType switch
                {
                    StatEffectData.BuffType.Attack => isDebuff ? "atk_debuff" : "atk_buff",
                    StatEffectData.BuffType.Defense => isDebuff ? "def_debuff" : "def_buff",
                    StatEffectData.BuffType.Speed => isDebuff ? "speed_debuff" : "speed_buff",
                    StatEffectData.BuffType.Health => "health_buff",
                    _ => isDebuff ? "atk_debuff" : "atk_buff"
                };

                list.Add(new BuffDisplayInfo
                {
                    id = $"enemy_buff_{i}",
                    title = $"{ab.buff.buffName} ({sign}{ab.buffValue} {statName})",
                    category = isDebuff ? "[BẤT LỢI]" : "[CƯỜNG HÓA]",
                    categoryColor = isDebuff ? new Color(1f, 0.35f, 0.35f) : new Color(0.3f, 0.85f, 1f),
                    icon = BuffSpriteFactory.GetSprite(spriteKey),
                    stackBadge = $"{ab.remainingDuration}",
                    durationText = $"⏱ Còn {ab.remainingDuration} Lượt",
                    description = $"Trạng thái quái vật: <b>{sign}{ab.buffValue} {statName}</b> còn hiệu lực trong {ab.remainingDuration} lượt."
                });
            }
        }
    }

    private void UpdateWidgets(List<BuffDisplayInfo> displayList)
    {
        EnsureLayout();

        // Recycle extra widgets into pool
        while (activeWidgets.Count > displayList.Count)
        {
            BuffIconWidget widget = activeWidgets[activeWidgets.Count - 1];
            activeWidgets.RemoveAt(activeWidgets.Count - 1);
            widget.gameObject.SetActive(false);
            widgetPool.Add(widget);
        }

        // Instantiate / activate widgets
        for (int i = 0; i < displayList.Count; i++)
        {
            BuffIconWidget widget;
            if (i < activeWidgets.Count)
            {
                widget = activeWidgets[i];
            }
            else if (widgetPool.Count > 0)
            {
                widget = widgetPool[widgetPool.Count - 1];
                widgetPool.RemoveAt(widgetPool.Count - 1);
                widget.gameObject.SetActive(true);
                activeWidgets.Add(widget);
            }
            else
            {
                widget = CreateWidgetInstance();
                activeWidgets.Add(widget);
            }

            widget.Setup(displayList[i]);
        }

        // Adjust bar size
        float totalWidth = Mathf.Max(0, displayList.Count * iconSize + Mathf.Max(0, displayList.Count - 1) * spacing);
        rectTransform.sizeDelta = new Vector2(totalWidth, iconSize);
    }

    private BuffIconWidget CreateWidgetInstance()
    {
        GameObject go = new GameObject("BuffIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BuffIconWidget));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(iconSize, iconSize);

        Image img = go.GetComponent<Image>();
        img.raycastTarget = true;
        img.preserveAspect = true;

        BuffIconWidget widget = go.GetComponent<BuffIconWidget>();
        return widget;
    }
}
