using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VERSIÓN SIMPLIFICADA: Integración automática entre Inventory_Manager e InventoryNavigationManager
/// SIN desactivación de otros sistemas - Solo maneja su propia navegación
/// </summary>
[RequireComponent(typeof(Inventory_Manager))]
public class InventoryNavigationIntegration : MonoBehaviour
{
    [Header("Configuración Automática")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool enableNavigationWhenInventoryOpens = true;
    [SerializeField] private bool enableCanvasNavigation = true;

    private Inventory_Manager inventoryManager;
    private InventoryNavigationManager navigationManager;
    private InventoryCanvasIntegration canvasIntegration;

    // Referencias para intercambio de controles
    private bool originalInventoryState = false;

    [Header("Configuración de Auto-Scroll")]
    [SerializeField] private bool enableAutoScrollOnNavigation = true;
    [SerializeField] private bool onlyScrollIfElementNotVisible = true;
    [SerializeField] private float manualScrollGracePeriod = 2f;
    [SerializeField] private float visibilityMargin = 50f;

    private void Awake()
    {
        // Obtener componentes
        inventoryManager = GetComponent<Inventory_Manager>();
        navigationManager = GetComponent<InventoryNavigationManager>();
        canvasIntegration = GetComponent<InventoryCanvasIntegration>();

        // Crear InventoryNavigationManager si no existe
        if (navigationManager == null)
        {
            navigationManager = gameObject.AddComponent<InventoryNavigationManager>();
        }

        // Crear InventoryCanvasIntegration si no existe y está habilitado
        if (canvasIntegration == null && enableCanvasNavigation)
        {
            canvasIntegration = gameObject.AddComponent<InventoryCanvasIntegration>();
        }
    }

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupIntegration();
        }
    }

    private void OnEnable()
    {
        SetupInventoryEvents();
    }

    private void OnDisable()
    {
        CleanupInventoryEvents();
    }

    public void SetupIntegration()
    {
        Debug.Log("Configurando integración de navegación para inventario...");

        if (inventoryManager != null && navigationManager != null)
        {
            ConfigureNavigationReferences();
            ConfigureScrollRects();
            ConfigureAutoScroll();
        }

        SetupNavigationEvents();

        Debug.Log("Integración de navegación configurada correctamente");
    }

    private void ConfigureNavigationReferences()
    {
        var inventoryType = typeof(Inventory_Manager);

        // Obtener noteContainer usando reflexión
        var noteContainerField = inventoryType.GetField("noteContainer");
        if (noteContainerField != null)
        {
            Transform noteContainer = (Transform)noteContainerField.GetValue(inventoryManager);
            SetNavigationField("noteContainer", noteContainer);
        }

        // Obtener objectContainer usando reflexión
        var objectContainerField = inventoryType.GetField("objectContainer");
        if (objectContainerField != null)
        {
            Transform objectContainer = (Transform)objectContainerField.GetValue(inventoryManager);
            SetNavigationField("objectContainer", objectContainer);
        }

        DetectScrollRects();
    }

    private void SetNavigationField(string fieldName, object value)
    {
        var navType = typeof(InventoryNavigationManager);
        var field = navType.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(navigationManager, value);
            Debug.Log($"Campo {fieldName} configurado en InventoryNavigationManager");
        }
    }

    private void DetectScrollRects()
    {
        if (inventoryManager.InventoryInterface != null)
        {
            ScrollRect[] scrolls = inventoryManager.InventoryInterface.GetComponentsInChildren<ScrollRect>();

            if (scrolls.Length >= 1)
            {
                SetNavigationField("noteScrollRect", scrolls[0]);
            }

            if (scrolls.Length >= 2)
            {
                SetNavigationField("objectScrollRect", scrolls[1]);
            }

            Debug.Log($"Detectados {scrolls.Length} ScrollRects automáticamente");
        }
    }

    private void SetupInventoryEvents()
    {
        if (enableNavigationWhenInventoryOpens)
        {
            InvokeRepeating(nameof(CheckInventoryState), 0.1f, 0.1f);
        }
    }

    private void CleanupInventoryEvents()
    {
        CancelInvoke(nameof(CheckInventoryState));
    }

    private void CheckInventoryState()
    {
        if (inventoryManager == null) return;

        bool currentInventoryState = inventoryManager.IsInventoryOpen();

        if (currentInventoryState != originalInventoryState)
        {
            originalInventoryState = currentInventoryState;

            if (currentInventoryState)
            {
                OnInventoryOpened();
            }
            else
            {
                OnInventoryClosed();
            }
        }
    }

    /// <summary>
    /// VERSIÓN SIMPLIFICADA: Solo habilita la navegación del inventario sin afectar otros sistemas
    /// </summary>
    private void OnInventoryOpened()
    {
        Debug.Log("Inventario abierto - Habilitando navegación del inventario");

        // CAMBIO IMPORTANTE: Solo habilitar la navegación del inventario
        // NO desactivar otros sistemas
        if (navigationManager != null)
        {
            navigationManager.enabled = true;

            // Configurar navegación específica del inventario
            try
            {
                var resetMethod = navigationManager.GetType().GetMethod("ResetManualScrollState");
                if (resetMethod != null)
                {
                    resetMethod.Invoke(navigationManager, null);
                }

                var refreshMethod = navigationManager.GetType().GetMethod("ForceRefresh");
                if (refreshMethod != null)
                {
                    refreshMethod.Invoke(navigationManager, null);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error al configurar navegación del inventario: {e.Message}");
            }
        }
    }

    /// <summary>
    /// VERSIÓN SIMPLIFICADA: Solo deshabilita la navegación del inventario
    /// </summary>
    private void OnInventoryClosed()
    {
        Debug.Log("Inventario cerrado - Deshabilitando navegación del inventario");

        // CAMBIO IMPORTANTE: Solo deshabilitar la navegación del inventario
        // NO afectar otros sistemas
        if (navigationManager != null)
        {
            navigationManager.enabled = false;
        }
    }

    private void SetupNavigationEvents()
    {
        if (navigationManager != null)
        {
            try
            {
                var navType = typeof(InventoryNavigationManager);

                // Buscar eventos usando reflexión
                var onElementSelectedEvent = navType.GetEvent("OnElementSelected");
                var onElementSubmittedEvent = navType.GetEvent("OnElementSubmitted");
                var onSectionChangedEvent = navType.GetEvent("OnSectionChanged");

                // Suscribirse a eventos si existen
                if (onElementSelectedEvent != null)
                {
                    var addMethod = onElementSelectedEvent.GetAddMethod();
                    var delegateType = onElementSelectedEvent.EventHandlerType;
                    var delegateInstance = System.Delegate.CreateDelegate(delegateType, this, nameof(OnNavigationElementSelected));
                    addMethod.Invoke(navigationManager, new object[] { delegateInstance });
                }

                if (onElementSubmittedEvent != null)
                {
                    var addMethod = onElementSubmittedEvent.GetAddMethod();
                    var delegateType = onElementSubmittedEvent.EventHandlerType;
                    var delegateInstance = System.Delegate.CreateDelegate(delegateType, this, nameof(OnNavigationElementSubmitted));
                    addMethod.Invoke(navigationManager, new object[] { delegateInstance });
                }

                if (onSectionChangedEvent != null)
                {
                    var addMethod = onSectionChangedEvent.GetAddMethod();
                    var delegateType = onSectionChangedEvent.EventHandlerType;
                    var delegateInstance = System.Delegate.CreateDelegate(delegateType, this, nameof(OnNavigationSectionChangedReflection));
                    addMethod.Invoke(navigationManager, new object[] { delegateInstance });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error al configurar eventos de navegación: {e.Message}");
            }
        }
    }

    private void OnNavigationElementSelected(Button selectedButton)
    {
        Debug.Log($"Elemento seleccionado por navegación: {selectedButton.name}");
    }

    private void OnNavigationElementSubmitted(Button submittedButton)
    {
        Debug.Log($"Elemento confirmado por navegación: {submittedButton.name}");
    }

    private void OnNavigationSectionChangedReflection(object newSection)
    {
        Debug.Log($"Sección cambiada a: {newSection}");
    }

    private void ConfigureScrollRects()
    {
        // Verificar si los ScrollRects están asignados usando reflexión
        var navType = typeof(InventoryNavigationManager);
        var noteScrollField = navType.GetField("noteScrollRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var objectScrollField = navType.GetField("objectScrollRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (noteScrollField?.GetValue(navigationManager) == null || objectScrollField?.GetValue(navigationManager) == null)
        {
            Debug.LogWarning("ScrollRects no están asignados. Intentando detección automática...");
            DetectScrollRects();
        }

        // Configurar propiedades de scroll usando reflexión si los métodos existen
        try
        {
            var setScrollEnabledMethod = navType.GetMethod("SetScrollEnabled");
            if (setScrollEnabledMethod != null)
            {
                setScrollEnabledMethod.Invoke(navigationManager, new object[] { true });
            }

            var setScrollSensitivityMethod = navType.GetMethod("SetScrollSensitivity");
            if (setScrollSensitivityMethod != null)
            {
                setScrollSensitivityMethod.Invoke(navigationManager, new object[] { 2.0f });
            }

            var setScrollDeadZoneMethod = navType.GetMethod("SetScrollDeadZone");
            if (setScrollDeadZoneMethod != null)
            {
                setScrollDeadZoneMethod.Invoke(navigationManager, new object[] { 0.3f });
            }

            Debug.Log("Configuración de scroll completada");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error al configurar scroll: {e.Message}");
        }
    }

    public void ConfigureAutoScroll()
    {
        if (navigationManager != null)
        {
            try
            {
                var navType = typeof(InventoryNavigationManager);

                var setAutoScrollEnabledMethod = navType.GetMethod("SetAutoScrollEnabled");
                if (setAutoScrollEnabledMethod != null)
                {
                    setAutoScrollEnabledMethod.Invoke(navigationManager, new object[] { enableAutoScrollOnNavigation });
                }

                var setScrollOnlyIfNotVisibleMethod = navType.GetMethod("SetScrollOnlyIfNotVisible");
                if (setScrollOnlyIfNotVisibleMethod != null)
                {
                    setScrollOnlyIfNotVisibleMethod.Invoke(navigationManager, new object[] { onlyScrollIfElementNotVisible });
                }

                var setManualScrollGracePeriodMethod = navType.GetMethod("SetManualScrollGracePeriod");
                if (setManualScrollGracePeriodMethod != null)
                {
                    setManualScrollGracePeriodMethod.Invoke(navigationManager, new object[] { manualScrollGracePeriod });
                }

                var setVisibilityMarginMethod = navType.GetMethod("SetVisibilityMargin");
                if (setVisibilityMarginMethod != null)
                {
                    setVisibilityMarginMethod.Invoke(navigationManager, new object[] { visibilityMargin });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error al configurar auto-scroll: {e.Message}");
            }
        }
    }

    // Métodos públicos para control manual - SIMPLIFICADOS
    public void EnableInventoryNavigation()
    {
        if (navigationManager != null)
        {
            navigationManager.enabled = true;

            try
            {
                var refreshMethod = navigationManager.GetType().GetMethod("ForceRefresh");
                if (refreshMethod != null)
                {
                    refreshMethod.Invoke(navigationManager, null);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error al refrescar navegación: {e.Message}");
            }
        }
    }

    public void DisableInventoryNavigation()
    {
        if (navigationManager != null)
        {
            navigationManager.enabled = false;
        }
    }

    public void RefreshInventoryNavigation()
    {
        if (navigationManager != null)
        {
            try
            {
                var refreshMethod = navigationManager.GetType().GetMethod("ForceRefresh");
                if (refreshMethod != null)
                {
                    refreshMethod.Invoke(navigationManager, null);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error al refrescar navegación: {e.Message}");
            }
        }
    }

    // Métodos de contexto para debugging
    [ContextMenu("Setup Manual Integration")]
    public void SetupManualIntegration()
    {
        SetupIntegration();
    }

    [ContextMenu("Test Navigation")]
    public void TestNavigation()
    {
        if (navigationManager != null)
        {
            try
            {
                var debugMethod = navigationManager.GetType().GetMethod("DebugCurrentState");
                if (debugMethod != null)
                {
                    debugMethod.Invoke(navigationManager, null);
                }
                else
                {
                    Debug.Log($"InventoryNavigationManager está presente: {navigationManager.name}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error al hacer debug de navegación: {e.Message}");
            }
        }
    }

    [ContextMenu("Force Refresh Navigation")]
    public void ForceRefreshNavigation()
    {
        RefreshInventoryNavigation();
    }

    [ContextMenu("Debug Integration State")]
    public void DebugIntegrationState()
    {
        Debug.Log($"=== INVENTORY NAVIGATION INTEGRATION STATE (SIMPLIFIED) ===");
        Debug.Log($"Inventory Manager: {inventoryManager?.name ?? "NULL"}");
        Debug.Log($"Navigation Manager: {navigationManager?.name ?? "NULL"}");
        Debug.Log($"Navigation Manager Enabled: {navigationManager?.enabled}");
        Debug.Log($"Canvas Integration: {canvasIntegration?.name ?? "NULL"}");
        Debug.Log($"Original Inventory State: {originalInventoryState}");
        Debug.Log($"Current Inventory Open: {inventoryManager?.IsInventoryOpen()}");
        Debug.Log("=== SIN INTERFERENCIA CON OTROS SISTEMAS ===");
    }
}