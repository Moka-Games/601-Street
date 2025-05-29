using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Extensión del sistema de navegación específicamente para los bonuses
/// Se encarga de registrar/desregistrar automáticamente los botones de bonus
/// </summary>
public class BonusNavigationExtension : MonoBehaviour
{
    [Header("Referencias del Sistema")]
    [SerializeField] private UINavigationManager navigationManager;
    [SerializeField] private BonusManager bonusManager;
    [SerializeField] private Transform bonusesContent; // El contenedor donde aparecen los bonuses

    [Header("Configuración")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool autoRegisterOnBonusCreation = true;
    [SerializeField] private bool setFirstBonusAsSelected = true;

    // Control de estado
    private List<Button> registeredBonusButtons = new List<Button>();
    private bool isNavigationActive = false;

    private void Start()
    {
        InitializeNavigationExtension();
    }

    private void InitializeNavigationExtension()
    {
        Debug.Log("=== INICIALIZANDO BONUS NAVIGATION EXTENSION ===");

        // Buscar referencias automáticamente si no están asignadas
        FindMissingReferences();

        // Validar referencias críticas
        if (!ValidateReferences())
        {
            Debug.LogError("BonusNavigationExtension: Referencias incompletas. La navegación de bonuses no funcionará.");
            return;
        }

        // Configurar el sistema
        SetupNavigationSystem();

        Debug.Log("BonusNavigationExtension inicializado correctamente");
        Debug.Log("================================================");
    }

    private void FindMissingReferences()
    {
        if (navigationManager == null)
        {
            navigationManager = GetComponentInParent<UINavigationManager>();
            if (navigationManager == null)
            {
                navigationManager = FindAnyObjectByType<UINavigationManager>();
            }
            LogDebug($"UINavigationManager encontrado: {navigationManager != null}");
        }

        if (bonusManager == null)
        {
            bonusManager = BonusManager.Instance;
            if (bonusManager == null)
            {
                bonusManager = FindAnyObjectByType<BonusManager>();
            }
            LogDebug($"BonusManager encontrado: {bonusManager != null}");
        }

        if (bonusesContent == null)
        {
            // Buscar el contenedor de bonuses por nombre
            GameObject bonusesContentGO = GameObject.Find("Bonuses_Content");
            if (bonusesContentGO != null)
            {
                bonusesContent = bonusesContentGO.transform;
            }
            LogDebug($"Bonuses_Content encontrado: {bonusesContent != null}");
        }
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (navigationManager == null)
        {
            Debug.LogError("BonusNavigationExtension: UINavigationManager no encontrado");
            isValid = false;
        }

        if (bonusManager == null)
        {
            Debug.LogError("BonusNavigationExtension: BonusManager no encontrado");
            isValid = false;
        }

        if (bonusesContent == null)
        {
            Debug.LogError("BonusNavigationExtension: bonusesContent no encontrado");
            isValid = false;
        }

        return isValid;
    }

    private void SetupNavigationSystem()
    {
        // Escanear bonuses existentes al inicializar
        ScanAndRegisterExistingBonuses();

        LogDebug("Sistema de navegación de bonuses configurado");
    }

    #region Gestión de Bonuses en Navegación

    /// <summary>
    /// Llamado cuando se añade un nuevo bonus (desde BonusUI)
    /// </summary>
    public void OnBonusAdded(Button bonusButton)
    {
        if (bonusButton == null)
        {
            LogDebug("OnBonusAdded: bonusButton es null");
            return;
        }

        LogDebug($"=== AÑADIENDO BONUS A NAVEGACIÓN ===");
        LogDebug($"Bonus: {bonusButton.name}");

        // Verificar que no esté ya registrado
        if (registeredBonusButtons.Contains(bonusButton))
        {
            LogDebug($"Bonus {bonusButton.name} ya está registrado");
            return;
        }

        // Añadir a nuestra lista local
        registeredBonusButtons.Add(bonusButton);

        // Añadir al UINavigationManager
        if (navigationManager != null)
        {
            navigationManager.AddNavigableElement(bonusButton);
            LogDebug($"Bonus {bonusButton.name} añadido al UINavigationManager");

            // Si es el primer bonus y está configurado, seleccionarlo
            if (setFirstBonusAsSelected && registeredBonusButtons.Count == 1)
            {
                navigationManager.SetFirstSelected(bonusButton);
                LogDebug($"Primer bonus configurado como seleccionado por defecto: {bonusButton.name}");
            }

            // Forzar actualización del sistema de navegación
            navigationManager.ForceAutoSelectionCheck();
        }

        LogDebug($"Total bonuses registrados: {registeredBonusButtons.Count}");
        LogDebug("=======================================");
    }

    /// <summary>
    /// Llamado cuando se remueve un bonus (desde BonusUI)
    /// </summary>
    public void OnBonusRemoved(Button bonusButton)
    {
        if (bonusButton == null) return;

        LogDebug($"=== REMOVIENDO BONUS DE NAVEGACIÓN ===");
        LogDebug($"Bonus: {bonusButton.name}");

        // Remover de nuestra lista local
        registeredBonusButtons.Remove(bonusButton);

        // Remover del UINavigationManager
        if (navigationManager != null)
        {
            navigationManager.RemoveNavigableElement(bonusButton);
            LogDebug($"Bonus {bonusButton.name} removido del UINavigationManager");

            // Si era el primer seleccionado, encontrar un reemplazo
            if (navigationManager.CurrentSelected == bonusButton && registeredBonusButtons.Count > 0)
            {
                navigationManager.SetFirstSelected(registeredBonusButtons[0]);
                LogDebug($"Nuevo primer seleccionado: {registeredBonusButtons[0].name}");
            }

            // Forzar actualización del sistema de navegación
            navigationManager.ForceAutoSelectionCheck();
        }

        LogDebug($"Total bonuses registrados: {registeredBonusButtons.Count}");
        LogDebug("=====================================");
    }

    /// <summary>
    /// Escanea y registra todos los bonuses existentes en el contenedor
    /// </summary>
    public void ScanAndRegisterExistingBonuses()
    {
        if (bonusesContent == null) return;

        LogDebug("=== ESCANEANDO BONUSES EXISTENTES ===");

        // Limpiar lista actual
        registeredBonusButtons.Clear();

        // Buscar todos los botones de bonus en el contenedor
        Button[] bonusButtons = bonusesContent.GetComponentsInChildren<Button>();

        LogDebug($"Bonuses encontrados: {bonusButtons.Length}");

        foreach (Button bonusButton in bonusButtons)
        {
            // Verificar que el botón esté activo y sea interactuable
            if (bonusButton.gameObject.activeInHierarchy && bonusButton.interactable)
            {
                OnBonusAdded(bonusButton);
            }
        }

        LogDebug($"Bonuses registrados: {registeredBonusButtons.Count}");
        LogDebug("===================================");
    }

    /// <summary>
    /// Actualiza el estado de navegación cuando se abre/cierra la ventana de bonuses
    /// </summary>
    public void OnBonusWindowStateChanged(bool isOpen)
    {
        isNavigationActive = isOpen;

        LogDebug($"=== VENTANA DE BONUSES {(isOpen ? "ABIERTA" : "CERRADA")} ===");

        if (isOpen)
        {
            // Ventana abierta: asegurar que los bonuses estén registrados
            ScanAndRegisterExistingBonuses();

            // Configurar navegación si hay bonuses
            if (registeredBonusButtons.Count > 0 && navigationManager != null)
            {
                // Seleccionar el primer bonus automáticamente
                if (setFirstBonusAsSelected)
                {
                    navigationManager.SetFirstSelected(registeredBonusButtons[0]);
                }

                navigationManager.ForceAutoSelectionCheck();
                LogDebug("Navegación de bonuses activada");
            }
        }
        else
        {
            // Ventana cerrada: limpiar selección si es necesario
            if (navigationManager != null)
            {
                // Solo limpiar si el elemento seleccionado es un bonus
                if (navigationManager.CurrentSelected != null &&
                    registeredBonusButtons.Contains(navigationManager.CurrentSelected as Button))
                {
                    // No limpiar la selección completamente, dejar que el sistema maneje la transición
                    LogDebug("Ventana de bonuses cerrada - navegación transferida");
                }
            }
        }

        LogDebug("===========================================");
    }

    #endregion

    #region Métodos Públicos de Utilidad

    /// <summary>
    /// Fuerza una actualización completa del sistema de navegación de bonuses
    /// </summary>
    public void RefreshBonusNavigation()
    {
        LogDebug("Refrescando navegación de bonuses...");
        ScanAndRegisterExistingBonuses();
    }

    /// <summary>
    /// Obtiene la lista de botones de bonus registrados
    /// </summary>
    public List<Button> GetRegisteredBonusButtons()
    {
        return new List<Button>(registeredBonusButtons);
    }

    /// <summary>
    /// Verifica si un botón específico está registrado
    /// </summary>
    public bool IsBonusRegistered(Button bonusButton)
    {
        return registeredBonusButtons.Contains(bonusButton);
    }

    /// <summary>
    /// Configura qué bonus debe ser seleccionado por defecto
    /// </summary>
    public void SetDefaultSelectedBonus(Button bonusButton)
    {
        if (bonusButton != null && registeredBonusButtons.Contains(bonusButton) && navigationManager != null)
        {
            navigationManager.SetFirstSelected(bonusButton);
            LogDebug($"Bonus seleccionado por defecto configurado: {bonusButton.name}");
        }
    }

    /// <summary>
    /// Habilita o deshabilita la selección automática del primer bonus
    /// </summary>
    public void SetFirstBonusSelection(bool enabled)
    {
        setFirstBonusAsSelected = enabled;
        LogDebug($"Selección automática del primer bonus: {(enabled ? "habilitada" : "deshabilitada")}");
    }

    #endregion

    #region Integración con BonusManager

    /// <summary>
    /// Método que puede ser llamado desde BonusManager cuando se crea un nuevo bonus
    /// </summary>
    public void NotifyBonusCreated(GameObject bonusUI)
    {
        if (bonusUI == null) return;

        Button bonusButton = bonusUI.GetComponent<Button>();
        if (bonusButton != null)
        {
            OnBonusAdded(bonusButton);
        }
        else
        {
            LogDebug($"WARNING: Bonus UI {bonusUI.name} no tiene componente Button");
        }
    }

    /// <summary>
    /// Método que puede ser llamado desde BonusManager cuando se destruye un bonus
    /// </summary>
    public void NotifyBonusDestroyed(GameObject bonusUI)
    {
        if (bonusUI == null) return;

        Button bonusButton = bonusUI.GetComponent<Button>();
        if (bonusButton != null)
        {
            OnBonusRemoved(bonusButton);
        }
    }

    #endregion

    #region Métodos de Debug

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[BonusNavigation] {message}");
        }
    }

    [ContextMenu("Debug Navigation State")]
    public void DebugNavigationState()
    {
        Debug.Log("=== ESTADO DE NAVEGACIÓN DE BONUSES ===");
        Debug.Log($"NavigationManager: {(navigationManager != null ? navigationManager.name : "NULL")}");
        Debug.Log($"BonusManager: {(bonusManager != null ? bonusManager.name : "NULL")}");
        Debug.Log($"BonusesContent: {(bonusesContent != null ? bonusesContent.name : "NULL")}");
        Debug.Log($"Navegación activa: {isNavigationActive}");
        Debug.Log($"Bonuses registrados: {registeredBonusButtons.Count}");

        if (registeredBonusButtons.Count > 0)
        {
            Debug.Log("--- BONUSES REGISTRADOS ---");
            for (int i = 0; i < registeredBonusButtons.Count; i++)
            {
                Button btn = registeredBonusButtons[i];
                Debug.Log($"[{i}] {btn.name} - Activo: {btn.gameObject.activeInHierarchy} - Interactuable: {btn.interactable}");
            }
        }

        if (navigationManager != null)
        {
            Debug.Log($"Elemento seleccionado en NavigationManager: {navigationManager.CurrentSelected?.name ?? "NINGUNO"}");
            Debug.Log($"Total elementos en NavigationManager: {navigationManager.NavigableElements.Count}");
        }

        Debug.Log("=====================================");
    }

    [ContextMenu("Force Refresh Navigation")]
    public void ForceRefreshNavigationFromContext()
    {
        RefreshBonusNavigation();
    }

    [ContextMenu("Test Register All Bonuses")]
    public void TestRegisterAllBonusesFromContext()
    {
        if (Application.isPlaying)
        {
            ScanAndRegisterExistingBonuses();
            Debug.Log($"Test completado: {registeredBonusButtons.Count} bonuses registrados");
        }
        else
        {
            Debug.LogWarning("Este test solo funciona en Play Mode");
        }
    }

    #endregion

    private void OnDestroy()
    {
        // Limpiar referencias
        registeredBonusButtons.Clear();
    }
}