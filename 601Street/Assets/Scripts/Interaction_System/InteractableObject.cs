using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public interface IInteractable
{
    void Interact();
    void SecondInteraction();
    string GetInteractionID();
    bool CanBeInteractedAgain();
    string GetInteractionPrompt();
}

public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Configuración básica")]
    [SerializeField] private string interactionID;
    [Tooltip("Texto que se mostrará en el indicador de interacción")]
    [SerializeField] private string interactionPrompt = "Presiona E para interactuar";
    [Tooltip("Evento que se disparará cuando el jugador interactúe con este objeto")]
    public UnityEvent onInteraction;
    [SerializeField] private UnityEvent onInteracted;

    [Header("Comportamiento de interacción")]
    [Tooltip("Si está activado, este objeto solo podrá ser interactuado una vez")]
    [SerializeField] private bool singleUseInteraction = false;
    [Tooltip("Si está activado, el objeto se desactivará después de una interacción (solo aplica si singleUseInteraction = true)")]
    [SerializeField] private bool disableAfterInteraction = false;

    [Header("Detección de Rango")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private string detectionTriggerName = "DetectionTrigger";

    [Header("Configuración de Indicadores")]
    [SerializeField] private float edgeOffset = 50f;

    // Referencias a los indicadores
    private GameObject rangeIndicator;
    private GameObject interactIndicator;

    // Estado del objeto
    private bool playerOnRange = false;
    private bool isInitialized = false;
    public bool objectInteracted = false;

    // GameObject hijo para el collider de detección
    private GameObject detectionTriggerObject;
    private SphereCollider detectionCollider;
    private DetectionTriggerHandler triggerHandler;

    // Referencias del sistema
    private PlayerInteraction playerInteraction;
    private Camera mainCamera;
    private RectTransform rangeIndicatorRect;
    private RectTransform interactIndicatorRect;
    private Canvas hudCanvas;
    private RectTransform canvasRectTransform;

    // Control de timing
    private float enterTime;
    private bool isExiting = false;
    private Vector3 lastScale;

    // Constantes para mejorar el rendimiento
    private const float UPDATE_DELAY = 0.2f;
    private const float MIN_BORDER_WIDTH = 100f;
    private const float TEXT_PADDING = 30f;
    private const float TEXT_MARGIN = 10f;

    private void Start()
    {
        CreateDetectionTrigger();
        InitializeReferences();
        CreateFeedbackIndicators();
        isInitialized = true;
    }

    /// <summary>
    /// Crea un GameObject hijo con el collider de detección
    /// </summary>
    private void CreateDetectionTrigger()
    {
        // Crear el GameObject hijo para el trigger de detección
        detectionTriggerObject = new GameObject(detectionTriggerName);
        detectionTriggerObject.transform.SetParent(transform);
        detectionTriggerObject.transform.localPosition = Vector3.zero;
        detectionTriggerObject.transform.localRotation = Quaternion.identity;
        detectionTriggerObject.transform.localScale = Vector3.one;

        // Configurar el layer del trigger (usar el mismo layer que el padre)
        detectionTriggerObject.layer = gameObject.layer;

        // Añadir y configurar el SphereCollider
        detectionCollider = detectionTriggerObject.AddComponent<SphereCollider>();
        detectionCollider.radius = detectionRadius;
        detectionCollider.isTrigger = true;

        // Ajustar el radio basado en la escala del objeto padre
        UpdateColliderRadius();

        // Añadir el componente que manejará los eventos del trigger
        triggerHandler = detectionTriggerObject.AddComponent<DetectionTriggerHandler>();
        triggerHandler.Initialize(this);

        Debug.Log($"Trigger de detección creado para {gameObject.name}");
    }

    /// <summary>
    /// Actualiza el radio del collider basado en la escala del objeto
    /// </summary>
    private void UpdateColliderRadius()
    {
        if (detectionCollider == null) return;

        float maxScaleFactor = Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Max(Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z))
        );

        detectionCollider.radius = detectionRadius / maxScaleFactor;
        lastScale = transform.lossyScale;
    }

    /// <summary>
    /// Inicializa las referencias del sistema
    /// </summary>
    private void InitializeReferences()
    {
        mainCamera = Camera.main;

        // Obtener el Canvas HUD
        if (UIFeedbackManager.Instance != null)
        {
            hudCanvas = UIFeedbackManager.Instance.GetHUDCanvas();
            if (hudCanvas != null)
            {
                canvasRectTransform = hudCanvas.GetComponent<RectTransform>();
            }
        }

        // Buscar el PlayerInteraction
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInteraction = player.GetComponent<PlayerInteraction>();
        }
    }

    /// <summary>
    /// Crea los indicadores de feedback visual
    /// </summary>
    private void CreateFeedbackIndicators()
    {
        if (UIFeedbackManager.Instance == null)
        {
            Debug.LogError($"UIFeedbackManager no encontrado para {gameObject.name}");
            enabled = false;
            return;
        }

        string objectIdentifier = gameObject.name + "_" + GetInstanceID();

        // Crear indicadores
        rangeIndicator = UIFeedbackManager.Instance.CreateRangeIndicator(objectIdentifier);
        interactIndicator = UIFeedbackManager.Instance.CreateInteractIndicator(objectIdentifier);

        // Obtener los RectTransform
        rangeIndicatorRect = GetRectTransform(rangeIndicator);
        interactIndicatorRect = GetRectTransform(interactIndicator);

        // Verificar que todo esté correctamente configurado
        if (rangeIndicator == null || interactIndicator == null ||
            rangeIndicatorRect == null || interactIndicatorRect == null)
        {
            Debug.LogError($"No se pudieron crear los indicadores para {gameObject.name}");
            enabled = false;
            return;
        }

        Debug.Log($"Indicadores de feedback creados para {gameObject.name}");
    }

    /// <summary>
    /// Obtiene el RectTransform de un GameObject, incluyendo componentes hijos
    /// </summary>
    private RectTransform GetRectTransform(GameObject obj)
    {
        if (obj == null) return null;

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = obj.GetComponentInChildren<RectTransform>();
        }
        return rect;
    }

    #region Implementación de IInteractable

    public virtual void Interact()
    {
        Debug.Log($"Interactuando con objeto: {gameObject.name} (ID: {interactionID})");
        onInteraction.Invoke();
        objectInteracted = true;

        if (singleUseInteraction)
        {
            DestroyFeedbackIndicators();

            if (disableAfterInteraction)
            {
                StartCoroutine(DisableAfterDelay(0.1f));
            }
        }
    }

    public virtual void SecondInteraction()
    {
        if (!singleUseInteraction)
        {
            Debug.Log($"Segunda interacción con objeto: {gameObject.name} (ID: {interactionID})");
            onInteracted.Invoke();
        }
    }

    public bool CanBeInteractedAgain()
    {
        return !singleUseInteraction || !objectInteracted;
    }

    public string GetInteractionID()
    {
        return interactionID;
    }

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    #endregion

    private void Update()
    {
        // Actualizar radio del collider si la escala cambió
        if (transform.lossyScale != lastScale)
        {
            UpdateColliderRadius();
        }

        // Si es de un solo uso y ya fue interactuado, no hacer nada más
        if (singleUseInteraction && objectInteracted)
        {
            return;
        }

        // Si no está inicializado o el jugador no está en rango, desactivar indicadores
        if (!isInitialized || !playerOnRange)
        {
            DeactivateIndicators();
            return;
        }

        // Actualizar posición de indicadores
        UpdateIndicatorPosition();

        // Verificar si puede mostrar indicador de interacción (con delay para evitar parpadeos)
        if (Time.time - enterTime > UPDATE_DELAY)
        {
            UpdateIndicatorVisibility();
        }
    }

    /// <summary>
    /// Actualiza la visibilidad de los indicadores basado en el estado actual
    /// </summary>
    private void UpdateIndicatorVisibility()
    {
        bool isTargetObject = IsTargetOfPlayerInteraction();

        if (isTargetObject && playerInteraction != null && playerInteraction.canInteract)
        {
            // Mostrar indicador de interacción
            SetIndicatorState(false, true);
        }
        else
        {
            // Mostrar indicador de rango
            SetIndicatorState(true, false);
        }
    }

    /// <summary>
    /// Establece el estado de los indicadores de manera segura
    /// </summary>
    private void SetIndicatorState(bool showRange, bool showInteract)
    {
        if (rangeIndicator != null)
            rangeIndicator.SetActive(showRange);

        if (interactIndicator != null)
        {
            if (showInteract)
            {
                UpdateInteractionPrompt(interactIndicator);
            }
            interactIndicator.SetActive(showInteract);
        }
    }

    /// <summary>
    /// Desactiva todos los indicadores
    /// </summary>
    private void DeactivateIndicators()
    {
        if (rangeIndicator != null) rangeIndicator.SetActive(false);
        if (interactIndicator != null) interactIndicator.SetActive(false);
    }

    /// <summary>
    /// Verifica si este objeto es el objetivo actual del raycast del jugador
    /// </summary>
    private bool IsTargetOfPlayerInteraction()
    {
        if (playerInteraction == null) return false;

        RaycastHit hit;
        Vector3 rayOrigin = playerInteraction.transform.position;
        Vector3 rayDirection = playerInteraction.transform.forward;
        float rayDistance = detectionRadius * 1.5f; // Un poco más que el radio de detección

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance))
        {
            // Verificar si el raycast golpeó este objeto específico o su trigger
            GameObject hitObject = hit.collider.gameObject;
            return hitObject == gameObject || hitObject == detectionTriggerObject;
        }

        return false;
    }

    /// <summary>
    /// Actualiza la posición de los indicadores en pantalla
    /// </summary>
    private void UpdateIndicatorPosition()
    {
        if (mainCamera == null || rangeIndicatorRect == null || interactIndicatorRect == null)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
        bool isInFrontOfCamera = screenPos.z > 0;

        if (isInFrontOfCamera)
        {
            UpdateIndicatorPositionInFront(screenPos);
        }
        else
        {
            UpdateIndicatorPositionBehind();
        }
    }

    /// <summary>
    /// Actualiza la posición cuando el objeto está frente a la cámara
    /// </summary>
    private void UpdateIndicatorPositionInFront(Vector3 screenPos)
    {
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        bool isVisible = screenPos.x >= 0 && screenPos.x <= screenSize.x &&
                        screenPos.y >= 0 && screenPos.y <= screenSize.y;

        if (isVisible)
        {
            // Objeto visible - posicionar directamente sobre él
            SetUIPosition(rangeIndicatorRect, screenPos);
            SetUIPosition(interactIndicatorRect, screenPos);
            ResetIndicatorRotation();
        }
        else
        {
            // Objeto fuera de pantalla - posicionar en el borde
            Vector2 screenCenter = screenSize * 0.5f;
            Vector2 directionToObject = new Vector2(screenPos.x - screenCenter.x, screenPos.y - screenCenter.y).normalized;
            Vector2 edgePosition = CalculateEdgePosition(screenCenter, directionToObject, screenSize);

            SetUIPosition(rangeIndicatorRect, edgePosition);
            SetUIPosition(interactIndicatorRect, edgePosition);
            SetIndicatorRotation(directionToObject);
        }
    }

    /// <summary>
    /// Actualiza la posición cuando el objeto está detrás de la cámara
    /// </summary>
    private void UpdateIndicatorPositionBehind()
    {
        Vector2 edgePosition = new Vector2(Screen.width * 0.5f, edgeOffset);
        SetUIPosition(rangeIndicatorRect, edgePosition);
        SetUIPosition(interactIndicatorRect, edgePosition);

        // Apuntar hacia abajo
        rangeIndicatorRect.rotation = Quaternion.Euler(0, 0, 180);
        interactIndicatorRect.rotation = Quaternion.Euler(0, 0, 180);
    }

    /// <summary>
    /// Calcula la posición en el borde de la pantalla
    /// </summary>
    private Vector2 CalculateEdgePosition(Vector2 screenCenter, Vector2 direction, Vector2 screenSize)
    {
        Vector2 edgePosition = screenCenter + direction *
            Vector2.Distance(Vector2.zero, new Vector2(screenCenter.x - edgeOffset, screenCenter.y - edgeOffset));

        edgePosition.x = Mathf.Clamp(edgePosition.x, edgeOffset, screenSize.x - edgeOffset);
        edgePosition.y = Mathf.Clamp(edgePosition.y, edgeOffset, screenSize.y - edgeOffset);

        return edgePosition;
    }

    /// <summary>
    /// Establece la rotación de los indicadores
    /// </summary>
    private void SetIndicatorRotation(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle - 90);

        rangeIndicatorRect.rotation = rotation;
        interactIndicatorRect.rotation = rotation;
    }

    /// <summary>
    /// Resetea la rotación de los indicadores
    /// </summary>
    private void ResetIndicatorRotation()
    {
        rangeIndicatorRect.rotation = Quaternion.identity;
        interactIndicatorRect.rotation = Quaternion.identity;
    }

    /// <summary>
    /// Actualiza el texto del prompt y ajusta el tamaño del contenedor
    /// </summary>
    private void UpdateInteractionPrompt(GameObject indicator)
    {
        Transform borderTransform = indicator.transform.Find("Interaction_Prompt_Border");
        TMP_Text descriptionText = indicator.transform.Find("Interaction_Description")?.GetComponent<TMP_Text>();

        if (descriptionText != null)
        {
            descriptionText.text = interactionPrompt;
            descriptionText.ForceMeshUpdate();

            float textWidth = descriptionText.preferredWidth;
            float borderWidth = textWidth + TEXT_PADDING;

            UpdateBorderSize(borderTransform, borderWidth);
            UpdateTextSize(descriptionText, textWidth);
        }
    }

    /// <summary>
    /// Actualiza el tamaño del borde del indicador
    /// </summary>
    private void UpdateBorderSize(Transform borderTransform, float borderWidth)
    {
        if (borderTransform == null) return;

        RectTransform borderRect = borderTransform as RectTransform;
        if (borderRect != null)
        {
            borderRect.pivot = new Vector2(0f, 0.5f);

            Vector2 sizeDelta = borderRect.sizeDelta;
            sizeDelta.x = Mathf.Max(MIN_BORDER_WIDTH, borderWidth);
            borderRect.sizeDelta = sizeDelta;

            // Configurar anclajes y posición
            borderRect.anchorMin = new Vector2(0f, borderRect.anchorMin.y);
            borderRect.anchorMax = new Vector2(0f, borderRect.anchorMax.y);

            Vector2 anchoredPosition = borderRect.anchoredPosition;
            anchoredPosition.x = 0f;
            borderRect.anchoredPosition = anchoredPosition;
        }
    }

    /// <summary>
    /// Actualiza el tamaño del texto
    /// </summary>
    private void UpdateTextSize(TMP_Text descriptionText, float textWidth)
    {
        RectTransform textRect = descriptionText.rectTransform;
        if (textRect != null)
        {
            textRect.pivot = new Vector2(0f, 0.5f);

            Vector2 textSizeDelta = textRect.sizeDelta;
            textSizeDelta.x = Mathf.Max(80f, textWidth + TEXT_MARGIN);
            textRect.sizeDelta = textSizeDelta;

            // Configurar anclajes y posición
            textRect.anchorMin = new Vector2(0f, textRect.anchorMin.y);
            textRect.anchorMax = new Vector2(0f, textRect.anchorMax.y);

            Vector2 textPosition = textRect.anchoredPosition;
            textPosition.x = TEXT_MARGIN;
            textRect.anchoredPosition = textPosition;
        }
    }

    /// <summary>
    /// Establece la posición de un elemento UI
    /// </summary>
    private void SetUIPosition(RectTransform rectTransform, Vector2 screenPosition)
    {
        if (rectTransform == null || hudCanvas == null) return;

        switch (hudCanvas.renderMode)
        {
            case RenderMode.ScreenSpaceOverlay:
                rectTransform.position = new Vector3(screenPosition.x, screenPosition.y, 0);
                break;

            case RenderMode.ScreenSpaceCamera:
                SetUIPositionScreenSpaceCamera(rectTransform, screenPosition);
                break;

            case RenderMode.WorldSpace:
                SetUIPositionWorldSpace(rectTransform, screenPosition);
                break;
        }
    }

    private void SetUIPositionScreenSpaceCamera(RectTransform rectTransform, Vector2 screenPosition)
    {
        Vector2 viewportPosition = new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height);
        Camera uiCamera = hudCanvas.worldCamera ?? Camera.main;

        if (uiCamera != null)
        {
            Vector3 worldPos = uiCamera.ViewportToWorldPoint(
                new Vector3(viewportPosition.x, viewportPosition.y, hudCanvas.planeDistance));
            rectTransform.position = worldPos;
        }
    }

    private void SetUIPositionWorldSpace(RectTransform rectTransform, Vector2 screenPosition)
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

    #region Métodos públicos para el trigger handler

    /// <summary>
    /// Llamado cuando el jugador entra en el rango de detección
    /// </summary>
    public void OnPlayerEnterRange(Collider playerCollider)
    {
        if (singleUseInteraction && objectInteracted) return;

        enterTime = Time.time;
        playerOnRange = true;
        playerInteraction = playerCollider.GetComponent<PlayerInteraction>();

        Debug.Log($"Jugador en rango de detección de {gameObject.name}");

        // Desactivar indicadores inmediatamente para evitar parpadeos
        DeactivateIndicators();

        // Activar indicadores con delay
        StartCoroutine(DelayedIndicatorActivation());
    }

    /// <summary>
    /// Llamado cuando el jugador sale del rango de detección
    /// </summary>
    public void OnPlayerExitRange()
    {
        isExiting = true;

        // Desactivar indicadores inmediatamente
        DeactivateIndicators();

        playerOnRange = false;
        playerInteraction = null;

        Debug.Log($"Jugador fuera de rango de detección de {gameObject.name}");

        // Cancelar corrutinas pendientes
        StopAllCoroutines();

        // Limpiar después de salir
        StartCoroutine(CleanupAfterExit());
    }

    #endregion

    /// <summary>
    /// Activa los indicadores con un pequeño delay para evitar parpadeos
    /// </summary>
    private IEnumerator DelayedIndicatorActivation()
    {
        yield return new WaitForSeconds(0.1f);

        if (playerOnRange && (!singleUseInteraction || !objectInteracted))
        {
            UpdateIndicatorVisibility();
        }
    }

    /// <summary>
    /// Limpia el estado después de que el jugador salga del rango
    /// </summary>
    private IEnumerator CleanupAfterExit()
    {
        yield return null;
        yield return null;

        // Asegurar que los indicadores estén desactivados
        DeactivateIndicators();

        yield return new WaitForSeconds(0.1f);

        DeactivateIndicators();
        isExiting = false;
    }

    /// <summary>
    /// Desactiva el objeto después de un delay
    /// </summary>
    private IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Destruye los indicadores de feedback
    /// </summary>
    private void DestroyFeedbackIndicators()
    {
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

        // Desactivar el collider de detección
        if (detectionCollider != null)
        {
            detectionCollider.enabled = false;
        }
    }

    #region Unity Events

    private void OnDisable()
    {
        DeactivateIndicators();
    }

    private void OnDestroy()
    {
        if (rangeIndicator != null) Destroy(rangeIndicator);
        if (interactIndicator != null) Destroy(interactIndicator);
        if (detectionTriggerObject != null) Destroy(detectionTriggerObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    #endregion
}

/// <summary>
/// Componente auxiliar para manejar los eventos del trigger de detección
/// </summary>
public class DetectionTriggerHandler : MonoBehaviour
{
    private InteractableObject parentInteractable;

    public void Initialize(InteractableObject parent)
    {
        parentInteractable = parent;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && parentInteractable != null)
        {
            parentInteractable.OnPlayerEnterRange(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && parentInteractable != null)
        {
            parentInteractable.OnPlayerExitRange();
        }
    }
}