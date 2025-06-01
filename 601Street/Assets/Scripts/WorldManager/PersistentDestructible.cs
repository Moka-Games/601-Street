using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Script mejorado que marca un objeto como destructible de forma persistente.
/// Solo registra destrucciones legítimas (NO por cambio de escena).
/// Utiliza múltiples métodos de detección para asegurar precisión.
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

    [Header("Configuración de Detección")]
    [SerializeField] private float sceneChangeGracePeriod = 1.0f; // Tiempo de gracia después de cambio de escena
    [SerializeField] private bool requireExplicitDestruction = false; // Solo registrar si se llama DestroyIntentionally()

    [Header("Eventos")]
    [SerializeField] private UnityEngine.Events.UnityEvent onObjectDestroyed;
    [SerializeField] private UnityEngine.Events.UnityEvent onObjectPersistentlyRemoved;

    // Variables de detección de cambio de escena
    private static bool isGlobalSceneChanging = false;
    private static float lastSceneChangeTime = 0f;
    private static readonly object lockObject = new object();

    // Variables de instancia
    private string sceneName;
    private string stateKey;
    private bool hasBeenInitialized = false;
    private bool isExplicitDestruction = false;
    private bool wasInDontDestroyOnLoad = false;
    private bool hasRegisteredForSceneEvents = false;

    // Detección adicional de contexto
    private bool isApplicationQuitting = false;
    private float initializationTime;

    /// <summary>
    /// Propiedades públicas
    /// </summary>
    public string ObjectID => objectID;
    public string SceneName => sceneName;
    public bool IsExplicitDestructionRequired => requireExplicitDestruction;

    private void Awake()
    {
        initializationTime = Time.time;

        // Generar ID único si no se ha especificado
        GenerateObjectID();

        // Determinar el nombre de la escena
        sceneName = useSceneName ? gameObject.scene.name : customSceneName;

        // Verificar si está en DontDestroyOnLoad
        wasInDontDestroyOnLoad = (gameObject.scene.name == "DontDestroyOnLoad");

        // Crear la clave de estado
        stateKey = $"Destroyed_{sceneName}_{objectID}";

        // Registrarse para eventos de escena (solo una vez por instancia)
        RegisterForSceneEvents();

        if (logDestructionEvents)
            Debug.Log($"PersistentDestructible inicializado: {gameObject.name} con ID {objectID} en escena {sceneName}");
    }

    private void Start()
    {
        // Verificar si este objeto debería estar destruido
        CheckForPreviousDestruction();

        hasBeenInitialized = true;

        // Destruir intencionalmente en Start si está configurado
        if (destroyOnStart)
        {
            DestroyIntentionally();
        }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    private void OnDestroy()
    {
        // Desuscribirse de eventos para evitar memory leaks
        UnregisterFromSceneEvents();

        // Solo registrar destrucción si cumple todos los criterios
        if (ShouldRegisterDestructionStrict())
        {
            RegisterDestruction();
        }
        else if (logDestructionEvents && hasBeenInitialized)
        {
            LogDestructionReason();
        }
    }

    #region Generación de ID y Inicialización

    private void GenerateObjectID()
    {
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
                // Generar ID más robusto
                Vector3 pos = transform.position;
                objectID = $"{gameObject.name}_{pos.x:F2}_{pos.y:F2}_{pos.z:F2}_{GetInstanceID()}";

                if (logDestructionEvents)
                    Debug.Log($"PersistentDestructible: ID generado automáticamente: {objectID}");
            }
        }
    }

    private void CheckForPreviousDestruction()
    {
        if (WorldStateManager.IsAvailable())
        {
            bool isDestroyed = WorldStateManager.Instance.GetFlag(stateKey, false);

            if (isDestroyed)
            {
                if (logDestructionEvents)
                    Debug.Log($"PersistentDestructible: Objeto {gameObject.name} ya fue destruido anteriormente. Destruyendo...");

                // Invocar evento de remoción persistente
                onObjectPersistentlyRemoved?.Invoke();

                // Destruir inmediatamente (esto NO se registrará como destrucción persistente)
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.LogWarning($"PersistentDestructible: WorldStateManager no disponible para {gameObject.name}");
        }
    }

    #endregion

    #region Gestión de Eventos de Escena

    private void RegisterForSceneEvents()
    {
        if (!hasRegisteredForSceneEvents)
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            hasRegisteredForSceneEvents = true;
        }
    }

    private void UnregisterFromSceneEvents()
    {
        if (hasRegisteredForSceneEvents)
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            hasRegisteredForSceneEvents = false;
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        lock (lockObject)
        {
            isGlobalSceneChanging = true;
            lastSceneChangeTime = Time.time;
        }

        if (logDestructionEvents && (scene.name == sceneName || scene.name == gameObject.scene.name))
        {
            Debug.Log($"PersistentDestructible: Escena {scene.name} descargándose - marcando período de gracia");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // SOLUCIÓN: No usar StartCoroutine directamente, usar un método estático seguro
        // que funcione con objetos inactivos
        SafeStartCoroutine(this, ResetSceneChangingFlagDelayed());
    }

    // Método estático seguro para iniciar corrutinas incluso en objetos inactivos
    private static void SafeStartCoroutine(PersistentDestructible script, IEnumerator routine)
    {
        // Verificar si el script existe y si su GameObject está activo
        if (script != null && script.gameObject != null)
        {
            // Si el GameObject está activo, usar StartCoroutine normal
            if (script.gameObject.activeInHierarchy)
            {
                script.StartCoroutine(routine);
            }
            else
            {
                // Si el GameObject está inactivo, usar un objeto global para la corrutina
                // o simplemente resetear directamente después del período de gracia
                if (WorldStateManager.Instance != null)
                {
                    WorldStateManager.Instance.StartCoroutine(routine);
                }
                else
                {
                    // Como respaldo, usar un método directo sin corrutinas
                    DelayedResetSceneChangingFlag(script.sceneChangeGracePeriod);
                }
            }
        }
    }

    // Método alternativo para resetear la bandera sin usar corrutinas
    private static void DelayedResetSceneChangingFlag(float delay)
    {
        // Programar una operación que se ejecutará en el próximo frame
        // después del período de gracia
        lock (lockObject)
        {
            lastSceneChangeTime = Time.time;
        }
    }

    private IEnumerator ResetSceneChangingFlagDelayed()
    {
        yield return new WaitForSeconds(sceneChangeGracePeriod);

        lock (lockObject)
        {
            // Solo resetear si ha pasado suficiente tiempo
            if (Time.time - lastSceneChangeTime >= sceneChangeGracePeriod)
            {
                isGlobalSceneChanging = false;
                if (logDestructionEvents)
                    Debug.Log("PersistentDestructible: Período de gracia de cambio de escena finalizado");
            }
        }
    }

    #endregion

    #region Detección de Destrucción Legítima

    /// <summary>
    /// Método estricto para determinar si la destrucción debe registrarse
    /// </summary>
    private bool ShouldRegisterDestructionStrict()
    {
        // No registrar si no está inicializado o la aplicación se está cerrando
        if (!hasBeenInitialized || isApplicationQuitting)
        {
            return false;
        }

        // No registrar si WorldStateManager no está disponible
        if (!WorldStateManager.IsAvailable())
        {
            return false;
        }

        // Si se requiere destrucción explícita, solo registrar si fue marcada explícitamente
        if (requireExplicitDestruction && !isExplicitDestruction)
        {
            return false;
        }

        // Si fue marcada como destrucción explícita, siempre registrar
        if (isExplicitDestruction)
        {
            return true;
        }

        // No registrar si estamos en período de gracia por cambio de escena
        lock (lockObject)
        {
            if (isGlobalSceneChanging)
            {
                return false;
            }

            // Verificar si ha pasado muy poco tiempo desde el último cambio de escena
            if (Time.time - lastSceneChangeTime < sceneChangeGracePeriod)
            {
                return false;
            }
        }

        // Verificaciones adicionales de contexto
        if (!IsDestructionContextValid())
        {
            return false;
        }

        // Si llegamos aquí y no estamos en modo explícito, es probablemente legítimo
        return true;
    }

    /// <summary>
    /// Verificaciones adicionales del contexto de destrucción
    /// </summary>
    private bool IsDestructionContextValid()
    {
        // Verificar que el objeto ha existido por un tiempo mínimo
        float minimumLifetime = 0.1f;
        if (Time.time - initializationTime < minimumLifetime)
        {
            return false;
        }

        // Verificar que la escena actual aún existe
        bool currentSceneExists = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == sceneName && scene.isLoaded)
            {
                currentSceneExists = true;
                break;
            }
        }

        if (!currentSceneExists)
        {
            return false;
        }

        // Si el objeto estaba en DontDestroyOnLoad, es más probable que sea intencional
        if (wasInDontDestroyOnLoad)
        {
            return true;
        }

        // Verificación final: Si no hay indicadores de cambio de escena, asumir legítimo
        return true;
    }

    /// <summary>
    /// Log detallado de por qué no se registró la destrucción
    /// </summary>
    private void LogDestructionReason()
    {
        if (!logDestructionEvents) return;

        string reason = "Destrucción NO registrada: ";

        if (!hasBeenInitialized)
            reason += "No inicializado. ";
        else if (isApplicationQuitting)
            reason += "Aplicación cerrándose. ";
        else if (!WorldStateManager.IsAvailable())
            reason += "WorldStateManager no disponible. ";
        else if (requireExplicitDestruction && !isExplicitDestruction)
            reason += "Destrucción explícita requerida pero no marcada. ";
        else if (isGlobalSceneChanging)
            reason += "Cambio de escena global detectado. ";
        else if (Time.time - lastSceneChangeTime < sceneChangeGracePeriod)
            reason += $"Dentro del período de gracia ({sceneChangeGracePeriod}s). ";
        else if (Time.time - initializationTime < 0.1f)
            reason += "Destruido demasiado pronto después de la inicialización. ";
        else
            reason += "Contexto de destrucción inválido. ";

        Debug.Log($"PersistentDestructible [{gameObject.name}]: {reason}");
    }

    #endregion

    #region Métodos de Destrucción

    /// <summary>
    /// Destruye el objeto de forma explícita y persistente
    /// </summary>
    public void DestroyIntentionally()
    {
        if (logDestructionEvents)
            Debug.Log($"PersistentDestructible: Destrucción EXPLÍCITA solicitada para {gameObject.name} (ID: {objectID})");

        // Marcar como destrucción explícita
        isExplicitDestruction = true;

        // Invocar evento de destrucción
        onObjectDestroyed?.Invoke();

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
            Debug.Log($"PersistentDestructible: ✓ DESTRUCCIÓN PERSISTENTE REGISTRADA para {objectID} en escena {sceneName}");

        // Guardar estado si está configurado
        if (saveStateOnDestroy)
        {
            WorldStateManager.Instance.SaveState();
            if (logDestructionEvents)
                Debug.Log($"PersistentDestructible: Estado guardado después de destruir {objectID}");
        }
    }

    #endregion

    #region Métodos de Restauración y Estado

    /// <summary>
    /// Restaura el objeto (lo marca como no destruido)
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

        if (saveStateOnDestroy)
        {
            WorldStateManager.Instance.SaveState();
        }
    }

    /// <summary>
    /// Verifica si este objeto ha sido destruido persistentemente
    /// </summary>
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
    public string GetStateKey()
    {
        return stateKey;
    }

    #endregion

    #region Métodos Estáticos de Utilidad

    /// <summary>
    /// Método estático para verificar si un objeto está destruido
    /// </summary>
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

    /// <summary>
    /// Obtiene información sobre el estado global de cambio de escena
    /// </summary>
    public static bool IsSceneChanging()
    {
        lock (lockObject)
        {
            return isGlobalSceneChanging || (Time.time - lastSceneChangeTime < 1.0f);
        }
    }

    #endregion

    #region Integración con Sistemas de Interacción

    /// <summary>
    /// Método para ser llamado desde WorldStateActivator o eventos de Unity
    /// </summary>
    public void OnInteract()
    {
        DestroyIntentionally();
    }

    /// <summary>
    /// Método alternativo para activación externa
    /// </summary>
    public void TriggerDestruction()
    {
        DestroyIntentionally();
    }

    /// <summary>
    /// Método específico para usar en sistemas de recolección/interacción
    /// </summary>
    public void OnCollected()
    {
        DestroyIntentionally();
    }

    /// <summary>
    /// Método específico para cuando un objeto es "usado" o "consumido"
    /// </summary>
    public void OnConsumed()
    {
        DestroyIntentionally();
    }

    /// <summary>
    /// Método de compatibilidad con la versión anterior
    /// </summary>
    public void DestroyPersistently()
    {
        DestroyIntentionally();
    }

    #endregion

    #region Métodos de Debug y Utilidad

    [ContextMenu("Destroy Intentionally")]
    private void DestroyIntentionallyFromContext()
    {
        DestroyIntentionally();
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
        Debug.Log($"Is Explicit Destruction: {isExplicitDestruction}");
        Debug.Log($"Require Explicit Destruction: {requireExplicitDestruction}");
        Debug.Log($"Was In DontDestroyOnLoad: {wasInDontDestroyOnLoad}");
        Debug.Log($"Is Global Scene Changing: {isGlobalSceneChanging}");
        Debug.Log($"Last Scene Change Time: {lastSceneChangeTime}");
        Debug.Log($"Time Since Scene Change: {Time.time - lastSceneChangeTime}");
        Debug.Log($"Grace Period: {sceneChangeGracePeriod}");
        Debug.Log($"WorldStateManager Available: {WorldStateManager.IsAvailable()}");
        Debug.Log($"Application Quitting: {isApplicationQuitting}");
        Debug.Log("=====================================");
    }

    [ContextMenu("Force Enable Explicit Mode")]
    private void ForceEnableExplicitMode()
    {
        requireExplicitDestruction = true;
        Debug.Log($"Modo explícito habilitado para {gameObject.name} - solo se registrarán destrucciones explícitas");
    }

    [ContextMenu("Test: Normal Destroy")]
    private void TestNormalDestroy()
    {
        Debug.Log("Probando Destroy() normal - detección automática de legitimidad");
        Destroy(gameObject);
    }

    [ContextMenu("Test: Simulate Scene Change")]
    private void SimulateSceneChange()
    {
        lock (lockObject)
        {
            isGlobalSceneChanging = true;
            lastSceneChangeTime = Time.time;
        }

        Debug.Log("Simulando cambio de escena - próxima destrucción no será persistente");

        // Uso del método seguro para iniciar la corrutina
        SafeStartCoroutine(this, ResetSceneChangingFlagDelayed());
    }

    #endregion
}