using UnityEngine;
using UnityEngine.Events; // Necesario para usar UnityEvent

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
    public GameObject noteContent; // Solo se usa si es una nota

}