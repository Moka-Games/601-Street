using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuración de interacción")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private LayerMask interactableLayer;

    private IInteractable currentInteractable;
    private UIManager uiManager;

    [Header("Depuración")]
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private Color debugRayColor = Color.green;

    private void Start()
    {
        uiManager = FindAnyObjectByType<UIManager>();
    }

    private void Update()
    {
        CheckForInteractables();

        if (currentInteractable != null && Input.GetKeyDown(interactionKey))
        {
            currentInteractable.Interact();
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

        if (Physics.Raycast(transform.position, rayDirection, out hit, interactionRange, interactableLayer))
        {
            Debug.Log($"Raycast golpeó: {hit.collider.gameObject.name} en la capa {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    Debug.Log($"Objeto interactuable encontrado: {hit.collider.gameObject.name}");

                    if (uiManager)
                    {
                        InteractableObject obj = hit.collider.GetComponent<InteractableObject>();
                        if (obj)
                        {
                            uiManager.ShowInteractionPrompt(obj.GetInteractionPrompt());
                        }
                    }
                }
                return;
            }
        }

        if (currentInteractable != null)
        {
            currentInteractable = null;

            if (uiManager)
            {
                uiManager.HideInteractionPrompt();
            }
        }
    }
}
