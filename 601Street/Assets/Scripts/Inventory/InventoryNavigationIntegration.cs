using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Integraci�n autom�tica entre Inventory_Manager e InventoryNavigationManager
/// Ahora incluye manejo autom�tico de canvas de interacci�n
/// A�ade este componente al mismo GameObject que Inventory_Manager
/// </summary>
[RequireComponent(typeof(Inventory_Manager))]
public class InventoryNavigationIntegration : MonoBehaviour
{
    [Header("Configuraci�n Autom�tica")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool enableNavigationWhenInventoryOpens = true;
    [SerializeField] private bool enableCanvasNavigation = true;

    private Inventory_Manager inventoryManager;
    private InventoryNavigationManager navigationManager;
    private InventoryCanvasIntegration canvasIntegration;
    private PlayerControls playerControls;

    // Referencias para intercambio de controles
    private bool originalInventoryState = false;

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

        // Crear InventoryCanvasIntegration si no existe y est� habilitado
        if (canvasIntegration == null && enableCanvasNavigation)
        {
            canvasIntegration = gameObject.AddComponent<InventoryCanvasIntegration>();
        }

        // Inicializar controles
        playerControls = new PlayerControls();
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
        // Configurar eventos del inventario
        SetupInventoryEvents();
    }

    private void OnDisable()
    {
        // Limpiar eventos
        CleanupInventoryEvents();
    }

    private void OnDestroy()
    {
        playerControls?.Dispose();
    }

    /// <summary>
    /// Configura la integraci�n autom�tica entre los sistemas
    /// </summary>
    public void SetupIntegration()
    {
        Debug.Log("Configurando integraci�n de navegaci�n para inventario...");

        // Auto-configurar referencias del navegador
        if (inventoryManager != null && navigationManager != null)
        {
            // Usar reflexi�n para acceder a campos privados si es necesario
            // O configurar manualmente las referencias p�blicas
            ConfigureNavigationReferences();
            ConfigureScrollRects();
        }

        // Configurar eventos
        SetupNavigationEvents();

        Debug.Log("Integraci�n de navegaci�n configurada correctamente");
    }

    private void ConfigureNavigationReferences()
    {
        // Configurar referencias usando los campos p�blicos del Inventory_Manager
        var navManager = navigationManager;

        // Usar reflexi�n para obtener las referencias privadas del inventario
        var inventoryType = typeof(Inventory_Manager);

        // Obtener noteContainer
        var noteContainerField = inventoryType.GetField("noteContainer");
        if (noteContainerField != null)
        {
            Transform noteContainer = (Transform)noteContainerField.GetValue(inventoryManager);
            SetNavigationField("noteContainer", noteContainer);
        }

        // Obtener objectContainer  
        var objectContainerField = inventoryType.GetField("objectContainer");
        if (objectContainerField != null)
        {
            Transform objectContainer = (Transform)objectContainerField.GetValue(inventoryManager);
            SetNavigationField("objectContainer", objectContainer);
        }

        // Auto-detectar ScrollRects
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
        // Buscar ScrollRects en la interfaz del inventario
        if (inventoryManager.InventoryInterface != null)
        {
            ScrollRect[] scrolls = inventoryManager.InventoryInterface.GetComponentsInChildren<ScrollRect>();

            // Asignar autom�ticamente los primeros dos ScrollRects encontrados
            if (scrolls.Length >= 1)
            {
                SetNavigationField("noteScrollRect", scrolls[0]);
            }

            if (scrolls.Length >= 2)
            {
                SetNavigationField("objectScrollRect", scrolls[1]);
            }

            Debug.Log($"Detectados {scrolls.Length} ScrollRects autom�ticamente");
        }
    }

    private void SetupInventoryEvents()
    {
        // Detectar cuando se abre/cierra el inventario
        if (enableNavigationWhenInventoryOpens)
        {
            // Usar un patr�n de observaci�n simple
            InvokeRepeating(nameof(CheckInventoryState), 0.1f, 0.1f);
        }
    }

