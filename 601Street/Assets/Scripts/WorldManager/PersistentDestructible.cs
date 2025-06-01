using UnityEngine;

/// <summary>
/// Script que marca un objeto como destructible de forma persistente.
/// Cuando el objeto es destruido, no volverá a aparecer en futuras cargas de la escena.
/// Integra con el WorldStateManager para mantener el estado entre sesiones.
/// </summary>
public class PersistentDestructible : MonoBehaviour
{
    [Header("Configuración de Identificación")]
    [SerializeField] private string objectID;
    [SerializeField] private bool useSceneName = true;
    [SerializeField] private string customSceneName = "";

    [Header("Configuración de Destrucción")]
    [SerializeField] private bool destroyOnStart = false;
    [SerializeField] private bool saveStateOnDestroy = true;
    [SerializeField] private bool logDestructionEvents = true;

    [Header("Eventos")]
    [SerializeField] private UnityEngine.Events.UnityEvent onObjectDestroyed;
    [SerializeField] private UnityEngine.Events.UnityEvent onObjectPersistentlyRemoved;

    // Variables privadas
    private string sceneName;
    private string stateKey;
    private bool hasBeenInitialized = false;
    private bool isBeingDestroyed = false;

    /// <summary>
    /// Propiedad pública para acceder al ID del objeto
    /// </summary>
    public string ObjectID => objectID;

    /// <summary>
    /// Propiedad para acceder al nombre de la escena
    /// </summary>
    public string SceneName => sceneName;

    private void Awake()
    {
        // Generar ID único si no se ha especificado
        if (string.IsNullOrEmpty(objectID))
        {
            // Intentar obtener un UniqueID si existe
            UniqueID uniqueIDComponent = GetComponent<UniqueID>();
            if (uniqueIDComponent != null)
            {
                objectID = uniqueIDComponent.ID;
                if (logDestructionEvents)
                    Debug.Log($"PersistentDestructible: Usando UniqueID existente: {objectID}");
            }
            else
            {
                // Generar ID basado en el nombre del objeto y su posición
                objectID = $"{gameObject.name}_{transform.position.GetHashCode()}";
                if (logDestructionEvents)
                    Debug.Log($"PersistentDestructible: ID generado automáticamente: {objectID}");
            }
        }

        // Determinar el nombre de la escena
        sceneName = useSceneName ? gameObject.scene.name : customSceneName;

        // Crear la clave de estado
        stateKey = $"Destroyed_{sceneName}_{objectID}";

        if (logDestructionEvents)
            Debug.Log($"PersistentDestructible inicializado: {gameObject.name} con ID {objectID} en escena {sceneName}");
    }

