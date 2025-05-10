using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionController : MonoBehaviour
{
    // M�todo para ser  desde el AnimationEvent al final de la animaci�n
    public void CambiarAEscenaJuego()
    {
        string escenaDestino = PlayerPrefs.GetString("NextSceneToLoad", "");

        if (string.IsNullOrEmpty(escenaDestino))
        {
            Debug.LogError("No se ha especificado una escena destino en PlayerPrefs");
            return;
        }

        Debug.Log($"TransitionController: Cambiando directamente a escena del juego '{escenaDestino}'");

        // Opcional: Puedes aplicar un efecto de fade out aqu� antes de la transici�n

        // Cargar directamente la escena del juego (esto provocar� que GameSceneManager detecte 
        // que debe cargar la persistente en segundo plano)
        SceneManager.LoadScene(escenaDestino);
    }
}