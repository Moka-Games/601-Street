using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// Gestor de navegación especializado para el inventario
/// Maneja elementos dinámicos, múltiples secciones y scrolls automáticos
/// </summary>
public class InventoryNavigationManager : MonoBehaviour
{
    [Header("Referencias del Inventario")]
    public Transform noteContainer;
    public Transform objectContainer;
    public ScrollRect noteScrollRect;
    public ScrollRect objectScrollRect;

    [Header("Configuración de Scroll")]
    [SerializeField] private bool enableScrollWithInput = true;
    [SerializeField] private float scrollSensitivity = 2f;
    [SerializeField] private float scrollDeadZone = 0.3f;
    [SerializeField] private bool invertScrollX = false;

    [Header("Configuración de Navegación")]
    [SerializeField] private float scrollAnimationDuration = 0.3f;
    [SerializeField] private bool autoDetectNewItems = true;
    [SerializeField] private float detectionInterval = 0.1f;

    [Header("Configuración Visual")]
    [SerializeField] private float selectedScale = 1.15f;
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private DG.Tweening.Ease animationEase = DG.Tweening.Ease.OutBack;

    [Header("Sonidos")]
    [SerializeField] private AudioClip navigationSound;
    [SerializeField] private AudioClip sectionChangeSound;
    [SerializeField] private AudioClip selectSound;

    // Sistema de Input
    private PlayerControls playerControls;
    private Vector2 navigationInput;
    private Vector2 scrollInput;
    private float lastNavigationTime;
    private float lastScrollTime;
    private float navigationDelay = 0.15f;
    private float scrollDelay = 0.05f;

    // Control de secciones
    public enum InventorySection { Notes, Objects }
    private InventorySection currentSection = InventorySection.Notes;

    // Listas de elementos por sección
    private List<Button> noteButtons = new List<Button>();
    private List<Button> objectButtons = new List<Button>();

    // Estado de navegación
    private int currentNoteIndex = 0;
    private int currentObjectIndex = 0;
    private Button currentSelectedButton;
    private Button previousSelectedButton;

    // Animaciones
    private Tween currentAnimationTween;
    private Tween currentScrollTween;

    // Componentes
    private AudioSource audioSource;
    private Inventory_Manager inventoryManager;

    // Control de detección automática
    private float lastDetectionTime;
    private int lastNoteCount = 0;
    private int lastObjectCount = 0;

    // Eventos
    public System.Action<Button> OnElementSelected;
    public System.Action<Button> OnElementSubmitted;
    public System.Action<InventorySection> OnSectionChanged;

    [Header("Configuración de Auto-Scroll")]
    [SerializeField] private bool enableAutoScrollOnNavigation = true;
    [SerializeField] private bool onlyScrollIfElementNotVisible = true;
    [SerializeField] private float visibilityMargin = 50f; // Margen para considerar un elemento "visible"

    // Variable para rastrear si el usuario ha hecho scroll manual recientemente
    private bool userHasScrolledManually = false;
    private float lastManualScrollTime = 0f;
    private float manualScrollGracePeriod = 2f;

    private void Awake()
    {
        // Inicializar componentes
        audioSource = GetComponent<AudioSource>();
        inventoryManager = GetComponent<Inventory_Manager>();

        if (inventoryManager == null)
        {
            inventoryManager = FindAnyObjectByType<Inventory_Manager>();
        }

        // Configurar controles
        playerControls = new PlayerControls();
        SetupInputActions();

        // Auto-detectar contenedores si no están asignados
        AutoDetectContainers();
    }

    private void AutoDetectContainers()
    {
        if (noteContainer == null && inventoryManager != null)
        {
            noteContainer = inventoryManager.noteContainer;
        }

        if (objectContainer == null && inventoryManager != null)
        {
            objectContainer = inventoryManager.objectContainer;
        }

        // Auto-detectar ScrollRects
        if (noteScrollRect == null && noteContainer != null)
        {
            noteScrollRect = noteContainer.GetComponentInParent<ScrollRect>();
        }

        if (objectScrollRect == null && objectContainer != null)
        {
            objectScrollRect = objectContainer.GetComponentInParent<ScrollRect>();
        }
    }

    private void SetupInputActions()
    {
        playerControls.UI.Navigate.performed += OnNavigate;
        playerControls.UI.Submit.performed += OnSubmit;
        playerControls.UI.Cancel.performed += OnCancel;

        // Configurar input de scroll
        if (enableScrollWithInput)
        {
            playerControls.UI.Scroll_Inventory.performed += OnScroll;
            playerControls.UI.Scroll_Inventory.canceled += OnScrollCanceled;
        }
    }

