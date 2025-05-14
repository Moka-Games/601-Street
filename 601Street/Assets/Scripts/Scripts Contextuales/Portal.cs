using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cargar escenas

public class Portal : MonoBehaviour
{
    //public GameObject panelFinal;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //panelFinal.SetActive(true);
        }
    }

    // Función que se puede asignar a un botón de la UI para reiniciar la aplicación
    public void ReiniciarAplicacion()
    {
        Debug.Log("Reiniciando aplicación...");

        // Recarga la escena inicial (escena 0 en el build index)
        SceneManager.LoadScene(0);

        // Alternativa: recargar la escena actual
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        // Si necesitas también limpiar los datos guardados:
        PlayerPrefs.DeleteAll();

        // Asegúrate de que el recolector de basura libere memoria
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}