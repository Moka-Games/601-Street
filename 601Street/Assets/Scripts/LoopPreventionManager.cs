using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Sistema global para prevenir bucles infinitos en navegación y ejecución de botones
/// Monitorea y bloquea operaciones repetitivas que puedan causar stack overflow
/// </summary>
public class LoopPreventionManager : MonoBehaviour
{
    public static LoopPreventionManager Instance { get; private set; }

    [Header("Configuración de Prevención")]
    [SerializeField] private bool enableLoopDetection = true;
    [SerializeField] private float globalOperationCooldown = 0.3f;
    [SerializeField] private int maxOperationsPerFrame = 3;
    [SerializeField] private bool logDebugInfo = true;

    [Header("Configuración de Timeouts")]
    [SerializeField] private float buttonExecutionTimeout = 2f;
    [SerializeField] private float closeOperationTimeout = 3f;
    [SerializeField] private float navigationOperationTimeout = 1f;

    // Estados globales de operaciones
    private static bool isButtonOperationInProgress = false;
    private static bool isCloseOperationInProgress = false;
    private static bool isNavigationOperationInProgress = false;

    // Contadores de operaciones por frame
    private Dictionary<string, int> operationsThisFrame = new Dictionary<string, int>();
    private int currentFrameCount = 0;

    // Timeouts
    private Dictionary<string, float> operationStartTimes = new Dictionary<string, float>();

    // Listas de bloqueo temporal
    private HashSet<string> temporarilyBlockedOperations = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLoopPrevention();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeLoopPrevention()
    {
        if (logDebugInfo)
            Debug.Log("LoopPreventionManager inicializado - Protección contra bucles infinitos activada");

        // Resetear todos los estados
        ResetAllOperationStates();

        // Iniciar monitoreo de frames
        StartCoroutine(FrameCounterCoroutine());
    }

    private void Update()
    {
        if (enableLoopDetection)
        {
            CheckForTimeouts();
            MonitorOperationCounts();
        }
    }

    #region Button Operation Protection

