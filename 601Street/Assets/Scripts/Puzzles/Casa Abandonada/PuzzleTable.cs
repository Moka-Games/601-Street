using UnityEngine;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(InteractableObject))]
public class PuzzleTable : MonoBehaviour
{
    [Header("Puzzle Table Configuration")]
    [SerializeField] private Transform[] objectPlacementPositions;

    [Header("Warning Configuration")]
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private float warningDuration = 2f;
    [SerializeField] private float fadeInTime = 0.3f;
    [SerializeField] private float fadeOutTime = 0.5f;

    private InteractableObject interactableObject;

    private void Awake()
    {
        interactableObject = GetComponent<InteractableObject>();

        // Ensure warning text is hidden at the start
        if (warningText != null)
        {
            warningText.alpha = 0f;
            warningText.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // If there are no placement positions assigned, log a warning
        if (objectPlacementPositions == null || objectPlacementPositions.Length == 0)
        {
            Debug.LogWarning("No object placement positions assigned to the puzzle table.");
        }

        // Subscribe to the interaction event
        if (interactableObject != null)
        {
            interactableObject.onInteraction.AddListener(OnTableInteracted);
        }
    }

    // This method is called when the player interacts with the table
    public void OnTableInteracted()
    {
        if (PuzzleSystem.Instance != null)
        {
            int collectedCount = PuzzleSystem.Instance.GetCollectedObjectsCount();

            // If not all objects are collected, show the warning
            if (collectedCount < 3)
            {
                ShowWarningMessage();
            }

            // Check if all objects are collected and place them if they are
            PuzzleSystem.Instance.CheckAndPlaceObjects();

            // If all objects are collected, deactivate the object images (they'll be shown on the table)
            if (collectedCount == 3)
            {
                DeactivateObjectImages();
            }
        }
        else
        {
            Debug.LogError("PuzzleSystem instance not found!");
        }
    }

    // Show the warning message with animation
    private void ShowWarningMessage()
    {
        if (warningText == null) return;

        // Stop any running animations
        warningText.DOKill();

        // Make sure the text is visible
        warningText.gameObject.SetActive(true);

        // Animation sequence
        Sequence warningSequence = DOTween.Sequence();

        // Fade in
        warningSequence.Append(warningText.DOFade(1f, fadeInTime));

        // Wait
        warningSequence.AppendInterval(warningDuration);

        // Fade out
        warningSequence.Append(warningText.DOFade(0f, fadeOutTime));

        // Hide the game object when done
        warningSequence.OnComplete(() => warningText.gameObject.SetActive(false));

        // Play the sequence
        warningSequence.Play();
    }

    // Deactivate all object images when the puzzle is solved
    private void DeactivateObjectImages()
    {
        // This method will be called when the puzzle is solved
        // It will deactivate the object images as they'll be shown on the table

        // Find all PuzzleObject components in the scene
        PuzzleObject[] puzzleObjects = FindObjectsByType<PuzzleObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PuzzleObject puzzleObj in puzzleObjects)
        {
            // Get the objectImage field using reflection (since it's private)
            System.Reflection.FieldInfo field = typeof(PuzzleObject).GetField("objectImage",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (field != null)
            {
                GameObject objectImage = field.GetValue(puzzleObj) as GameObject;
                if (objectImage != null)
                {
                    objectImage.SetActive(false);
                }
            }
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