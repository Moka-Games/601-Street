using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Sistema unificado de interacci�n del jugador que maneja tanto IInteractable como Inventory_Item
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuraci�n de interacci�n")]
    [SerializeField] private float interactionRange = 2.5f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Depuraci�n")]
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private Color debugRayColor = Color.green;

    // Referencias para objetos interactuables
    private IInteractable currentInteractable;
    private bool hasInteractedWithInteractable = false;

    // Referencias para objetos de inventario
    private Inventory_Item currentInventoryItem;
    private GameObject lastInteractableObject;
    private string lastItemName;

    // Estado de interacci�n
    [HideInInspector] public bool canInteract = false;
    private bool interactionsEnabled = true;

    // Variable est�tica para controlar el estado global de transici�n
    public static bool IsSceneTransitioning = false;

    // Diccionario para registrar si un objeto ya mostr� su pop-up
    private Dictionary<GameObject, bool> popUpShown = new Dictionary<GameObject, bool>();

    private void Start()
    {
        canInteract = false;
    }

    private void Update()
    {
        // Verificar primero si estamos en transici�n de escena
        if (IsSceneTransitioning || !interactionsEnabled)
        {
            canInteract = false;
            return;
        }

        // Si tenemos un objeto interactuable activado, manejarlo
        if (lastInteractableObject != null)
        {
            if (Input.GetKeyDown(interactionKey))
            {
                DeactivateObject();
            }
            return;
        }

        // Buscar objetos interactuables
        CheckForInteractables();

        // Procesar entrada de interacci�n
        if (Input.GetKeyDown(interactionKey) && canInteract)
        {
            // Dependiendo del tipo de objeto encontrado, interactuar
            if (currentInteractable != null)
            {
                InteractWithObject();
            }
            else if (currentInventoryItem != null)
            {
                InteractWithInventoryItem();
            }
        }
    }

    private void CheckForInteractables()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionRange, debugRayColor);
        }

        // Resetear estado de interacci�n
        canInteract = false;
        currentInteractable = null;
        currentInventoryItem = null;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            // Comprobar primero si es un objeto interactuable
            currentInteractable = hit.collider.GetComponent<IInteractable>();

            // Si no es un IInteractable, comprobar si es un item de inventario
            if (currentInteractable == null)
            {
                currentInventoryItem = hit.collider.GetComponent<Inventory_Item>();
                canInteract = currentInventoryItem != null && currentInventoryItem.itemData != null;
            }
            else
            {
                canInteract = true;

                // Si cambiamos de objeto, resetear el estado de interacci�n
                if (currentInteractable != hit.collider.GetComponent<IInteractable>())
                {
                    hasInteractedWithInteractable = false;
                }
            }

            // Entrar en estado de interacci�n si es necesario
            if (canInteract && GameStateManager.Instance != null)
            {
                GameStateManager.Instance.EnterInteractingState();
            }
        }
        else
        {
            // Si no estamos apuntando a nada, volver al estado normal
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.EnterGameplayState();
            }
        }
    }

    private void InteractWithObject()
    {
        // Comprobar si ya hemos interactuado con este objeto
        if (!hasInteractedWithInteractable)
        {
            string interactableID = currentInteractable.GetInteractionID();
            Debug.Log($"Interactuando con objeto: {(currentInteractable as MonoBehaviour)?.gameObject.name ?? "Unknown"} (ID: {interactableID})");

            currentInteractable.Interact();
            hasInteractedWithInteractable = true;
        }
        else if (currentInteractable.CanBeInteractedAgain())
        {
            // Solo permitir segunda interacci�n si el objeto lo permite
            currentInteractable.SecondInteraction();
            hasInteractedWithInteractable = false;
        }
    }

    private void InteractWithInventoryItem()
    {
        if (currentInventoryItem != null && canInteract)
        {
            lastItemName = currentInventoryItem.itemData.itemName;

            // Invocar el evento OnItemInteracted antes de cualquier otra acci�n
            if (currentInventoryItem.OnItemInteracted != null)
            {
                currentInventoryItem.OnItemInteracted.Invoke();
            }

            if (currentInventoryItem.interactableObject != null)
            {
                lastInteractableObject = currentInventoryItem.interactableObject;
                lastInteractableObject.SetActive(true);

                // Buscar y asignar la funci�n al bot�n de cerrar
                AssignButtonFunction(lastInteractableObject);
            }

            Inventory_Manager.Instance.AddItem(currentInventoryItem.itemData, currentInventoryItem.onItemClick);
            Destroy(currentInventoryItem.gameObject);
        }
    }

    private void AssignButtonFunction(GameObject parentObject)
    {
        // Buscar el bot�n en el objeto interactuable, incluso si est� desactivado
        Button closeButton = FindButtonInChildren(parentObject, "Close_Interacted_Button");

        if (closeButton != null)
        {
            // A�adir la funci�n sin eliminar las ya existentes
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

    public void SetInteractionsEnabled(bool enabled)
    {
        interactionsEnabled = enabled;
        if (!enabled)
        {
            canInteract = false;
            currentInteractable = null;
            currentInventoryItem = null;
        }
    }

    public void ForceUpdateInteraction()
    {
        // No actualizar si estamos en transici�n
        if (IsSceneTransitioning) return;

        // Reiniciar el estado de interacci�n
        currentInteractable = null;
        currentInventoryItem = null;
        canInteract = false;

        // Forzar una nueva detecci�n
        CheckForInteractables();
    }

    // M�todo para desactivar las interacciones durante las transiciones de escena
    public static void SetSceneTransitionState(bool isTransitioning)
    {
        IsSceneTransitioning = isTransitioning;
        Debug.Log($"Estado de transici�n de escena: {(isTransitioning ? "Activo" : "Inactivo")}");
    }

    private void OnDrawGizmos()
    {
        if (showDebugRay)
        {
            Gizmos.color = debugRayColor;
            Gizmos.DrawRay(transform.position, transform.forward * interactionRange);
        }
    }
}