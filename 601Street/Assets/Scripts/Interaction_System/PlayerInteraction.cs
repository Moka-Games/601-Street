using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// Sistema de interacci�n del jugador que maneja tanto IInteractable como Inventory_Item
/// Adaptado para funcionar con el nuevo sistema basado en prefabs
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuraci�n de interacci�n")]
    [SerializeField] private float interactionRange = 2.5f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Depuraci�n")]
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private Color debugRayColor = Color.green;

    // Nuevo Input System
    private PlayerControls playerControls;
    private bool interactPressed = false;

    // Referencias para objetos interactuables
    private IInteractable currentInteractable;
    private bool hasInteractedWithInteractable = false;

    // Referencias para objetos de inventario
    private Inventory_Item currentInventoryItem;

    // Estado de interacci�n
    [HideInInspector] public bool canInteract = false;
    private bool interactionsEnabled = true;

    // Variable est�tica para controlar el estado global de transici�n
    public static bool IsSceneTransitioning = false;

    private void Awake()
    {
        // Inicializar el sistema de input
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        // Habilitar el mapa de acciones
        playerControls.Gameplay.Enable();

        // Suscribirse a los eventos de input
        playerControls.Gameplay.Interact.performed += OnInteractPerformed;
        playerControls.Gameplay.Interact.canceled += OnInteractCanceled;
    }

    private void OnDisable()
    {
        // Desuscribirse de los eventos
        playerControls.Gameplay.Interact.performed -= OnInteractPerformed;
        playerControls.Gameplay.Interact.canceled -= OnInteractCanceled;

        // Deshabilitar el mapa de acciones
        playerControls.Gameplay.Disable();
    }

    // Callback para cuando se presiona el bot�n de interacci�n
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        interactPressed = true;
    }

    // Callback para cuando se suelta el bot�n de interacci�n
    private void OnInteractCanceled(InputAction.CallbackContext context)
    {
        interactPressed = false;
    }

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

        // Comprobar si hay un objeto de interacci�n activo en el Inventory_Manager
        bool isInteractionObjectActive = Inventory_Manager.Instance != null &&
                                       Inventory_Manager.Instance.HasActiveInteractionObject();

        // Si hay un objeto de interacci�n activo, la tecla E se usar� para cerrarlo
        if (isInteractionObjectActive)
        {
            if (interactPressed)
            {
                Inventory_Manager.Instance.CloseActiveInteractionObject();
                interactPressed = false; // Consumir el input
            }
            return;
        }

        // Buscar objetos interactuables
        CheckForInteractables();

        // Procesar entrada de interacci�n
        if (interactPressed && canInteract)
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

            interactPressed = false; // Consumir el input
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

        // Guardar el interactable actual antes de actualizarlo
        IInteractable previousInteractable = currentInteractable;

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

                // Comprobar si es un interactable diferente
                if (currentInteractable != previousInteractable)
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
            // Llamar al m�todo OnInteract del Inventory_Item
            // que ahora maneja toda la l�gica de interacci�n
            currentInventoryItem.OnInteract();
        }
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