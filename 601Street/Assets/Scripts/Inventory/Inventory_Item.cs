using UnityEngine;
using UnityEngine.Events;

public class Inventory_Item : MonoBehaviour
{
    public ItemData itemData;

    // Esta función se realiza tanto al recoger el objeto como cuando lo pulsas en el inventario
    [Header("Función Item Recogido")]
    public UnityEvent onItemClick;

    public GameObject interactableObject;
}
