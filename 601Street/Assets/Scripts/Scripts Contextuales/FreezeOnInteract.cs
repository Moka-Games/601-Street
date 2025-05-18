using UnityEngine;

/// <summary>
/// Componente para congelar/descongelar la cámara y el jugador al interactuar con objetos de UI.
/// </summary>
public class FreezeOnInteract : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Si está marcado, bloqueará el movimiento del jugador")]
    public bool freezePlayerMovement = true;

    [Tooltip("Si está marcado, congelará la cámara")]
    public bool freezeCamera = true;

    [Tooltip("Si está marcado, mostrará mensajes de depuración en la consola")]
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
            Debug.LogWarning("No se encontró Camera_Script en la escena. No se podrá congelar la cámara.");
        }

        if (playerController == null && freezePlayerMovement)
        {
            Debug.LogWarning("No se encontró PlayerController en la escena. No se podrá bloquear al jugador.");
        }
    }

    /// <summary>
    /// Activa un objeto y congela la cámara y/o el movimiento del jugador
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

        // Congelar la cámara si está configurado
        if (freezeCamera && cameraScript != null)
        {
            cameraScript.FreezeCamera();

            if (showDebugMessages)
                Debug.Log("Cámara congelada");
        }

        // Bloquear al jugador si está configurado
        if (freezePlayerMovement && playerController != null)
        {
            playerController.SetMovementEnabled(false);

            if (showDebugMessages)
                Debug.Log("Movimiento del jugador bloqueado");
        }
    }

    /// <summary>
    /// Desactiva un objeto y descongelar la cámara y/o el movimiento del jugador
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

        // Descongelar la cámara si está configurado
        if (freezeCamera && cameraScript != null)
        {
            cameraScript.UnfreezeCamera();

            if (showDebugMessages)
                Debug.Log("Cámara descongelada");
        }

        // Desbloquear al jugador si está configurado
        if (freezePlayerMovement && playerController != null)
        {
            playerController.SetMovementEnabled(true);

            if (showDebugMessages)
                Debug.Log("Movimiento del jugador desbloqueado");
        }
    }

    /// <summary>
    /// Método auxiliar para solo congelar sin activar ningún objeto
    /// </summary>
    public void FreezeOnly()
    {
        // Congelar la cámara si está configurado
        if (freezeCamera && cameraScript != null)
        {
            cameraScript.FreezeCamera();

            if (showDebugMessages)
                Debug.Log("Cámara congelada");
        }

        // Bloquear al jugador si está configurado
        if (freezePlayerMovement && playerController != null)
        {
            playerController.SetMovementEnabled(false);

            if (showDebugMessages)
                Debug.Log("Movimiento del jugador bloqueado");
        }
    }

    /// <summary>
    /// Método auxiliar para solo descongelar sin desactivar ningún objeto
    /// </summary>
    public void UnfreezeOnly()
    {
        // Descongelar la cámara si está configurado
        if (freezeCamera && cameraScript != null)
        {
            cameraScript.UnfreezeCamera();

            if (showDebugMessages)
                Debug.Log("Cámara descongelada");
        }

        // Desbloquear al jugador si está configurado
        if (freezePlayerMovement && playerController != null)
        {
            playerController.SetMovementEnabled(true);

            if (showDebugMessages)
                Debug.Log("Movimiento del jugador desbloqueado");
        }
    }
}