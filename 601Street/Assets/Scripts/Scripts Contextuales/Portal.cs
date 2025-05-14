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

    // Funci�n que se puede asignar a un bot�n de la UI para reiniciar la aplicaci�n
    public void ReiniciarAplicacion()
    {
        Debug.Log("Reiniciando aplicaci�n...");

        // Recarga la escena inicial (escena 0 en el build index)
        SceneManager.LoadScene(0);

        // Alternativa: recargar la escena actual
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        // Si necesitas tambi�n limpiar los datos guardados:
        PlayerPrefs.DeleteAll();

        // Aseg�rate de que el recolector de basura libere memoria
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}