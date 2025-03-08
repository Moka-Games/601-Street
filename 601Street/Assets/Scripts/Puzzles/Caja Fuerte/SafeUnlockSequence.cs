using UnityEngine;
using Cinemachine;
using System.Collections;

public class SafeUnlockSequence : MonoBehaviour
{
    [Header("Cámaras")]
    [Tooltip("Cámara virtual principal que se usa normalmente")]
    public CinemachineVirtualCamera mainVirtualCamera;

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

    // Prioridad original de la cámara principal
    private int mainCameraPriorityOriginal;

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
        if (mainVirtualCamera == null)
        {
            Debug.LogError("No se ha asignado la cámara virtual principal.");
        }

        if (unlockSequenceCamera == null)
        {
            Debug.LogError("No se ha asignado la cámara virtual de la secuencia de desbloqueo.");
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

        // Guardar la prioridad original de la cámara principal
        if (mainVirtualCamera != null)
        {
            mainCameraPriorityOriginal = mainVirtualCamera.Priority;
        }

        // Asegurar que la cámara de secuencia esté desactivada inicialmente
        if (unlockSequenceCamera != null)
        {
            // Configurar una prioridad baja para asegurar que no esté activa
            unlockSequenceCamera.Priority = 0;
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
        if (unlockSequenceCamera != null && mainVirtualCamera != null)
        {
            // Aumentar la prioridad de la cámara de secuencia para que sea la activa
            unlockSequenceCamera.Priority = sequenceCameraPriority;

            // Opcionalmente, reducir aún más la prioridad de la cámara principal
            mainVirtualCamera.Priority = 0;

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

        // 5. Restaurar la configuración original de las cámaras
        if (unlockSequenceCamera != null && mainVirtualCamera != null)
        {
            // Desactivar ambas cámaras (asumiendo que otra cámara del juego tomará el control)
            unlockSequenceCamera.Priority = 0;
            mainVirtualCamera.Priority = 0;

            Debug.Log("Ambas cámaras desactivadas tras secuencia");
        }
    }

    // Para depuración: método para iniciar la secuencia manualmente
    [ContextMenu("Probar Secuencia de Desbloqueo")]
    public void TestUnlockSequence()
    {
        StartUnlockSequence();
    }
}