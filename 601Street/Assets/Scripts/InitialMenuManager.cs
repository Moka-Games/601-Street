using UnityEngine;
using UnityEngine.UI;

public class InitialMenuManager : MonoBehaviour
{
    [Header("Configuración de Inicio")]
    [Tooltip("Nombre de la primera escena del juego a cargar")]
    [SerializeField] private string primerEscenaJuego = "Colegio";

    [Header("Referencias")]
    [Tooltip("Objeto que contiene el Animator para la animación de transición")]
    [SerializeField] private GameObject objetoAnimacionTransicion;

    [Header("Referencias UI")]
    [SerializeField] private Button botonIniciarJuego;
    [SerializeField] private Button botonSalir;

    void Start()
    {
        // Asegurarse de que el objeto con la animación está desactivado inicialmente
        if (objetoAnimacionTransicion != null)
        {
            objetoAnimacionTransicion.SetActive(false);
        }

        // Configurar botones
        if (botonIniciarJuego != null)
        {
            botonIniciarJuego.onClick.AddListener(IniciarJuego);
        }

        if (botonSalir != null)
        {
            botonSalir.onClick.AddListener(SalirJuego);
        }

        // Configurar la escena a cargar para que TransitionController la use
        PlayerPrefs.SetString("NextSceneToLoad", primerEscenaJuego);

        // Preparar el GameSceneManager para carga directa
        GameSceneManager.SetupForDirectLoad();
    }

    // Método para el botón de inicio
    public void IniciarJuego()
    {
        // Desactivar el botón para evitar múltiples clics
        if (botonIniciarJuego != null)
        {
            botonIniciarJuego.interactable = false;
        }

        // Activar el objeto con la animación
        if (objetoAnimacionTransicion != null)
        {
            objetoAnimacionTransicion.SetActive(true);
        }
        else
        {
            // Si no hay objeto de animación, cargar directamente
            string escenaDestino = PlayerPrefs.GetString("NextSceneToLoad", primerEscenaJuego);
            UnityEngine.SceneManagement.SceneManager.LoadScene(escenaDestino);
        }
    }

    public void SalirJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}