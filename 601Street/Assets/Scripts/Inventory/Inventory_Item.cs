using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Versi�n mejorada del componente Inventory_Item que muestra un canvas de informaci�n al recogerlo
/// </summary>
public class Inventory_Item : MonoBehaviour
{
    [Header("Datos del Item")]
    public ItemData itemData;

    [Header("Configuraci�n de Interacci�n")]
    [Tooltip("Texto que se mostrar� en el indicador de interacci�n")]
    [SerializeField] private string interactionPrompt = "Recoger"; // Nueva variable
    [Tooltip("Prefab que se instanciar� cuando se interact�e con este objeto o se seleccione en el inventario")]
    public GameObject interactionPrefab;

    [Tooltip("Funci�n que se ejecuta al pulsar el objeto en el inventario")]
    public UnityEvent onItemClick;

    [Tooltip("Funci�n que se ejecuta al interactuar con el objeto en el mundo")]
    public UnityEvent OnItemInteracted;

    [Header("Configuraci�n de Feedback")]
    public float edgeOffset = 50f; // Distancia desde el borde de la pantalla

    // Colliders para detecci�n
    [Header("Colliders")]
    public SphereCollider detectionCollider; // Collider para mostrar feedback inicial

    // Referencias de instancias para este item espec�fico
    private Canvas hudCanvas;
    private GameObject rangeIndicator; // Instancia del indicador de rango
    private GameObject interactIndicator; // Instancia del indicador de interacci�n

    private PlayerInteraction playerInteraction;
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
        playerInteraction = FindAnyObjectByType<PlayerInteraction>();
        mainCamera = Camera.main;

        // Obtener el Canvas HUD a trav�s del gestor
        hudCanvas = UIFeedbackManager.Instance.GetHUDCanvas();
        if (hudCanvas != null)
        {
            canvasRectTransform = hudCanvas.GetComponent<RectTransform>();
        }

        // Crear los indicadores para este �tem espec�fico
        CreateFeedbackIndicators();

        // Verificaci�n final
        if (playerInteraction == null)
        {
            Debug.LogWarning("No se pudo encontrar PlayerInteraction en la escena");
        }
       
        if (interactIndicator != null)
        {
            UpdateInteractionPrompt(interactIndicator);
            Debug.Log($"Inicializaci�n de prompt para {gameObject.name}: '{interactionPrompt}'");
        }


