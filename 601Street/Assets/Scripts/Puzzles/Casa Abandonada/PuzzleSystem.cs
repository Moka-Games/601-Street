using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening; // Agregado para DOTween

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

    [Header("Animation Settings")]
    [SerializeField] private float dropHeight = 2f; // Altura desde donde caen los objetos
    [SerializeField] private float animationDuration = 1f; // Duración de la animación de caída
    [SerializeField] private float delayBetweenObjects = 0.2f; // Delay entre cada objeto
    [SerializeField] private Ease dropEase = Ease.OutBounce; // Tipo de easing para la caída

    [Header("Events")]
    public UnityEvent OnPuzzleCompleted;
    public UnityEvent<string> OnShowMessage;
    public UnityEvent OnObjectsPlacementStarted; // Nuevo evento para cuando inicia la colocación
    public UnityEvent OnObjectsPlacementCompleted; // Nuevo evento para cuando termina la colocación

    // Puzzle state
    private Dictionary<PuzzleObjectType, bool> collectedObjects = new Dictionary<PuzzleObjectType, bool>();
    private Dictionary<PuzzleObjectType, GameObject> placedObjects = new Dictionary<PuzzleObjectType, GameObject>();
    private bool puzzleCompleted = false;
    private bool isAnimatingPlacement = false; // Flag para evitar múltiples animaciones

    // To keep track of the object images
    private Dictionary<PuzzleObjectType, GameObject> objectImages = new Dictionary<PuzzleObjectType, GameObject>();

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
            objectImages[type] = null;
        }
    }

    public void CollectObject(PuzzleObjectType objectType)
    {
        if (!collectedObjects[objectType])
        {
            collectedObjects[objectType] = true;
            Debug.Log($"Collected puzzle object: {objectType}");
        }
    }

    // Register an object image for a specific puzzle object type
    public void RegisterObjectImage(PuzzleObjectType objectType, GameObject image)
    {
        if (image != null)
        {
            objectImages[objectType] = image;
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

        // Evitar múltiples animaciones simultáneas
        if (isAnimatingPlacement)
        {
            Debug.Log("Ya se está ejecutando la animación de colocación de objetos");
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

        // If we have all objects, place them on the table with animation
        StartCoroutine(PlaceObjectsWithAnimation());
    }

    // Corrutina para manejar la colocación animada de objetos
    private IEnumerator PlaceObjectsWithAnimation()
    {
        isAnimatingPlacement = true;
        OnObjectsPlacementStarted?.Invoke();

        // Mostrar mensaje de puzzle completado al inicio de la animación
        OnShowMessage?.Invoke(puzzleCompletedMessage);

        List<PuzzleObjectType> objectsToPlace = new List<PuzzleObjectType>();

        // Recopilar todos los objetos que necesitan ser colocados
        foreach (PuzzleObjectType type in System.Enum.GetValues(typeof(PuzzleObjectType)))
        {
            if (collectedObjects[type])
            {
                objectsToPlace.Add(type);
            }
        }

        // Colocar cada objeto con delay
        for (int i = 0; i < objectsToPlace.Count && i < objectPositions.Length; i++)
        {
            PuzzleObjectType type = objectsToPlace[i];
            Transform targetPosition = objectPositions[i];

            // Instanciar y animar el objeto
            yield return StartCoroutine(InstantiateAndAnimateObject(type, targetPosition, i));

            // Esperar antes del siguiente objeto
            if (i < objectsToPlace.Count - 1)
            {
                yield return new WaitForSeconds(delayBetweenObjects);
            }
        }

        // Completar el puzzle
        puzzleCompleted = true;
        OnPuzzleCompleted?.Invoke();
        OnObjectsPlacementCompleted?.Invoke();

        // Disable the object images since the puzzle is now solved
        DisableAllObjectImages();

        // Try to access MisionManager - if it exists, complete the current mission
        if (typeof(MisionManager).Assembly.GetType("MisionManager") != null &&
            MisionManager.Instance != null)
        {
            MisionManager.Instance.CompletarMisionActual();
        }

        isAnimatingPlacement = false;
        Debug.Log("Puzzle completado con animación");
    }

    // Corrutina para instanciar y animar un objeto individual
    private IEnumerator InstantiateAndAnimateObject(PuzzleObjectType type, Transform targetPosition, int objectIndex)
    {
        // Verificar si tenemos un prefab para este tipo de objeto
        if (objectPrefabs.Length <= (int)type || objectPrefabs[(int)type] == null)
        {
            Debug.LogWarning($"No hay prefab asignado para el objeto tipo: {type}");
            yield break;
        }

        GameObject obj = null;

        // Si el objeto ya existe, usarlo; si no, crear uno nuevo
        if (placedObjects[type] != null)
        {
            obj = placedObjects[type];
        }
        else
        {
            // Instanciar el objeto en la posición elevada
            Vector3 startPosition = targetPosition.position + Vector3.up * dropHeight;
            obj = Instantiate(objectPrefabs[(int)type], startPosition, targetPosition.rotation);
            obj.transform.SetParent(tableTransform);
            placedObjects[type] = obj;
        }

        if (obj == null)
        {
            Debug.LogError($"No se pudo crear el objeto para tipo: {type}");
            yield break;
        }

        // Configurar posición inicial (elevada)
        Vector3 startPos = targetPosition.position + Vector3.up * dropHeight;
        Vector3 finalPos = targetPosition.position;
        obj.transform.position = startPos;

        // Opcional: Añadir un pequeño efecto de rotación durante la caída
        Vector3 initialRotation = obj.transform.eulerAngles;

        // Crear la secuencia de animación
        Sequence dropSequence = DOTween.Sequence();

        // Animación principal de caída
        dropSequence.Append(obj.transform.DOMove(finalPos, animationDuration).SetEase(dropEase));

        // Opcional: Pequeña rotación durante la caída para mayor dinamismo
        dropSequence.Join(obj.transform.DORotate(initialRotation + new Vector3(0, 360, 0), animationDuration, RotateMode.FastBeyond360));

        // Opcional: Efecto de escala (pequeño bounce al llegar)
        dropSequence.Join(obj.transform.DOScale(Vector3.one * 1.1f, animationDuration * 0.7f).SetEase(Ease.OutBack)
            .OnComplete(() => obj.transform.DOScale(Vector3.one, animationDuration * 0.3f).SetEase(Ease.InBack)));

        // Log para debugging
        Debug.Log($"Animando objeto {type} desde {startPos} hasta {finalPos}");

        // Esperar a que termine la animación
        yield return dropSequence.WaitForCompletion();

        // Asegurar posición final exacta
        obj.transform.position = finalPos;
        obj.transform.rotation = targetPosition.rotation;
        obj.transform.localScale = Vector3.one;

        Debug.Log($"Objeto {type} colocado correctamente en la posición {objectIndex}");
    }

    // Enable all object images for collected objects
    public void EnableObjectImages()
    {
        foreach (var entry in collectedObjects)
        {
            if (entry.Value && objectImages.ContainsKey(entry.Key) && objectImages[entry.Key] != null)
            {
                objectImages[entry.Key].SetActive(true);
            }
        }
    }

    // Disable all object images
    public void DisableAllObjectImages()
    {
        foreach (var entry in objectImages)
        {
            if (entry.Value != null)
            {
                entry.Value.SetActive(false);
            }
        }
    }

    // Reset the puzzle (for testing or restarting)
    public void ResetPuzzle()
    {
        // Detener cualquier animación en progreso
        DOTween.Kill(this);
        isAnimatingPlacement = false;

        foreach (PuzzleObjectType type in System.Enum.GetValues(typeof(PuzzleObjectType)))
        {
            collectedObjects[type] = false;
            if (placedObjects[type] != null)
            {
                // Detener animaciones del objeto antes de destruirlo
                placedObjects[type].transform.DOKill();
                Destroy(placedObjects[type]);
                placedObjects[type] = null;
            }

            // Disable the object image
            if (objectImages.ContainsKey(type) && objectImages[type] != null)
            {
                objectImages[type].SetActive(false);
            }
        }
        puzzleCompleted = false;
        Debug.Log("Puzzle reseteado");
    }

    // Método para verificar si la animación está en progreso
    public bool IsAnimating()
    {
        return isAnimatingPlacement;
    }

    // Método para configurar la animación desde el inspector o código
    public void SetAnimationSettings(float height, float duration, float delay, Ease easing)
    {
        dropHeight = height;
        animationDuration = duration;
        delayBetweenObjects = delay;
        dropEase = easing;
    }

    // Método para detener todas las animaciones de forma segura
    public void StopAllAnimations()
    {
        DOTween.Kill(this);
        StopAllCoroutines();
        isAnimatingPlacement = false;
    }

    private void OnDestroy()
    {
        // Limpiar las animaciones DOTween al destruir el objeto
        DOTween.Kill(this);
    }
}