using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Individual Recipe Card in the Recipe Notebook (Sổ Tay Công Thức) of KitchenUI.
/// </summary>
public class RecipeCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual Components")]
    public Image cardBackground;
    public Image iconFrame;
    public Image dishIcon;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI starsText;
    public TextMeshProUGUI detailText;

    [Header("Action Button")]
    public Button actionButton;
    public Image actionButtonBackground;
    public TextMeshProUGUI actionButtonText;
    public GameObject lockIcon;

    [Header("Cozy Theme Sprites")]
    public Sprite normalCardSprite;
    public Sprite selectedCardSprite;
    public Sprite lockedCardSprite;
    public Sprite autoFillButtonSprite;
    public Sprite missingButtonSprite;
    public Sprite neutralButtonSprite;

    private static readonly Color InkDark = new Color(0.23f, 0.14f, 0.08f, 1f);
    private static readonly Color InkBody = new Color(0.32f, 0.22f, 0.14f, 1f);

    private RecipeData currentRecipe;

    private void ApplyCardSprite(Sprite sprite, Color fallbackColor)
    {
        if (cardBackground == null) return;
        if (sprite != null)
        {
            cardBackground.sprite = sprite;
            cardBackground.type = Image.Type.Sliced;
            cardBackground.color = Color.white;
        }
        else
        {
            cardBackground.color = fallbackColor;
        }
    }

    private void ApplyActionButtonSprite(Sprite sprite, Color fallbackColor)
    {
        if (actionButtonBackground == null) return;
        if (sprite != null)
        {
            actionButtonBackground.sprite = sprite;
            actionButtonBackground.type = Image.Type.Sliced;
            actionButtonBackground.color = Color.white;
        }
        else
        {
            actionButtonBackground.color = fallbackColor;
        }
    }

    public void Setup(RecipeData recipe, ItemContainer backpack, bool isCurrentlySelected, System.Action onAutoFill, ItemContainer chest = null)
    {
        currentRecipe = recipe;
        if (recipe == null) return;

        // Visuals
        TextMeshProUGUI rowTMP = transform.Find("InfoColumn/TitleRow")?.GetComponent<TextMeshProUGUI>();
        if (rowTMP != null)
        {
            rowTMP.text = "";
            rowTMP.enabled = false;
            Destroy(rowTMP);
        }

        // Check unlocked state
        bool isUnlocked = ProgressionManager.Instance != null
            ? ProgressionManager.Instance.IsRecipeUnlocked(recipe)
            : recipe.isDefaultUnlocked;

        if (!isUnlocked)
        {
            Sprite lockedCard = lockedCardSprite != null ? lockedCardSprite : DungeonUIAssetHelper.FindSprite("Atom_Card_Missing");
            ApplyCardSprite(lockedCard, new Color(0.92f, 0.88f, 0.83f, 1f));

            if (titleText != null)
            {
                titleText.color = InkDark;
                titleText.text = "<color=#6B5B4D>??? [CHƯA MỞ KHÓA]</color>";
            }

            if (starsText != null)
            {
                starsText.gameObject.SetActive(false);
            }

            if (dishIcon != null)
            {
                dishIcon.sprite = null;
                dishIcon.gameObject.SetActive(false);
            }

            if (detailText != null)
            {
                detailText.color = InkBody;
                detailText.text = "<color=#5C4B3E>Chưa rõ công thức — Hãy tự kết hợp 4 nguyên liệu và nấu trên bếp để mở khóa!</color>";
            }

            if (lockIcon != null)
            {
                lockIcon.SetActive(true);
            }

            if (actionButton != null)
            {
                actionButton.onClick.RemoveAllListeners();
                if (actionButtonText != null)
                {
                    actionButtonText.text = "KHÓA";
                    actionButtonText.color = Color.white;
                }
                Sprite missBtn = missingButtonSprite != null ? missingButtonSprite : DungeonUIAssetHelper.FindSprite("Btn_Missing_Ingredients");
                ApplyActionButtonSprite(missBtn, new Color(0.45f, 0.36f, 0.30f, 0.95f));
                actionButton.interactable = false;
            }
            return;
        }

        if (dishIcon != null)
        {
            dishIcon.color = Color.white;
        }

        if (lockIcon != null)
        {
            lockIcon.SetActive(false);
        }

        if (titleText != null)
        {
            titleText.color = InkDark;
            titleText.text = recipe.DisplayName;
        }
        if (detailText != null)
        {
            detailText.color = InkBody;
        }
        if (starsText != null) starsText.gameObject.SetActive(false);

        Sprite cardSpr = isCurrentlySelected
            ? (selectedCardSprite != null ? selectedCardSprite : DungeonUIAssetHelper.FindSprite("Món ăn dự kiến"))
            : (normalCardSprite != null ? normalCardSprite : DungeonUIAssetHelper.FindSprite("Món ăn  (không chọn)"));
        ApplyCardSprite(cardSpr, Color.white);

        if (dishIcon != null && recipe.resultFood != null)
        {
            dishIcon.sprite = recipe.resultFood.itemIcon;
            dishIcon.gameObject.SetActive(dishIcon.sprite != null);
        }

        // Details (Buffs / Stats & Ingredients)
        if (detailText != null)
        {
            StringBuilder sb = new StringBuilder();
            if (recipe.resultFood != null)
            {
                if (recipe.resultFood.healthValue > 0) sb.Append($"Hồi {recipe.resultFood.healthValue} HP");
                if (recipe.resultFood.hungerValue > 0)
                {
                    if (sb.Length > 0) sb.Append(" & ");
                    sb.Append($"{recipe.resultFood.hungerValue} Độ No");
                }
            }
            if (recipe.isCategoryRecipe)
            {
                sb.Append(" | Cần: ");
                List<string> reqs = new List<string>();
                if (recipe.requiredVeggieCount > 0) reqs.Add($"{recipe.requiredVeggieCount} Rau củ bất kỳ");
                if (recipe.requiredMeatCount > 0) reqs.Add($"{recipe.requiredMeatCount} Thịt bất kỳ");
                sb.Append(string.Join(" + ", reqs));
            }
            else
            {
                sb.Append(" | Cần: ");
                if (recipe.ingredients != null)
                {
                    for (int i = 0; i < recipe.ingredients.Length; i++)
                    {
                        var req = recipe.ingredients[i];
                        if (req != null && req.item != null)
                        {
                            if (i > 0) sb.Append(" + ");
                            sb.Append($"{req.item.itemName}x{req.amount}");
                        }
                    }
                }
            }
            detailText.text = sb.ToString();
        }

        // Button state
        bool canCook = CookingManager.Instance != null && CookingManager.Instance.CanCook(recipe, backpack, chest);

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();

            if (isCurrentlySelected)
            {
                if (actionButtonText != null)
                {
                    actionButtonText.text = "ĐANG CHỌN";
                    actionButtonText.color = Color.white;
                }
                Sprite selBtn = neutralButtonSprite != null ? neutralButtonSprite : DungeonUIAssetHelper.FindSprite("Btn");
                ApplyActionButtonSprite(selBtn, new Color(0.85f, 0.50f, 0.05f, 1f));
                if (actionButtonBackground != null && selBtn != null) actionButtonBackground.color = new Color(1f, 0.85f, 0.55f, 1f);
                actionButton.interactable = false;
            }
            else if (canCook)
            {
                if (actionButtonText != null)
                {
                    actionButtonText.text = "TỰ ĐIỀN";
                    actionButtonText.color = Color.white;
                }
                Sprite autoBtn = autoFillButtonSprite != null ? autoFillButtonSprite : DungeonUIAssetHelper.FindSprite("Btn_Auto_Fill");
                ApplyActionButtonSprite(autoBtn, new Color(0.18f, 0.52f, 0.20f, 1f));
                actionButton.interactable = true;
                actionButton.onClick.AddListener(() => onAutoFill?.Invoke());
            }
            else
            {
                if (actionButtonText != null)
                {
                    actionButtonText.text = "THIẾU NL";
                    actionButtonText.color = Color.white;
                }
                Sprite missBtn = missingButtonSprite != null ? missingButtonSprite : DungeonUIAssetHelper.FindSprite("Btn_Missing_Ingredients");
                ApplyActionButtonSprite(missBtn, new Color(0.36f, 0.30f, 0.26f, 0.9f));
                actionButton.interactable = false;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentRecipe == null || currentRecipe.resultFood == null) return;
        bool isUnlocked = ProgressionManager.Instance != null
            ? ProgressionManager.Instance.IsRecipeUnlocked(currentRecipe)
            : currentRecipe.isDefaultUnlocked;
        if (!isUnlocked) return;

        ItemTooltipUI.EnsureInstance()?.Show(currentRecipe.resultFood, transform.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltipUI.Instance?.Hide();
    }

    private void OnDisable()
    {
        ItemTooltipUI.Instance?.Hide();
    }
}
