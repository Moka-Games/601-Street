using UnityEngine;

/// <summary>
/// Script que fuerza a la linterna a seguir la direcci�n de la c�mara,
/// manteniendo un l�mite m�ximo de rotaci�n desde la posici�n inicial.
/// </summary>
public class FlashlightFollowCamera : MonoBehaviour
{
    [Tooltip("Transform de la c�mara que la linterna debe seguir")]
    [SerializeField] private Transform cameraTransform;

    [Tooltip("Velocidad de seguimiento de la c�mara")]
    [SerializeField] private float followSpeed = 15f;

    [Tooltip("Offset de rotaci�n que se aplicar� a la direcci�n de la c�mara")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    [Tooltip("�ngulo m�ximo de rotaci�n desde la posici�n inicial (en grados)")]
    [SerializeField] private float maxAngleLimit = 90f;

    [Tooltip("Si est� activado, se ignorar� cualquier otra fuente de rotaci�n")]
    [SerializeField] private bool overrideAllRotation = true;

    [Tooltip("Usa el pivote de la mano como referencia de rotaci�n")]
    [SerializeField] private bool useHandReference = true;

    [Tooltip("Transform del pivote de la mano (opcional)")]
    [SerializeField] private Transform handPivot;

    [Header("Estabilizaci�n")]
    [Tooltip("Umbral en grados para ignorar peque�os cambios de rotaci�n")]
    [SerializeField] private float rotationThreshold = 0.5f;

    [Tooltip("Suavizado adicional para la rotaci�n")]
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
            Debug.Log("FlashlightFollowCamera: Se ha encontrado FlashlightController. La rotaci�n por entrada ser� ignorada.");
        }

        // Establecer rotaci�n inicial
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

        // Calcular l�mites de rotaci�n
        UpdateRotation();
    }

    private void LateUpdate()
    {
        // Tambi�n actualizar en LateUpdate para asegurar un seguimiento fluido
        if (!initialized || cameraTransform == null)
        {
            if (!initialized) Initialize();
            return;
        }

        // Aplicar la rotaci�n calculada
        ApplyCalculatedRotation();
    }

    private void UpdateRotation()
    {
        // Calcular la rotaci�n objetivo
        Quaternion newTargetRotation = CalculateLimitedRotation();

        // Comprobar si ha habido un cambio significativo en la rotaci�n
        float angleDiff = Quaternion.Angle(lastRotation, newTargetRotation);

        if (angleDiff > rotationThreshold)
        {
            // Si hay un cambio significativo, actualizar la rotaci�n objetivo
            currentTargetRotation = newTargetRotation;
            timeWithoutSignificantChange = 0;
        }
        else
        {
            // Si no hay cambio significativo, contar el tiempo sin cambios
            timeWithoutSignificantChange += Time.fixedDeltaTime;

            // Si llevamos suficiente tiempo sin cambios, estabilizar la rotaci�n
            if (timeWithoutSignificantChange > stabilizationTime)
            {
                // Estabilizar la rotaci�n: no cambiamos la rotaci�n objetivo
                // y dejamos que se aplique suavemente sin cambios
            }
        }

        // Actualizar la �ltima rotaci�n conocida
        lastRotation = newTargetRotation;
    }

    private void ApplyCalculatedRotation()
    {
        // Aplicar la rotaci�n con suavizado avanzado
        float smoothFactor = Time.deltaTime * followSpeed;

        // Si hemos estado estables por un tiempo, reducir el factor de suavizado
        if (timeWithoutSignificantChange > stabilizationTime)
        {
            smoothFactor *= (1 - additionalSmoothing);
        }

        // Aplicar la rotaci�n con suavidad
        transform.rotation = Quaternion.Slerp(transform.rotation, currentTargetRotation, smoothFactor);
    }

    private Quaternion CalculateLimitedRotation()
    {
        // Obtener la rotaci�n actual de la c�mara con offset
        Quaternion currentCameraRotation = cameraTransform.rotation * Quaternion.Euler(rotationOffset);

        // Calcular el �ngulo entre la rotaci�n inicial y la actual
        float angle = Quaternion.Angle(initialCameraRotation, currentCameraRotation);

        // Si el �ngulo est� dentro del l�mite, simplemente seguimos la c�mara
        if (angle <= maxAngleLimit)
        {
            if (useHandReference && handPivot != null)
            {
                // Aplicar a trav�s del pivote de la mano
                Quaternion handCameraOffset = Quaternion.Inverse(initialHandRotation) * currentCameraRotation;
                return handPivot.rotation * handCameraOffset;
            }
            else
            {
                // Aplicar directamente
                return currentCameraRotation;
            }
        }
        // Si excede el l�mite, necesitamos limitar la rotaci�n
        else
        {
            // Interpolamos para obtener una rotaci�n en el l�mite
            float t = maxAngleLimit / angle;
            Quaternion limitedCameraRot = Quaternion.Slerp(initialCameraRotation, currentCameraRotation, t);

            if (useHandReference && handPivot != null)
            {
                // Aplicar a trav�s del pivote de la mano
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