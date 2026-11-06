using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Recipe/Recipe Data")]
public class RecipeData : ScriptableObject
{
    public string recipeName;
    public ItemRequirement[] ingredients;
    public FoodData resultFood;
    public int resultAmount = 1;
    public bool isDefaultUnlocked = false;

    [Header("Category Recipe (Don't Starve Style)")]
    public bool isCategoryRecipe = false;
    public int requiredMeatCount = 0;   // Số lượng thịt bất kỳ
    public int requiredVeggieCount = 0; // Số lượng rau củ bất kỳ

    public string DisplayName => !string.IsNullOrWhiteSpace(recipeName)
        ? recipeName
        : (resultFood != null && !string.IsNullOrWhiteSpace(resultFood.itemName) ? resultFood.itemName : name);
}
