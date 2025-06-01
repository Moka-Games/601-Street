using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Gestor principal del sistema de bonuses para el dado - VERSIÓN ACTUALIZADA
/// Integrado con el nuevo sistema de feedback visual
/// </summary>
public class BonusManager : MonoBehaviour
{
    public static BonusManager Instance { get; private set; }

    [Header("Referencias de la Interfaz")]
    [SerializeField] private GameObject bonusWindow;
    [SerializeField] private Button openWindowButton;
    [SerializeField] private Animator bonusAnimator;
    [SerializeField] private Transform bonusesContent;
    [SerializeField] private GameObject bonusPrefab;

    [Header("Configuración de Animaciones")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    [Header("Referencias al Sistema de Dados")]
    [SerializeField] private Dice_Manager diceManager;
    [SerializeField] private BonusFeedbackManager feedbackManager;

    [Header("Feedback Visual del Bonus Activo")]
    [SerializeField] private GameObject activeBonusIndicator; // Indicador visual de bonus activo
    [SerializeField] private TMP_Text activeBonusValueText;   // Texto que muestra el valor del bonus activo
    [SerializeField] private Color activeBonusColor = Color.green;

    // Estado del sistema
    private List<CollectedBonus> collectedBonuses = new List<CollectedBonus>();
    private CollectedBonus activeBonus = null;
    private bool canActivateBonuses = true;
    private bool isWindowOpen = false;

    [System.Serializable]
    public class CollectedBonus
    {
        public string bonusName;
        public int bonusValue;
        public string description;
        public Sprite icon;
        public GameObject uiInstance;
        public BonusUI bonusUIScript;

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
        InitializeBonusManager();
    }

    private void InitializeBonusManager()
    {
        // Buscar referencias automáticamente
        if (diceManager == null)
            diceManager = FindAnyObjectByType<Dice_Manager>();

        if (feedbackManager == null)
            feedbackManager = FindAnyObjectByType<BonusFeedbackManager>();

        Debug.Log("=== BONUS MANAGER INITIALIZATION ===");
        Debug.Log($"BonusWindow assigned: {bonusWindow != null}");
        Debug.Log($"BonusAnimator assigned: {bonusAnimator != null}");
        Debug.Log($"OpenWindowButton assigned: {openWindowButton != null}");
        Debug.Log($"BonusesContent assigned: {bonusesContent != null}");
        Debug.Log($"BonusPrefab assigned: {bonusPrefab != null}");
        Debug.Log($"DiceManager found: {diceManager != null}");
        Debug.Log($"FeedbackManager found: {feedbackManager != null}");

        // Configurar ventana inicial
        if (bonusWindow != null)
        {
            bonusWindow.SetActive(false);
        }

        // Configurar botón de apertura
        if (openWindowButton != null)
        {
            openWindowButton.onClick.AddListener(ToggleBonusWindow);
            openWindowButton.gameObject.SetActive(false);
        }

        // Configurar indicador de bonus activo
        SetupActiveBonusIndicator();

        Debug.Log("BonusManager inicializado con sistema de feedback mejorado");
        Debug.Log("===================================");
    }

    private void SetupActiveBonusIndicator()
    {
        if (activeBonusIndicator != null)
        {
            activeBonusIndicator.SetActive(false);
        }

        if (activeBonusValueText != null)
        {
            activeBonusValueText.color = activeBonusColor;
            activeBonusValueText.text = "";
        }
    }

    #region Gestión de Bonuses

    /// <summary>
    /// Añade un nuevo bonus al sistema con feedback mejorado
    /// </summary>
    public void AddBonus(string bonusName, int bonusValue, string description = "", Sprite icon = null)
    {
        Debug.Log($"=== AÑADIENDO BONUS ===");
        Debug.Log($"Nombre: {bonusName}");
        Debug.Log($"Valor: +{bonusValue}");
        Debug.Log($"Descripción: {description}");

        if (bonusesContent == null || bonusPrefab == null)
        {
            Debug.LogError("Referencias faltantes para crear bonus UI");
            return;
        }

        // Crear el objeto de bonus
        CollectedBonus newBonus = new CollectedBonus(bonusName, bonusValue, description, icon);
        collectedBonuses.Add(newBonus);

        // Crear la interfaz del bonus
        bool uiCreated = CreateBonusUI(newBonus);

        if (uiCreated)
        {
            UpdateWindowVisibility();

            // Mostrar feedback rápido del nuevo bonus
            if (feedbackManager != null)
            {
                feedbackManager.ShowBonusValueQuick(bonusValue);
            }

            Debug.Log("Bonus añadido exitosamente con feedback visual");
        }
        else
        {
            collectedBonuses.Remove(newBonus);
            Debug.LogError("Error al crear la interfaz del bonus");
        }

        Debug.Log("===================");
    }

    /// <summary>
    /// Activa un bonus con feedback visual mejorado
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
            Debug.LogWarning("Desactivando bonus anterior");
            DeactivateCurrentBonus();
        }

