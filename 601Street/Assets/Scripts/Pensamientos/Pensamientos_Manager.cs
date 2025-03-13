using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class Pensamientos_Manager : MonoBehaviour
{
    public static Pensamientos_Manager Instance;

    public GameObject pensamientoUI;
    public TMP_Text pensamientoText;
    private Coroutine pensamientoSecundarioCoroutine;
    private Pensamiento pensamientoActual;
    private Pensamiento[] todosPensamientos;

    public EscenaConfig[] configuracionesPorEscena;
    private EscenaConfig configActual;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ConfigurarParaEscena(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene escena, LoadSceneMode modo)
    {
        ConfigurarParaEscena(escena.name);
    }

    private void ConfigurarParaEscena(string nombreEscena)
    {
        configActual = System.Array.Find(configuracionesPorEscena, c => c.nombreEscena == nombreEscena);

        pensamientoUI.SetActive(false);
        todosPensamientos = Object.FindObjectsByType<Pensamiento>(FindObjectsSortMode.None);

        if (configActual != null && configActual.activarPensamientoInicial)
        {
            MostrarPensamiento(configActual.pensamientoInicioTexto);
        }
    }

    public Pensamiento GetPensamientoByID(int id)
    {
        if (todosPensamientos == null || todosPensamientos.Length == 0)
        {
            todosPensamientos = Object.FindObjectsByType<Pensamiento>(FindObjectsSortMode.None);
        }

        foreach (Pensamiento pensamiento in todosPensamientos)
        {
            if (pensamiento.Id == id)
            {
                return pensamiento;
            }
        }

        Debug.LogWarning($"No se encontró ningún pensamiento con ID {id}");
        return null;
    }

    private void OnTriggerEnter(Collider other)
    {
        Pensamiento nuevoPensamiento = other.GetComponent<Pensamiento>();
        if (nuevoPensamiento != null)
        {
            if (pensamientoSecundarioCoroutine != null)
            {
                StopCoroutine(pensamientoSecundarioCoroutine);
            }
            pensamientoActual = nuevoPensamiento;
            MostrarPensamiento(pensamientoActual.pensamientoPrincipal);
            pensamientoSecundarioCoroutine = StartCoroutine(RepetirPensamientoSecundario());
        }
    }

    public void MostrarPensamiento(string texto)
    {
        pensamientoText.text = texto;
        pensamientoUI.SetActive(true);
        StartCoroutine(DesactivarPensamiento());
    }

    private IEnumerator DesactivarPensamiento()
    {
        yield return new WaitForSeconds(5);
        pensamientoUI.SetActive(false);
    }

    private IEnumerator RepetirPensamientoSecundario()
    {
        yield return new WaitForSeconds(60);
        while (true)
        {
            MostrarPensamiento(pensamientoActual.pensamientoSecundario);
            yield return new WaitForSeconds(60);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


}

[System.Serializable]
public class EscenaConfig
{
    public string nombreEscena;
    public bool activarPensamientoInicial;
    public string pensamientoInicioTexto;
}
