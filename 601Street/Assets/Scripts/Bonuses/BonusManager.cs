using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Gestor principal del sistema de bonuses para el dado
/// VERSIÓN ACTUALIZADA: Incluye integración con sistema de navegación dinámico
/// </summary>
public class BonusManager : MonoBehaviour
{
    public static BonusManager Instance { get; private set; }

    [Header("Referencias de la Interfaz")]
    [SerializeField] private GameObject bonusWindow; // La ventana completa de bonuses (siempre activa)
    [SerializeField] private Button openWindowButton; // Botón para abrir/cerrar ventana
    [SerializeField] private Animator bonusAnimator; // Animator del Parent/Background con animaciones Open_Bonuses/Close_Bonuses
    [SerializeField] private Transform bonusesContent; // Padre donde se instancian los bonuses
    [SerializeField] private GameObject bonusPrefab; // Prefab "Bonus_Prefab_Parent"

    [Header("Configuración de Animaciones")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    [Header("Referencias al Sistema de Dados")]
    [SerializeField] private Dice_Manager diceManager;

    [Header("NUEVO: Integración con Navegación")]
    [SerializeField] private BonusNavigationExtension navigationExtension;
    [SerializeField] private UINavigationManager uiNavigationManager;
    [SerializeField] private bool enableNavigationIntegration = true;

    // Estado del sistema
    private List<CollectedBonus> collectedBonuses = new List<CollectedBonus>();
    private CollectedBonus activeBonus = null;
    private bool canActivateBonuses = true;

    // Variables para el estado de la ventana
    private bool isWindowOpen = false;

    [System.Serializable]
    public class CollectedBonus
    {
        public string bonusName;
        public int bonusValue;
        public string description;
        public Sprite icon; // Opcional para diferentes iconos
        public GameObject uiInstance; // Referencia al prefab instanciado
        public BonusUI bonusUIScript; // Script del prefab

        public CollectedBonus(string name, int value, string desc = "", Sprite bonusIcon = null)
        {
            bonusName = name;
            bonusValue = value;
            description = string.IsNullOrEmpty(desc) ? $"+{value} al dado" : desc;
            icon = bonusIcon;
        }
    }

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
    }

    private void Start()
    {
        // Buscar referencias si no están asignadas
        if (diceManager == null)
            diceManager = FindAnyObjectByType<Dice_Manager>();

        // NUEVO: Inicializar sistema de navegación
        InitializeNavigationSystem();

        Debug.Log("=== BONUS MANAGER INITIALIZATION ===");
        Debug.Log($"BonusWindow assigned: {bonusWindow != null}");
        Debug.Log($"BonusAnimator assigned: {bonusAnimator != null}");
        Debug.Log($"OpenWindowButton assigned: {openWindowButton != null}");
        Debug.Log($"BonusesContent assigned: {bonusesContent != null}");
        Debug.Log($"BonusPrefab assigned: {bonusPrefab != null}");
        Debug.Log($"NavigationExtension assigned: {navigationExtension != null}");

        // Configurar la ventana principal (siempre activa pero inicialmente oculta)
        if (bonusWindow != null)
        {
            bonusWindow.SetActive(false); // Inicialmente oculta hasta que haya bonuses
            Debug.Log("BonusWindow configurada (oculta inicialmente)");
        }
        else
        {
            Debug.LogError("BonusWindow no está asignado en el inspector");
        }

        // Verificar Animator
        if (bonusAnimator != null)
        {
            // El animator debe empezar en estado cerrado por defecto
            Debug.Log("BonusAnimator configurado - Panel estará cerrado inicialmente");
        }
        else
        {
            Debug.LogError("BonusAnimator no está asignado en el inspector");
        }

        // Configurar botón de apertura
        if (openWindowButton != null)
        {
            openWindowButton.onClick.AddListener(ToggleBonusWindow);
            openWindowButton.gameObject.SetActive(false); // Ocultar hasta que haya bonuses
            Debug.Log("OpenWindowButton configurado y ocultado inicialmente");
        }
        else
        {
            Debug.LogError("OpenWindowButton no está asignado en el inspector");
        }

        // Verificar DiceManager
        if (diceManager != null)
        {
            Debug.Log("DiceManager encontrado y configurado");
        }
        else
        {
            Debug.LogWarning("DiceManager no encontrado");
        }

        Debug.Log("BonusManager inicializado correctamente");
        Debug.Log("===================================");
    }

