using UnityEngine;
using TMPro;

/// <summary>
/// Script de integración que coordina el sistema de dados y bonuses
/// Asegura que todos los componentes trabajen juntos correctamente
/// </summary>
public class DiceBonusIntegration : MonoBehaviour
{
    [Header("Referencias Principales")]
    [SerializeField] private Dice_Manager diceManager;
    [SerializeField] private BonusManager bonusManager;
    [SerializeField] private BonusFeedbackManager feedbackManager;

    [Header("Referencias de UI - Configurar en Inspector")]
    [SerializeField] private TMP_Text diceResultText;
    [SerializeField] private TMP_Text bonusDisplayText; // El componente "Bonus" de la captura

    [Header("Configuración del Sistema")]
    [SerializeField] private bool autoFindReferences = true;
    [SerializeField] private bool enableDebugLogs = true;

    private void Start()
    {
        InitializeIntegration();
    }

    private void InitializeIntegration()
    {
        Debug.Log("=== INICIALIZANDO INTEGRACIÓN DICE-BONUS ===");

        // Buscar referencias automáticamente si está habilitado
        if (autoFindReferences)
        {
            FindMissingReferences();
        }

        // Validar que todas las referencias estén correctas
        if (!ValidateReferences())
        {
            Debug.LogError("DiceBonusIntegration: Referencias incompletas. El sistema no funcionará correctamente.");
            return;
        }

        // Configurar el FeedbackManager con las referencias de UI
        ConfigureFeedbackManager();

        // Configurar callbacks del sistema de dados
        SetupDiceManagerCallbacks();

        Debug.Log("Integración Dice-Bonus inicializada correctamente");
        Debug.Log("=============================================");
    }

    private void FindMissingReferences()
    {
        Debug.Log("Buscando referencias faltantes...");

        if (diceManager == null)
        {
            diceManager = FindAnyObjectByType<Dice_Manager>();
            Debug.Log($"Dice_Manager encontrado: {diceManager != null}");
        }

        if (bonusManager == null)
        {
            bonusManager = BonusManager.Instance;
            if (bonusManager == null)
            {
                bonusManager = FindAnyObjectByType<BonusManager>();
            }
            Debug.Log($"BonusManager encontrado: {bonusManager != null}");
        }

        if (feedbackManager == null)
        {
            feedbackManager = FindAnyObjectByType<BonusFeedbackManager>();
            Debug.Log($"BonusFeedbackManager encontrado: {feedbackManager != null}");
        }

        // Buscar textos de UI si no están asignados
        if (diceResultText == null)
        {
            diceResultText = GameObject.Find("Dice_Result")?.GetComponent<TMP_Text>();
            Debug.Log($"Dice_Result text encontrado: {diceResultText != null}");
        }

        if (bonusDisplayText == null)
        {
            bonusDisplayText = GameObject.Find("Bonus")?.GetComponent<TMP_Text>();
            if (bonusDisplayText == null)
            {
                bonusDisplayText = GameObject.Find("Bonus_Txt")?.GetComponent<TMP_Text>();
            }
            Debug.Log($"Bonus display text encontrado: {bonusDisplayText != null}");
        }
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (diceManager == null)
        {
            Debug.LogError("Dice_Manager no está asignado o no se encontró");
            isValid = false;
        }

        if (bonusManager == null)
        {
            Debug.LogError("BonusManager no está asignado o no se encontró");
            isValid = false;
        }

        if (feedbackManager == null)
        {
            Debug.LogError("BonusFeedbackManager no está asignado o no se encontró");
            isValid = false;
        }

        if (diceResultText == null)
        {
            Debug.LogError("diceResultText no está asignado. Necesario para mostrar resultados.");
            isValid = false;
        }

        if (bonusDisplayText == null)
        {
            Debug.LogError("bonusDisplayText no está asignado. Necesario para mostrar feedback de bonus.");
            isValid = false;
        }

        return isValid;
    }

