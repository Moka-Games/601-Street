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

    [Header("Estabilización")]
    [Tooltip("Umbral en grados para ignorar pequeños cambios de rotación")]
    [SerializeField] private float rotationThreshold = 0.5f;

    [Tooltip("Suavizado adicional para la rotación")]
    [SerializeField] private float additionalSmoothing = 0.5f;

    // Variables privadas
    private FlashlightController flashlightController;
    private Quaternion initialCameraRotation;
    private Quaternion initialHandRotation;
    private bool initialized = false;
    private Quaternion lastRotation;
    private Quaternion currentTargetRotation;
    private float timeWithoutSignificantChange = 0f;
    private float stabilizationTime = 1.0f;

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

        // Inicializar las rotaciones de seguimiento
        lastRotation = transform.rotation;
        currentTargetRotation = lastRotation;

        // Obtener referencia al controlador de linterna si existe
        flashlightController = GetComponent<FlashlightController>();
        if (flashlightController != null && overrideAllRotation)
        {
            Debug.Log("FlashlightFollowCamera: Se ha encontrado FlashlightController. La rotación por entrada será ignorada.");
        }

        // Establecer rotación inicial
        //ResetOrientation();

        initialized = true;
    }

    private void FixedUpdate()
    {
        // Actualizar en FixedUpdate para una mayor consistencia
        if (!initialized || cameraTransform == null)
        {
            if (!initialized) Initialize();
            return;
        }

        // Calcular límites de rotación
        UpdateRotation();
    }

    private void LateUpdate()
    {
        // También actualizar en LateUpdate para asegurar un seguimiento fluido
        if (!initialized || cameraTransform == null)
        {
            if (!initialized) Initialize();
            return;
        }

        // Aplicar la rotación calculada
        ApplyCalculatedRotation();
    }

    private void UpdateRotation()
    {
        // Calcular la rotación objetivo
        Quaternion newTargetRotation = CalculateLimitedRotation();

        // Comprobar si ha habido un cambio significativo en la rotación
        float angleDiff = Quaternion.Angle(lastRotation, newTargetRotation);

        if (angleDiff > rotationThreshold)
        {
            // Si hay un cambio significativo, actualizar la rotación objetivo
            currentTargetRotation = newTargetRotation;
            timeWithoutSignificantChange = 0;
        }
        else
        {
            // Si no hay cambio significativo, contar el tiempo sin cambios
            timeWithoutSignificantChange += Time.fixedDeltaTime;

            // Si llevamos suficiente tiempo sin cambios, estabilizar la rotación
            if (timeWithoutSignificantChange > stabilizationTime)
            {
                // Estabilizar la rotación: no cambiamos la rotación objetivo
                // y dejamos que se aplique suavemente sin cambios
            }
        }

        // Actualizar la última rotación conocida
        lastRotation = newTargetRotation;
    }

    private void ApplyCalculatedRotation()
    {
        // Aplicar la rotación con suavizado avanzado
        float smoothFactor = Time.deltaTime * followSpeed;

        // Si hemos estado estables por un tiempo, reducir el factor de suavizado
        if (timeWithoutSignificantChange > stabilizationTime)
        {
            smoothFactor *= (1 - additionalSmoothing);
        }

        // Aplicar la rotación con suavidad
        transform.rotation = Quaternion.Slerp(transform.rotation, currentTargetRotation, smoothFactor);
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
}