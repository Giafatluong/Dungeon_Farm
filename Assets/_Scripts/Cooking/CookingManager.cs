using UnityEngine;
using System.Collections.Generic;

public class CookingManager : MonoBehaviour
{
    #region Singleton & Fields
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
    public List<RecipeData> defaultRecipes = new();
    #endregion

    #region Cooking Events
    public event System.Action<RecipeData> OnCookSuccess;
    public event System.Action<string> OnCookFailed;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        EnsureDefaultRecipes();
    }

    private void Start()
    {
        EnsureKitchenStationInBase();
    }

    public void EnsureDefaultRecipes()
    {
        if (defaultRecipes == null) defaultRecipes = new List<RecipeData>();

        if (defaultRecipes.Count == 0)
        {
            RecipeData[] allFound = Resources.FindObjectsOfTypeAll<RecipeData>();
            for (int i = 0; i < allFound.Length; i++)
            {
                RecipeData r = allFound[i];
                if (r != null && !defaultRecipes.Contains(r))
                {
                    defaultRecipes.Add(r);
                }
            }
        }
    }

    public void EnsureKitchenStationInBase()
    {
        UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.name == "Base" || activeScene.name.Contains("Base"))
        {
            // 1. Look for user's placed campfire
            GameObject campfire = GameObject.Find("Campfire_Anim_1");
            if (campfire == null)
            {
                var allGOs = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < allGOs.Length; i++)
                {
                    if (allGOs[i] != null && allGOs[i].name.ToLower().Contains("campfire"))
                    {
                        campfire = allGOs[i];
                        break;
                    }
                }
            }

            if (campfire != null)
            {
                // Ensure KitchenStation component is on the campfire
                KitchenStation ks = campfire.GetComponent<KitchenStation>();
                if (ks == null)
                {
                    ks = campfire.AddComponent<KitchenStation>();
                    Debug.Log($"[CookingManager] Cooking Station attached to user's Campfire ({campfire.name}) at {campfire.transform.position}");
                }

                // Clean up any old dummy KitchenStation GameObject if it exists
                GameObject oldDummy = GameObject.Find("KitchenStation");
                if (oldDummy != null && oldDummy != campfire)
                {
                    Destroy(oldDummy);
                }
            }
            else
            {
                // Fallback: if no campfire found, keep KitchenStation
                KitchenStation existing = FindFirstObjectByType<KitchenStation>(FindObjectsInactive.Include);
                if (existing == null)
                {
                    GameObject stationGO = new GameObject("KitchenStation");
                    stationGO.transform.position = new Vector3(14.46f, -2.59f, 0f);
                    stationGO.AddComponent<KitchenStation>();
                }
            }
        }
    }
    #endregion

    #region Recipe Queries
    public bool IsValidRecipe(RecipeData r)
    {
        if (r == null || r.resultFood == null) return false;
        if (r.ingredients == null || r.ingredients.Length == 0) return false;
        for (int i = 0; i < r.ingredients.Length; i++)
        {
            if (r.ingredients[i] != null && r.ingredients[i].item != null && r.ingredients[i].amount > 0)
                return true;
        }
        return false;
    }

    public List<RecipeData> GetAllAvailableRecipes()
    {
        EnsureDefaultRecipes();
        List<RecipeData> list = new();

        if (defaultRecipes != null)
        {
            for (int i = 0; i < defaultRecipes.Count; i++)
            {
                RecipeData r = defaultRecipes[i];
                if (r != null && IsValidRecipe(r) && !list.Contains(r))
                {
                    list.Add(r);
                }
            }
        }

        if (ProgressionManager.Instance != null && ProgressionManager.Instance.unlockedRecipes != null)
        {
            for (int i = 0; i < ProgressionManager.Instance.unlockedRecipes.Count; i++)
            {
                RecipeData r = ProgressionManager.Instance.unlockedRecipes[i];
                if (r != null && IsValidRecipe(r) && !list.Contains(r))
                {
                    list.Add(r);
                }
            }
        }
        return list;
    }

    public bool CanCook(RecipeData recipe, ItemContainer container)
    {
        if (container == null) return false;
        return CanCook(recipe, new ItemContainer[] { container });
    }

    public bool CanCook(RecipeData recipe, params ItemContainer[] containers)
    {
        if (recipe == null || containers == null || containers.Length == 0) return false;
        if (recipe.ingredients == null || recipe.ingredients.Length == 0) return false;
        if (recipe.resultFood == null) return false;

        bool hasValidIngredient = false;
        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            ItemRequirement req = recipe.ingredients[i];
            if (req == null || req.item == null || req.amount <= 0) return false;

            hasValidIngredient = true;
            if (GetTotalItemCount(req.item, containers) < req.amount)
            {
                return false;
            }
        }

        return hasValidIngredient;
    }
    #endregion

    #region Cooking Execution
    public bool Cook(RecipeData recipe, ItemContainer container)
    {
        if (container == null)
        {
            OnCookFailed?.Invoke("Inventory container not found.");
            return false;
        }
        return Cook(recipe, container, new ItemContainer[] { container });
    }

    public bool Cook(RecipeData recipe, ItemContainer primaryResultContainer, params ItemContainer[] ingredientContainers)
    {
        if (recipe == null)
        {
            OnCookFailed?.Invoke("Invalid recipe.");
            return false;
        }

        if (primaryResultContainer == null)
        {
            if (ingredientContainers != null && ingredientContainers.Length > 0)
                primaryResultContainer = ingredientContainers[0];
            else
            {
                OnCookFailed?.Invoke("No destination container found.");
                return false;
            }
        }

        if (!CanCook(recipe, ingredientContainers))
        {
            string failMsg = $"Not enough ingredients to cook: {recipe.recipeName}";
            OnCookFailed?.Invoke(failMsg);
            Debug.Log(failMsg);
            return false;
        }

        // Deduct ingredients sequentially across provided containers
        bool allDeducted = true;
        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            ItemRequirement req = recipe.ingredients[i];
            if (req == null || req.item == null || req.amount <= 0) continue;

            int remainingToDeduct = req.amount;
            for (int c = 0; c < ingredientContainers.Length && remainingToDeduct > 0; c++)
            {
                ItemContainer cont = ingredientContainers[c];
                if (cont == null) continue;

                int hasInThis = cont.GetItemCount(req.item);
                if (hasInThis > 0)
                {
                    int take = Mathf.Min(remainingToDeduct, hasInThis);
                    cont.RemoveItem(req.item, take);
                    remainingToDeduct -= take;
                }
            }

            if (remainingToDeduct > 0)
            {
                allDeducted = false;
                break;
            }
        }

        if (!allDeducted)
        {
            string failMsg = $"Failed to deduct required ingredients for: {recipe.recipeName}";
            OnCookFailed?.Invoke(failMsg);
            Debug.LogWarning(failMsg);
            return false;
        }

        // Add cooked food product
        primaryResultContainer.AddItem(recipe.resultFood, recipe.resultAmount);

        Debug.Log($"Successfully cooked: {recipe.resultFood.itemName} x{recipe.resultAmount}");
        OnCookSuccess?.Invoke(recipe);
        return true;
    }
    #endregion

    #region Inventory Helpers
    public int GetItemCount(ItemContainer container, ItemData item)
    {
        if (container == null || item == null) return 0;
        return container.GetItemCount(item);
    }

    public int GetTotalItemCount(ItemData item, params ItemContainer[] containers)
    {
        if (item == null || containers == null) return 0;
        int total = 0;
        for (int i = 0; i < containers.Length; i++)
        {
            if (containers[i] != null)
            {
                total += containers[i].GetItemCount(item);
            }
        }
        return total;
    }
    #endregion
}
