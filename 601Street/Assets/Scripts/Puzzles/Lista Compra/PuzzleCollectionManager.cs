using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class PuzzleCollectionManager : MonoBehaviour
{
    public static PuzzleCollectionManager Instance { get; private set; }

    [System.Serializable]
    public class CollectibleStatus
    {
        public CollectibleType type;
        public bool collected = false;
        public GameObject feedbackUI; // Referencia opcional a un elemento UI que muestra el estado
    }

    [Header("Configuración de objetos")]
    [SerializeField] private List<CollectibleStatus> collectibles = new List<CollectibleStatus>();

    [Header("Eventos")]
    [Tooltip("Evento que se dispara cuando se recogen todos los objetos (no cuando se completa el puzzle)")]
    public UnityEvent OnAllObjectsCollected;

    [Tooltip("Evento que se dispara cuando se completa el puzzle mediante EndPuzzle()")]
    public UnityEvent OnPuzzleCompleted;

    [Tooltip("Evento que se dispara cuando se recoge un objeto específico")]
    public UnityEvent<CollectibleType> OnObjectCollected;

    [Header("Depuración")]
    [SerializeField] private bool showDebugMessages = true;

    // Contador interno de objetos recogidos
    private int collectedCount = 0;

    // Flag para saber si todos los objetos han sido recogidos
    private bool allObjectsCollected = false;

    // Flag para saber si el puzzle ha sido completado
    private bool puzzleCompleted = false;

    public GameObject aplleObject;

    [Header("Elementos Fracaso Guardia 2")]
    public GameObject puertaSecta;
    public GameObject ganzua;

    private GameStateController gameStateController;
    [Header("Gestión de estados")]
    [SerializeField] private string Estado_Riku_Exito;
    [SerializeField] private string Estado_Riku_Fracaso;
    [SerializeField] private string Estado_Riku_FracasoGuardia;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Inicializar la lista de objetos si no está configurada
        if (collectibles.Count == 0)
        {
            // Crear entradas para cada tipo de objeto
            foreach (CollectibleType type in System.Enum.GetValues(typeof(CollectibleType)))
            {
                collectibles.Add(new CollectibleStatus { type = type, collected = false });
            }
        }
        aplleObject.SetActive(false); // Desactivar el objeto de la manzana al inicio
        UpdateAllFeedbackUI();

        gameStateController = GetComponent<GameStateController>();
    }

    public void CollectObject(CollectibleType type)
    {
        // Buscar el objeto en nuestra lista
        CollectibleStatus targetCollectible = collectibles.Find(c => c.type == type);

        if (targetCollectible != null && !targetCollectible.collected)
        {
            // Marcar como recogido
            targetCollectible.collected = true;
            collectedCount++;

            if (showDebugMessages)
            {
                Debug.Log($"Objeto recogido: {type} ({collectedCount}/{collectibles.Count})");
            }

            // Activar feedback UI (la imagen aparece)
            UpdateFeedbackUI(targetCollectible);

            // Disparar evento de objeto recogido
            OnObjectCollected?.Invoke(type);

            // Verificar si hemos recogido todos los objetos
            CheckAllObjectsCollected();
        }
        else if (targetCollectible != null && targetCollectible.collected)
        {
            if (showDebugMessages)
            {
                Debug.Log($"El objeto {type} ya ha sido recogido.");
            }
        }
        else
        {
            Debug.LogWarning($"Tipo de objeto no encontrado en la lista: {type}");
        }
    }

    private void CheckAllObjectsCollected()
    {
        // Verificar si todos los objetos han sido recogidos
        bool allCollected = true;
        foreach (var collectible in collectibles)
        {
            if (!collectible.collected)
            {
                allCollected = false;
                break;
            }
        }

        // Si todos los objetos han sido recogidos (pero el puzzle no está completado aún)
        if (allCollected && !allObjectsCollected)
        {
            allObjectsCollected = true;

            if (showDebugMessages)
            {
                Debug.Log("¡Todos los objetos recogidos! Ahora se puede finalizar el puzzle con EndPuzzle()");
            }

            // Disparar evento de todos los objetos recogidos
            OnAllObjectsCollected?.Invoke();
        }
    }

    /// <summary>
    /// Función para finalizar el puzzle. Solo funciona si todos los objetos han sido recogidos.
    /// </summary>
    public void EndPuzzle()
    {
        if (!allObjectsCollected)
        {
            if (showDebugMessages)
            {
                Debug.LogWarning("No se puede finalizar el puzzle. Aún faltan objetos por recoger.");
            }
            return;
        }

        if (puzzleCompleted)
        {
            if (showDebugMessages)
            {
                Debug.Log("El puzzle ya ha sido completado.");
            }
            return;
        }

        // Marcar el puzzle como completado
        puzzleCompleted = true;

        if (showDebugMessages)
        {
            Debug.Log("¡Puzzle completado exitosamente!");
        }

        // Aquí puedes ocultar las imágenes de los objetos si lo deseas
        HideAllFeedbackUI();

        // Disparar evento de puzzle completado
        OnPuzzleCompleted?.Invoke();
    }

    // Método para actualizar un elemento UI específico
    private void UpdateFeedbackUI(CollectibleStatus collectible)
    {
        if (collectible.feedbackUI != null)
        {
            collectible.feedbackUI.SetActive(collectible.collected);
        }
    }

    // Método para actualizar todos los elementos UI
    private void UpdateAllFeedbackUI()
    {
        foreach (var collectible in collectibles)
        {
            UpdateFeedbackUI(collectible);
        }
    }

    // Método para ocultar todas las imágenes de feedback UI
    private void HideAllFeedbackUI()
    {
        foreach (var collectible in collectibles)
        {
            if (collectible.feedbackUI != null)
            {
                collectible.feedbackUI.SetActive(false);
            }
        }
    }

    // Método para reiniciar el puzzle
    public void ResetCollection()
    {
        foreach (var collectible in collectibles)
        {
            collectible.collected = false;
        }

        collectedCount = 0;
        allObjectsCollected = false;
        puzzleCompleted = false;
        UpdateAllFeedbackUI();

        if (showDebugMessages)
        {
            Debug.Log("Colección reiniciada");
        }
    }

    // Método para verificar si un objeto específico ha sido recogido
    public bool IsObjectCollected(CollectibleType type)
    {
        CollectibleStatus collectible = collectibles.Find(c => c.type == type);
        return collectible != null && collectible.collected;
    }

    // Método para obtener la cantidad de objetos recogidos
    public int GetCollectedCount()
    {
        return collectedCount;
    }

    // Método para verificar si todos los objetos han sido recogidos
    public bool AreAllObjectsCollected()
    {
        return allObjectsCollected;
    }

    // Método para verificar si el puzzle ha sido completado
    public bool IsPuzzleCompleted()
    {
        return puzzleCompleted;
    }

    public void ActivateApple()
    {
        gameStateController.ChangeGameState(Estado_Riku_Exito);
    }

    public void FracasoRiku()
    {
        gameStateController.ChangeGameState(Estado_Riku_Fracaso);
    }

    public void FracasoSectario_2()
    {
        gameStateController.ChangeGameState(Estado_Riku_FracasoGuardia);
    }
}