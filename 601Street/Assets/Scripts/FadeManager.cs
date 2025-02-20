using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;
    public Image targetImage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void FadeIn(float duration)
    {
        if (targetImage != null)
        {
            StartCoroutine(FadeImageRoutine(targetImage, 0f, 1f, duration));
        }
        else
        {
            Debug.LogWarning("FadeManager: targetImage no está asignado.");
        }
    }

    public void FadeOut(float duration)
    {
        if (targetImage != null)
        {
            StartCoroutine(FadeImageRoutine(targetImage, 1f, 0f, duration));
        }
        else
        {
            Debug.LogWarning("FadeManager: targetImage no está asignado.");
        }
    }

    public void BlackScreenIntoFadeOut(float duration)
    {
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
            FadeOut(1f);
        }
        else
        {
            Debug.LogWarning("FadeManager: targetImage no está asignado.");
        }
    }

    private IEnumerator FadeImageRoutine(Image image, float startAlpha, float endAlpha, float duration)
    {
        float time = 0;
        Color color = image.color;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            image.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        image.color = new Color(color.r, color.g, color.b, endAlpha);
    }
}
