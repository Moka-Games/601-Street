using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// Sistema de navegación ESPECÍFICO para el inventario - INDEPENDIENTE de UINavigationManager
/// Maneja SOLO la navegación dentro del inventario usando el Input System
/// </summary>
public class InventoryNavigationManager : MonoBehaviour
{
    [Header("Contenedores del Inventario")]
    [SerializeField] private Transform noteContainer;
    [SerializeField] private Transform objectContainer;

    [Header("Configuración de Navegación")]
    [SerializeField] private int elementsPerRow = 6;
    [SerializeField] private float navigationDelay = 0.15f;

    [Header("Animaciones")]
    [SerializeField] private float selectedScale = 1.15f;
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private DG.Tweening.Ease animationEase = DG.Tweening.Ease.OutBack;

    [Header("Debug")]
    [SerializeField] private bool logNavigationActions = true;

    // Input del inventario - SEPARADO del UINavigationManager  
    private PlayerControls playerControls;
    private InputAction navigateAction;
    private InputAction submitAction;

    // Lista de elementos navegables del inventario
    private List<Button> inventoryButtons = new List<Button>();
    private int currentIndex = 0;
    private Button currentSelectedButton;
    private Button previousSelectedButton;

    // Control de tiempo
    private float lastNavigationTime;

    // Referencias del sistema
    private EventSystem eventSystem;
    private Tween currentAnimationTween;

    // Estado del sistema
    private bool isActive = false;
    private LayoutGroup noteLayoutGroup;
    private LayoutGroup objectLayoutGroup;

    #region Inicialización

    private void Awake()
    {
        // Configurar Input System ESPECÍFICO para inventario
        InitializeInputSystem();

        // Obtener EventSystem
        eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("InventoryNavigationManager: No se encontró EventSystem");
        }

