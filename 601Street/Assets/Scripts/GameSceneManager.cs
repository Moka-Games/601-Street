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
        }
        else
        {
            Scene persistentScene = SceneManager.GetSceneByName("PersistentScene");
            if (!persistentScene.isLoaded)
            {
                // Se guarda la referencia de la escena actual
                Scene initialScene = SceneManager.GetActiveScene();

                SceneManager.LoadSceneAsync("PersistentScene", LoadSceneMode.Additive).completed += (asyncOperation) =>
                {
                    persistentSceneLoaded = true;
                    FindPlayerAndCameraInPersistentScene();

                    // En vez de cargar de nuevo la escena actual, solo la establecemos
                    currentSceneName = initialScene.name;
                    StartCoroutine(MovePlayerAndCameraToSpawnPointWithDelay());
                };
            }
            else
            {
                persistentSceneLoaded = true;
                FindPlayerAndCameraInPersistentScene();
                currentSceneName = activeSceneName;
                StartCoroutine(MovePlayerAndCameraToSpawnPointWithDelay());
            }
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

    public void LoadScene(string sceneName)
    {
        if (currentSceneName == sceneName) return;

        // Hay descargar la escena actual antes de cargar la nueva
        if (!string.IsNullOrEmpty(currentSceneName))
        {
            SceneManager.UnloadSceneAsync(currentSceneName);
        }

        currentSceneName = sceneName;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == currentSceneName)
        {
            StartCoroutine(MovePlayerAndCameraToSpawnPointWithDelay());
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private IEnumerator MovePlayerAndCameraToSpawnPointWithDelay()
    {
        yield return new WaitUntil(() => SceneManager.GetSceneByName(currentSceneName).isLoaded);

        GameObject playerSpawnPoint = FindObjectInAllScenes("Player_InitialPosition");
        GameObject cameraSpawnPoint = FindObjectInAllScenes("Camera_InitialPosition");

        if (playerSpawnPoint != null && currentPlayer != null)
        {
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.controller.enabled = false;
                currentPlayer.transform.position = playerSpawnPoint.transform.position;
                currentPlayer.transform.rotation = playerSpawnPoint.transform.rotation;
                playerController.controller.enabled = true;

                Debug.Log("Jugador movido al punto de spawn en la nueva escena.");
            }
            else
            {
                Debug.LogError("No se encontró el PlayerController en el jugador!");
            }
        }
        else
        {
            Debug.LogError($"No se encontró 'Player_InitialPosition' o el jugador en la escena {currentSceneName}!");
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