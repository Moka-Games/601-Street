using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionController : MonoBehaviour
{
    // Método para ser  desde el AnimationEvent al final de la animación
    public void CambiarAEscenaJuego()
    {
        string escenaDestino = PlayerPrefs.GetString("NextSceneToLoad", "");

        if (string.IsNullOrEmpty(escenaDestino))
        {
            Debug.LogError("No se ha especificado una escena destino en PlayerPrefs");
            return;
        }

        Debug.Log($"TransitionController: Cambiando directamente a escena del juego '{escenaDestino}'");

        // Opcional: Puedes aplicar un efecto de fade out aquí antes de la transición

        // Cargar directamente la escena del juego (esto provocará que GameSceneManager detecte 
        // que debe cargar la persistente en segundo plano)
        SceneManager.LoadScene(escenaDestino);
    }
}