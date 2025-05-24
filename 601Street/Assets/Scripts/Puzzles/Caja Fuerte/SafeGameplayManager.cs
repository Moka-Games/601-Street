using UnityEngine;
using Cinemachine;
using System.Collections;

public class SafeGameplayManager : MonoBehaviour
{
    [Header("C�maras")]
    [Tooltip("C�mara virtual para la interacci�n con la caja fuerte")]
    public CinemachineVirtualCamera safeVCam;

    [Tooltip("Prioridad que tendr� la c�mara al activarse")]
    public int safeCameraPriority = 15;

    [Header("Configuraci�n")]
    [Tooltip("Referencia al sistema de la caja fuerte")]
    public SafeSystem safeSystem;

    [Tooltip("Referencia a la secuencia de desbloqueo (opcional)")]
    public SafeUnlockSequence unlockSequence;

    [Tooltip("Tiempo a esperar despu�s de la secuencia de desbloqueo para volver al gameplay")]
    public float returnToGameplayDelay = 2.0f;

    [Header("Interacci�n")]
    [Tooltip("Referencia al InteractableObject de la caja fuerte")]
    public InteractableObject safeInteractableObject;

    [Tooltip("�Desactivar temporalmente la interacci�n mientras se resuelve la caja fuerte?")]
    public bool disableInteractionDuringSafeMode = true;

    [Header("Player Controller Integration")]
    [Tooltip("Referencia al PlayerController para desactivar movimiento")]
    public PlayerController playerController;

    [Tooltip("�Desactivar movimiento del jugador en modo safe?")]
    public bool disablePlayerMovement = true;

    [Header("Input Configuration")]
    [Tooltip("�Habilitar autom�ticamente la navegaci�n con gamepad?")]
    public bool enableGamepadNavigationInSafeMode = true;

    // Estado actual
    private bool isSafeMode = false;

    // Para controlar el retorno al gameplay despu�s de desbloquear
    private Coroutine returnToGameplayCoroutine;

    // Estados previos del cursor para restaurar
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockState;

