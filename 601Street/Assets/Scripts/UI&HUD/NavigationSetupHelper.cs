using UnityEngine;

/// <summary>
/// Helper para configurar automáticamente el NavigationPriorityManager en la escena
/// Colócalo en un GameObject persistente o en el primer GameObject que se inicialice
/// </summary>
public class NavigationSetupHelper : MonoBehaviour
{
    [Header("Configuración Automática")]
    [SerializeField] private bool createNavigationManagerIfMissing = true;
    [SerializeField] private bool autoRegisterAllSystems = true;
    [SerializeField] private bool debugOutput = true;

    private void Awake()
    {
        SetupNavigationSystem();
    }

    private void SetupNavigationSystem()
    {
        if (debugOutput)
            Debug.Log("Configurando sistema de navegación...");

        // Crear NavigationPriorityManager si no existe
        if (NavigationPriorityManager.Instance == null && createNavigationManagerIfMissing)
        {
            GameObject navManagerObj = new GameObject("NavigationPriorityManager");
            navManagerObj.AddComponent<NavigationPriorityManager>();

            if (debugOutput)
                Debug.Log("NavigationPriorityManager creado automáticamente");
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

        // Registrar UINavigationManagers
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
                Debug.Log($"Registrado UINavigationManager: {uiManager.gameObject.name} con prioridad {priority}");
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
            Debug.Log($"Sistema de navegación configurado completamente. Total sistemas registrados: {registeredCount}");
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
    }
}