    /// <summary>
    /// Verifica si una operación de botón puede ejecutarse de forma segura
    /// </summary>
    public static bool CanExecuteButtonOperation(string buttonName = "")
    {
        if (Instance == null) return true;

        string operationKey = $"button_{buttonName}";

        // Verificar estado global
        if (isButtonOperationInProgress)
        {
            if (Instance.logDebugInfo)
                Debug.Log($"Button operation blocked - Global operation in progress");
            return false;
        }

        // Verificar timeout
        if (Instance.IsOperationTimedOut(operationKey, Instance.buttonExecutionTimeout))
        {
            if (Instance.logDebugInfo)
                Debug.Log($"Button operation timed out, resetting state");
            Instance.ResetOperationState(operationKey);
            return true;
        }

        // Verificar bloqueo temporal
        if (Instance.temporarilyBlockedOperations.Contains(operationKey))
        {
            if (Instance.logDebugInfo)
                Debug.Log($"Button operation temporarily blocked: {buttonName}");
            return false;
        }

        // Verificar límite de operaciones por frame
        if (Instance.IsOperationLimitExceeded(operationKey))
        {
            if (Instance.logDebugInfo)
                Debug.Log($"Button operation limit exceeded for frame: {buttonName}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Marca el inicio de una operación de botón
    /// </summary>
    public static void StartButtonOperation(string buttonName = "")
    {
        if (Instance == null) return;

        string operationKey = $"button_{buttonName}";

        isButtonOperationInProgress = true;
        Instance.RecordOperationStart(operationKey);

        if (Instance.logDebugInfo)
            Debug.Log($"Button operation started: {buttonName}");
    }

    /// <summary>
    /// Marca el fin de una operación de botón
    /// </summary>
    public static void EndButtonOperation(string buttonName = "")
    {
        if (Instance == null) return;

        string operationKey = $"button_{buttonName}";

        isButtonOperationInProgress = false;
        Instance.RecordOperationEnd(operationKey);

        if (Instance.logDebugInfo)
            Debug.Log($"Button operation ended: {buttonName}");

        // Aplicar cooldown temporal
        Instance.StartCoroutine(Instance.TemporaryBlockOperation(operationKey, Instance.globalOperationCooldown));
    }

    #endregion

    #region Close Operation Protection

    /// <summary>
    /// Verifica si una operación de cierre puede ejecutarse de forma segura
    /// </summary>
    public static bool CanExecuteCloseOperation(string objectName = "")
    {
        if (Instance == null) return true;

        string operationKey = $"close_{objectName}";

        // Verificar estado global
        if (isCloseOperationInProgress)
        {
            if (Instance.logDebugInfo)
                Debug.Log($"Close operation blocked - Global operation in progress");
            return false;
        }

        // Verificar timeout
        if (Instance.IsOperationTimedOut(operationKey, Instance.closeOperationTimeout))
        {
            if (Instance.logDebugInfo)
                Debug.Log($"Close operation timed out, resetting state");
            Instance.ResetOperationState(operationKey);
            return true;
        }

        // Verificar bloqueo temporal
        if (Instance.temporarilyBlockedOperations.Contains(operationKey))
        {
            if (Instance.logDebugInfo)
                Debug.Log($"Close operation temporarily blocked: {objectName}");
            return false;
        }

        // Verificar límite de operaciones por frame
        if (Instance.IsOperationLimitExceeded(operationKey))
        {
            if (Instance.logDebugInfo)
                Debug.Log($"Close operation limit exceeded for frame: {objectName}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Marca el inicio de una operación de cierre
    /// </summary>
    public static void StartCloseOperation(string objectName = "")
    {
        if (Instance == null) return;

        string operationKey = $"close_{objectName}";

        isCloseOperationInProgress = true;
        Instance.RecordOperationStart(operationKey);

        if (Instance.logDebugInfo)
            Debug.Log($"Close operation started: {objectName}");
    }

    /// <summary>
    /// Marca el fin de una operación de cierre
    /// </summary>
    public static void EndCloseOperation(string objectName = "")
    {
        if (Instance == null) return;

        string operationKey = $"close_{objectName}";

        isCloseOperationInProgress = false;
        Instance.RecordOperationEnd(operationKey);

        if (Instance.logDebugInfo)
            Debug.Log($"Close operation ended: {objectName}");

        // Aplicar cooldown temporal
        Instance.StartCoroutine(Instance.TemporaryBlockOperation(operationKey, Instance.globalOperationCooldown));
    }

    #endregion

    #region Navigation Operation Protection

    /// <summary>
    /// Verifica si una operación de navegación puede ejecutarse de forma segura
    /// </summary>
    public static bool CanExecuteNavigationOperation(string navigationType = "")
    {
        if (Instance == null) return true;

        string operationKey = $"nav_{navigationType}";

        // Verificar estado global
        if (isNavigationOperationInProgress)
        {
            if (Instance.logDebugInfo)
                Debug.Log($"Navigation operation blocked - Global operation in progress");
            return false;
        }

        // Verificar timeout
        if (Instance.IsOperationTimedOut(operationKey, Instance.navigationOperationTimeout))
        {
            if (Instance.logDebugInfo)
                Debug.Log($"Navigation operation timed out, resetting state");
            Instance.ResetOperationState(operationKey);
            return true;
        }

        // Verificar bloqueo temporal
        if (Instance.temporarilyBlockedOperations.Contains(operationKey))
        {
            if (Instance.logDebugInfo)
                Debug.Log($"Navigation operation temporarily blocked: {navigationType}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Marca el inicio de una operación de navegación
    /// </summary>
    public static void StartNavigationOperation(string navigationType = "")
    {
        if (Instance == null) return;

        string operationKey = $"nav_{navigationType}";

        isNavigationOperationInProgress = true;
        Instance.RecordOperationStart(operationKey);

        if (Instance.logDebugInfo)
            Debug.Log($"Navigation operation started: {navigationType}");
    }

    /// <summary>
    /// Marca el fin de una operación de navegación
    /// </summary>
    public static void EndNavigationOperation(string navigationType = "")
    {
        if (Instance == null) return;

        string operationKey = $"nav_{navigationType}";

        isNavigationOperationInProgress = false;
        Instance.RecordOperationEnd(operationKey);

        if (Instance.logDebugInfo)
            Debug.Log($"Navigation operation ended: {navigationType}");

        // Aplicar cooldown temporal más corto para navegación
        Instance.StartCoroutine(Instance.TemporaryBlockOperation(operationKey, Instance.globalOperationCooldown * 0.5f));
    }

    #endregion

    #region Internal Operation Management

    private void RecordOperationStart(string operationKey)
    {
        operationStartTimes[operationKey] = Time.unscaledTime;

        // Incrementar contador de operaciones para este frame
        if (!operationsThisFrame.ContainsKey(operationKey))
        {
            operationsThisFrame[operationKey] = 0;
        }
        operationsThisFrame[operationKey]++;
    }

    private void RecordOperationEnd(string operationKey)
    {
        if (operationStartTimes.ContainsKey(operationKey))
        {
            operationStartTimes.Remove(operationKey);
        }
    }

    private bool IsOperationTimedOut(string operationKey, float timeout)
    {
        if (operationStartTimes.ContainsKey(operationKey))
        {
            float elapsedTime = Time.unscaledTime - operationStartTimes[operationKey];
            return elapsedTime > timeout;
        }
        return false;
    }

    private bool IsOperationLimitExceeded(string operationKey)
    {
        if (operationsThisFrame.ContainsKey(operationKey))
        {
            return operationsThisFrame[operationKey] >= maxOperationsPerFrame;
        }
        return false;
    }

    private void ResetOperationState(string operationKey)
    {
        if (operationStartTimes.ContainsKey(operationKey))
        {
            operationStartTimes.Remove(operationKey);
        }

        if (temporarilyBlockedOperations.Contains(operationKey))
        {
            temporarilyBlockedOperations.Remove(operationKey);
        }

        // Resetear estado global si es necesario
        if (operationKey.StartsWith("button_"))
        {
            isButtonOperationInProgress = false;
        }
        else if (operationKey.StartsWith("close_"))
        {
            isCloseOperationInProgress = false;
        }
        else if (operationKey.StartsWith("nav_"))
        {
            isNavigationOperationInProgress = false;
        }
    }

    private IEnumerator TemporaryBlockOperation(string operationKey, float duration)
    {
        temporarilyBlockedOperations.Add(operationKey);
        yield return new WaitForSecondsRealtime(duration);
        temporarilyBlockedOperations.Remove(operationKey);
    }

    private IEnumerator FrameCounterCoroutine()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();

            int newFrameCount = Time.frameCount;
            if (newFrameCount != currentFrameCount)
            {
                // Nuevo frame - resetear contadores
                operationsThisFrame.Clear();
                currentFrameCount = newFrameCount;
            }
        }
    }

    private void CheckForTimeouts()
    {
        var keysToRemove = new List<string>();

        foreach (var kvp in operationStartTimes)
        {
            string operationKey = kvp.Key;
            float startTime = kvp.Value;
            float elapsedTime = Time.unscaledTime - startTime;

            float timeout = GetTimeoutForOperation(operationKey);

            if (elapsedTime > timeout)
            {
                Debug.LogWarning($"Operation timed out: {operationKey} (elapsed: {elapsedTime:F2}s, timeout: {timeout:F2}s)");
                keysToRemove.Add(operationKey);
            }
        }

        // Limpiar operaciones que han expirado
        foreach (string key in keysToRemove)
        {
            ResetOperationState(key);
        }
    }

    private float GetTimeoutForOperation(string operationKey)
    {
        if (operationKey.StartsWith("button_"))
        {
            return buttonExecutionTimeout;
        }
        else if (operationKey.StartsWith("close_"))
        {
            return closeOperationTimeout;
        }
        else if (operationKey.StartsWith("nav_"))
        {
            return navigationOperationTimeout;
        }
        return globalOperationCooldown;
    }

    private void MonitorOperationCounts()
    {
        foreach (var kvp in operationsThisFrame)
        {
            if (kvp.Value >= maxOperationsPerFrame)
            {
                Debug.LogWarning($"Operation limit exceeded this frame: {kvp.Key} (count: {kvp.Value}, limit: {maxOperationsPerFrame})");
            }
        }
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Resetea todos los estados de operaciones - usar en casos de emergencia
    /// </summary>
    public static void ResetAllOperationStates()
    {
        isButtonOperationInProgress = false;
        isCloseOperationInProgress = false;
        isNavigationOperationInProgress = false;

        if (Instance != null)
        {
            Instance.operationStartTimes.Clear();
            Instance.temporarilyBlockedOperations.Clear();
            Instance.operationsThisFrame.Clear();
        }

        Debug.Log("Todos los estados de operaciones reseteados");
    }

    /// <summary>
    /// Obtiene información del estado actual del sistema
    /// </summary>
    public static string GetSystemStatus()
    {
        if (Instance == null) return "LoopPreventionManager no disponible";

        return $"Button: {isButtonOperationInProgress}, " +
               $"Close: {isCloseOperationInProgress}, " +
               $"Navigation: {isNavigationOperationInProgress}, " +
               $"Active Operations: {Instance.operationStartTimes.Count}, " +
               $"Blocked Operations: {Instance.temporarilyBlockedOperations.Count}";
    }

    /// <summary>
    /// Verifica si hay alguna operación crítica en progreso
    /// </summary>
    public static bool IsAnyOperationInProgress()
    {
        return isButtonOperationInProgress || isCloseOperationInProgress || isNavigationOperationInProgress;
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug System Status")]
    public void DebugSystemStatus()
    {
        Debug.Log($"=== LOOP PREVENTION MANAGER STATUS ===");
        Debug.Log($"Button Operation In Progress: {isButtonOperationInProgress}");
        Debug.Log($"Close Operation In Progress: {isCloseOperationInProgress}");
        Debug.Log($"Navigation Operation In Progress: {isNavigationOperationInProgress}");
        Debug.Log($"Active Operations: {operationStartTimes.Count}");
        Debug.Log($"Blocked Operations: {temporarilyBlockedOperations.Count}");
        Debug.Log($"Operations This Frame: {operationsThisFrame.Count}");
        Debug.Log($"Current Frame: {currentFrameCount}");

        if (operationStartTimes.Count > 0)
        {
            Debug.Log("--- ACTIVE OPERATIONS ---");
            foreach (var kvp in operationStartTimes)
            {
                float elapsed = Time.unscaledTime - kvp.Value;
                Debug.Log($"  {kvp.Key}: {elapsed:F2}s");
            }
        }

        if (temporarilyBlockedOperations.Count > 0)
        {
            Debug.Log("--- BLOCKED OPERATIONS ---");
            foreach (string operation in temporarilyBlockedOperations)
            {
                Debug.Log($"  {operation}");
            }
        }

        Debug.Log("======================================");
    }

    [ContextMenu("Force Reset All States")]
    public void ForceResetAllStates()
    {
        ResetAllOperationStates();
        Debug.Log("Estados forzados a resetear");
    }

    [ContextMenu("Test Button Operation Protection")]
    public void TestButtonOperationProtection()
    {
        string testButton = "TestButton";
        Debug.Log($"Can execute button operation: {CanExecuteButtonOperation(testButton)}");

        StartButtonOperation(testButton);
        Debug.Log($"After start - Can execute: {CanExecuteButtonOperation(testButton)}");

        EndButtonOperation(testButton);
        Debug.Log($"After end - Can execute: {CanExecuteButtonOperation(testButton)}");
    }

    #endregion

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            ResetAllOperationStates();
        }
    }
}