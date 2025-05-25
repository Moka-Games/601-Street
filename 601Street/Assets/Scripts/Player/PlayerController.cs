using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, PlayerControls.IGameplayActions
{
    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float backwardsSpeedMultiplier = 0.7f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float movementThreshold = 0.1f;

    [Header("Gamepad Movement Settings")]
    [Tooltip("Zona muerta para el stick de movimiento del gamepad")]
    [Range(0f, 1f)]
    [SerializeField] private float gamepadMovementDeadzone = 0.15f;

    [Tooltip("Multiplicador de velocidad cuando se usa gamepad")]
    [SerializeField] private float gamepadSpeedMultiplier = 1f;

    [Tooltip("Curva de aceleración para el movimiento con gamepad")]
    [SerializeField] private AnimationCurve gamepadMovementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Velocidad de parada cuando no hay input (más alto = para más rápido)")]
    [SerializeField] private float gamepadStopSpeed = 10f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 8f;
    [Tooltip("Si está activado, el personaje rotará hacia la dirección de movimiento")]
    [SerializeField] private bool rotateTowardsMovement = true;

    [Header("Required Components")]
    [SerializeField] private CharacterController characterController;

    [Header("Animation")]
    private Animator animator;
    [SerializeField] private float animationSmoothTime = 0.1f;
    [SerializeField] private float afkTimeThreshold = 30f;

    // New Input System
    private PlayerControls playerControls;
    private Vector2 currentMovementInput;
    private Vector2 rawMovementInput;
    private bool isSprinting = false;
    private bool isUsingGamepad = false;

    private MovementState movementState;
    private Camera mainCamera;

    // Animation hashes
    private int isWalkingHash;
    private int isRunningHash;
    private int isWalkingBackHash;
    private int triggerAfkHash;

    // Animation states
    private bool isWalking = false;
    private bool isRunning = false;
    private bool isWalkingBack = false;

    // AFK system
    private bool isPlayingAfkAnimation = false;
    private float inactivityTimer = 0f;
    private bool isAfk = false;

    // Gamepad-specific variables
    private Vector2 smoothedGamepadInput;
    private float gamepadInputSmoothTime = 0.1f;

    private struct MovementState
    {
        public Vector3 Velocity;
        public Vector3 MoveDirection;
        public bool IsMoving;
        public float CurrentSpeed;
    }

    private void Awake()
    {
        // Initialize the input system
        playerControls = new PlayerControls();
        playerControls.Gameplay.AddCallbacks(this);

        InitializeComponents();
        InitializeStates();
        InitializeAnimationHashes();
    }

    private void InitializeAnimationHashes()
    {
        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        isWalkingBackHash = Animator.StringToHash("isWalkingBack");
        triggerAfkHash = Animator.StringToHash("triggerAfk");
    }

    private void OnEnable()
    {
        playerControls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        playerControls.Gameplay.Disable();
    }

    private void OnDestroy()
    {
        playerControls.Dispose();
    }

    #region Input System Callbacks

    public void OnWalking(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            // Cuando se cancela el input, resetear inmediatamente
            rawMovementInput = Vector2.zero;
            if (!isUsingGamepad)
            {
                currentMovementInput = Vector2.zero;
            }
        }
        else
        {
            rawMovementInput = context.ReadValue<Vector2>();
        }

        isUsingGamepad = IsCurrentlyUsingGamepad();

        // Procesar el input según el dispositivo
        if (isUsingGamepad)
        {
            ProcessGamepadMovementInput();
        }
        else
        {
            currentMovementInput = rawMovementInput;
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.ReadValueAsButton();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        // Este método se implementa en PlayerInteraction
        // Lo dejamos vacío aquí para cumplir con la interfaz
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // La lógica de rotación de cámara se maneja en el FreeLookCameraController
        // Lo dejamos vacío aquí para cumplir con la interfaz
    }

    // Nuevos métodos requeridos por la interfaz (no los usa PlayerController)
    public void OnAcceptCall(InputAction.CallbackContext context)
    {
        // No se usa en PlayerController - se maneja en CallSystem
    }

    public void OnToggleInventory(InputAction.CallbackContext context)
    {
        // No se usa en PlayerController - se maneja en Inventory_Manager
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        // No se usa en PlayerController - se maneja en PauseMenu
    }

    public void OnSkipDialogue(InputAction.CallbackContext context)
    {
        // No se usa en PlayerController - se maneja en DialogueManager
    }

    public void OnLockpick(InputAction.CallbackContext context)
    {
        // No se usa en PlayerController - se maneja en LockPick
    }

    // Métodos adicionales para los nuevos inputs (si existen)
    public void OnTurn_Lockpick(InputAction.CallbackContext context)
    {
        // No se usa en PlayerController - se maneja en LockPick
    }

    public void OnTry_LockPick(InputAction.CallbackContext context)
    {
        // No se usa en PlayerController - se maneja en LockPick
    }

    #endregion

    private bool IsCurrentlyUsingGamepad()
    {
        if (Gamepad.current != null)
        {
            // Verificar si el input actual viene del gamepad
            return rawMovementInput.magnitude > 0.01f &&
                   (Gamepad.current.leftStick.ReadValue().magnitude > 0.01f ||
                    Gamepad.current.wasUpdatedThisFrame);
        }
        return false;
    }

    private void ProcessGamepadMovementInput()
    {
        // Aplicar zona muerta circular
        Vector2 processedInput = ApplyCircularDeadzone(rawMovementInput, gamepadMovementDeadzone);

        // Si no hay input significativo, detener inmediatamente
        if (processedInput.magnitude < 0.01f)
        {
            // Detener rápidamente pero suavemente
            smoothedGamepadInput = Vector2.Lerp(smoothedGamepadInput, Vector2.zero,
                Time.unscaledDeltaTime * gamepadStopSpeed);

            // Si ya estamos muy cerca de cero, forzar a cero
            if (smoothedGamepadInput.magnitude < 0.05f)
            {
                smoothedGamepadInput = Vector2.zero;
            }
        }
        else
        {
            // Aplicar curva de respuesta para movimiento más natural
            float magnitude = processedInput.magnitude;
            float curveValue = gamepadMovementCurve.Evaluate(magnitude);
            processedInput = processedInput.normalized * curveValue * gamepadSpeedMultiplier;

            // Suavizar el input del gamepad para movimiento más fluido
            smoothedGamepadInput = Vector2.Lerp(smoothedGamepadInput, processedInput,
                Time.unscaledDeltaTime / gamepadInputSmoothTime);
        }

        currentMovementInput = smoothedGamepadInput;
    }

    private Vector2 ApplyCircularDeadzone(Vector2 input, float deadzone)
    {
        float magnitude = input.magnitude;

        if (magnitude < deadzone)
        {
            return Vector2.zero;
        }

        // Remapear el rango [deadzone, 1] a [0, 1]
        float normalizedMagnitude = (magnitude - deadzone) / (1f - deadzone);
        return input.normalized * normalizedMagnitude;
    }

    private void InitializeComponents()
    {
        mainCamera = Camera.main;

        if (!animator) animator = GetComponent<Animator>();
        if (!characterController) characterController = GetComponent<CharacterController>();

        if (!characterController) Debug.LogError($"CharacterController not found on {gameObject.name}!");
        if (!animator) Debug.LogWarning($"Animator not assigned on {gameObject.name}");
    }

    private void InitializeStates()
    {
        movementState = new MovementState
        {
            Velocity = Vector3.zero,
            MoveDirection = Vector3.zero,
            IsMoving = false,
            CurrentSpeed = baseSpeed
        };
    }

    private void Update()
    {
        // Procesar input de gamepad continuamente para manejo de parada suave
        if (isUsingGamepad)
        {
            ProcessGamepadMovementInput();
        }

        UpdateAnimationState();
        CheckInactivity();
    }

    private void FixedUpdate()
    {
        // Verificar si el CharacterController está habilitado antes de ejecutar la lógica
        if (characterController == null || !characterController.enabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        UpdateMovement();
        UpdateRotation();
        ApplyGravity();
    }

    private void UpdateMovement()
    {
        if (!characterController.enabled) return;

        // Obtener direcciones de la cámara
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;

        // Proyectar en el plano horizontal
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calcular dirección de movimiento basada en el input y la cámara
        movementState.MoveDirection = (cameraForward * currentMovementInput.y + cameraRight * currentMovementInput.x).normalized;
        movementState.IsMoving = currentMovementInput.magnitude > movementThreshold;

        // Calcular velocidad
        movementState.CurrentSpeed = CalculateCurrentSpeed();

        // Aplicar movimiento
        if (movementState.IsMoving && characterController.enabled)
        {
            Vector3 moveVector = movementState.MoveDirection * movementState.CurrentSpeed * Time.deltaTime;
            characterController.Move(moveVector);
        }
    }

    private void UpdateRotation()
    {
        if (!rotateTowardsMovement || !movementState.IsMoving) return;

        if (movementState.MoveDirection != Vector3.zero)
        {
            // Detectar si se está moviendo hacia atrás
            bool isMovingBackward = currentMovementInput.y < -0.1f;

            Quaternion targetRotation;

            if (isMovingBackward)
            {
                // Para movimiento hacia atrás: la espalda debe apuntar hacia la dirección de movimiento
                // Esto significa que el frente (cara) debe apuntar en la dirección OPUESTA
                targetRotation = Quaternion.LookRotation(-movementState.MoveDirection);
            }
            else
            {
                // Para movimiento hacia adelante/lateral: comportamiento normal
                targetRotation = Quaternion.LookRotation(movementState.MoveDirection);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void ApplyGravity()
    {
        if (!characterController.enabled) return;

        movementState.Velocity.y += gravity * Time.deltaTime;
        characterController.Move(movementState.Velocity * Time.deltaTime);
    }

    public void SetMovementEnabled(bool enabled)
    {
        if (characterController != null)
        {
            if (!enabled)
            {
                // Resetear TODOS los inputs de movimiento inmediatamente
                currentMovementInput = Vector2.zero;
                rawMovementInput = Vector2.zero;
                smoothedGamepadInput = Vector2.zero;
                isSprinting = false;
                isUsingGamepad = false;

                // Forzar actualización de animación inmediatamente
                if (animator != null)
                {
                    ResetAllAnimationStates();
                }
            }

            characterController.enabled = enabled;
        }
        else
        {
            Debug.LogError($"CharacterController not found on {gameObject.name}!");
        }
    }

    private void ResetAllAnimationStates()
    {
        isWalking = false;
        isRunning = false;
        isWalkingBack = false;

        animator.SetBool(isWalkingHash, false);
        animator.SetBool(isRunningHash, false);
        animator.SetBool(isWalkingBackHash, false);
    }

    public void Respawn(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            characterController.enabled = true;
            Debug.Log("Jugador movido al punto de spawn");
        }
        else
        {
            Debug.LogError("No se encontró 'CharacterController'!");
        }
    }

    private void UpdateAnimationState()
    {
        // Verificar si el CharacterController está habilitado
        if (characterController == null || !characterController.enabled || !gameObject.activeInHierarchy)
        {
            if (animator != null)
            {
                ResetAllAnimationStates();
                isPlayingAfkAnimation = false;
            }
            return;
        }

        if (isPlayingAfkAnimation && animator.GetCurrentAnimatorStateInfo(0).IsName("Afk_Animation"))
            return;

        // Determinar el tipo de movimiento basado en los inputs
        Vector2 input = currentMovementInput;
        bool moving = input.magnitude > movementThreshold;

        // Resetear todos los estados
        isWalking = false;
        isRunning = false;
        isWalkingBack = false;

        if (moving)
        {
            // Determinar la dirección principal del movimiento
            bool movingBackward = input.y < -0.1f;

            // Si está sprintando (y no va hacia atrás)
            if (isSprinting && !movingBackward)
            {
                isRunning = true;
            }
            // Movimiento hacia atrás
            else if (movingBackward)
            {
                isWalkingBack = true;
            }
            // Cualquier otro movimiento (adelante, lateral, diagonal)
            else
            {
                isWalking = true;
            }
        }

        // Actualizar los parámetros del Animator
        if (animator != null)
        {
            animator.SetBool(isWalkingHash, isWalking);
            animator.SetBool(isRunningHash, isRunning);
            animator.SetBool(isWalkingBackHash, isWalkingBack);
        }
    }

    private void CheckInactivity()
    {
        // Verificar si hay algún input de movimiento
        bool isMoving = currentMovementInput.magnitude > movementThreshold;

        if (isMoving || isSprinting)
        {
            inactivityTimer = 0f;
            isPlayingAfkAnimation = false;
            return;
        }

        if (isPlayingAfkAnimation && animator.GetCurrentAnimatorStateInfo(0).IsName("Afk_Animation"))
        {
            return;
        }

        inactivityTimer += Time.deltaTime;

        if (inactivityTimer >= afkTimeThreshold)
        {
            TriggerAfkAnimation();
            inactivityTimer = 0f;
        }
    }

    private void TriggerAfkAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerAfkHash);
            isPlayingAfkAnimation = true;
            Debug.Log("El jugador está AFK, activando animación");
        }
    }

    public void OnAfkAnimationComplete()
    {
        isPlayingAfkAnimation = false;
        isAfk = false;
    }

    #region Public Methods for Configuration

    /// <summary>
    /// Configura los parámetros de movimiento del gamepad
    /// </summary>
    public void SetGamepadMovementSettings(float deadzone, float speedMultiplier, float smoothTime)
    {
        gamepadMovementDeadzone = Mathf.Clamp01(deadzone);
        gamepadSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
        gamepadInputSmoothTime = Mathf.Max(0.01f, smoothTime);
    }

    /// <summary>
    /// Configura la velocidad de rotación
    /// </summary>
    public void SetRotationSpeed(float speed)
    {
        rotationSpeed = Mathf.Max(0.1f, speed);
    }

    /// <summary>
    /// Habilita o deshabilita la rotación automática hacia la dirección de movimiento
    /// </summary>
    public void SetRotateTowardsMovement(bool enable)
    {
        rotateTowardsMovement = enable;
    }

    /// <summary>
    /// Obtiene información sobre el dispositivo de entrada actual
    /// </summary>
    public bool IsCurrentlyUsingGamepadMovement()
    {
        return isUsingGamepad;
    }

    /// <summary>
    /// Fuerza el reseteo del estado de entrada
    /// </summary>
    public void ResetInputState()
    {
        currentMovementInput = Vector2.zero;
        rawMovementInput = Vector2.zero;
        smoothedGamepadInput = Vector2.zero;
        isSprinting = false;
        isUsingGamepad = false;
    }
    private float CalculateCurrentSpeed()
    {
        // Calcular velocidad base según la dirección
        float baseSpeedToUse = baseSpeed;

        // Si va hacia atrás, aplicar multiplicador
        if (currentMovementInput.y < 0)
        {
            baseSpeedToUse *= backwardsSpeedMultiplier;
        }

        // Si está sprintando, aplicar velocidad de sprint
        if (isSprinting && movementState.IsMoving)
        {
            baseSpeedToUse = sprintSpeed;
        }

        // Aplicar multiplicador de gamepad si es necesario
        if (isUsingGamepad)
        {
            baseSpeedToUse *= gamepadSpeedMultiplier;
        }

        return baseSpeedToUse;
    }
    #endregion
}