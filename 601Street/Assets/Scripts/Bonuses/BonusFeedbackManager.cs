using System.Collections;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Gestiona el feedback visual específico para los bonuses en el sistema de dados
/// Se encarga de mostrar claramente cómo se aplica el bonus al resultado
/// </summary>
public class BonusFeedbackManager : MonoBehaviour
{
    [Header("Referencias de UI")]
    [SerializeField] private TMP_Text bonusDisplayText; // El texto "Bonus" de la captura
    [SerializeField] private TMP_Text diceResultText;   // El texto del resultado del dado

    [Header("Configuración de Animaciones")]
    [SerializeField] private float bonusAppearDuration = 0.4f;
    [SerializeField] private float bonusDisplayTime = 1.2f;
    [SerializeField] private float bonusDisappearDuration = 0.3f;
    [SerializeField] private float resultUpdateDelay = 0.2f;

    [Header("Configuración Visual")]
    [SerializeField] private Color bonusTextColor = new Color(0.2f, 0.8f, 0.2f); // Verde
    [SerializeField] private Color resultHighlightColor = new Color(0.3f, 0.9f, 0.3f); // Verde claro
    [SerializeField] private float bonusScalePunch = 0.3f;
    [SerializeField] private float resultScalePunch = 0.2f;

    [Header("Efectos de Partículas (Opcional)")]
    [SerializeField] private GameObject bonusApplyEffect;
    [SerializeField] private Transform effectSpawnPoint;

    // Estado interno
    private Color originalResultColor;
    private bool isShowingBonus = false;

    private void Start()
    {
        InitializeFeedbackSystem();
    }

    private void InitializeFeedbackSystem()
    {
        // Configurar estado inicial del texto de bonus
        if (bonusDisplayText != null)
        {
            bonusDisplayText.gameObject.SetActive(false);
            bonusDisplayText.color = bonusTextColor;
            bonusDisplayText.transform.localScale = Vector3.one;
        }

        // Guardar color original del resultado
        if (diceResultText != null)
        {
            originalResultColor = diceResultText.color;
        }

        Debug.Log("BonusFeedbackManager inicializado");
    }

    /// <summary>
    /// Muestra la secuencia completa del feedback de bonus
    /// </summary>
    /// <param name="baseResult">Resultado base del dado</param>
    /// <param name="bonusValue">Valor del bonus a aplicar</param>
    /// <param name="finalResult">Resultado final (base + bonus)</param>
    public IEnumerator ShowBonusApplicationSequence(int baseResult, int bonusValue, int finalResult)
    {
        if (bonusValue <= 0)
        {
            Debug.Log("No hay bonus que aplicar");
            yield break;
        }

        isShowingBonus = true;

        Debug.Log($"=== INICIANDO FEEDBACK DE BONUS ===");
        Debug.Log($"Base: {baseResult}, Bonus: +{bonusValue}, Final: {finalResult}");

        // PASO 1: Aparecer el texto del bonus
        yield return StartCoroutine(ShowBonusText(bonusValue));

        // PASO 2: Esperar un momento para que el jugador vea el bonus
        yield return new WaitForSeconds(bonusDisplayTime);

        // PASO 3: Aplicar el bonus al resultado
        yield return StartCoroutine(ApplyBonusToResult(baseResult, bonusValue, finalResult));

        // PASO 4: Ocultar el texto del bonus
        yield return StartCoroutine(HideBonusText());

        isShowingBonus = false;

        Debug.Log("=== FEEDBACK DE BONUS COMPLETADO ===");
    }

