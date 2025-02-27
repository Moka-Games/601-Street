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
        if (lastInteractableObject != null)
        {
            if (Input.GetKeyDown(inputKey_to_Interact))
            {
                DeactivateObject();
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
                lastInteractableObject = currentInteractableItem.interactableObject;
                lastInteractableObject.SetActive(true);

                // Buscar y asignar la función al botón de cerrar
                AssignButtonFunction(lastInteractableObject);
            }

            Inventory_Manager.Instance.AddItem(currentInteractableItem.itemData, currentInteractableItem.onItemClick);
            Destroy(currentInteractableItem.gameObject);
        }
    }

    private void AssignButtonFunction(GameObject parentObject)
    {
        // Buscar el botón en el objeto interactuable, incluso si está desactivado
        Button closeButton = FindButtonInChildren(parentObject, "Close_Interacted_Button");

        if (closeButton != null)
        {
            // Añadir la función sin eliminar las ya existentes
            closeButton.onClick.AddListener(DeactivateObject);
        }
    }

    private Button FindButtonInChildren(GameObject parent, string buttonName)
    {
        Transform[] allChildren = parent.GetComponentsInChildren<Transform>(true); // Buscar en hijos, incluyendo inactivos
        foreach (Transform child in allChildren)
        {
            if (child.name == buttonName)
            {
                return child.GetComponent<Button>();
            }
        }
        return null;
    }


    public void DeactivateObject()
    {
        if (lastInteractableObject == null) return;

        lastInteractableObject.SetActive(false);

        // Mostrar el pop-up solo la primera vez que se desactiva
        if (!popUpShown.ContainsKey(lastInteractableObject) || !popUpShown[lastInteractableObject])
        {
            Inventory_Manager.Instance.DisplayPopUp(lastItemName);
            popUpShown[lastInteractableObject] = true;
        }

        lastInteractableObject = null;
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
