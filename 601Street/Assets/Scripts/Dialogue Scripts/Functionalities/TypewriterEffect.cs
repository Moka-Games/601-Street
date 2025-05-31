using UnityEngine;
using System.Collections;
using TMPro;

public class TypewriterEffect : MonoBehaviour
{
    public TMP_Text textComponent;
    public float typeSpeed = 0.05f;
    public AudioSource typingSoundEffect;

    [Header("Configuración de Audio")]
    [Tooltip("Clip de audio para cada letra (opcional - usa el clip del AudioSource si no se asigna)")]
    public AudioClip typeLetterClip;
    [Tooltip("Volumen del sonido de tipeo (0.0 a 1.0)")]
    [Range(0f, 1f)]
    public float typeVolume = 1f;
    [Tooltip("Variación de pitch para hacer el sonido más natural")]
    [Range(0f, 0.5f)]
    public float pitchVariation = 0.1f;

    private string processedText;
    private float timer;
    private int charIndex;
    private Coroutine typingCoroutine;

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
    }

    // Procesa el texto antes de iniciar la animación de escritura
    public void StartTyping(string text, NPC npc)
    {
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

                // Reproducir sonido para cada letra
                PlayTypingSound();

                timer = 0;
            }

            yield return null;
        }

        // Aseguramos que todo el texto sea visible al final
        textComponent.maxVisibleCharacters = int.MaxValue;

        // Notificamos que se ha completado la escritura
        DialogueManager.Instance.OnTypingComplete();
    }

    /// <summary>
    /// Reproduce el sonido de tipeo para cada letra
    /// </summary>
    private void PlayTypingSound()
    {
        if (typingSoundEffect == null) return;

        // Si hay un clip específico asignado, usarlo; si no, usar el clip del AudioSource
        AudioClip clipToPlay = typeLetterClip != null ? typeLetterClip : typingSoundEffect.clip;

        if (clipToPlay != null)
        {
            // Añadir variación de pitch para sonido más natural
            if (pitchVariation > 0f)
            {
                float originalPitch = typingSoundEffect.pitch;
                typingSoundEffect.pitch = originalPitch + Random.Range(-pitchVariation, pitchVariation);

                // PlayOneShot permite múltiples sonidos simultáneos
                typingSoundEffect.PlayOneShot(clipToPlay, typeVolume);

                // Restaurar pitch original
                typingSoundEffect.pitch = originalPitch;
            }
            else
            {
                typingSoundEffect.PlayOneShot(clipToPlay, typeVolume);
            }
        }
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

    #region Métodos adicionales para control avanzado

    /// <summary>
    /// Establece el volumen del sonido de tipeo
    /// </summary>
    public void SetTypeVolume(float volume)
    {
        typeVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Establece la variación de pitch
    /// </summary>
    public void SetPitchVariation(float variation)
    {
        pitchVariation = Mathf.Clamp(variation, 0f, 0.5f);
    }

    /// <summary>
    /// Cambia el clip de audio para el tipeo
    /// </summary>
    public void SetTypeClip(AudioClip newClip)
    {
        typeLetterClip = newClip;
    }

    #endregion
}