using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla el menú de pausa del juego, integrado con los sistemas de gestión de escenas y estados del juego.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject controlsUI; // Prefab o panel de controles

    [Header("Configuración")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private float delayBeforeSceneChange = 0.5f;

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
        // Inicializar
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        if (controlsUI != null)
        {
            controlsUI.SetActive(false);
        }

        // Buscar referencias a los demás sistemas
        FindManagerReferences();
    }

    private void Start()
    {
        gamePaused = false;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // Solo verificar input si no estamos en una transición de escena
        if (sceneManager != null && sceneManager.IsTransitioning())
            return;

        // Verificar si se presiona la tecla de pausa
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
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
    /// Reanuda el juego, oculta el menú y restaura el tiempo
    /// </summary>
    public void ResumeGame()
    {
        if (!gamePaused)
            return;

        // Ocultar el menú de pausa y el menú de controles
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        if (controlsUI != null)
        {
            controlsUI.SetActive(false);
        }

        // Restablecer la escala de tiempo
        Time.timeScale = 1f;
        gamePaused = false;

        // Restaurar estado del juego
        RestoreGameState();

        // Notificar al sistema de estado del juego
        if (stateManager != null)
        {
            stateManager.EnterGameplayState();
        }

        Debug.Log("Juego reanudado");
    }

    /// <summary>
    /// Pausa el juego, muestra el menú y detiene el tiempo
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

    // Limpia todas las escenas y recursos antes de cargar el menú principal
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

        // IMPORTANTE: Usar carga directa para descargar todas las escenas, incluida la persistente
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

    // Buscar referencias a los sistemas de gestión
    private void FindManagerReferences()
    {
        // Buscar GameSceneManager
        sceneManager = GameSceneManager.Instance;
        if (sceneManager == null)
        {
            sceneManager = FindFirstObjectByType<GameSceneManager>();
            Debug.LogWarning("PauseMenu: GameSceneManager no encontrado mediante Instance. Buscando mediante FindObjectOfType.");
        }

        // Buscar GameStateManager
        stateManager = GameStateManager.Instance;
        if (stateManager == null)
        {
            stateManager = FindFirstObjectByType<GameStateManager>();
            Debug.LogWarning("PauseMenu: GameStateManager no encontrado mediante Instance. Buscando mediante FindObjectOfType.");
        }

        // Buscar otros componentes relevantes
        cameraScript = FindFirstObjectByType<Camera_Script>();
        playerController = FindFirstObjectByType<PlayerController>();
        enabler = Enabler.Instance;
    }

    // Bloquear al jugador durante la pausa
    private void BlockPlayerDuringPause()
    {
        // Usar Enabler si está disponible
        if (enabler != null)
        {
            enabler.BlockPlayer();
            return;
        }

        // Método alternativo - desactivar controller
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
        }

        // Congelar cámara si está disponible
        if (cameraScript != null)
        {
            cameraScript.FreezeCamera();
        }
    }

    // Restaurar estado del juego al salir de la pausa
    private void RestoreGameState()
    {
        // Usar Enabler si está disponible
        if (enabler != null)
        {
            enabler.ReleasePlayer();
            return;
        }

        // Método alternativo - activar controller
        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
        }

        // Descongelar cámara si está disponible
        if (cameraScript != null)
        {
            cameraScript.UnfreezeCamera();
        }
    }

    // Para verificar si el juego está pausado (para otros sistemas)
    public bool IsGamePaused()
    {
        return gamePaused;
    }
}