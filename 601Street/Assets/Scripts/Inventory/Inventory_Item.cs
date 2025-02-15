using UnityEngine;
using UnityEngine.Events; // Necesario para usar UnityEvent

public class Inventory_Item : MonoBehaviour
{
    public ItemData itemData;

    // Evento que se activará al hacer clic en el ítem en el inventario
    public UnityEvent onItemClick;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (itemData != null)
            {
                Inventory_Manager.Instance.AddItem(itemData, onItemClick);
                Destroy(gameObject); // Simulamos la recolección eliminando el objeto de la escena
            }
            else
            {
                Debug.LogError("itemData is null on " + gameObject.name);
            }
        }
    }
}