    /// <summary>
    /// NUEVO: Inicializa el sistema de navegación
    /// </summary>
    private void InitializeNavigationSystem()
    {
        if (!enableNavigationIntegration)
        {
            Debug.Log("Integración de navegación deshabilitada");
            return;
        }

        // Buscar BonusNavigationExtension si no está asignado
        if (navigationExtension == null)
        {
            navigationExtension = FindAnyObjectByType<BonusNavigationExtension>();
            if (navigationExtension == null)
            {
                Debug.LogWarning("BonusNavigationExtension no encontrado - Los bonuses no serán navegables con gamepad");
            }
        }

        // Buscar UINavigationManager si no está asignado
        if (uiNavigationManager == null)
        {
            uiNavigationManager = FindAnyObjectByType<UINavigationManager>();
            if (uiNavigationManager == null)
            {
                Debug.LogWarning("UINavigationManager no encontrado");
            }
        }

        // Configurar el parent en el NavigationExtension
        if (navigationExtension != null && bonusesContent != null)
        {
            navigationExtension.SetBonusesParent(bonusesContent);
            Debug.Log("BonusNavigationExtension configurado con parent de bonuses");
        }
    }

    #region Gestión de Bonuses

    /// <summary>
    /// Añade un nuevo bonus al sistema
    /// ACTUALIZADO: Incluye notificación al sistema de navegación
    /// </summary>
    public void AddBonus(string bonusName, int bonusValue, string description = "", Sprite icon = null)
    {
        Debug.Log($"=== AÑADIENDO BONUS ===");
        Debug.Log($"Nombre: {bonusName}");
        Debug.Log($"Valor: +{bonusValue}");
        Debug.Log($"Descripción: {description}");

        // Verificar que el BonusManager está correctamente inicializado
        if (bonusesContent == null)
        {
            Debug.LogError("BonusesContent es null - No se puede añadir bonus. Verificar referencias en inspector.");
            return;
        }

        if (bonusPrefab == null)
        {
            Debug.LogError("BonusPrefab es null - No se puede crear interfaz. Verificar referencias en inspector.");
            return;
        }

        // Crear el objeto de bonus
        CollectedBonus newBonus = new CollectedBonus(bonusName, bonusValue, description, icon);

        // Añadir a la lista
        collectedBonuses.Add(newBonus);
        Debug.Log($"Bonus añadido a la lista. Total bonuses: {collectedBonuses.Count}");

        // Crear la interfaz del bonus
        bool uiCreated = CreateBonusUI(newBonus);

        if (uiCreated)
        {
            // Actualizar visibilidad de la ventana
            UpdateWindowVisibility();

            // NUEVO: Notificar al sistema de navegación si está habilitado
            if (enableNavigationIntegration && navigationExtension != null)
            {
                // La notificación se maneja automáticamente desde BonusUI.Initialize()
                // pero podemos forzar una actualización aquí también
                navigationExtension.ForceRefreshNavigation();
            }

            Debug.Log("Bonus añadido exitosamente y visibilidad actualizada");
        }
        else
        {
            Debug.LogError("Error al crear la interfaz del bonus");
            // Remover de la lista si no se pudo crear la UI
            collectedBonuses.Remove(newBonus);
        }

        Debug.Log("===================");
    }

    /// <summary>
    /// Activa un bonus para la próxima tirada
    /// </summary>
    public bool ActivateBonus(CollectedBonus bonus)
    {
        if (!canActivateBonuses)
        {
            Debug.LogWarning("No se pueden activar bonuses en este momento");
            return false;
        }

        if (activeBonus != null)
        {
            Debug.LogWarning("Ya hay un bonus activo. Desactivando el anterior.");
            DeactivateCurrentBonus();
        }

        activeBonus = bonus;

        // Actualizar la interfaz de todos los bonuses
        UpdateAllBonusesUI();

        // Notificar al sistema de dados
        if (diceManager != null)
        {
            // Aquí integraremos con el Dice_Manager mejorado
            NotifyDiceManagerBonusActivated(bonus);
        }

        Debug.Log($"Bonus activado: {bonus.bonusName} (+{bonus.bonusValue})");
        return true;
    }