        // Inicialmente desactivado
        this.enabled = false;
    }

    private void InitializeInputSystem()
    {
        playerControls = new PlayerControls();

        // IMPORTANTE: Usar acciones ESPECÍFICAS de UI para el inventario
        navigateAction = playerControls.UI.Navigate;
        submitAction = playerControls.UI.Submit;

        // Configurar callbacks
        submitAction.performed += OnSubmitInventory;

        if (logNavigationActions)
            Debug.Log("InventoryNavigationManager: Input System inicializado INDEPENDIENTEMENTE");
    }

    private void Start()
    {
        // Detectar contenedores automáticamente si no están asignados
        DetectContainers();

        // Detectar LayoutGroups para organización
        DetectLayoutGroups();
    }

    private void DetectContainers()
    {
        if (noteContainer == null || objectContainer == null)
        {
            Inventory_Manager inventoryManager = FindAnyObjectByType<Inventory_Manager>();
            if (inventoryManager != null)
            {
                noteContainer = inventoryManager.noteContainer;
                objectContainer = inventoryManager.objectContainer;

                if (logNavigationActions)
                    Debug.Log("InventoryNavigationManager: Contenedores detectados automáticamente");
            }
        }
    }

    private void DetectLayoutGroups()
    {
        if (noteContainer != null)
            noteLayoutGroup = noteContainer.GetComponent<LayoutGroup>();
        if (objectContainer != null)
            objectLayoutGroup = objectContainer.GetComponent<LayoutGroup>();
    }

    #endregion

    #region Activación/Desactivación del Sistema

    /// <summary>
    /// Activa el sistema de navegación del inventario
    /// </summary>
    public void ActivateInventoryNavigation()
    {
        if (isActive) return;

        if (logNavigationActions)
            Debug.Log("InventoryNavigationManager: Activando navegación del inventario");

        // Habilitar el sistema
        isActive = true;
        this.enabled = true;

        // Habilitar SOLO las acciones UI del inventario
        playerControls.UI.Enable();

        // Recopilar elementos del inventario
        RefreshInventoryElements();

        // Seleccionar primer elemento
        StartCoroutine(SelectFirstElementDelayed());
    }

    /// <summary>
    /// Desactiva el sistema de navegación del inventario
    /// </summary>
    public void DeactivateInventoryNavigation()
    {
        if (!isActive) return;

        if (logNavigationActions)
            Debug.Log("InventoryNavigationManager: Desactivando navegación del inventario");

        // Limpiar animaciones
        CleanupAnimations();

        // Deshabilitar inputs UI
        playerControls.UI.Disable();

        // Desactivar el sistema
        isActive = false;
        this.enabled = false;

        // Limpiar selección
        ClearSelection();
    }

    #endregion

    #region Gestión de Elementos

    /// <summary>
    /// Recopila todos los botones del inventario
    /// </summary>
    public void RefreshInventoryElements()
    {
        inventoryButtons.Clear();

        // Recopilar botones de notas
        if (noteContainer != null)
        {
            CollectButtonsFromContainer(noteContainer);
        }

        // Recopilar botones de objetos
        if (objectContainer != null)
        {
            CollectButtonsFromContainer(objectContainer);
        }

        if (logNavigationActions)
            Debug.Log($"InventoryNavigationManager: {inventoryButtons.Count} elementos recopilados");

        // Resetear índice si es necesario
        if (currentIndex >= inventoryButtons.Count)
        {
            currentIndex = 0;
        }
    }

    private void CollectButtonsFromContainer(Transform container)
    {
        foreach (Transform child in container)
        {
            if (child.gameObject.activeInHierarchy)
            {
                Button button = child.GetComponent<Button>();
                if (button != null && button.interactable)
                {
                    inventoryButtons.Add(button);
                }
            }
        }
    }

    private IEnumerator SelectFirstElementDelayed()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);

        if (inventoryButtons.Count > 0)
        {
            SelectButton(0);
        }
    }

    #endregion

    #region Navegación

    private void Update()
    {
        if (!isActive || !enabled) return;

        ProcessNavigation();
    }

    private void ProcessNavigation()
    {
        Vector2 navigationInput = navigateAction.ReadValue<Vector2>();

        if (navigationInput.magnitude < 0.3f || inventoryButtons.Count <= 1) return;

        if (Time.time - lastNavigationTime < navigationDelay) return;

        int newIndex = currentIndex;

        // Navegación basada en grid
        if (Mathf.Abs(navigationInput.x) > Mathf.Abs(navigationInput.y))
        {
            // Movimiento horizontal
            if (navigationInput.x > 0) // Derecha
                newIndex = GetNextIndexHorizontal(1);
            else // Izquierda
                newIndex = GetNextIndexHorizontal(-1);
        }
        else
        {
            // Movimiento vertical
            if (navigationInput.y > 0) // Arriba (en UI es negativo)
                newIndex = GetNextIndexVertical(-1);
            else // Abajo
                newIndex = GetNextIndexVertical(1);
        }

        if (newIndex != currentIndex && newIndex >= 0 && newIndex < inventoryButtons.Count)
        {
            SelectButton(newIndex);
            lastNavigationTime = Time.time;
        }
    }

    private int GetNextIndexHorizontal(int direction)
    {
        int row = currentIndex / elementsPerRow;
        int col = currentIndex % elementsPerRow;

        int newCol = col + direction;

        // Verificar límites de la fila
        if (newCol < 0)
        {
            // Ir al último elemento de la fila
            newCol = elementsPerRow - 1;
            while (row * elementsPerRow + newCol >= inventoryButtons.Count && newCol > col)
            {
                newCol--;
            }
        }
        else if (newCol >= elementsPerRow || row * elementsPerRow + newCol >= inventoryButtons.Count)
        {
            // Ir al primer elemento de la fila
            newCol = 0;
        }

        return row * elementsPerRow + newCol;
    }

    private int GetNextIndexVertical(int direction)
    {
        int newIndex = currentIndex + (direction * elementsPerRow);

        // Wrap around vertical
        if (newIndex < 0)
        {
            // Ir a la última fila posible en la misma columna
            int col = currentIndex % elementsPerRow;
            int lastRow = (inventoryButtons.Count - 1) / elementsPerRow;
            newIndex = lastRow * elementsPerRow + col;

            // Ajustar si se pasa del límite
            if (newIndex >= inventoryButtons.Count)
            {
                newIndex = inventoryButtons.Count - 1;
            }
        }
        else if (newIndex >= inventoryButtons.Count)
        {
            // Ir a la primera fila en la misma columna
            int col = currentIndex % elementsPerRow;
            newIndex = col;
        }

        return newIndex;
    }

    private void SelectButton(int index)
    {
        if (index < 0 || index >= inventoryButtons.Count) return;

        Button button = inventoryButtons[index];
        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable) return;

        // Actualizar selección
        previousSelectedButton = currentSelectedButton;
        currentIndex = index;
        currentSelectedButton = button;

        // Actualizar EventSystem
        eventSystem?.SetSelectedGameObject(button.gameObject);

        // Aplicar animaciones
        ApplySelectionAnimation();

        if (logNavigationActions)
            Debug.Log($"InventoryNavigationManager: Seleccionado {button.name} (índice: {index})");
    }

    #endregion

    #region Input Callbacks

    private void OnSubmitInventory(InputAction.CallbackContext context)
    {
        if (!isActive) return;

        if (currentSelectedButton != null &&
            currentSelectedButton.gameObject.activeInHierarchy &&
            currentSelectedButton.interactable)
        {
            if (logNavigationActions)
                Debug.Log($"InventoryNavigationManager: Ejecutando acción en {currentSelectedButton.name}");

            // Ejecutar la acción del botón
            currentSelectedButton.onClick.Invoke();
        }
        else
        {
            if (logNavigationActions)
                Debug.LogWarning("InventoryNavigationManager: No hay botón válido seleccionado para Submit");

            // Intentar refrescar y seleccionar un elemento válido
            RefreshInventoryElements();
            if (inventoryButtons.Count > 0)
            {
                SelectButton(0);
            }
        }
    }

    #endregion

    #region Animaciones

    private void ApplySelectionAnimation()
    {
        // Limpiar animaciones anteriores
        currentAnimationTween?.Kill();

        // Reset elemento anterior
        if (previousSelectedButton != null && previousSelectedButton != currentSelectedButton)
        {
            previousSelectedButton.transform.DOKill();
            previousSelectedButton.transform.localScale = Vector3.one;
        }

        // Animar elemento actual
        if (currentSelectedButton != null)
        {
            currentSelectedButton.transform.localScale = Vector3.one;
            currentAnimationTween = currentSelectedButton.transform
                .DOScale(Vector3.one * selectedScale, animationDuration)
                .SetEase(animationEase)
                .SetUpdate(true);
        }
    }

    private void CleanupAnimations()
    {
        currentAnimationTween?.Kill();

        foreach (Button button in inventoryButtons)
        {
            if (button != null)
            {
                button.transform.DOKill();
                button.transform.localScale = Vector3.one;
            }
        }
    }

    private void ClearSelection()
    {
        currentSelectedButton = null;
        previousSelectedButton = null;
        eventSystem?.SetSelectedGameObject(null);
    }

    #endregion

    #region Eventos del Sistema

    private void OnEnable()
    {
        if (isActive)
        {
            playerControls?.UI.Enable();
        }
    }

    private void OnDisable()
    {
        playerControls?.UI.Disable();
        CleanupAnimations();
    }

    private void OnDestroy()
    {
        CleanupAnimations();

        if (submitAction != null)
        {
            submitAction.performed -= OnSubmitInventory;
        }

        playerControls?.Dispose();
    }

    #endregion

    #region Métodos Públicos

    /// <summary>
    /// Fuerza la actualización de elementos del inventario
    /// </summary>
    public void ForceRefreshElements()
    {
        if (isActive)
        {
            RefreshInventoryElements();

            // Reseleccionar un elemento válido
            if (inventoryButtons.Count > 0)
            {
                int newIndex = Mathf.Clamp(currentIndex, 0, inventoryButtons.Count - 1);
                SelectButton(newIndex);
            }
        }
    }

    /// <summary>
    /// Configura los contenedores manualmente
    /// </summary>
    public void SetContainers(Transform noteContainer, Transform objectContainer)
    {
        this.noteContainer = noteContainer;
        this.objectContainer = objectContainer;

        DetectLayoutGroups();

        if (isActive)
        {
            RefreshInventoryElements();
        }
    }

    /// <summary>
    /// Verifica si el sistema está activo
    /// </summary>
    public bool IsNavigationActive()
    {
        return isActive && enabled;
    }

    #endregion

    #region Debug

    [ContextMenu("Debug Inventory Navigation")]
    public void DebugInventoryNavigation()
    {
        Debug.Log($"=== INVENTORY NAVIGATION STATE ===");
        Debug.Log($"Is Active: {isActive}");
        Debug.Log($"Component Enabled: {enabled}");
        Debug.Log($"Current Selected: {currentSelectedButton?.name ?? "NULL"}");
        Debug.Log($"Current Index: {currentIndex}");
        Debug.Log($"Total Buttons: {inventoryButtons.Count}");
        Debug.Log($"Note Container: {noteContainer?.name ?? "NULL"}");
        Debug.Log($"Object Container: {objectContainer?.name ?? "NULL"}");

        for (int i = 0; i < inventoryButtons.Count; i++)
        {
            var button = inventoryButtons[i];
            Debug.Log($"  [{i}] {button?.name ?? "NULL"} - Active: {button?.gameObject.activeInHierarchy} - Interactable: {button?.interactable}");
        }
    }

    [ContextMenu("Force Refresh Elements")]
    public void ForceRefreshElementsFromContext()
    {
        ForceRefreshElements();
    }

    [ContextMenu("Test Navigation")]
    public void TestNavigation()
    {
        if (!isActive)
        {
            ActivateInventoryNavigation();
        }
        else
        {
            DeactivateInventoryNavigation();
        }
    }

    #endregion
}