using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    private void Start()
    {
        // Obtener el parámetro de escena a cargar
        string targetScene = PlayerPrefs.GetString("NextSceneToLoad", "");

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("No hay escena destino especificada en PlayerPrefs");
            return;
        }

        // Iniciar la carga después de un breve retraso
        StartCoroutine(LoadSceneAfterDelay(targetScene, 0.5f));
    }

    private IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        // Esperar el tiempo especificado
        yield return new WaitForSeconds(delay);

        // Cargar la escena
        Debug.Log("Cargando escena: " + sceneName);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = true;

        // Esperar a que la carga termine
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}