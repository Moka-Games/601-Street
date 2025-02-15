using UnityEngine;

public class Inventory_Interactor : MonoBehaviour
{
    public float interactionRange = 5f;
    public LayerMask interactableLayer;
    private RaycastHit hitInfo;

    private Inventory_Item currentItem;  

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            InteractWithObject();
        }
    }

    private void InteractWithObject()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out hitInfo, interactionRange, interactableLayer))
        {
            Inventory_Item item = hitInfo.collider.GetComponent<Inventory_Item>();

            if (item != null && item.itemData != null)
            {
                if (currentItem != null && currentItem != item)
                {
                    DeactivateCurrentItem();
                }

                if (currentItem != item)
                {
                    ActivateItem(item);
                }
                else
                {
                    DeactivateCurrentItem();
                }
            }
        }
    }

    private void ActivateItem(Inventory_Item item)
    {
        currentItem = item;
        if (item.interactableObject != null)
        {
            item.interactableObject.SetActive(true);  
        }
        Inventory_Manager.Instance.AddItem(item.itemData, item.onItemClick);
    }

    private void DeactivateCurrentItem()
    {
        if (currentItem != null && currentItem.interactableObject != null)
        {
            currentItem.interactableObject.SetActive(false);  
            Inventory_Manager.Instance.DisplayPopUp(currentItem.itemData.itemName);  
            currentItem = null; 
        }
    }

    private void OnDrawGizmos()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        Gizmos.color = Color.red;  
        Gizmos.DrawRay(ray.origin, ray.direction * interactionRange);  
    }
}
