using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;
    public Image targetImage; // Variable pública para asignar el objeto Image

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
