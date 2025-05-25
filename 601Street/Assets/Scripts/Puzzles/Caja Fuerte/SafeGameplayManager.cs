using UnityEngine;
using Cinemachine;
using System.Collections;

public class SafeGameplayManager : MonoBehaviour
{
    [Header("Cámaras")]
    [Tooltip("Cámara virtual para la interacción con la caja fuerte")]
    public CinemachineVirtualCamera safeVCam;

    [Tooltip("Prioridad que tendrá la cámara al activarse")]
    public int safeCameraPriority = 15;

    [Header("Configuración")]
    [Tooltip("Referencia al sistema de la caja fuerte")]
    public SafeSystem safeSystem;

    [Tooltip("Referencia a la secuencia de desbloqueo (opcional)")]
    public SafeUnlockSequence unlockSequence;

    [Tooltip("Tiempo a esperar después de la secuencia de desbloqueo para volver al gameplay")]
    public float returnToGameplayDelay = 2.0f;

    [Header("Interacción")]
    [Tooltip("Referencia al InteractableObject de la caja fuerte")]
    public InteractableObject safeInteractableObject;

    [Tooltip("¿Desactivar temporalmente la interacción mientras se resuelve la caja fuerte?")]
    public bool disableInteractionDuringSafeMode = true;

    [Header("Player Controller Integration")]
    [Tooltip("Referencia al PlayerController para desactivar movimiento")]
    public PlayerController playerController;

    [Tooltip("¿Desactivar movimiento del jugador en modo safe?")]
    public bool disablePlayerMovement = true;

    [Header("Input Configuration")]
    [Tooltip("¿Habilitar automáticamente la navegación con gamepad?")]
    public bool enableGamepadNavigationInSafeMode = true;

    // Estado actual
    private bool isSafeMode = false;

    // Para controlar el retorno al gameplay después de desbloquear
    private Coroutine returnToGameplayCoroutine;

    // Estados previos del cursor para restaurar
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockState;

    private void Awake()
    {
        // Verificar referencias
        if (safeVCam == null)
        {
            Debug.LogError("No se ha asignado la cámara virtual de la caja fuerte.");
        }
        else
        {
            // Asegurarse de que la cámara esté desactivada al inicio
            safeVCam.gameObject.SetActive(false);
        }

        // Buscar el SafeSystem si no está asignado
        if (safeSystem == null)
        {
            safeSystem = GetComponent<SafeSystem>();
            if (safeSystem == null)
            {
                Debug.LogError("No se encontró el componente SafeSystem.");
            }
        }

        // Buscar la secuencia de desbloqueo si no está asignada
        if (unlockSequence == null && safeSystem != null)
        {
            unlockSequence = GetComponent<SafeUnlockSequence>();
        }

        // Buscar el InteractableObject si no está asignado
        if (safeInteractableObject == null)
        {
            safeInteractableObject = GetComponent<InteractableObject>();
            if (safeInteractableObject == null)
            {
                Debug.LogWarning("No se encontró InteractableObject. La desactivación de interacción no funcionará.");
            }
        }
    }

