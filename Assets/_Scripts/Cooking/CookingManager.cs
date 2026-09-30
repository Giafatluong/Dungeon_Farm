using UnityEngine;

public class CookingManager : MonoBehaviour
{
    public static CookingManager Instance { get; private set; }

    public event System.Action<RecipeData> OnCookSuccess;
    public event System.Action<string> OnCookFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
