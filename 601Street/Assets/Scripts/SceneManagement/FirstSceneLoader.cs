using UnityEngine;
using System.Collections;

public class FirstSceneLoader : MonoBehaviour
{
    [Tooltip("Tiempo de espera antes de cargar la primera escena (segundos)")]
    [SerializeField] private float tiempoDelay = 0.5f;

    [Tooltip("Este objeto se autodestruirá después de cargar la escena")]
    [SerializeField] private bool destruirDespuesDeCargar = true;

    private void Start()
    {
        StartCoroutine(CargarPrimeraEscena());
    }

    private IEnumerator CargarPrimeraEscena()
    {
        // Esperar a que todo esté inicializado
        yield return new WaitForSeconds(tiempoDelay);

        // Obtener el nombre de la escena a cargar
        string escenaDestino = PlayerPrefs.GetString("NextSceneToLoad", "");

        if (!string.IsNullOrEmpty(escenaDestino))
        {
            Debug.Log($"FirstSceneLoader: Cargando escena inicial {escenaDestino}");

            // Usar GameSceneManager para cargar la escena
            GameSceneManager gsm = GameSceneManager.Instance;
            if (gsm != null)
            {
                gsm.LoadScene(escenaDestino);
            }
            else
            {
                Debug.LogWarning("GameSceneManager no encontrado, usando SceneManager directamente");
                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(escenaDestino,
                    UnityEngine.SceneManagement.LoadSceneMode.Additive);
            }

            // Limpiar PlayerPrefs para evitar cargas no deseadas
            PlayerPrefs.DeleteKey("NextSceneToLoad");

            // Autodestrucción opcional
            if (destruirDespuesDeCargar)
            {
                // Esperar un momento para asegurar que la carga ha iniciado
                yield return new WaitForSeconds(0.5f);
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.LogWarning("FirstSceneLoader: No se especificó escena para cargar en PlayerPrefs");
        }
    }
}