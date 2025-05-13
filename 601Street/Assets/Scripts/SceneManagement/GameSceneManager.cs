using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSceneManager : MonoBehaviour
{
    private static GameSceneManager instance;
    public static GameSceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("GameSceneManager no está inicializado!");
            }
            return instance;
        }
    }

    private GameObject currentPlayer;
    private Camera_Script currentCamera;
    private string currentSceneName;
    private bool persistentSceneLoaded = false;

    // Variable para puntos de aparición personalizados
    private string customSpawnPointName = null;

    [Header("Configuración de Transiciones")]
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private float fadeOutDuration = 1.0f;
    [SerializeField] private float blackScreenDuration = 0.5f;

    [Header("Configuración de Puntos de Aparición")]
    [Tooltip("Nombre del punto de aparición por defecto al entrar a una escena")]
    [SerializeField] private string defaultInitialSpawnPointName = "Player_InitialPosition";
    [Tooltip("Nombre del punto de aparición por defecto al volver de otra escena")]
    [SerializeField] private string defaultExitSpawnPointName = "Player_ExitPosition";

    [Header("Integración con FontManager")]
    [Tooltip("¿Aplicar fuentes globales después de cargar una escena?")]
    [SerializeField] private bool applyGlobalFonts = true;
    [Tooltip("Retraso antes de aplicar las fuentes (segundos)")]
    [SerializeField] private float fontApplicationDelay = 0.2f;

    [Header("Debug")]
    [Tooltip("Mostrar logs detallados de depuración")]
    [SerializeField] private bool showDetailedLogs = false;

    private FadeManager fadeManager;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        string activeSceneName = SceneManager.GetActiveScene().name;

        if (activeSceneName == "PersistentScene")
        {
            persistentSceneLoaded = true;
            FindPlayerAndCameraInPersistentScene();
            FindFadeManager();
        }
        else
        {
            Scene persistentScene = SceneManager.GetSceneByName("PersistentScene");
            if (!persistentScene.isLoaded)
            {
                Scene initialScene = SceneManager.GetActiveScene();

                // Comprobar si estamos en un DirectLoad
                bool isDirectLoad = PlayerPrefs.GetInt("DirectSceneLoad", 0) == 1;

                // Si es carga directa, asegurar que mantenemos la pantalla negra durante toda la transición
                if (isDirectLoad && fadeManager != null)
                {
                    fadeManager.ForceBlackScreen(); // Añadir este método a FadeManager
                }

                SceneManager.LoadSceneAsync("PersistentScene", LoadSceneMode.Additive).completed += (asyncOperation) =>
                {
                    persistentSceneLoaded = true;
                    FindPlayerAndCameraInPersistentScene();
                    FindFadeManager();

                    currentSceneName = initialScene.name;
                    DisablePlayerMovement();

                    if (fadeManager != null)
                    {
                        // Solo hacer fadeOut cuando todo esté listo
                        fadeManager.BlackScreenIntoFadeOut(fadeOutDuration);
                        fadeManager.OnFadeOutComplete += EnablePlayerMovementAfterFade;

                        // Limpiar la bandera
                        PlayerPrefs.DeleteKey("DirectSceneLoad");
                    }

                    StartCoroutine(MovePlayerAndCameraToSpawnPointWithDelay());

                    // Aplicar fuentes globales a la escena inicial
                    if (applyGlobalFonts)
                    {
                        StartCoroutine(ApplyGlobalFontsWithDelay(initialScene));
                    }
                };
            }
            else
            {
                persistentSceneLoaded = true;
                FindPlayerAndCameraInPersistentScene();
                FindFadeManager();
                currentSceneName = activeSceneName;
                DisablePlayerMovement();
                if (fadeManager != null)
                {
                    fadeManager.BlackScreenIntoFadeOut(fadeOutDuration);
                    fadeManager.OnFadeOutComplete += EnablePlayerMovementAfterFade;
                }
                StartCoroutine(MovePlayerAndCameraToSpawnPointWithDelay());

                // Aplicar fuentes globales a la escena actual
                if (applyGlobalFonts)
                {
                    Scene currentScene = SceneManager.GetActiveScene();
                    StartCoroutine(ApplyGlobalFontsWithDelay(currentScene));
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (fadeManager != null)
        {
            fadeManager.OnFadeOutComplete -= EnablePlayerMovementAfterFade;
        }
    }

    private void FindFadeManager()
    {
        fadeManager = FindFirstObjectByType<FadeManager>();
        if (fadeManager == null)
        {
            Debug.LogError("No se encontró el FadeManager en la escena persistente!");
        }
    }

    private void FindPlayerAndCameraInPersistentScene()
    {
        currentPlayer = GameObject.FindGameObjectWithTag("Player");
        if (currentPlayer == null)
        {
            Debug.LogError("No se encontró el jugador en la escena persistente!");
        }

        GameObject cameraObject = FindFirstObjectByType<Camera_Script>()?.gameObject;
        if (cameraObject != null)
        {
            currentCamera = cameraObject.GetComponent<Camera_Script>();
        }
        else
        {
            Debug.LogError("No se encontró la cámara en la escena persistente!");
        }
    }

    // Método para aplicar fuentes globales con un retraso
    private IEnumerator ApplyGlobalFontsWithDelay(Scene scene)
    {
        // Esperar el tiempo configurado
        yield return new WaitForSeconds(fontApplicationDelay);

        // Buscar el FontManager y aplicar las fuentes
        FontManager fontManager = FontManager.Instance;
        if (fontManager != null)
        {
            if (showDetailedLogs)
            {
                Debug.Log($"GameSceneManager: Aplicando fuentes globales a la escena '{scene.name}'");
            }
            fontManager.ApplyFontToAllLoadedScenesImmediately();
        }
        else
        {
            Debug.LogWarning("GameSceneManager: No se encontró el FontManager para aplicar fuentes globales");
        }
    }

    // Método para establecer un punto de aparición personalizado
    public void SetCustomSpawnPoint(string spawnPointName)
    {
        if (!string.IsNullOrEmpty(spawnPointName))
        {
            customSpawnPointName = spawnPointName;
            Debug.Log($"Punto de aparición personalizado establecido: {spawnPointName}");
        }
    }

    // Cargar una nueva escena
    public void LoadScene(string sceneName, bool isBackward = false)
    {
        if (currentSceneName == sceneName || isTransitioning)
        {
            Debug.LogWarning($"No se puede cargar la escena '{sceneName}': Ya es la escena actual o hay una transición en curso");
            return;
        }

        if (showDetailedLogs)
        {
            Debug.Log($"GameSceneManager: Iniciando carga de escena '{sceneName}' (isBackward: {isBackward})");
        }

        StartCoroutine(LoadSceneWithTransition(sceneName, isBackward));
    }

    private IEnumerator LoadSceneWithTransition(string sceneName, bool isBackward)
    {
        WorldStateManager.Instance.ApplyStateToScene(sceneName);

        isTransitioning = true;

        if (showDetailedLogs)
        {
            Debug.Log($"GameSceneManager: Iniciando transición a escena '{sceneName}'");
        }

        DisablePlayerMovement();

        if (currentCamera != null)
        {
            currentCamera.FreezeCamera();
        }

        if (fadeManager != null)
        {
            fadeManager.FadeIn(fadeInDuration);
            yield return new WaitForSeconds(fadeInDuration);
        }

        // DESCARGA DE ESCENA ACTUAL
        if (!string.IsNullOrEmpty(currentSceneName))
        {
            if (showDetailedLogs)
            {
                Debug.Log($"GameSceneManager: Descargando escena actual '{currentSceneName}'");
            }

            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(currentSceneName);
            yield return unloadOperation;

            if (showDetailedLogs)
            {
                Debug.Log($"GameSceneManager: Escena '{currentSceneName}' descargada correctamente");
            }
        }

        yield return new WaitForSeconds(blackScreenDuration);

        // CARGA DE NUEVA ESCENA
        if (showDetailedLogs)
        {
            Debug.Log($"GameSceneManager: Iniciando carga de escena '{sceneName}'");
        }

        // Guardar la nueva escena como actual antes de cargarla
        currentSceneName = sceneName;

        // Nueva comprobación para asegurar que la escena existe antes de intentar cargarla
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            // Asegurarse de que la operación complete antes de continuar
            while (!loadOperation.isDone)
            {
                if (showDetailedLogs)
                {
                    Debug.Log($"GameSceneManager: Progreso de carga de escena '{sceneName}': {loadOperation.progress * 100}%");
                }
                yield return null;
            }

            if (showDetailedLogs)
            {
                Debug.Log($"GameSceneManager: Escena '{sceneName}' cargada correctamente");
            }

            // Hacer la escena recién cargada activa
            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (loadedScene.isLoaded)
            {
                SceneManager.SetActiveScene(loadedScene);

                if (showDetailedLogs)
                {
                    Debug.Log($"GameSceneManager: Escena '{sceneName}' establecida como activa");
                }

                // Aplicar fuentes globales a la nueva escena
                if (applyGlobalFonts)
                {
                    StartCoroutine(ApplyGlobalFontsWithDelay(loadedScene));
                }
            }
            else
            {
                Debug.LogError($"GameSceneManager: La escena '{sceneName}' no se cargó correctamente");
                isTransitioning = false;
                PlayerInteraction.SetSceneTransitionState(false);
                yield break;
            }
        }
        else
        {
            Debug.LogError($"GameSceneManager: No se puede cargar la escena '{sceneName}'. Asegúrate de que está en el Build Settings.");
            currentSceneName = ""; // Resetear ya que no pudimos cargar la nueva escena
            isTransitioning = false;
            PlayerInteraction.SetSceneTransitionState(false);
            yield break;
        }

        // POSICIONAMIENTO DEL JUGADOR Y CÁMARA
        yield return StartCoroutine(MovePlayerAndCameraToSpawnPointWithDelay());

        // FADE OUT Y FINALIZACIÓN
        if (fadeManager != null)
        {
            fadeManager.OnFadeOutComplete += EnablePlayerMovementAfterFade;
            fadeManager.FadeOut(fadeOutDuration);
        }

        if (currentCamera != null)
        {
            currentCamera.UnfreezeCamera();
        }

        // Limpiar el punto de aparición personalizado después de usarlo
        customSpawnPointName = null;

        isTransitioning = false;

        if (showDetailedLogs)
        {
            Debug.Log($"GameSceneManager: Transición a escena '{sceneName}' completada");
        }
        
        Camera_Script finalCameraCheck = FindFirstObjectByType<Camera_Script>();
        if (finalCameraCheck != null)
        {
            Debug.Log("GameSceneManager: Verificación final de desbloqueo de cámara");
            finalCameraCheck.UnfreezeCamera();
        }

        isTransitioning = false;
    }

    private void DisablePlayerMovement()
    {
        if (currentPlayer != null)
        {
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.SetMovementEnabled(false);
                Debug.Log("Movimiento del jugador desactivado durante transición");
            }
        }
    }

    private void EnablePlayerMovementAfterFade()
    {
        PlayerInteraction.SetSceneTransitionState(false);

        // NUEVO: Desbloquear explícitamente la cámara
        if (currentCamera != null)
        {
            Debug.Log("GameSceneManager: Desbloqueando cámara después del fade");
            currentCamera.UnfreezeCamera();
        }
        else
        {
            // Buscar la cámara si no la tenemos
            Camera_Script cameraScript = FindFirstObjectByType<Camera_Script>();
            if (cameraScript != null)
            {
                Debug.Log("GameSceneManager: Cámara encontrada y desbloqueada después del fade");
                cameraScript.UnfreezeCamera();
            }
        }

        if (currentPlayer != null)
        {
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.SetMovementEnabled(true);
                Debug.Log("Movimiento del jugador reactivado después del fade");
            }
        }

        if (fadeManager != null)
        {
            fadeManager.OnFadeOutComplete -= EnablePlayerMovementAfterFade;
        }

        // NUEVO: Iniciar comprobación de seguridad retrasada
        StartCoroutine(SafetyCheckUnfreezeCamera());
    }

    // NUEVO: Método de seguridad para garantizar que la cámara se desbloquea
    private IEnumerator SafetyCheckUnfreezeCamera()
    {
        // Esperar un poco para asegurar que todos los sistemas estén activos
        yield return new WaitForSeconds(0.5f);

        // Comprobar y desbloquear la cámara de nuevo
        Camera_Script cameraScript = FindFirstObjectByType<Camera_Script>();
        if (cameraScript != null && cameraScript.freeLookCamera != null)
        {
            if (!cameraScript.freeLookCamera.enabled)
            {
                Debug.Log("GameSceneManager: Desbloqueo de seguridad aplicado a la cámara");
                cameraScript.UnfreezeCamera();
            }
        }
    }

    private IEnumerator MovePlayerAndCameraToSpawnPointWithDelay()
    {
        if (showDetailedLogs)
        {
            Debug.Log($"GameSceneManager: Esperando a que la escena '{currentSceneName}' esté cargada");
        }

        // Esperar a que la escena esté cargada
        yield return new WaitUntil(() => {
            Scene targetScene = SceneManager.GetSceneByName(currentSceneName);
            return targetScene.isLoaded;
        });

        if (showDetailedLogs)
        {
            Debug.Log($"GameSceneManager: Escena '{currentSceneName}' confirmada como cargada, procediendo a mover al jugador");
        }

        // MOVER AL JUGADOR
        // Determinar qué punto de aparición usar
        string spawnPointName = DetermineSpawnPointName();

        Debug.Log($"Buscando punto de aparición: {spawnPointName}");
        GameObject spawnPoint = FindObjectInAllScenes(spawnPointName);

        // Si no se encuentra el punto específico, buscar el punto por defecto
        if (spawnPoint == null)
        {
            Debug.LogWarning($"No se encontró el punto de aparición '{spawnPointName}'. Buscando punto por defecto.");
            spawnPointName = defaultInitialSpawnPointName;
            spawnPoint = FindObjectInAllScenes(spawnPointName);

            // Si aún no se encuentra, intentar con el de salida
            if (spawnPoint == null)
            {
                spawnPointName = defaultExitSpawnPointName;
                spawnPoint = FindObjectInAllScenes(spawnPointName);

                // Si aún no se encuentra ningún punto, buscar cualquier objeto con "Spawn" en su nombre
                if (spawnPoint == null)
                {
                    Debug.LogWarning($"No se encontraron puntos de aparición estándar. Buscando cualquier objeto con 'Spawn' en su nombre.");
                    spawnPoint = FindObjectWithNameContaining("Spawn");

                    if (spawnPoint == null)
                    {
                        Debug.LogError($"No se pudo encontrar ningún punto de aparición en la escena '{currentSceneName}'");
                    }
                }
            }
        }

        // MOVER LA CÁMARA
        GameObject cameraSpawnPoint = FindObjectInAllScenes("Camera_InitialPosition");

        // Si no se encuentra, buscar alternativas
        if (cameraSpawnPoint == null)
        {
            cameraSpawnPoint = FindObjectWithNameContaining("Camera_");

            if (cameraSpawnPoint == null && spawnPoint != null)
            {
                // En el peor caso, usar el mismo punto que el jugador
                Debug.LogWarning("No se encontró punto para la cámara. Usando el punto del jugador.");
                cameraSpawnPoint = spawnPoint;
            }
        }

        // APLICAR POSICIONES
        if (spawnPoint != null && currentPlayer != null)
        {
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.SetMovementEnabled(false);
            }

            // Mover al jugador al punto de aparición
            currentPlayer.transform.position = spawnPoint.transform.position;
            currentPlayer.transform.rotation = spawnPoint.transform.rotation;

            Debug.Log($"Jugador movido al punto de aparición: {spawnPointName}");
        }
        else
        {
            Debug.LogError($"No se encontró un punto de aparición válido para el jugador en la escena {currentSceneName}!");
        }

        if (cameraSpawnPoint != null && currentCamera != null)
        {
            currentCamera.transform.position = cameraSpawnPoint.transform.position;
            currentCamera.transform.rotation = cameraSpawnPoint.transform.rotation;
            Debug.Log("Cámara movida al punto de spawn en la nueva escena.");
        }
        else
        {
            Debug.LogError($"No se encontró un punto de aparición válido para la cámara en la escena {currentSceneName}!");
        }
    }

    // Determinar el nombre del punto de aparición a usar
    private string DetermineSpawnPointName()
    {
        // Prioridad 1: Usar el punto personalizado si está establecido
        if (!string.IsNullOrEmpty(customSpawnPointName))
        {
            return customSpawnPointName;
        }

        // Prioridad 2: Usar el valor almacenado en PlayerPrefs
        string savedSpawnPoint = PlayerPrefs.GetString("LastSpawnPointName", "");
        if (!string.IsNullOrEmpty(savedSpawnPoint))
        {
            // Limpiar el valor guardado para no reutilizarlo accidentalmente
            PlayerPrefs.DeleteKey("LastSpawnPointName");
            return savedSpawnPoint;
        }

        // Prioridad 3: Usar el punto de aparición por defecto
        return defaultInitialSpawnPointName;
    }

    private GameObject FindObjectInAllScenes(string exactName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                foreach (GameObject obj in scene.GetRootGameObjects())
                {
                    if (obj.name == exactName)
                    {
                        return obj;
                    }

                    // También buscar en hijos
                    Transform childTransform = FindChildWithName(obj.transform, exactName);
                    if (childTransform != null)
                    {
                        return childTransform.gameObject;
                    }
                }
            }
        }
        return null;
    }

    // Buscar un objeto que contenga cierto string en su nombre
    private GameObject FindObjectWithNameContaining(string partialName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded)
            {
                foreach (GameObject obj in scene.GetRootGameObjects())
                {
                    if (obj.name.Contains(partialName))
                    {
                        return obj;
                    }

                    // También buscar en hijos
                    Transform childTransform = FindChildWithNameContaining(obj.transform, partialName);
                    if (childTransform != null)
                    {
                        return childTransform.gameObject;
                    }
                }
            }
        }
        return null;
    }

    // Buscar un hijo con un nombre exacto
    private Transform FindChildWithName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }

            Transform result = FindChildWithName(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    // Buscar un hijo que contenga cierto string en su nombre
    private Transform FindChildWithNameContaining(Transform parent, string partialName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(partialName))
            {
                return child;
            }

            Transform result = FindChildWithNameContaining(child, partialName);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    // Método para forzar la aplicación de fuentes a la escena actual
    public void ForceApplyFontsToCurrentScene()
    {
        if (string.IsNullOrEmpty(currentSceneName))
        {
            Debug.LogWarning("GameSceneManager: No hay una escena actual definida para aplicar fuentes");
            return;
        }

        Scene currentScene = SceneManager.GetSceneByName(currentSceneName);
        if (currentScene.isLoaded)
        {
            FontManager fontManager = FontManager.Instance;
            if (fontManager != null)
            {
                Debug.Log($"GameSceneManager: Forzando aplicación de fuentes globales a la escena '{currentSceneName}'");
                StartCoroutine(ApplyGlobalFontsWithDelay(currentScene));
            }
        }
    }
    public void ForceUnfreezeCamera()
    {
        if (currentCamera != null)
        {
            Debug.Log("GameSceneManager: Forzando desbloqueo de cámara");
            currentCamera.UnfreezeCamera();
        }
        else
        {
            Camera_Script cameraScript = FindFirstObjectByType<Camera_Script>();
            if (cameraScript != null)
            {
                Debug.Log("GameSceneManager: Cámara encontrada y desbloqueada forzosamente");
                cameraScript.UnfreezeCamera();
            }
        }
    }
    // Método para verificar si estamos actualmente en una transición de escena
    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    // Método para obtener la escena actual
    public string GetCurrentSceneName()
    {
        return currentSceneName;
    }
    public static void SetupForDirectLoad()
    {
        // Este método se puede llamar desde cualquier lugar antes de cargar la escena
        // Configura una bandera que indica al GameSceneManager que mantenga la pantalla
        // negra hasta que todo esté listo
        PlayerPrefs.SetInt("DirectSceneLoad", 1);
        PlayerPrefs.Save();
    }
}