    /// <summary>
    /// Desactiva el bonus actualmente activo
    /// </summary>
    public void DeactivateCurrentBonus()
    {
        if (activeBonus != null)
        {
            Debug.Log($"Desactivando bonus: {activeBonus.bonusName}");
            activeBonus = null;

            // Actualizar interfaz de todos los bonuses
            UpdateAllBonusesUI();

            // Notificar al sistema de dados
            if (diceManager != null)
            {
                NotifyDiceManagerBonusDeactivated();
            }
        }
    }

    /// <summary>
    /// Consume el bonus activo (llamado después de usar el bonus en una tirada)
    /// ACTUALIZADO: Incluye notificación al sistema de navegación
    /// </summary>
    public void ConsumeActiveBonus()
    {
        if (activeBonus != null)
        {
            Debug.Log($"Consumiendo bonus: {activeBonus.bonusName}");

            // NUEVO: Obtener el botón antes de destruir para notificar al sistema de navegación
            Button bonusButton = null;
            if (activeBonus.bonusUIScript != null)
            {
                bonusButton = activeBonus.bonusUIScript.GetButton();
            }

            // Remover de la lista
            collectedBonuses.Remove(activeBonus);

            // Destruir la interfaz
            if (activeBonus.uiInstance != null)
            {
                Destroy(activeBonus.uiInstance);
            }

            // NUEVO: Notificar al sistema de navegación sobre la remoción
            if (enableNavigationIntegration && navigationExtension != null && bonusButton != null)
            {
                navigationExtension.OnBonusRemoved(bonusButton);
            }

            activeBonus = null;

            // Actualizar visibilidad de la ventana
            UpdateWindowVisibility();

            Debug.Log($"Bonus consumido. Bonuses restantes: {collectedBonuses.Count}");
        }
    }

    #endregion

    #region Gestión de Interfaz

    private bool CreateBonusUI(CollectedBonus bonus)
    {
        if (bonusPrefab == null || bonusesContent == null)
        {
            Debug.LogError("Faltan referencias para crear la interfaz del bonus");
            return false;
        }

        try
        {
            Debug.Log($"Creando interfaz para bonus: {bonus.bonusName}");

            // Instanciar el prefab
            GameObject bonusInstance = Instantiate(bonusPrefab, bonusesContent);
            bonus.uiInstance = bonusInstance;

            // IMPORTANTE: Asegurar que la escala sea correcta
            bonusInstance.transform.localScale = Vector3.one;
            Debug.Log($"Escala del prefab establecida a: {bonusInstance.transform.localScale}");

            // Configurar el script del bonus
            BonusUI bonusUI = bonusInstance.GetComponent<BonusUI>();
            if (bonusUI == null)
            {
                Debug.Log("BonusUI no encontrado en prefab, añadiendo componente...");
                bonusUI = bonusInstance.AddComponent<BonusUI>();
            }

            bonusUI.Initialize(bonus, this);
            bonus.bonusUIScript = bonusUI;

            // NUEVO: La notificación al sistema de navegación se maneja automáticamente
            // desde BonusUI.Initialize() -> NotifyNavigationSystemAboutNewElement()

            Debug.Log($"Interfaz creada exitosamente para bonus: {bonus.bonusName}");
            Debug.Log($"Posición final del prefab: {bonusInstance.transform.position}");
            Debug.Log($"Escala final del prefab: {bonusInstance.transform.localScale}");

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al crear interfaz del bonus: {e.Message}");
            return false;
        }
    }

    private void UpdateAllBonusesUI()
    {
        foreach (var bonus in collectedBonuses)
        {
            if (bonus.bonusUIScript != null)
            {
                bonus.bonusUIScript.UpdateVisualState(bonus == activeBonus);
            }
        }
    }

    private void UpdateWindowVisibility()
    {
        bool shouldShowElements = collectedBonuses.Count > 0;

        Debug.Log($"UpdateWindowVisibility: shouldShowElements = {shouldShowElements}, bonusCount = {collectedBonuses.Count}");

        // Activar/desactivar el botón de toggle
        if (openWindowButton != null)
        {
            openWindowButton.gameObject.SetActive(shouldShowElements);
            Debug.Log($"OpenWindowButton activado: {shouldShowElements}");
        }
        else
        {
            Debug.LogWarning("OpenWindowButton es null - verificar referencia en el inspector");
        }

        // Activar la ventana padre cuando hay bonuses
        if (bonusWindow != null)
        {
            if (shouldShowElements && !bonusWindow.activeInHierarchy)
            {
                bonusWindow.SetActive(true);
                // El animator comenzará en estado cerrado por defecto
                Debug.Log("BonusWindow activada - Panel comenzará cerrado");
            }
            else if (!shouldShowElements && bonusWindow.activeInHierarchy)
            {
                bonusWindow.SetActive(false);
                isWindowOpen = false; // Resetear estado
                Debug.Log("BonusWindow desactivada");

                // NUEVO: Notificar al sistema de navegación que la ventana se cerró
                if (enableNavigationIntegration && navigationExtension != null)
                {
                    navigationExtension.OnBonusWindowClosed();
                }
            }
        }
        else
        {
            Debug.LogWarning("BonusWindow es null - verificar referencia en el inspector");
        }
    }

