using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Versión mejorada del Inventory_Item con limpieza garantizada de indicadores
/// </summary>
public class Inventory_Item : MonoBehaviour
{
    [Header("Datos del Item")]
    public ItemData itemData;

    [Header("Configuración de Interacción")]
    [SerializeField] private string interactionPrompt = "Recoger";
    public GameObject interactionPrefab;
    public UnityEvent onItemClick;
    public UnityEvent OnItemInteracted;

    [Header("Configuración de Feedback")]
    public float edgeOffset = 50f;

    [Header("Colliders")]
    public SphereCollider detectionCollider;

    // NUEVO: Variables para rastreo de limpieza
    private int instanceID;
    private bool indicatorsCreated = false;

    // Referencias de instancias para este item específico
    private Canvas hudCanvas;
    private GameObject rangeIndicator;
    private GameObject interactIndicator;

    private PlayerInteraction playerInteraction;
    private bool playerInDetectionRange = false;
    private RectTransform rangeIndicatorRect;
    private RectTransform interactIndicatorRect;
    private Camera mainCamera;
    private RectTransform canvasRectTransform;
    private bool isInitialized = false;

    private void Awake()
    {
        // NUEVO: Obtener y almacenar el ID de instancia
        instanceID = GetInstanceID();

        // Crear collider si no existe
        if (detectionCollider == null)
        {
            detectionCollider = gameObject.AddComponent<SphereCollider>();
            detectionCollider.radius = 3.0f;
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

        // MEJORADO: Crear los indicadores con registro
        CreateFeedbackIndicators();

        // Verificación final
        if (playerInteraction == null)
        {
            Debug.LogWarning("No se pudo encontrar PlayerInteraction en la escena");
        }

        if (interactIndicator != null)
        {
            UpdateInteractionPrompt(interactIndicator);
            Debug.Log($"Inicialización de prompt para {gameObject.name}: '{interactionPrompt}'");
        }

        isInitialized = true;
    }

    /// <summary>
    /// MEJORADO: Crear indicadores con registro automático
    /// </summary>
    private void CreateFeedbackIndicators()
    {
        if (UIFeedbackManager.Instance == null)
        {
            Debug.LogError($"UIFeedbackManager no encontrado para {gameObject.name}");
            enabled = false;
            return;
        }

        // Obtener el nombre único para este objeto
        string objectIdentifier = gameObject.name + "_" + instanceID;

        // MEJORADO: Crear indicadores a través del gestor con registro y referencia del componente
        rangeIndicator = UIFeedbackManager.Instance.CreateRangeIndicator(objectIdentifier, instanceID, this);
        interactIndicator = UIFeedbackManager.Instance.CreateInteractIndicator(objectIdentifier, instanceID, this);

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

        indicatorsCreated = true;
        Debug.Log($"Indicadores de feedback creados para {gameObject.name} (ID: {instanceID})");
    }

    private void Update()
    {
        // Verificar si el objeto sigue existiendo
        if (this == null || !gameObject.activeInHierarchy || !isInitialized)
        {
            HideFeedbackIndicators();
            return;
        }

        // Comprobaciones de seguridad
        if (rangeIndicator == null || interactIndicator == null || !enabled)
        {
            HideFeedbackIndicators();
            return;
        }

        // Actualizar la lógica de visualización basada en la posición del jugador
        UpdateFeedbackLogic();
    }

    private void UpdateFeedbackLogic()
    {
        // NUEVO: Verificar primero si este componente está habilitado
        if (!enabled || !gameObject.activeInHierarchy)
        {
            if (rangeIndicator != null) rangeIndicator.SetActive(false);
            if (interactIndicator != null) interactIndicator.SetActive(false);
            return;
        }

        // NUEVO: Verificar también a través del manager
        if (UIFeedbackManager.Instance != null && !UIFeedbackManager.Instance.IsComponentValid(instanceID))
        {
            if (rangeIndicator != null) rangeIndicator.SetActive(false);
            if (interactIndicator != null) interactIndicator.SetActive(false);
            return;
        }

        if (playerInDetectionRange)
        {
            // Actualizar la posición de los indicadores en pantalla
            UpdateIndicatorPosition();

            // Verificar si el jugador está mirando este objeto específico y puede interactuar
            bool canInteractWithThis = IsTargetOfPlayerInteraction();

            // Actualizar el texto del prompt
            if (interactIndicator != null)
            {
                UpdateInteractionPrompt(interactIndicator);
            }

            // Mostrar el indicador de interacción solo si se puede interactuar
            if (canInteractWithThis && interactIndicator != null)
            {
                interactIndicator.SetActive(true);
            }
            else if (interactIndicator != null)
            {
                interactIndicator.SetActive(false);
            }

            // Mostrar el indicador de detección solo si no se puede interactuar aún
            if (rangeIndicator != null)
            {
                rangeIndicator.SetActive(!canInteractWithThis);
            }
        }
        else
        {
            // El jugador no está en rango de detección
            if (rangeIndicator != null) rangeIndicator.SetActive(false);
            if (interactIndicator != null) interactIndicator.SetActive(false);
        }
    }

    private bool IsTargetOfPlayerInteraction()
    {
        if (playerInteraction == null || !playerInteraction.canInteract)
            return false;

        RaycastHit hit;
        if (Physics.Raycast(playerInteraction.transform.position,
                          playerInteraction.transform.forward,
                          out hit,
                          5f,
                          1 << gameObject.layer))
        {
            return hit.collider.gameObject == this.gameObject;
        }
        return false;
    }

    private void UpdateIndicatorPosition()
    {
        if (mainCamera == null || rangeIndicatorRect == null || interactIndicatorRect == null)
        {
            Debug.LogWarning("Faltan referencias para actualizar la posición de los indicadores");
            return;
        }

        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
        bool isInFrontOfCamera = screenPos.z > 0;

        if (isInFrontOfCamera)
        {
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Vector2 screenCenter = screenSize * 0.5f;

            bool isVisible = screenPos.x >= 0 && screenPos.x <= screenSize.x &&
                            screenPos.y >= 0 && screenPos.y <= screenSize.y;

            if (isVisible)
            {
                SetUIPosition(rangeIndicatorRect, screenPos);
                SetUIPosition(interactIndicatorRect, screenPos);

                rangeIndicatorRect.rotation = Quaternion.identity;
                interactIndicatorRect.rotation = Quaternion.identity;
            }
            else
            {
                Vector2 directionToObject = new Vector2(screenPos.x - screenCenter.x, screenPos.y - screenCenter.y).normalized;
                Vector2 edgePosition = screenCenter + directionToObject *
                    (Vector2.Distance(Vector2.zero, new Vector2(screenCenter.x - edgeOffset, screenCenter.y - edgeOffset)));

                edgePosition.x = Mathf.Clamp(edgePosition.x, edgeOffset, screenSize.x - edgeOffset);
                edgePosition.y = Mathf.Clamp(edgePosition.y, edgeOffset, screenSize.y - edgeOffset);

                SetUIPosition(rangeIndicatorRect, edgePosition);
                SetUIPosition(interactIndicatorRect, edgePosition);

                float angle = Mathf.Atan2(directionToObject.y, directionToObject.x) * Mathf.Rad2Deg;
                rangeIndicatorRect.rotation = Quaternion.Euler(0, 0, angle - 90);
                interactIndicatorRect.rotation = Quaternion.Euler(0, 0, angle - 90);
            }
        }
        else
        {
            Vector2 edgePosition = new Vector2(Screen.width * 0.5f, edgeOffset);
            SetUIPosition(rangeIndicatorRect, edgePosition);
            SetUIPosition(interactIndicatorRect, edgePosition);

            rangeIndicatorRect.rotation = Quaternion.Euler(0, 0, 180);
            interactIndicatorRect.rotation = Quaternion.Euler(0, 0, 180);
        }
    }

    private void UpdateInteractionPrompt(GameObject indicator)
    {
        if (indicator == null) return;

        Transform borderTransform = indicator.transform.Find("Interaction_Prompt_Border");
        TMPro.TMP_Text descriptionText = indicator.transform.Find("Interaction_Description")?.GetComponent<TMPro.TMP_Text>();

        if (descriptionText != null)
        {
            string displayText = !string.IsNullOrEmpty(interactionPrompt) ?
            interactionPrompt :
            (itemData != null ? "Recoger " + itemData.itemName : "Recoger");

            Debug.Log($"Actualizando texto prompt de {gameObject.name} a: '{displayText}'");
            descriptionText.text = displayText;
            descriptionText.ForceMeshUpdate(true);

            float textWidth = descriptionText.preferredWidth;
            float padding = 30f;
            float borderWidth = textWidth + padding;

            if (borderTransform != null)
            {
                RectTransform borderRect = borderTransform as RectTransform;
                if (borderRect != null)
                {
                    borderRect.pivot = new Vector2(0f, 0.5f);

                    Vector2 sizeDelta = borderRect.sizeDelta;
                    sizeDelta.x = Mathf.Max(100f, borderWidth);
                    borderRect.sizeDelta = sizeDelta;

                    if (borderRect.anchorMin.x != 0f || borderRect.anchorMax.x != 0f)
                    {
                        borderRect.anchorMin = new Vector2(0f, borderRect.anchorMin.y);
                        borderRect.anchorMax = new Vector2(0f, borderRect.anchorMax.y);

                        Vector2 anchoredPosition = borderRect.anchoredPosition;
                        anchoredPosition.x = 0f;
                        borderRect.anchoredPosition = anchoredPosition;
                    }
                }
            }

            RectTransform textRect = descriptionText.rectTransform;
            if (textRect != null)
            {
                textRect.pivot = new Vector2(0f, 0.5f);

                Vector2 textSizeDelta = textRect.sizeDelta;
                textSizeDelta.x = Mathf.Max(80f, textWidth + 10f);
                textRect.sizeDelta = textSizeDelta;

                if (textRect.anchorMin.x != 0f || textRect.anchorMax.x != 0f)
                {
                    textRect.anchorMin = new Vector2(0f, textRect.anchorMin.y);
                    textRect.anchorMax = new Vector2(0f, textRect.anchorMax.y);

                    Vector2 textPosition = textRect.anchoredPosition;
                    textPosition.x = 10f;
                    textRect.anchoredPosition = textPosition;
                }
            }
        }
    }

    private void SetUIPosition(RectTransform rectTransform, Vector2 screenPosition)
    {
        if (hudCanvas == null || rectTransform == null)
            return;

        if (hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rectTransform.position = new Vector3(screenPosition.x, screenPosition.y, 0);
        }
        else if (hudCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Vector2 viewportPosition = new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height);

            Vector3 worldPos = hudCanvas.worldCamera != null ?
                hudCanvas.worldCamera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, hudCanvas.planeDistance)) :
                Camera.main.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, 10));

            rectTransform.position = worldPos;
        }
        else // RenderMode.WorldSpace
        {
            if (canvasRectTransform != null)
            {
                Vector2 canvasSize = canvasRectTransform.sizeDelta;

                Vector2 normalizedPos = new Vector2(
                    screenPosition.x / Screen.width,
                    screenPosition.y / Screen.height
                );

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

        // Añadir el ítem al inventario
        if (Inventory_Manager.Instance != null)
        {
            if (interactionPrefab != null)
            {
                Inventory_Manager.Instance.AddItem(itemData, interactionPrefab, onItemClick, true);
                Inventory_Manager.Instance.ShowInteractionForNewItem(interactionPrefab, itemData.itemName);
            }
            else
            {
                Inventory_Manager.Instance.AddItem(itemData, onItemClick);
                Inventory_Manager.Instance.DisplayPopUp(itemData.itemName);
            }
        }
        else
        {
            Debug.LogError("No se encontró instancia de Inventory_Manager");
        }

        // CORREGIDO: Destruir indicadores antes de destruir el objeto
        DestroyFeedbackIndicators();

        // Destruir el objeto del mundo tras recogerlo
        Destroy(gameObject);
    }

    /// <summary>
    /// NUEVO: Destruir indicadores (solo cuando el objeto se destruye)
    /// </summary>
    private void DestroyFeedbackIndicators()
    {
        // Notificar al manager para destrucción inmediata
        if (UIFeedbackManager.Instance != null && indicatorsCreated)
        {
            UIFeedbackManager.Instance.DestroyIndicatorsForObject(instanceID);
        }

        // Limpieza local adicional por seguridad
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

        indicatorsCreated = false;
    }

    /// <summary>
    /// NUEVO: Ocultar indicadores (cuando el objeto se desactiva)
    /// </summary>
    private void HideFeedbackIndicators()
    {
        // Solo ocultar a través del manager
        if (UIFeedbackManager.Instance != null && indicatorsCreated)
        {
            UIFeedbackManager.Instance.HideIndicatorsForObject(instanceID);
        }

        // Ocultar localmente también
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(false);
        }

        if (interactIndicator != null)
        {
            interactIndicator.SetActive(false);
        }
    }

    /// <summary>
    /// CORREGIDO: OnDisable solo oculta indicadores, no los destruye
    /// </summary>
    private void OnDisable()
    {
        // Solo ocultar indicadores cuando se desactiva
        HideFeedbackIndicators();
    }

    /// <summary>
    /// CORREGIDO: OnDestroy ahora sí destruye los indicadores completamente
    /// </summary>
    private void OnDestroy()
    {
        // DESTRUIR indicadores completamente porque el objeto se está destruyendo
        if (UIFeedbackManager.Instance != null && indicatorsCreated)
        {
            UIFeedbackManager.Instance.DestroyIndicatorsForObject(instanceID);
        }

        // Limpieza manual como respaldo final
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(false);
            Destroy(rangeIndicator);
        }
        if (interactIndicator != null)
        {
            interactIndicator.SetActive(false);
            Destroy(interactIndicator);
        }

        Debug.Log($"Inventory_Item {gameObject.name} (ID: {instanceID}) destruido con limpieza completa");
    }

    /// <summary>
    /// NUEVO: OnEnable para mostrar indicadores cuando se reactiva
    /// </summary>
    private void OnEnable()
    {
        // Si el objeto se reactiva y ya estaba inicializado, mostrar indicadores si el jugador está en rango
        if (isInitialized && playerInDetectionRange && indicatorsCreated)
        {
            UpdateFeedbackLogic();
        }
    }
}