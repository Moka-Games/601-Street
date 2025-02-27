using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject interactionPromptPanel;
    [SerializeField] private TMPro.TextMeshProUGUI interactionPromptText;

    public void ShowInteractionPrompt(string promptText)
    {
        if (interactionPromptPanel)
        {
            interactionPromptPanel.SetActive(true);

            if (interactionPromptText)
            {
                interactionPromptText.text = promptText;
            }
        }
    }

    public void HideInteractionPrompt()
    {
        if (interactionPromptPanel)
        {
            interactionPromptPanel.SetActive(false);
        }
    }
}