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

    [Tooltip("¿Es una interacción única? Si es true, solo se podrá interactuar una vez con la caja fuerte")]
    public bool oneTimeInteraction = true;

    // Estado actual
    private bool isSafeMode = false;

    // Indica si ya se ha interactuado con la caja fuerte
    private bool hasInteracted = false;

    // Para controlar el retorno al gameplay después de desbloquear
    private Coroutine returnToGameplayCoroutine;

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
    }

    private void Start()
    {
        // Suscribirse al evento de desbloqueo si tenemos la referencia
        if (safeSystem != null)
        {
            safeSystem.OnSafeUnlocked.AddListener(HandleSafeUnlocked);
        }
    }

    // Método para activar el modo de interacción con la caja fuerte
    public void EnterSafeMode()
    {
        // Si está configurado para interacción única y ya se ha interactuado, no hacer nada
        if (oneTimeInteraction && hasInteracted)
        {
            Debug.Log("La caja fuerte ya ha sido interactuada. No se permite una segunda interacción.");
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

            // Activar la cámara de la caja fuerte
            safeVCam.gameObject.SetActive(true);
            safeVCam.Priority = safeCameraPriority;

            isSafeMode = true;

            // Mostrar el cursor
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            Debug.Log("Modo de interacción con caja fuerte activado");
        }
    }

    // Método para desactivar el modo de interacción con la caja fuerte
    public void ExitSafeMode()
    {
        print("Llanado Extit Safe Mode");
       
        // Desactivar la cámara de la caja fuerte
        safeVCam.gameObject.SetActive(false);

        isSafeMode = false;

        Debug.Log("Modo de interacción con caja fuerte desactivado");



    }

    // Método que se llama cuando se desbloquea la caja fuerte
    private void HandleSafeUnlocked()
    {
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

        // Si está configurado para interacción única y ya se ha interactuado, no hacer nada
        if (oneTimeInteraction && hasInteracted)
        {
            Debug.Log("La caja fuerte ya ha sido interactuada. No se permite una segunda interacción.");
            return;
        }

        // Activar el modo de interacción con la caja fuerte
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

    // Método para reiniciar el estado de interacción (para pruebas o para casos especiales)
    public void ResetInteractionState()
    {
        hasInteracted = false;
        Debug.Log("Estado de interacción de la caja fuerte reiniciado.");
    }

    // Método para verificar si ya se ha interactuado con la caja fuerte
    public bool HasInteracted()
    {
        return hasInteracted;
    }
}