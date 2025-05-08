using UnityEngine;

[RequireComponent(typeof(InteractableObject))]
public class PuzzleTable : MonoBehaviour
{
    [Header("Puzzle Table Configuration")]
    [SerializeField] private Transform[] objectPlacementPositions;

    private InteractableObject interactableObject;

    private void Awake()
    {
        interactableObject = GetComponent<InteractableObject>();
    }

    private void Start()
    {
        // If there are no placement positions assigned, log a warning
        if (objectPlacementPositions == null || objectPlacementPositions.Length == 0)
        {
            Debug.LogWarning("No object placement positions assigned to the puzzle table.");
        }
    }

    // This method is called when the player interacts with the table
    public void OnTableInteracted()
    {
        if (PuzzleSystem.Instance != null)
        {
            // Check if all objects are collected and place them if they are
            PuzzleSystem.Instance.CheckAndPlaceObjects();
        }
        else
        {
            Debug.LogError("PuzzleSystem instance not found!");
        }
    }

    // Get the position for a specific object type
    public Transform GetPositionForObjectType(PuzzleObjectType type)
    {
        int index = (int)type;
        if (objectPlacementPositions != null && index < objectPlacementPositions.Length)
        {
            return objectPlacementPositions[index];
        }
        return null;
    }

    // Helper method to visualize the object positions in the editor
    private void OnDrawGizmos()
    {
        if (objectPlacementPositions != null)
        {
            foreach (Transform position in objectPlacementPositions)
            {
                if (position != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(position.position, 0.1f);
                    Gizmos.DrawLine(transform.position, position.position);
                }
            }
        }
    }
}