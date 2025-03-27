using UnityEngine;

/// <summary>
/// Componente auxiliar para animar el panel de llamada.
/// Puedes usar este script junto con un Animator o implementar tus propias animaciones aqu�.
/// </summary>
public class CallPopupAnimator : MonoBehaviour
{
    [Header("Animaci�n por C�digo")]
    [SerializeField] private bool useCodeAnimation = true;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private float animationDuration = 0.5f;

    [Header("Animaci�n de Entrada")]
    [SerializeField] private Vector2 startPosition = new Vector2(350, 0); // Fuera de la pantalla
    [SerializeField] private Vector2 endPosition = Vector2.zero; // Centro del panel
    [SerializeField] private AnimationCurve entryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Animaci�n de Salida")]
    [SerializeField] private Vector2 exitPosition = new Vector2(350, 0); // Fuera de la pantalla
    [SerializeField] private AnimationCurve exitCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Estado de animaci�n
    private bool isAnimating = false;
    private float animationTime = 0f;
    private Vector2 currentStartPos;
    private Vector2 currentEndPos;
    private AnimationCurve currentCurve;
    private bool isEntering = true;

    private void Awake()
    {
        // Obtener el RectTransform si no est� asignado
        if (panelRect == null)
        {
            panelRect = GetComponent<RectTransform>();
        }
    }

    private void OnEnable()
    {
        if (useCodeAnimation)
        {
            // Configurar animaci�n de entrada
            StartEntryAnimation();
        }
    }

    private void Update()
    {
        if (!useCodeAnimation || !isAnimating)
            return;

        // Actualizar tiempo de animaci�n
        animationTime += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(animationTime / animationDuration);

        // Aplicar curva de animaci�n
        float curveValue = currentCurve.Evaluate(normalizedTime);

        // Interpolar posici�n
        panelRect.anchoredPosition = Vector2.Lerp(currentStartPos, currentEndPos, curveValue);

        // Finalizar animaci�n
        if (normalizedTime >= 1f)
        {
            isAnimating = false;

            // Si estamos saliendo, desactivar el objeto
            if (!isEntering)
            {
                gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Inicia la animaci�n de entrada.
    /// </summary>
    public void StartEntryAnimation()
    {
        if (!useCodeAnimation)
            return;

        // Configurar animaci�n
        isEntering = true;
        animationTime = 0f;
        currentStartPos = startPosition;
        currentEndPos = endPosition;
        currentCurve = entryCurve;

        // Colocar en posici�n inicial
        panelRect.anchoredPosition = currentStartPos;

        // Iniciar animaci�n
        isAnimating = true;
    }

    /// <summary>
    /// Inicia la animaci�n de salida.
    /// </summary>
    public void StartExitAnimation()
    {
        if (!useCodeAnimation)
            return;

        // Configurar animaci�n
        isEntering = false;
        animationTime = 0f;
        currentStartPos = panelRect.anchoredPosition;
        currentEndPos = exitPosition;
        currentCurve = exitCurve;

        // Iniciar animaci�n
        isAnimating = true;
    }

    /// <summary>
    /// M�todo para usar desde un bot�n o evento.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        StartEntryAnimation();
    }

    /// <summary>
    /// M�todo para usar desde un bot�n o evento.
    /// </summary>
    public void Hide()
    {
        StartExitAnimation();
    }
}