    private void ConfigureFeedbackManager()
    {
        if (feedbackManager != null && diceResultText != null && bonusDisplayText != null)
        {
            feedbackManager.SetReferences(bonusDisplayText, diceResultText);
            Debug.Log("FeedbackManager configurado con referencias de UI");
        }
    }

    private void SetupDiceManagerCallbacks()
    {
        if (diceManager == null) return;

        // El Dice_Manager ya maneja la lógica principal, pero podemos agregar callbacks adicionales si es necesario
        Debug.Log("Callbacks del Dice_Manager configurados");
    }

    #region Métodos Públicos de Utilidad

    /// <summary>
    /// Fuerza la actualización de todas las referencias
    /// </summary>
    [ContextMenu("Refresh All References")]
    public void RefreshAllReferences()
    {
        FindMissingReferences();
        ConfigureFeedbackManager();
        Debug.Log("Referencias actualizadas manualmente");
    }

    /// <summary>
    /// Verifica el estado de integración del sistema
    /// </summary>
    [ContextMenu("Check Integration Status")]
    public void CheckIntegrationStatus()
    {
        Debug.Log("=== ESTADO DE INTEGRACIÓN ===");
        Debug.Log($"Dice_Manager: {(diceManager != null ? "✅ OK" : "❌ FALTA")}");
        Debug.Log($"BonusManager: {(bonusManager != null ? "✅ OK" : "❌ FALTA")}");
        Debug.Log($"FeedbackManager: {(feedbackManager != null ? "✅ OK" : "❌ FALTA")}");
        Debug.Log($"Dice Result Text: {(diceResultText != null ? "✅ OK" : "❌ FALTA")}");
        Debug.Log($"Bonus Display Text: {(bonusDisplayText != null ? "✅ OK" : "❌ FALTA")}");

        if (bonusManager != null)
        {
            Debug.Log($"Bonuses recolectados: {bonusManager.GetCollectedBonusCount()}");
            Debug.Log($"Bonus activo: {(bonusManager.HasActiveBBonus() ? bonusManager.GetActiveBonusName() : "Ninguno")}");
        }

        if (diceManager != null)
        {
            Debug.Log($"Dice Manager en tirada: {diceManager.IsRolling()}");
        }

        Debug.Log("============================");
    }

