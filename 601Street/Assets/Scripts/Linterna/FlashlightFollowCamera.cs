using UnityEngine;

/// <summary>
/// Script que fuerza a la linterna a seguir la dirección de la cámara,
/// manteniendo un límite máximo de rotación desde la posición inicial.
/// </summary>
public class FlashlightFollowCamera : MonoBehaviour
{
    [Tooltip("Transform de la cámara que la linterna debe seguir")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("Velocidad de seguimiento de la cámara")]
    [SerializeField] private float followSpeed = 15f;

    [Tooltip("Offset de rotación que se aplicará a la dirección de la cámara")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    [Tooltip("Ángulo máximo de rotación desde la posición inicial (en grados)")]
    [SerializeField] private float maxAngleLimit = 90f;

    [Tooltip("Si está activado, se ignorará cualquier otra fuente de rotación")]
    [SerializeField] private bool overrideAllRotation = true;

    [Tooltip("Usa el pivote de la mano como referencia de rotación")]
    [SerializeField] private bool useHandReference = true;

    [Tooltip("Transform del pivote de la mano (opcional)")]
    [SerializeField] private Transform handPivot;

    // Variables privadas
    private FlashlightController flashlightController;
    private Quaternion initialCameraRotation;
    private Quaternion initialHandRotation;
    private bool initialized = false;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
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

        // Buscar el pivote de la mano si no fue asignado
        if (handPivot == null && useHandReference)
        {
            // Intentar usar el padre como pivote
            if (transform.parent != null)
            {
                handPivot = transform.parent;
                Debug.Log("FlashlightFollowCamera: Usando el padre como pivote de la mano");
            }
        }

        // Guardar rotaciones iniciales
        initialCameraRotation = cameraTransform.rotation;
        if (handPivot != null)
        {
            initialHandRotation = handPivot.rotation;
        }

        // Obtener referencia al controlador de linterna si existe
        flashlightController = GetComponent<FlashlightController>();
        if (flashlightController != null && overrideAllRotation)
        {
            Debug.Log("FlashlightFollowCamera: Se ha encontrado FlashlightController. La rotación por entrada será ignorada.");
        }

        // Establecer rotación inicial
        ResetOrientation();

        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized || cameraTransform == null)
        {
            if (!initialized) Initialize();
            return;
        }

        // Calcular límites de rotación
        Quaternion limitedRotation = CalculateLimitedRotation();

        // Aplicar la rotación con suavidad
        transform.rotation = Quaternion.Slerp(transform.rotation, limitedRotation, Time.deltaTime * followSpeed);
    }

    private Quaternion CalculateLimitedRotation()
    {
        // Obtener la rotación actual de la cámara con offset
        Quaternion currentCameraRotation = cameraTransform.rotation * Quaternion.Euler(rotationOffset);

        // Calcular el ángulo entre la rotación inicial y la actual
        float angle = Quaternion.Angle(initialCameraRotation, currentCameraRotation);

        // Si el ángulo está dentro del límite, simplemente seguimos la cámara
        if (angle <= maxAngleLimit)
        {
            if (useHandReference && handPivot != null)
            {
                // Aplicar a través del pivote de la mano
                Quaternion handCameraOffset = Quaternion.Inverse(initialHandRotation) * currentCameraRotation;
                return handPivot.rotation * handCameraOffset;
            }
            else
            {
                // Aplicar directamente
                return currentCameraRotation;
            }
        }
        // Si excede el límite, necesitamos limitar la rotación
        else
        {
            // Interpolamos para obtener una rotación en el límite
            float t = maxAngleLimit / angle;
            Quaternion limitedCameraRot = Quaternion.Slerp(initialCameraRotation, currentCameraRotation, t);

            if (useHandReference && handPivot != null)
            {
                // Aplicar a través del pivote de la mano
                Quaternion handCameraOffset = Quaternion.Inverse(initialHandRotation) * limitedCameraRot;
                return handPivot.rotation * handCameraOffset;
            }
            else
            {
                // Aplicar directamente
                return limitedCameraRot;
            }
        }
    }

    /// <summary>
    /// Resetea la orientación de la linterna a la posición inicial
    /// </summary>
    public void ResetOrientation()
    {
        if (cameraTransform == null) return;

        if (useHandReference && handPivot != null)
        {
            // Alineamos respecto a la mano primero
            Quaternion handToCamera = Quaternion.Inverse(initialHandRotation) * initialCameraRotation;
            transform.rotation = handPivot.rotation * handToCamera;
        }
        else
        {
            // Directamente a la rotación inicial de la cámara
            transform.rotation = initialCameraRotation;
        }
    }

    /// <summary>
    /// Actualiza las referencias iniciales (útil si la cámara o mano cambia)
    /// </summary>
    public void UpdateReferences()
    {
        if (cameraTransform != null)
        {
            initialCameraRotation = cameraTransform.rotation;
        }

        if (handPivot != null)
        {
            initialHandRotation = handPivot.rotation;
        }

        ResetOrientation();
    }

    
}