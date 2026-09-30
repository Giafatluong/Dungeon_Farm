using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Recipe/Recipe Data")]
public class RecipeData : ScriptableObject
{
    public string recipeName;
    public ItemRequirement[] ingredients;
    public FoodData resultFood;
    public int resultAmount = 1;
}
