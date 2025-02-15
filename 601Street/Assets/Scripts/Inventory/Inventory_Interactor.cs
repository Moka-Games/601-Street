using UnityEngine;

public class Inventory_Interactor : MonoBehaviour
{
    public float interactionRange = 5f;
    public LayerMask interactableLayer;
    private RaycastHit hitInfo;

    private GameObject lastInteractableObject;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (lastInteractableObject != null)
            {
                lastInteractableObject.SetActive(false);
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
            Inventory_Item item = hitInfo.collider.GetComponent<Inventory_Item>();

            if (item != null && item.itemData != null)
            {
                if (item.interactableObject != null)
                {
                    item.interactableObject.SetActive(true);
                    lastInteractableObject = item.interactableObject;
                }
                Inventory_Manager.Instance.AddItem(item.itemData, item.onItemClick);
                Destroy(item.gameObject);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(ray.origin, ray.direction * interactionRange);
    }
}
