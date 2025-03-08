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
        // Asegúrate de que textComponent está asignado
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
            if (textComponent == null)
            {
                Debug.LogError("TypewriterEffect no tiene un TextMeshPro asignado");
            }
        }
    }

    // Método para reiniciar el componente
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

    // Procesa el texto antes de iniciar la animación de escritura
    public void StartTyping(string text, NPC npc)
    {
        currentNPC = npc;

        // Asegurarse de detener cualquier animación en curso
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // Procesamos el texto ANTES de iniciar la animación
        processedText = TextFormatHelper.ProcessTextTags(text);

        // Aseguramos que el textComponent existe
        if (textComponent == null)
        {
            Debug.LogError("TypewriterEffect.textComponent no está asignado");
            return;
        }

        // Configuramos el texto inicial
        textComponent.text = processedText;
        textComponent.richText = true;
        textComponent.maxVisibleCharacters = 0;  // Inicialmente no se muestra ningún carácter

        // Iniciamos la animación después de un breve delay para asegurar que todo esté configurado
        typingCoroutine = StartCoroutine(TypeText());

        isInitialized = true;
    }

    private IEnumerator TypeText()
    {
        // Pequeño delay para asegurar que todo esté configurado correctamente
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

            // Notificar que se completó la escritura
            DialogueManager.Instance.OnTypingComplete();
        }
    }
}