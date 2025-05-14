using System.Collections.Generic;
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


    [Header("Configuraci�n de objetos")]
    [SerializeField] private List<CollectibleStatus> collectibles = new List<CollectibleStatus>();

    [Header("Eventos")]
    [Tooltip("Evento que se dispara cuando se completa la colecci�n")]
    public UnityEvent OnCompletedList;

    [Tooltip("Evento que se dispara cuando se recoge un objeto espec�fico")]
    public UnityEvent<CollectibleType> OnObjectCollected;

    [Header("Depuraci�n")]
    [SerializeField] private bool showDebugMessages = true;

    // Contador interno de objetos recogidos
    private int collectedCount = 0;

    public GameObject aplleObject; //Para el puesto de la manzana
    public GameObject sectGuard; 
    public GameObject sectGuard_Fracaso;
    public GameObject llamadaDaichi_Fracaso;
    public GameObject colliderGuardia;

    [Header("Elementos Fracaso Guardia 2")]
    public GameObject puertaSecta;
    public GameObject ganzua;

    public GameObject misi�nRikuFallado;
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
        // Inicializar la lista de objetos si no est� configurada
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

            // Activar feedback UI si existe
            UpdateFeedbackUI(targetCollectible);

            // Disparar evento de objeto recogido
            OnObjectCollected?.Invoke(type);

            // Verificar si hemos completado la colecci�n
            CheckCompletion();
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

    private void CheckCompletion()
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

        // Si todos los objetos han sido recogidos, disparar el evento
        if (allCollected)
        {
            if (showDebugMessages)
            {
                Debug.Log("�Colecci�n completada! Disparando evento OnCompletedList");
            }

            OnCompletedList?.Invoke();
        }
    }

    // M�todo para actualizar un elemento UI espec�fico
    private void UpdateFeedbackUI(CollectibleStatus collectible)
    {
        if (collectible.feedbackUI != null)
        {
            collectible.feedbackUI.SetActive(collectible.collected);
        }
    }

    // M�todo para actualizar todos los elementos UI
    private void UpdateAllFeedbackUI()
    {
        foreach (var collectible in collectibles)
        {
            UpdateFeedbackUI(collectible);
        }
    }

    // M�todo para reiniciar el puzzle
    public void ResetCollection()
    {
        foreach (var collectible in collectibles)
        {
            collectible.collected = false;
        }

        collectedCount = 0;
        UpdateAllFeedbackUI();

        if (showDebugMessages)
        {
            Debug.Log("Colecci�n reiniciada");
        }
    }

    // M�todo para verificar si un objeto espec�fico ha sido recogido
    public bool IsObjectCollected(CollectibleType type)
    {
        CollectibleStatus collectible = collectibles.Find(c => c.type == type);
        return collectible != null && collectible.collected;
    }

    // M�todo para obtener la cantidad de objetos recogidos
    public int GetCollectedCount()
    {
        return collectedCount;
    }

    public void ActivateApple()
    {
        aplleObject.SetActive(true);
        sectGuard.SetActive(false);
    }

    public void FracasoRiku()
    {
        sectGuard.SetActive(false);
        sectGuard_Fracaso.SetActive(true);
        misi�nRikuFallado.SetActive(true);
    }

    public void FracasoSectario_2()
    {
        llamadaDaichi_Fracaso.SetActive(true);
        colliderGuardia.SetActive(false);
        puertaSecta.SetActive(false);
        ganzua.SetActive(true);
    }

    
}