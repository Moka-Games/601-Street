using UnityEngine;
using System.Collections;
using TMPro;

public class TypewriterEffect : MonoBehaviour
{
    public TMP_Text textComponent;
    public float typeSpeed = 0.05f;
    public AudioSource typingSoundEffect;

    private string processedText;
    private float timer;
    private int charIndex;
    private Coroutine typingCoroutine;
    private NPC currentNPC;
    private bool isInitialized = false;

    private void Awake()
    {
        // Aseg�rate de que textComponent est� asignado
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
            if (textComponent == null)
            {
                Debug.LogError("TypewriterEffect no tiene un TextMeshPro asignado");
            }
        }
    }

    // M�todo para reiniciar el componente
    public void Reset()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (textComponent != null)
        {
            textComponent.text = "";
            textComponent.maxVisibleCharacters = 0;
        }

        isInitialized = false;
    }

    // Procesa el texto antes de iniciar la animaci�n de escritura
    public void StartTyping(string text, NPC npc)
    {
        currentNPC = npc;

        // Asegurarse de detener cualquier animaci�n en curso
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // Procesamos el texto ANTES de iniciar la animaci�n
        processedText = TextFormatHelper.ProcessTextTags(text);

        // Aseguramos que el textComponent existe
        if (textComponent == null)
        {
            Debug.LogError("TypewriterEffect.textComponent no est� asignado");
            return;
        }

        // Configuramos el texto inicial
        textComponent.text = processedText;
        textComponent.richText = true;
        textComponent.maxVisibleCharacters = 0;  // Inicialmente no se muestra ning�n car�cter

        // Iniciamos la animaci�n despu�s de un breve delay para asegurar que todo est� configurado
        typingCoroutine = StartCoroutine(TypeText());

        isInitialized = true;
    }

    private IEnumerator TypeText()
    {
        // Peque�o delay para asegurar que todo est� configurado correctamente
        yield return null;

        charIndex = 0;
        timer = 0;

        // Nos aseguramos que el texto se ha procesado correctamente
        textComponent.ForceMeshUpdate();

        while (charIndex < textComponent.textInfo.characterCount)
        {
            timer += Time.deltaTime;

            if (timer >= typeSpeed)
            {
                charIndex++;
                textComponent.maxVisibleCharacters = charIndex;

                // Reproducir sonido de tipeo
                if (typingSoundEffect != null && !typingSoundEffect.isPlaying)
                {
                    typingSoundEffect.Play();
                }

                timer = 0;
            }

            yield return null;
        }

        // Aseguramos que todo el texto sea visible al final
        textComponent.maxVisibleCharacters = int.MaxValue;

        // Notificamos que se ha completado la escritura
        DialogueManager.Instance.OnTypingComplete();
    }

    public void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;

            // Mostrar el texto completo inmediatamente
            if (textComponent != null)
            {
                textComponent.maxVisibleCharacters = int.MaxValue;
            }

            // Notificar que se complet� la escritura
            DialogueManager.Instance.OnTypingComplete();
        }
    }
}