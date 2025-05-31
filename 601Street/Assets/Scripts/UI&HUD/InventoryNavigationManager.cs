using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// Sistema de navegación ESPECÍFICO para el inventario - INDEPENDIENTE de UINavigationManager
/// Incluye scroll automático y animaciones de pulso
/// </summary>
public class InventoryNavigationManager : MonoBehaviour
{
    [Header("Contenedores del Inventario")]
    [SerializeField] private Transform noteContainer;
    [SerializeField] private Transform objectContainer;
    [SerializeField] private ScrollRect noteScrollRect;
    [SerializeField] private ScrollRect objectScrollRect;

    [Header("Configuración de Navegación")]
    [SerializeField] private int elementsPerRow = 6;
    [SerializeField] private float navigationDelay = 0.15f;

    [Header("Configuración de Scroll")]
    [SerializeField] private float scrollSensitivity = 2f;
    [SerializeField] private float scrollDeadZone = 0.3f;
    [SerializeField] private float autoScrollSpeed = 500f;
    [SerializeField] private bool enableAutoScrollToSelected = true;
    [SerializeField] private bool useSmoothedScrolling = true;
    [SerializeField] private float scrollSmoothTime = 0.1f;
    [SerializeField] private float manualScrollMemoryTime = 3f; // Tiempo que se "recuerda" el scroll manual

    [Header("Animaciones")]
    [SerializeField] private float selectedScale = 1.15f;
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private DG.Tweening.Ease animationEase = DG.Tweening.Ease.OutBack;

    [Header("Animación de Pulso")]
    [SerializeField] private bool enablePulseAnimation = true;
    [SerializeField] private float pulseScale = 0.05f;
    [SerializeField] private float pulseDuration = 1.5f;
    [SerializeField] private DG.Tweening.Ease pulseEase = DG.Tweening.Ease.InOutSine;

    [Header("Debug")]
    [SerializeField] private bool logNavigationActions = true;

    // Input del inventario
    private PlayerControls playerControls;
    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction scrollAction;

    // Lista de elementos navegables del inventario
    private List<Button> noteButtons = new List<Button>();
    private List<Button> objectButtons = new List<Button>();
    private int currentNoteIndex = 0;
    private int currentObjectIndex = 0;
    private InventorySection currentSection = InventorySection.Notes;
    private Button currentSelectedButton;
    private Button previousSelectedButton;

    // Control de tiempo
    private float lastNavigationTime;
    private float lastScrollTime;

    // Referencias del sistema
    private EventSystem eventSystem;
    private Tween currentScaleTween;
    private Tween currentPulseTween;

    // Estado del sistema
    private bool isActive = false;
    private LayoutGroup noteLayoutGroup;
    private LayoutGroup objectLayoutGroup;

    // Control de scroll manual
    private bool isManualScrolling = false;
    private float lastManualScrollTime = 0f;
    private const float manualScrollCooldown = 0.5f;

    // Sistema de memoria de scroll manual
    private bool hasManualScrolledNotes = false;
    private bool hasManualScrolledObjects = false;
    private float lastManualScrollTimeNotes = 0f;
    private float lastManualScrollTimeObjects = 0f;

    // Enum para identificar secciones
    private enum InventorySection
    {
        Notes,
        Objects
    }

    #region Inicialización

    private void Awake()
    {
        // Configurar Input System ESPECÍFICO para inventario
        InitializeInputSystem();

        // Obtener EventSystem
        eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("InventoryNavigationManager: No se encontró EventSystem");
        }