    private void OnEnable()
    {
        playerControls.UI.Enable();

        // Detectar elementos actuales y seleccionar el primero
        RefreshInventoryNavigation();

        // Iniciar detección automática si está habilitada
        if (autoDetectNewItems)
        {
            InvokeRepeating(nameof(CheckForNewItems), detectionInterval, detectionInterval);
        }
    }

    private void OnDisable()
    {
        playerControls.UI.Disable();
        CleanupAnimations();

        // Detener detección automática
        if (autoDetectNewItems)
        {
            CancelInvoke(nameof(CheckForNewItems));
        }
    }

    private void OnDestroy()
    {
        CleanupAnimations();

        // Limpiar eventos de scroll
        if (playerControls != null && enableScrollWithInput)
        {
            playerControls.UI.Scroll_Inventory.performed -= OnScroll;
            playerControls.UI.Scroll_Inventory.canceled -= OnScrollCanceled;
        }

        playerControls?.Dispose();
    }

    private void Update()
    {
        // Procesar navegación
        navigationInput = playerControls.UI.Navigate.ReadValue<Vector2>();

        if (Time.time - lastNavigationTime >= navigationDelay)
        {
            ProcessNavigation();
        }

        // ESTE MÉTODO NO EXISTE
        ProcessScrollInput(); // <-- Esta línea está causando errores
    }

    private void ProcessNavigation()
    {
        if (navigationInput.magnitude < 0.3f) return;

        // Navegación horizontal (cambio de sección o elemento)
        if (Mathf.Abs(navigationInput.x) > Mathf.Abs(navigationInput.y))
        {
            if (navigationInput.x > 0) // Derecha
            {
                NavigateRight();
            }
            else // Izquierda
            {
                NavigateLeft();
            }
        }
        // Navegación vertical (cambio de sección)
        else
        {
            if (navigationInput.y > 0) // Arriba
            {
                SwitchToSection(InventorySection.Notes);
            }
            else // Abajo
            {
                SwitchToSection(InventorySection.Objects);
            }
        }

        lastNavigationTime = Time.time;
    }

    private void NavigateRight()
    {
        List<Button> currentButtons = GetCurrentSectionButtons();
        if (currentButtons.Count == 0) return;

        int currentIndex = GetCurrentSectionIndex();
        int newIndex = (currentIndex + 1) % currentButtons.Count;

        SetCurrentSectionIndex(newIndex);
        SelectButton(currentButtons[newIndex]);
    }

    private void NavigateLeft()
    {
        List<Button> currentButtons = GetCurrentSectionButtons();
        if (currentButtons.Count == 0) return;

        int currentIndex = GetCurrentSectionIndex();
        int newIndex = (currentIndex - 1 + currentButtons.Count) % currentButtons.Count;

        SetCurrentSectionIndex(newIndex);
        SelectButton(currentButtons[newIndex]);
    }

    private void SwitchToSection(InventorySection newSection)
    {
        if (currentSection == newSection) return;

        currentSection = newSection;

        List<Button> newSectionButtons = GetCurrentSectionButtons();
        if (newSectionButtons.Count > 0)
        {
            int index = GetCurrentSectionIndex();
            SelectButton(newSectionButtons[index]);
            PlaySound(sectionChangeSound);
            OnSectionChanged?.Invoke(currentSection);
        }

        Debug.Log($"Cambiado a sección: {currentSection}");
    }

    private void SelectButton(Button button)
    {
        if (button == null || button == currentSelectedButton) return;

        previousSelectedButton = currentSelectedButton;
        currentSelectedButton = button;

        // Aplicar animaciones
        ApplySelectionAnimations();

        // Scroll automático
        ScrollToButton(button);

        // Efectos
        PlaySound(navigationSound);
        OnElementSelected?.Invoke(currentSelectedButton);

        Debug.Log($"Botón seleccionado: {button.name} en sección {currentSection}");
    }

    private void ApplySelectionAnimations()
    {
        // Limpiar animaciones anteriores
        currentAnimationTween?.Kill();

        // Resetear botón anterior
        if (previousSelectedButton != null)
        {
            previousSelectedButton.transform.DOKill();
            previousSelectedButton.transform.localScale = Vector3.one;
        }

        // Animar botón actual
        if (currentSelectedButton != null)
        {
            currentSelectedButton.transform.localScale = Vector3.one;
            currentAnimationTween = currentSelectedButton.transform
                .DOScale(Vector3.one * selectedScale, animationDuration)
                .SetEase(animationEase)
                .SetUpdate(true);
        }
    }

