using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CinemachineFreeLook))]
public class FreeLookCameraController : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("Velocidad base de rotación con el stick derecho del gamepad")]
    [SerializeField] private float gamepadLookSpeed = 300f;

    [Tooltip("Velocidad de rotación horizontal con el mouse")]
    [SerializeField] private float mouseXSpeed = 300f;

    [Tooltip("Velocidad de rotación vertical con el mouse")]
    [SerializeField] private float mouseYSpeed = 200f;

    [Tooltip("Botón para habilitar la rotación con el mouse (opcional)")]
    [SerializeField] private bool requireMouseButtonToRotate = true;

    [Tooltip("Botón del mouse que habilita la rotación (0 = izquierdo, 1 = derecho, 2 = medio)")]
    [SerializeField] private int mouseButton = 1;

    [Header("Gamepad Enhanced Settings")]
    [Tooltip("Multiplicador de velocidad cuando el stick está completamente presionado")]
    [SerializeField] private float gamepadSpeedMultiplier = 1.5f;

    [Tooltip("Curva de aceleración para el gamepad (X = input magnitude, Y = speed multiplier)")]
    [SerializeField] private AnimationCurve gamepadAccelerationCurve = AnimationCurve.EaseInOut(0f, 0.1f, 1f, 1f);

    [Tooltip("Zona muerta del stick analógico (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float gamepadDeadzone = 0.15f;

    [Tooltip("Sensibilidad diferente para el eje X del gamepad")]
    [SerializeField] private float gamepadXSensitivity = 1f;

    [Tooltip("Sensibilidad diferente para el eje Y del gamepad")]
    [SerializeField] private float gamepadYSensitivity = 0.8f;

    [Header("Rotation Settings")]
    [Tooltip("Suavizado de movimiento")]
    [Range(0.01f, 1f)]
    [SerializeField] private float lookSmoothing = 0.5f;

    [Tooltip("Tiempo de aceleración para el gamepad (en segundos)")]
    [SerializeField] private float gamepadAccelerationTime = 0.2f;

    [Tooltip("Tiempo de desaceleración para el gamepad (en segundos)")]
    [SerializeField] private float gamepadDecelerationTime = 0.1f;

    [Header("Comfort Settings")]
    [Tooltip("Invertir el eje Y del gamepad")]
    [SerializeField] private bool invertGamepadY = false;

    [Tooltip("Invertir el eje X del gamepad")]
    [SerializeField] private bool invertGamepadX = false;

    [Tooltip("Ajuste automático de sensibilidad basado en framerate")]
    [SerializeField] private bool frameRateCompensation = true;

    // Referencias internas
    private CinemachineFreeLook freeLookCamera;
    private PlayerControls playerControls;
    private Vector2 lookInput;
    private Vector2 smoothedLookInput;
    private Vector2 targetLookInput;
    private bool isUsingGamepad = false;
    private bool usingMouse = false;

    // Variables para el sistema de aceleración mejorado
    private Vector2 currentGamepadVelocity;
    private float currentAcceleration = 0f;
    private float lastInputMagnitude = 0f;

    // Referencias para el input del mouse
    private Vector2 mousePosition;
    private Vector2 lastMousePosition;
    private Vector2 mouseDelta;

    // Variables para compensación de framerate
    private float deltaTimeMultiplier = 1f;

    private void Awake()
    {
        // Obtener la cámara FreeLook
        freeLookCamera = GetComponent<CinemachineFreeLook>();

        // Inicializar los controles
        playerControls = new PlayerControls();

        // Inicializar posición del mouse
        if (Mouse.current != null)
        {
            mousePosition = lastMousePosition = Mouse.current.position.ReadValue();
        }
    }

    private void OnEnable()
    {
        // Habilitar los controles
        playerControls.Enable();

        // Suscribirse al evento de movimiento de la cámara
        playerControls.Gameplay.Look.performed += OnLookPerformed;
        playerControls.Gameplay.Look.canceled += OnLookCanceled;

        // Configurar la cámara para actualización post-input
        freeLookCamera.m_XAxis.m_InputAxisName = "";
        freeLookCamera.m_YAxis.m_InputAxisName = "";

        // Asegurarnos de que las velocidades iniciales de los ejes estén en 0
        freeLookCamera.m_XAxis.m_InputAxisValue = 0;
        freeLookCamera.m_YAxis.m_InputAxisValue = 0;
    }

    private void OnDisable()
    {
        // Desuscribirse de los eventos
        playerControls.Gameplay.Look.performed -= OnLookPerformed;
        playerControls.Gameplay.Look.canceled -= OnLookCanceled;

        // Deshabilitar los controles
        playerControls.Disable();
    }

    private void OnDestroy()
    {
        // Liberar recursos
        playerControls.Dispose();
    }

    // Callbacks para el Input System
    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
        isUsingGamepad = IsCurrentlyUsingGamepad();
    }

    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
    }

    // Método mejorado para detectar si estamos usando gamepad
    private bool IsCurrentlyUsingGamepad()
    {
        if (Gamepad.current != null)
        {
            // Verificar si hay actividad reciente en el gamepad
            return Gamepad.current.rightStick.ReadValue().magnitude > gamepadDeadzone ||
                   Gamepad.current.wasUpdatedThisFrame;
        }
        return false;
    }

    private void Update()
    {
        // Calcular compensación de framerate si está habilitada
        if (frameRateCompensation)
        {
            deltaTimeMultiplier = Mathf.Clamp(Time.unscaledDeltaTime * 60f, 0.5f, 2f);
        }

        // Manejar el input del mouse específicamente
        HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        if (Mouse.current != null)
        {
            // Capturar la posición actual del mouse
            mousePosition = Mouse.current.position.ReadValue();

            // Calcular el delta del mouse manualmente
            mouseDelta = mousePosition - lastMousePosition;

            // Verificar si el usuario está usando el mouse para la rotación
            if (mouseDelta.magnitude > 0.1f)
            {
                usingMouse = true;
            }

            // Guardar la posición del mouse para el próximo frame
            lastMousePosition = mousePosition;
        }
    }

    private void LateUpdate()
    {
        // Determinar qué método de input estamos usando
        bool isUsingMouse = !isUsingGamepad && usingMouse;

        // Verificar si podemos rotar con el mouse según la configuración
        bool canRotateWithMouse = !requireMouseButtonToRotate ||
                          (requireMouseButtonToRotate && IsMouseButtonPressed());

        // Procesar input de gamepad con mejoras
        if (isUsingGamepad && lookInput.magnitude > gamepadDeadzone)
        {
            ProcessGamepadInput();
        }
        // Procesar input de mouse
        else if (isUsingMouse && canRotateWithMouse && mouseDelta.magnitude > 0.1f)
        {
            ProcessMouseInput();
        }
        // No hay input válido
        else
        {
            ProcessNoInput();
        }

        // Resetear usingMouse cada frame para que necesite confirmación constante
        usingMouse = false;
    }

    private void ProcessGamepadInput()
    {
        // Aplicar zona muerta
        Vector2 processedInput = ApplyDeadzone(lookInput, gamepadDeadzone);

        // Aplicar inversiones
        if (invertGamepadX) processedInput.x = -processedInput.x;
        if (invertGamepadY) processedInput.y = -processedInput.y;

        // Aplicar sensibilidades individuales
        processedInput.x *= gamepadXSensitivity;
        processedInput.y *= gamepadYSensitivity;

        // Calcular la magnitud del input para la curva de aceleración
        float inputMagnitude = processedInput.magnitude;

        // Aplicar curva de aceleración
        float accelerationMultiplier = gamepadAccelerationCurve.Evaluate(inputMagnitude);

        // Sistema de aceleración temporal
        float targetAcceleration = accelerationMultiplier * gamepadSpeedMultiplier;

        // Interpolar la aceleración basada en si estamos acelerando o desacelerando
        float accelerationSpeed = inputMagnitude > lastInputMagnitude ?
            1f / gamepadAccelerationTime : 1f / gamepadDecelerationTime;

        currentAcceleration = Mathf.Lerp(currentAcceleration, targetAcceleration,
            Time.unscaledDeltaTime * accelerationSpeed);

        // Aplicar suavizado con velocidad variable
        float smoothingFactor = Mathf.Lerp(lookSmoothing * 2f, lookSmoothing * 0.5f, inputMagnitude);
        targetLookInput = processedInput * currentAcceleration;
        smoothedLookInput = Vector2.Lerp(smoothedLookInput, targetLookInput,
            Time.unscaledDeltaTime / smoothingFactor);

        // Aplicar compensación de framerate
        Vector2 finalInput = smoothedLookInput * deltaTimeMultiplier;

        // Aplicar la rotación
        freeLookCamera.m_XAxis.m_InputAxisValue = finalInput.x * gamepadLookSpeed * Time.unscaledDeltaTime;
        freeLookCamera.m_YAxis.m_InputAxisValue = finalInput.y * gamepadLookSpeed * Time.unscaledDeltaTime;

        lastInputMagnitude = inputMagnitude;
    }

    private void ProcessMouseInput()
    {
        // Para el mouse SIEMPRE invertimos el eje Y para comportamiento natural
        float yDelta = -mouseDelta.y;

        // Aplicar compensación de framerate y sensibilidades
        Vector2 mouseInput = new Vector2(
            mouseDelta.x * mouseXSpeed * 0.01f * deltaTimeMultiplier,
            yDelta * mouseYSpeed * 0.01f * deltaTimeMultiplier
        );

        // Aplicar directamente sin suavizado para respuesta inmediata del mouse
        freeLookCamera.m_XAxis.m_InputAxisValue = mouseInput.x;
        freeLookCamera.m_YAxis.m_InputAxisValue = mouseInput.y;

        // Resetear aceleración del gamepad
        currentAcceleration = 0f;
        lastInputMagnitude = 0f;
    }

    private void ProcessNoInput()
    {
        // Suavizar hacia cero cuando no hay input
        float dampingSpeed = 10f;

        freeLookCamera.m_XAxis.m_InputAxisValue = Mathf.Lerp(
            freeLookCamera.m_XAxis.m_InputAxisValue, 0,
            Time.unscaledDeltaTime * dampingSpeed);

        freeLookCamera.m_YAxis.m_InputAxisValue = Mathf.Lerp(
            freeLookCamera.m_YAxis.m_InputAxisValue, 0,
            Time.unscaledDeltaTime * dampingSpeed);

        // Suavizar hacia cero los valores internos
        smoothedLookInput = Vector2.Lerp(smoothedLookInput, Vector2.zero,
            Time.unscaledDeltaTime * dampingSpeed);

        currentAcceleration = Mathf.Lerp(currentAcceleration, 0f,
            Time.unscaledDeltaTime * dampingSpeed);

        lastInputMagnitude = 0f;
    }

    // Aplicar zona muerta circular mejorada
    private Vector2 ApplyDeadzone(Vector2 input, float deadzone)
    {
        float magnitude = input.magnitude;

        if (magnitude < deadzone)
        {
            return Vector2.zero;
        }

        // Remapear el rango [deadzone, 1] a [0, 1] para suavizar la transición
        float normalizedMagnitude = (magnitude - deadzone) / (1f - deadzone);
        return input.normalized * normalizedMagnitude;
    }

    // Verificar si algún botón del mouse está presionado según la configuración
    private bool IsMouseButtonPressed()
    {
        if (Mouse.current == null) return false;

        return (mouseButton == 0 && Mouse.current.leftButton.isPressed) ||
               (mouseButton == 1 && Mouse.current.rightButton.isPressed) ||
               (mouseButton == 2 && Mouse.current.middleButton.isPressed);
    }

    // Métodos públicos para ajustar parámetros en tiempo de ejecución
    public void SetGamepadLookSpeed(float speed)
    {
        gamepadLookSpeed = speed;
    }

    public void SetMouseXSpeed(float speed)
    {
        mouseXSpeed = speed;
    }

    public void SetMouseYSpeed(float speed)
    {
        mouseYSpeed = speed;
    }

    public void SetGamepadSensitivity(float xSensitivity, float ySensitivity)
    {
        gamepadXSensitivity = xSensitivity;
        gamepadYSensitivity = ySensitivity;
    }

    public void SetGamepadInversion(bool invertX, bool invertY)
    {
        invertGamepadX = invertX;
        invertGamepadY = invertY;
    }

    public void RequireMouseButtonToRotate(bool require)
    {
        requireMouseButtonToRotate = require;
    }

    public void SetMouseButton(int button)
    {
        if (button >= 0 && button <= 2)
        {
            mouseButton = button;
        }
    }

    public void SetGamepadDeadzone(float deadzone)
    {
        gamepadDeadzone = Mathf.Clamp01(deadzone);
    }

    // Método para aplicar un preset de configuración
    public void ApplyComfortPreset(string presetName)
    {
        switch (presetName.ToLower())
        {
            case "responsive":
                gamepadLookSpeed = 400f;
                gamepadAccelerationTime = 0.1f;
                gamepadDecelerationTime = 0.05f;
                lookSmoothing = 0.3f;
                break;

            case "smooth":
                gamepadLookSpeed = 250f;
                gamepadAccelerationTime = 0.3f;
                gamepadDecelerationTime = 0.2f;
                lookSmoothing = 0.7f;
                break;

            case "cinematic":
                gamepadLookSpeed = 200f;
                gamepadAccelerationTime = 0.5f;
                gamepadDecelerationTime = 0.3f;
                lookSmoothing = 0.8f;
                break;
        }
    }
}