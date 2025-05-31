using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class PuzzleObject : MonoBehaviour
{
    [Header("Puzzle Object Configuration")]
    [SerializeField] private PuzzleObjectType objectType;
    [SerializeField] private string collectMessage = "Has recogido un objeto para el puzzle.";
    [SerializeField] private GameObject objectImage; // Reference to the image that will be activated

    private InteractableObject interactableObject;

    private void Awake()
    {
        interactableObject = GetComponent<InteractableObject>();

        // Ensure the object image is deactivated by default
        if (objectImage != null)
        {
            objectImage.SetActive(false);
        }
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

            // Activate the object image
            if (objectImage != null)
            {
                objectImage.SetActive(true);
            }

            // Deactivate the object so it can't be interacted with again
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