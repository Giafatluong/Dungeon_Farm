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
                if (_instance == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("CookingManager_Runtime");
                    _instance = go.AddComponent<CookingManager>();
                }
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }

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

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        EnsureDefaultRecipes();
        EnsureKitchenStationInBase();
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
            RecipeData[] allFound = GameAssetHelper.LoadAll<RecipeData>();
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

    #region Recipe Queries & Pot Matching
    public bool IsValidRecipe(RecipeData r)
    {
        if (r == null || r.resultFood == null) return false;
        if (r.isCategoryRecipe)
        {
            return (r.requiredMeatCount + r.requiredVeggieCount) == 4;
        }

        if (r.ingredients == null || r.ingredients.Length == 0) return false;
        for (int i = 0; i < r.ingredients.Length; i++)
        {
            if (r.ingredients[i] != null && r.ingredients[i].item != null && r.ingredients[i].amount > 0)
                return true;
        }
        return false;
    }

    public static bool IsMeatIngredient(ItemData item)
    {
        if (item == null) return false;
        string n = item.name.ToLower();
        string d = !string.IsNullOrEmpty(item.itemName) ? item.itemName.ToLower() : "";
        return n.Contains("meat") || d.Contains("thịt") ||
               n.Contains("bone") || d.Contains("xương") ||
               n.Contains("batwing") || d.Contains("dạ lan") ||
               n.Contains("imptalon") || d.Contains("dạ quỷ") ||
               n.Contains("slime") || d.Contains("slime");
    }

    public static bool IsVeggieIngredient(ItemData item)
    {
        if (item == null) return false;
        string n = item.name.ToLower();
        string d = !string.IsNullOrEmpty(item.itemName) ? item.itemName.ToLower() : "";
        return n.Contains("carrot") || d.Contains("cà rốt") ||
               n.Contains("tomato") || d.Contains("cà chua") ||
               n.Contains("cabbage") || d.Contains("bắp cải") ||
               n.Contains("cucumber") || d.Contains("dưa chuột") ||
               n.Contains("corn") || d.Contains("bắp") ||
               n.Contains("wheat") || d.Contains("lúa mì") ||
               n.Contains("chilli") || n.Contains("chili") || d.Contains("ớt") ||
               n.Contains("spicyspore") || d.Contains("nấm") ||
               item.itemType == ItemData.ItemType.Produce;
    }

    public static int GetTotalMeatCount(params ItemContainer[] containers)
    {
        int count = 0;
        if (containers == null) return 0;
        for (int c = 0; c < containers.Length; c++)
        {
            ItemContainer cont = containers[c];
            if (cont == null || cont.itemSlots == null) continue;
            for (int s = 0; s < cont.itemSlots.Length; s++)
            {
                ItemSlot slot = cont.itemSlots[s];
                if (slot != null && slot.itemData != null && slot.amount > 0 && IsMeatIngredient(slot.itemData))
                {
                    count += slot.amount;
                }
            }
        }
        return count;
    }

    public static int GetTotalVeggieCount(params ItemContainer[] containers)
    {
        int count = 0;
        if (containers == null) return 0;
        for (int c = 0; c < containers.Length; c++)
        {
            ItemContainer cont = containers[c];
            if (cont == null || cont.itemSlots == null) continue;
            for (int s = 0; s < cont.itemSlots.Length; s++)
            {
                ItemSlot slot = cont.itemSlots[s];
                if (slot != null && slot.itemData != null && slot.amount > 0 && IsVeggieIngredient(slot.itemData))
                {
                    count += slot.amount;
                }
            }
        }
        return count;
    }

    public List<RecipeData> GetAllAvailableRecipes()
    {
        EnsureDefaultRecipes();
        List<RecipeData> list = new();
        HashSet<FoodData> seenFoods = new();

        void AddRecipeIfUnique(RecipeData r)
        {
            if (r == null || !IsValidRecipe(r)) return;
            if (r.resultFood != null)
            {
                if (seenFoods.Contains(r.resultFood)) return;
                seenFoods.Add(r.resultFood);
            }
            else if (list.Contains(r)) return;

            list.Add(r);
        }

        if (defaultRecipes != null)
        {
            for (int i = 0; i < defaultRecipes.Count; i++)
            {
                AddRecipeIfUnique(defaultRecipes[i]);
            }
        }

        if (ProgressionManager.Instance != null && ProgressionManager.Instance.unlockedRecipes != null)
        {
            for (int i = 0; i < ProgressionManager.Instance.unlockedRecipes.Count; i++)
            {
                AddRecipeIfUnique(ProgressionManager.Instance.unlockedRecipes[i]);
            }
        }
        return list;
    }

    public List<RecipeData> GetUnlockedRecipes()
    {
        List<RecipeData> all = GetAllAvailableRecipes();
        List<RecipeData> unlocked = new List<RecipeData>();
        for (int i = 0; i < all.Count; i++)
        {
            RecipeData r = all[i];
            if (r == null) continue;
            bool isUnlocked = ProgressionManager.Instance != null
                ? ProgressionManager.Instance.IsRecipeUnlocked(r)
                : r.isDefaultUnlocked;
            if (isUnlocked)
            {
                unlocked.Add(r);
            }
        }
        return unlocked;
    }

    /// <summary>
    /// Matches ingredients in the pot with all existing recipes.
    /// Every recipe strictly requires exactly 4 pot slots filled (total ingredients = 4).
    /// Freeform discovery mechanism: When types and quantities match a recipe exactly, the dish is cooked!
    /// </summary>
    public RecipeData MatchRecipe(List<ItemSlot> potItems)
    {
        // Enforce 4 slots filled rule
        if (potItems == null || potItems.Count != 4) return null;

        int totalAmount = 0;
        for (int i = 0; i < potItems.Count; i++)
        {
            if (potItems[i] == null || potItems[i].itemData == null || potItems[i].amount <= 0) return null;
            totalAmount += potItems[i].amount;
        }
        if (totalAmount != 4) return null;

        // 1. Group ingredients in pot by ItemData
        Dictionary<ItemData, int> potSummary = new Dictionary<ItemData, int>();
        for (int i = 0; i < potItems.Count; i++)
        {
            ItemSlot slot = potItems[i];
            if (slot == null || slot.itemData == null || slot.amount <= 0) continue;

            ItemData existingKey = null;
            foreach (var kvp in potSummary)
            {
                if (ItemContainer.IsItemMatch(kvp.Key, slot.itemData))
                {
                    existingKey = kvp.Key;
                    break;
                }
            }

            if (existingKey != null)
            {
                potSummary[existingKey] += slot.amount;
            }
            else
            {
                potSummary[slot.itemData] = slot.amount;
            }
        }

        if (potSummary.Count == 0) return null;

        // 2. PRIORITY 1: Match against specific recipes first
        List<RecipeData> allRecipes = GetAllAvailableRecipes();
        for (int r = 0; r < allRecipes.Count; r++)
        {
            RecipeData recipe = allRecipes[r];
            if (!IsValidRecipe(recipe) || recipe.isCategoryRecipe) continue;

            // Number of ingredient types must match
            int validReqCount = 0;
            for (int i = 0; i < recipe.ingredients.Length; i++)
            {
                if (recipe.ingredients[i] != null && recipe.ingredients[i].item != null && recipe.ingredients[i].amount > 0)
                {
                    validReqCount++;
                }
            }

            if (potSummary.Count != validReqCount) continue;

            // Check each required ingredient
            bool isMatch = true;
            for (int i = 0; i < recipe.ingredients.Length; i++)
            {
                ItemRequirement req = recipe.ingredients[i];
                if (req == null || req.item == null || req.amount <= 0) continue;

                int amountInPot = 0;
                foreach (var kvp in potSummary)
                {
                    if (ItemContainer.IsItemMatch(kvp.Key, req.item))
                    {
                        amountInPot = kvp.Value;
                        break;
                    }
                }

                // Must match exact required quantity
                if (amountInPot != req.amount)
                {
                    isMatch = false;
                    break;
                }
            }

            if (isMatch)
            {
                return recipe;
            }
        }

        // 3. PRIORITY 2: Match against Don't Starve-style Category Recipes!
        // (E.g. 3 Veggies + 1 Meat -> Meatballs, 4 Veggies -> Ratatouille, 2 Meat + 2 Veggies -> Stir-fry, 3 Meat + 1 Veggie -> Meaty Stew)
        int potMeatCount = 0;
        int potVeggieCount = 0;

        foreach (var kvp in potSummary)
        {
            ItemData item = kvp.Key;
            int count = kvp.Value;
            if (IsMeatIngredient(item))
            {
                potMeatCount += count;
            }
            else if (IsVeggieIngredient(item))
            {
                potVeggieCount += count;
            }
        }

        if (potMeatCount + potVeggieCount == 4)
        {
            for (int r = 0; r < allRecipes.Count; r++)
            {
                RecipeData recipe = allRecipes[r];
                if (!IsValidRecipe(recipe) || !recipe.isCategoryRecipe) continue;

                if (recipe.requiredMeatCount == potMeatCount && recipe.requiredVeggieCount == potVeggieCount)
                {
                    return recipe;
                }
            }
        }

        return null; // No matching recipe found
    }

    /// <summary>
    /// Attempts to cook experimental pot ingredients (freeform cooking).
    /// </summary>
    public bool TryCookPot(List<ItemSlot> potItems, ItemContainer destinationContainer, out RecipeData matchedRecipe, out bool isNewDiscovery)
    {
        return TryCookPot(potItems, destinationContainer, out matchedRecipe, out isNewDiscovery, out _);
    }

    public bool TryCookPot(List<ItemSlot> potItems, ItemContainer destinationContainer, out RecipeData matchedRecipe, out bool isNewDiscovery, out string failReason)
    {
        matchedRecipe = MatchRecipe(potItems);
        isNewDiscovery = false;
        failReason = "";

        if (matchedRecipe != null)
        {
            if (destinationContainer != null && matchedRecipe.resultFood != null)
            {
                if (!destinationContainer.CanAddItem(matchedRecipe.resultFood))
                {
                    failReason = "Backpack is full! Free up space before cooking.";
                    OnCookFailed?.Invoke(failReason);
                    return false;
                }
                destinationContainer.AddItem(matchedRecipe.resultFood, matchedRecipe.resultAmount);
            }

            if (ProgressionManager.Instance != null)
            {
                isNewDiscovery = ProgressionManager.Instance.DiscoverRecipe(matchedRecipe);
                ProgressionManager.Instance.UnlockRecipe(matchedRecipe);
            }

            OnCookSuccess?.Invoke(matchedRecipe);

            if (VFXManager.Instance != null)
            {
                Vector3 kitchenPos = (KitchenStation.Instance != null)
                    ? KitchenStation.Instance.transform.position
                    : (PlayerStats.Instance != null ? PlayerStats.Instance.transform.position : Vector3.zero);
                VFXManager.Instance.PlayCookingSparkles(kitchenPos);
            }

            return true;
        }

        failReason = "Ingredient combination failed! The dish was burnt.";
        OnCookFailed?.Invoke(failReason);
        return false;
    }

    public bool CanCook(RecipeData recipe, ItemContainer container)
    {
        if (container == null) return false;
        return CanCook(recipe, new ItemContainer[] { container });
    }

    public bool CanCook(RecipeData recipe, params ItemContainer[] containers)
    {
        if (recipe == null || containers == null || containers.Length == 0) return false;
        if (recipe.resultFood == null) return false;

        if (recipe.isCategoryRecipe)
        {
            int totalMeat = GetTotalMeatCount(containers);
            int totalVeggie = GetTotalVeggieCount(containers);
            return totalMeat >= recipe.requiredMeatCount && totalVeggie >= recipe.requiredVeggieCount;
        }

        if (recipe.ingredients == null || recipe.ingredients.Length == 0) return false;

        bool hasValidIngredient = false;
        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            ItemRequirement req = recipe.ingredients[i];
            if (req == null || req.item == null || req.amount <= 0) continue;

            hasValidIngredient = true;
            if (GetTotalItemCount(req.item, containers) < req.amount)
            {
                return false;
            }
        }

        return hasValidIngredient;
    }
    #endregion

    #region Cooking Execution (Legacy support for CampUI)
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
            string failMsg = $"Not enough ingredients to cook: {recipe.DisplayName}";
            OnCookFailed?.Invoke(failMsg);
            Debug.Log(failMsg);
            return false;
        }

        // Check dish storage capacity before deducting ingredients
        bool hasInitialSpace = primaryResultContainer.CanAddItem(recipe.resultFood);
        if (!hasInitialSpace)
        {
            // Check if consuming ingredients will free up at least 1 slot in the primary container
            bool willFreeSlot = false;
            for (int i = 0; i < recipe.ingredients.Length; i++)
            {
                ItemRequirement req = recipe.ingredients[i];
                if (req == null || req.item == null || req.amount <= 0) continue;

                int countInPrimary = primaryResultContainer.GetItemCount(req.item);
                if (countInPrimary > 0 && countInPrimary <= req.amount)
                {
                    willFreeSlot = true;
                    break;
                }
            }

            if (!willFreeSlot)
            {
                string failMsg = "Backpack is full! Free up space before cooking.";
                OnCookFailed?.Invoke(failMsg);
                Debug.LogWarning($"[CookingManager] {failMsg}");
                return false;
            }
        }

        // Deduct ingredients sequentially across provided containers with full rollback protection
        List<(ItemContainer container, ItemData item, int amount)> deductions = new();
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
                    deductions.Add((cont, req.item, take));
                    remainingToDeduct -= take;
                }
            }

            if (remainingToDeduct > 0)
            {
                allDeducted = false;
                break;
            }
        }

        if (!allDeducted || !primaryResultContainer.CanAddItem(recipe.resultFood))
        {
            // Rollback all deductions if any deduction failed or container still has no room
            for (int d = 0; d < deductions.Count; d++)
            {
                deductions[d].container.AddItem(deductions[d].item, deductions[d].amount);
            }

            string failMsg = !allDeducted
                ? $"Not enough ingredients to cook: {recipe.DisplayName}"
                : "Backpack is full! No space to receive the dish.";
            OnCookFailed?.Invoke(failMsg);
            Debug.LogWarning($"[CookingManager] {failMsg}");
            return false;
        }

        // Add cooked food product
        primaryResultContainer.AddItem(recipe.resultFood, recipe.resultAmount);

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.DiscoverRecipe(recipe);
            ProgressionManager.Instance.UnlockRecipe(recipe);
        }

        Debug.Log($"Successfully cooked: {recipe.DisplayName} x{recipe.resultAmount}");
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
