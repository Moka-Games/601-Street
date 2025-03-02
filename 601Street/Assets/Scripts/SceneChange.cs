using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    [System.Serializable]
    public class EscenaCambio
    {
        public string escenaActual;
        public string escenaAnterior;
        public string escenaSiguiente;
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
                bool isBackward = false;

                // Determinar qué escena cargar basado en el nombre del objeto trigger
                if (other.gameObject.name == "Escena_Anterior")
                {
                    escenaDestino = cambio.escenaAnterior;
                    isBackward = true; // Estamos retrocediendo a una escena anterior
                }
                else if (other.gameObject.name == "Escena_Siguiente")
                {
                    escenaDestino = cambio.escenaSiguiente;
                    isBackward = false; // Estamos avanzando a una escena siguiente
                }
                else
                {
                    Debug.LogWarning($"Trigger de cambio de escena no reconocido: {other.gameObject.name}");
                    return;
                }

                // Verificar que la escena destino tenga un valor válido
                if (!string.IsNullOrEmpty(escenaDestino))
                {
                    // Pasamos el parámetro isBackward para saber si estamos regresando
                    GameSceneManager.Instance.LoadScene(escenaDestino, isBackward);
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