using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// SOLUCIÓN DEFINITIVA: Sistema dedicado para navegación de canvas de inventario
/// Maneja específicamente el problema del botón Submit en prefabs del inventario
/// </summary>
[RequireComponent(typeof(Inventory_Manager))]
public class InventoryCanvasIntegration : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private float detectionDelay = 0.1f;
    [SerializeField] private bool logDebugInfo = true;

    [Header("Input Configuration")]
    [SerializeField] private float submitCooldown = 0.3f;

    private Inventory_Manager inventoryManager;
    private GameObject currentActiveCanvas;
    private bool isMonitoringCanvas = false;

    // NUEVO: Sistema de Input dedicado para canvas
    private PlayerControls canvasPlayerControls;
    private InputAction submitAction;
    private InputAction navigateAction;
    private InputAction cancelAction;

    // NUEVO: Sistema de navegación dedicado
    private List<Button> canvasButtons = new List<Button>();
    private int currentButtonIndex = 0;
    private Button currentSelectedButton;
    private EventSystem eventSystem;

    // Control de Input
    private float lastSubmitTime = 0f;
    private float navigationDelay = 0.15f;
    private float lastNavigationTime = 0f;

    // Estado del sistema
    private bool isCanvasNavigationActive = false;

    private void Awake()
    {
        inventoryManager = GetComponent<Inventory_Manager>();
        eventSystem = EventSystem.current;

        // Crear sistema de Input dedicado
        SetupDedicatedInputSystem();
    }

    private void SetupDedicatedInputSystem()
    {
        canvasPlayerControls = new PlayerControls();

        // Usar las acciones de UI para canvas específicamente
        submitAction = canvasPlayerControls.UI.Submit;
        navigateAction = canvasPlayerControls.UI.Navigate;
        cancelAction = canvasPlayerControls.UI.Cancel;

        // CRÍTICO: Configurar callbacks específicos para canvas
        submitAction.performed += OnCanvasSubmit;
        navigateAction.performed += OnCanvasNavigate;
        cancelAction.performed += OnCanvasCancel;

        if (logDebugInfo)
            Debug.Log("Sistema de Input dedicado para canvas configurado");
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
        // IMPORTANTE: Solo habilitar cuando realmente lo necesitemos
        // No habilitar automáticamente para evitar conflictos
    }

    private void OnDisable()
    {
        DeactivateCanvasNavigation();
    }

    private void SetupIntegration()
    {
        if (logDebugInfo)
            Debug.Log("Configurando integración de navegación para canvas de interacción...");

        StartCoroutine(MonitorActiveCanvas());

        if (logDebugInfo)
            Debug.Log("Integración de canvas configurada correctamente");
    }

    private IEnumerator MonitorActiveCanvas()
    {
        while (true)
        {
            yield return new WaitForSeconds(detectionDelay);

            bool hasActiveInteraction = inventoryManager.HasActiveInteractionObject();

            if (hasActiveInteraction && !isMonitoringCanvas)
            {
                OnInteractionCanvasOpened();
            }
            else if (!hasActiveInteraction && isMonitoringCanvas)
            {
                OnInteractionCanvasClosed();
            }
        }
    }

    private void OnInteractionCanvasOpened()
    {
        if (logDebugInfo)
            Debug.Log("Canvas de interacción detectado - Configurando navegación específica");

        currentActiveCanvas = FindActiveInteractionCanvas();

        if (currentActiveCanvas != null)
        {
            isMonitoringCanvas = true;

            // CRÍTICO: Configurar navegación con delay para asegurar que todo esté listo
            StartCoroutine(SetupCanvasNavigationDelayed());
        }
        else
        {
            Debug.LogWarning("No se pudo encontrar el canvas de interacción activo");
        }
    }

    private IEnumerator SetupCanvasNavigationDelayed()
    {
        if (logDebugInfo)
            Debug.Log("Iniciando configuración retardada de navegación de canvas...");

        // Esperar que el canvas esté completamente configurado
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.3f); // Más tiempo para asegurar estabilidad

        // Verificar que el canvas sigue activo
        if (currentActiveCanvas == null || !currentActiveCanvas.activeInHierarchy)
        {
            if (logDebugInfo)
                Debug.Log("Canvas ya no está activo, cancelando configuración");
            yield break;
        }

        // Activar navegación dedicada para el canvas
        ActivateCanvasNavigation();
    }

    /// <summary>
    /// CRÍTICO: Activa el sistema de navegación dedicado para el canvas
    /// </summary>
    public void ActivateCanvasNavigation()
    {
        if (logDebugInfo)
            Debug.Log("Activando navegación dedicada para canvas");

        // CRÍTICO: Usar sistema de prioridades para cambiar navegación
        /*if (NavigationPriorityManager.Instance != null)
        {
            NavigationPriorityManager.Instance.SwitchToCanvasFromInventory();
        }*/

        // Recopilar botones del canvas
        CollectCanvasButtons();

        if (canvasButtons.Count == 0)
        {
            Debug.LogWarning("No se encontraron botones en el canvas");
            return;
        }

        // CRÍTICO: Habilitar Input System SOLO para canvas
        if (canvasPlayerControls != null)
        {
            canvasPlayerControls.UI.Enable();
        }

        // Seleccionar primer botón válido
        SelectFirstValidButton();

        // Marcar como activo
        isCanvasNavigationActive = true;

        if (logDebugInfo)
            Debug.Log($"Navegación de canvas activada con {canvasButtons.Count} botones");
    }

    /// <summary>
    /// NUEVO: Recopila todos los botones del canvas de forma robusta
    /// </summary>
    private void CollectCanvasButtons()
    {
        canvasButtons.Clear();

        Button[] allButtons = currentActiveCanvas.GetComponentsInChildren<Button>(true);

        foreach (Button button in allButtons)
        {
            if (button != null && button.gameObject.activeInHierarchy && button.interactable)
            {
                canvasButtons.Add(button);

                if (logDebugInfo)
                    Debug.Log($"Botón añadido a navegación: {button.name}");
            }
        }

        // IMPORTANTE: Organizar botones (botón de cierre al final para mejor UX)
        OrganizeButtonsForNavigation();
    }

    /// <summary>
    /// NUEVO: Organiza los botones para una mejor experiencia de navegación
    /// </summary>
    private void OrganizeButtonsForNavigation()
    {
        Button closeButton = null;
        List<Button> otherButtons = new List<Button>();

        foreach (Button button in canvasButtons)
        {
            if (IsCloseButton(button))
            {
                closeButton = button;
            }
            else
            {
                otherButtons.Add(button);
            }
        }

        // Reorganizar: otros botones primero, cierre al final
        canvasButtons.Clear();
        canvasButtons.AddRange(otherButtons);

        if (closeButton != null)
        {
            canvasButtons.Add(closeButton);
        }

        if (logDebugInfo && closeButton != null)
            Debug.Log($"Botón de cierre organizado al final: {closeButton.name}");
    }

    private bool IsCloseButton(Button button)
    {
        string[] possibleNames = {
            "Close_Interacted_Button",
            "CloseButton",
            "Close",
            "Exit",
            "close",
            "btn_close"
        };

        foreach (string name in possibleNames)
        {
            if (button.name.Contains(name))
            {
                return true;
            }
        }

        return false;
    }

    private void SelectFirstValidButton()
    {
        if (canvasButtons.Count > 0)
        {
            currentButtonIndex = 0;
            SelectButton(currentButtonIndex);
        }
    }

    /// <summary>
    /// CRÍTICO: Selecciona un botón y lo configura en el EventSystem
    /// </summary>
    private void SelectButton(int index)
    {
        if (index < 0 || index >= canvasButtons.Count) return;

        Button button = canvasButtons[index];
        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable) return;

        // Limpiar selección anterior
        if (currentSelectedButton != null)
        {
            // Resetear escala o efectos visuales si es necesario
            currentSelectedButton.transform.localScale = Vector3.one;
        }

        // Configurar nueva selección
        currentSelectedButton = button;
        currentButtonIndex = index;

        // CRÍTICO: Configurar en EventSystem
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(button.gameObject);
        }

        // Efecto visual de selección
        button.transform.localScale = Vector3.one * 1.1f;

        if (logDebugInfo)
            Debug.Log($"Botón seleccionado: {button.name} (índice: {index})");
    }

    #region Input Callbacks

    /// <summary>
    /// CRÍTICO: Manejo dedicado del Submit para canvas
    /// </summary>
    private void OnCanvasSubmit(InputAction.CallbackContext context)
    {
        if (!isCanvasNavigationActive || currentSelectedButton == null)
        {
            if (logDebugInfo)
                Debug.Log("Submit ignorado: navegación no activa o botón no válido");
            return;
        }

        // Control de cooldown para evitar doble activación
        if (Time.time - lastSubmitTime < submitCooldown)
        {
            if (logDebugInfo)
                Debug.Log("Submit ignorado: cooldown activo");
            return;
        }

        lastSubmitTime = Time.time;

        if (logDebugInfo)
            Debug.Log($"CANVAS SUBMIT: Ejecutando onClick en {currentSelectedButton.name}");

        try
        {
            // CRÍTICO: Ejecutar directamente el onClick del botón
            currentSelectedButton.onClick.Invoke();

            if (logDebugInfo)
                Debug.Log($"onClick ejecutado exitosamente para: {currentSelectedButton.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error ejecutando onClick en {currentSelectedButton.name}: {e.Message}");
        }
    }

    private void OnCanvasNavigate(InputAction.CallbackContext context)
    {
        if (!isCanvasNavigationActive || canvasButtons.Count <= 1) return;

        if (Time.time - lastNavigationTime < navigationDelay) return;

        Vector2 input = context.ReadValue<Vector2>();
        if (input.magnitude < 0.3f) return;

        int newIndex = currentButtonIndex;

        // Navegación simple: arriba/abajo o izquierda/derecha
        if (Mathf.Abs(input.y) > Mathf.Abs(input.x))
        {
            if (input.y > 0) // Arriba
                newIndex = (currentButtonIndex - 1 + canvasButtons.Count) % canvasButtons.Count;
            else // Abajo
                newIndex = (currentButtonIndex + 1) % canvasButtons.Count;
        }
        else
        {
            if (input.x > 0) // Derecha
                newIndex = (currentButtonIndex + 1) % canvasButtons.Count;
            else // Izquierda
                newIndex = (currentButtonIndex - 1 + canvasButtons.Count) % canvasButtons.Count;
        }

        if (newIndex != currentButtonIndex)
        {
            SelectButton(newIndex);
            lastNavigationTime = Time.time;
        }
    }

    private void OnCanvasCancel(InputAction.CallbackContext context)
    {
        if (!isCanvasNavigationActive) return;

        if (logDebugInfo)
            Debug.Log("Cancel presionado - Buscando botón de cierre");

        // Buscar y ejecutar botón de cierre
        Button closeButton = FindCloseButton();
        if (closeButton != null)
        {
            if (logDebugInfo)
                Debug.Log($"Ejecutando botón de cierre: {closeButton.name}");

            closeButton.onClick.Invoke();
        }
        else
        {
            Debug.LogWarning("No se encontró botón de cierre para Cancel");
        }
    }

    #endregion

    private Button FindCloseButton()
    {
        foreach (Button button in canvasButtons)
        {
            if (IsCloseButton(button))
            {
                return button;
            }
        }
        return null;
    }

    private void OnInteractionCanvasClosed()
    {
        if (logDebugInfo)
            Debug.Log("Canvas de interacción cerrado - Desactivando navegación");

        DeactivateCanvasNavigation();

        // Limpiar estado
        isMonitoringCanvas = false;
        currentActiveCanvas = null;
    }

    /// <summary>
    /// CRÍTICO: Desactiva la navegación específica del canvas
    /// </summary>
    private void DeactivateCanvasNavigation()
    {
        if (!isCanvasNavigationActive) return;

        if (logDebugInfo)
            Debug.Log("Desactivando navegación de canvas");

        // CRÍTICO: Usar sistema de prioridades para restaurar navegación del inventario
        /*if (NavigationPriorityManager.Instance != null)
        {
            NavigationPriorityManager.Instance.SwitchToInventoryFromCanvas();
        }*/

        // Deshabilitar Input System del canvas
        if (canvasPlayerControls != null)
        {
            canvasPlayerControls.UI.Disable();
        }

        // Limpiar selección visual
        if (currentSelectedButton != null)
        {
            currentSelectedButton.transform.localScale = Vector3.one;
            currentSelectedButton = null;
        }

        // Limpiar estado
        canvasButtons.Clear();
        currentButtonIndex = 0;
        isCanvasNavigationActive = false;

        // Limpiar EventSystem
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }

        if (logDebugInfo)
            Debug.Log("Navegación de canvas desactivada");
    }

    private GameObject FindActiveInteractionCanvas()
    {
        if (inventoryManager.prefabContainer != null)
        {
            for (int i = 0; i < inventoryManager.prefabContainer.childCount; i++)
            {
                GameObject child = inventoryManager.prefabContainer.GetChild(i).gameObject;

                if (child.activeInHierarchy)
                {
                    // Verificar si tiene Canvas o botones
                    Canvas canvas = child.GetComponent<Canvas>();
                    if (canvas == null)
                    {
                        canvas = child.GetComponentInChildren<Canvas>();
                    }

                    if (canvas != null)
                    {
                        if (logDebugInfo)
                            Debug.Log($"Canvas de interacción encontrado: {child.name}");
                        return child;
                    }

                    // Si no tiene Canvas pero tiene botones, también es válido
                    Button[] buttons = child.GetComponentsInChildren<Button>();
                    if (buttons.Length > 0)
                    {
                        if (logDebugInfo)
                            Debug.Log($"Objeto de interacción con botones encontrado: {child.name}");
                        return child;
                    }
                }
            }
        }

        return null;
    }

    #region Update Loop

    private void Update()
    {
        // Solo procesar si la navegación del canvas está activa
        if (!isCanvasNavigationActive) return;

        // Verificar que el botón seleccionado sigue siendo válido
        if (currentSelectedButton != null &&
            (!currentSelectedButton.gameObject.activeInHierarchy || !currentSelectedButton.interactable))
        {
            if (logDebugInfo)
                Debug.Log("Botón seleccionado ya no es válido, buscando alternativa");

            SelectFirstValidButton();
        }
    }

    #endregion

    #region Public Methods

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

    public bool IsNavigationActive()
    {
        return isCanvasNavigationActive;
    }

    public Button GetCurrentSelectedButton()
    {
        return currentSelectedButton;
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug Canvas Navigation")]
    public void DebugCanvasNavigation()
    {
        Debug.Log($"=== CANVAS NAVIGATION DEBUG ===");
        Debug.Log($"Is Monitoring Canvas: {isMonitoringCanvas}");
        Debug.Log($"Is Navigation Active: {isCanvasNavigationActive}");
        Debug.Log($"Current Active Canvas: {currentActiveCanvas?.name ?? "NULL"}");
        Debug.Log($"Current Selected Button: {currentSelectedButton?.name ?? "NULL"}");
        Debug.Log($"Current Button Index: {currentButtonIndex}");
        Debug.Log($"Total Canvas Buttons: {canvasButtons.Count}");
        Debug.Log($"UI Controls Enabled: {canvasPlayerControls?.UI.enabled}");

        if (canvasButtons.Count > 0)
        {
            Debug.Log("--- CANVAS BUTTONS ---");
            for (int i = 0; i < canvasButtons.Count; i++)
            {
                var button = canvasButtons[i];
                bool isSelected = (i == currentButtonIndex);
                bool isClose = IsCloseButton(button);
                Debug.Log($"[{i}] {button.name} - Selected: {isSelected}, Close: {isClose}, " +
                         $"Active: {button.gameObject.activeInHierarchy}, Interactable: {button.interactable}");
            }
        }

        Debug.Log("==============================");
    }

    [ContextMenu("Test Submit on Current Button")]
    public void TestSubmitOnCurrentButton()
    {
        if (currentSelectedButton != null)
        {
            Debug.Log($"Testing Submit on: {currentSelectedButton.name}");
            currentSelectedButton.onClick.Invoke();
        }
        else
        {
            Debug.LogWarning("No hay botón seleccionado para testear");
        }
    }

    [ContextMenu("Force Select Close Button")]
    public void ForceSelectCloseButton()
    {
        Button closeButton = FindCloseButton();
        if (closeButton != null)
        {
            int index = canvasButtons.IndexOf(closeButton);
            if (index >= 0)
            {
                SelectButton(index);
                Debug.Log($"Botón de cierre seleccionado forzosamente: {closeButton.name}");
            }
        }
        else
        {
            Debug.LogWarning("No se encontró botón de cierre");
        }
    }

    #endregion

    private void OnDestroy()
    {
        // Limpiar callbacks
        if (submitAction != null)
        {
            submitAction.performed -= OnCanvasSubmit;
        }
        if (navigateAction != null)
        {
            navigateAction.performed -= OnCanvasNavigate;
        }
        if (cancelAction != null)
        {
            cancelAction.performed -= OnCanvasCancel;
        }

        // Limpiar Input System
        canvasPlayerControls?.Dispose();

        // Parar corrutinas
        StopAllCoroutines();
    }
}