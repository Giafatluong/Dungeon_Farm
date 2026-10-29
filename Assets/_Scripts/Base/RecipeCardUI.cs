using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    public void Setup(RecipeData recipe, ItemContainer backpack, bool isCurrentlySelected, System.Action onAutoFill, ItemContainer chest = null)
    {
        if (recipe == null) return;

        // Visuals
        TextMeshProUGUI rowTMP = transform.Find("InfoColumn/TitleRow")?.GetComponent<TextMeshProUGUI>();
        if (rowTMP != null)
        {
            rowTMP.text = "";
            rowTMP.enabled = false;
            Destroy(rowTMP);
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

        if (lockIcon != null)
        {
            lockIcon.SetActive(false);
        }
    }
}
