using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class SceneChange : MonoBehaviour
{
    [System.Serializable]
    public class SpawnPointMapping
    {
        public string sourceScene;      // Escena de origen
        public string targetScene;      // Escena de destino
        public string spawnPointName;   // Nombre del punto de aparición en la escena destino
    }

    [Header("Configuración de Escenas")]
    [Tooltip("Nombre de la escena principal (zona exterior)")]
    public string mainSceneName = "MainArea";

    [Tooltip("Mapeo de puntos de aparición para volver desde interiores")]
    public List<SpawnPointMapping> exitSpawnPoints = new List<SpawnPointMapping>();

    [Tooltip("Nombre por defecto del punto de aparición al entrar a interiores")]
    public string defaultInteriorSpawnPoint = "Player_InitialPosition";

    [Tooltip("Nombre por defecto del punto de aparición en la escena principal")]
    public string defaultMainSceneSpawnPoint = "Player_MainSpawnPoint";

    [Tooltip("Nombre de la escena persistente")]
    public string persistentSceneName = "PersistentScene";

    [Header("Configuración de Transiciones")]
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private float fadeOutDuration = 1.0f;
    [SerializeField] private float blackScreenDuration = 0.5f;

    [Header("Debug")]
    [Tooltip("Mostrar logs detallados")]
    [SerializeField] private bool showDetailedLogs = true;

    // Componentes y referencias
    private FadeManager fadeManager;
    private GameObject player;
    private Camera_Script cameraScript;
    private bool isTransitioning = false;
    private string currentSceneName;

    private void Awake()
    {
        // Validar configuración
        if (string.IsNullOrEmpty(mainSceneName))
        {
            Debug.LogWarning("No se ha configurado el nombre de la escena principal en SceneChange");
        }
    }

    private void Start()
    {
        // Obtener referencias importantes
        FindEssentialComponents();

        // Determinar la escena actual
        currentSceneName = GetCurrentActiveScene();

        Debug.Log($"SceneChange inicializado. Escena actual: {currentSceneName}");
    }

    private void FindEssentialComponents()
    {
        // Encontrar FadeManager
        fadeManager = FindFirstObjectByType<FadeManager>();
        if (fadeManager == null)
        {
            Debug.LogWarning("No se encontró el FadeManager. Las transiciones no tendrán efecto de fundido.");
        }

        // Encontrar jugador
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("No se encontró el jugador (tag: Player). Es necesario para las transiciones.");
        }

        // Encontrar cámara
        cameraScript = FindFirstObjectByType<Camera_Script>();
        if (cameraScript == null)
        {
            Debug.LogWarning("No se encontró Camera_Script. La cámara no se moverá durante transiciones.");
        }
    }

    // Método para entrar a un interior (compatible con UnityEvent - un solo parámetro)
    public void EntrarAInterior(string interiorSceneName)
    {
        if (string.IsNullOrEmpty(interiorSceneName))
        {
            Debug.LogError("No se ha especificado una escena destino");
            return;
        }

        if (isTransitioning)
        {
            Debug.LogWarning("Ya hay una transición en curso. Ignorando nueva solicitud.");
            return;
        }

        // Guardar la escena actual como origen
        string currentScene = GetCurrentActiveScene();
        PlayerPrefs.SetString("LastSourceScene", currentScene);

        // Verificar que la escena existe en el build
        if (!DoesSceneExist(interiorSceneName))
        {
            Debug.LogError($"SceneChange: La escena '{interiorSceneName}' no existe en el Build Settings. No se puede cargar.");
            return;
        }

        // Usar el punto de aparición predeterminado para interiores
        string spawnPoint = defaultInteriorSpawnPoint;

        Debug.Log($"Entrando a interior: {interiorSceneName}, apareciendo en: {spawnPoint}");

        // Guardar punto de aparición
        PlayerPrefs.SetString("LastSpawnPointName", spawnPoint);

        // Iniciar la transición directamente desde este componente
        StartCoroutine(PerformSceneTransition(currentScene, interiorSceneName, spawnPoint, false));
    }

    // Método para salir a la escena principal (compatible con UnityEvent - un solo parámetro)
    public void SalirAExterior(string overrideMainSceneName = "")
    {
        if (isTransitioning)
        {
            Debug.LogWarning("Ya hay una transición en curso. Ignorando nueva solicitud.");
            return;
        }

        // Si no se especifica una escena, usar la configurada por defecto
        string targetSceneName = string.IsNullOrEmpty(overrideMainSceneName) ? mainSceneName : overrideMainSceneName;

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("SceneChange: No se ha especificado una escena principal destino");
            return;
        }

        // Verificar que la escena existe en el build
        if (!DoesSceneExist(targetSceneName))
        {
            Debug.LogError($"SceneChange: La escena '{targetSceneName}' no existe en el Build Settings. No se puede cargar.");
            return;
        }

        // Obtener la escena actual como origen
        string currentScene = GetCurrentActiveScene();

        // Buscar un mapeo de punto de aparición para esta combinación origen-destino
        string spawnPointName = FindExitSpawnPoint(currentScene, targetSceneName);

        // Si no hay un mapeo específico, usar el valor guardado en PlayerPrefs (si existe)
        if (string.IsNullOrEmpty(spawnPointName))
        {
            spawnPointName = PlayerPrefs.GetString("LastSpawnPointName", defaultMainSceneSpawnPoint);
        }

        Debug.Log($"Saliendo a exterior: {targetSceneName}, apareciendo en: {spawnPointName}");

        // Guardar punto de aparición
        PlayerPrefs.SetString("LastSpawnPointName", spawnPointName);

        // Iniciar la transición directamente desde este componente
        StartCoroutine(PerformSceneTransition(currentScene, targetSceneName, spawnPointName, true));
    }

    // Método principal que realiza la transición entre escenas
    private IEnumerator PerformSceneTransition(string sourceSceneName, string targetSceneName, string spawnPointName, bool isReturningToMain)
    {
        isTransitioning = true;

        // Notificar al sistema de interacción
        PlayerInteraction.SetSceneTransitionState(true);

        // Deshabilitar movimiento del jugador
        DisablePlayerMovement();

        // Congelar cámara
        if (cameraScript != null)
        {
            cameraScript.FreezeCamera();
        }

        // Fade In (a negro)
        if (fadeManager != null)
        {
            fadeManager.FadeIn(fadeInDuration);
            yield return new WaitForSeconds(fadeInDuration);
        }
        else
        {
            yield return new WaitForSeconds(0.5f); // Espera mínima si no hay fade
        }

        Debug.Log($"Descargando escena: {sourceSceneName}");

        // Descargar escena actual
        if (!string.IsNullOrEmpty(sourceSceneName))
        {
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sourceSceneName);

            // Esperar a que termine de descargar
            while (unloadOperation != null && !unloadOperation.isDone)
            {
                if (showDetailedLogs)
                {
                    Debug.Log($"Progreso descarga: {unloadOperation.progress * 100}%");
                }
                yield return null;
            }

            Debug.Log($"Escena {sourceSceneName} descargada completamente");
        }

        // Esperar un breve momento con pantalla negra
        yield return new WaitForSeconds(blackScreenDuration);

        Debug.Log($"Cargando escena: {targetSceneName}");

        // CARGAR NUEVA ESCENA - Método directo para evitar problemas
        bool sceneLoaded = false;
        AsyncOperation loadOperation = null;

        // Intentar cargar la escena (sin usar try-catch con yield return)
        try
        {
            // Cargar la escena de manera aditiva
            loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            loadOperation.allowSceneActivation = true; // Asegurar que se activa inmediatamente
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al iniciar la carga de la escena {targetSceneName}: {e.Message}");
            loadOperation = null;
        }

        // Si se inició la operación correctamente, esperar a que complete
        if (loadOperation != null)
        {
            // Esperar a que termine de cargar
            while (!loadOperation.isDone)
            {
                if (showDetailedLogs)
                {
                    Debug.Log($"Progreso carga: {loadOperation.progress * 100}%");
                }
                yield return null;
            }

            // Verificar que la escena se haya cargado
            Scene targetScene = SceneManager.GetSceneByName(targetSceneName);
            sceneLoaded = targetScene.isLoaded;

            if (sceneLoaded)
            {
                // Activar la escena como activa
                SceneManager.SetActiveScene(targetScene);
                currentSceneName = targetSceneName;

                Debug.Log($"Escena {targetSceneName} cargada y activada correctamente");
            }
            else
            {
                Debug.LogError($"La escena {targetSceneName} no se pudo cargar correctamente");
            }
        }
        else
        {
            sceneLoaded = false;
        }

        // Si la carga falló, recuperar y salir
        if (!sceneLoaded)
        {
            Debug.LogError("Fallo en la carga de escena. Abortando transición.");
            isTransitioning = false;
            PlayerInteraction.SetSceneTransitionState(false);

            // Si el fade manager existe, hacer fade out para no dejar pantalla negra
            if (fadeManager != null)
            {
                fadeManager.FadeOut(fadeOutDuration);
            }

            yield break;
        }

        // Esperar para asegurar que todo esté inicializado
        yield return new WaitForSeconds(0.1f);

        // Posicionar jugador y cámara
        MovePlayerAndCamera(spawnPointName);

        // Fade Out (a transparente)
        if (fadeManager != null)
        {
            fadeManager.FadeOut(fadeOutDuration);
            yield return new WaitForSeconds(fadeOutDuration);
        }

        // Descongelar cámara
        if (cameraScript != null)
        {
            cameraScript.UnfreezeCamera();
        }

        // Habilitar movimiento del jugador
        EnablePlayerMovement();

        // Terminar estado de transición
        isTransitioning = false;
        PlayerInteraction.SetSceneTransitionState(false);

        Debug.Log($"Transición completada: {sourceSceneName} -> {targetSceneName}");

        // Aplicar fuentes si existe el FontManager
        FontManager fontManager = FindFirstObjectByType<FontManager>();
        if (fontManager != null)
        {
            fontManager.RefreshAllFonts();
        }
    }

    // Mover jugador y cámara a sus posiciones
    private void MovePlayerAndCamera(string spawnPointName)
    {
        Debug.Log($"Buscando punto de spawn: {spawnPointName}");

        // Buscar el punto de spawn
        GameObject spawnPoint = FindObjectInAllScenes(spawnPointName);

        // Si no se encuentra, buscar alternativas
        if (spawnPoint == null)
        {
            spawnPoint = FindObjectWithNameContaining("InitialPosition");

            if (spawnPoint == null)
            {
                spawnPoint = FindObjectWithNameContaining("Spawn");

                if (spawnPoint == null)
                {
                    Debug.LogError($"No se encontró ningún punto de aparición. El jugador permanecerá en su posición actual.");
                    return;
                }
            }
        }

        // Buscar punto para la cámara
        GameObject cameraPoint = FindObjectInAllScenes("Camera_InitialPosition");

        if (cameraPoint == null)
        {
            cameraPoint = FindObjectWithNameContaining("Camera_");

            if (cameraPoint == null)
            {
                cameraPoint = spawnPoint; // Usar mismo punto que el jugador
            }
        }

        // Mover al jugador
        if (player != null && spawnPoint != null)
        {
            player.transform.position = spawnPoint.transform.position;
            player.transform.rotation = spawnPoint.transform.rotation;
            Debug.Log($"Jugador movido a punto de spawn: {spawnPoint.name}");
        }

        // Mover la cámara
        if (cameraScript != null && cameraPoint != null)
        {
            cameraScript.transform.position = cameraPoint.transform.position;
            cameraScript.transform.rotation = cameraPoint.transform.rotation;
            Debug.Log($"Cámara movida a punto: {cameraPoint.name}");
        }
    }

    // Habilitar/Deshabilitar movimiento del jugador
    private void DisablePlayerMovement()
    {
        if (player != null)
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetMovementEnabled(false);
                Debug.Log("Movimiento del jugador desactivado para transición");
            }
        }
    }

    private void EnablePlayerMovement()
    {
        if (player != null)
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetMovementEnabled(true);
                Debug.Log("Movimiento del jugador reactivado después de transición");
            }
        }
    }

    // Método para buscar un punto de aparición específico basado en la escena origen y destino
    private string FindExitSpawnPoint(string sourceScene, string targetScene)
    {
        foreach (var mapping in exitSpawnPoints)
        {
            if (mapping.sourceScene == sourceScene && mapping.targetScene == targetScene)
            {
                return mapping.spawnPointName;
            }
        }
        return null;
    }

    // Método para obtener la escena activa actual (que no sea la persistente)
    private string GetCurrentActiveScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.name != persistentSceneName)
            {
                return scene.name;
            }
        }
        return SceneManager.GetActiveScene().name;
    }

    // Método para verificar si una escena existe en el build
    private bool DoesSceneExist(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        return Application.CanStreamedLevelBeLoaded(sceneName);
    }

    // Métodos de búsqueda de objetos
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

    // Método de ayuda para debug
    public void LogSceneInfo()
    {
        Debug.Log($"Escena activa: {GetCurrentActiveScene()}");
        Debug.Log($"Escena principal configurada: {mainSceneName}");
        Debug.Log($"Mapeos de puntos de aparición configurados: {exitSpawnPoints.Count}");

        // Listar todas las escenas cargadas
        Debug.Log("Escenas actualmente cargadas:");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            Debug.Log($"  - {scene.name} (Cargada: {scene.isLoaded}, Activa: {scene == SceneManager.GetActiveScene()})");
        }
    }

    // Método para verificar si estamos en transición
    public bool IsTransitioning()
    {
        return isTransitioning;
    }
}