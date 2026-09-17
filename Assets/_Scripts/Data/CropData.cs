using UnityEngine;

[CreateAssetMenu(fileName = "New Crop")]
public class CropData : ScriptableObject
{
    public string cropName;
    public ItemData seedItem;
    public ItemData producedItem; //san pham tao ra
    public Sprite[] growthStages; //cac giai doan phat trien
    public int growthTime;  //so ngay cua tung giai doan
    public int harvestAmount;
}
