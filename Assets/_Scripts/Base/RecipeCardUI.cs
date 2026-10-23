using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Individual Recipe Card in the Recipe Notebook (Sổ Tay Công Thức) of KitchenUI.
/// </summary>
public class RecipeCardUI : MonoBehaviour
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

    public void Setup(RecipeData recipe, ItemContainer backpack, bool isCurrentlySelected, System.Action onAutoFill)
    {
        if (recipe == null) return;

        // Visuals
        if (titleText != null) titleText.text = recipe.DisplayName;
        if (starsText != null) starsText.text = "⭐⭐⭐";

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
            detailText.text = sb.ToString();
        }

        // Button state
        bool canCook = CookingManager.Instance != null && CookingManager.Instance.CanCook(recipe, backpack);

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();

            if (isCurrentlySelected)
            {
                if (actionButtonText != null) actionButtonText.text = "ĐANG CHỌN";
                if (actionButtonBackground != null) actionButtonBackground.color = new Color(1f, 0.60f, 0f, 1f); // Vibrant orange
                actionButton.interactable = false;
            }
            else if (canCook)
            {
                if (actionButtonText != null) actionButtonText.text = "TỰ ĐIỀN";
                if (actionButtonBackground != null) actionButtonBackground.color = new Color(0.48f, 0.32f, 0.22f, 1f); // Cocoa brown
                actionButton.interactable = true;
                actionButton.onClick.AddListener(() => onAutoFill?.Invoke());
            }
            else
            {
                if (actionButtonText != null) actionButtonText.text = "THIẾU NL";
                if (actionButtonBackground != null) actionButtonBackground.color = new Color(0.65f, 0.62f, 0.58f, 0.85f); // Soft grey/tan
                actionButton.interactable = false;
            }
        }

        if (lockIcon != null)
        {
            lockIcon.SetActive(false);
        }
    }
}
