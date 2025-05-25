using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Script para cada instancia individual de bonus en la interfaz
/// Maneja la interacción y visualización de un bonus específico
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

            // Añadir eventos de hover si queremos efectos visuales
            var eventTrigger = bonusButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = bonusButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }

            // Evento de mouse enter
            var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => { OnPointerEnter(); });
            eventTrigger.triggers.Add(pointerEnter);

            // Evento de mouse exit
            var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => { OnPointerExit(); });
            eventTrigger.triggers.Add(pointerExit);
        }
        else
        {
            Debug.LogError($"BonusUI en {gameObject.name}: No se encontró componente Button");
        }
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

        // Animación visual si está activo
        if (isActive)
        {
            AnimateActive();
        }
        else
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

    #region Event Handlers

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

    private void OnPointerEnter()
    {
        if (!isInteractable || isActive) return;

        // Animación de hover
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale * scaleOnHover, animationDuration)
            .SetEase(Ease.OutQuint);
    }

    private void OnPointerExit()
    {
        if (!isInteractable) return;

        // Volver a escala normal (a menos que esté activo)
        if (!isActive)
        {
            currentTween?.Kill();
            currentTween = transform.DOScale(originalScale, animationDuration)
                .SetEase(Ease.OutQuint);
        }
    }

    #endregion

    #region Animaciones

    private void AnimateActive()
    {
        // Animación especial para bonus activo
        currentTween?.Kill();

        // Escala ligeramente mayor y efecto de "brillo"
        transform.localScale = originalScale * 1.05f;

        // Efecto de pulso sutil para indicar que está activo
        currentTween = transform.DOScale(originalScale * 1.1f, 1f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void ResetAnimation()
    {
        // Detener animaciones y volver al estado normal
        currentTween?.Kill();
        transform.DOScale(originalScale, animationDuration)
            .SetEase(Ease.OutQuint);
    }

    private void AnimateClick()
    {
        // Animación rápida de "punch" al hacer click
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
            Debug.Log($"===================");
        }
        else
        {
            Debug.Log("No hay bonus asociado a esta UI");
        }
    }

    #endregion

    private void OnDestroy()
    {
        // Limpiar tweens al destruir
        currentTween?.Kill();
        DOTween.Kill(transform);
    }
}