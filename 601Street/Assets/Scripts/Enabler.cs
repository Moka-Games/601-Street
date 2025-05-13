using UnityEngine;
using System.Diagnostics;
using Cinemachine;

public class Enabler : MonoBehaviour
{
    public static Enabler Instance { get; private set; } 

    private PlayerController playerController;
    private Camera_Script cameraReference;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    private string GetCaller()
    {
        StackTrace stackTrace = new StackTrace(0, true);
        if (stackTrace.FrameCount > 1)
        {
            var frame = stackTrace.GetFrame(1);
            return $"{frame.GetMethod().DeclaringType}.{frame.GetMethod().Name} (línea {frame.GetFileLineNumber()})";
        }
        return "Desconocido";
    }

    public void BlockPlayer()
    {
        //Al llamar al método, se muestra por consola la clase donde se le ha llamado
        UnityEngine.Debug.Log($"BlockPlayer() fue llamado desde: {GetCaller()}");
        
        playerController = FindAnyObjectByType<PlayerController>();
        cameraReference = FindAnyObjectByType<Camera_Script>();

        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
        }
        else
        {
            print("Script del player no se detecta");
        }

        if (cameraReference != null)
        {
            cameraReference.FreezeCamera();
        }
        else
        {
            print("Script de la cámara no se detecta");
        }
    }


    public void ReleasePlayer()
    {
        UnityEngine.Debug.Log($"ReleasePlayer() fue llamado desde: {GetCaller()}");
        
        playerController = FindAnyObjectByType<PlayerController>();
        cameraReference = FindAnyObjectByType<Camera_Script>();

        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
        }
        else
        {
            print("Script del player no se detecta");
        }

        if (cameraReference != null)
        {
            cameraReference.UnfreezeCamera();
        }
        else
        {
            print("Script de la cámara no se detecta");
        }
    }
}
