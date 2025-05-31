using UnityEngine;
using TMPro;
using DG.Tweening;

public class WarningText : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float moveDistance = 50f;
    [SerializeField] private Ease fadeInEase = Ease.OutQuad;
    [SerializeField] private Ease fadeOutEase = Ease.InQuad;

    private TMP_Text textComponent;
    private RectTransform rectTransform;
    private Vector2 initialPosition;
    private Sequence activeSequence;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
        rectTransform = GetComponent<RectTransform>();

        if (textComponent == null)
        {
            Debug.LogError("WarningText requires a TextMeshPro - Text (UI) component!");
            enabled = false;
            return;
        }

        // Hide initially
        textComponent.alpha = 0f;
        gameObject.SetActive(false);

        // Store the initial position
        initialPosition = rectTransform.anchoredPosition;
    }

    public void ShowWarning(string message = "")
    {
        // If a message is provided, update the text
        if (!string.IsNullOrEmpty(message))
        {
            textComponent.text = message;
        }

        // Kill any active animations
        if (activeSequence != null && activeSequence.IsActive())
        {
            activeSequence.Kill();
        }

        // Make sure the object is active
        gameObject.SetActive(true);

        // Reset position and alpha
        rectTransform.anchoredPosition = initialPosition - new Vector2(0, moveDistance);
        textComponent.alpha = 0f;

        // Create the animation sequence
        activeSequence = DOTween.Sequence();

        // Fade in and move up
        activeSequence.Append(textComponent.DOFade(1f, fadeInDuration).SetEase(fadeInEase));
        activeSequence.Join(rectTransform.DOAnchorPosY(initialPosition.y, fadeInDuration).SetEase(fadeInEase));

        // Hold
        activeSequence.AppendInterval(displayDuration);

        // Fade out and move up
        activeSequence.Append(textComponent.DOFade(0f, fadeOutDuration).SetEase(fadeOutEase));
        activeSequence.Join(rectTransform.DOAnchorPosY(initialPosition.y + moveDistance, fadeOutDuration).SetEase(fadeOutEase));

        // Deactivate when done
        activeSequence.OnComplete(() => {
            gameObject.SetActive(false);
            rectTransform.anchoredPosition = initialPosition;
        });

        // Play the sequence
        activeSequence.Play();
    }

    private void OnDestroy()
    {
        // Clean up DOTween animations
        if (activeSequence != null && activeSequence.IsActive())
        {
            activeSequence.Kill();
        }
    }
}