    /// <summary>
    /// Prueba completa del sistema de feedback
    /// </summary>
    [ContextMenu("Test Complete System")]
    public void TestCompleteSystem()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("El test solo funciona en Play Mode");
            return;
        }

        if (!ValidateReferences())
        {
            Debug.LogError("No se puede hacer el test: referencias incompletas");
            return;
        }

        Debug.Log("=== INICIANDO TEST COMPLETO DEL SISTEMA ===");

        // Paso 1: Agregar un bonus de prueba
        if (bonusManager != null)
        {
            bonusManager.AddBonus("Bonus de Prueba", 3, "Bonus para testing del sistema");
        }

        // Paso 2: El jugador puede activar el bonus manualmente desde la UI
        // Paso 3: El jugador puede hacer una tirada manualmente

        Debug.Log("Test iniciado. Usa la interfaz para:");
        Debug.Log("1. Activar el bonus de prueba");
        Debug.Log("2. Hacer una tirada de dado");
        Debug.Log("3. Observar el feedback visual");
    }

    /// <summary>
    /// Resetea completamente el sistema
    /// </summary>
    [ContextMenu("Reset Complete System")]
    public void ResetCompleteSystem()
    {
        if (!Application.isPlaying) return;

        Debug.Log("Reseteando sistema completo...");

        if (diceManager != null)
        {
            diceManager.ResetUI();
        }

        if (feedbackManager != null)
        {
            feedbackManager.ResetFeedback();
        }

        if (bonusManager != null)
        {
            bonusManager.DeactivateCurrentBonus();
        }

        Debug.Log("Sistema reseteado completamente");
    }

    #endregion

    #region Métodos de Debug y Configuración

    /// <summary>
    /// Configura manualmente las referencias (útil para configuración dinámica)
    /// </summary>
    public void SetReferences(Dice_Manager dice, BonusManager bonus, BonusFeedbackManager feedback,
                            TMP_Text diceResult, TMP_Text bonusDisplay)
    {
        diceManager = dice;
        bonusManager = bonus;
        feedbackManager = feedback;
        diceResultText = diceResult;
        bonusDisplayText = bonusDisplay;

        // Reconfigurar después de asignar referencias
        ConfigureFeedbackManager();

        Debug.Log("Referencias configuradas manualmente");
    }

    /// <summary>
    /// Habilita o deshabilita los logs de debug
    /// </summary>
    public void SetDebugLogging(bool enabled)
    {
        enableDebugLogs = enabled;
        Debug.Log($"Debug logging {(enabled ? "habilitado" : "deshabilitado")}");
    }

    /// <summary>
    /// Obtiene información del estado actual del sistema
    /// </summary>
    public SystemStatus GetSystemStatus()
    {
        return new SystemStatus
        {
            diceManagerReady = diceManager != null,
            bonusManagerReady = bonusManager != null,
            feedbackManagerReady = feedbackManager != null,
            uiReferencesReady = diceResultText != null && bonusDisplayText != null,
            hasActiveBonus = bonusManager != null && bonusManager.HasActiveBBonus(),
            activeBonusValue = bonusManager != null ? bonusManager.GetActiveBonusValue() : 0,
            isRolling = diceManager != null && diceManager.IsRolling(),
            collectedBonusCount = bonusManager != null ? bonusManager.GetCollectedBonusCount() : 0
        };
    }

    #endregion

    #region Eventos y Callbacks

    /// <summary>
    /// Evento que se dispara cuando se completa una tirada con bonus
    /// </summary>
    public System.Action<int, int, int> OnBonusRollCompleted; // baseResult, bonusValue, finalResult

    /// <summary>
    /// Evento que se dispara cuando se activa un bonus
    /// </summary>
    public System.Action<string, int> OnBonusActivated; // bonusName, bonusValue

    /// <summary>
    /// Evento que se dispara cuando se consume un bonus
    /// </summary>
    public System.Action<string, int> OnBonusConsumed; // bonusName, bonusValue

    /// <summary>
    /// Método llamado cuando el sistema completa una tirada (puede ser usado por otros sistemas)
    /// </summary>
    public void NotifyRollCompleted(int baseResult, int bonusValue, int finalResult)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"Tirada completada: {baseResult} + {bonusValue} = {finalResult}");
        }

        OnBonusRollCompleted?.Invoke(baseResult, bonusValue, finalResult);
    }

    /// <summary>
    /// Método llamado cuando se activa un bonus
    /// </summary>
    public void NotifyBonusActivated(string bonusName, int bonusValue)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"Bonus activado: {bonusName} (+{bonusValue})");
        }

        OnBonusActivated?.Invoke(bonusName, bonusValue);
    }

    /// <summary>
    /// Método llamado cuando se consume un bonus
    /// </summary>
    public void NotifyBonusConsumed(string bonusName, int bonusValue)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"Bonus consumido: {bonusName} (+{bonusValue})");
        }

        OnBonusConsumed?.Invoke(bonusName, bonusValue);
    }

    #endregion

    #region Configuración de Inspector

    /// <summary>
    /// Método para configurar automáticamente las referencias desde el inspector
    /// </summary>
    [ContextMenu("Auto-Configure from Scene")]
    public void AutoConfigureFromScene()
    {
        // Buscar en la escena activa
        Debug.Log("Buscando componentes en la escena...");

        // Buscar por nombre específico del GameObject
        GameObject diceInterface = GameObject.Find("Dice_Interface");
        if (diceInterface != null)
        {
            if (diceManager == null)
                diceManager = diceInterface.GetComponentInChildren<Dice_Manager>();

            if (diceResultText == null)
                diceResultText = diceInterface.transform.Find("Dice_Result")?.GetComponent<TMP_Text>();

            if (bonusDisplayText == null)
            {
                Transform bonusTransform = diceInterface.transform.Find("Bonus");
                if (bonusTransform == null)
                    bonusTransform = diceInterface.transform.Find("Bonus_Txt");

                if (bonusTransform != null)
                    bonusDisplayText = bonusTransform.GetComponent<TMP_Text>();
            }
        }

        // Buscar otros componentes
        if (bonusManager == null)
            bonusManager = FindAnyObjectByType<BonusManager>();

        if (feedbackManager == null)
            feedbackManager = FindAnyObjectByType<BonusFeedbackManager>();

        // Reconfigurar después de encontrar referencias
        ConfigureFeedbackManager();

        Debug.Log("Auto-configuración completada");
        CheckIntegrationStatus();
    }

    /// <summary>
    /// Crea los componentes faltantes automáticamente
    /// </summary>
    [ContextMenu("Create Missing Components")]
    public void CreateMissingComponents()
    {
        GameObject diceInterface = GameObject.Find("Dice_Interface");
        if (diceInterface == null)
        {
            Debug.LogError("No se encontró 'Dice_Interface' en la escena");
            return;
        }

        // Crear BonusFeedbackManager si no existe
        if (feedbackManager == null)
        {
            GameObject feedbackGO = new GameObject("BonusFeedbackManager");
            feedbackGO.transform.SetParent(diceInterface.transform);
            feedbackManager = feedbackGO.AddComponent<BonusFeedbackManager>();
            Debug.Log("BonusFeedbackManager creado automáticamente");
        }

        // Crear DiceBonusIntegration si no existe en la escena
        if (FindAnyObjectByType<DiceBonusIntegration>() == null)
        {
            GameObject integrationGO = new GameObject("DiceBonusIntegration");
            integrationGO.AddComponent<DiceBonusIntegration>();
            Debug.Log("DiceBonusIntegration creado automáticamente");
        }

        Debug.Log("Componentes faltantes creados");
    }

    #endregion

    #region Métodos de Utilidad Pública

    /// <summary>
    /// Obtiene el Dice_Manager configurado
    /// </summary>
    public Dice_Manager GetDiceManager() => diceManager;

    /// <summary>
    /// Obtiene el BonusManager configurado
    /// </summary>
    public BonusManager GetBonusManager() => bonusManager;

    /// <summary>
    /// Obtiene el BonusFeedbackManager configurado
    /// </summary>
    public BonusFeedbackManager GetFeedbackManager() => feedbackManager;

    /// <summary>
    /// Verifica si el sistema está completamente configurado
    /// </summary>
    public bool IsSystemReady()
    {
        return ValidateReferences();
    }

    /// <summary>
    /// Inicia una tirada de dado con feedback completo (método de conveniencia)
    /// </summary>
    public void StartDiceRollWithFeedback()
    {
        if (!IsSystemReady())
        {
            Debug.LogError("El sistema no está listo para hacer tiradas");
            return;
        }

        if (diceManager.IsRolling())
        {
            Debug.LogWarning("Ya hay una tirada en progreso");
            return;
        }

        diceManager.ThrowDice();
    }

    #endregion

    #region Estructuras de Datos

    [System.Serializable]
    public struct SystemStatus
    {
        public bool diceManagerReady;
        public bool bonusManagerReady;
        public bool feedbackManagerReady;
        public bool uiReferencesReady;
        public bool hasActiveBonus;
        public int activeBonusValue;
        public bool isRolling;
        public int collectedBonusCount;

        public bool IsFullyReady => diceManagerReady && bonusManagerReady && feedbackManagerReady && uiReferencesReady;
    }

    #endregion

    private void OnValidate()
    {
        // Validación en el editor
        if (Application.isPlaying) return;

        // Verificar que las referencias estén asignadas
        if (diceResultText == null)
        {
            Debug.LogWarning("diceResultText no está asignado. Busca el componente 'Dice_Result' en tu interfaz.");
        }

        if (bonusDisplayText == null)
        {
            Debug.LogWarning("bonusDisplayText no está asignado. Busca el componente 'Bonus' en tu interfaz.");
        }
    }

    private void OnDestroy()
    {
        // Limpiar eventos
        OnBonusRollCompleted = null;
        OnBonusActivated = null;
        OnBonusConsumed = null;
    }
}