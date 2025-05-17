using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// Contiene una serie de funciones para animar textos y elementos UI.
/// </summary>
public class InteractionPromptAnimator : MonoBehaviour
{
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private RectTransform promptRectTransform;

    [Header("Animación")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float scaleUpDuration = 0.4f;
    [SerializeField] private float initialScale = 0.8f;
    [SerializeField] private float finalScale = 1.0f;  // Nuevo parámetro para escala final

    [Header("Elementos Separados")]
    [SerializeField] private RectTransform iconRect;
    [SerializeField] private RectTransform textRect;

    [Tooltip("Determina que animación se realizará al activar el objeto")]
    [Header("Animation Type")]
    [SerializeField] private bool basic_Animation = true;
    [SerializeField] private bool slide_Animation;
    [SerializeField] private bool pulse_Animation;
    [SerializeField] private bool SeparatedElements_Animation;

    // Secuencia para almacenar y gestionar las animaciones activas
    private Sequence activeSequence;
    private bool isAnimating = false;

    private void OnEnable()
    {
        // Detener cualquier animación previa si existe
        KillActiveAnimations();

        // Iniciar la animación correspondiente
        PlaySelectedAnimation();
    }

    private void OnDisable()
    {
        // Asegurarse de detener todas las animaciones cuando el objeto se desactive
        KillActiveAnimations();
    }

    /// <summary>
    /// Reproduce la animación seleccionada según los booleanos configurados
    /// </summary>
    private void PlaySelectedAnimation()
    {
        if (basic_Animation)
            ShowPrompt();
        else if (slide_Animation)
            ShowPromptSlide();
        else if (pulse_Animation)
            ShowPromptWithPulse();
        else if (SeparatedElements_Animation)
            ShowPromptWithSeparateElements();
        else
            // Si ninguna está seleccionada, usar la animación básica por defecto
            ShowPrompt();
    }

    /// <summary>
    /// Detiene todas las animaciones activas
    /// </summary>
    private void KillActiveAnimations()
    {
        // Detener la secuencia principal si existe
        if (activeSequence != null)
        {
            activeSequence.Kill();
            activeSequence = null;
        }

        // Detener cualquier animación en los objetos
        promptRectTransform.DOKill();
        promptCanvasGroup.DOKill();

        if (iconRect != null) iconRect.DOKill();
        if (textRect != null) textRect.DOKill();

        isAnimating = false;
    }

    /// <summary>
    /// Muestra el prompt con una animación de fade in y escala
    /// </summary>
    public void ShowPrompt()
    {
        // Asegurarse de que las propiedades están en su estado inicial
        promptCanvasGroup.alpha = 0f;
        promptRectTransform.localScale = Vector3.one * initialScale;

        // Crear secuencia de animaciones
        activeSequence = DOTween.Sequence();

        // Añadir animaciones a la secuencia
        activeSequence.Append(promptCanvasGroup.DOFade(1f, fadeInDuration));
        activeSequence.Join(promptRectTransform.DOScale(finalScale, scaleUpDuration).SetEase(Ease.OutBack));

        // Marcar que estamos animando
        isAnimating = true;

        // Configurar callback para cuando termine la animación
        activeSequence.OnComplete(() => {
            isAnimating = false;
        });

        // Reproducir secuencia
        activeSequence.Play();
    }

    /// <summary>
    /// Oculta el prompt con una animación de fade out
    /// </summary>
    public void HidePrompt()
    {
        // Detener cualquier animación activa
        KillActiveAnimations();

        // Crear nueva secuencia para fade out
        activeSequence = DOTween.Sequence();

        // Ocultar con fade out
        activeSequence.Append(promptCanvasGroup.DOFade(0f, fadeInDuration));

        // Configurar para desactivar el objeto al completar
        activeSequence.OnComplete(() => {
            gameObject.SetActive(false);
            isAnimating = false;
        });

        activeSequence.Play();
    }

    /// <summary>
    /// Muestra el prompt con una animación deslizante desde abajo
    /// </summary>
    public void ShowPromptSlide()
    {
        // Configuración inicial
        promptCanvasGroup.alpha = 0f;

        // Posición inicial (abajo de su posición final)
        Vector2 finalPosition = promptRectTransform.anchoredPosition;
        promptRectTransform.anchoredPosition = new Vector2(finalPosition.x, finalPosition.y - 50f);

        // Crear secuencia
        activeSequence = DOTween.Sequence();

        // Animar
        activeSequence.Append(promptCanvasGroup.DOFade(1f, 0.25f));
        activeSequence.Join(promptRectTransform.DOAnchorPos(finalPosition, 0.5f).SetEase(Ease.OutBack));

        // Marcar que estamos animando
        isAnimating = true;

        // Configurar callback para cuando termine la animación
        activeSequence.OnComplete(() => {
            isAnimating = false;
        });

        activeSequence.Play();
    }

    /// <summary>
    /// Muestra el prompt con un efecto de pulso continuo después de aparecer
    /// </summary>
    public void ShowPromptWithPulse()
    {
        // Configuración inicial
        promptCanvasGroup.alpha = 0f;
        promptRectTransform.localScale = Vector3.one * initialScale;

        // Crear secuencia principal
        activeSequence = DOTween.Sequence();

        // Animación inicial de aparición
        activeSequence.Append(promptCanvasGroup.DOFade(1f, fadeInDuration));
        activeSequence.Join(promptRectTransform.DOScale(finalScale, scaleUpDuration).SetEase(Ease.OutBack));

        // Marcar que estamos animando
        isAnimating = true;

        // Al terminar la animación inicial, iniciar el pulso
        activeSequence.OnComplete(() => {
            // Crear secuencia de pulso
            Sequence pulseSequence = DOTween.Sequence();

            // Crear pulso sutilmente más grande y volver al tamaño normal
            float pulseScale = finalScale * 1.05f;
            pulseSequence.Append(promptRectTransform.DOScale(pulseScale, 0.5f).SetEase(Ease.InOutSine));
            pulseSequence.Append(promptRectTransform.DOScale(finalScale, 0.5f).SetEase(Ease.InOutSine));

            // Hacer que el pulso se repita indefinidamente
            pulseSequence.SetLoops(-1, LoopType.Restart);

            // Guardar referencia a la secuencia activa
            activeSequence = pulseSequence;

            pulseSequence.Play();
        });

        activeSequence.Play();
    }

    /// <summary>
    /// Anima los elementos del prompt (icono y texto) por separado
    /// </summary>
    public void ShowPromptWithSeparateElements()
    {
        // Verificar que tenemos las referencias necesarias
        if (iconRect == null || textRect == null)
        {
            Debug.LogWarning("No se han asignado referencias para iconRect o textRect. Usando animación básica.");
            ShowPrompt();
            return;
        }

        // Configuración inicial
        promptCanvasGroup.alpha = 1f; // Para que sea visible inmediatamente
        iconRect.localScale = Vector3.zero;
        textRect.localScale = Vector3.zero;

        // Secuencia de animación
        activeSequence = DOTween.Sequence();

        // Animar icono primero
        activeSequence.Append(iconRect.DOScale(finalScale, 0.3f).SetEase(Ease.OutBack));

        // Luego animar texto con un pequeño retraso
        activeSequence.Append(textRect.DOScale(finalScale, 0.3f).SetEase(Ease.OutBack));

        // Marcar que estamos animando
        isAnimating = true;

        // Configurar callback para cuando termine la animación
        activeSequence.OnComplete(() => {
            isAnimating = false;
        });

        activeSequence.Play();
    }
}