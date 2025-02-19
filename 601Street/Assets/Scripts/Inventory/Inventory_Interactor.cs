using UnityEngine;
public class Inventory_Interactor : MonoBehaviour
{
    public float interactionRange = 5f;
    public LayerMask interactableLayer;
    private RaycastHit hitInfo;
    private GameObject lastInteractableObject;
    private string lastItemName;
    public bool canInteract = false;
    private Inventory_Item currentInteractableItem;
    [Header("Tecla para Interactuar")]
    
    public KeyCode inputKey_to_Interact;

    private void Update()
    {
        if (lastInteractableObject != null)
        {
            if (Input.GetKeyDown(inputKey_to_Interact))
            {
                lastInteractableObject.SetActive(false);
                Inventory_Manager.Instance.DisplayPopUp(lastItemName);
                lastInteractableObject = null;
            }
        }
        else
        {
            CheckForInteractable();

            if (Input.GetKeyDown(inputKey_to_Interact) && canInteract)
            {
                InteractWithObject();
            }
        }
    }

    private void CheckForInteractable()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out hitInfo, interactionRange, interactableLayer))
        {
            currentInteractableItem = hitInfo.collider.GetComponent<Inventory_Item>();
            canInteract = currentInteractableItem != null && currentInteractableItem.itemData != null;
        }
        else
        {
            canInteract = false;
            currentInteractableItem = null;
        }
    }

    private void InteractWithObject()
    {
        if (currentInteractableItem != null && canInteract)
        {
            lastItemName = currentInteractableItem.itemData.itemName;

            if (currentInteractableItem.interactableObject != null)
            {
                currentInteractableItem.interactableObject.SetActive(true);
                lastInteractableObject = currentInteractableItem.interactableObject;
            }

            Inventory_Manager.Instance.AddItem(currentInteractableItem.itemData, currentInteractableItem.onItemClick);
            Destroy(currentInteractableItem.gameObject);
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