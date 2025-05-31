using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// VERSIÓN CORREGIDA: Controla el menú de pausa del juego usando NavigationPriorityManager
/// para evitar conflictos con otros sistemas de navegación
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject controlsUI; // Prefab o panel de controles

    [Header("Configuración")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float delayBeforeSceneChange = 0.5f;

    // Input System
    private PlayerControls playerControls;
    private InputAction pauseAction;

    // Estado del juego
    private bool gamePaused = false;

    // Referencias a otros sistemas
    private GameSceneManager sceneManager;
    private GameStateManager stateManager;
    private Camera_Script cameraScript;
    private PlayerController playerController;
    private Enabler enabler;

    private void Awake()
    {
        InitializeInputSystem();

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        if (controlsUI != null)
        {
            controlsUI.SetActive(false);
        }

        FindManagerReferences();
    }

    private void Start()
    {
        gamePaused = false;
        Time.timeScale = 1f;

        // Registrarse en el NavigationPriorityManager
        RegisterWithPriorityManager();
    }

    private void RegisterWithPriorityManager()
    {
        if (NavigationPriorityManager.Instance != null)
        {
            // Buscar el UINavigationManager asociado a este menú de pausa
            UINavigationManager uiNavManager = GetComponent<UINavigationManager>();
            if (uiNavManager == null)
            {
                uiNavManager = GetComponentInChildren<UINavigationManager>();
            }

            if (uiNavManager != null)
            {
                NavigationPriorityManager.Instance.RegisterSystem(
                    "PauseMenu",
                    NavigationPriorityManager.NavigationPriority.PauseMenu,
                    uiNavManager, null, null
                );
                Debug.Log("PauseMenu registrado en el NavigationPriorityManager");
            }
            else
            {
                Debug.LogWarning("PauseMenu: No se encontró UINavigationManager asociado");
            }
        }
        else
        {
            Debug.LogWarning("NavigationPriorityManager no encontrado para PauseMenu");
        }
    }

    private void InitializeInputSystem()
    {
        playerControls = new PlayerControls();
        pauseAction = playerControls.Gameplay.Pause;
        pauseAction.performed += OnPauseInput;
    }

    private void OnEnable()
    {
        playerControls?.Gameplay.Enable();
    }

    private void OnDisable()
    {
        playerControls?.Gameplay.Disable();
    }

    private void OnDestroy()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPauseInput;
        }
        playerControls?.Dispose();
    }

    private void OnPauseInput(InputAction.CallbackContext context)
    {
        if (sceneManager != null && sceneManager.IsTransitioning())
            return;

        TogglePause();
    }

    /// <summary>
    /// Activa o desactiva el estado de pausa del juego
    /// </summary>
    public void TogglePause()
    {
        if (gamePaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// VERSIÓN CORREGIDA: Reanuda el juego usando NavigationPriorityManager
    /// </summary>
    public void ResumeGame()
    {
        if (!gamePaused)
            return;

        // Ocultar menús
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        if (controlsUI != null)
            controlsUI.SetActive(false);

        // NO DESACTIVAR NAVEGACIÓN
        // Solo restaurar tiempo y estado del juego
        Time.timeScale = 1f;
        gamePaused = false;
        RestoreGameState();

        if (stateManager != null)
            stateManager.EnterGameplayState();
    }

    /// <summary>
    /// VERSIÓN CORREGIDA: Pausa el juego usando NavigationPriorityManager
    /// </summary>
    public void PauseGame()
    {
        if (gamePaused)
            return;

        // Verificar que no estemos en medio de una transición o diálogo
        if (stateManager != null &&
            (stateManager.CurrentState == GameState.OnDialogue ||
             stateManager.CurrentState == GameState.OnInteracting))
        {
            Debug.Log("No se puede pausar durante un diálogo o interacción");
            return;
        }

        // Mostrar el menú de pausa
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }

        // CAMBIO IMPORTANTE: Activar navegación del menú de pausa usando el sistema de prioridades
        // Esto automáticamente desactivará sistemas de menor prioridad y los restaurará al cerrar
        if (NavigationPriorityManager.Instance != null)
        {
            NavigationPriorityManager.Instance.ActivatePauseMenuNavigation();
            Debug.Log("PauseMenu: Navegación activada mediante NavigationPriorityManager");
        }
        else
        {
            // Fallback al método directo
            UINavigationManager uiNavManager = GetComponent<UINavigationManager>();
            if (uiNavManager != null)
            {
                uiNavManager.EnableUINavigation();
            }
            Debug.LogWarning("PauseMenu: Usando activación directa (sin NavigationPriorityManager)");
        }

        // Detener el tiempo
        Time.timeScale = 0f;
        gamePaused = true;

        // Bloquear al jugador durante la pausa
        BlockPlayerDuringPause();

        Debug.Log("Juego pausado");
    }

    /// <summary>
    /// Vuelve al menú principal descargando todas las escenas
    /// </summary>
    public void BackToMainMenu()
    {
        // Restablecer la escala de tiempo antes de cambiar de escena
        Time.timeScale = 1f;
        gamePaused = false;

        // IMPORTANTE: Desactivar navegación antes de cambiar de escena
        if (NavigationPriorityManager.Instance != null)
        {
            NavigationPriorityManager.Instance.DeactivatePauseMenuNavigation();
        }

        StartCoroutine(CleanupAndLoadMainMenu());
    }

    /// <summary>
    /// Muestra el panel de controles
    /// </summary>
    public void ShowControlsUI()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        if (controlsUI != null)
        {
            controlsUI.SetActive(true);
        }
    }

    /// <summary>
    /// Oculta el panel de controles y vuelve al menú de pausa
    /// </summary>
    public void HideControlsUI()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }

        if (controlsUI != null)
        {
            controlsUI.SetActive(false);
        }
    }

    private System.Collections.IEnumerator CleanupAndLoadMainMenu()
    {
        // Desactivar menús
        if (pauseMenuUI != null) pauseMenuUI.SetActive(false);
        if (controlsUI != null) controlsUI.SetActive(false);

        // Restaurar estado del juego por seguridad
        RestoreGameState();

        // Esperar antes de iniciar la transición
        yield return new WaitForSecondsRealtime(delayBeforeSceneChange);

        // Desuscribir eventos y limpiar referencias por seguridad
        if (stateManager != null)
        {
            // Desuscribir de eventos si es necesario
        }

        Debug.Log("Cargando menú principal mediante carga de escena directa");

        // Opcionalmente mostrar una pantalla de carga
        FadeManager fadeManager = FindFirstObjectByType<FadeManager>();
        if (fadeManager != null)
        {
            fadeManager.FadeIn(0.5f);
            yield return new WaitForSecondsRealtime(0.5f);
        }

        // Esto descargará TODAS las escenas activas (incluida la persistente) y cargará solo el menú
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void FindManagerReferences()
    {
        sceneManager = GameSceneManager.Instance;
        if (sceneManager == null)
        {
            sceneManager = FindFirstObjectByType<GameSceneManager>();
            Debug.LogWarning("PauseMenu: GameSceneManager no encontrado mediante Instance. Buscando mediante FindObjectOfType.");
        }

        stateManager = GameStateManager.Instance;
        if (stateManager == null)
        {
            stateManager = FindFirstObjectByType<GameStateManager>();
            Debug.LogWarning("PauseMenu: GameStateManager no encontrado mediante Instance. Buscando mediante FindObjectOfType.");
        }

        cameraScript = FindFirstObjectByType<Camera_Script>();
        playerController = FindFirstObjectByType<PlayerController>();
        enabler = Enabler.Instance;
    }

    private void BlockPlayerDuringPause()
    {
        if (enabler != null)
        {
            enabler.BlockPlayer();
            return;
        }

        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
        }

        if (cameraScript != null)
        {
            cameraScript.FreezeCamera();
        }
    }

    private void RestoreGameState()
    {
        if (enabler != null)
        {
            enabler.ReleasePlayer();
            return;
        }

        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
        }

        if (cameraScript != null)
        {
            cameraScript.UnfreezeCamera();
        }
    }

    // Métodos públicos sin cambios
    public bool IsGamePaused()
    {
        return gamePaused;
    }

    public void ForcePause()
    {
        if (!gamePaused)
        {
            PauseGame();
        }
    }

    public void ForceResume()
    {
        if (gamePaused)
        {
            ResumeGame();
        }
    }

    public void SetPauseInputEnabled(bool enabled)
    {
        if (pauseAction != null)
        {
            if (enabled)
            {
                pauseAction.Enable();
            }
            else
            {
                pauseAction.Disable();
            }
        }
    }

    /// <summary>
    /// Método de debug para verificar el estado
    /// </summary>
    [ContextMenu("Debug Pause Menu State")]
    public void DebugPauseMenuState()
    {
        Debug.Log($"=== PAUSE MENU STATE ===");
        Debug.Log($"Game Paused: {gamePaused}");
        Debug.Log($"PauseMenuUI Active: {pauseMenuUI?.activeInHierarchy}");
        Debug.Log($"ControlsUI Active: {controlsUI?.activeInHierarchy}");
        Debug.Log($"Time Scale: {Time.timeScale}");

        if (NavigationPriorityManager.Instance != null)
        {
            NavigationPriorityManager.Instance.DebugCurrentState();
        }
        else
        {
            Debug.Log("NavigationPriorityManager: NULL");
        }
    }
}