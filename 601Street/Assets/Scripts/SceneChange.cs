using UnityEngine;
using UnityEngine.SceneManagement;
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

    private void Awake()
    {
        // Validar configuración
        if (string.IsNullOrEmpty(mainSceneName))
        {
            Debug.LogWarning("No se ha configurado el nombre de la escena principal en SceneChange");
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

        // Guardar la escena actual como origen
        string currentScene = GetCurrentActiveScene();
        PlayerPrefs.SetString("LastSourceScene", currentScene);

        // Usar el punto de aparición predeterminado para interiores
        string spawnPoint = defaultInteriorSpawnPoint;

        Debug.Log($"Entrando a interior: {interiorSceneName}, apareciendo en: {spawnPoint}");

        // Configurar el GameSceneManager con el punto de aparición
        if (GameSceneManager.Instance != null)
        {
            // Marcar que estamos en transición
            PlayerInteraction.SetSceneTransitionState(true);

            // Configurar el punto de aparición personalizado
            GameSceneManager.Instance.SetCustomSpawnPoint(spawnPoint);

            // Cargar la escena
            GameSceneManager.Instance.LoadScene(interiorSceneName, false);
        }
        else
        {
            Debug.LogError("GameSceneManager.Instance es null");
            PlayerInteraction.SetSceneTransitionState(false);
        }
    }

    // Método para salir a la escena principal (compatible con UnityEvent - un solo parámetro)
    public void SalirAExterior(string mainSceneName = "")
    {
        // Si no se especifica una escena, usar la configurada por defecto
        if (string.IsNullOrEmpty(mainSceneName))
        {
            mainSceneName = this.mainSceneName;
        }

        if (string.IsNullOrEmpty(mainSceneName))
        {
            Debug.LogError("No se ha especificado una escena principal destino");
            return;
        }

        // Obtener la escena actual como origen
        string currentScene = GetCurrentActiveScene();

        // Buscar un mapeo de punto de aparición para esta combinación origen-destino
        string spawnPointName = FindExitSpawnPoint(currentScene, mainSceneName);

        // Si no hay un mapeo específico, usar el valor guardado en PlayerPrefs (si existe)
        if (string.IsNullOrEmpty(spawnPointName))
        {
            spawnPointName = PlayerPrefs.GetString("LastSpawnPointName", defaultMainSceneSpawnPoint);
        }

        Debug.Log($"Saliendo a exterior: {mainSceneName}, apareciendo en: {spawnPointName}");

        // Configurar el GameSceneManager con el punto de aparición
        if (GameSceneManager.Instance != null)
        {
            // Marcar que estamos en transición
            PlayerInteraction.SetSceneTransitionState(true);

            // Configurar el punto de aparición personalizado
            GameSceneManager.Instance.SetCustomSpawnPoint(spawnPointName);

            // Cargar la escena
            GameSceneManager.Instance.LoadScene(mainSceneName, true);
        }
        else
        {
            Debug.LogError("GameSceneManager.Instance es null");
            PlayerInteraction.SetSceneTransitionState(false);
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

    // Método de ayuda para debug
    public void LogSceneInfo()
    {
        Debug.Log($"Escena activa: {GetCurrentActiveScene()}");
        Debug.Log($"Escena principal configurada: {mainSceneName}");
        Debug.Log($"Mapeos de puntos de aparición configurados: {exitSpawnPoints.Count}");
    }
}