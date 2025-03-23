using UnityEngine;

/// <summary>
/// Script opcional que fuerza a la linterna a seguir siempre la dirección de la cámara,
/// ignorando cualquier entrada del jugador para la rotación.
/// Útil si quieres que la linterna apunte exactamente donde mira la cámara.
/// </summary>
public class FlashlightFollowCamera : MonoBehaviour
{
    [Tooltip("Transform de la cámara que la linterna debe seguir")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("Velocidad de seguimiento de la cámara")]
    [SerializeField] private float followSpeed = 15f;

    [Tooltip("Offset de rotación que se aplicará a la dirección de la cámara")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    [Tooltip("Si está activado, se ignorará cualquier otra fuente de rotación")]
    [SerializeField] private bool overrideAllRotation = true;

    // Referencia al FlashlightController si existe
    private FlashlightController flashlightController;

    private void Start()
    {
        // Buscar la cámara si no fue asignada
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
            else
            {
                Debug.LogError("FlashlightFollowCamera: No se encontró una cámara principal");
                enabled = false;
                return;
            }
        }

        // Obtener referencia al controlador de linterna si existe
        flashlightController = GetComponent<FlashlightController>();
        if (flashlightController != null && overrideAllRotation)
        {
            Debug.Log("FlashlightFollowCamera: Se ha encontrado FlashlightController. La rotación por entrada será ignorada.");
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Calcular la rotación objetivo basada en la dirección de la cámara
        Quaternion targetRotation = cameraTransform.rotation * Quaternion.Euler(rotationOffset);

        // Aplicar la rotación con suavidad
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
    }
}