using UnityEngine;

public class FreezeScript : MonoBehaviour
{

   private Camera_Script cameraScript;
   private PlayerController playerController;


    private void Awake()
    {
        cameraScript = FindAnyObjectByType<Camera_Script>();
        playerController = FindAnyObjectByType<PlayerController>();
    }
    private void Start()
    {
        cameraScript = FindAnyObjectByType<Camera_Script>();
        playerController = FindAnyObjectByType<PlayerController>(); 
    }

    //
    /// <summary>
    /// CONGELAR MOVIMIENTO Y CAMARA
    /// </summary>
    /// 
    /// 
    public void FreezeMovement()
    {
        playerController.SetMovementEnabled(false);
    }

    public void FreezeCamera()
    {
        cameraScript.FreezeCamera();
    }

    public void FreezeMovement_And_Camera()
    {
        print("Congelando movimiento y cámara");
        FreezeMovement();
        FreezeCamera();
    }
    //
    //
    /// <summary>
    /// DES-CONGELAR MOVIMIENTO Y CAMARA
    /// </summary>
    /// 
    //
    //
    public void UnfreezeMovement()
    {
        playerController.SetMovementEnabled(true);
    }
    public void UnfreezeCamera()
    {
        cameraScript.UnfreezeCamera();
    }

    public void UnfreezeMovement_And_Camera()
    {
        print("Descongelando movimiento y cámara");
        UnfreezeMovement();
        UnfreezeCamera();
    }
}
