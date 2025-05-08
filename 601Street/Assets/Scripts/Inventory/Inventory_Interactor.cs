using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Inventory_Interactor : MonoBehaviour
{
    public float interactionRange = 5f;
    public LayerMask interactableLayer;
    private RaycastHit hitInfo;
    private GameObject lastInteractableObject;
    private string lastItemName;
    public bool canInteract = false;
    [HideInInspector] public Inventory_Item currentInteractableItem;

    [Header("Tecla para Interactuar")]
    public KeyCode inputKey_to_Interact;

    // Diccionario para registrar si un objeto ya mostró su pop-up
    private Dictionary<GameObject, bool> popUpShown = new Dictionary<GameObject, bool>();

    private void Update()
    {
        // Si hay un objeto de interacción activo en el Inventory_Manager, usar eso en su lugar
        if (Inventory_Manager.Instance != null && Inventory_Manager.Instance.HasActiveInteractionObject())
        {
            if (Input.GetKeyDown(inputKey_to_Interact))
            {
                Inventory_Manager.Instance.CloseActiveInteractionObject();
            }
            return;
        }

        // Comportamiento tradicional para compatibilidad
        CheckForInteractable();

        if (Input.GetKeyDown(inputKey_to_Interact) && canInteract)
        {
            InteractWithObject();
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
            // Guardar el nombre del item para posible uso posterior
            lastItemName = currentInteractableItem.itemData.itemName;

            // Llamar al nuevo método OnInteract que maneja toda la lógica
            currentInteractableItem.OnInteract();
        }
    }

    // Este método ya no es necesario en el nuevo sistema, pero lo mantenemos para compatibilidad
    public void DeactivateObject()
    {
        if (Inventory_Manager.Instance != null)
        {
            Inventory_Manager.Instance.CloseActiveInteractionObject();
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