    /// <summary>
    /// Muestra el texto del bonus con animación
    /// </summary>
    private IEnumerator ShowBonusText(int bonusValue)
    {
        if (bonusDisplayText == null)
        {
            Debug.LogError("bonusDisplayText no está asignado");
            yield break;
        }

        // Configurar el texto
        bonusDisplayText.text = $"+{bonusValue}";
        bonusDisplayText.gameObject.SetActive(true);
        bonusDisplayText.transform.localScale = Vector3.zero;
        bonusDisplayText.color = bonusTextColor;

        Debug.Log($"Mostrando texto de bonus: +{bonusValue}");

        // Animación de aparición con bounce
        Sequence appearSequence = DOTween.Sequence();
        appearSequence.Append(bonusDisplayText.transform.DOScale(1.2f, bonusAppearDuration * 0.7f)
            .SetEase(Ease.OutBack));
        appearSequence.Append(bonusDisplayText.transform.DOScale(1f, bonusAppearDuration * 0.3f)
            .SetEase(Ease.InOutQuad));

        // Efecto de pulso en el color
        bonusDisplayText.DOColor(Color.white, 0.3f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        yield return appearSequence.WaitForCompletion();
    }

    /// <summary>
    /// Aplica visualmente el bonus al resultado del dado
    /// </summary>
    private IEnumerator ApplyBonusToResult(int baseResult, int bonusValue, int finalResult)
    {
        if (diceResultText == null)
        {
            Debug.LogError("diceResultText no está asignado");
            yield break;
        }

        Debug.Log($"Aplicando bonus al resultado: {baseResult} -> {finalResult}");

        // Efecto de partículas si está disponible
        if (bonusApplyEffect != null && effectSpawnPoint != null)
        {
            GameObject effect = Instantiate(bonusApplyEffect, effectSpawnPoint.position, effectSpawnPoint.rotation);
            Destroy(effect, 2f);
        }

        // Animación de transición del resultado
        Sequence resultSequence = DOTween.Sequence();

        // Punch scale para llamar la atención
        resultSequence.Append(diceResultText.transform.DOPunchScale(
            Vector3.one * resultScalePunch, 0.4f, 6, 0.7f));

        // Cambiar color para indicar que se está aplicando el bonus
        resultSequence.Join(diceResultText.DOColor(resultHighlightColor, 0.3f));

        // Esperar un momento
        resultSequence.AppendInterval(resultUpdateDelay);

        // Actualizar el texto al resultado final
        resultSequence.AppendCallback(() => {
            diceResultText.text = finalResult.ToString("00");
        });

        // Animación final de énfasis
        resultSequence.Append(diceResultText.transform.DOPunchScale(
            Vector3.one * (resultScalePunch * 1.5f), 0.5f, 8, 0.8f));

        // Restaurar color original gradualmente
        resultSequence.Join(diceResultText.DOColor(originalResultColor, 0.4f));

        yield return resultSequence.WaitForCompletion();
    }

    /// <summary>
    /// Oculta el texto del bonus con animación
    /// </summary>
    private IEnumerator HideBonusText()
    {
        if (bonusDisplayText == null || !bonusDisplayText.gameObject.activeInHierarchy)
        {
            yield break;
        }

        Debug.Log("Ocultando texto de bonus");

        // Animación de desaparición
        Sequence hideSequence = DOTween.Sequence();

        // Fade out del color
        hideSequence.Append(bonusDisplayText.DOFade(0f, bonusDisappearDuration * 0.5f));

        // Scale down
        hideSequence.Join(bonusDisplayText.transform.DOScale(0.8f, bonusDisappearDuration * 0.7f)
            .SetEase(Ease.InBack));

        // Callback para desactivar el objeto
        hideSequence.OnComplete(() => {
            bonusDisplayText.gameObject.SetActive(false);
            bonusDisplayText.color = bonusTextColor; // Restaurar color para próximo uso
            bonusDisplayText.transform.localScale = Vector3.one; // Restaurar escala
        });

        yield return hideSequence.WaitForCompletion();
    }

    /// <summary>
    /// Muestra solo el feedback básico del bonus (sin secuencia completa)
    /// </summary>
    public void ShowBonusValueQuick(int bonusValue)
    {
        if (bonusValue <= 0 || bonusDisplayText == null) return;

        StartCoroutine(ShowBonusValueQuickCoroutine(bonusValue));
    }

    private IEnumerator ShowBonusValueQuickCoroutine(int bonusValue)
    {
        bonusDisplayText.text = $"+{bonusValue}";
        bonusDisplayText.gameObject.SetActive(true);
        bonusDisplayText.transform.localScale = Vector3.zero;

        // Animación rápida de aparición
        bonusDisplayText.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(0.8f);

        // Animación rápida de desaparición
        bonusDisplayText.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
            .OnComplete(() => bonusDisplayText.gameObject.SetActive(false));
    }

    /// <summary>
    /// Resetea el estado del sistema de feedback
    /// </summary>
    public void ResetFeedback()
    {
        // Detener todas las animaciones
        DOTween.Kill(bonusDisplayText?.transform);
        DOTween.Kill(diceResultText?.transform);
        DOTween.Kill(bonusDisplayText);
        DOTween.Kill(diceResultText);

        // Restaurar estados iniciales
        if (bonusDisplayText != null)
        {
            bonusDisplayText.gameObject.SetActive(false);
            bonusDisplayText.transform.localScale = Vector3.one;
            bonusDisplayText.color = bonusTextColor;
        }

        if (diceResultText != null)
        {
            diceResultText.color = originalResultColor;
            diceResultText.transform.localScale = Vector3.one;
        }

        isShowingBonus = false;

        Debug.Log("Feedback de bonus reseteado");
    }

    /// <summary>
    /// Configura las referencias dinámicamente
    /// </summary>
    public void SetReferences(TMP_Text bonusText, TMP_Text resultText)
    {
        bonusDisplayText = bonusText;
        diceResultText = resultText;

        if (diceResultText != null)
        {
            originalResultColor = diceResultText.color;
        }

        Debug.Log("Referencias del BonusFeedbackManager configuradas dinámicamente");
    }

    /// <summary>
    /// Verifica si hay feedback de bonus en progreso
    /// </summary>
    public bool IsFeedbackActive()
    {
        return isShowingBonus;
    }

    #region Métodos de Configuración

    public void SetBonusTextColor(Color color)
    {
        bonusTextColor = color;
        if (bonusDisplayText != null)
        {
            bonusDisplayText.color = color;
        }
    }

    public void SetResultHighlightColor(Color color)
    {
        resultHighlightColor = color;
    }

    public void SetAnimationDurations(float appear, float display, float disappear)
    {
        bonusAppearDuration = appear;
        bonusDisplayTime = display;
        bonusDisappearDuration = disappear;
    }

    #endregion

    #region Debug

    [ContextMenu("Test Bonus Feedback")]
    public void TestBonusFeedback()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(ShowBonusApplicationSequence(12, 3, 15));
        }
    }

    [ContextMenu("Test Quick Bonus")]
    public void TestQuickBonus()
    {
        if (Application.isPlaying)
        {
            ShowBonusValueQuick(2);
        }
    }

    #endregion

    private void OnDestroy()
    {
        // Limpiar tweens al destruir
        DOTween.Kill(bonusDisplayText?.transform);
        DOTween.Kill(diceResultText?.transform);
        DOTween.Kill(bonusDisplayText);
        DOTween.Kill(diceResultText);
    }
}