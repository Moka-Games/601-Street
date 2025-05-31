using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// VERSIÓN CORREGIDA: Eliminados todos los métodos de desactivación problemáticos
/// Mantiene la navegación siempre activa para evitar conflictos
/// </summary>
public class UINavigationManager : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float navigationDelay = 0.15f;
    [SerializeField] private bool autoSelectFirstButton = true;
    [SerializeField] private AudioClip navigationSound;
    [SerializeField] private AudioClip selectSound;

    [Header("Selección Automática")]
    [SerializeField] private bool enableAutoSelection = true;
    [SerializeField] private float autoSelectionCheckInterval = 0.1f;

    [Header("Selección por Defecto")]
    [SerializeField] private Selectable firstSelected;
    [SerializeField] private bool isFirstSelected = true;

    [Header("Animaciones DOTween")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private DG.Tweening.Ease scaleInEase = DG.Tweening.Ease.OutBack;
    [SerializeField] private DG.Tweening.Ease scaleOutEase = DG.Tweening.Ease.InBack;
    [SerializeField] private bool enablePulseEffect = true;
    [SerializeField] private float pulseIntensity = 0.05f;
    [SerializeField] private float pulseDuration = 1.5f;

    [Header("Referencias")]
    [SerializeField] private List<Selectable> navigableElements = new List<Selectable>();

    // Sistema de Input
    private PlayerControls playerControls;
    private Vector2 navigationInput;
    private float lastNavigationTime;

    // Estado de navegación
    private int currentIndex = 0;
    private Selectable currentSelected;
    private Selectable previousSelected;
    private EventSystem eventSystem;
    private AudioSource audioSource;

    // Sistema de selección automática
    private Coroutine autoSelectionCoroutine;
    private int lastActiveElementCount = -1;

    // Animaciones
    private Tween currentAnimationTween;
    private Tween currentPulseTween;

    // Eventos
    public System.Action<Selectable> OnElementSelected;
    public System.Action<Selectable> OnElementSubmitted;
    public System.Action OnCancelled;

    private void Awake()
    {
        // Configurar controles de input
        playerControls = new PlayerControls();
        SetupInputActions();

        // Obtener o crear EventSystem
        eventSystem = EventSystemManager.GetEventSystem();
        if (eventSystem == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystem = eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();

            Debug.LogWarning("UINavigationManager: EventSystem creado manualmente. Considera usar EventSystemManager.");
        }

        // Obtener AudioSource si existe
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        // CORREGIDO: Siempre habilitar UI desde el inicio
        playerControls.UI.Enable();

        // Limpiar estado anterior
        CleanupAnimations();

        // Iniciar sistema de selección automática
        StartAutoSelectionSystem();

        // Esperar un frame para que todo esté inicializado
        StartCoroutine(SelectDefaultElementCoroutine());
    }

    private void OnDisable()
    {
        // CORREGIDO: Solo deshabilitar si realmente se está destruyendo
        // No hacer nada aquí para evitar desactivaciones problemáticas
        StopAutoSelectionSystem();
        CleanupAnimations();
    }

    private void OnDestroy()
    {
        StopAutoSelectionSystem();
        CleanupAnimations();
        playerControls?.Dispose();
    }

    #region Auto Selection System

    private void StartAutoSelectionSystem()
    {
        if (enableAutoSelection && autoSelectionCoroutine == null)
        {
            autoSelectionCoroutine = StartCoroutine(AutoSelectionCoroutine());
        }
    }

    private void StopAutoSelectionSystem()
    {
        if (autoSelectionCoroutine != null)
        {
            StopCoroutine(autoSelectionCoroutine);
            autoSelectionCoroutine = null;
        }
    }

    private IEnumerator AutoSelectionCoroutine()
    {
        while (enabled && gameObject.activeInHierarchy)
        {
            yield return new WaitForSeconds(autoSelectionCheckInterval);

            CheckForAutoSelection();
        }
    }

    private void CheckForAutoSelection()
    {
        if (!enableAutoSelection) return;

        // Contar elementos activos e interactuables
        List<Selectable> activeElements = GetActiveInteractableElements();

        // Si el número de elementos activos cambió, verificar si necesitamos auto-seleccionar
        if (activeElements.Count != lastActiveElementCount)
        {
            lastActiveElementCount = activeElements.Count;
            Debug.Log($"Cambio detectado: {activeElements.Count} elementos activos");

            // LÓGICA MEJORADA DE SELECCIÓN
            if (activeElements.Count > 0)
            {
                Selectable elementToSelect = DetermineElementToSelect(activeElements);

                if (elementToSelect != null)
                {
                    int index = navigableElements.IndexOf(elementToSelect);
                    if (index >= 0)
                    {
                        // Solo cambiar selección si es diferente al actual o si no hay nada seleccionado
                        if (currentSelected != elementToSelect)
                        {
                            Debug.Log($"Auto-seleccionando elemento: {elementToSelect.name}");
                            SelectElement(index, true);
                        }
                    }
                }
            }
            // Si no hay elementos activos, limpiar selección
            else if (activeElements.Count == 0)
            {
                if (currentSelected != null)
                {
                    Debug.Log("No hay elementos activos, limpiando selección");
                    ClearSelection();
                }
            }
        }

        // Verificar si el elemento actualmente seleccionado sigue siendo válido
        if (currentSelected != null && !IsElementInteractable(currentSelected))
        {
            Debug.Log($"Elemento seleccionado {currentSelected.name} ya no es interactuable");

            // Buscar un elemento de reemplazo
            if (activeElements.Count > 0)
            {
                Selectable replacementElement = DetermineElementToSelect(activeElements);
                if (replacementElement != null)
                {
                    int index = navigableElements.IndexOf(replacementElement);
                    if (index >= 0)
                    {
                        SelectElement(index, true);
                    }
                }
            }
            else
            {
                ClearSelection();
            }
        }
    }

    /// <summary>
    /// Determina qué elemento debe ser seleccionado basado en la lógica de prioridades
    /// </summary>
    private Selectable DetermineElementToSelect(List<Selectable> activeElements)
    {
        if (activeElements.Count == 0) return null;

        // CASO 1: Si First Selected está configurado y está entre los elementos activos
        if (firstSelected != null && activeElements.Contains(firstSelected))
        {
            Debug.Log($"First Selected ({firstSelected.name}) encontrado entre elementos activos - Seleccionando");
            return firstSelected;
        }

        // CASO 2: Solo hay un elemento activo
        if (activeElements.Count == 1)
        {
            Debug.Log($"Solo un elemento activo: {activeElements[0].name}");
            return activeElements[0];
        }

        // CASO 3: Múltiples elementos activos, ninguno es First Selected
        // Seleccionar uno aleatorio entre los activos
        int randomIndex = UnityEngine.Random.Range(0, activeElements.Count);
        Selectable randomElement = activeElements[randomIndex];
        Debug.Log($"Múltiples elementos activos ({activeElements.Count}), seleccionando aleatorio: {randomElement.name}");
        return randomElement;
    }

    private List<Selectable> GetActiveInteractableElements()
    {
        List<Selectable> activeElements = new List<Selectable>();

        foreach (var element in navigableElements)
        {
            if (IsElementInteractable(element))
            {
                activeElements.Add(element);
            }
        }

        return activeElements;
    }

    private bool IsElementInteractable(Selectable element)
    {
        return element != null &&
               element.gameObject.activeInHierarchy &&
               element.interactable &&
               element.IsInteractable(); // Verificación adicional de Unity
    }

    private void ClearSelection()
    {
        if (currentSelected != null)
        {
            // Detener animaciones del elemento actual
            StopAllAnimations();
            ResetAllElementsToNormalScale();
        }

        currentSelected = null;
        previousSelected = null;
        eventSystem.SetSelectedGameObject(null);
    }

    #endregion

    #region Input System

    private void SetupInputActions()
    {
        // Configurar callbacks de input
        playerControls.UI.Navigate.performed += OnNavigate;
        playerControls.UI.Submit.performed += OnSubmit;
        playerControls.UI.Cancel.performed += OnCancel;
    }

    // Variables para prevenir doble input
    private float lastSubmitTime = 0f;
    private float submitCooldown = 0.3f; // Cooldown entre inputs Submit
    private bool isInputBlocked = false;

    private void OnSubmit(InputAction.CallbackContext context)
    {
        // PROTECCIÓN CONTRA DOBLE INPUT
        float currentTime = Time.unscaledTime;
        if (currentTime - lastSubmitTime < submitCooldown)
        {
            Debug.Log("Input Submit bloqueado por cooldown");
            return;
        }

        if (isInputBlocked)
        {
            Debug.Log("Input Submit bloqueado por flag de bloqueo");
            return;
        }

        // VERIFICACIÓN MEJORADA: Asegurarse de que el elemento sigue siendo válido antes de ejecutar la acción
        if (currentSelected != null && IsElementInteractable(currentSelected))
        {
            Debug.Log($"Ejecutando acción en elemento válido: {currentSelected.name}");

            // Actualizar tiempo del último submit
            lastSubmitTime = currentTime;

            // Simular click en el botón actual
            if (currentSelected is Button button)
            {
                button.onClick.Invoke();
                PlaySound(selectSound);
                OnElementSubmitted?.Invoke(currentSelected);
            }
            else
            {
                // Para otros tipos de Selectable (Toggle, Slider, etc.)
                ExecuteEvents.Execute(currentSelected.gameObject,
                    new BaseEventData(eventSystem), ExecuteEvents.submitHandler);
                PlaySound(selectSound);
                OnElementSubmitted?.Invoke(currentSelected);
            }
        }
        else
        {
            Debug.LogWarning("Intento de ejecutar acción en elemento no válido o no interactuable");

            // Intentar encontrar un elemento válido automáticamente SOLO si no hay cooldown activo
            if (currentTime - lastSubmitTime >= submitCooldown)
            {
                List<Selectable> activeElements = GetActiveInteractableElements();
                if (activeElements.Count == 1)
                {
                    int index = navigableElements.IndexOf(activeElements[0]);
                    if (index >= 0)
                    {
                        SelectElement(index, true);
                        // NO ejecutar la acción inmediatamente para evitar bucles
                        Debug.Log("Elemento auto-seleccionado, esperando próximo input para ejecutar");
                    }
                }
            }
        }
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        OnCancelled?.Invoke();
        PlaySound(navigationSound);
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        // La navegación se maneja en Update para mayor control
    }

    #endregion

    #region Navigation Logic

    private void Update()
    {
        if (!enabled || !gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[UINavigationManager] {gameObject.name} está inactivo en Update!");
            return;
        }

        navigationInput = playerControls.UI.Navigate.ReadValue<Vector2>();

        // Procesar navegación si ha pasado suficiente tiempo
        if (Time.time - lastNavigationTime >= navigationDelay)
        {
            ProcessNavigation();
        }

        // Mantener selección sincronizada con EventSystem
        SyncWithEventSystem();
    }

    private void ProcessNavigation()
    {
        if (navigationInput.magnitude < 0.3f || navigableElements.Count == 0) return;

        int newIndex = currentIndex;

        // Navegación horizontal
        if (Mathf.Abs(navigationInput.x) > Mathf.Abs(navigationInput.y))
        {
            if (navigationInput.x > 0) // Derecha
                newIndex = GetNextValidIndex(currentIndex, 1);
            else // Izquierda  
                newIndex = GetNextValidIndex(currentIndex, -1);
        }
        // Navegación vertical
        else
        {
            if (navigationInput.y > 0) // Arriba
                newIndex = GetNextValidIndex(currentIndex, -1);
            else // Abajo
                newIndex = GetNextValidIndex(currentIndex, 1);
        }

        if (newIndex != currentIndex)
        {
            SelectElement(newIndex, false); // false indica navegación manual
            lastNavigationTime = Time.time;
        }
    }

    private int GetNextValidIndex(int startIndex, int direction)
    {
        int attempts = 0;
        int index = startIndex;

        do
        {
            index = (index + direction + navigableElements.Count) % navigableElements.Count;
            attempts++;

            if (IsElementInteractable(navigableElements[index]))
            {
                return index;
            }
        }
        while (attempts < navigableElements.Count);

        return startIndex; // Si no encuentra ninguno válido, mantiene el actual
    }

    private void SelectElement(int index, bool isAutoSelection = false)
    {
        if (index < 0 || index >= navigableElements.Count) return;

        Selectable element = navigableElements[index];
        if (!IsElementInteractable(element))
            return;

        // Actualizar selección
        previousSelected = currentSelected;
        currentIndex = index;
        currentSelected = element;

        // Actualizar EventSystem
        eventSystem.SetSelectedGameObject(element.gameObject);

        // SIEMPRE aplicar animaciones al cambiar selección
        ApplySelectionAnimations();

        // Efectos sonoros (solo si no es selección automática para no ser molesto)
        if (!isAutoSelection)
        {
            PlaySound(navigationSound);
        }

        OnElementSelected?.Invoke(currentSelected);

        Debug.Log($"Elemento seleccionado: {element.name} (índice: {index}, auto: {isAutoSelection})");
    }

    #endregion

    #region Default Selection

    private System.Collections.IEnumerator SelectDefaultElementCoroutine()
    {
        yield return null; // Esperar un frame

        // Asegurar que todos los elementos estén en escala normal
        ResetAllElementsToNormalScale();

        Debug.Log($"=== SELECCIÓN POR DEFECTO ===");
        Debug.Log($"FirstSelected configurado: {firstSelected?.name ?? "NINGUNO"}");
        Debug.Log($"IsFirstSelected habilitado: {isFirstSelected}");
        Debug.Log($"AutoSelectFirstButton habilitado: {autoSelectFirstButton}");

        // Obtener elementos activos actuales
        List<Selectable> activeElements = GetActiveInteractableElements();
        Debug.Log($"Elementos activos encontrados: {activeElements.Count}");

        foreach (var element in activeElements)
        {
            Debug.Log($"  - {element.name}");
        }

        // USAR LA MISMA LÓGICA QUE EL SISTEMA DE AUTO-SELECCIÓN
        if (activeElements.Count > 0)
        {
            Selectable elementToSelect = null;

            // Si isFirstSelected está habilitado, usar la lógica completa de prioridades
            if (isFirstSelected)
            {
                elementToSelect = DetermineElementToSelect(activeElements);
            }
            // Si no, usar solo autoSelectFirstButton como fallback
            else if (autoSelectFirstButton)
            {
                elementToSelect = activeElements[0]; // Tomar el primer elemento activo
                Debug.Log($"AutoSelectFirstButton: seleccionando primer elemento activo: {elementToSelect.name}");
            }

            // Seleccionar el elemento determinado
            if (elementToSelect != null)
            {
                int index = navigableElements.IndexOf(elementToSelect);
                if (index >= 0)
                {
                    Debug.Log($"Seleccionando elemento por defecto: {elementToSelect.name} (índice: {index})");
                    SelectElement(index, true);
                }
                else
                {
                    Debug.LogWarning($"Elemento {elementToSelect.name} no encontrado en navigableElements");
                    // Añadir a la lista si no está
                    navigableElements.Add(elementToSelect);
                    SelectElement(navigableElements.Count - 1, true);
                }
            }
        }
        else
        {
            Debug.Log("No hay elementos activos para seleccionar por defecto");
        }

        Debug.Log($"=== FIN SELECCIÓN POR DEFECTO ===");
    }

    #endregion

    #region Animation Methods

    private void CleanupAnimations()
    {
        Debug.Log("Limpiando animaciones...");

        // Detener todas las animaciones del sistema
        StopAllAnimations();

        // Resetear todas las escalas
        ResetAllElementsToNormalScale();

        // Limpiar referencias
        currentSelected = null;
        previousSelected = null;
    }

    private void ApplySelectionAnimations()
    {
        // PASO 1: Detener TODAS las animaciones existentes
        StopAllAnimations();

        // PASO 2: Resetear TODOS los elementos a escala normal
        ResetAllElementsToNormalScale();

        // PASO 3: Animar SOLO el elemento seleccionado
        if (currentSelected != null)
        {
            AnimateSelectedElement();
        }

        Debug.Log($"Aplicando animación a: {currentSelected?.name}");
    }

    private void StopAllAnimations()
    {
        // Detener animaciones del sistema
        currentAnimationTween?.Kill();
        currentPulseTween?.Kill();

        // Detener TODAS las animaciones DOTween en los elementos navegables
        foreach (var element in navigableElements)
        {
            if (element != null)
            {
                element.transform.DOKill();
            }
        }
    }

    private void ResetAllElementsToNormalScale()
    {
        foreach (var element in navigableElements)
        {
            if (element != null)
            {
                element.transform.localScale = Vector3.one;
            }
        }
    }

    private void AnimateSelectedElement()
    {
        // Asegurar que el elemento empiece en escala normal
        currentSelected.transform.localScale = Vector3.one;

        // Animación principal: escalar hacia el tamaño seleccionado
        currentAnimationTween = currentSelected.transform
            .DOScale(Vector3.one * selectedScale, animationDuration)
            .SetEase(scaleInEase)
            .SetUpdate(true)
            .OnComplete(() => {
                // Una vez completada la animación principal, aplicar efecto de pulso
                if (enablePulseEffect && currentSelected != null && gameObject.activeInHierarchy)
                {
                    ApplyPulseEffect();
                }
            });
    }

    private void ApplyPulseEffect()
    {
        if (currentSelected == null) return;

        // Efecto de pulso sutil que se repite
        Vector3 baseScale = Vector3.one * selectedScale;
        Vector3 pulseScale = baseScale + (Vector3.one * pulseIntensity);

        currentPulseTween = currentSelected.transform
            .DOScale(pulseScale, pulseDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo) // Bucle infinito de ida y vuelta
            .SetUpdate(true);
    }

    #endregion

    #region Synchronization

    private void SyncWithEventSystem()
    {
        // Sincronizar con selección manual del EventSystem (ratón, etc.)
        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected != null)
        {
            Selectable selectable = selected.GetComponent<Selectable>();
            if (selectable != null && navigableElements.Contains(selectable))
            {
                int index = navigableElements.IndexOf(selectable);
                if (index != currentIndex && index >= 0)
                {
                    // Solo actualizar si realmente cambió
                    currentIndex = index;
                    previousSelected = currentSelected;
                    currentSelected = selectable;

                    // Aplicar animaciones para el cambio de selección
                    ApplySelectionAnimations();
                }
            }
        }
        else if (currentSelected != null)
        {
            // Si no hay nada seleccionado en el EventSystem pero nosotros sí tenemos algo seleccionado,
            // restaurar la selección
            eventSystem.SetSelectedGameObject(currentSelected.gameObject);
        }
    }

    #endregion

    #region Audio

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region Public Methods

    // Métodos públicos para gestión de elementos
    public void AddNavigableElement(Selectable element)
    {
        if (!navigableElements.Contains(element))
        {
            navigableElements.Add(element);
            Debug.Log($"Elemento añadido: {element.name}. Total elementos: {navigableElements.Count}");
        }
    }

    public void RemoveNavigableElement(Selectable element)
    {
        navigableElements.Remove(element);
    }

    /// <summary>
    /// Configura los elementos navegables y selecciona el elemento por defecto
    /// Llama a este método después de añadir todos los elementos
    /// </summary>
    public void ConfigureAndSelectDefault()
    {
        Debug.Log($"Configurando navegación con {navigableElements.Count} elementos");

        if (gameObject.activeInHierarchy)
        {
            // Ejecutar la selección por defecto
            StartCoroutine(SelectDefaultElementCoroutine());
        }
    }

    public void RefreshNavigableElements()
    {
        Debug.Log("Refrescando elementos navegables...");

        // Limpiar animaciones antes de actualizar elementos
        CleanupAnimations();

        // Actualizar lista automáticamente buscando en hijos
        navigableElements.Clear();
        Selectable[] selectables = GetComponentsInChildren<Selectable>();

        foreach (var selectable in selectables)
        {
            if (selectable.gameObject.activeInHierarchy && selectable.interactable)
            {
                navigableElements.Add(selectable);
            }
        }

        Debug.Log($"Encontrados {navigableElements.Count} elementos navegables");

        // Configurar y seleccionar elemento por defecto
        ConfigureAndSelectDefault();
    }

    public void SelectElementByName(string elementName)
    {
        for (int i = 0; i < navigableElements.Count; i++)
        {
            if (navigableElements[i].name == elementName)
            {
                SelectElement(i);
                break;
            }
        }
    }

    /// <summary>
    /// CORREGIDO: EnableUINavigation mantiene navegación activa
    /// </summary>
    public void EnableUINavigation()
    {
        Debug.Log("EnableUINavigation llamado - Navegación mantenida activa");

        enabled = true;

        // CORREGIDO: Asegurar que UI esté habilitado
        if (playerControls != null && !playerControls.UI.enabled)
        {
            playerControls.UI.Enable();
        }

        StartAutoSelectionSystem();
    }

    /// <summary>
    /// CORREGIDO: DisableUINavigation ya no desactiva para evitar problemas
    /// </summary>
    public void DisableUINavigation()
    {
        Debug.LogError($"[NAVEGACIÓN DESACTIVADA] DisableUINavigation llamado en: {gameObject.name}");
        Debug.LogError($"STACK TRACE: {System.Environment.StackTrace}");

        // CORREGIDO: NO desactivar navegación para evitar problemas
        Debug.LogWarning("DisableUINavigation IGNORADO para prevenir problemas de navegación");

        // NO hacer nada para mantener la navegación activa
        // enabled = false;
        // playerControls.UI.Disable();
        // StopAutoSelectionSystem();
        // CleanupAnimations();
    }

    public void SetFirstSelected(Selectable element)
    {
        firstSelected = element;

        // Si el componente está activo y el elemento es válido, seleccionarlo inmediatamente
        if (isFirstSelected && gameObject.activeInHierarchy && element != null &&
            IsElementInteractable(element))
        {
            // Asegurar que esté en la lista
            if (!navigableElements.Contains(element))
            {
                navigableElements.Add(element);
            }

            int index = navigableElements.IndexOf(element);
            if (index >= 0)
            {
                SelectElement(index, true);
            }
        }
    }

    public void ForceSelectDefault()
    {
        Debug.Log("Forzando selección por defecto...");
        StartCoroutine(SelectDefaultElementCoroutine());
    }

    public void SetAnimationSettings(float duration, float scale, DG.Tweening.Ease scaleIn, DG.Tweening.Ease scaleOut)
    {
        animationDuration = duration;
        selectedScale = scale;
        scaleInEase = scaleIn;
        scaleOutEase = scaleOut;
    }

    public void SetAnimationSettings(float duration, float scale, DG.Tweening.Ease ease)
    {
        animationDuration = duration;
        selectedScale = scale;
        scaleInEase = ease;
        scaleOutEase = ease;
    }

    public void SetPulseEffect(bool enabled, float intensity = 0.05f, float duration = 1.5f)
    {
        enablePulseEffect = enabled;
        pulseIntensity = intensity;
        pulseDuration = duration;

        // Si estamos deshabilitando el pulso y hay uno activo, cancelarlo
        if (!enabled)
        {
            currentPulseTween?.Kill();
        }
        // Si lo estamos habilitando y hay un elemento seleccionado, aplicarlo
        else if (currentSelected != null)
        {
            ApplyPulseEffect();
        }
    }

    /// <summary>
    /// Habilita o deshabilita el sistema de selección automática
    /// </summary>
    public void SetAutoSelectionEnabled(bool enabled)
    {
        enableAutoSelection = enabled;

        if (enabled)
        {
            StartAutoSelectionSystem();
        }
        else
        {
            StopAutoSelectionSystem();
        }
    }

    /// <summary>
    /// Fuerza una verificación inmediata del sistema de selección automática
    /// </summary>
    public void ForceAutoSelectionCheck()
    {
        if (enableAutoSelection)
        {
            CheckForAutoSelection();
        }
    }

    /// <summary>
    /// Fuerza la selección siguiendo la lógica de prioridades actual
    /// </summary>
    public void ForceSmartSelection()
    {
        List<Selectable> activeElements = GetActiveInteractableElements();
        if (activeElements.Count > 0)
        {
            Selectable elementToSelect = DetermineElementToSelect(activeElements);
            if (elementToSelect != null)
            {
                int index = navigableElements.IndexOf(elementToSelect);
                if (index >= 0)
                {
                    Debug.Log($"Forzando selección inteligente: {elementToSelect.name}");
                    SelectElement(index, true);
                }
            }
        }
        else
        {
            Debug.Log("No hay elementos activos para selección forzada");
            ClearSelection();
        }
    }

    /// <summary>
    /// Bloquea temporalmente los inputs Submit para prevenir doble activación
    /// </summary>
    public void BlockInputTemporarily(float duration = 0.5f)
    {
        StartCoroutine(BlockInputCoroutine(duration));
    }

    private IEnumerator BlockInputCoroutine(float duration)
    {
        Debug.Log($"Bloqueando inputs por {duration} segundos");
        isInputBlocked = true;
        yield return new WaitForSecondsRealtime(duration);
        isInputBlocked = false;
        Debug.Log("Bloqueo de inputs liberado");
    }

    /// <summary>
    /// Configura el cooldown entre inputs Submit
    /// </summary>
    public void SetSubmitCooldown(float cooldown)
    {
        submitCooldown = cooldown;
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Debug State")]
    public void DebugCurrentState()
    {
        Debug.Log($"=== DEBUG {gameObject.name} ===");
        Debug.Log($"GameObject activo: {gameObject.activeInHierarchy}");
        Debug.Log($"Componente habilitado: {enabled}");
        Debug.Log($"PlayerControls: {(playerControls != null ? "OK" : "NULL")}");
        Debug.Log($"UI Controls Enabled: {(playerControls?.UI.enabled ?? false)}");
        Debug.Log($"EventSystem: {(eventSystem != null ? eventSystem.name : "NULL")}");
        Debug.Log($"Elementos navegables: {navigableElements.Count}");
        Debug.Log($"Elemento actual: {currentSelected?.name ?? "NULL"}");
        Debug.Log($"===============================");
    }

    [ContextMenu("Force Smart Selection")]
    public void ForceSmartSelectionFromContext()
    {
        ForceSmartSelection();
    }

    [ContextMenu("Force Auto Selection Check")]
    public void ForceAutoSelectionCheckFromContext()
    {
        ForceAutoSelectionCheck();
    }

    [ContextMenu("Force Select Default")]
    public void ForceSelectDefaultFromContext()
    {
        ForceSelectDefault();
    }

    [ContextMenu("Test Enable UI Navigation")]
    public void TestEnableUINavigation()
    {
        EnableUINavigation();
    }

    [ContextMenu("Test Disable UI Navigation (FIXED)")]
    public void TestDisableUINavigation()
    {
        DisableUINavigation();
    }

    #endregion

    #region Properties

    // Propiedades públicas
    public Selectable CurrentSelected => currentSelected;
    public int CurrentIndex => currentIndex;
    public List<Selectable> NavigableElements => new List<Selectable>(navigableElements);
    public bool AutoSelectionEnabled => enableAutoSelection;

    #endregion
}