    private void ToggleBonusWindow()
    {
        if (isWindowOpen)
        {
            HideBonusWindow();
        }
        else
        {
            ShowBonusWindow();
        }
    }

    private void ShowBonusWindow()
    {
        if (bonusAnimator == null)
        {
            Debug.LogError("BonusAnimator es null - no se puede reproducir animación");
            return;
        }

        Debug.Log("Abriendo ventana de bonuses - reproduciendo animación Open_Bonuses");

        StartCoroutine(WindowInteracted(1f));

        // Reproducir animación de apertura
        bonusAnimator.Play("Open_Bonuses");

        // NUEVO: Notificar al sistema de navegación que la ventana se abrió
        if (enableNavigationIntegration && navigationExtension != null)
        {
            navigationExtension.OnBonusWindowOpened();
        }
    }

    private void HideBonusWindow()
    {
        if (bonusAnimator == null)
        {
            Debug.LogError("BonusAnimator es null - no se puede reproducir animación");
            return;
        }

        Debug.Log("Cerrando ventana de bonuses - reproduciendo animación Close_Bonuses");

        isWindowOpen = false;

        // Reproducir animación de cierre
        bonusAnimator.Play("Close_Bonuses");

        // NUEVO: Notificar al sistema de navegación que la ventana se cerró
        if (enableNavigationIntegration && navigationExtension != null)
        {
            navigationExtension.OnBonusWindowClosed();
        }
    }

    #endregion

    #region Integración con Sistema de Dados

    private void NotifyDiceManagerBonusActivated(CollectedBonus bonus)
    {
        // Esta función se integrará con el Dice_Manager mejorado
        Debug.Log($"Notificando al Dice_Manager: Bonus {bonus.bonusName} activado");

        // Por ahora, usar el sistema existente como fallback
        if (diceManager != null)
        {
            // Resetear todos los bonuses existentes
            diceManager.bonus1Activated = false;
            diceManager.bonus2Activated = false;
            diceManager.bonus3Activated = false;

            // Activar el bonus apropiado basado en el valor (temporal)
            if (bonus.bonusValue <= 2)
                diceManager.bonus1Activated = true;
            else if (bonus.bonusValue <= 3)
                diceManager.bonus2Activated = true;
            else
                diceManager.bonus3Activated = true;
        }
    }

    private void NotifyDiceManagerBonusDeactivated()
    {
        Debug.Log("Notificando al Dice_Manager: Bonus desactivado");

        if (diceManager != null)
        {
            diceManager.bonus1Activated = false;
            diceManager.bonus2Activated = false;
            diceManager.bonus3Activated = false;
        }
    }

    /// <summary>
    /// Método llamado por el Dice_Manager cuando inicia una tirada
    /// </summary>
    public void OnDiceRollStarted()
    {
        Debug.Log("Tirada de dado iniciada - Bloqueando activación de bonuses");
        canActivateBonuses = false;
    }

    /// <summary>
    /// Método llamado por el Dice_Manager cuando termina una tirada
    /// </summary>
    public void OnDiceRollCompleted()
    {
        Debug.Log("Tirada de dado completada - Permitiendo activación de bonuses");

        // Si había un bonus activo, consumirlo
        if (activeBonus != null)
        {
            ConsumeActiveBonus();
        }

        canActivateBonuses = true;
    }

    #endregion

    #region NUEVO: Métodos de Navegación

    /// <summary>
    /// Habilita o deshabilita la integración con el sistema de navegación
    /// </summary>
    public void SetNavigationIntegrationEnabled(bool enabled)
    {
        enableNavigationIntegration = enabled;

        if (navigationExtension != null)
        {
            navigationExtension.SetAutomaticNavigation(enabled);
        }

        Debug.Log($"Integración de navegación {(enabled ? "habilitada" : "deshabilitada")}");
    }

