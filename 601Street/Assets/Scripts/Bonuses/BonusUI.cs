using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Script para cada instancia individual de bonus en la interfaz
/// Maneja la interacción y visualización de un bonus específico
/// VERSIÓN CORREGIDA: Soluciona problemas de hover y navegación
/// </summary>
public class BonusUI : MonoBehaviour
{
    [Header("Referencias del Prefab")]
    [SerializeField] private Button bonusButton;
    [SerializeField] private Image bonusImage;
    [SerializeField] private TMP_Text bonusValueText;
    [SerializeField] private GameObject activeIndicator; // Opcional: indicador visual de bonus activo

    [Header("Configuración Visual")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color disabledColor = Color.gray;
    [SerializeField] private float scaleOnHover = 1.1f;
    [SerializeField] private float animationDuration = 0.2f;

    // Referencias
    private BonusManager bonusManager;
    private BonusManager.CollectedBonus associatedBonus;
    private bool isActive = false;
    private bool isInteractable = true;

    // Para animaciones
    private Vector3 originalScale;
    private Tween currentTween;

    // NUEVO: Control de hover manual para evitar conflictos con navegación
    private bool isHovering = false;
    private bool isNavigationSelected = false;

    private void Awake()
    {
        // Buscar componentes automáticamente si no están asignados
        if (bonusButton == null)
            bonusButton = GetComponent<Button>();

        if (bonusImage == null)
            bonusImage = GetComponent<Image>();

        if (bonusValueText == null)
            bonusValueText = GetComponentInChildren<TMP_Text>();

        // Guardar escala original
        originalScale = transform.localScale;
    }

    private void Start()
    {
        // IMPORTANTE: Preservar escala correcta
        if (transform.localScale == Vector3.zero)
        {
            transform.localScale = Vector3.one;
            Debug.Log($"BonusUI Start - Escala corregida a: {transform.localScale}");
        }

        // Configurar eventos del botón
        if (bonusButton != null)
        {
            bonusButton.onClick.AddListener(OnBonusClicked);

            // CORRECCIÓN: Configurar eventos de hover de forma más controlada
            ConfigureHoverEvents();
        }
        else
        {
            Debug.LogError($"BonusUI en {gameObject.name}: No se encontró componente Button");
        }
    }

    /// <summary>
    /// NUEVO: Configuración más robusta de eventos de hover
    /// </summary>
    private void ConfigureHoverEvents()
    {
        // Añadir eventos de hover usando el nuevo sistema
        var eventTrigger = bonusButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = bonusButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        // Limpiar eventos existentes para evitar duplicados
        eventTrigger.triggers.Clear();

        // Evento de mouse enter
        var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { OnPointerEnterSafe(); });
        eventTrigger.triggers.Add(pointerEnter);

        // Evento de mouse exit
        var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { OnPointerExitSafe(); });
        eventTrigger.triggers.Add(pointerExit);

        // NUEVO: Eventos de selección/deselección para navegación con gamepad
        var selectEvent = new UnityEngine.EventSystems.EventTrigger.Entry();
        selectEvent.eventID = UnityEngine.EventSystems.EventTriggerType.Select;
        selectEvent.callback.AddListener((data) => { OnNavigationSelect(); });
        eventTrigger.triggers.Add(selectEvent);

