using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Light spotLight;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform handPosition; // Pivote de la mano del jugador

    [Header("Configuraci�n del movimiento")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float maxAngle = 90f; // Limitado a 90 grados m�ximo
    [SerializeField] private Vector3 originalLocalRotation = Vector3.zero; // Rotaci�n local inicial
    [SerializeField] private bool naturalHandMovement = true; // Para comportamiento m�s natural de mano

    [Header("Configuraci�n de la luz")]
    [SerializeField] private float maxIntensity = 2f;
    [SerializeField] private float minIntensity = 0f;
    [SerializeField] private float intensityChangeSpeed = 2f;

    // Variables privadas
    private bool isFlashlightOn = false;
    private float currentIntensity = 0f;
    private Quaternion initialHandRotation;
    private Quaternion initialCameraRotation;
    private Vector3 initialForwardVector;
    private Vector2 moveInput;

    // Limitadores de rotaci�n
    private float currentPitchAngle = 0f; // Rotaci�n vertical (arriba/abajo)
    private float currentYawAngle = 0f;   // Rotaci�n horizontal (izquierda/derecha)

    void Start()
    {
        // Si no se asign� en el inspector, buscar autom�ticamente
        if (spotLight == null)
        {
            spotLight = GetComponentInChildren<Light>();
            if (spotLight == null)
            {
                Debug.LogError("No se encontr� componente Light en los hijos");
            }
        }

        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
            else
            {
                Debug.LogError("No se encontr� una c�mara principal");
            }
        }

        // Si no hay handPosition asignada, usar el transform del padre
        if (handPosition == null && transform.parent != null)
        {
            handPosition = transform.parent;
            Debug.Log("Se usar� el transform del padre como handPosition");
        }

        // Guardar la rotaci�n inicial del soporte de la mano
        if (handPosition != null)
        {
            initialHandRotation = handPosition.rotation;
        }
        else
        {
            initialHandRotation = transform.rotation;
        }

        // Configuraci�n inicial de la luz
        if (spotLight != null)
        {
            // Guardamos la intensidad configurada como intensidad m�xima
            if (maxIntensity <= 0)
            {
                maxIntensity = spotLight.intensity;
            }
            currentIntensity = spotLight.intensity;

            // Inicialmente encendemos la linterna
            isFlashlightOn = true;
            spotLight.enabled = true;
        }
    }

    void Update()
    {
        // Verificar teclas para encender/apagar - procesamos esto primero
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlashlight();
        }

        // Actualizar la intensidad de la luz gradualmente
        UpdateLightIntensity();

        // Capturar entrada para mover la linterna - usando el sistema de input tradicional
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");

        // Actualizar la rotaci�n de la linterna
        UpdateFlashlightRotation();
    }

    private void UpdateLightIntensity()
    {
        if (spotLight == null) return;

        float targetIntensity = isFlashlightOn ? maxIntensity : minIntensity;

        // Interpolar suavemente hasta la intensidad objetivo
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * intensityChangeSpeed);
        spotLight.intensity = currentIntensity;

        // Verificar si la luz deber�a estar habilitada o no
        spotLight.enabled = isFlashlightOn || currentIntensity > 0.01f;

        // Debug para ver los valores de la luz
        Debug.Log($"Flashlight: isOn={isFlashlightOn}, intensity={currentIntensity}, enabled={spotLight.enabled}");
    }

    private void UpdateFlashlightRotation()
    {
        // Obtenemos la rotaci�n actual de la c�mara
        Quaternion cameraRotation = cameraTransform != null ? cameraTransform.rotation : Quaternion.identity;

        if (naturalHandMovement)
        {
            // Enfoque natural para movimiento de mano
            UpdateNaturalHandMovement(cameraRotation);
        }
        else
        {
            // Enfoque original (respecto a la rotaci�n actual de la c�mara)
            UpdateRelativeCameraMovement(cameraRotation);
        }
    }

    private void UpdateNaturalHandMovement(Quaternion cameraRotation)
    {
        // Calculamos el offset de rotaci�n basado en la entrada
        float deltaYaw = moveInput.x * rotationSpeed * Time.deltaTime;
        float deltaPitch = -moveInput.y * rotationSpeed * Time.deltaTime;

        // Acumular los �ngulos
        currentYawAngle += deltaYaw;
        currentPitchAngle += deltaPitch;

        // Limitar los �ngulos para que no excedan el m�ximo desde la rotaci�n inicial
        currentYawAngle = Mathf.Clamp(currentYawAngle, -maxAngle, maxAngle);
        currentPitchAngle = Mathf.Clamp(currentPitchAngle, -maxAngle, maxAngle);

        // Crear una rotaci�n basada en los �ngulos acumulados, relativa a la rotaci�n inicial
        Quaternion offsetRotation = Quaternion.Euler(currentPitchAngle, currentYawAngle, 0);

        if (handPosition != null)
        {
            // Si hay una mano, calculamos la rotaci�n respecto a ella

            // 1. Calculamos c�mo la c�mara ha rotado desde su posici�n inicial
            Quaternion cameraChange = Quaternion.Inverse(initialCameraRotation) * cameraRotation;

            // 2. Aplicamos esta rotaci�n relativa a la mano
            Quaternion handWithCameraMovement = handPosition.rotation * cameraChange;

            // 3. Aplicamos nuestra rotaci�n de offset del jugador
            Quaternion targetRotation = handWithCameraMovement * offsetRotation;

            // 4. Aplicamos la rotaci�n con suavidad
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
        else
        {
            // Si no hay mano, simplemente combinamos la rotaci�n actual de la c�mara con nuestro offset
            Quaternion targetRotation = cameraRotation * offsetRotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // Cuando no hay entrada, volvemos gradualmente a la posici�n central (sin offset)
        if (Mathf.Abs(moveInput.x) < 0.05f && Mathf.Abs(moveInput.y) < 0.05f)
        {
            currentYawAngle = Mathf.Lerp(currentYawAngle, 0, Time.deltaTime * 2f);
            currentPitchAngle = Mathf.Lerp(currentPitchAngle, 0, Time.deltaTime * 2f);
        }
    }

    private void UpdateRelativeCameraMovement(Quaternion cameraRotation)
    {
        // Calculamos el offset de rotaci�n basado en la entrada
        float deltaYaw = moveInput.x * rotationSpeed * Time.deltaTime * 0.5f;
        float deltaPitch = -moveInput.y * rotationSpeed * Time.deltaTime * 0.5f;

        // Acumular los �ngulos
        currentYawAngle = Mathf.Lerp(currentYawAngle, currentYawAngle + deltaYaw, 0.5f);
        currentPitchAngle = Mathf.Lerp(currentPitchAngle, currentPitchAngle + deltaPitch, 0.5f);

        // Limitar los �ngulos para que no excedan el m�ximo
        currentYawAngle = Mathf.Clamp(currentYawAngle, -maxAngle, maxAngle);
        currentPitchAngle = Mathf.Clamp(currentPitchAngle, -maxAngle, maxAngle);

        // Creamos una rotaci�n de ajuste basada en los controles del jugador
        Quaternion offsetRotation = Quaternion.Euler(currentPitchAngle, currentYawAngle, 0);

        // La rotaci�n final combina la direcci�n de la c�mara con el ajuste del jugador
        Quaternion targetRotation = cameraRotation * offsetRotation;

        // Si hay una posici�n de mano, aplicamos cualquier offset necesario
        if (handPosition != null)
        {
            // Calculamos la diferencia de rotaci�n entre la c�mara y la mano
            Quaternion handCameraOffset = Quaternion.Inverse(handPosition.rotation) * cameraRotation;

            // La rotaci�n final aplica esta diferencia para mantener la alineaci�n correcta
            targetRotation = handPosition.rotation * handCameraOffset * offsetRotation;
        }

        // Aplicamos la rotaci�n con suavidad
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed * 2f);

        // Siempre que no haya entrada del jugador, volvemos gradualmente a alinear con la c�mara
        if (Mathf.Abs(moveInput.x) < 0.05f && Mathf.Abs(moveInput.y) < 0.05f)
        {
            currentYawAngle = Mathf.Lerp(currentYawAngle, 0, Time.deltaTime * 2f);
            currentPitchAngle = Mathf.Lerp(currentPitchAngle, 0, Time.deltaTime * 2f);
        }
    }

    public void ToggleFlashlight()
    {
        isFlashlightOn = !isFlashlightOn;

        // Asegurarnos de que el cambio se refleje inmediatamente
        if (spotLight != null)
        {
            spotLight.enabled = isFlashlightOn;
            if (isFlashlightOn)
            {
                // Establecer directamente la intensidad al encender
                spotLight.intensity = maxIntensity;
                currentIntensity = maxIntensity;
            }
        }

        Debug.Log($"Linterna: {(isFlashlightOn ? "ENCENDIDA" : "APAGADA")}");
    }

    public void SetFlashlightOn(bool value)
    {
        // Solo hacemos cambios si el estado es diferente
        if (isFlashlightOn != value)
        {
            isFlashlightOn = value;

            // Reflejar el cambio inmediatamente
            if (spotLight != null)
            {
                spotLight.enabled = isFlashlightOn;
                if (isFlashlightOn)
                {
                    spotLight.intensity = maxIntensity;
                    currentIntensity = maxIntensity;
                }
            }

            Debug.Log($"Linterna cambiada a: {(isFlashlightOn ? "ENCENDIDA" : "APAGADA")}");
        }
    }

    public bool IsFlashlightOn()
    {
        return isFlashlightOn;
    }

    // M�todo para resetear la orientaci�n de la linterna
    public void ResetOrientation()
    {
        currentPitchAngle = 0f;
        currentYawAngle = 0f;

        if (handPosition != null)
        {
            transform.rotation = handPosition.rotation;
        }
        else if (cameraTransform != null)
        {
            transform.rotation = cameraTransform.rotation;
        }
    }
}