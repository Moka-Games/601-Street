using UnityEngine;
using Cinemachine;
using System.Collections;

public class SafeUnlockSequence : MonoBehaviour
{
    [Header("Cámaras")]
    [Tooltip("Cámara virtual que se activará durante la secuencia de desbloqueo")]
    public CinemachineVirtualCamera unlockSequenceCamera;

    [Header("Animación")]
    [Tooltip("Animator que controla la puerta de la caja fuerte")]
    public Animator safeDoorAnimator;

    [Tooltip("Nombre del trigger de animación para abrir la puerta")]
    public string openDoorTrigger = "Open";

    [Header("Audio")]
    [Tooltip("Sonido que se reproducirá durante la secuencia de desbloqueo")]
    public AudioClip unlockSequenceSound;

    [Tooltip("Fuente de audio para reproducir el sonido de la secuencia")]
    public AudioSource sequenceAudioSource;

    [Header("Configuración")]
    [Tooltip("Duración total de la secuencia de desbloqueo en segundos")]
    public float sequenceDuration = 3.0f;

    [Tooltip("Prioridad de la cámara de secuencia (debe ser mayor que la principal)")]
    public int sequenceCameraPriority = 15;

    // Referencia al sistema de la caja fuerte
    private SafeSystem safeSystem;

    private void Awake()
    {
        // Obtener referencia al sistema de la caja fuerte
        safeSystem = GetComponent<SafeSystem>();

        if (safeSystem == null)
        {
            Debug.LogError("No se encontró el componente SafeSystem en el mismo objeto que SafeUnlockSequence.");
        }

        // Validar referencias
        if (unlockSequenceCamera == null)
        {
            Debug.LogError("No se ha asignado la cámara virtual de la secuencia de desbloqueo.");
        }
        else
        {
            // Asegurarse de que la cámara de secuencia esté desactivada inicialmente
            unlockSequenceCamera.gameObject.SetActive(false);
        }

        if (safeDoorAnimator == null)
        {
            Debug.LogWarning("No se ha asignado el Animator de la puerta. La animación de apertura no funcionará.");
        }

        // Configurar la fuente de audio si no está asignada
        if (sequenceAudioSource == null)
        {
            sequenceAudioSource = GetComponent<AudioSource>();

            if (sequenceAudioSource == null && unlockSequenceSound != null)
            {
                sequenceAudioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("Se ha añadido automáticamente un componente AudioSource para la secuencia de desbloqueo.");
            }
        }
    }

    private void Start()
    {
        // Suscribirse al evento de desbloqueo de la caja fuerte
        if (safeSystem != null)
        {
            safeSystem.OnSafeUnlocked.AddListener(StartUnlockSequence);
        }
    }

    // Método para iniciar la secuencia de desbloqueo
    public void StartUnlockSequence()
    {
        // Iniciar la corrutina de la secuencia
        StartCoroutine(PlayUnlockSequence());
    }

    // Corrutina para la secuencia de desbloqueo
    private IEnumerator PlayUnlockSequence()
    {
        // 1. Activar la cámara de secuencia
        if (unlockSequenceCamera != null)
        {
            // Activar la cámara de secuencia
            unlockSequenceCamera.gameObject.SetActive(true);
            unlockSequenceCamera.Priority = sequenceCameraPriority;

            Debug.Log("Cámara de secuencia activada");
        }

        // 2. Reproducir el sonido
        if (sequenceAudioSource != null && unlockSequenceSound != null)
        {
            sequenceAudioSource.clip = unlockSequenceSound;
            sequenceAudioSource.Play();

            Debug.Log("Reproduciendo sonido de secuencia");
        }

        // 3. Activar la animación de apertura de la puerta
        if (safeDoorAnimator != null)
        {
            safeDoorAnimator.SetTrigger(openDoorTrigger);
            Debug.Log("Animación de apertura iniciada");
        }

        // 4. Esperar la duración de la secuencia
        yield return new WaitForSeconds(sequenceDuration);

        // La desactivación de las cámaras se maneja en SafeGameplayManager
        // después de que esta secuencia termina + un retraso adicional
    }

    // Para depuración: método para iniciar la secuencia manualmente
    [ContextMenu("Probar Secuencia de Desbloqueo")]
    public void TestUnlockSequence()
    {
        StartUnlockSequence();
    }
}