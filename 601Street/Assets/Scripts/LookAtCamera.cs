using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera mainCamera;

    // Opci�n para voltear el sprite horizontalmente si es necesario
    public bool flipHorizontally = false;

    // Opci�n para mantener la rotaci�n vertical original
    public bool lockVerticalRotation = true;

    void Start()
    {
        // Obtener la referencia a la c�mara principal
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null)
            return;

        // Obtener la direcci�n hacia la c�mara
        Vector3 directionToCamera = mainCamera.transform.position - transform.position;

        if (lockVerticalRotation)
        {
            // Mantener solo la rotaci�n en el eje Y
            directionToCamera.y = 0;
        }

        // Hacer que el sprite mire hacia la c�mara
        transform.rotation = Quaternion.LookRotation(-directionToCamera);

        // Si est� activada la opci�n de volteo horizontal, rotar 180 grados en Y
        if (flipHorizontally)
        {
            transform.Rotate(0, 180, 0);
        }
    }
}
