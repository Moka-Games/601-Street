using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Gestor principal del sistema de bonuses para el dado
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

        Debug.Log("=== BONUS MANAGER INITIALIZATION ===");
        Debug.Log($"BonusWindow assigned: {bonusWindow != null}");
        Debug.Log($"BonusAnimator assigned: {bonusAnimator != null}");
        Debug.Log($"OpenWindowButton assigned: {openWindowButton != null}");
        Debug.Log($"BonusesContent assigned: {bonusesContent != null}");
        Debug.Log($"BonusPrefab assigned: {bonusPrefab != null}");

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

    #region Gestión de Bonuses

    /// <summary>
    /// Añade un nuevo bonus al sistema
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
    /// </summary>
    public void ConsumeActiveBonus()
    {
        if (activeBonus != null)
        {
            Debug.Log($"Consumiendo bonus: {activeBonus.bonusName}");

            // Remover de la lista
            collectedBonuses.Remove(activeBonus);

            // Destruir la interfaz
            if (activeBonus.uiInstance != null)
            {
                Destroy(activeBonus.uiInstance);
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
        //isWindowOpen = true;

        // Reproducir animación de apertura
        bonusAnimator.Play("Open_Bonuses");
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

    #region Métodos Públicos de Consulta

    public bool HasActiveBBonus() => activeBonus != null;

    public int GetActiveBonusValue() => activeBonus?.bonusValue ?? 0;

    public string GetActiveBonusName() => activeBonus?.bonusName ?? "";

    public int GetCollectedBonusCount() => collectedBonuses.Count;

    public bool CanActivateBonuses() => canActivateBonuses;

    /// <summary>
    /// Método para debug - mostrar estado actual
    /// </summary>
    [ContextMenu("Debug Bonus State")]
    public void DebugBonusState()
    {
        Debug.Log("=== ESTADO DEL SISTEMA DE BONUSES ===");
        Debug.Log($"Bonuses recolectados: {collectedBonuses.Count}");
        Debug.Log($"Bonus activo: {(activeBonus != null ? $"{activeBonus.bonusName} (+{activeBonus.bonusValue})" : "NINGUNO")}");
        Debug.Log($"Puede activar bonuses: {canActivateBonuses}");
        Debug.Log($"Ventana abierta: {isWindowOpen}");

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
                Debug.Log($"[{i}] {bonus.bonusName} (+{bonus.bonusValue}) - UI: {(bonus.uiInstance != null ? "Created" : "NULL")}");
            }
        }

        Debug.Log("================================");
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

    private IEnumerator WindowInteracted(float delay)
    {
        yield return new WaitForSeconds(delay);
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