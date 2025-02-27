using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;
    public Image targetImage;

    [Header("Configuración")]
    [SerializeField] private float defaultFadeDuration = 1.0f;

    private Coroutine currentFadeCoroutine;
    private bool isFading = false;

    public event Action OnFadeInComplete;
    public event Action OnFadeOutComplete;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Asegurar que la imagen comienza completamente negra (alpha = 1)
            if (targetImage != null)
            {
                Color color = targetImage.color;
                targetImage.color = new Color(color.r, color.g, color.b, 1f);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void FadeIn(float duration = -1)
    {
        if (duration < 0) duration = defaultFadeDuration;

        if (targetImage != null)
        {
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }

            currentFadeCoroutine = StartCoroutine(FadeImageRoutine(targetImage, 0f, 1f, duration));
        }
        else
        {
            Debug.LogWarning("FadeManager: targetImage no está asignado.");
        }
    }

    public void FadeOut(float duration = -1)
    {
        if (duration < 0) duration = defaultFadeDuration;

        if (targetImage != null)
        {
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }

            currentFadeCoroutine = StartCoroutine(FadeImageRoutine(targetImage, 1f, 0f, duration));
        }
        else
        {
            Debug.LogWarning("FadeManager: targetImage no está asignado.");
        }
    }

    public void BlackScreenIntoFadeOut(float duration = -1)
    {
        if (duration < 0) duration = defaultFadeDuration;
        StartCoroutine(BlackScreenForDuration(duration));
    }

    public IEnumerator BlackScreenForDuration(float duration)
    {
        if (targetImage != null)
        {
            // Poner la imagen completamente negra inmediatamente
            Color color = targetImage.color;
            targetImage.color = new Color(color.r, color.g, color.b, 1f);

            // Esperar el tiempo especificado
            yield return new WaitForSeconds(duration);

            // Realizar Fade-Out después de la espera
            FadeOut();
        }
        else
        {
            Debug.LogWarning("FadeManager: targetImage no está asignado.");
        }
    }

    public IEnumerator FadeInAndWait(float duration = -1)
    {
        if (duration < 0) duration = defaultFadeDuration;

        isFading = true;
        FadeIn(duration);
        yield return new WaitForSeconds(duration);
        isFading = false;
    }

    public IEnumerator FadeOutAndWait(float duration = -1)
    {
        if (duration < 0) duration = defaultFadeDuration;

        isFading = true;
        FadeOut(duration);
        yield return new WaitForSeconds(duration);
        isFading = false;
    }

    private IEnumerator FadeImageRoutine(Image image, float startAlpha, float endAlpha, float duration)
    {
        isFading = true;
        float time = 0;
        Color color = image.color;

        // Establecer el alpha inicial
        image.color = new Color(color.r, color.g, color.b, startAlpha);

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            image.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        // Asegurar que llegamos exactamente al valor final
        image.color = new Color(color.r, color.g, color.b, endAlpha);

        isFading = false;

        // Disparar eventos según el tipo de fade
        if (endAlpha == 1f)
        {
            if (OnFadeInComplete != null)
            {
                Debug.Log("Fade In completado - Invocando evento");
                OnFadeInComplete.Invoke();
            }
        }
        else if (endAlpha == 0f)
        {
            if (OnFadeOutComplete != null)
            {
                Debug.Log("Fade Out completado - Invocando evento");
                OnFadeOutComplete.Invoke();
            }
        }
    }

    public bool IsFading()
    {
        return isFading;
    }
}