using UnityEngine;

/// <summary>
/// Componente para congelar/descongelar la c�mara y el jugador al interactuar con objetos de UI.
/// </summary>
public class FreezeOnInteract : MonoBehaviour
{
    [Header("Configuraci�n")]
    [Tooltip("Si est� marcado, bloquear� el movimiento del jugador")]
    public bool freezePlayerMovement = true;

    [Tooltip("Si est� marcado, congelar� la c�mara")]
    public bool freezeCamera = true;

    [Tooltip("Si est� marcado, mostrar� mensajes de depuraci�n en la consola")]
    public bool showDebugMessages = true;

    // Referencias cacheadas para mejor rendimiento
    private Camera_Script cameraScript;
    private PlayerController playerController;

    private void Start()
    {
        // Buscar referencias al iniciar para no tener que buscarlas cada vez
        cameraScript = FindAnyObjectByType<Camera_Script>();
        playerController = FindAnyObjectByType<PlayerController>();

        // Comprobar si encontramos las referencias
        if (cameraScript == null && freezeCamera)
        {
            Debug.LogWarning("No se encontr� Camera_Script en la escena. No se podr� congelar la c�mara.");
        }

        if (playerController == null && freezePlayerMovement)
        {
            Debug.LogWarning("No se encontr� PlayerController en la escena. No se podr� bloquear al jugador.");
        }
    }

    /// <summary>
    /// Activa un objeto y congela la c�mara y/o el movimiento del jugador
    /// </summary>
    /// <param name="objectToActivate">GameObject a activar (ej. un panel o interfaz)</param>
    public void OnInteractOpenAndFreeze(GameObject objectToActivate)
    {
        // Activar el objeto
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);

            if (showDebugMessages)
                Debug.Log($"Objeto activado: {objectToActivate.name}");
        }
        else
        {
            Debug.LogError("OnInteractOpenAndFreeze: El objeto a activar es null.");
            return;
        }

        // Congelar la c�mara si est� configurado
        if (freezeCamera && cameraScript != null)
        {
            cameraScript.FreezeCamera();

            if (showDebugMessages)
                Debug.Log("C�mara congelada");
        }

        // Bloquear al jugador si est� configurado
        if (freezePlayerMovement && playerController != null)
        {
            playerController.SetMovementEnabled(false);

            if (showDebugMessages)
                Debug.Log("Movimiento del jugador bloqueado");
        }
    }

    /// <summary>
    /// Desactiva un objeto y descongelar la c�mara y/o el movimiento del jugador
    /// </summary>
    /// <param name="objectToDeactivate">GameObject a desactivar</param>
    public void OnInteractCloseAndUnfreeze(GameObject objectToDeactivate)
    {
        // Desactivar el objeto
        if (objectToDeactivate != null)
        {
            objectToDeactivate.SetActive(false);

            if (showDebugMessages)
                Debug.Log($"Objeto desactivado: {objectToDeactivate.name}");
        }
        else
        {
            Debug.LogError("OnInteractCloseAndUnfreeze: El objeto a desactivar es null.");
            return;
        }

        // Descongelar la c�mara si est� configurado
        if (freezeCamera && cameraScript != null)
        {
            cameraScript.UnfreezeCamera();

            if (showDebugMessages)
                Debug.Log("C�mara descongelada");
        }

        // Desbloquear al jugador si est� configurado
        if (freezePlayerMovement && playerController != null)
        {
            playerController.SetMovementEnabled(true);

            if (showDebugMessages)
                Debug.Log("Movimiento del jugador desbloqueado");
        }
    }

    /// <summary>
    /// M�todo auxiliar para solo congelar sin activar ning�n objeto
    /// </summary>
    public void FreezeOnly()
    {
        // Congelar la c�mara si est� configurado
        if (freezeCamera && cameraScript != null)
        {
            cameraScript.FreezeCamera();

            if (showDebugMessages)
                Debug.Log("C�mara congelada");
        }

        // Bloquear al jugador si est� configurado
        if (freezePlayerMovement && playerController != null)
        {
            playerController.SetMovementEnabled(false);

            if (showDebugMessages)
                Debug.Log("Movimiento del jugador bloqueado");
        }
    }

    /// <summary>
    /// M�todo auxiliar para solo descongelar sin desactivar ning�n objeto
    /// </summary>
    public void UnfreezeOnly()
    {
        // Descongelar la c�mara si est� configurado
        if (freezeCamera && cameraScript != null)
        {
            cameraScript.UnfreezeCamera();

            if (showDebugMessages)
                Debug.Log("C�mara descongelada");
        }

        // Desbloquear al jugador si est� configurado
        if (freezePlayerMovement && playerController != null)
        {
            playerController.SetMovementEnabled(true);

            if (showDebugMessages)
                Debug.Log("Movimiento del jugador desbloqueado");
        }
    }
}