    private void ScrollToButton(Button button)
    {
        if (!enableAutoScrollOnNavigation) return;

        ScrollRect scrollRect = GetCurrentScrollRect();
        if (scrollRect == null || button == null) return;

        // Si el usuario ha hecho scroll manual recientemente, no hacer auto-scroll
        if (userHasScrolledManually && (Time.time - lastManualScrollTime) < manualScrollGracePeriod)
        {
            Debug.Log("Auto-scroll omitido: Usuario hizo scroll manual recientemente");
            return;
        }

        // Si solo queremos hacer scroll cuando el elemento no es visible, verificar visibilidad
        if (onlyScrollIfElementNotVisible)
        {
            if (IsButtonVisible(button, scrollRect, visibilityMargin))
            {
                Debug.Log($"Auto-scroll omitido: Elemento {button.name} ya está visible");
                return;
            }
            else
            {
                Debug.Log($"Auto-scroll activado: Elemento {button.name} no está completamente visible");
            }
        }

        // Proceder con el scroll automático
        RectTransform content = scrollRect.content;
        RectTransform buttonRect = button.GetComponent<RectTransform>();

        if (content == null || buttonRect == null) return;

        // Obtener posición local del botón
        Vector3 buttonPosition = content.InverseTransformPoint(buttonRect.position);

        // Calcular nueva posición del scroll
        float contentWidth = content.rect.width;
        float viewportWidth = scrollRect.GetComponent<RectTransform>().rect.width;

        if (contentWidth <= viewportWidth) return; // No necesita scroll

        // Calcular posición normalizada para centrar el elemento
        float normalizedPosition = Mathf.Clamp01(-buttonPosition.x / (contentWidth - viewportWidth));

        // Animar scroll
        currentScrollTween?.Kill();
        currentScrollTween = DOTween.To(
            () => scrollRect.horizontalNormalizedPosition,
            x => scrollRect.horizontalNormalizedPosition = x,
            normalizedPosition,
            scrollAnimationDuration
        ).SetEase(Ease.OutCubic).SetUpdate(true);

        Debug.Log($"Auto-scroll aplicado a elemento: {button.name}, Nueva posición: {normalizedPosition:F3}");
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        // La navegación se maneja en Update para mayor control
    }

    private void OnSubmit(InputAction.CallbackContext context)
    {
        if (currentSelectedButton != null)
        {
            currentSelectedButton.onClick.Invoke();
            PlaySound(selectSound);
            OnElementSubmitted?.Invoke(currentSelectedButton);
        }
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        // Cerrar inventario
        if (inventoryManager != null)
        {
            inventoryManager.ToggleInventory();
        }
    }

    private void OnScroll(InputAction.CallbackContext context)
    {
        if (!enableScrollWithInput)
        {
            Debug.Log("Scroll deshabilitado por configuración");
            return;
        }

        // Marcar que el usuario ha hecho scroll manual
        userHasScrolledManually = true;
        lastManualScrollTime = Time.time;

        // Debug: Verificar qué tipo de valor estamos recibiendo
        Debug.Log($"Scroll Manual Detectado - Tipo: {context.valueType}, Valor Raw: {context.ReadValueAsObject()}");

        // Capturar input de scroll
        if (context.valueType == typeof(Vector2))
        {
            scrollInput = context.ReadValue<Vector2>();
        }
        else if (context.valueType == typeof(float))
        {
            float value = context.ReadValue<float>();
            scrollInput = new Vector2(value, 0);
        }
    }
    private void OnScrollCanceled(InputAction.CallbackContext context)
    {
        scrollInput = Vector2.zero;
    }

    private void ProcessScrollInput()
    {
        if (scrollInput.magnitude < scrollDeadZone) return;

        ScrollRect currentScrollRect = GetCurrentScrollRect();
        if (currentScrollRect == null) return;

        // Calcular velocidad de scroll
        float scrollValue = scrollInput.x * scrollSensitivity * Time.deltaTime;

        // Invertir scroll si está configurado
        if (invertScrollX)
        {
            scrollValue = -scrollValue;
        }

        // Aplicar scroll horizontal
        float currentPos = currentScrollRect.horizontalNormalizedPosition;
        float newPos = Mathf.Clamp01(currentPos + scrollValue);

        currentScrollRect.horizontalNormalizedPosition = newPos;

        lastScrollTime = Time.time;

        Debug.Log($"Scroll aplicado: {scrollValue:F3}, Nueva posición: {newPos:F3}");
    }

