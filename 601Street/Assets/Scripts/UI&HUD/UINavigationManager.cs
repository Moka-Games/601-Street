using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// Maneja la navegaci�n por UI usando el nuevo Input System de Unity
/// Sistema reutilizable para cualquier men�
/// </summary>
public class UINavigationManager : MonoBehaviour
{
    [Header("Configuraci�n")]
    [SerializeField] private float navigationDelay = 0.15f;
    [SerializeField] private bool autoSelectFirstButton = true;
    [SerializeField] private AudioClip navigationSound;
    [SerializeField] private AudioClip selectSound;

    [Header("Selecci�n por Defecto")]
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

    // Estado de navegaci�n
    private int currentIndex = 0;
    private Selectable currentSelected;
    private Selectable previousSelected;
    private EventSystem eventSystem;
    private AudioSource audioSource;

    // Animaciones
    private Tween currentAnimationTween;
    private Tween currentPulseTween;

    // Eventos
    public System.Action<Selectable> OnElementSelected;
    public System.Action<Selectable> OnElementSubmitted;
    public System.Action OnCancelled;

    private void Awake()
    {
        // Inicializar componentes
        eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystem = eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
        }

        audioSource = GetComponent<AudioSource>();

        // Configurar controles
        playerControls = new PlayerControls();
        SetupInputActions();
    }

    private void OnEnable()
    {
        playerControls.UI.Enable();

        // Limpiar estado anterior
        CleanupAnimations();

        // Esperar un frame para que todo est� inicializado
        StartCoroutine(SelectDefaultElementCoroutine());
    }

    private System.Collections.IEnumerator SelectDefaultElementCoroutine()
    {
        yield return null; // Esperar un frame

        // Asegurar que todos los elementos est�n en escala normal
        ResetAllElementsToNormalScale();

        Debug.Log($"Intentando seleccionar elemento por defecto. FirstSelected: {firstSelected?.name}, IsFirstSelected: {isFirstSelected}");
        Debug.Log($"Elementos navegables disponibles: {navigableElements.Count}");

        // Seleccionar elemento por defecto
        if (isFirstSelected && firstSelected != null)
        {
            // Verificar si el elemento est� activo e interactuable
            if (firstSelected.gameObject.activeInHierarchy && firstSelected.interactable)
            {
                // Buscar el �ndice del elemento seleccionado en la lista
                int index = navigableElements.IndexOf(firstSelected);

                if (index >= 0)
                {
                    Debug.Log($"Elemento encontrado en �ndice {index}, seleccionando...");
                    SelectElement(index);
                }
                else
                {
                    Debug.Log($"Elemento no encontrado en la lista, a�adi�ndolo...");
                    // Si no est� en la lista, a�adirlo y seleccionarlo
                    navigableElements.Add(firstSelected);
                    SelectElement(navigableElements.Count - 1);
                }
            }
            else
            {
                Debug.LogWarning($"FirstSelected ({firstSelected.name}) no est� activo o no es interactuable");
                // Fallback al primer elemento disponible
                if (autoSelectFirstButton && navigableElements.Count > 0)
                {
                    SelectElement(0);
                }
            }
        }
        else if (autoSelectFirstButton && navigableElements.Count > 0)
        {
            Debug.Log("Usando autoSelectFirstButton, seleccionando primer elemento");
            SelectElement(0);
        }
        else
        {
            Debug.Log("No se seleccion� ning�n elemento por defecto");
        }
    }

    private void OnDisable()
    {
        playerControls.UI.Disable();

        // Limpiar animaciones
        CleanupAnimations();
    }

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

    private void OnDestroy()
    {
        CleanupAnimations();
        playerControls?.Dispose();
    }

    private void SetupInputActions()
    {
        // Configurar callbacks de input
        playerControls.UI.Navigate.performed += OnNavigate;
        playerControls.UI.Submit.performed += OnSubmit;
        playerControls.UI.Cancel.performed += OnCancel;
    }

    private void Update()
    {
        // Actualizar input de navegaci�n
        navigationInput = playerControls.UI.Navigate.ReadValue<Vector2>();

        // Procesar navegaci�n si ha pasado suficiente tiempo
        if (Time.time - lastNavigationTime >= navigationDelay)
        {
            ProcessNavigation();
        }

        // Mantener selecci�n sincronizada con EventSystem
        SyncWithEventSystem();
    }

    private void ProcessNavigation()
    {
        if (navigationInput.magnitude < 0.3f || navigableElements.Count == 0) return;

        int newIndex = currentIndex;

        // Navegaci�n horizontal
        if (Mathf.Abs(navigationInput.x) > Mathf.Abs(navigationInput.y))
        {
            if (navigationInput.x > 0) // Derecha
                newIndex = GetNextValidIndex(currentIndex, 1);
            else // Izquierda  
                newIndex = GetNextValidIndex(currentIndex, -1);
        }
        // Navegaci�n vertical
        else
        {
            if (navigationInput.y > 0) // Arriba
                newIndex = GetNextValidIndex(currentIndex, -1);
            else // Abajo
                newIndex = GetNextValidIndex(currentIndex, 1);
        }

        if (newIndex != currentIndex)
        {
            SelectElement(newIndex);
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

            if (navigableElements[index] != null &&
                navigableElements[index].gameObject.activeInHierarchy &&
                navigableElements[index].interactable)
            {
                return index;
            }
        }
        while (attempts < navigableElements.Count);

        return startIndex; // Si no encuentra ninguno v�lido, mantiene el actual
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        // La navegaci�n se maneja en Update para mayor control
    }

    private void OnSubmit(InputAction.CallbackContext context)
    {
        if (currentSelected != null)
        {
            // Simular click en el bot�n actual
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
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        OnCancelled?.Invoke();
        PlaySound(navigationSound);
    }

    private void SelectElement(int index)
    {
        if (index < 0 || index >= navigableElements.Count) return;

        Selectable element = navigableElements[index];
        if (element == null || !element.gameObject.activeInHierarchy || !element.interactable)
            return;

        // Actualizar selecci�n
        previousSelected = currentSelected;
        currentIndex = index;
        currentSelected = element;

        // Actualizar EventSystem
        eventSystem.SetSelectedGameObject(element.gameObject);

        // SIEMPRE aplicar animaciones al cambiar selecci�n
        ApplySelectionAnimations();

        // Efectos sonoros
        PlaySound(navigationSound);
        OnElementSelected?.Invoke(currentSelected);

        Debug.Log($"Elemento seleccionado: {element.name} (�ndice: {index})");
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

        Debug.Log($"Aplicando animaci�n a: {currentSelected?.name}");
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

        // Animaci�n principal: escalar hacia el tama�o seleccionado
        currentAnimationTween = currentSelected.transform
            .DOScale(Vector3.one * selectedScale, animationDuration)
            .SetEase(scaleInEase)
            .SetUpdate(true)
            .OnComplete(() => {
                // Una vez completada la animaci�n principal, aplicar efecto de pulso
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

    private void ResetAllElementsScale()
    {
        // Resetear todos los elementos navegables a escala normal
        foreach (var element in navigableElements)
        {
            if (element != null && element != currentSelected)
            {
                // Detener cualquier animaci�n en curso y resetear escala
                element.transform.DOKill();
                element.transform.localScale = Vector3.one;
            }
        }
    }

    private void SyncWithEventSystem()
    {
        // Sincronizar con selecci�n manual del EventSystem (rat�n, etc.)
        GameObject selected = eventSystem.currentSelectedGameObject;
        if (selected != null)
        {
            Selectable selectable = selected.GetComponent<Selectable>();
            if (selectable != null && navigableElements.Contains(selectable))
            {
                int index = navigableElements.IndexOf(selectable);
                if (index != currentIndex && index >= 0)
                {
                    // Solo actualizar si realmente cambi�
                    currentIndex = index;
                    previousSelected = currentSelected;
                    currentSelected = selectable;

                    // Aplicar animaciones para el cambio de selecci�n
                    ApplySelectionAnimations();
                }
            }
        }
        else if (currentSelected != null)
        {
            // Si no hay nada seleccionado en el EventSystem pero nosotros s� tenemos algo seleccionado,
            // restaurar la selecci�n
            eventSystem.SetSelectedGameObject(currentSelected.gameObject);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // M�todos p�blicos para gesti�n de elementos
    public void AddNavigableElement(Selectable element)
    {
        if (!navigableElements.Contains(element))
        {
            navigableElements.Add(element);
            Debug.Log($"Elemento a�adido: {element.name}. Total elementos: {navigableElements.Count}");
        }
    }

    public void RemoveNavigableElement(Selectable element)
    {
        navigableElements.Remove(element);
    }

    /// <summary>
    /// Configura los elementos navegables y selecciona el elemento por defecto
    /// Llama a este m�todo despu�s de a�adir todos los elementos
    /// </summary>
    public void ConfigureAndSelectDefault()
    {
        Debug.Log($"Configurando navegaci�n con {navigableElements.Count} elementos");

        if (gameObject.activeInHierarchy)
        {
            // Ejecutar la selecci�n por defecto
            StartCoroutine(SelectDefaultElementCoroutine());
        }
    }

    public void RefreshNavigableElements()
    {
        Debug.Log("Refrescando elementos navegables...");

        // Limpiar animaciones antes de actualizar elementos
        CleanupAnimations();

        // Actualizar lista autom�ticamente buscando en hijos
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

    // M�todo de debug para verificar estado
    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log($"=== UI Navigation State ===");
        Debug.Log($"Current Selected: {currentSelected?.name ?? "NULL"}");
        Debug.Log($"Current Index: {currentIndex}");
        Debug.Log($"First Selected: {firstSelected?.name ?? "NULL"}");
        Debug.Log($"Is First Selected: {isFirstSelected}");
        Debug.Log($"Total Elements: {navigableElements.Count}");
        Debug.Log($"GameObject Active: {gameObject.activeInHierarchy}");

        for (int i = 0; i < navigableElements.Count; i++)
        {
            var element = navigableElements[i];
            Debug.Log($"  [{i}] {element?.name ?? "NULL"} - Scale: {element?.transform.localScale ?? Vector3.zero}");
        }
    }

    [ContextMenu("Force Select Default")]
    public void ForceSelectDefaultFromContext()
    {
        ForceSelectDefault();
    }

    [ContextMenu("Configure And Select Default")]
    public void ConfigureAndSelectDefaultFromContext()
    {
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

    public void EnableUINavigation()
    {
        enabled = true;
        playerControls.UI.Enable();
    }

    public void DisableUINavigation()
    {
        enabled = false;
        playerControls.UI.Disable();
        CleanupAnimations();
    }

    public void SetFirstSelected(Selectable element)
    {
        firstSelected = element;

        // Si el componente est� activo y el elemento es v�lido, seleccionarlo inmediatamente
        if (isFirstSelected && gameObject.activeInHierarchy && element != null &&
            element.gameObject.activeInHierarchy && element.interactable)
        {
            // Asegurar que est� en la lista
            if (!navigableElements.Contains(element))
            {
                navigableElements.Add(element);
            }

            int index = navigableElements.IndexOf(element);
            if (index >= 0)
            {
                SelectElement(index);
            }
        }
    }

    public void ForceSelectDefault()
    {
        Debug.Log("Forzando selecci�n por defecto...");
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

    // Propiedades p�blicas
    public Selectable CurrentSelected => currentSelected;
    public int CurrentIndex => currentIndex;
    public List<Selectable> NavigableElements => new List<Selectable>(navigableElements);
}