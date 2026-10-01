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
                if (_instance == null)
                {
                    GameObject go = new GameObject("CookingManager");
                    _instance = go.AddComponent<CookingManager>();
                }
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

        // Tự động tìm thêm recipe trong Resources / Project nếu danh sách trống
        if (list.Count == 0)
        {
            RecipeData[] loaded = Resources.FindObjectsOfTypeAll<RecipeData>();
            if (loaded != null)
            {
                for (int i = 0; i < loaded.Length; i++)
                {
                    if (loaded[i] != null && !list.Contains(loaded[i]))
                    {
                        list.Add(loaded[i]);
                    }
                }
            }
        }

        // Dự phòng: Tự động tạo công thức từ các loại cây/nông sản nếu chưa có ScriptableObject nào
        if (list.Count == 0)
        {
            GenerateFallbackRecipes(list);
        }

        return list;
    }

    private void GenerateFallbackRecipes(System.Collections.Generic.List<RecipeData> list)
    {
        ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
        FoodData[] allFoods = Resources.FindObjectsOfTypeAll<FoodData>();

        ItemData tomato = null, corn = null, chilli = null, cabbage = null;
        FoodData tomatoFood = null, cornFood = null, chilliFood = null, cabbageFood = null;

        foreach (var it in allItems)
        {
            if (it == null) continue;
            if (it.itemName == "Tomato" || it.name == "Tomato") tomato = it;
            if (it.itemName == "Corn" || it.name == "Corn") corn = it;
            if (it.itemName == "Chilli" || it.name == "Chilli") chilli = it;
            if (it.itemName == "Cabbage" || it.name == "Cabbage") cabbage = it;
        }

        foreach (var fd in allFoods)
        {
            if (fd == null) continue;
            if (fd.itemName == "Tomato" || fd.name == "Tomato") tomatoFood = fd;
            if (fd.itemName == "Corn" || fd.name == "Corn") cornFood = fd;
            if (fd.itemName == "Chilli" || fd.name == "Chilli") chilliFood = fd;
            if (fd.itemName == "Cabbage" || fd.name == "Cabbage") cabbageFood = fd;
        }

        if (tomato != null && tomatoFood != null)
        {
            RecipeData r1 = ScriptableObject.CreateInstance<RecipeData>();
            r1.recipeName = "Súp Cà Chua Nóng (Tomato Soup)";
            r1.ingredients = new ItemRequirement[] { new ItemRequirement { item = tomato, amount = 2 } };
            r1.resultFood = tomatoFood;
            r1.resultAmount = 1;
            list.Add(r1);
        }

        if (corn != null && cornFood != null && chilli != null)
        {
            RecipeData r2 = ScriptableObject.CreateInstance<RecipeData>();
            r2.recipeName = "Bắp Nướng Cay (Spicy Grilled Corn)";
            r2.ingredients = new ItemRequirement[] { new ItemRequirement { item = corn, amount = 1 }, new ItemRequirement { item = chilli, amount = 1 } };
            r2.resultFood = cornFood;
            r2.resultAmount = 1;
            list.Add(r2);
        }

        if (cabbage != null && cabbageFood != null)
        {
            RecipeData r3 = ScriptableObject.CreateInstance<RecipeData>();
            r3.recipeName = "Salad Bắp Cải Tươi (Fresh Salad)";
            r3.ingredients = new ItemRequirement[] { new ItemRequirement { item = cabbage, amount = 2 } };
            r3.resultFood = cabbageFood;
            r3.resultAmount = 1;
            list.Add(r3);
        }
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
            OnCookFailed?.Invoke("Công thức không hợp lệ.");
            return false;
        }

        if (container == null)
        {
            OnCookFailed?.Invoke("Không tìm thấy túi đồ.");
            return false;
        }

        if (!CanCook(recipe, container))
        {
            OnCookFailed?.Invoke("Không đủ nguyên liệu để nấu: " + recipe.recipeName);
            Debug.Log("Không đủ nguyên liệu để nấu: " + recipe.recipeName);
            return false;
        }

        // Trừ nguyên liệu
        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            ItemRequirement req = recipe.ingredients[i];
            if (req.item != null && req.amount > 0)
            {
                container.RemoveItem(req.item, req.amount);
            }
        }

        // Thêm món ăn vào túi đồ
        container.AddItem(recipe.resultFood, recipe.resultAmount);

        Debug.Log("Đã nấu thành công: " + recipe.resultFood.itemName + " x" + recipe.resultAmount);
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
