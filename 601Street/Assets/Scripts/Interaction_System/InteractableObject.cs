using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public interface IInteractable
{
    void Interact();
    void SecondInteraction();
    string GetInteractionID();
}

public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Configuración básica")]
    [SerializeField] private string interactionID;
    [SerializeField] private string interactionPrompt = "Presiona E para interactuar";
    [Tooltip("Evento que se disparará cuando el jugador interactúe con este objeto")]
    [SerializeField] private UnityEvent onInteraction;
    [SerializeField] private UnityEvent onInteracted; //Evento por si volvemos a interactuar con el mismo objeto

    [Header("Feedback")]
    [SerializeField] private GameObject rangeIndicator;
    [SerializeField] private GameObject interactIndicator;

    [Header("Detección de Rango")]
    [SerializeField] private float detectionRadius = 5f; // Radio del SphereCollider

    [Header("Configuración de Indicadores")]
    [SerializeField] private float edgeOffset = 50f; // Distancia desde el borde de la pantalla

    private bool playerOnRange = false;
    private bool isInitialized = false;

    private SphereCollider detectionCollider; // Collider para el rango de detección
    private PlayerInteraction playerInteraction;
    private Camera mainCamera;
    private RectTransform rangeIndicatorRect;
    private RectTransform interactIndicatorRect;
    private Canvas hudCanvas;
    private RectTransform canvasRectTransform;

    public bool objectInteracted = false;

    private void Start()
    {
        // Crear y configurar el SphereCollider
        detectionCollider = gameObject.AddComponent<SphereCollider>();
        detectionCollider.radius = detectionRadius;
        detectionCollider.isTrigger = true;

        mainCamera = Camera.main;

        // Buscar el HUD Canvas
        GameObject hudObj = GameObject.Find("HUD");
        if (hudObj != null)
        {
            hudCanvas = hudObj.GetComponent<Canvas>();
            if (hudCanvas == null)
            {
                hudCanvas = hudObj.GetComponentInChildren<Canvas>();
                if (hudCanvas == null)
                {
                    Debug.LogError("No se encontró componente Canvas en el objeto 'HUD'");
                }
            }

            if (hudCanvas != null)
            {
                canvasRectTransform = hudCanvas.GetComponent<RectTransform>();
            }
        }

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

        // Obtener los RectTransforms
        rangeIndicatorRect = rangeIndicator.GetComponent<RectTransform>();
        interactIndicatorRect = interactIndicator.GetComponent<RectTransform>();

        // Si no tienen RectTransform, intentar encontrarlos en sus hijos
        if (rangeIndicatorRect == null)
        {
            rangeIndicatorRect = rangeIndicator.GetComponentInChildren<RectTransform>();
        }

        if (interactIndicatorRect == null)
        {
            interactIndicatorRect = interactIndicator.GetComponentInChildren<RectTransform>();
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

    public virtual void SecondInteraction()
    {
        Debug.Log($"Interactuando por segunda vez con objeto: {gameObject.name} (ID: {interactionID})");
        onInteracted.Invoke();
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

            // Verificar si el jugador puede interactuar con este objeto específico
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
            // Si el jugador no está en rango, desactivar ambos indicadores
            rangeIndicator.SetActive(false);
            interactIndicator.SetActive(false);
        }
    }

    private bool CanInteract()
    {
        // Verificar si tenemos referencia al PlayerInteraction
        if (playerInteraction == null)
        {
            // Intentar encontrar el componente PlayerInteraction en el jugador
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerInteraction = player.GetComponent<PlayerInteraction>();
            }
        }

        // Si tenemos el playerInteraction, usar su valor canInteract
        if (playerInteraction != null)
        {
            return playerInteraction.canInteract;
        }

        // Si no podemos obtener el playerInteraction, usar la lógica por defecto
        return true;
    }

    private void UpdateIndicatorPosition()
    {
        // Verificar que tengamos todas las referencias necesarias
        if (mainCamera == null || rangeIndicatorRect == null || interactIndicatorRect == null)
        {
            // Si no tenemos la cámara principal, intentar obtenerla
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null || rangeIndicatorRect == null || interactIndicatorRect == null)
            {
                Debug.LogWarning("Faltan referencias para actualizar la posición de los indicadores");
                return;
            }
        }

        // Convertir la posición del objeto a coordenadas de pantalla
        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);

        // Verificar si el objeto está frente a la cámara
        bool isInFrontOfCamera = screenPos.z > 0;

        if (isInFrontOfCamera)
        {
            // Obtener las dimensiones de la pantalla
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 screenCenter = screenSize * 0.5f;

            // Comprobar si está dentro de la vista de la cámara
            bool isVisible = screenPos.x >= 0 && screenPos.x <= screenSize.x &&
                            screenPos.y >= 0 && screenPos.y <= screenSize.y;

            if (isVisible)
            {
                // El objeto es visible, posiciona el indicador directamente sobre él
                SetUIPosition(rangeIndicatorRect, screenPos);
                SetUIPosition(interactIndicatorRect, screenPos);

                // Restablecer la rotación cuando está visible
                rangeIndicatorRect.rotation = Quaternion.identity;
                interactIndicatorRect.rotation = Quaternion.identity;
            }
            else
            {
                // El objeto está fuera de la pantalla, posicionar en el borde
                Vector2 directionToObject = new Vector2(screenPos.x - screenCenter.x, screenPos.y - screenCenter.y).normalized;
                Vector2 edgePosition = screenCenter + directionToObject *
                    (Vector2.Distance(Vector2.zero, new Vector2(screenCenter.x - edgeOffset, screenCenter.y - edgeOffset)));

                // Asegurarse de que no se sale de los bordes de la pantalla
                edgePosition.x = Mathf.Clamp(edgePosition.x, edgeOffset, screenSize.x - edgeOffset);
                edgePosition.y = Mathf.Clamp(edgePosition.y, edgeOffset, screenSize.y - edgeOffset);

                SetUIPosition(rangeIndicatorRect, edgePosition);
                SetUIPosition(interactIndicatorRect, edgePosition);

                // Rotar el indicador para que apunte hacia el objeto
                float angle = Mathf.Atan2(directionToObject.y, directionToObject.x) * Mathf.Rad2Deg;
                rangeIndicatorRect.rotation = Quaternion.Euler(0, 0, angle - 90);
                interactIndicatorRect.rotation = Quaternion.Euler(0, 0, angle - 90);
            }
        }
        else
        {
            // Si el objeto está detrás de la cámara, colocar en el borde inferior
            Vector2 edgePosition = new Vector2(Screen.width * 0.5f, edgeOffset);
            SetUIPosition(rangeIndicatorRect, edgePosition);
            SetUIPosition(interactIndicatorRect, edgePosition);

            // Apuntar hacia abajo (objeto está detrás)
            rangeIndicatorRect.rotation = Quaternion.Euler(0, 0, 180);
            interactIndicatorRect.rotation = Quaternion.Euler(0, 0, 180);
        }
    }

    private void SetUIPosition(RectTransform rectTransform, Vector2 screenPosition)
    {
        if (rectTransform == null)
            return;

        if (hudCanvas != null)
        {
            // Determinar el tipo de renderizado del canvas
            if (hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Para ScreenSpaceOverlay, las coordenadas de pantalla se pueden usar directamente
                rectTransform.position = new Vector3(screenPosition.x, screenPosition.y, 0);
            }
            else if (hudCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // Para ScreenSpaceCamera, convertir de coordenadas de pantalla a viewport
                Vector2 viewportPosition = new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height);

                // Y luego a coordenadas de mundo
                Vector3 worldPos = hudCanvas.worldCamera != null ?
                    hudCanvas.worldCamera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, hudCanvas.planeDistance)) :
                    Camera.main.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, 10));

                rectTransform.position = worldPos;
            }
            else // RenderMode.WorldSpace
            {
                // Para WorldSpace, usar un enfoque diferente
                if (canvasRectTransform != null)
                {
                    // Calcular la posición relativa al canvas
                    Vector2 canvasSize = canvasRectTransform.sizeDelta;
                    float canvasScale = canvasRectTransform.localScale.x;

                    // Convertir de coordenadas de pantalla a coordenadas normalizadas (0-1)
                    Vector2 normalizedPos = new Vector2(
                        screenPosition.x / Screen.width,
                        screenPosition.y / Screen.height
                    );

                    // Convertir a coordenadas locales del canvas
                    Vector2 localPos = new Vector2(
                        (normalizedPos.x - 0.5f) * canvasSize.x,
                        (normalizedPos.y - 0.5f) * canvasSize.y
                    );

                    rectTransform.localPosition = new Vector3(localPos.x, localPos.y, 0);
                }
            }
        }
        else
        {
            // Si no tenemos referencia al canvas, usar el método por defecto
            rectTransform.position = new Vector3(screenPosition.x, screenPosition.y, 0);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificamos si el objeto que entra en el trigger es el jugador y si el collider es del objeto de detección
        if (other.CompareTag("Player") && other.gameObject.GetComponent<PlayerInteraction>() != null)
        {
            playerOnRange = true;
            playerInteraction = other.gameObject.GetComponent<PlayerInteraction>();
            Debug.Log("Jugador en rango de detección");

            // Forzar la activación del rangeIndicator y desactivar el interactIndicator al entrar en el rango
            rangeIndicator.SetActive(true);
            interactIndicator.SetActive(false);

            // Llamar a un pequeño retraso para asegurarnos de que el PlayerInteraction se actualice
            StartCoroutine(CheckInteractionAfterDelay());
        }
    }

    private IEnumerator CheckInteractionAfterDelay()
    {
        // Esperar un frame para que el PlayerInteraction se actualice
        yield return null;

        // Después del retraso, actualizar los indicadores
        if (playerOnRange)
        {
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
    }

    private void OnTriggerExit(Collider other)
    {
        // Verificamos si el objeto que sale del trigger es el jugador
        if (other.CompareTag("Player") && other.gameObject.GetComponent<PlayerInteraction>() != null)
        {
            playerOnRange = false;
            Debug.Log("Jugador fuera de rango de detección");

            // Desactivar ambos indicadores al salir del rango
            rangeIndicator.SetActive(false);
            interactIndicator.SetActive(false);
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

    // Método para dibujar Gizmos en el editor
    private void OnDrawGizmosSelected()
    {
        // Dibujar un círculo que represente el rango de detección
        Gizmos.color = Color.cyan; // Color del Gizmo
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}