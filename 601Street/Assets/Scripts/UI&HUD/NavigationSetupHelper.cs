using UnityEngine;

/// <summary>
/// Helper para configurar automáticamente el NavigationPriorityManager en la escena
/// VERSIÓN CORREGIDA: Solo activa sistemas, nunca los desactiva
/// </summary>
public class NavigationSetupHelper : MonoBehaviour
{
    [Header("Configuración Automática")]
    [SerializeField] private bool createNavigationManagerIfMissing = true;
    [SerializeField] private bool autoRegisterAllSystems = true;
    [SerializeField] private bool debugOutput = true;

    [Header("Protección UINavigationManager")]
    [SerializeField] private bool protectUINavigationManagers = true; // Nuevo: proteger UINavigationManagers

    private void Awake()
    {
        SetupNavigationSystem();
    }

    private void SetupNavigationSystem()
    {
        if (debugOutput)
            Debug.Log("Configurando sistema de navegación - MODO PROTEGIDO (sin desactivaciones)...");

        // Crear NavigationPriorityManager si no existe
        if (NavigationPriorityManager.Instance == null && createNavigationManagerIfMissing)
        {
            GameObject navManagerObj = new GameObject("NavigationPriorityManager");
            navManagerObj.AddComponent<NavigationPriorityManager>();

            if (debugOutput)
                Debug.Log("NavigationPriorityManager creado automáticamente con protección UINavigationManager");
        }

        // Esperar un frame para que todo se inicialice
        if (autoRegisterAllSystems)
        {
            Invoke(nameof(RegisterAllSystems), 0.1f);
        }
    }

    private void RegisterAllSystems()
    {
        if (NavigationPriorityManager.Instance == null)
        {
            Debug.LogError("NavigationPriorityManager no está disponible para registro automático");
            return;
        }

        int registeredCount = 0;

        // Registrar UINavigationManagers con protección especial
        UINavigationManager[] uiManagers = FindObjectsByType<UINavigationManager>(FindObjectsSortMode.None);
        foreach (var uiManager in uiManagers)
        {
            NavigationPriorityManager.NavigationPriority priority =
                DeterminePriorityForUIManager(uiManager);

            NavigationPriorityManager.Instance.RegisterSystem(
                uiManager.gameObject.name,
                priority,
                uiManager, null, null
            );

            registeredCount++;

            if (debugOutput)
            {
                Debug.Log($"Registrado UINavigationManager PROTEGIDO: {uiManager.gameObject.name} con prioridad {priority} - NUNCA será desactivado");
            }
        }

        // Registrar InventoryNavigationManagers
        InventoryNavigationManager[] inventoryManagers = FindObjectsByType<InventoryNavigationManager>(FindObjectsSortMode.None);
        foreach (var invManager in inventoryManagers)
        {
            NavigationPriorityManager.Instance.RegisterSystem(
                "InventoryNavigation",
                NavigationPriorityManager.NavigationPriority.Inventory,
                null, invManager, null
            );

            registeredCount++;

            if (debugOutput)
                Debug.Log($"Registrado InventoryNavigationManager: {invManager.gameObject.name}");
        }

        // Registrar InteractionCanvasNavigationManagers
        InteractionCanvasNavigationManager[] interactionManagers = FindObjectsByType<InteractionCanvasNavigationManager>(FindObjectsSortMode.None);
        foreach (var intManager in interactionManagers)
        {
            NavigationPriorityManager.Instance.RegisterSystem(
                "InteractionCanvas",
                NavigationPriorityManager.NavigationPriority.Interaction,
                null, null, intManager
            );

            registeredCount++;

            if (debugOutput)
                Debug.Log($"Registrado InteractionCanvasNavigationManager: {intManager.gameObject.name}");
        }

        if (debugOutput)
        {
            Debug.Log($"Sistema de navegación configurado completamente con PROTECCIÓN UINavigationManager.");
            Debug.Log($"Total sistemas registrados: {registeredCount}");
            Debug.Log("IMPORTANTE: UINavigationManager components NUNCA serán desactivados por el sistema de prioridades");
        }
    }