    /// <summary>
    /// Fuerza la actualización del sistema de navegación
    /// </summary>
    public void RefreshNavigation()
    {
        if (enableNavigationIntegration && navigationExtension != null)
        {
            navigationExtension.ForceRefreshNavigation();
        }
    }

    /// <summary>
    /// Selecciona automáticamente el primer bonus disponible
    /// </summary>
    public void SelectFirstBonus()
    {
        if (enableNavigationIntegration && navigationExtension != null)
        {
            navigationExtension.SelectFirstBonus();
        }
    }

    /// <summary>
    /// Obtiene el número de bonuses navegables actualmente
    /// </summary>
    public int GetNavigableBonusCount()
    {
        if (enableNavigationIntegration && navigationExtension != null)
        {
            return navigationExtension.GetActiveInteractableBonusCount();
        }
        return 0;
    }

    /// <summary>
    /// Verifica si hay bonuses disponibles para navegación
    /// </summary>
    public bool HasNavigableBonuses()
    {
        return GetNavigableBonusCount() > 0;
    }

    #endregion

    #region Métodos Públicos de Consulta

    public bool HasActiveBBonus() => activeBonus != null;

    public int GetActiveBonusValue() => activeBonus?.bonusValue ?? 0;

    public string GetActiveBonusName() => activeBonus?.bonusName ?? "";

    public int GetCollectedBonusCount() => collectedBonuses.Count;

    public bool CanActivateBonuses() => canActivateBonuses;

    /// <summary>
    /// NUEVO: Obtiene una lista de todos los bonuses disponibles
    /// </summary>
    public List<CollectedBonus> GetAllBonuses() => new List<CollectedBonus>(collectedBonuses);