    /// <summary>
    /// Detecta automáticamente nuevos elementos añadidos al inventario
    /// </summary>
    private void CheckForNewItems()
    {
        bool hasChanges = false;

        // Verificar cambios en notas
        int currentNoteCount = noteContainer != null ? noteContainer.childCount : 0;
        if (currentNoteCount != lastNoteCount)
        {
            RefreshSectionButtons(InventorySection.Notes);
            lastNoteCount = currentNoteCount;
            hasChanges = true;
        }

        // Verificar cambios en objetos
        int currentObjectCount = objectContainer != null ? objectContainer.childCount : 0;
        if (currentObjectCount != lastObjectCount)
        {
            RefreshSectionButtons(InventorySection.Objects);
            lastObjectCount = currentObjectCount;
            hasChanges = true;
        }

        // Si hay cambios y no hay elemento seleccionado, seleccionar el primero
        if (hasChanges && currentSelectedButton == null)
        {
            SelectFirstAvailableButton();
        }
    }

    /// <summary>
    /// Refresca la navegación completa del inventario
    /// </summary>
    public void RefreshInventoryNavigation()
    {
        RefreshSectionButtons(InventorySection.Notes);
        RefreshSectionButtons(InventorySection.Objects);
        SelectFirstAvailableButton();

        Debug.Log($"Navegación refrescada: {noteButtons.Count} notas, {objectButtons.Count} objetos");
    }

    private void RefreshSectionButtons(InventorySection section)
    {
        List<Button> buttonList = section == InventorySection.Notes ? noteButtons : objectButtons;
        Transform container = section == InventorySection.Notes ? noteContainer : objectContainer;

        buttonList.Clear();

        if (container == null) return;

        // Buscar todos los botones activos en el contenedor
        Button[] buttons = container.GetComponentsInChildren<Button>();

        foreach (Button button in buttons)
        {
            if (button.gameObject.activeInHierarchy && button.interactable)
            {
                buttonList.Add(button);
            }
        }

        Debug.Log($"Sección {section}: {buttonList.Count} botones encontrados");
    }

    private void SelectFirstAvailableButton()
    {
        // Intentar seleccionar en la sección actual primero
        List<Button> currentButtons = GetCurrentSectionButtons();

        if (currentButtons.Count > 0)
        {
            SetCurrentSectionIndex(0);
            SelectButton(currentButtons[0]);
            return;
        }

        // Si la sección actual está vacía, probar la otra
        InventorySection otherSection = currentSection == InventorySection.Notes ?
            InventorySection.Objects : InventorySection.Notes;

        List<Button> otherButtons = otherSection == InventorySection.Notes ? noteButtons : objectButtons;

        if (otherButtons.Count > 0)
        {
            SwitchToSection(otherSection);
            return;
        }

        // No hay elementos en ninguna sección
        currentSelectedButton = null;
        Debug.Log("No hay elementos navegables en el inventario");
    }

    // Métodos auxiliares
    private List<Button> GetCurrentSectionButtons()
    {
        return currentSection == InventorySection.Notes ? noteButtons : objectButtons;
    }

    private ScrollRect GetCurrentScrollRect()
    {
        return currentSection == InventorySection.Notes ? noteScrollRect : objectScrollRect;
    }

    private int GetCurrentSectionIndex()
    {
        return currentSection == InventorySection.Notes ? currentNoteIndex : currentObjectIndex;
    }

    private void SetCurrentSectionIndex(int index)
    {
        if (currentSection == InventorySection.Notes)
            currentNoteIndex = index;
        else
            currentObjectIndex = index;
    }

