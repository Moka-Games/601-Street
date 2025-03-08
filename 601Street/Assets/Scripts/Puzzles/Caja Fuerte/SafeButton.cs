using UnityEngine;

// Clase para representar un botón individual de la caja fuerte
public class SafeButton : MonoBehaviour
{
    [Tooltip("Tipo de botón (Número, Enter, Delete, Clear)")]
    public SafeButtonType buttonType = SafeButtonType.Number;

    [Tooltip("Valor numérico del botón (solo para botones de tipo Number)")]
    public string buttonValue = "0";

    [Header("Audio")]
    [Tooltip("¿Usar un sonido personalizado para este botón? Si es falso, se usa el sonido del SafeSystem")]
    public bool useCustomSound = false;

    [Tooltip("Sonido personalizado para este botón específico")]
    public AudioClip customButtonSound;

    [Tooltip("Fuente de audio local (opcional)")]
    public AudioSource localAudioSource;

    [Header("Animación")]
    [Tooltip("Referencia al componente del animador de botón")]
    public SafeButtonAnimator buttonAnimator;

    private void Start()
    {
        // Si queremos usar sonidos personalizados pero no hay fuente de audio, añadirla
        if (useCustomSound && localAudioSource == null)
        {
            localAudioSource = GetComponent<AudioSource>();

            if (localAudioSource == null)
            {
                localAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Obtener o añadir el componente del animador si no está ya asignado
        if (buttonAnimator == null)
        {
            buttonAnimator = GetComponent<SafeButtonAnimator>();

            if (buttonAnimator == null)
            {
                // Buscar en hijos
                buttonAnimator = GetComponentInChildren<SafeButtonAnimator>();
            }

            // Nota: No creamos automáticamente el SafeButtonAnimator
            // porque requiere configuración específica en el editor
        }
    }

    // Método para presionar el botón (llamado desde SafeSystem)
    public void PressButton()
    {
        // Si este botón tiene sonido personalizado, reproducirlo
        if (useCustomSound && customButtonSound != null && localAudioSource != null)
        {
            localAudioSource.clip = customButtonSound;
            localAudioSource.Play();
        }

        // Activar la animación de pulsado
        if (buttonAnimator != null)
        {
            buttonAnimator.TriggerPressAnimation();
        }
    }
}

public enum SafeButtonType
{
    Number,
    Enter,
    Delete,
    Clear
}