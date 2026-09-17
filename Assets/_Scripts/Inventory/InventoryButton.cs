using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class InventoryButton : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemAmount;

    public ItemData currentItem;
    public static ItemData selectedItem;

    public static event Action<ItemData> OnItemSelected;

    public void SetItem(ItemData itemData, int amount)
    {
        currentItem = itemData;

        itemIcon.gameObject.SetActive(true);
        itemIcon.sprite = itemData.itemIcon;

        if (itemData.isStackable)
        {
            itemAmount.gameObject.SetActive(true);
            itemAmount.text = amount.ToString();
        }
        else
        {
            itemAmount.gameObject.SetActive(false);
        }
    }

    public void ClearItem()
    {
        currentItem = null;
        itemIcon.sprite = null;
        itemIcon.gameObject.SetActive(false);
        itemAmount.gameObject.SetActive(false);
    }

    public void SelectItem()
    {
        if (currentItem != null)
        {
            selectedItem = currentItem;

            Debug.Log("Selected: " + currentItem.itemName);

            OnItemSelected?.Invoke(currentItem);
        }
    }
}