using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class PuzzleObject : MonoBehaviour
{
    [Header("Puzzle Object Configuration")]
    [SerializeField] private PuzzleObjectType objectType;
    [SerializeField] private string collectMessage = "Has recogido un objeto para el puzzle.";

    private InteractableObject interactableObject;

    private void Awake()
    {
        interactableObject = GetComponent<InteractableObject>();
    }

    private void Start()
    {
        // Subscribe to the interaction event if it's not already done in the inspector
        if (interactableObject != null)
        {
            interactableObject.onInteraction.AddListener(OnObjectInteracted);
        }
        else
        {
            Debug.LogError("InteractableObject component is missing on " + gameObject.name);
        }
    }

    // This method is called when the player interacts with this object
    public void OnObjectInteracted()
    {
        if (PuzzleSystem.Instance != null)
        {
            // Register this object as collected
            PuzzleSystem.Instance.CollectObject(objectType);
             
            // Show collection message to player
            if (PuzzleSystem.Instance.OnShowMessage != null)
            {
                PuzzleSystem.Instance.OnShowMessage.Invoke(collectMessage);
            }

            // Deactivate the object so it can't be interacted with again
            // Alternatively, you could destroy it or handle it differently based on your game's needs
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("PuzzleSystem instance not found!");
        }
    }

    // This allows you to set the puzzle object type from another script if needed
    public void SetPuzzleObjectType(PuzzleObjectType type)
    {
        objectType = type;
    }
}