using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Integración automática para manejar la navegación en canvas de interacción
/// Se añade automáticamente al Inventory_Manager para gestionar transiciones de navegación
/// </summary>
[RequireComponent(typeof(Inventory_Manager))]
public class InventoryCanvasIntegration : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private float detectionDelay = 0.1f;

    private Inventory_Manager inventoryManager;
    private InteractionCanvasNavigationManager canvasNavigationManager;
    private GameObject currentActiveCanvas;
    private bool isMonitoringCanvas = false;

    // Referencias para el manejo de estados
    private InventoryNavigationManager inventoryNavigation;

    private void Awake()
    {
        inventoryManager = GetComponent<Inventory_Manager>();

        // Crear o encontrar el InteractionCanvasNavigationManager
        canvasNavigationManager = FindAnyObjectByType<InteractionCanvasNavigationManager>();
        if (canvasNavigationManager == null)
        {
            GameObject navObject = new GameObject("InteractionCanvasNavigationManager");
            canvasNavigationManager = navObject.AddComponent<InteractionCanvasNavigationManager>();

            // Hacer que persista entre escenas si es necesario
            DontDestroyOnLoad(navObject);
        }

        // Buscar InventoryNavigationManager
        inventoryNavigation = GetComponent<InventoryNavigationManager>();
    }

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupIntegration();
        }
    }

    private void SetupIntegration()
    {
        Debug.Log("Configurando integración de navegación para canvas de interacción...");

        // Iniciar monitoreo de canvas activos
        StartCoroutine(MonitorActiveCanvas());

        Debug.Log("Integración de canvas configurada correctamente");
    }

    /// <summary>
    /// Monitorea continuamente si hay un canvas de interacción activo
    /// </summary>
    private IEnumerator MonitorActiveCanvas()
    {
        while (true)
        {
            yield return new WaitForSeconds(detectionDelay);

            // Verificar si hay un objeto de interacción activo
            bool hasActiveInteraction = inventoryManager.HasActiveInteractionObject();

            if (hasActiveInteraction && !isMonitoringCanvas)
            {
                // Se acaba de abrir un canvas de interacción
                OnInteractionCanvasOpened();
            }
            else if (!hasActiveInteraction && isMonitoringCanvas)
            {
                // Se acaba de cerrar un canvas de interacción
                OnInteractionCanvasClosed();
            }
        }
    }

    private void OnInteractionCanvasOpened()
    {
        Debug.Log("Canvas de interacción detectado - Activando navegación específica");

        // Buscar el canvas activo a través del prefabContainer
        currentActiveCanvas = FindActiveInteractionCanvas();

        if (currentActiveCanvas != null)
        {
            // Activar navegación específica para el canvas
            canvasNavigationManager.ActivateForCanvas(currentActiveCanvas);
            isMonitoringCanvas = true;

            // Configurar el botón de cierre para que desactive la navegación
            SetupCloseButtonCallback(currentActiveCanvas);
        }
        else
        {
            Debug.LogWarning("No se pudo encontrar el canvas de interacción activo");
        }
    }

    private void OnInteractionCanvasClosed()
    {
        Debug.Log("Canvas de interacción cerrado - Desactivando navegación específica");

        // Desactivar navegación del canvas
        if (canvasNavigationManager != null)
        {
            canvasNavigationManager.DeactivateNavigation();
        }

        isMonitoringCanvas = false;
        currentActiveCanvas = null;
    }

    private GameObject FindActiveInteractionCanvas()
    {
        // Buscar en el prefabContainer del inventario
        if (inventoryManager.prefabContainer != null)
        {
            // Recorrer los hijos del prefabContainer
            for (int i = 0; i < inventoryManager.prefabContainer.childCount; i++)
            {
                GameObject child = inventoryManager.prefabContainer.GetChild(i).gameObject;

                if (child.activeInHierarchy)
                {
                    // Verificar si es un canvas o contiene un canvas
                    Canvas canvas = child.GetComponent<Canvas>();
                    if (canvas == null)
                    {
                        canvas = child.GetComponentInChildren<Canvas>();
                    }

                    if (canvas != null)
                    {
                        Debug.Log($"Canvas de interacción encontrado: {child.name}");
                        return child;
                    }
                }
            }
        }

        return null;
    }

    private void SetupCloseButtonCallback(GameObject canvasObject)
    {
        // Buscar el botón de cierre específico
        Button closeButton = FindCloseButtonInCanvas(canvasObject);

        if (closeButton != null)
        {
            // Añadir callback adicional para desactivar navegación
            closeButton.onClick.AddListener(() => {
                Debug.Log("Botón de cierre presionado - Desactivando navegación del canvas");

                // Pequeño delay para asegurar que otras operaciones se completen primero
                StartCoroutine(DelayedDeactivation());
            });

            Debug.Log($"Callback de cierre configurado para: {closeButton.name}");
        }
        else
        {
            Debug.LogWarning("No se encontró botón de cierre en el canvas");
        }
    }

    private IEnumerator DelayedDeactivation()
    {
        yield return new WaitForEndOfFrame();

        if (canvasNavigationManager != null)
        {
            canvasNavigationManager.DeactivateNavigation();
        }

        isMonitoringCanvas = false;
        currentActiveCanvas = null;
    }

    private Button FindCloseButtonInCanvas(GameObject canvasObject)
    {
        // Buscar por nombre específico
        string[] possibleNames = { "Close_Interacted_Button", "CloseButton", "Close", "Exit" };

        Transform[] allChildren = canvasObject.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            foreach (string name in possibleNames)
            {
                if (child.name.Contains(name))
                {
                    Button button = child.GetComponent<Button>();
                    if (button != null)
                    {
                        return button;
                    }
                }
            }
        }

        return null;
    }

    // Métodos públicos para control manual
    public void ForceActivateCanvasNavigation()
    {
        if (inventoryManager.HasActiveInteractionObject())
        {
            OnInteractionCanvasOpened();
        }
    }

    public void ForceDeactivateCanvasNavigation()
    {
        if (isMonitoringCanvas)
        {
            OnInteractionCanvasClosed();
        }
    }

    // Método para debug
    [ContextMenu("Debug Canvas Integration")]
    public void DebugIntegrationState()
    {
        Debug.Log($"=== Canvas Integration State ===");
        Debug.Log($"Is Monitoring Canvas: {isMonitoringCanvas}");
        Debug.Log($"Current Active Canvas: {currentActiveCanvas?.name ?? "NULL"}");
        Debug.Log($"Has Active Interaction: {inventoryManager?.HasActiveInteractionObject()}");
        Debug.Log($"Canvas Navigation Manager: {canvasNavigationManager?.name ?? "NULL"}");

        if (canvasNavigationManager != null)
        {
            canvasNavigationManager.DebugCurrentState();
        }
    }

    [ContextMenu("Force Activate Canvas Navigation")]
    public void ForceActivateFromContext()
    {
        ForceActivateCanvasNavigation();
    }

    [ContextMenu("Force Deactivate Canvas Navigation")]
    public void ForceDeactivateFromContext()
    {
        ForceDeactivateCanvasNavigation();
    }

    private void OnDestroy()
    {
        // Limpiar si es necesario
        StopAllCoroutines();

        if (isMonitoringCanvas && canvasNavigationManager != null)
        {
            canvasNavigationManager.DeactivateNavigation();
        }
    }
}