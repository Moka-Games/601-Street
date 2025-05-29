using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Gestor para mantener un único EventSystem en la escena
/// Debe colocarse en un GameObject persistente al inicio del juego
/// </summary>
public class EventSystemManager : MonoBehaviour
{
    private static EventSystemManager instance;
    private EventSystem managedEventSystem;

    [Header("Configuración")]
    [SerializeField] private bool destroyDuplicates = true;
    [SerializeField] private bool logEventSystemActions = true;

    private void Awake()
    {
        // Implementar singleton
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Asegurar que tenemos un EventSystem
            EnsureEventSystem();

            if (logEventSystemActions)
                Debug.Log("EventSystemManager inicializado como singleton");
        }
        else
        {
            if (logEventSystemActions)
                Debug.Log("EventSystemManager duplicado destruido");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Limpiar EventSystems duplicados al inicio
        if (destroyDuplicates)
        {
            CleanupDuplicateEventSystems();
        }
    }

    /// <summary>
    /// Asegura que existe un EventSystem válido
    /// </summary>
    private void EnsureEventSystem()
    {
        managedEventSystem = GetComponent<EventSystem>();

        if (managedEventSystem == null)
        {
            managedEventSystem = gameObject.AddComponent<EventSystem>();

            // Añadir StandaloneInputModule si no existe
            if (GetComponent<StandaloneInputModule>() == null)
            {
                gameObject.AddComponent<StandaloneInputModule>();
            }

            if (logEventSystemActions)
                Debug.Log("EventSystem creado por EventSystemManager");
        }
    }

    /// <summary>
    /// Elimina EventSystems duplicados de la escena
    /// </summary>
    public void CleanupDuplicateEventSystems()
    {
        EventSystem[] allEventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int duplicatesDestroyed = 0;

        foreach (EventSystem eventSystem in allEventSystems)
        {
            // No destruir nuestro EventSystem gestionado
            if (eventSystem != managedEventSystem)
            {
                if (logEventSystemActions)
                    Debug.Log($"Destruyendo EventSystem duplicado: {eventSystem.name}");

                Destroy(eventSystem.gameObject);
                duplicatesDestroyed++;
            }
        }

        if (logEventSystemActions && duplicatesDestroyed > 0)
            Debug.Log($"Se destruyeron {duplicatesDestroyed} EventSystems duplicados");
    }

    /// <summary>
    /// Método para ser llamado cuando se instancia nuevo contenido UI
    /// </summary>
    public static void OnUIContentInstantiated()
    {
        if (instance != null && instance.destroyDuplicates)
        {
            // Usar Invoke para dar tiempo a que se completen las instanciaciones
            instance.Invoke(nameof(CleanupDuplicateEventSystems), 0.1f);
        }
    }

    /// <summary>
    /// Obtener el EventSystem gestionado
    /// </summary>
    public static EventSystem GetManagedEventSystem()
    {
        return instance?.managedEventSystem;
    }

    /// <summary>
    /// Método de compatibilidad - alias para GetManagedEventSystem
    /// </summary>
    public static EventSystem GetEventSystem()
    {
        return GetManagedEventSystem();
    }

    private void Update()
    {
        // Verificación periódica opcional (desactivable para rendimiento)
        if (destroyDuplicates && Time.frameCount % 300 == 0) // Cada 5 segundos aprox a 60fps
        {
            EventSystem[] allEventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (allEventSystems.Length > 1)
            {
                if (logEventSystemActions)
                    Debug.LogWarning($"Detectados {allEventSystems.Length} EventSystems. Limpiando...");
                CleanupDuplicateEventSystems();
            }
        }
    }
}