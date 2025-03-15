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

    private void Start()
    {
        canInteract = false;
        hasInteracted = false;
    }

    private void Update()
    {
        CheckForInteractables();

        if (currentInteractable != null && Input.GetKeyDown(interactionKey) && canInteract)
        {
            // Comprobar si ya hemos interactuado con este objeto
            if (!hasInteracted)
            {
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
}