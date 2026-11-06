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
    private RecipeData currentRecipe;

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
            if (titleText != null)
            {
                titleText.text = "<color=#888888>??? [CHƯA MỞ KHÓA]</color>";
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
                detailText.text = "<color=#888888>Chưa rõ công thức — Hãy tự kết hợp 4 nguyên liệu và nấu trên bếp để mở khóa!</color>";
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
                    actionButtonText.color = new Color(0.65f, 0.65f, 0.70f, 0.85f);
                }
                if (actionButtonBackground != null) actionButtonBackground.color = new Color(0.18f, 0.20f, 0.25f, 0.85f);
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

        if (titleText != null) titleText.text = recipe.DisplayName;
        if (starsText != null) starsText.gameObject.SetActive(false);

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
                if (actionButtonBackground != null) actionButtonBackground.color = new Color(0.85f, 0.50f, 0.05f, 1f); // Amber orange
                actionButton.interactable = false;
            }
            else if (canCook)
            {
                if (actionButtonText != null)
                {
                    actionButtonText.text = "TỰ ĐIỀN";
                    actionButtonText.color = Color.white;
                }
                if (actionButtonBackground != null) actionButtonBackground.color = new Color(0.18f, 0.52f, 0.20f, 1f); // Emerald green
                actionButton.interactable = true;
                actionButton.onClick.AddListener(() => onAutoFill?.Invoke());
            }
            else
            {
                if (actionButtonText != null)
                {
                    actionButtonText.text = "THIẾU NL";
                    actionButtonText.color = new Color(0.75f, 0.75f, 0.78f, 0.9f);
                }
                if (actionButtonBackground != null) actionButtonBackground.color = new Color(0.24f, 0.26f, 0.32f, 0.85f); // Dark slate
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
