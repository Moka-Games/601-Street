using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuración de interacción")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private LayerMask interactableLayer;

    private IInteractable currentInteractable;

    [Header("Depuración")]
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private Color debugRayColor = Color.green;

    private bool hasInteracted = false;
    public bool canInteract = false;
    private bool interactionsEnabled = true;

    // Variable estática para controlar el estado global de transición
    public static bool IsSceneTransitioning = false;

    public void SetInteractionsEnabled(bool enabled)
    {
        interactionsEnabled = enabled;
        if (!enabled)
        {
            canInteract = false;
            currentInteractable = null;
        }
    }

    private void Start()
    {
        canInteract = false;
        hasInteracted = false;
    }

    private void Update()
    {
        // Verificar primero si estamos en transición de escena
        if (IsSceneTransitioning || !interactionsEnabled)
        {
            canInteract = false;
            return;
        }

        // Solo procesar interacciones si no estamos en transición
        CheckForInteractables();

        if (currentInteractable != null && Input.GetKeyDown(interactionKey) && canInteract)
        {
            // Comprobar si ya hemos interactuado con este objeto
            if (!hasInteracted)
            {
                // Marcar que estamos iniciando una posible transición antes de la interacción
                string interactableID = currentInteractable.GetInteractionID();
                // Corregido: Eliminada la referencia incorrecta a hit
                Debug.Log($"Interactuando con objeto: {(currentInteractable as MonoBehaviour)?.gameObject.name ?? "Unknown"} (ID: {interactableID})");

                currentInteractable.Interact();
                hasInteracted = true;
            }
            else if (currentInteractable.CanBeInteractedAgain())
            {
                // Solo permitir segunda interacción si el objeto lo permite
                currentInteractable.SecondInteraction();
                hasInteracted = false;
            }
        }
    }

    private void CheckForInteractables()
    {
        // No verificar interactables si estamos en transición
        if (IsSceneTransitioning)
        {
            canInteract = false;
            return;
        }

        RaycastHit hit;
        Vector3 rayDirection = transform.forward;

        if (showDebugRay)
        {
            Debug.DrawRay(transform.position, rayDirection * interactionRange, debugRayColor, 0.1f);
        }

        canInteract = false;

        if (Physics.Raycast(transform.position, rayDirection, out hit, interactionRange, interactableLayer))
        {
            Debug.Log($"Raycast golpeó: {hit.collider.gameObject.name} en la capa {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

            // No procesar más si estamos en transición
            if (IsSceneTransitioning) return;

            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                // Verificar si se puede interactuar con este objeto
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    hasInteracted = false; // Reiniciar el estado de interacción al cambiar de objeto
                    Debug.Log($"Objeto interactuable encontrado: {hit.collider.gameObject.name}");
                }
                // Solo permitir interacción si el objeto lo permite
                canInteract = true;
                return;
            }
        }

        // Si no estamos apuntando a un objeto interactuable, limpiar la referencia
        if (currentInteractable != null)
        {
            currentInteractable = null;
            hasInteracted = false;
        }

        canInteract = false;
    }

    public void ForceUpdateInteraction()
    {
        // No actualizar si estamos en transición
        if (IsSceneTransitioning) return;

        // Esto forzará a que se actualice el estado de interacción en el próximo Update
        // Esencialmente reiniciamos la detección de objetos interactuables
        // Liberar cualquier referencia actual
        if (currentInteractable != null)
        {
            currentInteractable = null;
            canInteract = false;
        }

        // Forzar una nueva detección inmediatamente
        CheckForInteractables();
    }

    // Método para desactivar las interacciones durante las transiciones de escena
    public static void SetSceneTransitionState(bool isTransitioning)
    {
        IsSceneTransitioning = isTransitioning;
        Debug.Log($"Estado de transición de escena: {(isTransitioning ? "Activo" : "Inactivo")}");
    }

    // Asegurarnos de que se restablece si se destruye o desactiva
    private void OnDisable()
    {
        canInteract = false;
        currentInteractable = null;
    }
}