    private void CleanupInventoryEvents()
    {
        CancelInvoke(nameof(CheckInventoryState));
    }

    /// <summary>
    /// Verifica cambios en el estado del inventario
    /// </summary>
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

    private void OnInventoryOpened()
    {
        Debug.Log("Inventario abierto - Habilitando navegaci�n");

        // Cambiar a controles de UI
        playerControls.Gameplay.Disable();
        playerControls.UI.Enable();

        // Habilitar navegaci�n del inventario
        if (navigationManager != null)
        {
            navigationManager.SetNavigationEnabled(true);

            // Forzar refresco para detectar elementos actuales
            navigationManager.ForceRefresh();
        }
    }

    private void OnInventoryClosed()
    {
        Debug.Log("Inventario cerrado - Deshabilitando navegaci�n");

        // Volver a controles de gameplay
        playerControls.UI.Disable();
        playerControls.Gameplay.Enable();

        // Deshabilitar navegaci�n del inventario
        if (navigationManager != null)
        {
            navigationManager.SetNavigationEnabled(false);
        }
    }

    private void SetupNavigationEvents()
    {
        if (navigationManager != null)
        {
            // Suscribirse a eventos de navegaci�n
            navigationManager.OnElementSelected += OnNavigationElementSelected;
            navigationManager.OnElementSubmitted += OnNavigationElementSubmitted;
            navigationManager.OnSectionChanged += OnNavigationSectionChanged;
        }
    }

    private void OnNavigationElementSelected(Button selectedButton)
    {
        Debug.Log($"Elemento seleccionado por navegaci�n: {selectedButton.name}");

        // Aqu� puedes a�adir l�gica adicional cuando se selecciona un elemento
        // Por ejemplo, mostrar informaci�n del �tem, actualizar UI, etc.
    }

    private void OnNavigationElementSubmitted(Button submittedButton)
    {
        Debug.Log($"Elemento confirmado por navegaci�n: {submittedButton.name}");

        // El bot�n ya se presiona autom�ticamente, pero puedes a�adir efectos adicionales
        // Por ejemplo, animaciones especiales, sonidos, etc.
    }

    private void OnNavigationSectionChanged(InventoryNavigationManager.InventorySection newSection)
    {
        Debug.Log($"Secci�n cambiada a: {newSection}");

        // Aqu� puedes a�adir l�gica cuando cambia de secci�n
        // Por ejemplo, cambiar el color de un indicador, actualizar texto, etc.
    }

    // M�todos p�blicos para control manual
    public void EnableInventoryNavigation()
    {
        if (navigationManager != null)
        {
            navigationManager.SetNavigationEnabled(true);
            navigationManager.ForceRefresh();
        }
    }

    public void DisableInventoryNavigation()
    {
        if (navigationManager != null)
        {
            navigationManager.SetNavigationEnabled(false);
        }
    }

    public void RefreshInventoryNavigation()
    {
        if (navigationManager != null)
        {
            navigationManager.ForceRefresh();
        }
    }

    // M�todo para forzar configuraci�n manual si la autom�tica falla
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
            navigationManager.DebugCurrentState();
        }
    }

    [ContextMenu("Force Refresh Navigation")]
    public void ForceRefreshNavigation()
    {
        RefreshInventoryNavigation();
    }

    private void ConfigureScrollRects()
    {
        // Verificar que los ScrollRects est�n correctamente asignados
        if (navigationManager.noteScrollRect == null || navigationManager.objectScrollRect == null)
        {
            Debug.LogWarning("ScrollRects no est�n asignados. Intentando detecci�n autom�tica...");
            DetectScrollRects();
        }

        // Configurar par�metros de scroll
        navigationManager.SetScrollEnabled(true);
        navigationManager.SetScrollSensitivity(2.0f);
        navigationManager.SetScrollDeadZone(0.3f);

        Debug.Log("Configuraci�n de scroll completada");
    }
}