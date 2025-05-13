using UnityEngine;

public enum CollectibleType
{
    Manzana,
    Dango,
    Abanico
}

public class CollectibleObject : MonoBehaviour
{
    [Header("Configuraci�n")]
    [Tooltip("Tipo de objeto coleccionable")]
    [SerializeField] private CollectibleType collectibleType;

    [Tooltip("Referencia al componente InteractableObject")]
    [SerializeField] private InteractableObject interactableObject;

    private PuzzleCollectionManager puzzleManager;

    public CollectibleType GetCollectibleType()
    {
        return collectibleType;
    }

    private void Awake()
    {
        // Si no hay una referencia al InteractableObject, intentar obtenerla
        if (interactableObject == null)
        {
            interactableObject = GetComponent<InteractableObject>();
            if (interactableObject == null)
            {
                Debug.LogError("No se encontr� componente InteractableObject en " + gameObject.name);
            }
        }
    }

    private void Start()
    {
        puzzleManager = Object.FindFirstObjectByType<PuzzleCollectionManager>();
        if (puzzleManager == null)
        {
            Debug.LogError("No se encontr� PuzzleCollectionManager en la escena");
            return;
        }

        if (interactableObject != null)
        {
            interactableObject.onInteraction.AddListener(OnObjectInteraction);
        }
    }


    public void OnObjectInteraction()
    {
        if (puzzleManager != null)
        {
            puzzleManager.CollectObject(collectibleType);
        }
    }
}