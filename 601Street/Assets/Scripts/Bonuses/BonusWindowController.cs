using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

/// <summary>
/// Controlador específico para la ventana de bonuses
/// Maneja las interacciones y estados específicos de la ventana
/// </summary>
public class BonusWindowController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private BonusManager bonusManager;
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject bonusWindow;
    [SerializeField] private Transform bonusesContent;

    [Header("Configuración Visual")]
    [SerializeField] private GameObject emptyStateMessage; // Mensaje cuando no hay bonuses
    [SerializeField] private TMP_Text bonusCountText; // Opcional: mostrar cantidad de bonuses

    [Header("Integración con Sistema de Navegación")]
    [SerializeField] private UINavigationManager navigationManager;
    [SerializeField] private bool addToNavigationWhenOpen = true;

    private bool isWindowOpen = false;

    private void Start()
    {
        // Buscar referencias automáticamente si no están asignadas
        if (bonusManager == null)
            bonusManager = BonusManager.Instance;

        if (navigationManager == null)
            navigationManager = FindAnyObjectByType<UINavigationManager>();

        // Configurar botón
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(OnToggleButtonClicked);
        }

        // Estado inicial
        UpdateVisualState();
    }

    private void OnToggleButtonClicked()
    {
        Debug.Log("BonusWindowController: Toggle button clicked");

        if (bonusManager == null)
        {
            Debug.LogWarning("BonusWindowController: No hay BonusManager disponible");
            return;
        }

        // El BonusManager maneja la lógica principal del toggle
        // Este método puede añadir lógica adicional específica del controlador

        // Actualizar estado visual después del toggle
        // (El BonusManager ya maneja la animación principal)
        Invoke(nameof(UpdateVisualState), 0.1f);
    }

    /// <summary>
    /// Actualiza el estado visual de la ventana
    /// </summary>
    public void UpdateVisualState()
    {
        if (bonusManager == null) return;

        int bonusCount = bonusManager.GetCollectedBonusCount();
        bool hasActiveBnus = bonusManager.HasActiveBBonus();

        // Actualizar contador de bonuses
        if (bonusCountText != null)
        {
            bonusCountText.text = bonusCount.ToString();
        }

        // Mostrar/ocultar mensaje de estado vacío
        if (emptyStateMessage != null)
        {
            emptyStateMessage.SetActive(bonusCount == 0);
        }

        // Actualizar color del botón basado en el estado
        UpdateToggleButtonVisual(bonusCount, hasActiveBnus);

        Debug.Log($"BonusWindowController: Visual state updated - Count: {bonusCount}, Active: {hasActiveBnus}");
    }

    private void UpdateToggleButtonVisual(int bonusCount, bool hasActiveBonus)
    {
        if (toggleButton == null) return;

        Image buttonImage = toggleButton.GetComponent<Image>();
        if (buttonImage == null) return;

        // Cambiar color basado en el estado
        Color targetColor;

        if (hasActiveBonus)
        {
            targetColor = Color.green; // Verde si hay bonus activo
        }
        else if (bonusCount > 0)
        {
            targetColor = Color.yellow; // Amarillo si hay bonuses disponibles
        }
        else
        {
            targetColor = Color.white; // Blanco por defecto
        }

        // Animar el cambio de color
        buttonImage.DOColor(targetColor, 0.3f);

        // Efecto de pulso si hay bonus activo
        if (hasActiveBonus)
        {
            buttonImage.transform.DOScale(1.1f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
        else
        {
            // Detener el pulso
            buttonImage.transform.DOKill();
            buttonImage.transform.DOScale(1f, 0.3f);
        }
    }

    /// <summary>
    /// Método llamado cuando la ventana se abre
    /// </summary>
    public void OnWindowOpened()
    {
        isWindowOpen = true;

        // Añadir elementos de la ventana al sistema de navegación
        if (addToNavigationWhenOpen && navigationManager != null)
        {
            AddBonusesToNavigation();
        }

        Debug.Log("BonusWindowController: Window opened");
    }

    /// <summary>
    /// Método llamado cuando la ventana se cierra
    /// </summary>
    public void OnWindowClosed()
    {
        isWindowOpen = false;

        // Remover elementos de la ventana del sistema de navegación
        if (navigationManager != null)
        {
            RemoveBonusesFromNavigation();
        }

        Debug.Log("BonusWindowController: Window closed");
    }

    private void AddBonusesToNavigation()
    {
        if (bonusesContent == null || navigationManager == null) return;

        // Añadir todos los botones de bonus al sistema de navegación
        Button[] bonusButtons = bonusesContent.GetComponentsInChildren<Button>();

        foreach (Button bonusButton in bonusButtons)
        {
            navigationManager.AddNavigableElement(bonusButton);
        }

        // Forzar actualización del sistema de navegación
        navigationManager.ForceAutoSelectionCheck();

        Debug.Log($"BonusWindowController: {bonusButtons.Length} bonus buttons added to navigation");
    }

    private void RemoveBonusesFromNavigation()
    {
        if (bonusesContent == null || navigationManager == null) return;

        // Remover todos los botones de bonus del sistema de navegación
        Button[] bonusButtons = bonusesContent.GetComponentsInChildren<Button>();

        foreach (Button bonusButton in bonusButtons)
        {
            navigationManager.RemoveNavigableElement(bonusButton);
        }

        Debug.Log($"BonusWindowController: Bonus buttons removed from navigation");
    }

    /// <summary>
    /// Actualiza la integración con el sistema de navegación cuando se añade un nuevo bonus
    /// </summary>
    public void OnBonusAdded(GameObject newBonusUI)
    {
        if (!isWindowOpen || !addToNavigationWhenOpen || navigationManager == null) return;

        Button bonusButton = newBonusUI.GetComponent<Button>();
        if (bonusButton != null)
        {
            navigationManager.AddNavigableElement(bonusButton);
            navigationManager.ForceAutoSelectionCheck();

            Debug.Log($"BonusWindowController: New bonus button added to navigation: {newBonusUI.name}");
        }
    }

    /// <summary>
    /// Actualiza la integración con el sistema de navegación cuando se remueve un bonus
    /// </summary>
    public void OnBonusRemoved(GameObject removedBonusUI)
    {
        if (navigationManager == null) return;

        Button bonusButton = removedBonusUI.GetComponent<Button>();
        if (bonusButton != null)
        {
            navigationManager.RemoveNavigableElement(bonusButton);
            navigationManager.ForceAutoSelectionCheck();

            Debug.Log($"BonusWindowController: Bonus button removed from navigation: {removedBonusUI.name}");
        }
    }

    #region Public Methods

    /// <summary>
    /// Verifica si la ventana está actualmente abierta
    /// </summary>
    public bool IsWindowOpen()
    {
        return isWindowOpen;
    }

    /// <summary>
    /// Fuerza la actualización del estado visual
    /// </summary>
    public void ForceUpdateVisualState()
    {
        UpdateVisualState();
    }

    /// <summary>
    /// Configura si los bonuses deben añadirse al sistema de navegación
    /// </summary>
    public void SetNavigationIntegration(bool enable)
    {
        addToNavigationWhenOpen = enable;
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug Window State")]
    public void DebugWindowState()
    {
        Debug.Log($"=== BONUS WINDOW CONTROLLER STATE ===");
        Debug.Log($"Window Open: {isWindowOpen}");
        Debug.Log($"Bonus Manager: {(bonusManager != null ? "Found" : "Missing")}");
        Debug.Log($"Navigation Manager: {(navigationManager != null ? "Found" : "Missing")}");
        Debug.Log($"Toggle Button: {(toggleButton != null ? "Found" : "Missing")}");
        Debug.Log($"Navigation Integration: {addToNavigationWhenOpen}");

        if (bonusManager != null)
        {
            Debug.Log($"Collected Bonuses: {bonusManager.GetCollectedBonusCount()}");
            Debug.Log($"Active Bonus: {(bonusManager.HasActiveBBonus() ? bonusManager.GetActiveBonusName() : "None")}");
        }
        Debug.Log($"===================================");
    }

    [ContextMenu("Force Update Visual State")]
    public void ForceUpdateVisualStateFromContext()
    {
        ForceUpdateVisualState();
    }

    #endregion

    private void OnDestroy()
    {
        // Limpiar tweens
        if (toggleButton != null)
        {
            DOTween.Kill(toggleButton.GetComponent<Image>());
            DOTween.Kill(toggleButton.transform);
        }
    }
}