        var deselectEvent = new UnityEngine.EventSystems.EventTrigger.Entry();
        deselectEvent.eventID = UnityEngine.EventSystems.EventTriggerType.Deselect;
        deselectEvent.callback.AddListener((data) => { OnNavigationDeselect(); });
        eventTrigger.triggers.Add(deselectEvent);
    }

    /// <summary>
    /// Inicializa el bonus UI con los datos proporcionados
    /// </summary>
    public void Initialize(BonusManager.CollectedBonus bonus, BonusManager manager)
    {
        associatedBonus = bonus;
        bonusManager = manager;

        // IMPORTANTE: Asegurar escala correcta desde el inicio
        transform.localScale = Vector3.one;
        originalScale = Vector3.one; // Actualizar la referencia de escala original
        Debug.Log($"BonusUI - Escala establecida a: {transform.localScale}");

        // Configurar texto del valor
        if (bonusValueText != null)
        {
            bonusValueText.text = $"+{bonus.bonusValue}";
            Debug.Log($"BonusUI - Texto configurado: +{bonus.bonusValue}");
        }
        else
        {
            Debug.LogWarning($"BonusUI en {gameObject.name}: No se encontró componente TMP_Text para mostrar el valor");
        }

        // Configurar icono si está disponible
        if (bonusImage != null && bonus.icon != null)
        {
            bonusImage.sprite = bonus.icon;
        }

        // Configurar tooltip si hay descripción
        if (!string.IsNullOrEmpty(bonus.description))
        {
            Debug.Log($"Bonus inicializado: {bonus.bonusName} - {bonus.description}");
        }

        // Estado inicial
        UpdateVisualState(false);

        Debug.Log($"BonusUI inicializado completamente para: {bonus.bonusName} (+{bonus.bonusValue})");

        // NUEVO: Notificar al sistema de navegación que hay un nuevo elemento
        NotifyNavigationSystemAboutNewElement();
    }

    /// <summary>
    /// NUEVO: Notifica al sistema de navegación sobre este nuevo elemento
    /// </summary>
    private void NotifyNavigationSystemAboutNewElement()
    {
        // Buscar el BonusNavigationExtension en la escena
        BonusNavigationExtension bonusNavigation = FindAnyObjectByType<BonusNavigationExtension>();
        if (bonusNavigation != null)
        {
            bonusNavigation.OnBonusAdded(bonusButton);
        }
        else
        {
            Debug.LogWarning("BonusNavigationExtension no encontrado - Los bonuses no serán navegables con gamepad");
        }
    }

    /// <summary>
    /// Actualiza el estado visual del bonus
    /// </summary>
    public void UpdateVisualState(bool isActive)
    {
        this.isActive = isActive;

        // Actualizar colores
        if (bonusImage != null)
        {
            Color targetColor;

            if (!isInteractable)
                targetColor = disabledColor;
            else if (isActive)
                targetColor = activeColor;
            else
                targetColor = normalColor;

            bonusImage.color = targetColor;
        }

        // Mostrar/ocultar indicador de activo
        if (activeIndicator != null)
        {
            activeIndicator.SetActive(isActive);
        }

        // Actualizar interactividad del botón
        if (bonusButton != null)
        {
            bonusButton.interactable = isInteractable && bonusManager.CanActivateBonuses();
        }

        // CORRECCIÓN: Solo aplicar animación de "activo" si realmente está activo
        // y no hay interacciones de hover/navegación en curso
        if (isActive && !isHovering && !isNavigationSelected)
        {
            AnimateActive();
        }
        else if (!isActive && !isHovering && !isNavigationSelected)
        {
            ResetAnimation();
        }
    }

    /// <summary>
    /// Establece si este bonus puede ser interactuado
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        UpdateVisualState(isActive);
    }

    #region Event Handlers - CORREGIDOS

    private void OnBonusClicked()
    {
        if (!isInteractable || bonusManager == null || associatedBonus == null)
        {
            Debug.LogWarning("BonusUI: Intento de click en bonus no interactuable");
            return;
        }

        if (!bonusManager.CanActivateBonuses())
        {
            Debug.LogWarning("BonusUI: No se pueden activar bonuses en este momento");
            return;
        }

        Debug.Log($"BonusUI: Click en bonus {associatedBonus.bonusName}");

        // Si ya está activo, desactivarlo
        if (isActive)
        {
            bonusManager.DeactivateCurrentBonus();
        }
        else
        {
            // Activar este bonus
            bonusManager.ActivateBonus(associatedBonus);
        }

        // Animación de click
        AnimateClick();
    }

    /// <summary>
    /// NUEVO: Manejo seguro del hover del ratón
    /// </summary>
    private void OnPointerEnterSafe()
    {
        if (!isInteractable || isActive) return;

        isHovering = true;
        Debug.Log($"Hover Enter en bonus: {associatedBonus?.bonusName}");

        // Solo aplicar hover si no está siendo controlado por navegación
        if (!isNavigationSelected)
        {
            ApplyHoverEffect();
        }
    }

    /// <summary>
    /// NUEVO: Manejo seguro de la salida del hover
    /// </summary>
    private void OnPointerExitSafe()
    {
        if (!isInteractable) return;

        isHovering = false;
        Debug.Log($"Hover Exit en bonus: {associatedBonus?.bonusName}");

        // Solo quitar hover si no está siendo controlado por navegación y no está activo
        if (!isNavigationSelected && !isActive)
        {
            ResetToNormalState();
        }
    }

    /// <summary>
    /// NUEVO: Manejo de selección por navegación (gamepad)
    /// </summary>
    private void OnNavigationSelect()
    {
        if (!isInteractable) return;

        isNavigationSelected = true;
        Debug.Log($"Navegación Select en bonus: {associatedBonus?.bonusName}");

        // Aplicar efecto de selección (siempre, incluso si está activo)
        ApplyNavigationSelectEffect();
    }

    /// <summary>
    /// NUEVO: Manejo de deselección por navegación
    /// </summary>
    private void OnNavigationDeselect()
    {
        if (!isInteractable) return;

        isNavigationSelected = false;
        Debug.Log($"Navegación Deselect en bonus: {associatedBonus?.bonusName}");

        // Volver al estado apropiado basado en si está activo o en hover
        if (isActive)
        {
            AnimateActive();
        }
        else if (isHovering)
        {
            ApplyHoverEffect();
        }
        else
        {
            ResetToNormalState();
        }
    }

    #endregion

    #region Animaciones - MEJORADAS

    /// <summary>
    /// NUEVO: Aplica efecto de hover controlado
    /// </summary>
    private void ApplyHoverEffect()
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale * scaleOnHover, animationDuration)
            .SetEase(Ease.OutQuint);
    }

    /// <summary>
    /// NUEVO: Aplica efecto de selección por navegación (más prominente que hover)
    /// </summary>
    private void ApplyNavigationSelectEffect()
    {
        currentTween?.Kill();
        float navigationScale = scaleOnHover * 1.1f; // Ligeramente más grande que hover
        currentTween = transform.DOScale(originalScale * navigationScale, animationDuration)
            .SetEase(Ease.OutQuint);
    }

    /// <summary>
    /// NUEVO: Resetea a estado normal de forma controlada
    /// </summary>
    private void ResetToNormalState()
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale, animationDuration)
            .SetEase(Ease.OutQuint);
    }

    private void AnimateActive()
    {
        // CORRECCIÓN: Solo animar si no hay otras interacciones activas
        if (isHovering || isNavigationSelected) return;

        currentTween?.Kill();

        // Escala ligeramente mayor para bonus activo
        transform.localScale = originalScale * 1.05f;

        // Efecto de pulso sutil para indicar que está activo
        currentTween = transform.DOScale(originalScale * 1.1f, 1f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void ResetAnimation()
    {
        // CORRECCIÓN: Solo resetear si no hay interacciones activas
        if (isHovering || isNavigationSelected) return;

        currentTween?.Kill();
        transform.DOScale(originalScale, animationDuration)
            .SetEase(Ease.OutQuint);
    }

    private void AnimateClick()
    {
        // La animación de click siempre se ejecuta
        currentTween?.Kill();

        transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5, 0.5f)
            .SetEase(Ease.OutQuint);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Obtiene el bonus asociado a esta UI
    /// </summary>
    public BonusManager.CollectedBonus GetAssociatedBonus()
    {
        return associatedBonus;
    }

    /// <summary>
    /// Verifica si este bonus está actualmente activo
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }

    /// <summary>
    /// Actualiza el texto del valor del bonus dinámicamente
    /// </summary>
    public void UpdateBonusValue(int newValue)
    {
        if (associatedBonus != null)
        {
            associatedBonus.bonusValue = newValue;
        }

        if (bonusValueText != null)
        {
            bonusValueText.text = $"+{newValue}";
        }
    }

    /// <summary>
    /// NUEVO: Obtiene el botón para navegación
    /// </summary>
    public Button GetButton()
    {
        return bonusButton;
    }

    /// <summary>
    /// NUEVO: Fuerza el estado de navegación (útil para sistemas externos)
    /// </summary>
    public void SetNavigationSelected(bool selected)
    {
        if (selected)
        {
            OnNavigationSelect();
        }
        else
        {
            OnNavigationDeselect();
        }
    }

    #endregion

    #region Debug

    [ContextMenu("Debug Bonus Info")]
    public void DebugBonusInfo()
    {
        if (associatedBonus != null)
        {
            Debug.Log($"=== BONUS UI DEBUG ===");
            Debug.Log($"Nombre: {associatedBonus.bonusName}");
            Debug.Log($"Valor: +{associatedBonus.bonusValue}");
            Debug.Log($"Descripción: {associatedBonus.description}");
            Debug.Log($"Activo: {isActive}");
            Debug.Log($"Interactuable: {isInteractable}");
            Debug.Log($"Hovering: {isHovering}");
            Debug.Log($"Navigation Selected: {isNavigationSelected}");
            Debug.Log($"===================");
        }
        else
        {
            Debug.Log("No hay bonus asociado a esta UI");
        }
    }

    [ContextMenu("Test Hover Effect")]
    public void TestHoverEffect()
    {
        if (Application.isPlaying)
        {
            ApplyHoverEffect();
        }
    }

    [ContextMenu("Test Navigation Effect")]
    public void TestNavigationEffect()
    {
        if (Application.isPlaying)
        {
            ApplyNavigationSelectEffect();
        }
    }

    #endregion

    private void OnDestroy()
    {
        // Limpiar tweens al destruir
        currentTween?.Kill();
        DOTween.Kill(transform);

        // NUEVO: Notificar al sistema de navegación que este elemento será destruido
        BonusNavigationExtension bonusNavigation = FindAnyObjectByType<BonusNavigationExtension>();
        if (bonusNavigation != null && bonusButton != null)
        {
            bonusNavigation.OnBonusRemoved(bonusButton);
        }
    }
}