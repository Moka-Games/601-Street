using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    [System.Serializable]
    public class EscenaCambio
    {
        public string escenaActual; // La escena donde está el objeto
        public string escenaDestino; // La escena a la que cambiará
    }

    public EscenaCambio[] cambiosDeEscena;
    public string escenaPersistente = "PersistentScene"; // Nombre de la escena persistente

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SceneChange"))
        {
            string escenaActual = ObtenerEscenaActual();
            EscenaCambio cambio = System.Array.Find(cambiosDeEscena, c => c.escenaActual == escenaActual);

            if (cambio != null)
            {
                GameSceneManager.Instance.LoadScene(cambio.escenaDestino);
            }
            else
            {
                Debug.LogWarning($"No se ha definido un cambio de escena para la escena actual: {escenaActual}");
            }
        }
    }

    private string ObtenerEscenaActual()
    {
        Scene[] escenasCargadas = new Scene[SceneManager.sceneCount];
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            escenasCargadas[i] = SceneManager.GetSceneAt(i);
        }

        foreach (Scene escena in escenasCargadas)
        {
            if (escena.name != escenaPersistente && escena.isLoaded)
            {
                return escena.name; // Devuelve la primera escena que no sea la persistente
            }
        }

        return SceneManager.GetActiveScene().name; // Si no encuentra otra, devuelve la activa
    }
}
