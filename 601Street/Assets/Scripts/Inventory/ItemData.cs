using UnityEngine;
using UnityEngine.Events; 

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;

    public enum ItemType
    {
        Nota,
        Objeto
    }
    public ItemType itemType;

    public Sprite inventoryImage;
}