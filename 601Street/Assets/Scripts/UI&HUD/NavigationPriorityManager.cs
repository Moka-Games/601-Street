using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor centralizado de prioridades de navegación que evita conflictos entre sistemas
/// </summary>
public class NavigationPriorityManager : MonoBehaviour
{
    public static NavigationPriorityManager Instance { get; private set; }

    [System.Serializable]
    public enum NavigationPriority
    {
        Background = 0,      // Elementos de fondo (desactivados siempre)
        Normal = 1,          // Navegación normal de juego
        Inventory = 2,       // Sistema de inventario
        Interaction = 3,     // Canvas de interacción
        PauseMenu = 4,       // Menú de pausa (máxima prioridad)
        Dialog = 5           // Diálogos (prioridad absoluta)
    }

    [System.Serializable]
    public class NavigationSystem
    {
        public string systemName;
        public NavigationPriority priority;
        public UINavigationManager uiNavigationManager;
        public InventoryNavigationManager inventoryNavigationManager;
        public InteractionCanvasNavigationManager interactionNavigationManager;
        public bool wasActiveBeforeDisable;
        public bool isCurrentlyActive;

        public void SetActive(bool active)
        {
            isCurrentlyActive = active;

            if (uiNavigationManager != null)
            {
                if (active)
                    uiNavigationManager.EnableUINavigation();
                else
                    uiNavigationManager.DisableUINavigation();
            }

            if (inventoryNavigationManager != null)
            {
                inventoryNavigationManager.enabled = active;
            }

            if (interactionNavigationManager != null)
            {
                if (active)
                    interactionNavigationManager.enabled = true;
                else
                    interactionNavigationManager.enabled = false;
            }
        }

        public bool IsActive()
        {
            if (uiNavigationManager != null)
                return uiNavigationManager.enabled;

            if (inventoryNavigationManager != null)
                return inventoryNavigationManager.enabled;

            if (interactionNavigationManager != null)
                return interactionNavigationManager.enabled;

            return false;
        }
    }

    [Header("Configuración")]
    [SerializeField] private List<NavigationSystem> registeredSystems = new List<NavigationSystem>();

    private NavigationPriority currentHighestPriority = NavigationPriority.Normal;
    private Dictionary<NavigationPriority, List<NavigationSystem>> systemsByPriority = new Dictionary<NavigationPriority, List<NavigationSystem>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePrioritySystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Auto-registrar sistemas existentes si la lista está vacía
        if (registeredSystems.Count == 0)
        {
            AutoRegisterSystems();
        }

