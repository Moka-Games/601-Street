using UnityEngine;
using UnityEngine.Events;

public interface IInteractable
{
    void Interact();
    string GetInteractionID();
}

public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Configuración básica")]
    [SerializeField] private string interactionID;
    [SerializeField] private string interactionPrompt = "Presiona E para interactuar";
    [Tooltip("Evento que se disparará cuando el jugador interactúe con este objeto")]
    [SerializeField] private UnityEvent onInteraction;

    [Header("Feedback")]
    [SerializeField] private GameObject rangeIndicator;
    [SerializeField] private GameObject interactIndicator;

    private bool playerOnRange = false;
    private bool isInitialized = false;

    private Collider detectionCollider;  // Collider para el rango de detección
    private Collider interactionCollider;  // Collider para la interacción

    private void Start()
    {
        // Obtener el collider de detección del hijo y el collider de interacción del objeto principal
        detectionCollider = transform.Find("Detection_Feedback").GetComponent<SphereCollider>();  // Collider para el rango de detección
        interactionCollider = GetComponent<Collider>();  // Collider para la interacción

        InitializeFeedback();
    }

    private void InitializeFeedback()
    {
        // Buscar los objetos de feedback en la escena
        rangeIndicator = GameObject.Find("Near_Interactable_Item_Feedback");
        interactIndicator = GameObject.Find("Input_Interactable_Feedback");

        if (rangeIndicator == null || interactIndicator == null)
        {
            Debug.LogError("No se encontraron los objetos de feedback en la escena.");
            enabled = false;
            return;
        }

        // Desactivar los indicadores al inicio
        rangeIndicator.SetActive(false);
        interactIndicator.SetActive(false);

        isInitialized = true;
    }

    public virtual void Interact()
    {
        Debug.Log($"Interactuando con objeto: {gameObject.name} (ID: {interactionID})");
        onInteraction.Invoke();
    }

    public string GetInteractionID()
    {
        return interactionID;
    }

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    private void Update()
    {
        if (!isInitialized) return;

        if (playerOnRange)
        {
            UpdateIndicatorPosition();

            if (CanInteract())
            {
                rangeIndicator.SetActive(false);
                interactIndicator.SetActive(true);
            }
            else
            {
                rangeIndicator.SetActive(true);
                interactIndicator.SetActive(false);
            }
        }
        else
        {
            rangeIndicator.SetActive(false);
            interactIndicator.SetActive(false);
        }
    }

    private bool CanInteract()
    {
        // Aquí puedes agregar lógica adicional para determinar si el jugador puede interactuar
        return true;
    }

    private void UpdateIndicatorPosition()
    {
        // Implementa la lógica para posicionar los indicadores en la pantalla
        // Similar a la del script de referencia
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificamos si el objeto que entra en el trigger es el jugador y si el collider es el de detección
        if (other.CompareTag("Player") && other == detectionCollider)
        {
            playerOnRange = true;
            Debug.Log("Jugador en rango de detección");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Verificamos si el objeto que sale del trigger es el jugador y si el collider es el de detección
        if (other.CompareTag("Player") && other == detectionCollider)
        {
            playerOnRange = false;
            Debug.Log("Jugador fuera de rango de detección");
        }
    }

    private void OnDisable()
    {
        if (rangeIndicator != null) rangeIndicator.SetActive(false);
        if (interactIndicator != null) interactIndicator.SetActive(false);
    }

    private void OnDestroy()
    {
        if (rangeIndicator != null) Destroy(rangeIndicator);
        if (interactIndicator != null) Destroy(interactIndicator);
    }
}
