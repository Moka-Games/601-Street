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

        // Verificamos y cargamos la PersistentScene si no está cargada
        if (!persistentSceneLoaded)
        {
            LoadPersistentScene();
        }
    }

    private void LoadPersistentScene()
    {
        Scene persistentScene = SceneManager.GetSceneByName("PersistentScene");
        if (!persistentScene.isLoaded)
        {
            SceneManager.LoadSceneAsync("PersistentScene", LoadSceneMode.Additive).completed += (asyncOperation) =>
            {
                persistentSceneLoaded = true;
                FindPlayerInPersistentScene();
            };
        }
        else
        {
            FindPlayerInPersistentScene();
        }
    }

    private void FindPlayerInPersistentScene()
    {
        currentPlayer = GameObject.FindGameObjectWithTag("Player");
        if (currentPlayer == null)
        {
            Debug.LogError("No se encontró el jugador en la escena persistente!");
        }
    }

    public void LoadScene(string sceneName)
    {
        currentSceneName = sceneName;

        SceneManager.sceneLoaded += OnSceneLoaded;  // Suscribimos a la carga de la nueva escena

        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == currentSceneName)
        {
            StartCoroutine(MovePlayerToSpawnPointWithDelay());
            SceneManager.sceneLoaded -= OnSceneLoaded;  // Desuscribirnos después de que se cargue la escena
        }
    }



    private IEnumerator MovePlayerToSpawnPointWithDelay()
    {
        Debug.Log("Esperando para mover al jugador...");
        yield return new WaitForSeconds(0.1f); // Pequeño retraso para asegurar que la escena esté lista
        Debug.Log("Esperado, ahora buscando el punto de spawn");

        // Buscar el punto de spawn en todas las escenas cargadas
        GameObject spawnPoint = FindObjectInAllScenes("Player_InitialPosition");

        if (spawnPoint != null)
        {
            Debug.Log("Punto de spawn encontrado en la posición: " + spawnPoint.transform.position);
        }
        else
        {
            Debug.LogError("No se encontró el punto de spawn en ninguna escena");
        }

        if (spawnPoint != null && currentPlayer != null)
        {
            currentPlayer.transform.position = spawnPoint.transform.position;
            currentPlayer.transform.rotation = spawnPoint.transform.rotation;
            Debug.Log("Jugador movido al punto de spawn");
        }
        else
        {
            Debug.LogError($"No se encontró 'Player_InitialPosition' o el jugador en la escena {currentSceneName}!");
        }
    }


    private GameObject FindObjectInAllScenes(string objectName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            Debug.Log("Escena cargada: " + scene.name);  // Añadir un debug aquí para ver qué escenas están cargadas
            foreach (GameObject obj in scene.GetRootGameObjects())
            {
                if (obj.name == objectName)
                {
                    Debug.Log($"Encontrado {objectName} en la escena {scene.name}");
                    return obj;
                }
            }
        }
        return null;
    }


    private void UnloadPreviousScene()
    {
        int countLoaded = SceneManager.sceneCount;
        for (int i = 0; i < countLoaded; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.name.Equals("PersistentScene") && !scene.name.Equals(currentSceneName))
            {
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}
