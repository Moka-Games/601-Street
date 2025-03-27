using UnityEngine;

/// <summary>
/// Componente auxiliar para animar el panel de llamada.
/// Puedes usar este script junto con un Animator o implementar tus propias animaciones aquí.
/// </summary>
public class CallPopupAnimator : MonoBehaviour
{
    [Header("Animación por Código")]
    [SerializeField] private bool useCodeAnimation = true;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private float animationDuration = 0.5f;

    [Header("Animación de Entrada")]
    [SerializeField] private Vector2 startPosition = new Vector2(350, 0); // Fuera de la pantalla
    [SerializeField] private Vector2 endPosition = Vector2.zero; // Centro del panel
    [SerializeField] private AnimationCurve entryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Animación de Salida")]
    [SerializeField] private Vector2 exitPosition = new Vector2(350, 0); // Fuera de la pantalla
    [SerializeField] private AnimationCurve exitCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Estado de animación
    private bool isAnimating = false;
    private float animationTime = 0f;
    private Vector2 currentStartPos;
    private Vector2 currentEndPos;
    private AnimationCurve currentCurve;
    private bool isEntering = true;

    private void Awake()
    {
        // Obtener el RectTransform si no está asignado
        if (panelRect == null)
        {
            panelRect = GetComponent<RectTransform>();
        }
    }

    private void OnEnable()
    {
        if (useCodeAnimation)
        {
            // Configurar animación de entrada
            StartEntryAnimation();
        }
    }

    private void Update()
    {
        if (!useCodeAnimation || !isAnimating)
            return;

        // Actualizar tiempo de animación
        animationTime += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(animationTime / animationDuration);

        // Aplicar curva de animación
        float curveValue = currentCurve.Evaluate(normalizedTime);

        // Interpolar posición
        panelRect.anchoredPosition = Vector2.Lerp(currentStartPos, currentEndPos, curveValue);

        // Finalizar animación
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
    /// Inicia la animación de entrada.
    /// </summary>
    public void StartEntryAnimation()
    {
        if (!useCodeAnimation)
            return;

        // Configurar animación
        isEntering = true;
        animationTime = 0f;
        currentStartPos = startPosition;
        currentEndPos = endPosition;
        currentCurve = entryCurve;

        // Colocar en posición inicial
        panelRect.anchoredPosition = currentStartPos;

        // Iniciar animación
        isAnimating = true;
    }

    /// <summary>
    /// Inicia la animación de salida.
    /// </summary>
    public void StartExitAnimation()
    {
        if (!useCodeAnimation)
            return;

        // Configurar animación
        isEntering = false;
        animationTime = 0f;
        currentStartPos = panelRect.anchoredPosition;
        currentEndPos = exitPosition;
        currentCurve = exitCurve;

        // Iniciar animación
        isAnimating = true;
    }

    /// <summary>
    /// Método para usar desde un botón o evento.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        StartEntryAnimation();
    }

    /// <summary>
    /// Método para usar desde un botón o evento.
    /// </summary>
    public void Hide()
    {
        StartExitAnimation();
    }
}