using UnityEngine;
using Cinemachine;
using System.Collections;

public class SafeUnlockSequence : MonoBehaviour
{
    [Header("C�maras")]
    [Tooltip("C�mara virtual principal que se usa normalmente")]
    public CinemachineVirtualCamera mainVirtualCamera;

    [Tooltip("C�mara virtual que se activar� durante la secuencia de desbloqueo")]
    public CinemachineVirtualCamera unlockSequenceCamera;

    [Header("Animaci�n")]
    [Tooltip("Animator que controla la puerta de la caja fuerte")]
    public Animator safeDoorAnimator;

    [Tooltip("Nombre del trigger de animaci�n para abrir la puerta")]
    public string openDoorTrigger = "Open";

    [Header("Audio")]
    [Tooltip("Sonido que se reproducir� durante la secuencia de desbloqueo")]
    public AudioClip unlockSequenceSound;

    [Tooltip("Fuente de audio para reproducir el sonido de la secuencia")]
    public AudioSource sequenceAudioSource;

    [Header("Configuraci�n")]
    [Tooltip("Duraci�n total de la secuencia de desbloqueo en segundos")]
    public float sequenceDuration = 3.0f;

    [Tooltip("Prioridad de la c�mara de secuencia (debe ser mayor que la principal)")]
    public int sequenceCameraPriority = 15;

    // Prioridad original de la c�mara principal
    private int mainCameraPriorityOriginal;

    // Referencia al sistema de la caja fuerte
    private SafeSystem safeSystem;

    private void Awake()
    {
        // Obtener referencia al sistema de la caja fuerte
        safeSystem = GetComponent<SafeSystem>();

        if (safeSystem == null)
        {
            Debug.LogError("No se encontr� el componente SafeSystem en el mismo objeto que SafeUnlockSequence.");
        }

        // Validar referencias
        if (mainVirtualCamera == null)
        {
            Debug.LogError("No se ha asignado la c�mara virtual principal.");
        }

        if (unlockSequenceCamera == null)
        {
            Debug.LogError("No se ha asignado la c�mara virtual de la secuencia de desbloqueo.");
        }

        if (safeDoorAnimator == null)
        {
            Debug.LogWarning("No se ha asignado el Animator de la puerta. La animaci�n de apertura no funcionar�.");
        }

        // Configurar la fuente de audio si no est� asignada
        if (sequenceAudioSource == null)
        {
            sequenceAudioSource = GetComponent<AudioSource>();

            if (sequenceAudioSource == null && unlockSequenceSound != null)
            {
                sequenceAudioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("Se ha a�adido autom�ticamente un componente AudioSource para la secuencia de desbloqueo.");
            }
        }

        // Guardar la prioridad original de la c�mara principal
        if (mainVirtualCamera != null)
        {
            mainCameraPriorityOriginal = mainVirtualCamera.Priority;
        }

        // Asegurar que la c�mara de secuencia est� desactivada inicialmente
        if (unlockSequenceCamera != null)
        {
            // Configurar una prioridad baja para asegurar que no est� activa
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

    // M�todo para iniciar la secuencia de desbloqueo
    public void StartUnlockSequence()
    {
        // Iniciar la corrutina de la secuencia
        StartCoroutine(PlayUnlockSequence());
    }

    // Corrutina para la secuencia de desbloqueo
    private IEnumerator PlayUnlockSequence()
    {
        // 1. Activar la c�mara de secuencia
        if (unlockSequenceCamera != null && mainVirtualCamera != null)
        {
            // Aumentar la prioridad de la c�mara de secuencia para que sea la activa
            unlockSequenceCamera.Priority = sequenceCameraPriority;

            // Opcionalmente, reducir a�n m�s la prioridad de la c�mara principal
            mainVirtualCamera.Priority = 0;

            Debug.Log("C�mara de secuencia activada");
        }

        // 2. Reproducir el sonido
        if (sequenceAudioSource != null && unlockSequenceSound != null)
        {
            sequenceAudioSource.clip = unlockSequenceSound;
            sequenceAudioSource.Play();

            Debug.Log("Reproduciendo sonido de secuencia");
        }

        // 3. Activar la animaci�n de apertura de la puerta
        if (safeDoorAnimator != null)
        {
            safeDoorAnimator.SetTrigger(openDoorTrigger);
            Debug.Log("Animaci�n de apertura iniciada");
        }

        // 4. Esperar la duraci�n de la secuencia
        yield return new WaitForSeconds(sequenceDuration);

        // 5. Restaurar la configuraci�n original de las c�maras
        if (unlockSequenceCamera != null && mainVirtualCamera != null)
        {
            // Desactivar ambas c�maras (asumiendo que otra c�mara del juego tomar� el control)
            unlockSequenceCamera.Priority = 0;
            mainVirtualCamera.Priority = 0;

            Debug.Log("Ambas c�maras desactivadas tras secuencia");
        }
    }

    // Para depuraci�n: m�todo para iniciar la secuencia manualmente
    [ContextMenu("Probar Secuencia de Desbloqueo")]
    public void TestUnlockSequence()
    {
        StartUnlockSequence();
    }
}