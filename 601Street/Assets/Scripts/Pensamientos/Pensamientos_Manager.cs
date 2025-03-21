using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
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

    // Queue to store pending thoughts
    private Queue<string> pendingThoughts = new Queue<string>();

    // Flag to indicate if we're processing the pending thoughts queue
    private bool isProcessingPendingThoughts = false;

    public int timeBetweenThoughts;
    public float showThoughtFor;

    private bool isPendingSecondaryThought = false;
    private float remainingTimeForSecondaryThought = 0f;
    private bool isShowingThought = false;
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

        // Subscribe to game state changes
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
        else
        {
            Debug.LogError("GameStateManager not found! PensamientosManager requires GameStateManager to function properly.");
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        // Si entramos en el estado gameplay
        if (newState == GameState.OnGameplay)
        {
            // Si hay pensamientos pendientes en la cola, procesarlos
            if (pendingThoughts.Count > 0 && !isProcessingPendingThoughts)
            {
                StartCoroutine(ProcessPendingThoughts());
            }

            // Si había un pensamiento secundario pendiente, reanudar la corutina
            if (isPendingSecondaryThought && pensamientoActual != null)
            {
                if (pensamientoSecundarioCoroutine != null)
                {
                    StopCoroutine(pensamientoSecundarioCoroutine);
                }
                pensamientoSecundarioCoroutine = StartCoroutine(ResumeSecondaryThought());
            }
        }
        else
        {
            // Si salimos del estado gameplay y hay una corutina activa de pensamiento secundario,
            // guardamos el tiempo restante
            if (pensamientoSecundarioCoroutine != null && pensamientoActual != null)
            {
                isPendingSecondaryThought = true;
                // No necesitamos hacer nada más porque el tiempo restante se actualiza continuamente
            }
        }
    }

    private IEnumerator ResumeSecondaryThought()
    {
        // Si estamos mostrando un pensamiento, esperamos a que termine más un margen
        if (isShowingThought)
        {
            yield return new WaitUntil(() => !isShowingThought);
            yield return new WaitForSeconds(1f); // Pequeño margen adicional
        }

        // Esperamos el tiempo restante que quedaba
        if (remainingTimeForSecondaryThought > 0)
        {
            yield return new WaitForSeconds(remainingTimeForSecondaryThought);
        }

        // Mostramos el pensamiento y reiniciamos el ciclo completo
        if (GameStateManager.Instance.IsInGameplayState())
        {
            MostrarPensamiento(pensamientoActual.pensamientoSecundario);
        }

        // Reiniciamos el ciclo normal
        isPendingSecondaryThought = false;
        remainingTimeForSecondaryThought = 0f;
        pensamientoSecundarioCoroutine = StartCoroutine(RepetirPensamientoSecundario());
    }

    private IEnumerator ProcessPendingThoughts()
    {
        isProcessingPendingThoughts = true;

        while (pendingThoughts.Count > 0 && GameStateManager.Instance.IsInGameplayState())
        {
            string thought = pendingThoughts.Dequeue();
            DisplayThought(thought);

            // Wait until the thought is no longer displayed
            yield return new WaitForSeconds(showThoughtFor);
            yield return new WaitUntil(() => !pensamientoUI.activeSelf);

            // Small delay between thoughts
            yield return new WaitForSeconds(1);
        }

        isProcessingPendingThoughts = false;
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
        // Check if we're in gameplay state
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsInGameplayState())
        {
            // If we're in gameplay state, display the thought immediately
            DisplayThought(texto);
        }
        else
        {
            // If we're not in gameplay state, queue the thought
            pendingThoughts.Enqueue(texto);
            Debug.Log($"Pensamiento en espera: \"{texto}\" (total en cola: {pendingThoughts.Count})");
        }
    }

    // Internal method to actually display the thought
    private void DisplayThought(string texto)
    {
        isShowingThought = true;
        pensamientoText.text = texto;
        pensamientoUI.SetActive(true);
        StartCoroutine(DesactivarPensamiento());
    }

    private IEnumerator DesactivarPensamiento()
    {
        yield return new WaitForSeconds(showThoughtFor);
        pensamientoUI.SetActive(false);
        isShowingThought = false;
    }
    private IEnumerator RepetirPensamientoSecundario()
    {
        // Tiempo total a esperar
        float totalWaitTime = timeBetweenThoughts;
        remainingTimeForSecondaryThought = totalWaitTime;

        // Iniciamos el contador
        float elapsedTime = 0f;

        while (elapsedTime < totalWaitTime)
        {
            // Solo contamos el tiempo cuando estamos en estado gameplay
            if (GameStateManager.Instance.IsInGameplayState())
            {
                elapsedTime += Time.deltaTime;
                remainingTimeForSecondaryThought = totalWaitTime - elapsedTime;
            }

            // Si salimos del estado gameplay, la corutina sigue activa pero no incrementa el tiempo
            yield return null;
        }

        // Cuando completamos el tiempo, mostramos el pensamiento si estamos en estado gameplay
        if (GameStateManager.Instance.IsInGameplayState())
        {
            MostrarPensamiento(pensamientoActual.pensamientoSecundario);
        }
        else
        {
            // Si no estamos en gameplay, marcamos que hay un pensamiento pendiente
            isPendingSecondaryThought = true;
            yield break; // Terminamos esta corutina
        }

        // Reiniciamos el ciclo
        pensamientoSecundarioCoroutine = StartCoroutine(RepetirPensamientoSecundario());
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Unsubscribe from game state changes
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }
}