        isInitialized = true;
    }

    private void CreateFeedbackIndicators()
    {
        // Obtener el nombre �nico para este objeto
        string objectIdentifier = gameObject.name + "_" + GetInstanceID();

        // Crear indicadores a trav�s del gestor
        rangeIndicator = UIFeedbackManager.Instance.CreateRangeIndicator(objectIdentifier);
        interactIndicator = UIFeedbackManager.Instance.CreateInteractIndicator(objectIdentifier);

        // Obtener los RectTransform
        if (rangeIndicator != null)
        {
            rangeIndicatorRect = rangeIndicator.GetComponent<RectTransform>();
            if (rangeIndicatorRect == null)
            {
                rangeIndicatorRect = rangeIndicator.GetComponentInChildren<RectTransform>();
            }
        }

        if (interactIndicator != null)
        {
            interactIndicatorRect = interactIndicator.GetComponent<RectTransform>();
            if (interactIndicatorRect == null)
            {
                interactIndicatorRect = interactIndicator.GetComponentInChildren<RectTransform>();
            }
        }

        // Comprobar que todo est� correcto
        if (rangeIndicator == null || interactIndicator == null ||
            rangeIndicatorRect == null || interactIndicatorRect == null)
        {
            Debug.LogError("No se pudieron crear o configurar los indicadores para " + gameObject.name);
            enabled = false;
            return;
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

    // En el m�todo UpdateFeedbackLogic
    private void UpdateFeedbackLogic()
    {
        if (playerInDetectionRange)
        {
            // Actualizar la posici�n de los indicadores en pantalla
            UpdateIndicatorPosition();

            // Verificar si el jugador est� mirando este objeto espec�fico y puede interactuar
            bool canInteractWithThis = IsTargetOfPlayerInteraction();

            // Modificaci�n clave: Primero actualizamos el texto del prompt SIEMPRE,
            // independientemente de si se muestra o no
            if (interactIndicator != null)
            {
                UpdateInteractionPrompt(interactIndicator);
            }

            // Mostrar el indicador de interacci�n solo si se puede interactuar
            if (canInteractWithThis && interactIndicator != null)
            {
                interactIndicator.SetActive(true);
            }
            else if (interactIndicator != null)
            {
                interactIndicator.SetActive(false);
            }

            // Mostrar el indicador de detecci�n solo si no se puede interactuar a�n
            if (rangeIndicator != null)
            {
                rangeIndicator.SetActive(!canInteractWithThis);
            }
        }
        else
        {
            // El jugador no est� en rango de detecci�n
            if (rangeIndicator != null) rangeIndicator.SetActive(false);
            if (interactIndicator != null) interactIndicator.SetActive(false);
        }
    }

    // M�todo para verificar si este objeto es el objetivo actual de la interacci�n del jugador
    private bool IsTargetOfPlayerInteraction()
    {
        if (playerInteraction == null || !playerInteraction.canInteract)
            return false;

        // Lanzar un raycast desde la posici�n del jugador en la direcci�n que mira
        RaycastHit hit;
        if (Physics.Raycast(playerInteraction.transform.position,
                          playerInteraction.transform.forward,
                          out hit,
                          5f, // Usar un valor similar al rango de interacci�n
                          1 << gameObject.layer))
        {
            // Comprobar si el raycast golpe� este objeto espec�fico
            return hit.collider.gameObject == this.gameObject;
        }
        return false;
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

    // M�todo para actualizar el prompt y redimensionar el indicador expandiendo solo hacia la derecha
    private void UpdateInteractionPrompt(GameObject indicator)
    {
        if (indicator == null) return;

        // Buscar los componentes necesarios de manera m�s robusta
        Transform borderTransform = indicator.transform.Find("Interaction_Prompt_Border");
        TMPro.TMP_Text descriptionText = indicator.transform.Find("Interaction_Description")?.GetComponent<TMPro.TMP_Text>();

        if (descriptionText != null)
        {
            // Asignar el texto (usar itemData.itemName si est� disponible)
            string displayText = !string.IsNullOrEmpty(interactionPrompt) ?
            interactionPrompt :
            (itemData != null ? "Recoger " + itemData.itemName : "Recoger");

            Debug.Log($"Actualizando texto prompt de {gameObject.name} a: '{displayText}'");
            descriptionText.text = displayText;

            // Forzar actualizaci�n del texto
            descriptionText.ForceMeshUpdate(true);

            // Obtener el ancho preferido del texto
            float textWidth = descriptionText.preferredWidth;

            // A�adir padding para el borde
            float padding = 30f;
            float borderWidth = textWidth + padding;

            // Ajustar el ancho del borde
            if (borderTransform != null)
            {
                RectTransform borderRect = borderTransform as RectTransform;
                if (borderRect != null)
                {
                    // Asegurar que el pivote est� a la izquierda (para crecer hacia la derecha)
                    borderRect.pivot = new Vector2(0f, 0.5f);

                    // Mantener la altura actual, cambiar solo el ancho
                    Vector2 sizeDelta = borderRect.sizeDelta;
                    sizeDelta.x = Mathf.Max(100f, borderWidth); // M�nimo 100 unidades de ancho
                    borderRect.sizeDelta = sizeDelta;

                    // Si la posici�n del borde se basa en un anclaje que no es izquierdo,
                    // ajustar la posici�n para mantenerla consistente
                    if (borderRect.anchorMin.x != 0f || borderRect.anchorMax.x != 0f)
                    {
                        // Establecer anclajes a la izquierda
                        borderRect.anchorMin = new Vector2(0f, borderRect.anchorMin.y);
                        borderRect.anchorMax = new Vector2(0f, borderRect.anchorMax.y);

                        // Asegurar que la posici�n sea correcta
                        Vector2 anchoredPosition = borderRect.anchoredPosition;
                        anchoredPosition.x = 0f; // Alinear con el borde izquierdo
                        borderRect.anchoredPosition = anchoredPosition;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"No se pudo encontrar 'Interaction_Description' en {indicator.name}");
            }

            // Ajustar el rectTransform del texto
            RectTransform textRect = descriptionText.rectTransform;
            if (textRect != null)
            {
                // Asegurar que el pivote del texto tambi�n est� a la izquierda
                textRect.pivot = new Vector2(0f, 0.5f);

                // Ajustar el ancho del texto (dejando un poco de margen interno)
                Vector2 textSizeDelta = textRect.sizeDelta;
                textSizeDelta.x = Mathf.Max(80f, textWidth + 10f);
                textRect.sizeDelta = textSizeDelta;

                // Si la posici�n del texto depende de un anclaje no izquierdo, ajustarla
                if (textRect.anchorMin.x != 0f || textRect.anchorMax.x != 0f)
                {
                    // Establecer anclajes a la izquierda
                    textRect.anchorMin = new Vector2(0f, textRect.anchorMin.y);
                    textRect.anchorMax = new Vector2(0f, textRect.anchorMax.y);

                    // Asegurar la posici�n correcta
                    Vector2 textPosition = textRect.anchoredPosition;
                    textPosition.x = 10f; // Un peque�o margen desde el borde izquierdo
                    textRect.anchoredPosition = textPosition;
                }
            }
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

    /// <summary>
    /// M�todo para procesar la interacci�n con este objeto
    /// </summary>
    public void OnInteract()
    {
        if (itemData == null)
        {
            Debug.LogError("No hay ItemData asignado a este Inventory_Item: " + gameObject.name);
            return;
        }

        // Invocar el evento de interacci�n
        OnItemInteracted?.Invoke();

        // A�adir el �tem al inventario con su prefab de interacci�n
        // Usamos la opci�n suppressPopup=true para evitar que se muestre el popup ahora
        if (Inventory_Manager.Instance != null)
        {
            if (interactionPrefab != null)
            {
                Inventory_Manager.Instance.AddItem(itemData, interactionPrefab, onItemClick, true);

                // Mostrar el prefab de interacci�n y configurarlo para que al cerrarlo muestre el popup
                Inventory_Manager.Instance.ShowInteractionForNewItem(interactionPrefab, itemData.itemName);
            }
            else
            {
                // Mantener compatibilidad con el sistema anterior
                Inventory_Manager.Instance.AddItem(itemData, onItemClick);
                Inventory_Manager.Instance.DisplayPopUp(itemData.itemName);
            }
        }
        else
        {
            Debug.LogError("No se encontr� instancia de Inventory_Manager");
        }

        // Destruir el objeto del mundo tras recogerlo
        Destroy(gameObject);
    }

    private void CleanupIndicators()
    {
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