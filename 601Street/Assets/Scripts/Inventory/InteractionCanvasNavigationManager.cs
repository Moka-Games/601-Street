using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// Gestor de navegación específico para los canvas de interacción de ítems del inventario
/// Se activa automáticamente cuando se abre un InteractionPrefab
/// </summary>
public class InteractionCanvasNavigationManager : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float selectedScale = 1.15f;
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private DG.Tweening.Ease animationEase = DG.Tweening.Ease.OutBack;

    [Header("Auto-detección")]
    [SerializeField] private bool autoDetectButtons = true;
    [SerializeField] private string closeButtonName = "Close_Interacted_Button";

    // Sistema de Input
    private PlayerControls playerControls;

    // Referencias de navegación
    private List<Button> navigableButtons = new List<Button>();
    private int currentIndex = 0;
    private Button currentSelectedButton;
    private Button previousSelectedButton;

    // Animaciones
    private Tween currentAnimationTween;

    // Control de estado
    private bool isActive = false;
    private EventSystem eventSystem;

    // Referencias externas
    private InventoryNavigationManager inventoryNavigation;
    private UINavigationManager pauseMenuNavigation;

    private void Awake()
    {
        // Inicializar referencias
        eventSystem = EventSystem.current;

        // Configurar controles
        playerControls = new PlayerControls();
        SetupInputActions();

        // Buscar sistemas de navegación existentes
        FindNavigationSystems();

        // Inicialmente desactivado
        this.enabled = false;
    }

    private void FindNavigationSystems()
    {
        // Buscar InventoryNavigationManager
        inventoryNavigation = FindAnyObjectByType<InventoryNavigationManager>();

        // Buscar UINavigationManager (para menús de pausa, etc.)
        UINavigationManager[] uiManagers = FindObjectsByType<UINavigationManager>(FindObjectsSortMode.None);
        foreach (var manager in uiManagers)
        {
            // Tomar el que no sea el del inventario
            if (manager.gameObject != inventoryNavigation?.gameObject)
            {
                pauseMenuNavigation = manager;
                break;
            }
        }
    }

    private void SetupInputActions()
    {
        playerControls.UI.Submit.performed += OnSubmit;
        playerControls.UI.Cancel.performed += OnCancel;
        playerControls.UI.Navigate.performed += OnNavigate;
    }

    public void ActivateForCanvas(GameObject canvasObject)
    {
        Debug.Log($"Activando navegación para canvas: {canvasObject.name}");

        // Desactivar otros sistemas de navegación
        DisableOtherNavigationSystems();

        // Configurar navegación para este canvas
        SetupCanvasNavigation(canvasObject);

        // Activar este sistema
        isActive = true;
        this.enabled = true;
        playerControls.UI.Enable();

        // Seleccionar primer botón
        if (navigableButtons.Count > 0)
        {
            SelectButton(0);
        }
    }

    public void DeactivateNavigation()
    {
        Debug.Log("Desactivando navegación del canvas");

        // Limpiar animaciones
        CleanupAnimations();

        // Desactivar este sistema
        isActive = false;
        this.enabled = false;
        playerControls.UI.Disable();

        // Reactivar sistemas de navegación anteriores
        ReactivateOtherNavigationSystems();

        // Limpiar referencias
        navigableButtons.Clear();
        currentSelectedButton = null;
        previousSelectedButton = null;
    }

    private void SetupCanvasNavigation(GameObject canvasObject)
    {
        navigableButtons.Clear();

        if (autoDetectButtons)
        {
            // Auto-detectar todos los botones en el canvas
            Button[] buttons = canvasObject.GetComponentsInChildren<Button>();

            foreach (Button button in buttons)
            {
                if (button.gameObject.activeInHierarchy && button.interactable)
                {
                    navigableButtons.Add(button);
                }
            }

            Debug.Log($"Detectados {navigableButtons.Count} botones en el canvas");
        }

        // Asegurar que el botón de cierre esté al final para navegación intuitiva
        OrganizeButtons();
    }

    private void OrganizeButtons()
    {
        // Mover el botón de cierre al final de la lista si existe
        Button closeButton = null;

        for (int i = 0; i < navigableButtons.Count; i++)
        {
            if (navigableButtons[i].name == closeButtonName)
            {
                closeButton = navigableButtons[i];
                navigableButtons.RemoveAt(i);
                break;
            }
        }

        if (closeButton != null)
        {
            navigableButtons.Add(closeButton);
            Debug.Log("Botón de cierre movido al final de la lista");
        }
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        if (!isActive || navigableButtons.Count <= 1) return;

        Vector2 input = context.ReadValue<Vector2>();

        if (input.magnitude < 0.3f) return;

        int newIndex = currentIndex;

        // Navegación vertical u horizontal
        if (Mathf.Abs(input.y) > Mathf.Abs(input.x))
        {
            // Navegación vertical
            if (input.y > 0) // Arriba
                newIndex = (currentIndex - 1 + navigableButtons.Count) % navigableButtons.Count;
            else // Abajo
                newIndex = (currentIndex + 1) % navigableButtons.Count;
        }
        else
        {
            // Navegación horizontal
            if (input.x > 0) // Derecha
                newIndex = (currentIndex + 1) % navigableButtons.Count;
            else // Izquierda
                newIndex = (currentIndex - 1 + navigableButtons.Count) % navigableButtons.Count;
        }

        if (newIndex != currentIndex)
        {
            SelectButton(newIndex);
        }
    }

    private void OnSubmit(InputAction.CallbackContext context)
    {
        if (!isActive || currentSelectedButton == null) return;

        Debug.Log($"Submit presionado en: {currentSelectedButton.name}");

        // Ejecutar el onClick del botón actual
        try
        {
            currentSelectedButton.onClick.Invoke();
            Debug.Log($"onClick ejecutado correctamente para: {currentSelectedButton.name}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al ejecutar onClick en {currentSelectedButton.name}: {e.Message}");
        }
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        if (!isActive) return;

        // Buscar el botón de cierre y presionarlo
        Button closeButton = FindCloseButton();
        if (closeButton != null)
        {
            closeButton.onClick.Invoke();
        }
        else
        {
            // Si no hay botón de cierre, desactivar directamente
            Debug.LogWarning("No se encontró botón de cierre, desactivando navegación");
            DeactivateNavigation();
        }
    }

    private Button FindCloseButton()
    {
        foreach (Button button in navigableButtons)
        {
            if (button.name == closeButtonName)
            {
                return button;
            }
        }
        return null;
    }

    private void SelectButton(int index)
    {
        if (index < 0 || index >= navigableButtons.Count) return;

        Button button = navigableButtons[index];
        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable) return;

        // Actualizar referencias
        previousSelectedButton = currentSelectedButton;
        currentIndex = index;
        currentSelectedButton = button;

        // Actualizar EventSystem
        eventSystem.SetSelectedGameObject(button.gameObject);

        // Aplicar animaciones
        ApplySelectionAnimation();

        Debug.Log($"Botón seleccionado: {button.name} (índice: {index})");
    }

    private void ApplySelectionAnimation()
    {
        // Limpiar animación anterior
        currentAnimationTween?.Kill();

        // Resetear botón anterior
        if (previousSelectedButton != null && previousSelectedButton != currentSelectedButton)
        {
            previousSelectedButton.transform.DOKill();
            previousSelectedButton.transform.localScale = Vector3.one;
        }

        // Animar botón actual
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

        foreach (Button button in navigableButtons)
        {
            if (button != null)
            {
                button.transform.DOKill();
                button.transform.localScale = Vector3.one;
            }
        }
    }

    private void DisableOtherNavigationSystems()
    {
        // Desactivar navegación del inventario
        if (inventoryNavigation != null)
        {
            inventoryNavigation.SetNavigationEnabled(false);
            Debug.Log("InventoryNavigationManager desactivado");
        }

        // Desactivar navegación de menú de pausa si está activa
        if (pauseMenuNavigation != null && pauseMenuNavigation.enabled)
        {
            pauseMenuNavigation.DisableUINavigation();
            Debug.Log("UINavigationManager desactivado");
        }
    }

    private void ReactivateOtherNavigationSystems()
    {
        // Reactivar navegación del inventario si el inventario sigue abierto
        if (inventoryNavigation != null)
        {
            var inventoryManager = FindAnyObjectByType<Inventory_Manager>();
            if (inventoryManager != null && inventoryManager.IsInventoryOpen())
            {
                inventoryNavigation.SetNavigationEnabled(true);
                // Forzar refresco para actualizar selección
                inventoryNavigation.ForceRefresh();
                Debug.Log("InventoryNavigationManager reactivado");
            }
        }
    }

    private void OnDestroy()
    {
        CleanupAnimations();
        playerControls?.Dispose();
    }

    // Método público para configuración externa
    public void SetupForSpecificCanvas(GameObject canvasObject, List<Button> specificButtons = null)
    {
        if (specificButtons != null && specificButtons.Count > 0)
        {
            navigableButtons = new List<Button>(specificButtons);
        }
        else
        {
            SetupCanvasNavigation(canvasObject);
        }
    }

    // Método de debug
    [ContextMenu("Debug Canvas Navigation")]
    public void DebugCurrentState()
    {
        Debug.Log($"=== InteractionCanvas Navigation State ===");
        Debug.Log($"Is Active: {isActive}");
        Debug.Log($"Current Selected: {currentSelectedButton?.name ?? "NULL"}");
        Debug.Log($"Current Index: {currentIndex}");
        Debug.Log($"Total Buttons: {navigableButtons.Count}");

        for (int i = 0; i < navigableButtons.Count; i++)
        {
            var button = navigableButtons[i];
            Debug.Log($"  [{i}] {button?.name ?? "NULL"} - Active: {button?.gameObject.activeInHierarchy} - Interactable: {button?.interactable}");
        }
    }
}