        activeBonus = bonus;

        // Actualizar interfaz de todos los bonuses
        UpdateAllBonusesUI();

        // Mostrar indicador de bonus activo
        ShowActiveBonusIndicator();

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

            // Actualizar interfaz
            UpdateAllBonusesUI();
            HideActiveBonusIndicator();

            // Notificar al sistema de dados
            NotifyDiceManagerBonusDeactivated();
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

            // Animación de consumo antes de eliminar
            if (activeBonus.uiInstance != null)
            {
                StartCoroutine(AnimateBonusConsumption(activeBonus));
            }
            else
            {
                // Si no hay UI, eliminar directamente
                FinalizeBonusConsumption();
            }
        }
    }

    /// <summary>
    /// Anima el consumo del bonus antes de eliminarlo
    /// </summary>
    private IEnumerator AnimateBonusConsumption(CollectedBonus bonusToConsume)
    {
        if (bonusToConsume.uiInstance != null)
        {
            // IMPORTANTE: Notificar al sistema de navegación ANTES de la animación
            NotifyNavigationSystemBonusDestroyed(bonusToConsume.uiInstance);

            // Animación de "consumido"
            Transform bonusTransform = bonusToConsume.uiInstance.transform;

            // Fade out y scale down
            CanvasGroup canvasGroup = bonusToConsume.uiInstance.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = bonusToConsume.uiInstance.AddComponent<CanvasGroup>();
            }

            Sequence consumeSequence = DOTween.Sequence();
            consumeSequence.Append(bonusTransform.DOScale(1.2f, 0.2f).SetEase(Ease.OutQuart));
            consumeSequence.Append(canvasGroup.DOFade(0f, 0.3f));
            consumeSequence.Join(bonusTransform.DOScale(0f, 0.3f).SetEase(Ease.InBack));

            yield return consumeSequence.WaitForCompletion();

            Destroy(bonusToConsume.uiInstance);
        }

        FinalizeBonusConsumption();
    }

    /// <summary>
    /// Finaliza el proceso de consumo del bonus
    /// </summary>
    private void FinalizeBonusConsumption()
    {
        if (activeBonus != null)
        {
            collectedBonuses.Remove(activeBonus);
            activeBonus = null;

            HideActiveBonusIndicator();
            UpdateWindowVisibility();

            Debug.Log($"Bonus consumido. Bonuses restantes: {collectedBonuses.Count}");
        }
    }

    #endregion

    #region Indicador de Bonus Activo

    /// <summary>
    /// Muestra el indicador de bonus activo
    /// </summary>
    private void ShowActiveBonusIndicator()
    {
        if (activeBonus == null) return;

        if (activeBonusIndicator != null)
        {
            activeBonusIndicator.SetActive(true);

            // Animación de aparición
            activeBonusIndicator.transform.localScale = Vector3.zero;
            //activeBonusIndicator.transform.DOScale(400f, 380).SetEase(Ease.OutBack);
            activeBonusIndicator.transform.localScale = new Vector3(400, 400, 400);
        }

        if (activeBonusValueText != null)
        {
            activeBonusValueText.text = $"+{activeBonus.bonusValue}";

            // Efecto de pulso sutil
            //activeBonusValueText.transform.DOPunchScale(Vector3.one * 0.1f, 0.4f, 4, 0.5f);
        }

        Debug.Log($"Indicador de bonus activo mostrado: +{activeBonus.bonusValue}");
    }

    /// <summary>
    /// Oculta el indicador de bonus activo
    /// </summary>
    private void HideActiveBonusIndicator()
    {
        if (activeBonusIndicator != null && activeBonusIndicator.activeInHierarchy)
        {
            activeBonusIndicator.transform.DOScale(0f, 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() => activeBonusIndicator.SetActive(false));
        }

        if (activeBonusValueText != null)
        {
            activeBonusValueText.text = "";
        }

        Debug.Log("Indicador de bonus activo ocultado");
    }

    #endregion

    #region Gestión de Interfaz (Métodos existentes actualizados)

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

            GameObject bonusInstance = Instantiate(bonusPrefab, bonusesContent);
            bonus.uiInstance = bonusInstance;
            bonusInstance.transform.localScale = Vector3.one;

            BonusUI bonusUI = bonusInstance.GetComponent<BonusUI>();
            if (bonusUI == null)
            {
                bonusUI = bonusInstance.AddComponent<BonusUI>();
            }

            bonusUI.Initialize(bonus, this);
            bonus.bonusUIScript = bonusUI;

            // CRÍTICO: Notificar al sistema de navegación
            NotifyNavigationSystemBonusCreated(bonusInstance);

            Debug.Log($"Interfaz creada exitosamente para bonus: {bonus.bonusName}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al crear interfaz del bonus: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Notifica al sistema de navegación cuando se crea un nuevo bonus
    /// </summary>
    private void NotifyNavigationSystemBonusCreated(GameObject bonusUI)
    {
        // Buscar BonusNavigationExtension en la escena
        BonusNavigationExtension navigationExtension = FindAnyObjectByType<BonusNavigationExtension>();
        if (navigationExtension != null)
        {
            navigationExtension.NotifyBonusCreated(bonusUI);
            Debug.Log($"Sistema de navegación notificado: bonus creado {bonusUI.name}");
        }
        else
        {
            Debug.LogWarning("BonusNavigationExtension no encontrado - Los bonuses no serán navegables");
        }
    }

    /// <summary>
    /// Notifica al sistema de navegación cuando se destruye un bonus
    /// </summary>
    private void NotifyNavigationSystemBonusDestroyed(GameObject bonusUI)
    {
        if (bonusUI == null) return;

        BonusNavigationExtension navigationExtension = FindAnyObjectByType<BonusNavigationExtension>();
        if (navigationExtension != null)
        {
            navigationExtension.NotifyBonusDestroyed(bonusUI);
            Debug.Log($"Sistema de navegación notificado: bonus destruido {bonusUI.name}");
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

        if (openWindowButton != null)
        {
            openWindowButton.gameObject.SetActive(shouldShowElements);
        }

        if (bonusWindow != null)
        {
            if (shouldShowElements && !bonusWindow.activeInHierarchy)
            {
                bonusWindow.SetActive(true);
            }
            else if (!shouldShowElements && bonusWindow.activeInHierarchy)
            {
                bonusWindow.SetActive(false);
                isWindowOpen = false;
            }
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
        if (bonusAnimator == null) return;

        Debug.Log("Abriendo ventana de bonuses");
        StartCoroutine(WindowInteracted(1f));
        bonusAnimator.Play("Open_Bonuses");

        // CRÍTICO: Notificar al sistema de navegación que la ventana se abrió
        NotifyNavigationSystemWindowStateChanged(true);
    }

    private void HideBonusWindow()
    {
        if (bonusAnimator == null) return;

        Debug.Log("Cerrando ventana de bonuses");
        isWindowOpen = false;
        bonusAnimator.Play("Close_Bonuses");

        // CRÍTICO: Notificar al sistema de navegación que la ventana se cerró
        NotifyNavigationSystemWindowStateChanged(false);
    }

    /// <summary>
    /// Notifica al sistema de navegación sobre el cambio de estado de la ventana
    /// </summary>
    private void NotifyNavigationSystemWindowStateChanged(bool isOpen)
    {
        BonusNavigationExtension navigationExtension = FindAnyObjectByType<BonusNavigationExtension>();
        if (navigationExtension != null)
        {
            navigationExtension.OnBonusWindowStateChanged(isOpen);
            Debug.Log($"Sistema de navegación notificado: ventana {(isOpen ? "abierta" : "cerrada")}");
        }
    }

    private IEnumerator WindowInteracted(float delay)
    {
        yield return new WaitForSeconds(delay);
        isWindowOpen = true;
    }

    #endregion

    #region Integración con Sistema de Dados (Actualizada)

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

        // Actualizar UI para mostrar que no se pueden activar bonuses
        foreach (var bonus in collectedBonuses)
        {
            if (bonus.bonusUIScript != null)
            {
                bonus.bonusUIScript.SetInteractable(false);
            }
        }
    }

    /// <summary>
    /// Método llamado por el Dice_Manager cuando termina una tirada
    /// </summary>
    public void OnDiceRollCompleted()
    {
        Debug.Log("Tirada de dado completada");

        // Si había un bonus activo, consumirlo
        if (activeBonus != null)
        {
            ConsumeActiveBonus();
        }

        canActivateBonuses = true;

        // Reactivar UI de bonuses
        foreach (var bonus in collectedBonuses)
        {
            if (bonus.bonusUIScript != null)
            {
                bonus.bonusUIScript.SetInteractable(true);
            }
        }
    }

    #endregion

    #region Métodos Públicos de Consulta

    public bool HasActiveBBonus() => activeBonus != null;
    public int GetActiveBonusValue() => activeBonus?.bonusValue ?? 0;
    public string GetActiveBonusName() => activeBonus?.bonusName ?? "";
    public int GetCollectedBonusCount() => collectedBonuses.Count;
    public bool CanActivateBonuses() => canActivateBonuses;

    /// <summary>
    /// Obtiene información detallada del bonus activo para el sistema de feedback
    /// </summary>
    public (bool hasBonus, string name, int value) GetActiveBonusInfo()
    {
        if (activeBonus != null)
        {
            return (true, activeBonus.bonusName, activeBonus.bonusValue);
        }
        return (false, "", 0);
    }

    #endregion

    #region Métodos de Debug Actualizados

    [ContextMenu("Debug Bonus State")]
    public void DebugBonusState()
    {
        Debug.Log("=== ESTADO DEL SISTEMA DE BONUSES ===");
        Debug.Log($"Bonuses recolectados: {collectedBonuses.Count}");
        Debug.Log($"Bonus activo: {(activeBonus != null ? $"{activeBonus.bonusName} (+{activeBonus.bonusValue})" : "NINGUNO")}");
        Debug.Log($"Puede activar bonuses: {canActivateBonuses}");
        Debug.Log($"Ventana abierta: {isWindowOpen}");
        Debug.Log($"FeedbackManager disponible: {feedbackManager != null}");

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

    [ContextMenu("Add Test Bonus")]
    public void AddTestBonus()
    {
        if (Application.isPlaying)
        {
            AddBonus("Bonus de Prueba", 3, "Un bonus para testing");
        }
    }

    [ContextMenu("Test Bonus Feedback")]
    public void TestBonusFeedback()
    {
        if (Application.isPlaying && feedbackManager != null)
        {
            StartCoroutine(feedbackManager.ShowBonusApplicationSequence(12, 3, 15));
        }
    }

    #endregion

    private void OnDestroy()
    {
        // Limpiar animaciones DOTween
        DOTween.Kill(activeBonusIndicator?.transform);
        DOTween.Kill(activeBonusValueText?.transform);

        Debug.Log("BonusManager destruido");
    }
}