    private NavigationPriorityManager.NavigationPriority DeterminePriorityForUIManager(UINavigationManager uiManager)
    {
        string objectName = uiManager.gameObject.name.ToLower();

        // Determinar prioridad basada en el nombre del GameObject
        if (objectName.Contains("pause") || objectName.Contains("menu"))
        {
            return NavigationPriorityManager.NavigationPriority.PauseMenu;
        }
        else if (objectName.Contains("dialog") || objectName.Contains("dialogue"))
        {
            return NavigationPriorityManager.NavigationPriority.Dialog;
        }
        else if (objectName.Contains("interaction"))
        {
            return NavigationPriorityManager.NavigationPriority.Interaction;
        }
        else if (objectName.Contains("inventory"))
        {
            return NavigationPriorityManager.NavigationPriority.Inventory;
        }

        return NavigationPriorityManager.NavigationPriority.Normal;
    }

    /// <summary>
    /// Método manual para forzar el registro de sistemas
    /// </summary>
    [ContextMenu("Force Register All Systems")]
    public void ForceRegisterAllSystems()
    {
        RegisterAllSystems();
    }

    /// <summary>
    /// Método para debug del estado actual
    /// </summary>
    [ContextMenu("Debug Navigation System")]
    public void DebugNavigationSystem()
    {
        if (NavigationPriorityManager.Instance != null)
        {
            NavigationPriorityManager.Instance.DebugCurrentState();
        }
        else
        {
            Debug.LogWarning("NavigationPriorityManager no está disponible");
        }

        // Debug adicional específico para UINavigationManagers
        UINavigationManager[] uiManagers = FindObjectsByType<UINavigationManager>(FindObjectsSortMode.None);
        Debug.Log($"=== ESTADO UINavigationManager PROTEGIDOS ===");
        Debug.Log($"Total UINavigationManagers encontrados: {uiManagers.Length}");

        foreach (var uiManager in uiManagers)
        {
            Debug.Log($"  - {uiManager.gameObject.name}: Enabled={uiManager.enabled}, GameObject Active={uiManager.gameObject.activeInHierarchy}");
        }
        Debug.Log("Todos los UINavigationManager están PROTEGIDOS contra desactivación");
    }

    /// <summary>
    /// NUEVO: Método para verificar que todos los UINavigationManager estén activos
    /// </summary>
    [ContextMenu("Verify UINavigationManager Protection")]
    public void VerifyUINavigationManagerProtection()
    {
        UINavigationManager[] uiManagers = FindObjectsByType<UINavigationManager>(FindObjectsSortMode.None);

        Debug.Log("=== VERIFICACIÓN PROTECCIÓN UINavigationManager ===");

        int activeCount = 0;
        int inactiveCount = 0;

        foreach (var uiManager in uiManagers)
        {
            if (uiManager.enabled)
            {
                activeCount++;
                Debug.Log($"✓ PROTEGIDO: {uiManager.gameObject.name} está activo");
            }
            else
            {
                inactiveCount++;
                Debug.LogWarning($"⚠ POSIBLE PROBLEMA: {uiManager.gameObject.name} está desactivado");
            }
        }

        Debug.Log($"Resumen: {activeCount} activos, {inactiveCount} inactivos de {uiManagers.Length} total");

        if (inactiveCount == 0)
        {
            Debug.Log("✓ PROTECCIÓN EXITOSA: Todos los UINavigationManager están activos");
        }
        else
        {
            Debug.LogWarning("⚠ REVISAR: Algunos UINavigationManager están desactivados");
        }
    }

    /// <summary>
    /// NUEVO: Método para reactivar todos los UINavigationManager desactivados
    /// </summary>
    [ContextMenu("Force Reactivate All UINavigationManagers")]
    public void ForceReactivateAllUINavigationManagers()
    {
        UINavigationManager[] uiManagers = FindObjectsByType<UINavigationManager>(FindObjectsSortMode.None);

        int reactivatedCount = 0;

        foreach (var uiManager in uiManagers)
        {
            if (!uiManager.enabled)
            {
                uiManager.enabled = true;
                reactivatedCount++;
                Debug.Log($"REACTIVADO: {uiManager.gameObject.name}");
            }
        }

        if (reactivatedCount > 0)
        {
            Debug.Log($"Se reactivaron {reactivatedCount} UINavigationManager components");
        }
        else
        {
            Debug.Log("Todos los UINavigationManager ya estaban activos");
        }
    }
}