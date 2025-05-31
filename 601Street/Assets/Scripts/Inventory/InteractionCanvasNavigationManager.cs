using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// VERSIÓN CORREGIDA: Eliminadas todas las desactivaciones problemáticas
/// Solo maneja activación de navegación, nunca desactivación
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

    private void Awake()
    {
        // Inicializar referencias
        eventSystem = EventSystem.current;

        // Configurar controles
        playerControls = new PlayerControls();
        SetupInputActions();

        // CORREGIDO: Inicialmente habilitado y listo
        this.enabled = true;
    }

    private void SetupInputActions()
    {
        playerControls.UI.Submit.performed += OnSubmit;
        playerControls.UI.Cancel.performed += OnCancel;
        playerControls.UI.Navigate.performed += OnNavigate;
    }

    private void OnEnable()
    {
        // CORREGIDO: Siempre habilitar UI si está disponible
        if (playerControls != null)
        {
            playerControls.UI.Enable();
        }
    }

    private void OnDisable()
    {
        // CORREGIDO: Solo desactivar si realmente se está destruyendo
        // No hacer nada aquí para evitar desactivaciones problemáticas
    }

    public void ActivateForCanvas(GameObject canvasObject)
    {
        Debug.Log($"Activando navegación para canvas: {canvasObject.name}");

        // CORREGIDO: Solo configurar navegación, sin afectar otros sistemas
        SetupCanvasNavigation(canvasObject);

        // Activar este sistema
        isActive = true;
        this.enabled = true;

        // CORREGIDO: Asegurar que UI esté habilitado
        if (playerControls != null)
        {
            playerControls.UI.Enable();
            Debug.Log("InteractionCanvas: UI actions enabled para canvas específico");
        }

        // Seleccionar primer botón
        if (navigableButtons.Count > 0)
        {
            SelectButton(0);
        }
    }

    /// <summary>
    /// CORREGIDO: DeactivateNavigation ya no desactiva para evitar problemas
    /// </summary>
    public void DeactivateNavigation()
    {
        Debug.LogWarning("DeactivateNavigation llamado - IGNORADO para prevenir problemas");

        // CORREGIDO: NO desactivar navegación para evitar conflictos
        // Solo limpiar animaciones y referencias locales

        // Limpiar animaciones
        CleanupAnimations();

        // CORREGIDO: NO desactivar UI actions
        // if (playerControls != null)
        // {
        //     playerControls.UI.Disable();
        // }

        // Marcar como inactivo pero mantener componente habilitado
        isActive = false;
        // this.enabled = false; // ← ELIMINADO para evitar desactivaciones

        // Limpiar referencias locales
        navigableButtons.Clear();
        currentSelectedButton = null;
        previousSelectedButton = null;

        Debug.Log("Navegación del canvas marcada como inactiva (pero mantenida habilitada)");
    }

    private void SetupCanvasNavigation(GameObject canvasObject)
    {
        navigableButtons.Clear();

        if (autoDetectButtons)
        {
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

        OrganizeButtons();
    }

    private void OrganizeButtons()
    {
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

        if (Mathf.Abs(input.y) > Mathf.Abs(input.x))
        {
            if (input.y > 0)
                newIndex = (currentIndex - 1 + navigableButtons.Count) % navigableButtons.Count;
            else
                newIndex = (currentIndex + 1) % navigableButtons.Count;
        }
        else
        {
            if (input.x > 0)
                newIndex = (currentIndex + 1) % navigableButtons.Count;
            else
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
        if (!isActive)
        {
            Debug.Log("Cancel recibido pero canvas no está activo - ignorando");
            return;
        }

        Debug.Log($"Cancel recibido en InteractionCanvas - Cerrando canvas");

        Button closeButton = FindCloseButton();
        if (closeButton != null)
        {
            Debug.Log("Ejecutando cierre del canvas de interacción");
            closeButton.onClick.Invoke();
        }
        else
        {
            Debug.LogWarning("No se encontró botón de cierre");
            // CORREGIDO: NO desactivar navegación automáticamente
            // DeactivateNavigation();
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

        previousSelectedButton = currentSelectedButton;
        currentIndex = index;
        currentSelectedButton = button;

        eventSystem.SetSelectedGameObject(button.gameObject);
        ApplySelectionAnimation();

        Debug.Log($"Botón seleccionado: {button.name} (índice: {index})");
    }

    private void ApplySelectionAnimation()
    {
        currentAnimationTween?.Kill();

        if (previousSelectedButton != null && previousSelectedButton != currentSelectedButton)
        {
            previousSelectedButton.transform.DOKill();
            previousSelectedButton.transform.localScale = Vector3.one;
        }

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

    private void OnDestroy()
    {
        CleanupAnimations();

        // Limpiar suscripciones
        if (playerControls != null)
        {
            playerControls.UI.Submit.performed -= OnSubmit;
            playerControls.UI.Cancel.performed -= OnCancel;
            playerControls.UI.Navigate.performed -= OnNavigate;
        }

        playerControls?.Dispose();
    }

    public bool IsHandlingInputs()
    {
        return isActive;
    }

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

    [ContextMenu("Debug Canvas Navigation")]
    public void DebugCurrentState()
    {
        Debug.Log($"=== InteractionCanvas Navigation State (FIXED) ===");
        Debug.Log($"Is Active: {isActive}");
        Debug.Log($"Component Enabled: {enabled}");
        Debug.Log($"Current Selected: {currentSelectedButton?.name ?? "NULL"}");
        Debug.Log($"Current Index: {currentIndex}");
        Debug.Log($"Total Buttons: {navigableButtons.Count}");
        Debug.Log($"UI Controls Enabled: {(playerControls?.UI.enabled ?? false)}");

        for (int i = 0; i < navigableButtons.Count; i++)
        {
            var button = navigableButtons[i];
            Debug.Log($"  [{i}] {button?.name ?? "NULL"} - Active: {button?.gameObject.activeInHierarchy} - Interactable: {button?.interactable}");
        }

        Debug.Log("=== DESACTIVACIONES ELIMINADAS ===");
    }
}