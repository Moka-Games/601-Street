using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class SafeNavigationController : MonoBehaviour
{
    [Header("Input References")]
    [Tooltip("Referencia a los controles del jugador")]
    public PlayerControls playerControls;

    [Header("Navigation Settings")]
    [Tooltip("Threshold para detectar input direccional")]
    public float navigationThreshold = 0.5f;

    [Tooltip("Tiempo mínimo entre navegaciones")]
    public float navigationCooldown = 0.2f;

    [Header("Visual Feedback")]
    [Tooltip("Material para resaltar el botón seleccionado")]
    public Material highlightMaterial;

    [Tooltip("Escala del objeto cuando está seleccionado")]
    public float highlightScale = 1.1f;

    [Tooltip("Color del highlight")]
    public Color highlightColor = Color.yellow;

    // Estado interno
    private SafeButton currentSelectedButton;
    private float lastNavigationTime;
    private bool isNavigationActive = false;

    // Mapa de botones organizados por tipo y valor
    private Dictionary<string, SafeButton> buttonMap = new Dictionary<string, SafeButton>();

    // Referencias para el highlight visual
    private Dictionary<SafeButton, ButtonHighlight> buttonHighlights = new Dictionary<SafeButton, ButtonHighlight>();

    // Referencia al SafeSystem
    private SafeSystem safeSystem;

    private struct ButtonHighlight
    {
        public Material originalMaterial;
        public Vector3 originalScale;
        public Renderer renderer;
        public Transform transform;
    }

    // Layout fijo conocido del teclado
    // Fila, Columna -> ButtonKey
    private readonly string[,] keypadLayout = new string[,]
    {
        { "1", "2", "3", "Delete" },
        { "4", "5", "6", "Enter" },
        { "7", "8", "9", "Exit" }
    };

    private void Awake()
    {
        // Obtener referencia al SafeSystem
        safeSystem = GetComponent<SafeSystem>();

        if (safeSystem == null)
        {
            Debug.LogError("SafeNavigationController debe estar en el mismo objeto que SafeSystem");
        }

        // Crear PlayerControls si no está asignado
        if (playerControls == null)
        {
            playerControls = new PlayerControls();
        }

        // Configurar los botones automáticamente
        SetupButtons();
    }

    private void OnEnable()
    {
        // Activar el ActionMap UI para navegación
        playerControls.UI.Enable();

        // Suscribirse a las acciones
        playerControls.UI.Navigate.performed += OnNavigate;
        playerControls.UI.Submit.performed += OnSubmit;
        playerControls.UI.Cancel.performed += OnCancel;
    }

    private void OnDisable()
    {
        // Desuscribirse de las acciones
        if (playerControls != null)
        {
            playerControls.UI.Navigate.performed -= OnNavigate;
            playerControls.UI.Submit.performed -= OnSubmit;
            playerControls.UI.Cancel.performed -= OnCancel;

            playerControls.UI.Disable();
        }
    }

    private void SetupButtons()
    {
        buttonMap.Clear();
        buttonHighlights.Clear();

        // Encontrar todos los SafeButton en hijos
        SafeButton[] safeButtons = GetComponentsInChildren<SafeButton>();

        foreach (SafeButton button in safeButtons)
        {
            string key = GetButtonKey(button);
            if (!string.IsNullOrEmpty(key))
            {
                buttonMap[key] = button;
                InitializeButtonHighlight(button);
            }
        }

        Debug.Log($"SafeNavigationController configurado con {buttonMap.Count} botones");
    }

    private string GetButtonKey(SafeButton button)
    {
        switch (button.buttonType)
        {
            case SafeButtonType.Number:
                return button.buttonValue;
            case SafeButtonType.Enter:
                return "Enter";
            case SafeButtonType.Delete:
                return "Delete";
            case SafeButtonType.Clear:
                return "Clear";
            case SafeButtonType.Exit:
                return "Exit";
            default:
                return null;
        }
    }

    private void InitializeButtonHighlight(SafeButton button)
    {
        var renderer = button.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = button.GetComponentInChildren<Renderer>();
        }

        if (renderer != null)
        {
            ButtonHighlight highlight = new ButtonHighlight
            {
                originalMaterial = renderer.material,
                originalScale = button.transform.localScale,
                renderer = renderer,
                transform = button.transform
            };

            buttonHighlights[button] = highlight;
        }
    }

    /// <summary>
    /// Activa o desactiva el sistema de navegación
    /// </summary>
    public void SetNavigationActive(bool active)
    {
        isNavigationActive = active;

        if (active)
        {
            // Seleccionar el botón "5" (centro) por defecto
            if (buttonMap.ContainsKey("5"))
            {
                SetSelectedButton(buttonMap["5"]);
            }
            else if (buttonMap.ContainsKey("1"))
            {
                SetSelectedButton(buttonMap["1"]);
            }
        }
        else
        {
            // Quitar highlight al desactivar
            ClearAllHighlights();
            currentSelectedButton = null;
        }
    }

    /// <summary>
    /// Maneja el input de navegación
    /// </summary>
    private void OnNavigate(InputAction.CallbackContext context)
    {
        if (!isNavigationActive || safeSystem.IsSafeUnlocked() || currentSelectedButton == null)
            return;

        Vector2 navigationInput = context.ReadValue<Vector2>();

        // Verificar threshold y cooldown
        if (navigationInput.magnitude < navigationThreshold)
            return;

        if (Time.time - lastNavigationTime < navigationCooldown)
            return;

        lastNavigationTime = Time.time;

        // Determinar dirección
        SafeButton newButton = GetButtonInDirection(currentSelectedButton, navigationInput);

        if (newButton != null && newButton != currentSelectedButton)
        {
            SetSelectedButton(newButton);

            // Reproducir sonido de navegación específico
            if (safeSystem != null)
            {
                safeSystem.PlayNavigationSound();
            }
        }
    }

    /// <summary>
    /// Obtiene el botón en la dirección especificada
    /// </summary>
    private SafeButton GetButtonInDirection(SafeButton currentButton, Vector2 direction)
    {
        // Encontrar la posición actual en el layout
        (int currentRow, int currentCol) = FindButtonPosition(currentButton);

        if (currentRow == -1 || currentCol == -1)
            return null;

        int newRow = currentRow;
        int newCol = currentCol;

        // Determinar nueva posición basada en la dirección
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Movimiento horizontal
            if (direction.x > 0) // Derecha
                newCol++;
            else // Izquierda
                newCol--;
        }
        else
        {
            // Movimiento vertical
            if (direction.y > 0) // Arriba
                newRow--;
            else // Abajo
                newRow++;
        }

        // Verificar límites y obtener el botón
        if (newRow >= 0 && newRow < keypadLayout.GetLength(0) &&
            newCol >= 0 && newCol < keypadLayout.GetLength(1))
        {
            string targetKey = keypadLayout[newRow, newCol];
            if (buttonMap.ContainsKey(targetKey))
            {
                return buttonMap[targetKey];
            }
        }

        return null; // No hay movimiento válido
    }

    /// <summary>
    /// Encuentra la posición de un botón en el layout
    /// </summary>
    private (int row, int col) FindButtonPosition(SafeButton button)
    {
        string buttonKey = GetButtonKey(button);

        for (int row = 0; row < keypadLayout.GetLength(0); row++)
        {
            for (int col = 0; col < keypadLayout.GetLength(1); col++)
            {
                if (keypadLayout[row, col] == buttonKey)
                {
                    return (row, col);
                }
            }
        }

        return (-1, -1);
    }

    /// <summary>
    /// Establece el botón seleccionado
    /// </summary>
    private void SetSelectedButton(SafeButton newButton)
    {
        if (newButton == null) return;

        // Quitar highlight del botón anterior
        if (currentSelectedButton != null)
        {
            ClearButtonHighlight(currentSelectedButton);
        }

        // Establecer nuevo botón
        currentSelectedButton = newButton;

        // Aplicar highlight al nuevo botón
        ApplyButtonHighlight(currentSelectedButton);
    }

    /// <summary>
    /// Aplica el highlight visual a un botón
    /// </summary>
    private void ApplyButtonHighlight(SafeButton button)
    {
        if (!buttonHighlights.ContainsKey(button))
            return;

        var highlight = buttonHighlights[button];

        // Cambiar material si está disponible
        if (highlightMaterial != null && highlight.renderer != null)
        {
            highlight.renderer.material = highlightMaterial;
        }
        else if (highlight.renderer != null)
        {
            // Cambiar color si no hay material personalizado
            highlight.renderer.material.color = highlightColor;
        }

        // Cambiar escala
        highlight.transform.localScale = highlight.originalScale * highlightScale;
    }

    /// <summary>
    /// Quita el highlight de un botón
    /// </summary>
    private void ClearButtonHighlight(SafeButton button)
    {
        if (!buttonHighlights.ContainsKey(button))
            return;

        var highlight = buttonHighlights[button];

        // Restaurar material original
        if (highlight.renderer != null)
        {
            highlight.renderer.material = highlight.originalMaterial;
        }

        // Restaurar escala original
        highlight.transform.localScale = highlight.originalScale;
    }

    /// <summary>
    /// Quita todos los highlights
    /// </summary>
    private void ClearAllHighlights()
    {
        foreach (var button in buttonMap.Values)
        {
            ClearButtonHighlight(button);
        }
    }

    /// <summary>
    /// Maneja la confirmación de selección
    /// </summary>
    private void OnSubmit(InputAction.CallbackContext context)
    {
        if (!isNavigationActive || safeSystem.IsSafeUnlocked() || currentSelectedButton == null)
            return;

        // Procesar el botón usando el sistema existente
        if (safeSystem != null)
        {
            safeSystem.ProcessButtonPress(currentSelectedButton);
        }
    }

    /// <summary>
    /// Maneja la cancelación (salir del modo safe)
    /// </summary>
    private void OnCancel(InputAction.CallbackContext context)
    {
        if (!isNavigationActive)
            return;

        // Buscar SafeGameplayManager para salir del modo
        SafeGameplayManager gameplayManager = FindAnyObjectByType<SafeGameplayManager>();
        if (gameplayManager != null)
        {
            gameplayManager.ExitSafeMode();
        }
    }

    /// <summary>
    /// Método público para obtener el botón actualmente seleccionado
    /// </summary>
    public SafeButton GetCurrentSelectedButton()
    {
        return currentSelectedButton;
    }

    /// <summary>
    /// Debug: Mostrar información del layout
    /// </summary>
    [ContextMenu("Mostrar Layout Debug")]
    public void ShowLayoutDebug()
    {
        Debug.Log("=== SAFE NAVIGATION LAYOUT ===");
        for (int row = 0; row < keypadLayout.GetLength(0); row++)
        {
            string rowInfo = $"Fila {row}: ";
            for (int col = 0; col < keypadLayout.GetLength(1); col++)
            {
                string key = keypadLayout[row, col];
                bool hasButton = buttonMap.ContainsKey(key);
                rowInfo += $"[{key}: {(hasButton ? "OK" : "MISSING")}] ";
            }
            Debug.Log(rowInfo);
        }
        Debug.Log($"Total botones detectados: {buttonMap.Count}");
    }
}