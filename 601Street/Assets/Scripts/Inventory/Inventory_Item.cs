using UnityEngine;
using UnityEngine.Events;

public class Inventory_Item : MonoBehaviour
{
    public ItemData itemData;

    // Esta funci�n se realiza tanto al recoger el objeto como cuando lo pulsas en el inventario
    [Header("Funci�n Item Recogido")]
    public UnityEvent onItemClick;

    public GameObject interactableObject;
}
