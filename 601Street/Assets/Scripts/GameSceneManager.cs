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

    // Añadimos una variable para rastrear la dirección del cambio de escena
    private enum SceneChangeDirection { Initial, Forward, Backward };
    private SceneChangeDirection lastChangeDirection = SceneChangeDirection.Initial;

    [Header("Configuración de Transiciones")]
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private float fadeOutDuration = 1.0f;
    [SerializeField] private float blackScreenDuration = 0.5f;

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
                    // Desactivamos el movimiento del jugador inicialmente
                    DisablePlayerMovement();
                    // Iniciamos con un fadeOut al cargar por primera vez
                    if (fadeManager != null)
                    {
                        fadeManager.BlackScreenIntoFadeOut(fadeOutDuration);
                        // Suscribimos al evento de finalización de fadeOut
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
                // Desactivamos el movimiento del jugador inicialmente
                DisablePlayerMovement();
                // Iniciamos con un fadeOut al cargar por primera vez
                if (fadeManager != null)
                {
                    fadeManager.BlackScreenIntoFadeOut(fadeOutDuration);
                    // Suscribimos al evento de finalización de fadeOut
                    fadeManager.OnFadeOutComplete += EnablePlayerMovementAfterFade;
                }
                StartCoroutine(MovePlayerAndCameraToSpawnPointWithDelay());
            }
        }
    }
    private void OnDestroy()
    {
        // Limpiamos los eventos suscritos
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

    // Añadimos parámetro para la dirección
    public void LoadScene(string sceneName, bool isBackward = false)
    {
        if (currentSceneName == sceneName || isTransitioning) return;

        // Guardamos la dirección del cambio
        lastChangeDirection = isBackward ? SceneChangeDirection.Backward : SceneChangeDirection.Forward;

        StartCoroutine(LoadSceneWithTransition(sceneName));
    }

    private IEnumerator LoadSceneWithTransition(string sceneName)
    {
        isTransitioning = true;

        // Desactivar el controlador del jugador durante la transición
        DisablePlayerMovement();

        // Congelar la cámara durante la transición
        if (currentCamera != null)
        {
            currentCamera.FreezeCamera();
        }

        // Realizar el fade in (a negro)
        if (fadeManager != null)
        {
            fadeManager.FadeIn(fadeInDuration);
            yield return new WaitForSeconds(fadeInDuration);
        }

        // Descargar la escena actual
        if (!string.IsNullOrEmpty(currentSceneName))
        {
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(currentSceneName);
            yield return unloadOperation;
        }

        // Mantener la pantalla en negro por un momento
        yield return new WaitForSeconds(blackScreenDuration);

        // Cargar la nueva escena
        currentSceneName = sceneName;
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // Esperar a que la escena esté completamente cargada
        yield return loadOperation;

        // Asegurarse de que los indicadores estén activos
        //UITemplateManager.Instance.EnsureTemplatesAreInactive();

        // Mover al jugador y la cámara a los puntos de spawn
        yield return StartCoroutine(MovePlayerAndCameraToSpawnPointWithDelay());

        // Realizar el fade out (de negro a transparente)
        if (fadeManager != null)
        {
            fadeManager.OnFadeOutComplete += EnablePlayerMovementAfterFade;
            fadeManager.FadeOut(fadeOutDuration);
        }

        // Descongelar la cámara
        if (currentCamera != null)
        {
            currentCamera.UnfreezeCamera();
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

    // In the EnablePlayerMovementAfterFade method in GameSceneManager.cs
    private void EnablePlayerMovementAfterFade()
    {
        // Restablecer el estado de transición
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

        // Desuscribimos el evento para evitar múltiples activaciones
        if (fadeManager != null)
        {
            fadeManager.OnFadeOutComplete -= EnablePlayerMovementAfterFade;
        }
    }

    private IEnumerator MovePlayerAndCameraToSpawnPointWithDelay()
    {
        yield return new WaitUntil(() => SceneManager.GetSceneByName(currentSceneName).isLoaded);

        // Elegimos el punto de spawn dependiendo de la dirección
        string playerSpawnPointName = "Player_InitialPosition";
        if (lastChangeDirection == SceneChangeDirection.Backward)
        {
            // Si estamos regresando a una escena anterior, usamos Player_ExitPosition
            GameObject exitPoint = FindObjectInAllScenes("Player_ExitPosition");
            if (exitPoint != null)
            {
                playerSpawnPointName = "Player_ExitPosition";
            }
            else
            {
                Debug.LogWarning("No se encontró 'Player_ExitPosition', usando la posición inicial por defecto.");
            }
        }

        GameObject playerSpawnPoint = FindObjectInAllScenes(playerSpawnPointName);
        GameObject cameraSpawnPoint = FindObjectInAllScenes("Camera_InitialPosition");

        if (playerSpawnPoint != null && currentPlayer != null)
        {
            // Aseguramos que el movimiento está desactivado durante el posicionamiento
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.SetMovementEnabled(false);
            }

            currentPlayer.transform.position = playerSpawnPoint.transform.position;
            currentPlayer.transform.rotation = playerSpawnPoint.transform.rotation;

            Debug.Log($"Jugador movido al punto de spawn '{playerSpawnPointName}' en la nueva escena.");
        }
        else
        {
            Debug.LogError($"No se encontró '{playerSpawnPointName}' o el jugador en la escena {currentSceneName}!");
        }

        if (cameraSpawnPoint != null && currentCamera != null)
        {
            currentCamera.transform.position = cameraSpawnPoint.transform.position;
            currentCamera.transform.rotation = cameraSpawnPoint.transform.rotation;

            Debug.Log("Cámara movida al punto de spawn en la nueva escena.");
        }
        else
        {
            Debug.LogError($"No se encontró 'Camera_InitialPosition' o la cámara en la escena {currentSceneName}!");
        }
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