    private void Start()
    {
        // Suscribirse al evento de desbloqueo si tenemos la referencia
        if (safeSystem != null)
        {
            safeSystem.OnSafeUnlocked.AddListener(HandleSafeUnlocked);
        }

        // Buscar el PlayerController si no está asignado
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
            if (playerController == null && disablePlayerMovement)
            {
                Debug.LogWarning("No se encontró PlayerController. No se podrá desactivar el movimiento del jugador.");
            }
        }
    }

    // Método para activar el modo de interacción con la caja fuerte
    public void EnterSafeMode()
    {
        if (!isSafeMode && safeVCam != null)
        {
            // Cancelar cualquier corrutina de retorno al gameplay si estaba en marcha
            if (returnToGameplayCoroutine != null)
            {
                StopCoroutine(returnToGameplayCoroutine);
                returnToGameplayCoroutine = null;
            }

            // Guardar estado previo del cursor
            previousCursorVisible = Cursor.visible;
            previousCursorLockState = Cursor.lockState;

            // DESACTIVAR MOVIMIENTO DEL JUGADOR
            if (disablePlayerMovement && playerController != null)
            {
                playerController.SetMovementEnabled(false);
                Debug.Log("Movimiento del jugador desactivado para modo caja fuerte");
            }

            // DESACTIVAR INTERACCIÓN CON LA CAJA FUERTE
            if (disableInteractionDuringSafeMode && safeInteractableObject != null)
            {
                safeInteractableObject.enabled = false;
                Debug.Log("InteractableObject desactivado durante modo caja fuerte");
            }

            // Activar la cámara de la caja fuerte
            safeVCam.gameObject.SetActive(true);
            safeVCam.Priority = safeCameraPriority;

            isSafeMode = true;

            // Configurar cursor para interacción con UI
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Activar navegación con gamepad si está habilitado
            if (enableGamepadNavigationInSafeMode && safeSystem != null)
            {
                safeSystem.SetNavigationActive(true);
            }

            Debug.Log("Modo de interacción con caja fuerte activado - Jugador no puede moverse");
        }
    }

    // Método para desactivar el modo de interacción con la caja fuerte
    public void ExitSafeMode()
    {
        Debug.Log("Llamado Exit Safe Mode");

        if (isSafeMode)
        {
            // Desactivar navegación si está activa
            if (safeSystem != null)
            {
                safeSystem.SetNavigationActive(false);
            }

            // Desactivar la cámara de la caja fuerte
            safeVCam.gameObject.SetActive(false);

            // REACTIVAR MOVIMIENTO DEL JUGADOR
            if (disablePlayerMovement && playerController != null)
            {
                playerController.SetMovementEnabled(true);
                Debug.Log("Movimiento del jugador reactivado");
            }

            // REACTIVAR INTERACCIÓN CON LA CAJA FUERTE (solo si no está desbloqueada)
            if (disableInteractionDuringSafeMode && safeInteractableObject != null)
            {
                // Solo reactivar si la caja fuerte no ha sido desbloqueada
                if (safeSystem == null || !safeSystem.IsSafeUnlocked())
                {
                    safeInteractableObject.enabled = true;
                    Debug.Log("InteractableObject reactivado - se puede volver a interactuar");
                }
                else
                {
                    Debug.Log("InteractableObject no reactivado - caja fuerte ya desbloqueada");
                }
            }

            isSafeMode = false;

            // Restaurar estado previo del cursor
            Cursor.visible = previousCursorVisible;
            Cursor.lockState = previousCursorLockState;

            Debug.Log("Modo de interacción con caja fuerte desactivado - Jugador puede moverse nuevamente");
        }
    }

    // Método que se llama cuando se desbloquea la caja fuerte
    private void HandleSafeUnlocked()
    {
        Debug.Log("Caja fuerte desbloqueada - manejando secuencia de salida");

        // DESACTIVAR PERMANENTEMENTE LA INTERACCIÓN (la caja ya está abierta)
        if (safeInteractableObject != null)
        {
            safeInteractableObject.enabled = false;
            Debug.Log("InteractableObject desactivado permanentemente - caja fuerte desbloqueada");
        }

        // Si hay una secuencia de desbloqueo, dejamos que ella maneje las cámaras
        // Si no, programamos el retorno al gameplay después del retraso
        if (unlockSequence == null)
        {
            // Programar el retorno al gameplay
            returnToGameplayCoroutine = StartCoroutine(ReturnToGameplayAfterDelay());
        }
        else
        {
            // La secuencia de desbloqueo ya está gestionando las cámaras,
            // pero aún necesitamos programar el retorno al gameplay
            returnToGameplayCoroutine = StartCoroutine(ReturnToGameplayAfterSequence());
        }
    }

    // Corrutina para volver al gameplay después del retraso
    private IEnumerator ReturnToGameplayAfterDelay()
    {
        // Esperar el tiempo especificado
        yield return new WaitForSeconds(returnToGameplayDelay);

        // Salir del modo de caja fuerte
        ExitSafeMode();

        returnToGameplayCoroutine = null;
    }

    // Corrutina para volver al gameplay después de que termine la secuencia
    private IEnumerator ReturnToGameplayAfterSequence()
    {
        // Esperar a que termine la secuencia (usando la duración definida en SafeUnlockSequence)
        yield return new WaitForSeconds(unlockSequence.sequenceDuration);

        // Desactivar la cámara de la secuencia (si la secuencia usa una cámara diferente)
        if (unlockSequence.unlockSequenceCamera != null)
        {
            unlockSequence.unlockSequenceCamera.gameObject.SetActive(false);
        }

        // Esperar el tiempo adicional después de la secuencia
        yield return new WaitForSeconds(returnToGameplayDelay);

        // Salir del modo de caja fuerte
        ExitSafeMode();

        returnToGameplayCoroutine = null;
    }

    // Para interacción con la caja fuerte desde otros scripts
    public void OnSafeInteracted()
    {
        // Si la caja ya está desbloqueada, no hacemos nada
        if (safeSystem != null && safeSystem.IsSafeUnlocked())
        {
            Debug.Log("La caja fuerte ya está desbloqueada.");
            return;
        }

        // Activar el modo de interacción con la caja fuerte
        EnterSafeMode();
    }

    /// <summary>
    /// Verifica si actualmente estamos en modo caja fuerte
    /// </summary>
    public bool IsInSafeMode()
    {
        return isSafeMode;
    }

    /// <summary>
    /// Método para forzar la activación/desactivación de la navegación
    /// </summary>
    public void SetGamepadNavigationEnabled(bool enabled)
    {
        enableGamepadNavigationInSafeMode = enabled;

        // Si estamos en modo safe, aplicar el cambio inmediatamente
        if (isSafeMode && safeSystem != null)
        {
            safeSystem.SetNavigationActive(enabled);
        }
    }

    /// <summary>
    /// Configura la referencia al PlayerController externamente
    /// </summary>
    public void SetPlayerController(PlayerController controller)
    {
        playerController = controller;
    }

    /// <summary>
    /// Configura la referencia al InteractableObject externamente
    /// </summary>
    public void SetSafeInteractableObject(InteractableObject interactable)
    {
        safeInteractableObject = interactable;
    }

    /// <summary>
    /// Método para reiniciar completamente el estado de la caja fuerte (para pruebas)
    /// </summary>
    [ContextMenu("Reiniciar Estado Completo")]
    public void ResetCompleteState()
    {
        // Salir del modo safe si estamos en él
        if (isSafeMode)
        {
            ExitSafeMode();
        }

        // Reiniciar el sistema de la caja fuerte
        if (safeSystem != null)
        {
            safeSystem.HardResetSafe();
        }

        // Reactivar el InteractableObject
        if (safeInteractableObject != null)
        {
            safeInteractableObject.enabled = true;
        }

        Debug.Log("Estado completo de la caja fuerte reiniciado.");
    }
}