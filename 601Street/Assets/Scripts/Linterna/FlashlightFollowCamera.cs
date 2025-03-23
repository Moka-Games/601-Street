using UnityEngine;

/// <summary>
/// Script opcional que fuerza a la linterna a seguir siempre la direcci�n de la c�mara,
/// ignorando cualquier entrada del jugador para la rotaci�n.
/// �til si quieres que la linterna apunte exactamente donde mira la c�mara.
/// </summary>
public class FlashlightFollowCamera : MonoBehaviour
{
    [Tooltip("Transform de la c�mara que la linterna debe seguir")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("Velocidad de seguimiento de la c�mara")]
    [SerializeField] private float followSpeed = 15f;

    [Tooltip("Offset de rotaci�n que se aplicar� a la direcci�n de la c�mara")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    [Tooltip("Si est� activado, se ignorar� cualquier otra fuente de rotaci�n")]
    [SerializeField] private bool overrideAllRotation = true;

    // Referencia al FlashlightController si existe
    private FlashlightController flashlightController;

    private void Start()
    {
        // Buscar la c�mara si no fue asignada
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
            else
            {
                Debug.LogError("FlashlightFollowCamera: No se encontr� una c�mara principal");
                enabled = false;
                return;
            }
        }

        // Obtener referencia al controlador de linterna si existe
        flashlightController = GetComponent<FlashlightController>();
        if (flashlightController != null && overrideAllRotation)
        {
            Debug.Log("FlashlightFollowCamera: Se ha encontrado FlashlightController. La rotaci�n por entrada ser� ignorada.");
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Calcular la rotaci�n objetivo basada en la direcci�n de la c�mara
        Quaternion targetRotation = cameraTransform.rotation * Quaternion.Euler(rotationOffset);

        // Aplicar la rotaci�n con suavidad
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
    }
}