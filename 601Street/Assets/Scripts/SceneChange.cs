using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    [System.Serializable]
    public class EscenaCambio
    {
        public string escenaActual;       // La escena donde est� el objeto
        public string escenaAnterior;     // La escena a la que cambiar� si el trigger es "Escena_Anterior"
        public string escenaSiguiente;    // La escena a la que cambiar� si el trigger es "Escena_Siguiente"
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
                string escenaDestino = "";
                bool usarPuntoSalida = false;

                // Determinar qu� escena cargar basado en el nombre del objeto trigger
                if (other.gameObject.name == "Escena_Anterior")
                {
                    escenaDestino = cambio.escenaAnterior;
                    usarPuntoSalida = true;
                }
                else if (other.gameObject.name == "Escena_Siguiente")
                {
                    escenaDestino = cambio.escenaSiguiente;
                }
                else
                {
                    Debug.LogWarning($"Trigger de cambio de escena no reconocido: {other.gameObject.name}");
                    return;
                }

                // Verificar que la escena destino tenga un valor v�lido
                if (!string.IsNullOrEmpty(escenaDestino))
                {
                    // Llamar al m�todo de cambio de escena con el par�metro de punto de salida
                    GameSceneManager.Instance.LoadScene(escenaDestino, usarPuntoSalida);
                }
                else
                {
                    Debug.LogWarning($"No se ha definido un destino para {other.gameObject.name} en la escena actual: {escenaActual}");
                }
            }
            else
            {
                Debug.LogWarning($"No se ha definido un cambio de escena para la escena actual: {escenaActual}");
            }
        }
    }

    public void CambiarEscenaConPuntoSalida(string escenaDestino)
    {
        if (!string.IsNullOrEmpty(escenaDestino))
        {
            GameSceneManager.Instance.LoadScene(escenaDestino, true);
        }
        else
        {
            Debug.LogWarning("Escena destino no especificada para cambio con punto de salida.");
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