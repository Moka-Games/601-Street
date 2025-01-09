using System.Collections;
using UnityEngine;
using TMPro;

public class TypewriterEffect : MonoBehaviour
{
    public float defaultLetterDelay = 0.1f;
    private float letterDelay;
    private TextMeshProUGUI textComponent;
    private Coroutine typingCoroutine;
    private string processedText = "";
    private NPC currentNPC;
    private DialogueManager dialogueManager;

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        letterDelay = defaultLetterDelay;
        dialogueManager = FindAnyObjectByType<DialogueManager>();
    }

    public void StartTyping(string textToWrite, NPC npc)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        letterDelay = defaultLetterDelay;
        currentNPC = npc;
        typingCoroutine = StartCoroutine(ShowText(textToWrite));
    }

    public void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        textComponent.text = processedText;
    }

    private IEnumerator ShowText(string textToWrite)
    {
        int index = 0;
        textComponent.text = "";
        processedText = "";

        while (index < textToWrite.Length)
        {
            if (textToWrite[index] == '<')
            {
                int endIndex = textToWrite.IndexOf('>', index);
                if (endIndex != -1)
                {
                    string tag = textToWrite.Substring(index + 1, endIndex - index - 1);
                    ProcessTag(tag);
                    index = endIndex + 1;
                    continue;
                }
            }
            textComponent.text += textToWrite[index];
            processedText += textToWrite[index];
            index++;
            yield return new WaitForSeconds(letterDelay);
        }

        // Llamamos a OnTypingComplete cuando termine de escribir todo el texto
        if (dialogueManager != null)
        {
            dialogueManager.OnTypingComplete();
        }
    }

    private void ProcessTag(string tag)
    {
        string[] parts = tag.Split('=');
        if (parts.Length == 2)
        {
            string action = parts[0].Trim();
            string parameter = parts[1].Trim();
            switch (action)
            {
                case "speed":
                    float newSpeed;
                    if (float.TryParse(parameter, out newSpeed))
                    {
                        letterDelay = 1f / newSpeed;
                    }
                    break;
                case "pause":
                    float pauseDuration;
                    if (float.TryParse(parameter, out pauseDuration))
                    {
                        StartCoroutine(Pause(pauseDuration));
                    }
                    break;
                case "emotion":
                    ChangeEmotion(parameter);
                    break;
                case "action":
                    ExecuteAction(parameter);
                    break;
                case "size":
                    ChangeTextSize(parameter);
                    break;
            }
        }
    }

    private IEnumerator Pause(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    private void ChangeEmotion(string emotion)
    {
        if (currentNPC != null)
        {
            currentNPC.PerformEmotion(emotion);
        }
    }

    private void ExecuteAction(string action)
    {
        if (currentNPC != null)
        {
            currentNPC.PerformAction(action);
        }
    }

    private void ChangeTextSize(string size)
    {
        float newSize;
        if (float.TryParse(size.Replace("%", ""), out newSize))
        {
            textComponent.fontSize = newSize;
        }
    }
}