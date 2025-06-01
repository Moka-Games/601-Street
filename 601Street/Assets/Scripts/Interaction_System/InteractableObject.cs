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
    [SerializeField] private string interactionPrompt = "Presiona E para interactuar";
    public UnityEvent onInteraction;
    [SerializeField] private UnityEvent onInteracted;

    [Header("Comportamiento de interacción")]
    [SerializeField] private bool singleUseInteraction = false;
    [SerializeField] private bool disableAfterInteraction = false;

    [Header("Configuración de Audio")]
    [SerializeField] private AudioClip interactionSound;
    [SerializeField] private AudioClip secondInteractionSound;
    [SerializeField] private float soundVolume = 1f;
    [SerializeField] private float soundPitch = 1f;
    [SerializeField] private bool usePositionalAudio = true;
    [SerializeField] private float audioMaxDistance = 30f;

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

    // NUEVO: Variable para rastrear el ID de instancia
    private int instanceID;
    private bool indicatorsCreated = false;

    // Referencias del sistema
    private GameObject detectionTriggerObject;
    private SphereCollider detectionCollider;
    private DetectionTriggerHandler triggerHandler;
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

    private const float UPDATE_DELAY = 0.2f;
    private const float MIN_BORDER_WIDTH = 100f;
    private const float TEXT_PADDING = 30f;
    private const float TEXT_MARGIN = 10f;

    // NUEVO: Evento estático para notificar destrucción
    public static System.Action<int> OnObjectDestroyed;

    private AudioPlaybackManager audioPlaybackManager;
    private void Awake()
    {
        // NUEVO: Obtener y almacenar el ID de instancia
        instanceID = GetInstanceID();
    }

    private void Start()
    {
        CreateDetectionTrigger();
        InitializeReferences();
        CreateFeedbackIndicators();
        isInitialized = true;
    }

    private void CreateDetectionTrigger()
    {
        detectionTriggerObject = new GameObject(detectionTriggerName);
        detectionTriggerObject.transform.SetParent(transform);
        detectionTriggerObject.transform.localPosition = Vector3.zero;
        detectionTriggerObject.transform.localRotation = Quaternion.identity;
        detectionTriggerObject.transform.localScale = Vector3.one;
        detectionTriggerObject.layer = gameObject.layer;

        detectionCollider = detectionTriggerObject.AddComponent<SphereCollider>();
        detectionCollider.radius = detectionRadius;
        detectionCollider.isTrigger = true;

        UpdateColliderRadius();

        triggerHandler = detectionTriggerObject.AddComponent<DetectionTriggerHandler>();
        triggerHandler.Initialize(this);

        Debug.Log($"Trigger de detección creado para {gameObject.name}");
    }

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

    private void InitializeReferences()
    {
        mainCamera = Camera.main;

        if (UIFeedbackManager.Instance != null)
        {
            hudCanvas = UIFeedbackManager.Instance.GetHUDCanvas();
            if (hudCanvas != null)
            {
                canvasRectTransform = hudCanvas.GetComponent<RectTransform>();
            }
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInteraction = player.GetComponent<PlayerInteraction>();
        }
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

        string objectIdentifier = gameObject.name + "_" + instanceID;

        // MEJORADO: Crear indicadores pasando el instanceID y la referencia del componente
        rangeIndicator = UIFeedbackManager.Instance.CreateRangeIndicator(objectIdentifier, instanceID, this);
        interactIndicator = UIFeedbackManager.Instance.CreateInteractIndicator(objectIdentifier, instanceID, this);

        rangeIndicatorRect = GetRectTransform(rangeIndicator);
        interactIndicatorRect = GetRectTransform(interactIndicator);

        if (rangeIndicator == null || interactIndicator == null ||
            rangeIndicatorRect == null || interactIndicatorRect == null)
        {
            Debug.LogError($"No se pudieron crear los indicadores para {gameObject.name}");
            enabled = false;
            return;
        }

        indicatorsCreated = true;
        Debug.Log($"Indicadores de feedback creados para {gameObject.name} (ID: {instanceID})");
    }

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

        // NUEVO: Reproducir sonido de interacción
        PlayInteractionAudio(interactionSound);

        onInteraction.Invoke();
        objectInteracted = true;

        if (singleUseInteraction)
        {
            HideFeedbackIndicators();

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

            // NUEVO: Reproducir sonido de segunda interacción
            PlayInteractionAudio(secondInteractionSound);

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

    #region Sistema de Audio

    /// <summary>
    /// Reproduce el sonido de interacción usando el AudioPlaybackManager
    /// </summary>
    /// <param name="clip">Clip de audio a reproducir</param>
    private void PlayInteractionAudio(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.Log($"No hay clip de audio asignado para la interacción con {gameObject.name}");
            return;
        }

        // Verificar que el AudioPlaybackManager esté disponible
        if (AudioPlaybackManager.Instance == null)
        {
            Debug.LogError("AudioPlaybackManager no encontrado. No se puede reproducir el sonido de interacción.");
            return;
        }

        // Reproducir el sonido en la posición del objeto
        if (usePositionalAudio)
        {
            AudioPlaybackManager.Instance.PlaySoundAtPosition(
                clip,
                transform.position,
                soundVolume,
                soundPitch,
                3f, // Auto-destrucción en 3 segundos
                true, // Audio 3D
                audioMaxDistance
            );
        }
        else
        {
            AudioPlaybackManager.Instance.PlaySound2D(
                clip,
                soundVolume,
                soundPitch,
                3f // Auto-destrucción en 3 segundos
            );
        }

        Debug.Log($"Reproduciendo sonido '{clip.name}' para interacción con {gameObject.name}");
    }

    /// <summary>
    /// Método público para reproducir sonidos personalizados desde UnityEvents
    /// </summary>
    /// <param name="customClip">Clip personalizado a reproducir</param>
    public void PlayCustomSound(AudioClip customClip)
    {
        PlayInteractionAudio(customClip);
    }

    /// <summary>
    /// Configura los parámetros de audio en tiempo de ejecución
    /// </summary>
    /// <param name="volume">Volumen (0-1)</param>
    /// <param name="pitch">Pitch del sonido</param>
    /// <param name="maxDistance">Distancia máxima para audio 3D</param>
    public void SetAudioParameters(float volume, float pitch = 1f, float maxDistance = 30f)
    {
        soundVolume = Mathf.Clamp01(volume);
        soundPitch = pitch;
        audioMaxDistance = maxDistance;
    }

    /// <summary>
    /// Configura si el audio debe ser posicional o 2D
    /// </summary>
    /// <param name="positional">True para audio 3D, false para 2D</param>
    public void SetPositionalAudio(bool positional)
    {
        usePositionalAudio = positional;
    }

    #endregion

    private void Update()
    {
        if (transform.lossyScale != lastScale)
        {
            UpdateColliderRadius();
        }

        if (singleUseInteraction && objectInteracted)
        {
            return;
        }

        if (!isInitialized || !playerOnRange)
        {
            DeactivateIndicators();
            return;
        }

        UpdateIndicatorPosition();

        if (Time.time - enterTime > UPDATE_DELAY)
        {
            UpdateIndicatorVisibility();
        }
    }

    private void UpdateIndicatorVisibility()
    {
        // NUEVO: Verificar primero si este componente está habilitado
        if (!enabled || !gameObject.activeInHierarchy)
        {
            DeactivateIndicators();
            return;
        }

        // NUEVO: Verificar también a través del manager
        if (UIFeedbackManager.Instance != null && !UIFeedbackManager.Instance.IsComponentValid(instanceID))
        {
            DeactivateIndicators();
            return;
        }

        bool isTargetObject = IsTargetOfPlayerInteraction();

        if (isTargetObject && playerInteraction != null && playerInteraction.canInteract)
        {
            SetIndicatorState(false, true);
        }
        else
        {
            SetIndicatorState(true, false);
        }
    }

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

    private void DeactivateIndicators()
    {
        if (rangeIndicator != null) rangeIndicator.SetActive(false);
        if (interactIndicator != null) interactIndicator.SetActive(false);
    }

    private bool IsTargetOfPlayerInteraction()
    {
        if (playerInteraction == null) return false;

        RaycastHit hit;
        Vector3 rayOrigin = playerInteraction.transform.position;
        Vector3 rayDirection = playerInteraction.transform.forward;
        float rayDistance = detectionRadius * 1.5f;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance))
        {
            GameObject hitObject = hit.collider.gameObject;
            return hitObject == gameObject || hitObject == detectionTriggerObject;
        }

        return false;
    }

    // [Métodos de actualización de posición e interfaz - mantener como estaban]
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

    private void UpdateIndicatorPositionInFront(Vector3 screenPos)
    {
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        bool isVisible = screenPos.x >= 0 && screenPos.x <= screenSize.x &&
                        screenPos.y >= 0 && screenPos.y <= screenSize.y;

        if (isVisible)
        {
            SetUIPosition(rangeIndicatorRect, screenPos);
            SetUIPosition(interactIndicatorRect, screenPos);
            ResetIndicatorRotation();
        }
        else
        {
            Vector2 screenCenter = screenSize * 0.5f;
            Vector2 directionToObject = new Vector2(screenPos.x - screenCenter.x, screenPos.y - screenCenter.y).normalized;
            Vector2 edgePosition = CalculateEdgePosition(screenCenter, directionToObject, screenSize);

            SetUIPosition(rangeIndicatorRect, edgePosition);
            SetUIPosition(interactIndicatorRect, edgePosition);
            SetIndicatorRotation(directionToObject);
        }
    }

    private void UpdateIndicatorPositionBehind()
    {
        Vector2 edgePosition = new Vector2(Screen.width * 0.5f, edgeOffset);
        SetUIPosition(rangeIndicatorRect, edgePosition);
        SetUIPosition(interactIndicatorRect, edgePosition);

        rangeIndicatorRect.rotation = Quaternion.Euler(0, 0, 180);
        interactIndicatorRect.rotation = Quaternion.Euler(0, 0, 180);
    }

    private Vector2 CalculateEdgePosition(Vector2 screenCenter, Vector2 direction, Vector2 screenSize)
    {
        Vector2 edgePosition = screenCenter + direction *
            Vector2.Distance(Vector2.zero, new Vector2(screenCenter.x - edgeOffset, screenCenter.y - edgeOffset));

        edgePosition.x = Mathf.Clamp(edgePosition.x, edgeOffset, screenSize.x - edgeOffset);
        edgePosition.y = Mathf.Clamp(edgePosition.y, edgeOffset, screenSize.y - edgeOffset);

        return edgePosition;
    }

    private void SetIndicatorRotation(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle - 90);

        rangeIndicatorRect.rotation = rotation;
        interactIndicatorRect.rotation = rotation;
    }

    private void ResetIndicatorRotation()
    {
        rangeIndicatorRect.rotation = Quaternion.identity;
        interactIndicatorRect.rotation = Quaternion.identity;
    }

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

            borderRect.anchorMin = new Vector2(0f, borderRect.anchorMin.y);
            borderRect.anchorMax = new Vector2(0f, borderRect.anchorMax.y);

            Vector2 anchoredPosition = borderRect.anchoredPosition;
            anchoredPosition.x = 0f;
            borderRect.anchoredPosition = anchoredPosition;
        }
    }

    private void UpdateTextSize(TMP_Text descriptionText, float textWidth)
    {
        RectTransform textRect = descriptionText.rectTransform;
        if (textRect != null)
        {
            textRect.pivot = new Vector2(0f, 0.5f);

            Vector2 textSizeDelta = textRect.sizeDelta;
            textSizeDelta.x = Mathf.Max(80f, textWidth + TEXT_MARGIN);
            textRect.sizeDelta = textSizeDelta;

            textRect.anchorMin = new Vector2(0f, textRect.anchorMin.y);
            textRect.anchorMax = new Vector2(0f, textRect.anchorMax.y);

            Vector2 textPosition = textRect.anchoredPosition;
            textPosition.x = TEXT_MARGIN;
            textRect.anchoredPosition = textPosition;
        }
    }

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

    public void OnPlayerEnterRange(Collider playerCollider)
    {
        if (singleUseInteraction && objectInteracted) return;

        enterTime = Time.time;
        playerOnRange = true;
        playerInteraction = playerCollider.GetComponent<PlayerInteraction>();

        Debug.Log($"Jugador en rango de detección de {gameObject.name}");

        DeactivateIndicators();
        StartCoroutine(DelayedIndicatorActivation());
    }

    public void OnPlayerExitRange()
    {
        isExiting = true;
        DeactivateIndicators();
        playerOnRange = false;
        playerInteraction = null;

        Debug.Log($"Jugador fuera de rango de detección de {gameObject.name}");

        StopAllCoroutines();
        StartCoroutine(CleanupAfterExit());
    }

    #endregion

    private IEnumerator DelayedIndicatorActivation()
    {
        yield return new WaitForSeconds(0.1f);

        if (playerOnRange && (!singleUseInteraction || !objectInteracted))
        {
            UpdateIndicatorVisibility();
        }
    }

    private IEnumerator CleanupAfterExit()
    {
        yield return null;
        yield return null;

        DeactivateIndicators();

        yield return new WaitForSeconds(0.1f);

        DeactivateIndicators();
        isExiting = false;
    }

    private IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// CORREGIDO: Método para destruir indicadores (solo cuando el objeto se destruye)
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

        // Desactivar el collider de detección
        if (detectionCollider != null)
        {
            detectionCollider.enabled = false;
        }

        indicatorsCreated = false;
    }

    /// <summary>
    /// NUEVO: Método para ocultar indicadores (cuando el objeto se desactiva)
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

    #region Unity Events

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
        // Notificar destrucción vía evento estático
        OnObjectDestroyed?.Invoke(instanceID);

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
        if (detectionTriggerObject != null)
        {
            Destroy(detectionTriggerObject);
        }

        Debug.Log($"InteractableObject {gameObject.name} (ID: {instanceID}) destruido con limpieza completa");
    }

    /// <summary>
    /// NUEVO: OnEnable para mostrar indicadores cuando se reactiva
    /// </summary>
    private void OnEnable()
    {
        // Si el objeto se reactiva y ya estaba inicializado, mostrar indicadores si el jugador está en rango
        if (isInitialized && playerOnRange && indicatorsCreated)
        {
            UpdateIndicatorVisibility();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Test Interaction Sound")]
    public void TestInteractionSoundFromContext()
    {
        if (Application.isPlaying && interactionSound != null)
        {
            Debug.Log($"Probando sonido de interacción: {interactionSound.name}");
            PlayInteractionAudio(interactionSound);
        }
        else if (interactionSound == null)
        {
            Debug.LogWarning("No hay clip de audio asignado para probar");
        }
        else
        {
            Debug.LogWarning("El test solo funciona en modo Play");
        }
    }

    [ContextMenu("Test Second Interaction Sound")]
    public void TestSecondInteractionSoundFromContext()
    {
        if (Application.isPlaying && secondInteractionSound != null)
        {
            Debug.Log($"Probando sonido de segunda interacción: {secondInteractionSound.name}");
            PlayInteractionAudio(secondInteractionSound);
        }
        else if (secondInteractionSound == null)
        {
            Debug.LogWarning("No hay clip de segunda interacción asignado para probar");
        }
        else
        {
            Debug.LogWarning("El test solo funciona en modo Play");
        }
    }

    [ContextMenu("Debug Audio Settings")]
    public void DebugAudioSettings()
    {
        Debug.Log("=== INTERACTABLE OBJECT AUDIO SETTINGS ===");
        Debug.Log($"Interaction Sound: {(interactionSound != null ? interactionSound.name : "NULL")}");
        Debug.Log($"Second Interaction Sound: {(secondInteractionSound != null ? secondInteractionSound.name : "NULL")}");
        Debug.Log($"Sound Volume: {soundVolume}");
        Debug.Log($"Sound Pitch: {soundPitch}");
        Debug.Log($"Use Positional Audio: {usePositionalAudio}");
        Debug.Log($"Audio Max Distance: {audioMaxDistance}");
        Debug.Log($"AudioPlaybackManager Available: {AudioPlaybackManager.Instance != null}");
        Debug.Log("==========================================");
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