    private void Start()
    {
        // Verificar si este objeto debería estar destruido
        if (WorldStateManager.IsAvailable())
        {
            bool isDestroyed = WorldStateManager.Instance.GetFlag(stateKey, false);

            if (isDestroyed)
            {
                if (logDestructionEvents)
                    Debug.Log($"PersistentDestructible: Objeto {gameObject.name} ya fue destruido anteriormente. Destruyendo...");

                // Invocar evento de remoción persistente
                onObjectPersistentlyRemoved?.Invoke();

                // Destruir inmediatamente
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            Debug.LogWarning($"PersistentDestructible: WorldStateManager no disponible para {gameObject.name}");
        }

        // Si llegamos aquí, el objeto no ha sido destruido anteriormente
        hasBeenInitialized = true;

        // Destruir en Start si está configurado
        if (destroyOnStart)
        {
            DestroyPersistently();
        }
    }

    private void OnDestroy()
    {
        // Solo registrar la destrucción si fue inicializado correctamente
        // y no estamos cerrando la aplicación
        if (hasBeenInitialized && !isBeingDestroyed && WorldStateManager.IsAvailable())
        {
            RegisterDestruction();
        }
    }

    /// <summary>
    /// Destruye el objeto de forma persistente
    /// </summary>
    public void DestroyPersistently()
    {
        if (isBeingDestroyed)
        {
            return; // Evitar destrucción múltiple
        }

        isBeingDestroyed = true;

        if (logDestructionEvents)
            Debug.Log($"PersistentDestructible: Destruyendo persistentemente {gameObject.name} (ID: {objectID})");

        // Invocar evento de destrucción
        onObjectDestroyed?.Invoke();

        // Registrar la destrucción en el WorldStateManager
        RegisterDestruction();

        // Destruir el objeto
        Destroy(gameObject);
    }

    /// <summary>
    /// Registra la destrucción en el WorldStateManager
    /// </summary>
    private void RegisterDestruction()
    {
        if (!WorldStateManager.IsAvailable())
        {
            Debug.LogWarning($"PersistentDestructible: No se puede registrar destrucción - WorldStateManager no disponible");
            return;
        }

        // Marcar como destruido en el WorldStateManager
        WorldStateManager.Instance.SetFlag(stateKey, true);

        if (logDestructionEvents)
            Debug.Log($"PersistentDestructible: Destrucción registrada para {objectID} en escena {sceneName}");

        // Guardar estado si está configurado
        if (saveStateOnDestroy)
        {
            WorldStateManager.Instance.SaveState();
            if (logDestructionEvents)
                Debug.Log($"PersistentDestructible: Estado guardado después de destruir {objectID}");
        }
    }

    /// <summary>
    /// Restaura el objeto (lo marca como no destruido)
    /// Útil para sistemas de respawn o reset
    /// </summary>
    public void RestoreObject()
    {
        if (!WorldStateManager.IsAvailable())
        {
            Debug.LogWarning($"PersistentDestructible: No se puede restaurar objeto - WorldStateManager no disponible");
            return;
        }

        WorldStateManager.Instance.SetFlag(stateKey, false);

        if (logDestructionEvents)
            Debug.Log($"PersistentDestructible: Objeto {objectID} restaurado en escena {sceneName}");

        // Guardar estado si está configurado
        if (saveStateOnDestroy)
        {
            WorldStateManager.Instance.SaveState();
        }
    }

    /// <summary>
    /// Verifica si este objeto ha sido destruido persistentemente
    /// </summary>
    /// <returns>True si el objeto ha sido destruido anteriormente</returns>
    public bool IsDestroyedPersistently()
    {
        if (!WorldStateManager.IsAvailable())
        {
            return false;
        }

        return WorldStateManager.Instance.GetFlag(stateKey, false);
    }

    /// <summary>
    /// Obtiene la clave de estado que usa este objeto
    /// </summary>
    /// <returns>La clave de estado en el WorldStateManager</returns>
    public string GetStateKey()
    {
        return stateKey;
    }

    /// <summary>
    /// Método estático para verificar si un objeto está destruido sin necesidad de instancia
    /// </summary>
    /// <param name="objectID">ID del objeto</param>
    /// <param name="sceneName">Nombre de la escena</param>
    /// <returns>True si el objeto está marcado como destruido</returns>
    public static bool IsObjectDestroyed(string objectID, string sceneName)
    {
        if (!WorldStateManager.IsAvailable())
        {
            return false;
        }

        string stateKey = $"Destroyed_{sceneName}_{objectID}";
        return WorldStateManager.Instance.GetFlag(stateKey, false);
    }

    /// <summary>
    /// Método estático para restaurar un objeto destruido
    /// </summary>
    /// <param name="objectID">ID del objeto</param>
    /// <param name="sceneName">Nombre de la escena</param>
    public static void RestoreDestroyedObject(string objectID, string sceneName)
    {
        if (!WorldStateManager.IsAvailable())
        {
            Debug.LogWarning($"No se puede restaurar objeto {objectID} - WorldStateManager no disponible");
            return;
        }

        string stateKey = $"Destroyed_{sceneName}_{objectID}";
        WorldStateManager.Instance.SetFlag(stateKey, false);

        Debug.Log($"Objeto {objectID} restaurado en escena {sceneName}");
    }

    /// <summary>
    /// Método estático para destruir persistentemente un objeto por ID
    /// </summary>
    /// <param name="objectID">ID del objeto</param>
    /// <param name="sceneName">Nombre de la escena</param>
    public static void DestroyObjectPersistently(string objectID, string sceneName)
    {
        if (!WorldStateManager.IsAvailable())
        {
            Debug.LogWarning($"No se puede destruir objeto {objectID} - WorldStateManager no disponible");
            return;
        }

        string stateKey = $"Destroyed_{sceneName}_{objectID}";
        WorldStateManager.Instance.SetFlag(stateKey, true);

        Debug.Log($"Objeto {objectID} marcado como destruido en escena {sceneName}");
    }

    #region Métodos de Debug y Utilidad

    [ContextMenu("Destroy Persistently")]
    private void DestroyPersistentlyFromContext()
    {
        DestroyPersistently();
    }

    [ContextMenu("Restore Object")]
    private void RestoreObjectFromContext()
    {
        RestoreObject();
    }

    [ContextMenu("Check Destruction Status")]
    private void CheckDestructionStatusFromContext()
    {
        bool isDestroyed = IsDestroyedPersistently();
        Debug.Log($"Estado de destrucción para {gameObject.name} (ID: {objectID}): {isDestroyed}");
    }

    [ContextMenu("Debug Info")]
    private void DebugInfoFromContext()
    {
        Debug.Log($"=== PERSISTENT DESTRUCTIBLE DEBUG ===");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"Object ID: {objectID}");
        Debug.Log($"Scene Name: {sceneName}");
        Debug.Log($"State Key: {stateKey}");
        Debug.Log($"Is Destroyed: {IsDestroyedPersistently()}");
        Debug.Log($"Has Been Initialized: {hasBeenInitialized}");
        Debug.Log($"WorldStateManager Available: {WorldStateManager.IsAvailable()}");
        Debug.Log("=====================================");
    }

    #endregion

    #region Integración con WorldStateActivator

    /// <summary>
    /// Método para ser llamado desde WorldStateActivator o eventos de Unity
    /// </summary>
    public void OnInteract()
    {
        DestroyPersistently();
    }

    /// <summary>
    /// Método alternativo para activación externa
    /// </summary>
    public void TriggerDestruction()
    {
        DestroyPersistently();
    }

    #endregion
}