    /// <summary>
    /// NUEVO: Busca un bonus por nombre
    /// </summary>
    public CollectedBonus FindBonusByName(string bonusName)
    {
        return collectedBonuses.Find(bonus => bonus.bonusName.Equals(bonusName, System.StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Métodos de Debug

    /// <summary>
    /// Método para debug - mostrar estado actual
    /// ACTUALIZADO: Incluye información de navegación
    /// </summary>
    [ContextMenu("Debug Bonus State")]
    public void DebugBonusState()
    {
        Debug.Log("=== ESTADO DEL SISTEMA DE BONUSES ===");
        Debug.Log($"Bonuses recolectados: {collectedBonuses.Count}");
        Debug.Log($"Bonus activo: {(activeBonus != null ? $"{activeBonus.bonusName} (+{activeBonus.bonusValue})" : "NINGUNO")}");
        Debug.Log($"Puede activar bonuses: {canActivateBonuses}");
        Debug.Log($"Ventana abierta: {isWindowOpen}");

        // NUEVO: Información de navegación
        Debug.Log("--- NAVEGACIÓN ---");
        Debug.Log($"Integración de navegación habilitada: {enableNavigationIntegration}");
        Debug.Log($"NavigationExtension disponible: {navigationExtension != null}");
        Debug.Log($"UINavigationManager disponible: {uiNavigationManager != null}");
        if (enableNavigationIntegration && navigationExtension != null)
        {
            Debug.Log($"Bonuses navegables: {navigationExtension.GetActiveInteractableBonusCount()}");
            Debug.Log($"NavigationExtension gestionando: {navigationExtension.ManagedBonusCount} botones");
        }

        // Debug de referencias
        Debug.Log("--- REFERENCIAS ---");
        Debug.Log($"BonusWindow: {(bonusWindow != null ? bonusWindow.name : "NULL")}");
        Debug.Log($"BonusWindow Active: {(bonusWindow != null ? bonusWindow.activeInHierarchy.ToString() : "N/A")}");
        Debug.Log($"BonusAnimator: {(bonusAnimator != null ? bonusAnimator.name : "NULL")}");
        Debug.Log($"BonusAnimator Enabled: {(bonusAnimator != null ? bonusAnimator.enabled.ToString() : "N/A")}");
        Debug.Log($"OpenWindowButton: {(openWindowButton != null ? openWindowButton.name : "NULL")}");
        Debug.Log($"OpenWindowButton Active: {(openWindowButton != null ? openWindowButton.gameObject.activeInHierarchy.ToString() : "N/A")}");
        Debug.Log($"BonusesContent: {(bonusesContent != null ? bonusesContent.name : "NULL")}");
        Debug.Log($"BonusPrefab: {(bonusPrefab != null ? bonusPrefab.name : "NULL")}");

        // Debug de bonuses individuales
        if (collectedBonuses.Count > 0)
        {
            Debug.Log("--- BONUSES INDIVIDUALES ---");
            for (int i = 0; i < collectedBonuses.Count; i++)
            {
                var bonus = collectedBonuses[i];
                string buttonInfo = "NULL";
                if (bonus.bonusUIScript != null)
                {
                    Button button = bonus.bonusUIScript.GetButton();
                    buttonInfo = button != null ? $"Button: {button.name}, Interactable: {button.interactable}" : "NULL";
                }
                Debug.Log($"[{i}] {bonus.bonusName} (+{bonus.bonusValue}) - UI: {(bonus.uiInstance != null ? "Created" : "NULL")} - {buttonInfo}");
            }
        }

        Debug.Log("================================");
    }

    /// <summary>
    /// NUEVO: Debug específico del sistema de navegación
    /// </summary>
    [ContextMenu("Debug Navigation State")]
    public void DebugNavigationState()
    {
        if (enableNavigationIntegration && navigationExtension != null)
        {
            Debug.Log(navigationExtension.GetDetailedStatus());
        }
        else
        {
            Debug.Log("Sistema de navegación no disponible o deshabilitado");
        }
    }

    /// <summary>
    /// Fuerza la actualización de la visibilidad - útil para debugging
    /// </summary>
    [ContextMenu("Force Update Visibility")]
    public void ForceUpdateVisibility()
    {
        Debug.Log("Forzando actualización de visibilidad...");
        UpdateWindowVisibility();
    }

    /// <summary>
    /// Añade un bonus de prueba para testing
    /// </summary>
    [ContextMenu("Add Test Bonus")]
    public void AddTestBonus()
    {
        if (Application.isPlaying)
        {
            AddBonus("Bonus de Prueba", 3, "Un bonus para testing");
        }
        else
        {
            Debug.Log("Este método solo funciona en Play Mode");
        }
    }

    /// <summary>
    /// NUEVO: Añade múltiples bonuses de prueba para testing de navegación
    /// </summary>
    [ContextMenu("Add Multiple Test Bonuses")]
    public void AddMultipleTestBonuses()
    {
        if (Application.isPlaying)
        {
            AddBonus("Bonus Pequeño", 1, "Bonus de prueba pequeño");
            AddBonus("Bonus Mediano", 2, "Bonus de prueba mediano");
            AddBonus("Bonus Grande", 4, "Bonus de prueba grande");
            Debug.Log("Añadidos 3 bonuses de prueba para testing de navegación");
        }
        else
        {
            Debug.Log("Este método solo funciona en Play Mode");
        }
    }

    /// <summary>
    /// NUEVO: Test de navegación - selecciona el primer bonus
    /// </summary>
    [ContextMenu("Test Select First Bonus")]
    public void TestSelectFirstBonus()
    {
        if (Application.isPlaying)
        {
            SelectFirstBonus();
        }
        else
        {
            Debug.Log("Este método solo funciona en Play Mode");
        }
    }

    /// <summary>
    /// NUEVO: Test de navegación - refresca el sistema
    /// </summary>
    [ContextMenu("Test Refresh Navigation")]
    public void TestRefreshNavigation()
    {
        if (Application.isPlaying)
        {
            RefreshNavigation();
        }
        else
        {
            Debug.Log("Este método solo funciona en Play Mode");
        }
    }

    /// <summary>
    /// NUEVO: Toggle del sistema de navegación para testing
    /// </summary>
    [ContextMenu("Toggle Navigation Integration")]
    public void ToggleNavigationIntegration()
    {
        if (Application.isPlaying)
        {
            SetNavigationIntegrationEnabled(!enableNavigationIntegration);
            Debug.Log($"Integración de navegación ahora: {(enableNavigationIntegration ? "HABILITADA" : "DESHABILITADA")}");
        }
        else
        {
            Debug.Log("Este método solo funciona en Play Mode");
        }
    }

    private IEnumerator WindowInteracted(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        isWindowOpen = true;
    }

    #endregion

    private void OnDestroy()
    {
        // Ya no necesitamos limpiar tweens de DOTween para el deslizamiento
        // Las animaciones de Unity se limpian automáticamente
        Debug.Log("BonusManager destruido");
    }
}