    private void Awake()
    {
        // Verificar referencias
        if (safeVCam == null)
        {
            Debug.LogError("No se ha asignado la c�mara virtual de la caja fuerte.");
        }
        else
        {
            // Asegurarse de que la c�mara est� desactivada al inicio
            safeVCam.gameObject.SetActive(false);
        }

        // Buscar el SafeSystem si no est� asignado
        if (safeSystem == null)
        {
            safeSystem = GetComponent<SafeSystem>();
            if (safeSystem == null)
            {
                Debug.LogError("No se encontr� el componente SafeSystem.");
            }
        }

        // Buscar la secuencia de desbloqueo si no est� asignada
        if (unlockSequence == null && safeSystem != null)
        {
            unlockSequence = GetComponent<SafeUnlockSequence>();
        }

        // Buscar el InteractableObject si no est� asignado
        if (safeInteractableObject == null)
        {
            safeInteractableObject = GetComponent<InteractableObject>();
            if (safeInteractableObject == null)
            {
                Debug.LogWarning("No se encontr� InteractableObject. La desactivaci�n de interacci�n no funcionar�.");
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

        // Buscar el PlayerController si no est� asignado
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
            if (playerController == null && disablePlayerMovement)
            {
                Debug.LogWarning("No se encontr� PlayerController. No se podr� desactivar el movimiento del jugador.");
            }
        }
    }

    // M�todo para activar el modo de interacci�n con la caja fuerte
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

            // DESACTIVAR INTERACCI�N CON LA CAJA FUERTE
            if (disableInteractionDuringSafeMode && safeInteractableObject != null)
            {
                safeInteractableObject.enabled = false;
                Debug.Log("InteractableObject desactivado durante modo caja fuerte");
            }

            // Activar la c�mara de la caja fuerte
            safeVCam.gameObject.SetActive(true);
            safeVCam.Priority = safeCameraPriority;

            isSafeMode = true;

            // Configurar cursor para interacci�n con UI
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Activar navegaci�n con gamepad si est� habilitado
            if (enableGamepadNavigationInSafeMode && safeSystem != null)
            {
                safeSystem.SetNavigationActive(true);
            }

            Debug.Log("Modo de interacci�n con caja fuerte activado - Jugador no puede moverse");
        }
    }

    // M�todo para desactivar el modo de interacci�n con la caja fuerte
    public void ExitSafeMode()
    {
        Debug.Log("Llamado Exit Safe Mode");

        if (isSafeMode)
        {
            // Desactivar navegaci�n si est� activa
            if (safeSystem != null)
            {
                safeSystem.SetNavigationActive(false);
            }

            // Desactivar la c�mara de la caja fuerte
            safeVCam.gameObject.SetActive(false);

            // REACTIVAR MOVIMIENTO DEL JUGADOR
            if (disablePlayerMovement && playerController != null)
            {
                playerController.SetMovementEnabled(true);
                Debug.Log("Movimiento del jugador reactivado");
            }

            // REACTIVAR INTERACCI�N CON LA CAJA FUERTE (solo si no est� desbloqueada)
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

            Debug.Log("Modo de interacci�n con caja fuerte desactivado - Jugador puede moverse nuevamente");
        }
    }

    // M�todo que se llama cuando se desbloquea la caja fuerte
    private void HandleSafeUnlocked()
    {
        Debug.Log("Caja fuerte desbloqueada - manejando secuencia de salida");

        // DESACTIVAR PERMANENTEMENTE LA INTERACCI�N (la caja ya est� abierta)
        if (safeInteractableObject != null)
        {
            safeInteractableObject.enabled = false;
            Debug.Log("InteractableObject desactivado permanentemente - caja fuerte desbloqueada");
        }

        // Si hay una secuencia de desbloqueo, dejamos que ella maneje las c�maras
        // Si no, programamos el retorno al gameplay despu�s del retraso
        if (unlockSequence == null)
        {
            // Programar el retorno al gameplay
            returnToGameplayCoroutine = StartCoroutine(ReturnToGameplayAfterDelay());
        }
        else
        {
            // La secuencia de desbloqueo ya est� gestionando las c�maras,
            // pero a�n necesitamos programar el retorno al gameplay
            returnToGameplayCoroutine = StartCoroutine(ReturnToGameplayAfterSequence());
        }
    }

    // Corrutina para volver al gameplay despu�s del retraso
    private IEnumerator ReturnToGameplayAfterDelay()
    {
        // Esperar el tiempo especificado
        yield return new WaitForSeconds(returnToGameplayDelay);

        // Salir del modo de caja fuerte
        ExitSafeMode();

        returnToGameplayCoroutine = null;
    }

    // Corrutina para volver al gameplay despu�s de que termine la secuencia
    private IEnumerator ReturnToGameplayAfterSequence()
    {
        // Esperar a que termine la secuencia (usando la duraci�n definida en SafeUnlockSequence)
        yield return new WaitForSeconds(unlockSequence.sequenceDuration);

        // Desactivar la c�mara de la secuencia (si la secuencia usa una c�mara diferente)
        if (unlockSequence.unlockSequenceCamera != null)
        {
            unlockSequence.unlockSequenceCamera.gameObject.SetActive(false);
        }

        // Esperar el tiempo adicional despu�s de la secuencia
        yield return new WaitForSeconds(returnToGameplayDelay);

        // Salir del modo de caja fuerte
        ExitSafeMode();

        returnToGameplayCoroutine = null;
    }

    // Para interacci�n con la caja fuerte desde otros scripts
    public void OnSafeInteracted()
    {
        // Si la caja ya est� desbloqueada, no hacemos nada
        if (safeSystem != null && safeSystem.IsSafeUnlocked())
        {
            Debug.Log("La caja fuerte ya est� desbloqueada.");
            return;
        }

        // Activar el modo de interacci�n con la caja fuerte
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
    /// M�todo para forzar la activaci�n/desactivaci�n de la navegaci�n
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
    /// M�todo para reiniciar completamente el estado de la caja fuerte (para pruebas)
    /// </summary>
    [ContextMenu("Reiniciar Estado Completo")]
    public void ResetCompleteState()
    {
        // Salir del modo safe si estamos en �l
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