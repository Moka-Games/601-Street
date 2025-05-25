using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// Extensión del UINavigationManager para manejar la navegación dinámica de bonuses
/// Se encarga de añadir/remover bonuses automáticamente del sistema de navegación
/// </summary>
public class BonusNavigationExtension : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private UINavigationManager navigationManager;
    [SerializeField] private BonusManager bonusManager;
    [SerializeField] private Transform bonusesContentParent; // Padre donde se instancian los bonuses

    [Header("Configuración")]
    [SerializeField] private bool enableAutomaticNavigation = true;
    [SerializeField] private bool debugMode = false;
    [SerializeField] private float navigationPriority = 1f; // Prioridad para ordenar bonuses

    [Header("Integración con Ventana de Bonuses")]
    [SerializeField] private BonusWindowController windowController;
    [SerializeField] private bool addToNavigationWhenWindowOpen = true;
    [SerializeField] private bool removeFromNavigationWhenWindowClosed = true;

    // Estado interno
    private List<Button> managedBonusButtons = new List<Button>();
    private bool isWindowCurrentlyOpen = false;
    private Coroutine bonusDetectionCoroutine;

    // Cache para optimización
    private int lastKnownBonusCount = 0;
    private Dictionary<Button, BonusUI> bonusUICache = new Dictionary<Button, BonusUI>();

    private void Start()
    {
        InitializeReferences();
        StartBonusDetectionSystem();
    }

    private void InitializeReferences()
    {
        // Buscar referencias automáticamente si no están asignadas
        if (navigationManager == null)
        {
            navigationManager = FindAnyObjectByType<UINavigationManager>();
            if (navigationManager == null)
            {
                Debug.LogWarning("BonusNavigationExtension: UINavigationManager no encontrado");
                return;
            }
        }

        if (bonusManager == null)
        {
            bonusManager = BonusManager.Instance;
            if (bonusManager == null)
            {
                Debug.LogWarning("BonusNavigationExtension: BonusManager no encontrado");
                return;
            }
        }

        if (windowController == null)
        {
            windowController = FindAnyObjectByType<BonusWindowController>();
        }

        if (bonusesContentParent == null && bonusManager != null)
        {
            // Intentar obtener el parent desde el BonusManager via reflection o buscar en la escena
            bonusesContentParent = GameObject.Find("BonusesContent")?.transform;
            if (bonusesContentParent == null)
            {
                Debug.LogWarning("BonusNavigationExtension: No se pudo encontrar BonusesContent parent");
            }
        }

        Debug.Log($"BonusNavigationExtension inicializado - NavigationManager: {navigationManager != null}, BonusManager: {bonusManager != null}");
    }

    private void StartBonusDetectionSystem()
    {
        if (enableAutomaticNavigation && bonusDetectionCoroutine == null)
        {
            bonusDetectionCoroutine = StartCoroutine(BonusDetectionCoroutine());
        }
    }

    private void StopBonusDetectionSystem()
    {
        if (bonusDetectionCoroutine != null)
        {
            StopCoroutine(bonusDetectionCoroutine);
            bonusDetectionCoroutine = null;
        }
    }

    /// <summary>
    /// Corrutina que detecta automáticamente cambios en los bonuses
    /// </summary>
    private IEnumerator BonusDetectionCoroutine()
    {
        while (enableAutomaticNavigation)
        {
            yield return new WaitForSeconds(0.2f); // Verificar cada 0.2 segundos

            DetectBonusChanges();
        }
    }

    /// <summary>
    /// Detecta cambios en la cantidad de bonuses y actualiza la navegación
    /// </summary>
    private void DetectBonusChanges()
    {
        if (bonusManager == null) return;

        int currentBonusCount = bonusManager.GetCollectedBonusCount();

        // Si cambió la cantidad de bonuses, actualizar
        if (currentBonusCount != lastKnownBonusCount)
        {
            if (debugMode)
            {
                Debug.Log($"Cambio detectado en bonuses: {lastKnownBonusCount} -> {currentBonusCount}");
            }

            lastKnownBonusCount = currentBonusCount;
            RefreshBonusNavigation();
        }

        // También verificar si la ventana cambió de estado
        CheckWindowStateChange();
    }

    /// <summary>
    /// Verifica si la ventana de bonuses cambió de estado
    /// </summary>
    private void CheckWindowStateChange()
    {
        if (windowController == null) return;

        bool windowOpen = windowController.IsWindowOpen();
        if (windowOpen != isWindowCurrentlyOpen)
        {
            isWindowCurrentlyOpen = windowOpen;

            if (debugMode)
            {
                Debug.Log($"Estado de ventana cambió: {(windowOpen ? "Abierta" : "Cerrada")}");
            }

            HandleWindowStateChange(windowOpen);
        }
    }

    /// <summary>
    /// Maneja los cambios de estado de la ventana
    /// </summary>
    private void HandleWindowStateChange(bool isOpen)
    {
        if (isOpen && addToNavigationWhenWindowOpen)
        {
            AddAllBonusesToNavigation();
        }
        else if (!isOpen && removeFromNavigationWhenWindowClosed)
        {
            RemoveAllBonusesFromNavigation();
        }
    }

    /// <summary>
    /// Refresca toda la navegación de bonuses
    /// </summary>
    private void RefreshBonusNavigation()
    {
        // Primero limpiar los bonuses que ya no existen
        CleanupRemovedBonuses();

        // Luego añadir los nuevos bonuses
        DetectAndAddNewBonuses();

        // Actualizar el sistema de navegación
        if (navigationManager != null)
        {
            navigationManager.ForceAutoSelectionCheck();
        }
    }

    /// <summary>
    /// Limpia bonuses que han sido removidos
    /// </summary>
    private void CleanupRemovedBonuses()
    {
        List<Button> buttonsToRemove = new List<Button>();

        foreach (var button in managedBonusButtons)
        {
            if (button == null || !button.gameObject.activeInHierarchy)
            {
                buttonsToRemove.Add(button);
            }
        }

        foreach (var button in buttonsToRemove)
        {
            OnBonusRemoved(button);
        }
    }

    /// <summary>
    /// Detecta y añade nuevos bonuses al sistema de navegación
    /// </summary>
    private void DetectAndAddNewBonuses()
    {
        if (bonusesContentParent == null) return;

        // Buscar todos los BonusUI activos
        BonusUI[] allBonusUIs = bonusesContentParent.GetComponentsInChildren<BonusUI>();

        foreach (var bonusUI in allBonusUIs)
        {
            Button bonusButton = bonusUI.GetButton();
            if (bonusButton != null && !managedBonusButtons.Contains(bonusButton))
            {
                OnBonusAdded(bonusButton);
            }
        }
    }

    /// <summary>
    /// Añade todos los bonuses existentes a la navegación
    /// </summary>
    private void AddAllBonusesToNavigation()
    {
        if (bonusesContentParent == null || navigationManager == null) return;

        BonusUI[] allBonusUIs = bonusesContentParent.GetComponentsInChildren<BonusUI>();

        foreach (var bonusUI in allBonusUIs)
        {
            Button bonusButton = bonusUI.GetButton();
            if (bonusButton != null && bonusButton.gameObject.activeInHierarchy && bonusButton.interactable)
            {
                if (!managedBonusButtons.Contains(bonusButton))
                {
                    OnBonusAdded(bonusButton);
                }
            }
        }

        if (debugMode)
        {
            Debug.Log($"Añadidos {managedBonusButtons.Count} bonuses a la navegación");
        }
    }

    /// <summary>
    /// Remueve todos los bonuses de la navegación
    /// </summary>
    private void RemoveAllBonusesFromNavigation()
    {
        if (navigationManager == null) return;

        foreach (var button in managedBonusButtons.ToList())
        {
            OnBonusRemoved(button);
        }

        if (debugMode)
        {
            Debug.Log("Todos los bonuses removidos de la navegación");
        }
    }

    #region Public Methods - Llamados por BonusUI

    /// <summary>
    /// Llamado cuando se añade un nuevo bonus
    /// </summary>
    public void OnBonusAdded(Button bonusButton)
    {
        if (bonusButton == null || managedBonusButtons.Contains(bonusButton))
            return;

        if (navigationManager == null)
        {
            Debug.LogWarning("BonusNavigationExtension: NavigationManager no disponible");
            return;
        }

        // Añadir a nuestro tracking
        managedBonusButtons.Add(bonusButton);

        // Añadir al sistema de navegación si la ventana está abierta o no importa el estado
        bool shouldAdd = !addToNavigationWhenWindowOpen || isWindowCurrentlyOpen;

        if (shouldAdd)
        {
            navigationManager.AddNavigableElement(bonusButton);

            // Cachear el componente BonusUI para optimización
            BonusUI bonusUI = bonusButton.GetComponent<BonusUI>();
            if (bonusUI != null)
            {
                bonusUICache[bonusButton] = bonusUI;
            }

            if (debugMode)
            {
                Debug.Log($"Bonus añadido a navegación: {bonusButton.name}");
            }

            // Forzar verificación de selección automática
            navigationManager.ForceAutoSelectionCheck();
        }
    }

    /// <summary>
    /// Llamado cuando se remueve un bonus
    /// </summary>
    public void OnBonusRemoved(Button bonusButton)
    {
        if (bonusButton == null)
            return;

        // Remover de nuestro tracking
        managedBonusButtons.Remove(bonusButton);

        // Remover del cache
        if (bonusUICache.ContainsKey(bonusButton))
        {
            bonusUICache.Remove(bonusButton);
        }

        // Remover del sistema de navegación
        if (navigationManager != null)
        {
            navigationManager.RemoveNavigableElement(bonusButton);

            if (debugMode)
            {
                Debug.Log($"Bonus removido de navegación: {bonusButton.name}");
            }

            // Forzar verificación de selección automática
            navigationManager.ForceAutoSelectionCheck();
        }
    }

    #endregion

    #region Configuration Methods

    /// <summary>
    /// Habilita o deshabilita la navegación automática
    /// </summary>
    public void SetAutomaticNavigation(bool enabled)
    {
        enableAutomaticNavigation = enabled;

        if (enabled)
        {
            StartBonusDetectionSystem();
        }
        else
        {
            StopBonusDetectionSystem();
        }
    }

    /// <summary>
    /// Configura el comportamiento de integración con la ventana
    /// </summary>
    public void ConfigureWindowIntegration(bool addWhenOpen, bool removeWhenClosed)
    {
        addToNavigationWhenWindowOpen = addWhenOpen;
        removeFromNavigationWhenWindowClosed = removeWhenClosed;

        if (debugMode)
        {
            Debug.Log($"Configuración de ventana actualizada - AddWhenOpen: {addWhenOpen}, RemoveWhenClosed: {removeWhenClosed}");
        }
    }

    /// <summary>
    /// Establece el parent donde se instancian los bonuses
    /// </summary>
    public void SetBonusesParent(Transform parent)
    {
        bonusesContentParent = parent;
        RefreshBonusNavigation();
    }

    /// <summary>
    /// Fuerza una actualización completa del sistema de navegación
    /// </summary>
    public void ForceRefreshNavigation()
    {
        RefreshBonusNavigation();
    }

    /// <summary>
    /// Obtiene el botón de bonus actualmente seleccionado (si existe)
    /// </summary>
    public Button GetCurrentlySelectedBonusButton()
    {
        if (navigationManager == null) return null;

        var currentSelected = navigationManager.CurrentSelected;
        if (currentSelected is Button button && managedBonusButtons.Contains(button))
        {
            return button;
        }

        return null;
    }

    /// <summary>
    /// Selecciona un bonus específico por índice
    /// </summary>
    public void SelectBonusByIndex(int index)
    {
        if (index < 0 || index >= managedBonusButtons.Count)
        {
            Debug.LogWarning($"Índice de bonus fuera de rango: {index}");
            return;
        }

        Button targetButton = managedBonusButtons[index];
        if (targetButton != null && navigationManager != null)
        {
            // Buscar el índice en la lista completa del NavigationManager
            int navigationIndex = navigationManager.NavigableElements.IndexOf(targetButton);
            if (navigationIndex >= 0)
            {
                // Usar método interno del NavigationManager si está disponible
                // Como no tenemos acceso directo, usamos el EventSystem
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(targetButton.gameObject);
            }
        }
    }

    /// <summary>
    /// Selecciona el primer bonus disponible
    /// </summary>
    public void SelectFirstBonus()
    {
        SelectBonusByIndex(0);
    }

    /// <summary>
    /// Selecciona el último bonus disponible
    /// </summary>
    public void SelectLastBonus()
    {
        SelectBonusByIndex(managedBonusButtons.Count - 1);
    }

    #endregion

    #region Integration with BonusWindowController

    /// <summary>
    /// Método llamado por BonusWindowController cuando la ventana se abre
    /// </summary>
    public void OnBonusWindowOpened()
    {
        if (debugMode)
        {
            Debug.Log("Ventana de bonuses abierta - Integrando navegación");
        }

        isWindowCurrentlyOpen = true;

        if (addToNavigationWhenWindowOpen)
        {
            AddAllBonusesToNavigation();
        }
    }

    /// <summary>
    /// Método llamado por BonusWindowController cuando la ventana se cierra
    /// </summary>
    public void OnBonusWindowClosed()
    {
        if (debugMode)
        {
            Debug.Log("Ventana de bonuses cerrada - Removiendo navegación");
        }

        isWindowCurrentlyOpen = false;

        if (removeFromNavigationWhenWindowClosed)
        {
            RemoveAllBonusesFromNavigation();
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Obtiene información detallada sobre el estado actual
    /// </summary>
    public string GetDetailedStatus()
    {
        string status = "=== BONUS NAVIGATION EXTENSION STATUS ===\n";
        status += $"Navegación Automática: {enableAutomaticNavigation}\n";
        status += $"Ventana Abierta: {isWindowCurrentlyOpen}\n";
        status += $"Bonuses Gestionados: {managedBonusButtons.Count}\n";
        status += $"NavigationManager: {(navigationManager != null ? "Disponible" : "NO DISPONIBLE")}\n";
        status += $"BonusManager: {(bonusManager != null ? "Disponible" : "NO DISPONIBLE")}\n";
        status += $"WindowController: {(windowController != null ? "Disponible" : "NO DISPONIBLE")}\n";
        status += $"BonusesParent: {(bonusesContentParent != null ? bonusesContentParent.name : "NO ASIGNADO")}\n";

        if (managedBonusButtons.Count > 0)
        {
            status += "Bonuses en Navegación:\n";
            for (int i = 0; i < managedBonusButtons.Count; i++)
            {
                var button = managedBonusButtons[i];
                bool isActive = button != null && button.gameObject.activeInHierarchy;
                bool isInteractable = button != null && button.interactable;
                status += $"  {i + 1}. {(button != null ? button.name : "NULL")} - Activo: {isActive}, Interactuable: {isInteractable}\n";
            }
        }

        status += "=== END STATUS ===";
        return status;
    }

    /// <summary>
    /// Cuenta los bonuses activos e interactuables
    /// </summary>
    public int GetActiveInteractableBonusCount()
    {
        return managedBonusButtons.Count(button =>
            button != null &&
            button.gameObject.activeInHierarchy &&
            button.interactable);
    }

    /// <summary>
    /// Verifica si hay bonuses disponibles para navegación
    /// </summary>
    public bool HasNavigableBonuses()
    {
        return GetActiveInteractableBonusCount() > 0;
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug Status")]
    public void DebugStatus()
    {
        Debug.Log(GetDetailedStatus());
    }

    [ContextMenu("Force Refresh Navigation")]
    public void ForceRefreshNavigationFromContext()
    {
        ForceRefreshNavigation();
    }

    [ContextMenu("Add All Bonuses to Navigation")]
    public void AddAllBonusesToNavigationFromContext()
    {
        AddAllBonusesToNavigation();
    }

    [ContextMenu("Remove All Bonuses from Navigation")]
    public void RemoveAllBonusesFromNavigationFromContext()
    {
        RemoveAllBonusesFromNavigation();
    }

    [ContextMenu("Select First Bonus")]
    public void SelectFirstBonusFromContext()
    {
        SelectFirstBonus();
    }

    [ContextMenu("Test Window State Toggle")]
    public void TestWindowStateToggle()
    {
        isWindowCurrentlyOpen = !isWindowCurrentlyOpen;
        HandleWindowStateChange(isWindowCurrentlyOpen);
        Debug.Log($"Estado de ventana simulado: {(isWindowCurrentlyOpen ? "Abierta" : "Cerrada")}");
    }

    #endregion

    #region Event Handlers

    private void OnEnable()
    {
        StartBonusDetectionSystem();
    }

    private void OnDisable()
    {
        StopBonusDetectionSystem();
    }

    private void OnDestroy()
    {
        StopBonusDetectionSystem();

        // Limpiar todas las referencias
        if (navigationManager != null)
        {
            foreach (var button in managedBonusButtons.ToList())
            {
                if (button != null)
                {
                    navigationManager.RemoveNavigableElement(button);
                }
            }
        }

        managedBonusButtons.Clear();
        bonusUICache.Clear();
    }

    #endregion

    #region Properties

    public int ManagedBonusCount => managedBonusButtons.Count;
    public bool IsWindowOpen => isWindowCurrentlyOpen;
    public bool AutomaticNavigationEnabled => enableAutomaticNavigation;
    public List<Button> ManagedButtons => new List<Button>(managedBonusButtons);

    #endregion
}