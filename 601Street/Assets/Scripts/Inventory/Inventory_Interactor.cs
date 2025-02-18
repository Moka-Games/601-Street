using UnityEngine;

public class Inventory_Interactor : MonoBehaviour
{
    public float interactionRange = 5f;
    public LayerMask interactableLayer;
    private RaycastHit hitInfo;

    private GameObject lastInteractableObject;
    private string lastItemName; // Guardamos el nombre del último objeto

    public bool canInteract = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (lastInteractableObject != null)
            {
                lastInteractableObject.SetActive(false);
                Inventory_Manager.Instance.DisplayPopUp(lastItemName); // Muestra el popup con el nombre del último objeto
                lastInteractableObject = null;
            }
            else
            {
                InteractWithObject();
            }
        }
    }

    private void InteractWithObject()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out hitInfo, interactionRange, interactableLayer))
        {
            canInteract = true;
            Inventory_Item item = hitInfo.collider.GetComponent<Inventory_Item>();

            if (item != null && item.itemData != null)
            {
                lastItemName = item.itemData.itemName; // Guardamos el nombre antes de destruirlo

                if (item.interactableObject != null)
                {
                    item.interactableObject.SetActive(true);
                    lastInteractableObject = item.interactableObject;
                }

                Inventory_Manager.Instance.AddItem(item.itemData, item.onItemClick);
                Destroy(item.gameObject);
            }
        }
        else
        {
            canInteract = false;
        }
    }

    private void OnDrawGizmos()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(ray.origin, ray.direction * interactionRange);
    }

    public void IsInteratable(bool value)
    {
        canInteract = value;
    }

}
