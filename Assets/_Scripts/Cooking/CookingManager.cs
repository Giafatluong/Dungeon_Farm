using UnityEngine;

public class CookingManager : MonoBehaviour
{
    private static CookingManager _instance;
    public static CookingManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<CookingManager>();
            }
            return _instance;
        }
    }

    [Header("Default Known Recipes")]
    public System.Collections.Generic.List<RecipeData> defaultRecipes = new System.Collections.Generic.List<RecipeData>();

    public event System.Action<RecipeData> OnCookSuccess;
    public event System.Action<string> OnCookFailed;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public System.Collections.Generic.List<RecipeData> GetAllAvailableRecipes()
    {
        System.Collections.Generic.List<RecipeData> list = new System.Collections.Generic.List<RecipeData>();

        if (defaultRecipes != null)
        {
            for (int i = 0; i < defaultRecipes.Count; i++)
            {
                if (defaultRecipes[i] != null && !list.Contains(defaultRecipes[i]))
                {
                    list.Add(defaultRecipes[i]);
                }
            }
        }

        if (ProgressionManager.Instance != null && ProgressionManager.Instance.unlockedRecipes != null)
        {
            for (int i = 0; i < ProgressionManager.Instance.unlockedRecipes.Count; i++)
            {
                RecipeData r = ProgressionManager.Instance.unlockedRecipes[i];
                if (r != null && !list.Contains(r))
                {
                    list.Add(r);
                }
            }
        }
        return list;
    }

    public bool CanCook(RecipeData recipe, ItemContainer container)
    {
        if (recipe == null || container == null) return false;
        if (recipe.ingredients == null || recipe.ingredients.Length == 0) return false;

        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            ItemRequirement req = recipe.ingredients[i];
            if (req.item == null || req.amount <= 0) continue;

            int count = GetItemCount(container, req.item);
            if (count < req.amount)
            {
                return false;
            }
        }

        return true;
    }

    public bool Cook(RecipeData recipe, ItemContainer container)
    {
        if (recipe == null)
        {
            OnCookFailed?.Invoke("Invalid recipe.");
            return false;
        }

        if (container == null)
        {
            OnCookFailed?.Invoke("Inventory container not found.");
            return false;
        }

        if (!CanCook(recipe, container))
        {
            OnCookFailed?.Invoke("Not enough ingredients to cook: " + recipe.recipeName);
            Debug.Log("Not enough ingredients to cook: " + recipe.recipeName);
            return false;
        }

        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            ItemRequirement req = recipe.ingredients[i];
            if (req.item != null && req.amount > 0)
            {
                container.RemoveItem(req.item, req.amount);
            }
        }

        container.AddItem(recipe.resultFood, recipe.resultAmount);

        Debug.Log("Successfully cooked: " + recipe.resultFood.itemName + " x" + recipe.resultAmount);
        OnCookSuccess?.Invoke(recipe);
        return true;
    }

    public int GetItemCount(ItemContainer container, ItemData item)
    {
        if (container == null || item == null) return 0;
        int total = 0;
        for (int i = 0; i < container.itemSlots.Length; i++)
        {
            if (container.itemSlots[i].itemData == item)
            {
                total += container.itemSlots[i].amount;
            }
        }
        return total;
    }
}