        // Inicialmente desactivado
        this.enabled = false;
    }

    private void InitializeInputSystem()
    {
        playerControls = new PlayerControls();

        // IMPORTANTE: Usar acciones ESPECÍFICAS de UI para el inventario
        navigateAction = playerControls.UI.Navigate;
        submitAction = playerControls.UI.Submit;
        scrollAction = playerControls.UI.Scroll_Inventory;

        // Configurar callbacks
        submitAction.performed += OnSubmitInventory;

        if (logNavigationActions)
            Debug.Log("InventoryNavigationManager: Input System inicializado con scroll");
    }

    private void Start()
    {
        // Detectar contenedores automáticamente si no están asignados
        DetectContainers();

        // Detectar ScrollRects automáticamente
        DetectScrollRects();

        // Detectar LayoutGroups para organización
        DetectLayoutGroups();
    }

    private void DetectContainers()
    {
        if (noteContainer == null || objectContainer == null)
        {
            Inventory_Manager inventoryManager = FindAnyObjectByType<Inventory_Manager>();
            if (inventoryManager != null)
            {
                noteContainer = inventoryManager.noteContainer;
                objectContainer = inventoryManager.objectContainer;

                if (logNavigationActions)
                    Debug.Log("InventoryNavigationManager: Contenedores detectados automáticamente");
            }
        }
    }

    private void DetectScrollRects()
    {
        // Buscar ScrollRects en los padres de los contenedores
        if (noteContainer != null && noteScrollRect == null)
        {
            noteScrollRect = noteContainer.GetComponentInParent<ScrollRect>();

            // Si no lo encuentra en el padre, buscar en hermanos
            if (noteScrollRect == null)
            {
                Transform parent = noteContainer.parent;
                while (parent != null && noteScrollRect == null)
                {
                    noteScrollRect = parent.GetComponentInChildren<ScrollRect>();
                    if (noteScrollRect != null && !IsChildOf(noteContainer, noteScrollRect.content))
                    {
                        noteScrollRect = null; // No es el ScrollRect correcto
                    }
                    parent = parent.parent;
                }
            }
        }

        if (objectContainer != null && objectScrollRect == null)
        {
            objectScrollRect = objectContainer.GetComponentInParent<ScrollRect>();

            // Si no lo encuentra en el padre, buscar en hermanos
            if (objectScrollRect == null)
            {
                Transform parent = objectContainer.parent;
                while (parent != null && objectScrollRect == null)
                {
                    objectScrollRect = parent.GetComponentInChildren<ScrollRect>();
                    if (objectScrollRect != null && !IsChildOf(objectContainer, objectScrollRect.content))
                    {
                        objectScrollRect = null; // No es el ScrollRect correcto
                    }
                    parent = parent.parent;
                }
            }
        }

        if (logNavigationActions)
        {
            Debug.Log($"ScrollRects detectados:");
            Debug.Log($"  - Notes: {noteScrollRect?.name ?? "NULL"} (Content: {noteScrollRect?.content?.name ?? "NULL"})");
            Debug.Log($"  - Objects: {objectScrollRect?.name ?? "NULL"} (Content: {objectScrollRect?.content?.name ?? "NULL"})");
        }
    }

    private bool IsChildOf(Transform child, Transform potentialParent)
    {
        if (potentialParent == null) return false;

        Transform current = child;
        while (current != null)
        {
            if (current == potentialParent)
                return true;
            current = current.parent;
        }
        return false;
    }

    private void DetectLayoutGroups()
    {
        if (noteContainer != null)
            noteLayoutGroup = noteContainer.GetComponent<LayoutGroup>();
        if (objectContainer != null)
            objectLayoutGroup = objectContainer.GetComponent<LayoutGroup>();
    }

    #endregion

    #region Activación/Desactivación del Sistema

    /// <summary>
    /// Activa el sistema de navegación del inventario
    /// </summary>
    public void ActivateInventoryNavigation()
    {
        if (isActive) return;

        if (logNavigationActions)
            Debug.Log("InventoryNavigationManager: Activando navegación del inventario");

        // Habilitar el sistema
        isActive = true;
        this.enabled = true;

        // Habilitar SOLO las acciones UI del inventario
        playerControls.UI.Enable();

        // Recopilar elementos del inventario
        RefreshInventoryElements();

        // Seleccionar primer elemento
        StartCoroutine(SelectFirstElementDelayed());
    }

    /// <summary>
    /// Desactiva el sistema de navegación del inventario
    /// </summary>
    public void DeactivateInventoryNavigation()
    {
        if (!isActive) return;

        if (logNavigationActions)
            Debug.Log("InventoryNavigationManager: Desactivando navegación del inventario");

        // Limpiar animaciones
        CleanupAnimations();

        // Deshabilitar inputs UI
        playerControls.UI.Disable();

        // Desactivar el sistema
        isActive = false;
        this.enabled = false;

        // Limpiar selección
        ClearSelection();
    }

    #endregion

    #region Gestión de Elementos

    /// <summary>
    /// Recopila todos los botones del inventario
    /// </summary>
    public void RefreshInventoryElements()
    {
        noteButtons.Clear();
        objectButtons.Clear();

        // Recopilar botones de notas
        if (noteContainer != null)
        {
            CollectButtonsFromContainer(noteContainer, noteButtons);
        }

        // Recopilar botones de objetos
        if (objectContainer != null)
        {
            CollectButtonsFromContainer(objectContainer, objectButtons);
        }

        if (logNavigationActions)
            Debug.Log($"InventoryNavigationManager: {noteButtons.Count} notas y {objectButtons.Count} objetos recopilados");

        // Resetear índices si es necesario
        if (currentNoteIndex >= noteButtons.Count)
        {
            currentNoteIndex = 0;
        }
        if (currentObjectIndex >= objectButtons.Count)
        {
            currentObjectIndex = 0;
        }

        // Determinar sección inicial
        if (noteButtons.Count > 0)
        {
            currentSection = InventorySection.Notes;
        }
        else if (objectButtons.Count > 0)
        {
            currentSection = InventorySection.Objects;
        }
    }

    private void CollectButtonsFromContainer(Transform container, List<Button> buttonList)
    {
        foreach (Transform child in container)
        {
            if (child.gameObject.activeInHierarchy)
            {
                Button button = child.GetComponent<Button>();
                if (button != null && button.interactable)
                {
                    buttonList.Add(button);
                }
            }
        }
    }

    private IEnumerator SelectFirstElementDelayed()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);

        SelectCurrentButton();
    }

    #endregion

    #region Navegación y Scroll

    private void Update()
    {
        if (!isActive || !enabled) return;

        // Verificar si el scroll manual ha terminado
        if (isManualScrolling && Time.time - lastManualScrollTime > manualScrollCooldown)
        {
            isManualScrolling = false;
        }

        // Actualizar estado de memoria de scroll manual
        UpdateManualScrollMemory();

        ProcessNavigation();
        ProcessScroll();
    }

    private void UpdateManualScrollMemory()
    {
        // Verificar si el tiempo de memoria ha expirado para cada sección
        if (hasManualScrolledNotes && Time.time - lastManualScrollTimeNotes > manualScrollMemoryTime)
        {
            hasManualScrolledNotes = false;
            if (logNavigationActions)
                Debug.Log("Memoria de scroll manual de Notas expirada - auto-scroll reactivado");
        }

        if (hasManualScrolledObjects && Time.time - lastManualScrollTimeObjects > manualScrollMemoryTime)
        {
            hasManualScrolledObjects = false;
            if (logNavigationActions)
                Debug.Log("Memoria de scroll manual de Objetos expirada - auto-scroll reactivado");
        }
    }

    private void ProcessNavigation()
    {
        Vector2 navigationInput = navigateAction.ReadValue<Vector2>();

        if (navigationInput.magnitude < 0.3f) return;

        if (Time.time - lastNavigationTime < navigationDelay) return;

        // Si se está haciendo scroll manual, ignorar navegación por un momento
        if (isManualScrolling) return;

        bool moved = false;

        // Navegación basada en secciones y grid
        if (Mathf.Abs(navigationInput.x) > Mathf.Abs(navigationInput.y))
        {
            // Movimiento horizontal
            if (navigationInput.x > 0) // Derecha
                moved = MoveHorizontal(1);
            else // Izquierda
                moved = MoveHorizontal(-1);
        }
        else
        {
            // Movimiento vertical
            if (navigationInput.y > 0) // Arriba (cambiar de sección o fila)
                moved = MoveVertical(-1);
            else // Abajo (cambiar de sección o fila)
                moved = MoveVertical(1);
        }

        if (moved)
        {
            SelectCurrentButton();
            lastNavigationTime = Time.time;

            // Solo hacer auto-scroll si no se ha hecho scroll manual recientemente en la sección actual
            if (enableAutoScrollToSelected && !isManualScrolling && !HasRecentManualScroll())
            {
                StartCoroutine(ScrollToSelectedElement());
            }
            else if (logNavigationActions && HasRecentManualScroll())
            {
                Debug.Log($"Auto-scroll desactivado - scroll manual reciente en sección {currentSection}");
            }
        }
    }

    private bool MoveHorizontal(int direction)
    {
        List<Button> currentButtons = GetCurrentSectionButtons();
        if (currentButtons.Count == 0) return false;

        int currentIndex = GetCurrentIndex();
        int row = currentIndex / elementsPerRow;
        int col = currentIndex % elementsPerRow;

        int newCol = col + direction;

        // Verificar límites horizontales - NO WRAP AROUND
        if (newCol < 0)
        {
            // Si estamos en la primera columna, no moverse
            return false;
        }
        else if (newCol >= elementsPerRow || row * elementsPerRow + newCol >= currentButtons.Count)
        {
            // Si estamos en la última columna o nos pasamos del límite, no moverse
            return false;
        }

        int newIndex = row * elementsPerRow + newCol;
        SetCurrentIndex(newIndex);
        return true;
    }

    private bool MoveVertical(int direction)
    {
        List<Button> currentButtons = GetCurrentSectionButtons();
        if (currentButtons.Count == 0) return false;

        int currentIndex = GetCurrentIndex();
        int newIndex = currentIndex + (direction * elementsPerRow);

        // Verificar si necesitamos cambiar de sección
        if (newIndex < 0)
        {
            // Moverse hacia arriba - cambiar a la otra sección si existe
            if (currentSection == InventorySection.Objects && noteButtons.Count > 0)
            {
                currentSection = InventorySection.Notes;
                // Ir a la última fila de las notas, manteniendo la columna
                int col = currentIndex % elementsPerRow;
                int lastNoteRow = (noteButtons.Count - 1) / elementsPerRow;
                int targetIndex = lastNoteRow * elementsPerRow + col;

                // Ajustar si se pasa del límite
                if (targetIndex >= noteButtons.Count)
                {
                    targetIndex = noteButtons.Count - 1;
                }

                currentNoteIndex = targetIndex;
                return true;
            }
            return false; // No hay sección arriba
        }
        else if (newIndex >= currentButtons.Count)
        {
            // Moverse hacia abajo - cambiar a la otra sección si existe
            if (currentSection == InventorySection.Notes && objectButtons.Count > 0)
            {
                currentSection = InventorySection.Objects;
                // Ir a la primera fila de los objetos, manteniendo la columna
                int col = currentIndex % elementsPerRow;
                currentObjectIndex = Mathf.Min(col, objectButtons.Count - 1);
                return true;
            }
            return false; // No hay sección abajo
        }
        else
        {
            // Movimiento normal dentro de la misma sección
            SetCurrentIndex(newIndex);
            return true;
        }
    }

    private void ProcessScroll()
    {
        Vector2 scrollInput = scrollAction.ReadValue<Vector2>();

        if (logNavigationActions && scrollInput.magnitude > 0.1f)
            Debug.Log($"Scroll Input Raw: {scrollInput}, Magnitude: {scrollInput.magnitude:F3}");

        if (scrollInput.magnitude < scrollDeadZone) return;

        if (Time.time - lastScrollTime < 0.02f) return;

        // Marcar que se está haciendo scroll manual
        isManualScrolling = true;
        lastManualScrollTime = Time.time;

        // Marcar memoria de scroll manual para la sección actual
        MarkManualScrollForCurrentSection();

        // Determinar en qué sección estamos
        ScrollRect targetScrollRect = GetScrollRectForSection(currentSection);

        if (targetScrollRect != null)
        {
            float scrollDirection = 0f;

            // Usar X para horizontal, Y para vertical
            if (targetScrollRect.horizontal)
            {
                if (Mathf.Abs(scrollInput.x) > 0.1f)
                {
                    scrollDirection = scrollInput.x;
                }
                else if (Mathf.Abs(scrollInput.y) > 0.1f)
                {
                    scrollDirection = scrollInput.y;
                }
            }
            else if (targetScrollRect.vertical)
            {
                scrollDirection = -scrollInput.y;
            }

            float scrollDelta = scrollDirection * scrollSensitivity * Time.deltaTime;

            float currentPos, newPos;

            if (targetScrollRect.horizontal)
            {
                currentPos = targetScrollRect.horizontalNormalizedPosition;
                newPos = Mathf.Clamp01(currentPos + scrollDelta);

                if (useSmoothedScrolling)
                {
                    targetScrollRect.DOHorizontalNormalizedPos(newPos, scrollSmoothTime)
                        .SetEase(Ease.OutQuad);
                }
                else
                {
                    targetScrollRect.horizontalNormalizedPosition = newPos;
                }
            }
            else
            {
                currentPos = targetScrollRect.verticalNormalizedPosition;
                newPos = Mathf.Clamp01(currentPos + scrollDelta);

                if (useSmoothedScrolling)
                {
                    targetScrollRect.DOVerticalNormalizedPos(newPos, scrollSmoothTime)
                        .SetEase(Ease.OutQuad);
                }
                else
                {
                    targetScrollRect.verticalNormalizedPosition = newPos;
                }
            }

            if (logNavigationActions)
            {
                Debug.Log($"Scroll manual aplicado en {currentSection}: {scrollDelta:F4}, nueva posición: {newPos:F3}");
            }

            lastScrollTime = Time.time;
        }
    }

    private void MarkManualScrollForCurrentSection()
    {
        if (currentSection == InventorySection.Notes)
        {
            hasManualScrolledNotes = true;
            lastManualScrollTimeNotes = Time.time;
            if (logNavigationActions)
                Debug.Log("Marcado scroll manual en sección de Notas");
        }
        else
        {
            hasManualScrolledObjects = true;
            lastManualScrollTimeObjects = Time.time;
            if (logNavigationActions)
                Debug.Log("Marcado scroll manual en sección de Objetos");
        }
    }

    private bool HasRecentManualScroll()
    {
        if (currentSection == InventorySection.Notes)
        {
            return hasManualScrolledNotes;
        }
        else
        {
            return hasManualScrolledObjects;
        }
    }

    private List<Button> GetCurrentSectionButtons()
    {
        return currentSection == InventorySection.Notes ? noteButtons : objectButtons;
    }

    private int GetCurrentIndex()
    {
        return currentSection == InventorySection.Notes ? currentNoteIndex : currentObjectIndex;
    }

    private void SetCurrentIndex(int index)
    {
        if (currentSection == InventorySection.Notes)
        {
            currentNoteIndex = index;
        }
        else
        {
            currentObjectIndex = index;
        }
    }

    private bool IsButtonInContainer(Button button, Transform container)
    {
        Transform parent = button.transform;
        while (parent != null)
        {
            if (parent == container)
                return true;
            parent = parent.parent;
        }
        return false;
    }

    private ScrollRect GetScrollRectForSection(InventorySection section)
    {
        switch (section)
        {
            case InventorySection.Notes:
                return noteScrollRect;
            case InventorySection.Objects:
                return objectScrollRect;
            default:
                return null;
        }
    }

    private IEnumerator ScrollToSelectedElement()
    {
        yield return new WaitForEndOfFrame();

        if (currentSelectedButton == null) yield break;

        ScrollRect scrollRect = GetScrollRectForSection(currentSection);

        if (scrollRect == null) yield break;

        // Obtener el RectTransform del botón seleccionado
        RectTransform buttonRect = currentSelectedButton.GetComponent<RectTransform>();
        RectTransform contentRect = scrollRect.content;
        RectTransform viewportRect = scrollRect.viewport;

        if (buttonRect == null || contentRect == null || viewportRect == null) yield break;

        // Auto-scroll usando la velocidad configurada
        Vector3[] buttonCorners = new Vector3[4];
        buttonRect.GetWorldCorners(buttonCorners);

        Vector3[] viewportCorners = new Vector3[4];
        viewportRect.GetWorldCorners(viewportCorners);

        // Convertir a coordenadas locales del contenido
        Vector2 buttonPosInContent = contentRect.InverseTransformPoint(buttonCorners[0]);
        Vector2 viewportSize = viewportRect.rect.size;
        Vector2 contentSize = contentRect.rect.size;

        // Calcular duración basada en autoScrollSpeed
        float scrollDistance = 0f;
        float targetPos = 0f;
        float currentPos = 0f;

        if (scrollRect.horizontal && contentSize.x > viewportSize.x)
        {
            currentPos = scrollRect.horizontalNormalizedPosition;
            targetPos = Mathf.Clamp01((-buttonPosInContent.x) / (contentSize.x - viewportSize.x));
            scrollDistance = Mathf.Abs(targetPos - currentPos) * (contentSize.x - viewportSize.x);

            float duration = scrollDistance / autoScrollSpeed;
            duration = Mathf.Clamp(duration, 0.1f, 1f);

            scrollRect.DOHorizontalNormalizedPos(targetPos, duration)
                .SetEase(Ease.OutQuad);

            if (logNavigationActions)
                Debug.Log($"Auto-scroll HORIZONTAL: distancia={scrollDistance:F1}px, duración={duration:F2}s");
        }
        else if (scrollRect.vertical && contentSize.y > viewportSize.y)
        {
            currentPos = scrollRect.verticalNormalizedPosition;
            targetPos = Mathf.Clamp01((-buttonPosInContent.y) / (contentSize.y - viewportSize.y));
            scrollDistance = Mathf.Abs(targetPos - currentPos) * (contentSize.y - viewportSize.y);

            float duration = scrollDistance / autoScrollSpeed;
            duration = Mathf.Clamp(duration, 0.1f, 1f);

            scrollRect.DOVerticalNormalizedPos(targetPos, duration)
                .SetEase(Ease.OutQuad);

            if (logNavigationActions)
                Debug.Log($"Auto-scroll VERTICAL: distancia={scrollDistance:F1}px, duración={duration:F2}s");
        }
    }

    private void SelectCurrentButton()
    {
        List<Button> currentButtons = GetCurrentSectionButtons();
        if (currentButtons.Count == 0) return;

        int index = GetCurrentIndex();
        if (index < 0 || index >= currentButtons.Count) return;

        Button button = currentButtons[index];
        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable) return;

        // Actualizar selección
        previousSelectedButton = currentSelectedButton;
        currentSelectedButton = button;

        // Actualizar EventSystem
        eventSystem?.SetSelectedGameObject(button.gameObject);

        // Aplicar animaciones
        ApplySelectionAnimations();

        if (logNavigationActions)
            Debug.Log($"InventoryNavigationManager: Seleccionado {button.name} (sección: {currentSection}, índice: {index})");
    }

    #endregion

    #region Input Callbacks

    private void OnSubmitInventory(InputAction.CallbackContext context)
    {
        if (!isActive) return;

        if (currentSelectedButton != null &&
            currentSelectedButton.gameObject.activeInHierarchy &&
            currentSelectedButton.interactable)
        {
            if (logNavigationActions)
                Debug.Log($"InventoryNavigationManager: Ejecutando acción en {currentSelectedButton.name}");

            currentSelectedButton.onClick.Invoke();
        }
        else
        {
            if (logNavigationActions)
                Debug.LogWarning("InventoryNavigationManager: No hay botón válido seleccionado para Submit");

            RefreshInventoryElements();
            SelectCurrentButton();
        }
    }

    #endregion

    #region Animaciones

    private void ApplySelectionAnimations()
    {
        // Limpiar animaciones anteriores
        CleanupAnimations();

        // Reset elemento anterior
        if (previousSelectedButton != null && previousSelectedButton != currentSelectedButton)
        {
            previousSelectedButton.transform.DOKill();
            previousSelectedButton.transform.localScale = Vector3.one;
        }

        // Animar elemento actual
        if (currentSelectedButton != null)
        {
            // Reset inicial
            currentSelectedButton.transform.localScale = Vector3.one;

            // Animación de escala inicial
            currentScaleTween = currentSelectedButton.transform
                .DOScale(Vector3.one * selectedScale, animationDuration)
                .SetEase(animationEase)
                .SetUpdate(true)
                .OnComplete(() => {
                    // Una vez completada la animación de escala, iniciar el pulso
                    if (enablePulseAnimation && currentSelectedButton != null)
                    {
                        StartPulseAnimation();
                    }
                });
        }
    }

    private void StartPulseAnimation()
    {
        if (currentSelectedButton == null) return;

        // Configurar animación de pulso
        Vector3 baseScale = Vector3.one * selectedScale;
        Vector3 pulseScaleVector = baseScale + (Vector3.one * pulseScale);

        currentPulseTween = currentSelectedButton.transform
            .DOScale(pulseScaleVector, pulseDuration * 0.5f)
            .SetEase(pulseEase)
            .SetLoops(-1, LoopType.Yoyo) // Bucle infinito de ida y vuelta
            .SetUpdate(true);

        if (logNavigationActions)
            Debug.Log($"Iniciando animación de pulso en: {currentSelectedButton.name}");
    }

    private void CleanupAnimations()
    {
        currentScaleTween?.Kill();
        currentPulseTween?.Kill();

        currentScaleTween = null;
        currentPulseTween = null;

        // Resetear escala de todos los botones
        foreach (Button button in noteButtons)
        {
            if (button != null)
            {
                button.transform.DOKill();
                button.transform.localScale = Vector3.one;
            }
        }

        foreach (Button button in objectButtons)
        {
            if (button != null)
            {
                button.transform.DOKill();
                button.transform.localScale = Vector3.one;
            }
        }
    }

    private void ClearSelection()
    {
        currentSelectedButton = null;
        previousSelectedButton = null;
        eventSystem?.SetSelectedGameObject(null);
    }

    #endregion

    #region Eventos del Sistema

    private void OnEnable()
    {
        if (isActive)
        {
            playerControls?.UI.Enable();
        }
    }

    private void OnDisable()
    {
        playerControls?.UI.Disable();
        CleanupAnimations();
    }

    private void OnDestroy()
    {
        CleanupAnimations();

        if (submitAction != null)
        {
            submitAction.performed -= OnSubmitInventory;
        }

        playerControls?.Dispose();
    }

    #endregion

    #region Métodos Públicos

    /// <summary>
    /// Fuerza la actualización de elementos del inventario
    /// </summary>
    public void ForceRefreshElements()
    {
        if (isActive)
        {
            RefreshInventoryElements();
            SelectCurrentButton();
        }
    }

    /// <summary>
    /// Configura los contenedores y ScrollRects manualmente
    /// </summary>
    public void SetContainers(Transform noteContainer, Transform objectContainer, ScrollRect noteScroll = null, ScrollRect objectScroll = null)
    {
        this.noteContainer = noteContainer;
        this.objectContainer = objectContainer;

        if (noteScroll != null) this.noteScrollRect = noteScroll;
        if (objectScroll != null) this.objectScrollRect = objectScroll;

        DetectScrollRects();
        DetectLayoutGroups();

        if (isActive)
        {
            RefreshInventoryElements();
        }
    }

    /// <summary>
    /// Configura las opciones de scroll
    /// </summary>
    public void SetScrollSettings(float sensitivity, float deadZone, float autoScrollSpeed, bool enableAutoScroll)
    {
        scrollSensitivity = sensitivity;
        scrollDeadZone = deadZone;
        this.autoScrollSpeed = autoScrollSpeed;
        enableAutoScrollToSelected = enableAutoScroll;

        if (logNavigationActions)
            Debug.Log($"Configuración de scroll actualizada: Sensitivity={sensitivity}, DeadZone={deadZone}, AutoSpeed={autoScrollSpeed}");
    }

    /// <summary>
    /// Configura el tiempo de memoria del scroll manual
    /// </summary>
    public void SetManualScrollMemoryTime(float memoryTime)
    {
        manualScrollMemoryTime = memoryTime;
        if (logNavigationActions)
            Debug.Log($"Tiempo de memoria de scroll manual cambiado a: {memoryTime} segundos");
    }

    /// <summary>
    /// Resetea la memoria de scroll manual para todas las secciones
    /// </summary>
    public void ResetManualScrollMemory()
    {
        hasManualScrolledNotes = false;
        hasManualScrolledObjects = false;
        lastManualScrollTimeNotes = 0f;
        lastManualScrollTimeObjects = 0f;

        if (logNavigationActions)
            Debug.Log("Memoria de scroll manual reseteada para todas las secciones");
    }

    /// <summary>
    /// Resetea la memoria de scroll manual solo para la sección actual
    /// </summary>
    public void ResetCurrentSectionScrollMemory()
    {
        if (currentSection == InventorySection.Notes)
        {
            hasManualScrolledNotes = false;
            lastManualScrollTimeNotes = 0f;
            if (logNavigationActions)
                Debug.Log("Memoria de scroll manual reseteada para sección de Notas");
        }
        else
        {
            hasManualScrolledObjects = false;
            lastManualScrollTimeObjects = 0f;
            if (logNavigationActions)
                Debug.Log("Memoria de scroll manual reseteada para sección de Objetos");
        }
    }

    /// <summary>
    /// Configura solo la sensibilidad del scroll
    /// </summary>
    public void SetScrollSensitivity(float sensitivity)
    {
        scrollSensitivity = sensitivity;
        if (logNavigationActions)
            Debug.Log($"Sensibilidad de scroll cambiada a: {sensitivity}");
    }

    /// <summary>
    /// Configura el suavizado del scroll
    /// </summary>
    public void SetScrollSmoothing(bool enabled, float smoothTime = 0.1f)
    {
        useSmoothedScrolling = enabled;
        scrollSmoothTime = smoothTime;

        if (logNavigationActions)
            Debug.Log($"Scroll suavizado: {(enabled ? "habilitado" : "deshabilitado")}, tiempo: {smoothTime}");
    }

    /// <summary>
    /// Configura las opciones de animación de pulso
    /// </summary>
    public void SetPulseSettings(bool enable, float scale, float duration, DG.Tweening.Ease ease)
    {
        enablePulseAnimation = enable;
        pulseScale = scale;
        pulseDuration = duration;
        pulseEase = ease;

        if (enable && currentSelectedButton != null)
        {
            StartPulseAnimation();
        }
        else if (!enable)
        {
            currentPulseTween?.Kill();
        }
    }

    /// <summary>
    /// Verifica si el sistema está activo
    /// </summary>
    public bool IsNavigationActive()
    {
        return isActive && enabled;
    }

    #endregion

    #region Debug

    [ContextMenu("Debug Inventory Navigation")]
    public void DebugInventoryNavigation()
    {
        Debug.Log($"=== INVENTORY NAVIGATION STATE ===");
        Debug.Log($"Is Active: {isActive}");
        Debug.Log($"Component Enabled: {enabled}");
        Debug.Log($"Current Section: {currentSection}");
        Debug.Log($"Current Selected: {currentSelectedButton?.name ?? "NULL"}");
        Debug.Log($"Current Note Index: {currentNoteIndex}");
        Debug.Log($"Current Object Index: {currentObjectIndex}");
        Debug.Log($"Total Note Buttons: {noteButtons.Count}");
        Debug.Log($"Total Object Buttons: {objectButtons.Count}");
        Debug.Log($"Note Container: {noteContainer?.name ?? "NULL"}");
        Debug.Log($"Object Container: {objectContainer?.name ?? "NULL"}");
        Debug.Log($"Note ScrollRect: {noteScrollRect?.name ?? "NULL"}");
        Debug.Log($"Object ScrollRect: {objectScrollRect?.name ?? "NULL"}");
        Debug.Log($"Manual Scrolling: {isManualScrolling}");
        Debug.Log($"Manual Scroll Memory - Notes: {hasManualScrolledNotes}, Objects: {hasManualScrolledObjects}");
        Debug.Log($"Manual Scroll Memory Time: {manualScrollMemoryTime}s");
        Debug.Log($"Pulse Animation: {enablePulseAnimation}");

        // Debug específico de ScrollRects
        if (noteScrollRect != null)
        {
            Debug.Log($"  Note ScrollRect Details:");
            Debug.Log($"    - Horizontal: {noteScrollRect.horizontal}, Vertical: {noteScrollRect.vertical}");
            Debug.Log($"    - Content: {noteScrollRect.content?.name ?? "NULL"}");
            Debug.Log($"    - Viewport: {noteScrollRect.viewport?.name ?? "NULL"}");
            if (noteScrollRect.vertical)
                Debug.Log($"    - Current Pos: {noteScrollRect.verticalNormalizedPosition:F3}");
            if (noteScrollRect.horizontal)
                Debug.Log($"    - Current Pos: {noteScrollRect.horizontalNormalizedPosition:F3}");
        }

        if (objectScrollRect != null)
        {
            Debug.Log($"  Object ScrollRect Details:");
            Debug.Log($"    - Horizontal: {objectScrollRect.horizontal}, Vertical: {objectScrollRect.vertical}");
            Debug.Log($"    - Content: {objectScrollRect.content?.name ?? "NULL"}");
            Debug.Log($"    - Viewport: {objectScrollRect.viewport?.name ?? "NULL"}");
            if (objectScrollRect.vertical)
                Debug.Log($"    - Current Pos: {objectScrollRect.verticalNormalizedPosition:F3}");
            if (objectScrollRect.horizontal)
                Debug.Log($"    - Current Pos: {objectScrollRect.horizontalNormalizedPosition:F3}");
        }

        Debug.Log("=== NOTE BUTTONS ===");
        for (int i = 0; i < noteButtons.Count; i++)
        {
            var button = noteButtons[i];
            string selected = (currentSection == InventorySection.Notes && i == currentNoteIndex) ? " [SELECTED]" : "";
            Debug.Log($"  [{i}] {button?.name ?? "NULL"} - Active: {button?.gameObject.activeInHierarchy} - Interactable: {button?.interactable}{selected}");
        }

        Debug.Log("=== OBJECT BUTTONS ===");
        for (int i = 0; i < objectButtons.Count; i++)
        {
            var button = objectButtons[i];
            string selected = (currentSection == InventorySection.Objects && i == currentObjectIndex) ? " [SELECTED]" : "";
            Debug.Log($"  [{i}] {button?.name ?? "NULL"} - Active: {button?.gameObject.activeInHierarchy} - Interactable: {button?.interactable}{selected}");
        }
    }

    [ContextMenu("Force Refresh Elements")]
    public void ForceRefreshElementsFromContext()
    {
        ForceRefreshElements();
    }

    [ContextMenu("Test Navigation")]
    public void TestNavigation()
    {
        if (!isActive)
        {
            ActivateInventoryNavigation();
        }
        else
        {
            DeactivateInventoryNavigation();
        }
    }

    [ContextMenu("Switch to Notes Section")]
    public void SwitchToNotesSection()
    {
        if (noteButtons.Count > 0)
        {
            currentSection = InventorySection.Notes;
            if (currentNoteIndex >= noteButtons.Count)
                currentNoteIndex = 0;
            SelectCurrentButton();
            Debug.Log("Cambiado a sección de Notas");
        }
    }

    [ContextMenu("Switch to Objects Section")]
    public void SwitchToObjectsSection()
    {
        if (objectButtons.Count > 0)
        {
            currentSection = InventorySection.Objects;
            if (currentObjectIndex >= objectButtons.Count)
                currentObjectIndex = 0;
            SelectCurrentButton();
            Debug.Log("Cambiado a sección de Objetos");
        }
    }

    [ContextMenu("Test Scroll To Selected")]
    public void TestScrollToSelected()
    {
        if (currentSelectedButton != null)
        {
            StartCoroutine(ScrollToSelectedElement());
        }
    }

    [ContextMenu("Toggle Pulse Animation")]
    public void TogglePulseAnimation()
    {
        SetPulseSettings(!enablePulseAnimation, pulseScale, pulseDuration, pulseEase);
    }

    [ContextMenu("Test Manual Scroll")]
    public void TestManualScroll()
    {
        ScrollRect scrollRect = GetScrollRectForSection(currentSection);

        if (scrollRect != null)
        {
            Debug.Log($"Probando scroll manual en sección: {currentSection}");
            Debug.Log($"ScrollRect: {scrollRect.name}");
            Debug.Log($"Horizontal: {scrollRect.horizontal}, Vertical: {scrollRect.vertical}");
            Debug.Log($"Configuración actual - Sensitivity: {scrollSensitivity}, Smoothed: {useSmoothedScrolling}, SmoothTime: {scrollSmoothTime}");

            if (scrollRect.horizontal)
            {
                float currentPos = scrollRect.horizontalNormalizedPosition;
                float newPos = Mathf.Clamp01(currentPos + 0.2f);
                Debug.Log($"Posición horizontal: {currentPos:F3} -> {newPos:F3}");

                if (useSmoothedScrolling)
                {
                    scrollRect.DOHorizontalNormalizedPos(newPos, scrollSmoothTime);
                }
                else
                {
                    scrollRect.horizontalNormalizedPosition = newPos;
                }
            }

            if (scrollRect.vertical)
            {
                float currentPos = scrollRect.verticalNormalizedPosition;
                float newPos = Mathf.Clamp01(currentPos - 0.2f);
                Debug.Log($"Posición vertical: {currentPos:F3} -> {newPos:F3}");

                if (useSmoothedScrolling)
                {
                    scrollRect.DOVerticalNormalizedPos(newPos, scrollSmoothTime);
                }
                else
                {
                    scrollRect.verticalNormalizedPosition = newPos;
                }
            }
        }
        else
        {
            Debug.LogError($"No se encontró ScrollRect para sección: {currentSection}");
        }
    }

    [ContextMenu("Reset Manual Scrolling Flag")]
    public void ResetManualScrollingFlag()
    {
        isManualScrolling = false;
        Debug.Log("Flag de scroll manual reseteado");
    }

    [ContextMenu("Reset Manual Scroll Memory")]
    public void ResetManualScrollMemoryFromContext()
    {
        ResetManualScrollMemory();
    }

    [ContextMenu("Reset Current Section Scroll Memory")]
    public void ResetCurrentSectionScrollMemoryFromContext()
    {
        ResetCurrentSectionScrollMemory();
    }

    [ContextMenu("Test Manual Scroll Memory")]
    public void TestManualScrollMemory()
    {
        Debug.Log("=== ESTADO DE MEMORIA DE SCROLL MANUAL ===");
        Debug.Log($"Sección actual: {currentSection}");
        Debug.Log($"Notas - Manual: {hasManualScrolledNotes}, Último tiempo: {lastManualScrollTimeNotes}");
        Debug.Log($"Objetos - Manual: {hasManualScrolledObjects}, Último tiempo: {lastManualScrollTimeObjects}");
        Debug.Log($"Tiempo de memoria configurado: {manualScrollMemoryTime}s");
        Debug.Log($"¿Scroll manual reciente en sección actual?: {HasRecentManualScroll()}");
        Debug.Log($"¿Auto-scroll activado?: {enableAutoScrollToSelected && !HasRecentManualScroll()}");

        if (HasRecentManualScroll())
        {
            float timeRemaining = 0f;
            if (currentSection == InventorySection.Notes)
                timeRemaining = manualScrollMemoryTime - (Time.time - lastManualScrollTimeNotes);
            else
                timeRemaining = manualScrollMemoryTime - (Time.time - lastManualScrollTimeObjects);

            Debug.Log($"Tiempo restante hasta reactivar auto-scroll: {timeRemaining:F1}s");
        }
    }

    [ContextMenu("Test Scroll Speed - Slow")]
    public void TestScrollSpeedSlow()
    {
        SetScrollSensitivity(1f);
        SetScrollSmoothing(true, 0.3f);
        Debug.Log("Velocidad de scroll configurada a: LENTA");
    }

    [ContextMenu("Test Scroll Speed - Normal")]
    public void TestScrollSpeedNormal()
    {
        SetScrollSensitivity(3f);
        SetScrollSmoothing(true, 0.1f);
        Debug.Log("Velocidad de scroll configurada a: NORMAL");
    }

    [ContextMenu("Test Scroll Speed - Fast")]
    public void TestScrollSpeedFast()
    {
        SetScrollSensitivity(6f);
        SetScrollSmoothing(false);
        Debug.Log("Velocidad de scroll configurada a: RÁPIDA");
    }

    [ContextMenu("Test Scroll Speed - Very Fast")]
    public void TestScrollSpeedVeryFast()
    {
        SetScrollSensitivity(10f);
        SetScrollSmoothing(false);
        Debug.Log("Velocidad de scroll configurada a: MUY RÁPIDA");
    }

    [ContextMenu("Toggle Scroll Smoothing")]
    public void ToggleScrollSmoothing()
    {
        SetScrollSmoothing(!useSmoothedScrolling, scrollSmoothTime);
    }

    [ContextMenu("Test Navigation Limits")]
    public void TestNavigationLimits()
    {
        Debug.Log("=== PRUEBA DE LÍMITES DE NAVEGACIÓN ===");
        Debug.Log($"Sección actual: {currentSection}");

        List<Button> currentButtons = GetCurrentSectionButtons();
        int currentIndex = GetCurrentIndex();

        Debug.Log($"Índice actual: {currentIndex}/{currentButtons.Count - 1}");

        if (currentButtons.Count > 0)
        {
            int row = currentIndex / elementsPerRow;
            int col = currentIndex % elementsPerRow;
            Debug.Log($"Posición en grid: Fila {row}, Columna {col}");

            // Verificar límites
            bool canMoveLeft = col > 0;
            bool canMoveRight = col < elementsPerRow - 1 && (row * elementsPerRow + col + 1) < currentButtons.Count;
            bool canMoveUp = currentIndex - elementsPerRow >= 0 || (currentSection == InventorySection.Objects && noteButtons.Count > 0);
            bool canMoveDown = currentIndex + elementsPerRow < currentButtons.Count || (currentSection == InventorySection.Notes && objectButtons.Count > 0);

            Debug.Log($"Puede moverse - Izquierda: {canMoveLeft}, Derecha: {canMoveRight}, Arriba: {canMoveUp}, Abajo: {canMoveDown}");
        }
    }

    #endregion
}