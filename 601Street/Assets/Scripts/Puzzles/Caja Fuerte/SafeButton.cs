using UnityEngine;

// Clase para representar un bot�n individual de la caja fuerte
public class SafeButton : MonoBehaviour
{
    [Tooltip("Tipo de bot�n (N�mero, Enter, Delete, Clear)")]
    public SafeButtonType buttonType = SafeButtonType.Number;

    [Tooltip("Valor num�rico del bot�n (solo para botones de tipo Number)")]
    public string buttonValue = "0";

    [Header("Audio")]
    [Tooltip("�Usar un sonido personalizado para este bot�n? Si es falso, se usa el sonido del SafeSystem")]
    public bool useCustomSound = false;

    [Tooltip("Sonido personalizado para este bot�n espec�fico")]
    public AudioClip customButtonSound;

    [Tooltip("Fuente de audio local (opcional)")]
    public AudioSource localAudioSource;

    [Header("Animaci�n")]
    [Tooltip("Referencia al componente del animador de bot�n")]
    public SafeButtonAnimator buttonAnimator;

    private void Start()
    {
        // Si queremos usar sonidos personalizados pero no hay fuente de audio, a�adirla
        if (useCustomSound && localAudioSource == null)
        {
            localAudioSource = GetComponent<AudioSource>();

            if (localAudioSource == null)
            {
                localAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Obtener o a�adir el componente del animador si no est� ya asignado
        if (buttonAnimator == null)
        {
            buttonAnimator = GetComponent<SafeButtonAnimator>();

            if (buttonAnimator == null)
            {
                // Buscar en hijos
                buttonAnimator = GetComponentInChildren<SafeButtonAnimator>();
            }

            // Nota: No creamos autom�ticamente el SafeButtonAnimator
            // porque requiere configuraci�n espec�fica en el editor
        }
    }

    // M�todo para presionar el bot�n (llamado desde SafeSystem)
    public void PressButton()
    {
        // Si este bot�n tiene sonido personalizado, reproducirlo
        if (useCustomSound && customButtonSound != null && localAudioSource != null)
        {
            localAudioSource.clip = customButtonSound;
            localAudioSource.Play();
        }

        // Activar la animaci�n de pulsado
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