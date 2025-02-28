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
        if (currentInteractable != null && Input.GetKeyDown(interactionKey) && !hasInteracted && canInteract)
        {
            currentInteractable.Interact();
            hasInteracted = true;
        }
        else if (currentInteractable != null && Input.GetKeyDown(interactionKey) && hasInteracted && canInteract)
        {
            currentInteractable.SecondInteraction();
            hasInteracted = false;
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
                canInteract = true;

                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    Debug.Log($"Objeto interactuable encontrado: {hit.collider.gameObject.name}");
                }
                return;
            }
        }

        canInteract = false;
    }
}