using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Versión mejorada del componente Inventory_Item que utiliza prefabs para las interacciones
/// </summary>
public class Inventory_Item : MonoBehaviour
{
    [Header("Datos del Item")]
    public ItemData itemData;

    [Header("Configuración de Interacción")]
    [Tooltip("Prefab que se instanciará cuando se interactúe con este objeto o se seleccione en el inventario")]
    public GameObject interactionPrefab;

    [Tooltip("Función que se ejecuta al pulsar el objeto en el inventario")]
    public UnityEvent onItemClick;

    [Tooltip("Función que se ejecuta al interactuar con el objeto en el mundo")]
    public UnityEvent OnItemInteracted;

    [Header("Configuración de Feedback")]
    public float edgeOffset = 50f; // Distancia desde el borde de la pantalla

    // Colliders para detección
    [Header("Colliders")]
    public SphereCollider detectionCollider; // Collider para mostrar feedback inicial

    // Referencias de instancias para este item específico
    private Canvas hudCanvas;
    private GameObject rangeIndicator; // Instancia del indicador de rango
    private GameObject interactIndicator; // Instancia del indicador de interacción

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
            detectionCollider.radius = 3.0f; // Radio para detección
            detectionCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        // Buscar referencias automáticamente
        playerInteraction = FindAnyObjectByType<PlayerInteraction>();
        mainCamera = Camera.main;

        // Obtener el Canvas HUD a través del gestor
        hudCanvas = UIFeedbackManager.Instance.GetHUDCanvas();
        if (hudCanvas != null)
        {
            canvasRectTransform = hudCanvas.GetComponent<RectTransform>();
        }

        // Crear los indicadores para este ítem específico
        CreateFeedbackIndicators();

        // Verificación final
        if (playerInteraction == null)
        {
            Debug.LogWarning("No se pudo encontrar PlayerInteraction en la escena");
        }

        isInitialized = true;
    }

    private void CreateFeedbackIndicators()
    {
        // Obtener el nombre único para este objeto
        string objectIdentifier = gameObject.name + "_" + GetInstanceID();

        // Crear indicadores a través del gestor
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

        // Comprobar que todo está correcto
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

        // Actualizar la lógica de visualización basada en la posición del jugador
        UpdateFeedbackLogic();
    }

    private void UpdateFeedbackLogic()
    {
        if (playerInDetectionRange)
        {
            // Actualizar la posición de los indicadores en pantalla
            UpdateIndicatorPosition();

            // Verificar si el jugador está mirando este objeto específico y puede interactuar
            bool canInteractWithThis = IsTargetOfPlayerInteraction();

            // Mostrar el indicador de interacción solo si se puede interactuar
            interactIndicator.SetActive(canInteractWithThis);

            // Mostrar el indicador de detección solo si no se puede interactuar aún
            rangeIndicator.SetActive(!canInteractWithThis);
        }
        else
        {
            // El jugador no está en rango de detección
            rangeIndicator.SetActive(false);
            interactIndicator.SetActive(false);
        }
    }

    // Método para verificar si este objeto es el objetivo actual de la interacción del jugador
    private bool IsTargetOfPlayerInteraction()
    {
        if (playerInteraction == null || !playerInteraction.canInteract)
            return false;

        // Lanzar un raycast desde la posición del jugador en la dirección que mira
        RaycastHit hit;
        if (Physics.Raycast(playerInteraction.transform.position,
                          playerInteraction.transform.forward,
                          out hit,
                          5f, // Usar un valor similar al rango de interacción
                          1 << gameObject.layer))
        {
            // Comprobar si el raycast golpeó este objeto específico
            return hit.collider.gameObject == this.gameObject;
        }
        return false;
    }

    private void UpdateIndicatorPosition()
    {
        // Verificar que tengamos todas las referencias necesarias
        if (mainCamera == null || rangeIndicatorRect == null || interactIndicatorRect == null)
        {
            Debug.LogWarning("Faltan referencias para actualizar la posición de los indicadores");
            return;
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
    /// Método para procesar la interacción con este objeto
    /// </summary>
    public void OnInteract()
    {
        if (itemData == null)
        {
            Debug.LogError("No hay ItemData asignado a este Inventory_Item: " + gameObject.name);
            return;
        }

        // Invocar el evento de interacción
        OnItemInteracted?.Invoke();

        // Verificar si tenemos un prefab para instanciar
        if (interactionPrefab != null)
        {
            // Añadir el ítem al inventario con su prefab de interacción
            Inventory_Manager.Instance.AddItem(itemData, interactionPrefab, onItemClick);
        }
        else
        {
            // Mantener compatibilidad con el sistema anterior
            Inventory_Manager.Instance.AddItem(itemData, onItemClick);
        }

        // Mostrar popup directamente si no hay prefab de interacción
        if (interactionPrefab == null)
        {
            Inventory_Manager.Instance.DisplayPopUp(itemData.itemName);
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