    private void CleanupAnimations()
    {
        currentAnimationTween?.Kill();
        currentScrollTween?.Kill();

        // Resetear todas las escalas
        foreach (var button in noteButtons)
        {
            if (button != null)
            {
                button.transform.DOKill();
                button.transform.localScale = Vector3.one;
            }
        }

        foreach (var button in objectButtons)
        {
            if (button != null)
            {
                button.transform.DOKill();
                button.transform.localScale = Vector3.one;
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Métodos públicos
    public void ForceRefresh()
    {
        RefreshInventoryNavigation();
    }

    public void SetNavigationEnabled(bool enabled)
    {
        this.enabled = enabled;

        if (!enabled)
        {
            CleanupAnimations();
        }
    }

    public Button GetCurrentSelectedButton()
    {
        return currentSelectedButton;
    }

    public InventorySection GetCurrentSection()
    {
        return currentSection;
    }

    // Métodos públicos para configurar scroll
    public void SetScrollEnabled(bool enabled)
    {
        enableScrollWithInput = enabled;

        if (playerControls != null)
        {
            if (enabled)
            {
                playerControls.UI.Scroll_Inventory.performed += OnScroll;
                playerControls.UI.Scroll_Inventory.canceled += OnScrollCanceled;
            }
            else
            {
                playerControls.UI.Scroll_Inventory.performed -= OnScroll;
                playerControls.UI.Scroll_Inventory.canceled -= OnScrollCanceled;
            }
        }
    }

    public void SetScrollSensitivity(float sensitivity)
    {
        scrollSensitivity = Mathf.Max(0.1f, sensitivity);
    }

    public void SetScrollDeadZone(float deadZone)
    {
        scrollDeadZone = Mathf.Clamp01(deadZone);
    }

    public void SetScrollInverted(bool inverted)
    {
        invertScrollX = inverted;
    }

    // Método para scroll manual (útil para UI externa)
    public void ManualScroll(float scrollAmount, InventorySection? targetSection = null)
    {
        InventorySection sectionToScroll = targetSection ?? currentSection;
        ScrollRect scrollRect = sectionToScroll == InventorySection.Notes ? noteScrollRect : objectScrollRect;

        if (scrollRect == null) return;

        float currentPos = scrollRect.horizontalNormalizedPosition;
        float newPos = Mathf.Clamp01(currentPos + scrollAmount);
        scrollRect.horizontalNormalizedPosition = newPos;
    }
    private bool IsButtonVisible(Button button, ScrollRect scrollRect, float margin = 0f)
    {
        if (button == null || scrollRect == null) return false;

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        RectTransform viewportRect = scrollRect.viewport;
        RectTransform contentRect = scrollRect.content;

        if (buttonRect == null || viewportRect == null || contentRect == null) return false;

        // Obtener los límites del viewport en coordenadas del content
        Vector3[] viewportCorners = new Vector3[4];
        viewportRect.GetWorldCorners(viewportCorners);

        Vector3 viewportMin = contentRect.InverseTransformPoint(viewportCorners[0]);
        Vector3 viewportMax = contentRect.InverseTransformPoint(viewportCorners[2]);

        // Obtener los límites del botón en coordenadas del content
        Vector3[] buttonCorners = new Vector3[4];
        buttonRect.GetWorldCorners(buttonCorners);

        Vector3 buttonMin = contentRect.InverseTransformPoint(buttonCorners[0]);
        Vector3 buttonMax = contentRect.InverseTransformPoint(buttonCorners[2]);

        // Verificar si el botón está completamente visible (con margen)
        bool isVisible = (buttonMin.x >= viewportMin.x - margin) &&
                         (buttonMax.x <= viewportMax.x + margin);

        return isVisible;
    }

    public void ResetManualScrollState()
    {
        userHasScrolledManually = false;
        lastManualScrollTime = 0f;
        Debug.Log("Estado de scroll manual reseteado");
    }

    // Métodos públicos para configurar el comportamiento
    public void SetAutoScrollEnabled(bool enabled)
    {
        enableAutoScrollOnNavigation = enabled;
    }

    public void SetScrollOnlyIfNotVisible(bool enabled)
    {
        onlyScrollIfElementNotVisible = enabled;
    }

    public void SetManualScrollGracePeriod(float seconds)
    {
        manualScrollGracePeriod = Mathf.Max(0f, seconds);
    }

    public void SetVisibilityMargin(float margin)
    {
        visibilityMargin = Mathf.Max(0f, margin);
    }
    // Método de debug
    [ContextMenu("Debug Inventory Navigation")]
    public void DebugCurrentState()
    {
        Debug.Log($"=== Inventory Navigation State ===");
        Debug.Log($"Current Section: {currentSection}");
        Debug.Log($"Current Selected: {currentSelectedButton?.name ?? "NULL"}");
        Debug.Log($"Note Buttons: {noteButtons.Count}");
        Debug.Log($"Object Buttons: {objectButtons.Count}");
        Debug.Log($"Note Index: {currentNoteIndex}");
        Debug.Log($"Object Index: {currentObjectIndex}");
        Debug.Log($"Scroll Enabled: {enableScrollWithInput}");
        Debug.Log($"Scroll Input: {scrollInput}");
        Debug.Log($"Scroll Sensitivity: {scrollSensitivity}");

        // Debug scroll positions
        if (noteScrollRect != null)
            Debug.Log($"Notes Scroll Position: {noteScrollRect.horizontalNormalizedPosition:F3}");
        if (objectScrollRect != null)
            Debug.Log($"Objects Scroll Position: {objectScrollRect.horizontalNormalizedPosition:F3}");
    }
}