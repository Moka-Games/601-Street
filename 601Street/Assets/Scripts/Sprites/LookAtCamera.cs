using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera mainCamera;

    // Opción para voltear el sprite horizontalmente si es necesario
    public bool flipHorizontally = false;

    // Opción para mantener la rotación vertical original
    public bool lockVerticalRotation = true;

    void Start()
    {
        // Obtener la referencia a la cámara principal
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null)
            return;

        // Obtener la dirección hacia la cámara
        Vector3 directionToCamera = mainCamera.transform.position - transform.position;

        if (lockVerticalRotation)
        {
            // Mantener solo la rotación en el eje Y
            directionToCamera.y = 0;
        }

        // Hacer que el sprite mire hacia la cámara
        transform.rotation = Quaternion.LookRotation(-directionToCamera);

        // Si está activada la opción de volteo horizontal, rotar 180 grados en Y
        if (flipHorizontally)
        {
            transform.Rotate(0, 180, 0);
        }
    }
}
