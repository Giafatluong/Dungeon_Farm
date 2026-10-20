using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Recipe/Recipe Data")]
public class RecipeData : ScriptableObject
{
    public string recipeName;
    public ItemRequirement[] ingredients;
    public FoodData resultFood;
    public int resultAmount = 1;

    public string DisplayName => !string.IsNullOrWhiteSpace(recipeName)
        ? recipeName
        : (resultFood != null && !string.IsNullOrWhiteSpace(resultFood.itemName) ? resultFood.itemName : name);
}
