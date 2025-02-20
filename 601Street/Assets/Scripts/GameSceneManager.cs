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

        if (SceneManager.GetActiveScene().name == "PersistentScene")
        {
            persistentSceneLoaded = true;
            FindPlayerInPersistentScene();
            LoadScene("PruebaConPersistente_2");
        }
        else
        {
            Scene persistentScene = SceneManager.GetSceneByName("PersistentScene");
            if (!persistentScene.isLoaded)
            {
                SceneManager.LoadSceneAsync("PersistentScene", LoadSceneMode.Additive).completed += (asyncOperation) =>
                {
                    persistentSceneLoaded = true;
                    FindPlayerInPersistentScene();
                    LoadScene("PruebaConPersistente_2");
                };
            }
            else
            {
                FindPlayerInPersistentScene();
            }
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
        if (currentSceneName == sceneName) return;  

        currentSceneName = sceneName;

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == currentSceneName)
        {
            StartCoroutine(MovePlayerToSpawnPointWithDelay());
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private IEnumerator MovePlayerToSpawnPointWithDelay()
    {
        yield return new WaitUntil(() => SceneManager.GetSceneByName(currentSceneName).isLoaded);

        GameObject spawnPoint = GameObject.Find("Player_InitialPosition");

        if (spawnPoint != null && currentPlayer != null)
        {
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.controller.enabled = false;
                currentPlayer.transform.position = spawnPoint.transform.position;
                currentPlayer.transform.rotation = spawnPoint.transform.rotation;
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
