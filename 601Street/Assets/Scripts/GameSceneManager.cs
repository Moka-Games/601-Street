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
        DontDestroyOnLoad(gameObject);

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

                SceneManager.LoadSceneAsync("PersistentScene", LoadSceneMode.Additive).completed += (asyncOperation) =>
                {
                    persistentSceneLoaded = true;
                    FindPlayerAndCameraInPersistentScene();
                    FindFadeManager();

                    currentSceneName = initialScene.name;
                    DisablePlayerMovement();
                    if (fadeManager != null)
                    {
                        fadeManager.BlackScreenIntoFadeOut(fadeOutDuration);
                        fadeManager.OnFadeOutComplete += EnablePlayerMovementAfterFade;
                    }
                    StartCoroutine(MovePlayerAndCameraToSpawnPointWithDelay());
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
        fadeManager = FindAnyObjectByType<FadeManager>();
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

        GameObject cameraObject = FindAnyObjectByType<Camera_Script>()?.gameObject;
        if (cameraObject != null)
        {
            currentCamera = cameraObject.GetComponent<Camera_Script>();
        }
        else
        {
            Debug.LogError("No se encontró la cámara en la escena persistente!");
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
        if (currentSceneName == sceneName || isTransitioning) return;
        StartCoroutine(LoadSceneWithTransition(sceneName, isBackward));
    }

    private IEnumerator LoadSceneWithTransition(string sceneName, bool isBackward)
    {
        isTransitioning = true;

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

        if (!string.IsNullOrEmpty(currentSceneName))
        {
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(currentSceneName);
            yield return unloadOperation;
        }

        yield return new WaitForSeconds(blackScreenDuration);

        currentSceneName = sceneName;
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return loadOperation;

        yield return StartCoroutine(MovePlayerAndCameraToSpawnPointWithDelay());

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
    }

    private IEnumerator MovePlayerAndCameraToSpawnPointWithDelay()
    {
        yield return new WaitUntil(() => SceneManager.GetSceneByName(currentSceneName).isLoaded);

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
            }
        }

        GameObject cameraSpawnPoint = FindObjectInAllScenes("Camera_InitialPosition");

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
            Debug.LogError($"No se encontró 'Camera_InitialPosition' para la cámara en la escena {currentSceneName}!");
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

    private GameObject FindObjectInAllScenes(string objectName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                if (obj.name == objectName)
                {
                    return obj;
                }
            }
        }
        return null;
    }
}