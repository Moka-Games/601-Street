using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuración de interacción")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private LayerMask interactableLayer;

    // Referencias
    private IInteractable currentInteractable;
    private UIManager uiManager;

    // Para depuración
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private Color debugRayColor = Color.green;

    private void Start()
    {
        // Busca el UIManager en la escena
        uiManager = FindObjectOfType<UIManager>();
    }

    private void Update()
    {
        CheckForInteractables();

        // Si hay un objeto interactuable cerca y se presiona la tecla de interacción
        if (currentInteractable != null && Input.GetKeyDown(interactionKey))
        {
            currentInteractable.Interact();
        }
    }

    private void CheckForInteractables()
    {
        RaycastHit hit;
        Vector3 rayDirection = transform.forward; // Usa la dirección hacia adelante del jugador

        // Dibuja un rayo de depuración para visualizar
        if (showDebugRay)
        {
            Debug.DrawRay(transform.position, rayDirection * interactionRange, debugRayColor, 0.1f);
        }

        // Lanza un rayo desde el jugador hacia adelante
        if (Physics.Raycast(transform.position, rayDirection, out hit, interactionRange, interactableLayer))
        {
            Debug.Log($"Raycast golpeó: {hit.collider.gameObject.name} en la capa {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

            // Intenta obtener un componente IInteractable
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // Si encontramos un nuevo objeto interactuable
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    Debug.Log($"Objeto interactuable encontrado: {hit.collider.gameObject.name}");

                    // Actualiza la UI si el UIManager existe
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

        // Si no estamos apuntando a un objeto interactuable
        if (currentInteractable != null)
        {
            currentInteractable = null;

            // Oculta el prompt de interacción
            if (uiManager)
            {
                uiManager.HideInteractionPrompt();
            }
        }
    }
}