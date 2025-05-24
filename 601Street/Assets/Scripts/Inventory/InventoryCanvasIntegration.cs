using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Integraci�n autom�tica para manejar la navegaci�n en canvas de interacci�n
/// Se a�ade autom�ticamente al Inventory_Manager para gestionar transiciones de navegaci�n
/// </summary>
[RequireComponent(typeof(Inventory_Manager))]
public class InventoryCanvasIntegration : MonoBehaviour
{
    [Header("Configuraci�n")]
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
        Debug.Log("Configurando integraci�n de navegaci�n para canvas de interacci�n...");

        // Iniciar monitoreo de canvas activos
        StartCoroutine(MonitorActiveCanvas());

        Debug.Log("Integraci�n de canvas configurada correctamente");
    }

    /// <summary>
    /// Monitorea continuamente si hay un canvas de interacci�n activo
    /// </summary>
    private IEnumerator MonitorActiveCanvas()
    {
        while (true)
        {
            yield return new WaitForSeconds(detectionDelay);

            // Verificar si hay un objeto de interacci�n activo
            bool hasActiveInteraction = inventoryManager.HasActiveInteractionObject();

            if (hasActiveInteraction && !isMonitoringCanvas)
            {
                // Se acaba de abrir un canvas de interacci�n
                OnInteractionCanvasOpened();
            }
            else if (!hasActiveInteraction && isMonitoringCanvas)
            {
                // Se acaba de cerrar un canvas de interacci�n
                OnInteractionCanvasClosed();
            }
        }
    }

    private void OnInteractionCanvasOpened()
    {
        Debug.Log("Canvas de interacci�n detectado - Activando navegaci�n espec�fica");

        // Buscar el canvas activo a trav�s del prefabContainer
        currentActiveCanvas = FindActiveInteractionCanvas();

        if (currentActiveCanvas != null)
        {
            // Activar navegaci�n espec�fica para el canvas
            canvasNavigationManager.ActivateForCanvas(currentActiveCanvas);
            isMonitoringCanvas = true;

            // Configurar el bot�n de cierre para que desactive la navegaci�n
            SetupCloseButtonCallback(currentActiveCanvas);
        }
        else
        {
            Debug.LogWarning("No se pudo encontrar el canvas de interacci�n activo");
        }
    }

    private void OnInteractionCanvasClosed()
    {
        Debug.Log("Canvas de interacci�n cerrado - Desactivando navegaci�n espec�fica");

        // Desactivar navegaci�n del canvas
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
                        Debug.Log($"Canvas de interacci�n encontrado: {child.name}");
                        return child;
                    }
                }
            }
        }

        return null;
    }

    private void SetupCloseButtonCallback(GameObject canvasObject)
    {
        // Buscar el bot�n de cierre espec�fico
        Button closeButton = FindCloseButtonInCanvas(canvasObject);

        if (closeButton != null)
        {
            // A�adir callback adicional para desactivar navegaci�n
            closeButton.onClick.AddListener(() => {
                Debug.Log("Bot�n de cierre presionado - Desactivando navegaci�n del canvas");

                // Peque�o delay para asegurar que otras operaciones se completen primero
                StartCoroutine(DelayedDeactivation());
            });

            Debug.Log($"Callback de cierre configurado para: {closeButton.name}");
        }
        else
        {
            Debug.LogWarning("No se encontr� bot�n de cierre en el canvas");
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
        // Buscar por nombre espec�fico
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

    // M�todos p�blicos para control manual
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

    // M�todo para debug
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