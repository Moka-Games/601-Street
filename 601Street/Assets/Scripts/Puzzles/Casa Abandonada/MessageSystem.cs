using System.Collections;
using UnityEngine;
using TMPro;

public class MessageSystem : MonoBehaviour
{
    // Singleton instance
    public static MessageSystem Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;

    [Header("Message Settings")]
    [SerializeField] private float displayTime = 3f;
    [SerializeField] private float fadeTime = 0.5f;
    [SerializeField] private CanvasGroup canvasGroup;

    private Coroutine activeMessageCoroutine;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // If no canvas group is assigned, try to get it
        if (canvasGroup == null && messagePanel != null)
        {
            canvasGroup = messagePanel.GetComponent<CanvasGroup>();

            // If it doesn't exist, add one
            if (canvasGroup == null)
            {
                canvasGroup = messagePanel.AddComponent<CanvasGroup>();
            }
        }

        // Hide the message panel initially
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Subscribe to the PuzzleSystem's OnShowMessage event
        if (PuzzleSystem.Instance != null)
        {
            PuzzleSystem.Instance.OnShowMessage.AddListener(ShowMessage);
        }
    }

    // Show a message to the player
    public void ShowMessage(string message)
    {
        if (string.IsNullOrEmpty(message) || messagePanel == null || messageText == null)
            return;

        // Stop any active message coroutine
        if (activeMessageCoroutine != null)
        {
            StopCoroutine(activeMessageCoroutine);
        }

        // Start a new message coroutine
        activeMessageCoroutine = StartCoroutine(ShowMessageCoroutine(message));
    }

    private IEnumerator ShowMessageCoroutine(string message)
    {
        // Set the message text
        messageText.text = message;

        // Show the panel
        messagePanel.SetActive(true);

        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            float elapsedTime = 0;

            while (elapsedTime < fadeTime)
            {
                canvasGroup.alpha = Mathf.Lerp(0, 1, elapsedTime / fadeTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            canvasGroup.alpha = 1;
        }

        // Wait for the display time
        yield return new WaitForSeconds(displayTime);

        // Fade out
        if (canvasGroup != null)
        {
            float elapsedTime = 0;

            while (elapsedTime < fadeTime)
            {
                canvasGroup.alpha = Mathf.Lerp(1, 0, elapsedTime / fadeTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            canvasGroup.alpha = 0;
        }

        // Hide the panel
        messagePanel.SetActive(false);
        activeMessageCoroutine = null;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (PuzzleSystem.Instance != null)
        {
            PuzzleSystem.Instance.OnShowMessage.RemoveListener(ShowMessage);
        }
    }
}