using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sprite_XRay : MonoBehaviour
{
    private Camera mainCamera;
    public bool flipHorizontally = false;
    public bool lockVerticalRotation = true;

    // Referencias para el efecto X-Ray
    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private Material xrayMaterial;

    void Start()
    {
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Guardar el material original
        originalMaterial = spriteRenderer.material;

        // Crear una copia del material para el efecto X-Ray
        xrayMaterial = new Material(Shader.Find("GUI/Text Shader"));
        xrayMaterial.color = originalMaterial.color;

        // Configurar el material para renderizar siempre encima
        xrayMaterial.renderQueue = 4000; // Número alto para renderizar al final
        spriteRenderer.material = xrayMaterial;

        // Asegurarse de que el sprite ignore la profundidad
        spriteRenderer.sortingOrder = 999; // Número alto para estar siempre visible
    }

    void LateUpdate()
    {
        if (mainCamera == null)
            return;

        Vector3 directionToCamera = mainCamera.transform.position - transform.position;

        if (lockVerticalRotation)
        {
            directionToCamera.y = 0;
        }

        transform.rotation = Quaternion.LookRotation(-directionToCamera);

        if (flipHorizontally)
        {
            transform.Rotate(0, 180, 0);
        }
    }

    void OnDestroy()
    {
        // Limpiar el material cuando se destruye el objeto
        if (xrayMaterial != null)
        {
            Destroy(xrayMaterial);
        }
    }
}
