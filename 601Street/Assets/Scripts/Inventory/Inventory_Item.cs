using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class Inventory_Item : MonoBehaviour
{
    [Header("Datos del Item")]
    public ItemData itemData;

    [Header("Funci�n Item Recogido")]
    public UnityEvent onItemClick; //Funci�n que se realiza el pulsar el objeto en el inventario
    public UnityEvent OnItemInteracted; //Funci�n que se realiza interactuar con el objeto
    public GameObject interactableObject;

    [Header("Configuraci�n de Feedback")]
    public float edgeOffset = 50f; // Distancia desde el borde de la pantalla

    // Colliders para detecci�n
    [Header("Colliders")]
    public SphereCollider detectionCollider; // Collider para mostrar feedback inicial

    // Referencias a los prefabs originales (templates)
    private static GameObject nearItemFeedbackTemplate;
    private static GameObject inputFeedbackTemplate;
    private static bool templatesInitialized = false;

    // Referencias de instancias para este item espec�fico
    private Canvas hudCanvas;
    private GameObject rangeIndicator; // Instancia del indicador de rango
    private GameObject interactIndicator; // Instancia del indicador de interacci�n

    private Inventory_Interactor inventory_Interactor;
    private bool playerInDetectionRange = false;
    private RectTransform rangeIndicatorRect;
    private RectTransform interactIndicatorRect;
    private Camera mainCamera;
    private RectTransform canvasRectTransform;
    private bool isInitialized = false;

    private void Awake()
    {
        // Crear collider si no existe
        if (detectionCollider == null)
        {
            detectionCollider = gameObject.AddComponent<SphereCollider>();
            detectionCollider.radius = 3.0f; // Radio para detecci�n
            detectionCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        // Buscar referencias autom�ticamente
        inventory_Interactor = FindAnyObjectByType<Inventory_Interactor>();
        mainCamera = Camera.main;

        // Buscar el HUD Canvas por nombre
        GameObject hudObj = GameObject.Find("HUD");
        if (hudObj != null)
        {
            hudCanvas = hudObj.GetComponent<Canvas>();
            if (hudCanvas == null)
            {
                hudCanvas = hudObj.GetComponentInChildren<Canvas>();
                if (hudCanvas == null)
                {
                    Debug.LogError("No se encontr� componente Canvas en el objeto 'HUD'");
                    enabled = false;
                    return;
                }
            }

            canvasRectTransform = hudCanvas.GetComponent<RectTransform>();
            if (canvasRectTransform == null)
            {
                Debug.LogError("El Canvas no tiene un componente RectTransform");
                enabled = false;
                return;
            }
        }
        else
        {
            Debug.LogError("No se encontr� objeto con nombre 'HUD'");
            enabled = false;
            return;
        }

        // Inicializar las plantillas solo una vez para todos los items
        InitializeFeedbackTemplates();

        // Crear los indicadores para este �tem espec�fico (pero no activarlos a�n)
        CreateFeedbackIndicators();

        // Verificaci�n final
        if (inventory_Interactor == null)
        {
            Debug.LogWarning("No se pudo encontrar Inventory_Interactor en la escena");
        }

        isInitialized = true;
    }

    private void InitializeFeedbackTemplates()
    {
        // Usar el Singleton para obtener las plantillas
        nearItemFeedbackTemplate = UITemplateManager.Instance.GetNearItemFeedbackTemplate();
        inputFeedbackTemplate = UITemplateManager.Instance.GetInputFeedbackTemplate();

        if (nearItemFeedbackTemplate == null || inputFeedbackTemplate == null)
        {
            Debug.LogError("Las plantillas de feedback no se han inicializado correctamente");
            enabled = false;
            return;
        }

        templatesInitialized = true;
        Debug.Log("Plantillas de feedback inicializadas");
    }

    private void CreateFeedbackIndicators()
    {
        // Verificar que las plantillas existan
        if (nearItemFeedbackTemplate == null || inputFeedbackTemplate == null)
        {
            Debug.LogError("Las plantillas de feedback no se han inicializado correctamente");
            return;
        }

        // Instanciar los indicadores como hijos del canvas
        rangeIndicator = Instantiate(nearItemFeedbackTemplate, hudCanvas.transform);
        interactIndicator = Instantiate(inputFeedbackTemplate, hudCanvas.transform);

        // Asegurarse de que tengan los nombres correctos para identificarlos
        rangeIndicator.name = "RangeIndicator_" + gameObject.name;
        interactIndicator.name = "InteractIndicator_" + gameObject.name;

        // Obtener sus RectTransforms
        rangeIndicatorRect = rangeIndicator.GetComponent<RectTransform>();
        interactIndicatorRect = interactIndicator.GetComponent<RectTransform>();

        // Si los indicadores no tienen RectTransform, intentar encontrarlo en sus hijos
        if (rangeIndicatorRect == null)
        {
            rangeIndicatorRect = rangeIndicator.GetComponentInChildren<RectTransform>();
            if (rangeIndicatorRect == null)
            {
                Debug.LogError("No se pudo encontrar RectTransform para el indicador de rango");
                enabled = false;
                return;
            }
        }

        if (interactIndicatorRect == null)
        {
            interactIndicatorRect = interactIndicator.GetComponentInChildren<RectTransform>();
            if (interactIndicatorRect == null)
            {
                Debug.LogError("No se pudo encontrar RectTransform para el indicador de interacci�n");
                enabled = false;
                return;
            }
        }

        // Inicialmente desactivados
        rangeIndicator.SetActive(false);
        interactIndicator.SetActive(false);

        Debug.Log("Indicadores de feedback creados para " + gameObject.name);
    }
    private void Update()
    {
        // Verificar si el objeto sigue existiendo
        if (this == null || !gameObject.activeInHierarchy || !isInitialized)
        {
            CleanupIndicators();
            return;
        }

        // Comprobaciones de seguridad
        if (rangeIndicator == null || interactIndicator == null || !enabled)
        {
            CleanupIndicators();
            return;
        }

        // Actualizar la l�gica de visualizaci�n basada en la posici�n del jugador
        UpdateFeedbackLogic();
    }

    private void UpdateFeedbackLogic()
    {
        if (playerInDetectionRange)
        {
            // Actualizar la posici�n de los indicadores en pantalla
            UpdateIndicatorPosition();

            // Verificar si el jugador est� mirando este objeto espec�fico y puede interactuar
            bool canInteractWithThis = inventory_Interactor != null &&
                                      inventory_Interactor.canInteract &&
                                      inventory_Interactor.currentInteractableItem == this;

            // Mostrar el indicador de interacci�n solo si se puede interactuar
            interactIndicator.SetActive(canInteractWithThis);

            // Mostrar el indicador de detecci�n solo si no se puede interactuar a�n
            rangeIndicator.SetActive(!canInteractWithThis);
        }
        else
        {
            // El jugador no est� en rango de detecci�n
            rangeIndicator.SetActive(false);
            interactIndicator.SetActive(false);
        }
    }

    private void UpdateIndicatorPosition()
    {
        // Verificar que tengamos todas las referencias necesarias
        if (mainCamera == null || rangeIndicatorRect == null || interactIndicatorRect == null)
        {
            Debug.LogWarning("Faltan referencias para actualizar la posici�n de los indicadores");
            return;
        }

        // Convertir la posici�n del objeto a coordenadas de pantalla
        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);

        // Verificar si el objeto est� frente a la c�mara
        bool isInFrontOfCamera = screenPos.z > 0;

        if (isInFrontOfCamera)
        {
            // Obtener las dimensiones de la pantalla
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 screenCenter = screenSize * 0.5f;

            // Comprobar si est� dentro de la vista de la c�mara
            bool isVisible = screenPos.x >= 0 && screenPos.x <= screenSize.x &&
                            screenPos.y >= 0 && screenPos.y <= screenSize.y;

            if (isVisible)
            {
                // El objeto es visible, posiciona el indicador directamente sobre �l
                SetUIPosition(rangeIndicatorRect, screenPos);
                SetUIPosition(interactIndicatorRect, screenPos);

                // Restablecer la rotaci�n cuando est� visible
                rangeIndicatorRect.rotation = Quaternion.identity;
                interactIndicatorRect.rotation = Quaternion.identity;
            }
            else
            {
                // El objeto est� fuera de la pantalla, posicionar en el borde
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
            // Si el objeto est� detr�s de la c�mara, colocar en el borde inferior
            Vector2 edgePosition = new Vector2(Screen.width * 0.5f, edgeOffset);
            SetUIPosition(rangeIndicatorRect, edgePosition);
            SetUIPosition(interactIndicatorRect, edgePosition);

            // Apuntar hacia abajo (objeto est� detr�s)
            rangeIndicatorRect.rotation = Quaternion.Euler(0, 0, 180);
            interactIndicatorRect.rotation = Quaternion.Euler(0, 0, 180);
        }
    }

    private void SetUIPosition(RectTransform rectTransform, Vector2 screenPosition)
    {
        if (hudCanvas == null || rectTransform == null)
            return;

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
                // Calcular la posici�n relativa al canvas
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInDetectionRange = true;
            Debug.Log(gameObject.name + ": Player entered detection range");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInDetectionRange = false;
            Debug.Log(gameObject.name + ": Player left detection range");

            // Desactivar los indicadores inmediatamente al salir del rango
            if (rangeIndicator != null) rangeIndicator.SetActive(false);
            if (interactIndicator != null) interactIndicator.SetActive(false);
        }
    }

    // Esta funci�n puede ser llamada cuando el jugador interact�a con el objeto
    public void OnInteract()
    {
        if (onItemClick != null)
        {
            onItemClick.Invoke();
        }
    }

    private void CleanupIndicators()
    {
        print("Cleaning Indicators");
        // Eliminar los indicadores si existen
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(false);
            Destroy(rangeIndicator);
            rangeIndicator = null;
        }

        if (interactIndicator != null)
        {
            interactIndicator.SetActive(false);
            Destroy(interactIndicator);
            interactIndicator = null;
        }
    }
    private void OnDisable()
    {
        // Limpiar cuando se deshabilite este script
        CleanupIndicators();
    }

    private void OnDestroy()
    {
        // Limpiar cuando se destruya este objeto
        CleanupIndicators();
    }
}