        RefreshSystemPriorities();
    }

    private void InitializePrioritySystem()
    {
        // Inicializar diccionario de prioridades
        foreach (NavigationPriority priority in System.Enum.GetValues(typeof(NavigationPriority)))
        {
            systemsByPriority[priority] = new List<NavigationSystem>();
        }
    }

    private void AutoRegisterSystems()
    {
        Debug.Log("Auto-registrando sistemas de navegación...");

        // Buscar UINavigationManager (PauseMenu y otros)
        UINavigationManager[] uiManagers = FindObjectsByType<UINavigationManager>(FindObjectsSortMode.None);
        foreach (var uiManager in uiManagers)
        {
            NavigationPriority priority = NavigationPriority.Normal;

            // Determinar prioridad basada en el nombre del GameObject
            if (uiManager.gameObject.name.Contains("Pause") || uiManager.gameObject.name.Contains("Menu"))
            {
                priority = NavigationPriority.PauseMenu;
            }

            RegisterSystem(uiManager.gameObject.name, priority, uiManager, null, null);
        }

        // Buscar InventoryNavigationManager
        InventoryNavigationManager[] inventoryManagers = FindObjectsByType<InventoryNavigationManager>(FindObjectsSortMode.None);
        foreach (var invManager in inventoryManagers)
        {
            RegisterSystem("InventoryNavigation", NavigationPriority.Inventory, null, invManager, null);
        }

        // Buscar InteractionCanvasNavigationManager
        InteractionCanvasNavigationManager[] interactionManagers = FindObjectsByType<InteractionCanvasNavigationManager>(FindObjectsSortMode.None);
        foreach (var intManager in interactionManagers)
        {
            RegisterSystem("InteractionCanvas", NavigationPriority.Interaction, null, null, intManager);
        }

        Debug.Log($"Auto-registrados {registeredSystems.Count} sistemas de navegación");
    }

    /// <summary>
    /// Registra un sistema de navegación con su prioridad
    /// </summary>
    public void RegisterSystem(string systemName, NavigationPriority priority,
                              UINavigationManager uiNav = null,
                              InventoryNavigationManager invNav = null,
                              InteractionCanvasNavigationManager intNav = null)
    {
        var system = new NavigationSystem
        {
            systemName = systemName,
            priority = priority,
            uiNavigationManager = uiNav,
            inventoryNavigationManager = invNav,
            interactionNavigationManager = intNav,
            wasActiveBeforeDisable = false,
            isCurrentlyActive = false
        };

        // Verificar estado inicial
        system.isCurrentlyActive = system.IsActive();
        system.wasActiveBeforeDisable = system.isCurrentlyActive;

        registeredSystems.Add(system);
        systemsByPriority[priority].Add(system);

        Debug.Log($"Sistema registrado: {systemName} con prioridad {priority}");
    }

    /// <summary>
    /// Activa un sistema con prioridad específica y gestiona conflictos
    /// </summary>
    public void ActivateSystemWithPriority(NavigationPriority priority, string systemName = "")
    {
        Debug.Log($"Activando sistema con prioridad {priority} ({systemName})");

        // Guardar estados actuales antes de cambiar
        SaveCurrentStates();

        // Si la nueva prioridad es menor o igual, no hacer nada
        if (priority <= currentHighestPriority && currentHighestPriority != NavigationPriority.Normal)
        {
            Debug.Log($"Prioridad {priority} no es mayor que la actual {currentHighestPriority}, ignorando");
            return;
        }

        // Desactivar sistemas de menor prioridad
        DeactivateLowerPrioritySystems(priority);

        // Activar sistemas de la nueva prioridad
        ActivateSystemsOfPriority(priority, systemName);

        currentHighestPriority = priority;
    }

    /// <summary>
    /// Desactiva un sistema con prioridad específica y restaura sistemas anteriores
    /// </summary>
    public void DeactivateSystemWithPriority(NavigationPriority priority, string systemName = "")
    {
        Debug.Log($"Desactivando sistema con prioridad {priority} ({systemName})");

        // Si no es la prioridad actual más alta, no hacer nada
        if (priority != currentHighestPriority)
        {
            Debug.Log($"La prioridad {priority} no es la actual más alta ({currentHighestPriority}), ignorando");
            return;
        }

        // Desactivar sistemas de esta prioridad
        DeactivateSystemsOfPriority(priority, systemName);

        // Encontrar la siguiente prioridad más alta que tenga sistemas activos
        NavigationPriority nextHighestPriority = FindNextHighestPriority();

        // Restaurar sistemas de la siguiente prioridad más alta
        if (nextHighestPriority != currentHighestPriority)
        {
            RestoreSystemsOfPriority(nextHighestPriority);
            currentHighestPriority = nextHighestPriority;
        }
    }

    private void SaveCurrentStates()
    {
        foreach (var system in registeredSystems)
        {
            if (system.isCurrentlyActive && !system.wasActiveBeforeDisable)
            {
                system.wasActiveBeforeDisable = true;
            }
        }
    }

    private void DeactivateLowerPrioritySystems(NavigationPriority targetPriority)
    {
        foreach (var system in registeredSystems)
        {
            if (system.priority < targetPriority && system.isCurrentlyActive)
            {
                system.wasActiveBeforeDisable = true;
                system.SetActive(false);
                Debug.Log($"Desactivado temporalmente: {system.systemName} (prioridad {system.priority})");
            }
        }
    }

    private void ActivateSystemsOfPriority(NavigationPriority priority, string specificSystemName = "")
    {
        if (systemsByPriority.ContainsKey(priority))
        {
            foreach (var system in systemsByPriority[priority])
            {
                if (string.IsNullOrEmpty(specificSystemName) || system.systemName.Contains(specificSystemName))
                {
                    system.SetActive(true);
                    Debug.Log($"Activado: {system.systemName} (prioridad {priority})");
                }
            }
        }
    }

    private void DeactivateSystemsOfPriority(NavigationPriority priority, string specificSystemName = "")
    {
        if (systemsByPriority.ContainsKey(priority))
        {
            foreach (var system in systemsByPriority[priority])
            {
                if (string.IsNullOrEmpty(specificSystemName) || system.systemName.Contains(specificSystemName))
                {
                    system.SetActive(false);
                    Debug.Log($"Desactivado: {system.systemName} (prioridad {priority})");
                }
            }
        }
    }

    private void RestoreSystemsOfPriority(NavigationPriority priority)
    {
        if (systemsByPriority.ContainsKey(priority))
        {
            foreach (var system in systemsByPriority[priority])
            {
                if (system.wasActiveBeforeDisable)
                {
                    system.SetActive(true);
                    system.wasActiveBeforeDisable = false; // Reset flag
                    Debug.Log($"Restaurado: {system.systemName} (prioridad {priority})");
                }
            }
        }
    }

    private NavigationPriority FindNextHighestPriority()
    {
        // Buscar la prioridad más alta que tenga sistemas que estaban activos
        for (int i = (int)NavigationPriority.Dialog; i >= 0; i--)
        {
            NavigationPriority priority = (NavigationPriority)i;

            if (priority == currentHighestPriority)
                continue;

            if (systemsByPriority.ContainsKey(priority))
            {
                foreach (var system in systemsByPriority[priority])
                {
                    if (system.wasActiveBeforeDisable)
                    {
                        return priority;
                    }
                }
            }
        }

        return NavigationPriority.Normal;
    }

    private void RefreshSystemPriorities()
    {
        // Reorganizar sistemas por prioridad
        systemsByPriority.Clear();
        foreach (NavigationPriority priority in System.Enum.GetValues(typeof(NavigationPriority)))
        {
            systemsByPriority[priority] = new List<NavigationSystem>();
        }

        foreach (var system in registeredSystems)
        {
            systemsByPriority[system.priority].Add(system);
        }
    }

    /// <summary>
    /// Métodos de conveniencia para sistemas específicos
    /// </summary>
    public void ActivateInventoryNavigation()
    {
        ActivateSystemWithPriority(NavigationPriority.Inventory, "Inventory");
    }

    public void DeactivateInventoryNavigation()
    {
        DeactivateSystemWithPriority(NavigationPriority.Inventory, "Inventory");
    }

    public void ActivateInteractionNavigation()
    {
        ActivateSystemWithPriority(NavigationPriority.Interaction, "Interaction");
    }

    public void DeactivateInteractionNavigation()
    {
        DeactivateSystemWithPriority(NavigationPriority.Interaction, "Interaction");
    }

    public void ActivatePauseMenuNavigation()
    {
        ActivateSystemWithPriority(NavigationPriority.PauseMenu, "Pause");
    }

    public void DeactivatePauseMenuNavigation()
    {
        DeactivateSystemWithPriority(NavigationPriority.PauseMenu, "Pause");
    }

    /// <summary>
    /// Método de debug para inspector
    /// </summary>
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log($"=== NAVIGATION PRIORITY MANAGER STATE ===");
        Debug.Log($"Current Highest Priority: {currentHighestPriority}");
        Debug.Log($"Registered Systems: {registeredSystems.Count}");

        foreach (var system in registeredSystems)
        {
            Debug.Log($"  {system.systemName} - Priority: {system.priority} - Active: {system.isCurrentlyActive} - Was Active: {system.wasActiveBeforeDisable}");
        }
    }

    /// <summary>
    /// Fuerza una actualización completa del estado
    /// </summary>
    [ContextMenu("Force Refresh")]
    public void ForceRefresh()
    {
        foreach (var system in registeredSystems)
        {
            system.isCurrentlyActive = system.IsActive();
        }

        RefreshSystemPriorities();
        Debug.Log("Estados de navegación actualizados");
    }
}