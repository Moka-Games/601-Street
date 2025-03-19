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

    [Tooltip("�Es una interacci�n �nica? Si es true, solo se podr� interactuar una vez con la caja fuerte")]
    public bool oneTimeInteraction = true;

    // Estado actual
    private bool isSafeMode = false;

    // Indica si ya se ha interactuado con la caja fuerte
    private bool hasInteracted = false;

    // Para controlar el retorno al gameplay despu�s de desbloquear
    private Coroutine returnToGameplayCoroutine;

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
    }

    private void Start()
    {
        // Suscribirse al evento de desbloqueo si tenemos la referencia
        if (safeSystem != null)
        {
            safeSystem.OnSafeUnlocked.AddListener(HandleSafeUnlocked);
        }
    }

    // M�todo para activar el modo de interacci�n con la caja fuerte
    public void EnterSafeMode()
    {
        // Si est� configurado para interacci�n �nica y ya se ha interactuado, no hacer nada
        if (oneTimeInteraction && hasInteracted)
        {
            Debug.Log("La caja fuerte ya ha sido interactuada. No se permite una segunda interacci�n.");
            return;
        }

        if (!isSafeMode && safeVCam != null)
        {
            // Marcar como interactuada
            hasInteracted = true;

            // Cancelar cualquier corrutina de retorno al gameplay si estaba en marcha
            if (returnToGameplayCoroutine != null)
            {
                StopCoroutine(returnToGameplayCoroutine);
                returnToGameplayCoroutine = null;
            }

            // Activar la c�mara de la caja fuerte
            safeVCam.gameObject.SetActive(true);
            safeVCam.Priority = safeCameraPriority;

            isSafeMode = true;

            // Mostrar el cursor
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            Debug.Log("Modo de interacci�n con caja fuerte activado");
        }
    }

    // M�todo para desactivar el modo de interacci�n con la caja fuerte
    public void ExitSafeMode()
    {
        print("Llanado Extit Safe Mode");
       
        // Desactivar la c�mara de la caja fuerte
        safeVCam.gameObject.SetActive(false);

        isSafeMode = false;

        Debug.Log("Modo de interacci�n con caja fuerte desactivado");



    }

    // M�todo que se llama cuando se desbloquea la caja fuerte
    private void HandleSafeUnlocked()
    {
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

        // Si est� configurado para interacci�n �nica y ya se ha interactuado, no hacer nada
        if (oneTimeInteraction && hasInteracted)
        {
            Debug.Log("La caja fuerte ya ha sido interactuada. No se permite una segunda interacci�n.");
            return;
        }

        // Activar el modo de interacci�n con la caja fuerte
        EnterSafeMode();
    }

    // Para manejar la tecla de escape o cancelar
    private void Update()
    {
        // Solo procesamos input si estamos en modo de caja fuerte
        if (isSafeMode)
        {
            // Salir del modo con Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitSafeMode();
            }
        }
    }

    // M�todo para reiniciar el estado de interacci�n (para pruebas o para casos especiales)
    public void ResetInteractionState()
    {
        hasInteracted = false;
        Debug.Log("Estado de interacci�n de la caja fuerte reiniciado.");
    }

    // M�todo para verificar si ya se ha interactuado con la caja fuerte
    public bool HasInteracted()
    {
        return hasInteracted;
    }
}