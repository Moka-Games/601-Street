using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CinemachineFreeLook))]
public class FreeLookCameraController : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("Velocidad de rotación con el stick derecho del gamepad")]
    [SerializeField] private float gamepadLookSpeed = 300f;

    [Tooltip("Velocidad de rotación horizontal con el mouse")]
    [SerializeField] private float mouseXSpeed = 300f;

    [Tooltip("Velocidad de rotación vertical con el mouse")]
    [SerializeField] private float mouseYSpeed = 200f;

    [Tooltip("Botón para habilitar la rotación con el mouse (opcional)")]
    [SerializeField] private bool requireMouseButtonToRotate = true;

    [Tooltip("Botón del mouse que habilita la rotación (0 = izquierdo, 1 = derecho, 2 = medio)")]
    [SerializeField] private int mouseButton = 1;

    [Header("Rotation Settings")]
    [Tooltip("Suavizado de movimiento")]
    [Range(0.01f, 1f)]
    [SerializeField] private float lookSmoothing = 0.5f;

    // Referencias internas
    private CinemachineFreeLook freeLookCamera;
    private PlayerControls playerControls;
    private Vector2 lookInput;
    private Vector2 smoothedLookInput;
    private bool isUsingGamepad = false;
    private bool usingMouse = false;

    // Referencias para el input del mouse
    private Vector2 mousePosition;
    private Vector2 lastMousePosition;
    private Vector2 mouseDelta;

    // Método para detectar si estamos usando gamepad
    private bool IsCurrentlyUsingGamepad()
    {
        if (Gamepad.current != null)
        {
            // Si hay alguna actividad en el gamepad, asumimos que estamos usándolo
            return Gamepad.current.wasUpdatedThisFrame;
        }
        return false;
    }

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

    private void Update()
    {
        // Manejar el input del mouse específicamente
        if (Mouse.current != null)
        {
            // Capturar la posición actual del mouse
            mousePosition = Mouse.current.position.ReadValue();

            // Calcular el delta del mouse manualmente 
            // (más preciso que depender del binding en algunas situaciones)
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
        // Primero verificamos si estamos usando el gamepad o el mouse
        bool isUsingMouse = !isUsingGamepad && usingMouse;

        // Verificamos si podemos rotar con el botón del mouse si es necesario
        bool canRotateWithMouse = !requireMouseButtonToRotate ||
                           (mouseButton == 0 && Mouse.current != null && Mouse.current.leftButton.isPressed) ||
                           (mouseButton == 1 && Mouse.current != null && Mouse.current.rightButton.isPressed) ||
                           (mouseButton == 2 && Mouse.current != null && Mouse.current.middleButton.isPressed);

        // Si estamos usando el gamepad, usamos directamente el lookInput
        if (isUsingGamepad && lookInput.magnitude > 0.1f)
        {
            // Aplicamos suavizado
            smoothedLookInput = Vector2.Lerp(smoothedLookInput, lookInput, Time.deltaTime / lookSmoothing);

            // Para el gamepad NO invertimos el eje Y
            float yInput = smoothedLookInput.y;

            // Aplicar la rotación con la velocidad del gamepad
            freeLookCamera.m_XAxis.m_InputAxisValue = smoothedLookInput.x * gamepadLookSpeed * Time.deltaTime;
            freeLookCamera.m_YAxis.m_InputAxisValue = yInput * gamepadLookSpeed * Time.deltaTime;
        }
        // Si estamos usando el mouse y podemos rotar, usamos el mouseDelta
        else if (isUsingMouse && canRotateWithMouse && mouseDelta.magnitude > 0.1f)
        {
            // Para el mouse SIEMPRE invertimos el eje Y
            // Nota: en Unity, el movimiento hacia arriba del mouse es positivo,
            // pero para la cámara, el movimiento hacia arriba debe ser negativo,
            // así que lo invertimos con un -mouseDelta.y
            float yDelta = -mouseDelta.y;

            // Aplicamos la rotación con velocidades específicas para mouse
            // Nota: El mouse necesita valores más altos que el gamepad porque el delta es pequeño
            freeLookCamera.m_XAxis.m_InputAxisValue = mouseDelta.x * mouseXSpeed * 0.01f;
            freeLookCamera.m_YAxis.m_InputAxisValue = yDelta * mouseYSpeed * 0.01f;
        }
        else
        {
            // Si no hay input o no podemos rotar, gradualmente reducimos la velocidad a cero
            freeLookCamera.m_XAxis.m_InputAxisValue = Mathf.Lerp(freeLookCamera.m_XAxis.m_InputAxisValue, 0, Time.deltaTime * 10f);
            freeLookCamera.m_YAxis.m_InputAxisValue = Mathf.Lerp(freeLookCamera.m_YAxis.m_InputAxisValue, 0, Time.deltaTime * 10f);

            // Reseteamos el smoothedLookInput
            smoothedLookInput = Vector2.Lerp(smoothedLookInput, Vector2.zero, Time.deltaTime * 10f);
        }

        // Resetear usingMouse cada frame para que necesite confirmación constante
        usingMouse = false;
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
}