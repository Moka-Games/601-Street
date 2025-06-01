using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VERSIÓN CORREGIDA: Solución completa al problema de navegación del inventario
/// - Elimina todas las desactivaciones de navegación
/// - Mantiene consistencia entre sistemas de navegación
/// - Corrige el problema de cierre de prefabs con el mando
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
        Debug.Log("Configurando integración de navegación para inventario - SIN DESACTIVACIONES...");

        if (inventoryManager != null && navigationManager != null)
        {
            ConfigureNavigationReferences();
            ConfigureScrollRects();
            ConfigureAutoScroll();
        }

        SetupNavigationEvents();

        // NUEVO: Configurar el componente InventoryCanvasIntegration
        if (canvasIntegration != null)
        {
            // Asegurarnos de que el canvasIntegration esté activado
            canvasIntegration.enabled = true;
        }

        Debug.Log("Integración de navegación configurada correctamente - TODOS los sistemas permanecen activos");
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
        bool hasActiveInteraction = inventoryManager.HasActiveInteractionObject();

        // NUEVO: Comprobar si hay un objeto de interacción activo
        // Si hay un objeto de interacción activo, activar el InventoryCanvasIntegration
        if (hasActiveInteraction && canvasIntegration != null && !canvasIntegration.IsNavigationActive())
        {
            canvasIntegration.ForceActivateCanvasNavigation();
            Debug.Log("Detectado objeto de interacción - Activando navegación de canvas");
        }

        // NUEVO: Comprobar si el inventario ha cambiado de estado
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
    /// CORREGIDO: Solo habilita la navegación del inventario, NUNCA desactiva otros sistemas
    /// </summary>
    private void OnInventoryOpened()
    {
        Debug.Log("Inventario abierto - Habilitando navegación del inventario SIN TOCAR otros sistemas");

        // SOLO ACTIVAR, NUNCA desactivar otros sistemas
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

                var refreshMethod = navigationManager.GetType().GetMethod("ForceRefreshElements");
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

        Debug.Log("Navegación del inventario activada - TODOS los demás sistemas mantienen su estado");
    }

    /// <summary>
    /// COMPLETAMENTE CORREGIDO: NO deshabilita NINGÚN sistema de navegación
    /// </summary>
    private void OnInventoryClosed()
    {
        Debug.Log("Inventario cerrado - MANTENIENDO TODOS los sistemas de navegación activos");

        // CRÍTICO: NO desactivar NINGÚN sistema de navegación
        // Todos los sistemas permanecen activos para evitar problemas

        if (navigationManager != null)
        {
            Debug.Log("InventoryNavigationManager mantiene su estado activo para evitar conflictos");
            // NO HACER: navigationManager.enabled = false;
        }

        Debug.Log("TODOS los sistemas de navegación permanecen activos - sin desactivaciones");
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

        // NUEVO: Comprobar si después de la acción hay un objeto de interacción activo
        // y si es así, activar la navegación del canvas
        if (inventoryManager.HasActiveInteractionObject() && canvasIntegration != null)
        {
            // Pequeño delay para dar tiempo a que se instancie el prefab
            StartCoroutine(ActivateCanvasNavigationDelayed());
        }
    }

    /// <summary>
    /// NUEVO: Activa la navegación del canvas con un pequeño delay
    /// </summary>
    private System.Collections.IEnumerator ActivateCanvasNavigationDelayed()
    {
        // Esperar un poco para asegurar que el prefab está completamente instanciado
        yield return new WaitForSeconds(0.2f);

        // Si sigue habiendo un objeto de interacción activo, activar la navegación
        if (inventoryManager.HasActiveInteractionObject())
        {
            canvasIntegration.ForceActivateCanvasNavigation();
            Debug.Log("Navegación de canvas activada para objeto de interacción");
        }
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

    /// <summary>
    /// CORREGIDO: Solo permite activación, NUNCA desactivación
    /// </summary>
    public void EnableInventoryNavigation()
    {
        if (navigationManager != null)
        {
            navigationManager.enabled = true;

            try
            {
                var refreshMethod = navigationManager.GetType().GetMethod("ForceRefreshElements");
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

        Debug.Log("EnableInventoryNavigation ejecutado - sistema activado");
    }

    /// <summary>
    /// COMPLETAMENTE ELIMINADO: DisableInventoryNavigation ya no desactiva NADA
    /// </summary>
    public void DisableInventoryNavigation()
    {
        Debug.Log("DisableInventoryNavigation llamado - COMPLETAMENTE IGNORADO para prevenir desactivaciones problemáticas");
        // CRÍTICO: NO hacer NADA para evitar desactivaciones problemáticas
        // NO HACER: navigationManager.enabled = false;
        // NO HACER: Ninguna desactivación de ningún tipo
    }

    public void RefreshInventoryNavigation()
    {
        if (navigationManager != null)
        {
            try
            {
                var refreshMethod = navigationManager.GetType().GetMethod("ForceRefreshElements");
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

    /// <summary>
    /// NUEVO: Activa manualmente la navegación del canvas para objetos de interacción
    /// </summary>
    public void ActivateCanvasNavigationForPrefab()
    {
        if (canvasIntegration != null && inventoryManager.HasActiveInteractionObject())
        {
            canvasIntegration.ForceActivateCanvasNavigation();
            Debug.Log("Navegación de canvas activada manualmente para objeto de interacción");
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

    [ContextMenu("Force Activate Canvas Navigation")]
    public void ForceActivateCanvasNavigationFromMenu()
    {
        ActivateCanvasNavigationForPrefab();
    }

    [ContextMenu("Debug Integration State")]
    public void DebugIntegrationState()
    {
        Debug.Log($"=== INVENTORY NAVIGATION INTEGRATION STATE (FIXED - NO DEACTIVATIONS) ===");
        Debug.Log($"Inventory Manager: {inventoryManager?.name ?? "NULL"}");
        Debug.Log($"Navigation Manager: {navigationManager?.name ?? "NULL"}");
        Debug.Log($"Navigation Manager Enabled: {navigationManager?.enabled}");
        Debug.Log($"Canvas Integration: {canvasIntegration?.name ?? "NULL"}");
        Debug.Log($"Canvas Integration Active: {canvasIntegration?.IsNavigationActive()}");
        Debug.Log($"Original Inventory State: {originalInventoryState}");
        Debug.Log($"Current Inventory Open: {inventoryManager?.IsInventoryOpen()}");
        Debug.Log($"Has Active Interaction: {inventoryManager?.HasActiveInteractionObject()}");
        Debug.Log("=== TODAS LAS DESACTIVACIONES COMPLETAMENTE ELIMINADAS ===");
        Debug.Log("=== TODOS LOS SISTEMAS DE NAVEGACIÓN PERMANECEN SIEMPRE ACTIVOS ===");
    }
}