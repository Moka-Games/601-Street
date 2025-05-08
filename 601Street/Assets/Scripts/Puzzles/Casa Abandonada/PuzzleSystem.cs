using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Enum to identify each puzzle object
public enum PuzzleObjectType
{
    Cubo,
    Tren,
    Peluche
}

// Class for managing the puzzle state
public class PuzzleSystem : MonoBehaviour
{
    // Singleton instance
    public static PuzzleSystem Instance { get; private set; }

    [Header("Puzzle Configuration")]
    [SerializeField] private Transform tableTransform;
    [SerializeField] private Transform[] objectPositions; // Positions where objects will be placed
    [SerializeField] private GameObject[] objectPrefabs; // Optional prefabs to instantiate for visual feedback

    [Header("Feedback")]
    [SerializeField] private string missingAllObjectsMessage = "Necesitas encontrar 3 objetos para resolver el puzzle.";
    [SerializeField] private string missingTwoObjectsMessage = "Aún necesitas 2 objetos más.";
    [SerializeField] private string missingOneObjectMessage = "Te falta 1 objeto más.";
    [SerializeField] private string puzzleCompletedMessage = "¡Puzzle completado!";

    [Header("Events")]
    public UnityEvent OnPuzzleCompleted;
    public UnityEvent<string> OnShowMessage;

    // Puzzle state
    private Dictionary<PuzzleObjectType, bool> collectedObjects = new Dictionary<PuzzleObjectType, bool>();
    private Dictionary<PuzzleObjectType, GameObject> placedObjects = new Dictionary<PuzzleObjectType, GameObject>();
    private bool puzzleCompleted = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize the dictionary
        foreach (PuzzleObjectType type in System.Enum.GetValues(typeof(PuzzleObjectType)))
        {
            collectedObjects[type] = false;
            placedObjects[type] = null;
        }
    }

    // Called when a puzzle object is collected
    public void CollectObject(PuzzleObjectType objectType)
    {
        if (!collectedObjects[objectType])
        {
            collectedObjects[objectType] = true;
            Debug.Log($"Collected puzzle object: {objectType}");
        }
    }

    // Check if all objects are collected
    public bool AreAllObjectsCollected()
    {
        foreach (var item in collectedObjects)
        {
            if (!item.Value) return false;
        }
        return true;
    }

    // Get the count of collected objects
    public int GetCollectedObjectsCount()
    {
        int count = 0;
        foreach (var item in collectedObjects)
        {
            if (item.Value) count++;
        }
        return count;
    }

    // Check if a specific object is collected
    public bool IsObjectCollected(PuzzleObjectType objectType)
    {
        return collectedObjects.ContainsKey(objectType) && collectedObjects[objectType];
    }

    // Called when player interacts with the table
    public void CheckAndPlaceObjects()
    {
        if (puzzleCompleted)
        {
            OnShowMessage?.Invoke(puzzleCompletedMessage);
            return;
        }

        int collectedCount = GetCollectedObjectsCount();

        if (collectedCount < 3)
        {
            // Show appropriate message based on how many objects are missing
            switch (collectedCount)
            {
                case 0:
                    OnShowMessage?.Invoke(missingAllObjectsMessage);
                    break;
                case 1:
                    OnShowMessage?.Invoke(missingTwoObjectsMessage);
                    break;
                case 2:
                    OnShowMessage?.Invoke(missingOneObjectMessage);
                    break;
            }
            return;
        }

        // If we have all objects, place them on the table
        PlaceObjectsOnTable();
        OnShowMessage?.Invoke(puzzleCompletedMessage);
        OnPuzzleCompleted?.Invoke();
        puzzleCompleted = true;
    }

    // Place collected objects on the table at their designated positions
    private void PlaceObjectsOnTable()
    {
        int index = 0;
        foreach (PuzzleObjectType type in System.Enum.GetValues(typeof(PuzzleObjectType)))
        {
            if (collectedObjects[type] && index < objectPositions.Length)
            {
                // If we have prefabs, instantiate them at the positions
                if (objectPrefabs.Length > (int)type && objectPrefabs[(int)type] != null)
                {
                    if (placedObjects[type] == null) // Only instantiate if not already placed
                    {
                        GameObject obj = Instantiate(objectPrefabs[(int)type], objectPositions[index].position, objectPositions[index].rotation);
                        obj.transform.SetParent(tableTransform);
                        placedObjects[type] = obj;
                    }
                    else
                    {
                        // Move existing object to position
                        placedObjects[type].transform.position = objectPositions[index].position;
                        placedObjects[type].transform.rotation = objectPositions[index].rotation;
                        placedObjects[type].transform.SetParent(tableTransform);
                    }
                }
                index++;
            }
        }
    }

    // Reset the puzzle (for testing or restarting)
    public void ResetPuzzle()
    {
        foreach (PuzzleObjectType type in System.Enum.GetValues(typeof(PuzzleObjectType)))
        {
            collectedObjects[type] = false;
            if (placedObjects[type] != null)
            {
                Destroy(placedObjects[type]);
                placedObjects[type] = null;
            }
        }
        puzzleCompleted = false;
    }
}