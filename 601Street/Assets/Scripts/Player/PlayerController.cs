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

    [Header("Rotation Settings")]
    [SerializeField] private float baseRotationSpeed = 5f;
    [SerializeField] private float initialRotationMultiplier = 0.5f;
    [SerializeField] private float transitionRotationMultiplier = 0.3f;
    [SerializeField] private float diagonalRotationAngle = 45f;
    [SerializeField] private float rotationThreshold = 0.1f;

    [Header("Required Components")]
    [SerializeField] private CharacterController characterController;

    // New Input System
    private PlayerControls playerControls;
    private Vector2 currentMovementInput;
    private bool isSprinting = false;

    private MovementState movementState;
    private RotationState rotationState;
    private Camera mainCamera;

    [Header("Animation")]
    private Animator animator;
    [SerializeField] private float animationSmoothTime = 0.1f;
    [SerializeField] private float afkTimeThreshold = 30f;


    private int isWalkingHash;
    private int isRunningHash;
    private int isWalkingBackHash;
    private int isWalkingLeftHash;
    private int isWalkingRightHash;
    private int triggerAfkHash; 


    private bool isWalking = false;
    private bool isRunning = false;
    private bool isWalkingBack = false;
    private bool isWalkingLeft = false;
    private bool isWalkingRight = false;

    private bool isPlayingAfkAnimation = false;
    private float inactivityTimer = 0f;
    private bool isAfk = false;

    private Vector3 fixedRightDirection; // Direcci�n "derecha" inicial
    private Vector3 fixedLeftDirection;  // Direcci�n "izquierda" inicial
    private bool isMovingPurelyHorizontal = false; // Indica si el movimiento es puramente horizontal
    private bool lastMoveWasHorizontal = false;    // Indica si el �ltimo movimiento fue horizontal
    private float lastHorizontalInput = 0f;
    private struct MovementState
    {
        public Vector3 Velocity;
        public Vector3 MoveDirection;
        public bool IsMoving;
        public float CurrentSpeed;
    }

    private struct RotationState
    {
        public bool ForwardPressed;
        public bool HorizontalPressedAfterForward;
        public bool WasMovingForwardAndHorizontal;
        public bool IsTransitioning;
        public float CurrentRotationSpeed;
        public Quaternion TargetRotation;
    }

    private void Awake()
    {
        // Initialize the input system
        playerControls = new PlayerControls();
        playerControls.Gameplay.AddCallbacks(this);

        InitializeComponents();
        InitializeStates();

        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        isWalkingBackHash = Animator.StringToHash("isWalkingBack");
        isWalkingLeftHash = Animator.StringToHash("isWalkingLeft");
        isWalkingRightHash = Animator.StringToHash("isWalkingRight");
        triggerAfkHash = Animator.StringToHash("triggerAfk");

    }

    private void OnEnable()
    {
        // Enable the input actions
        playerControls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        // Disable the input actions
        playerControls.Gameplay.Disable();
    }

    private void OnDestroy()
    {
        // Dispose of the input action asset
        playerControls.Dispose();
    }

    // Input System callbacks implementation
    public void OnWalking(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        // For a button action, we can check if it's pressed using ReadValueAsButton()
        isSprinting = context.ReadValueAsButton();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        // Este m�todo necesita ser implementado debido a la interfaz,
        // pero la l�gica real est� en PlayerInteraction
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // La l�gica de rotaci�n de c�mara se maneja en el FreeLookCameraController
        // Si no existe ese script, esta implementaci�n vac�a evita el error
    }

    private void InitializeComponents()
    {
        mainCamera = Camera.main;

        // Si el animator no est� asignado, intentar obtenerlo
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

        rotationState = new RotationState
        {
            ForwardPressed = false,
            HorizontalPressedAfterForward = false,
            WasMovingForwardAndHorizontal = false,
            IsTransitioning = false,
            CurrentRotationSpeed = baseRotationSpeed,
            TargetRotation = transform.rotation
        };
    }
    private void Update()
    {
        UpdateAnimationState();
        CheckInactivity();
    }

    private void FixedUpdate()
    {
        // Verificar si el CharacterController est� habilitado antes de ejecutar la l�gica
        if (characterController == null || !characterController.enabled || !gameObject.activeInHierarchy)
        {
            return; // No ejecutar la l�gica de movimiento si el controller est� desactivado
        }

        // Use the input from the Input System
        Vector2 input = currentMovementInput;

        UpdateMovement(input);
        UpdateRotation(input);
        ApplyGravity();
    }

    private void UpdateMovement(Vector2 input)
    {
        // Verificaci�n adicional por seguridad
        if (!characterController.enabled) return;

        (Vector3 forward, Vector3 right) = GetCameraDirections();

        // Detectar si estamos empezando un movimiento puramente horizontal
        bool isPurelyHorizontal = Mathf.Abs(input.x) > movementThreshold && Mathf.Abs(input.y) < movementThreshold;

        // Si acabamos de empezar un movimiento puramente horizontal
        if (isPurelyHorizontal && !lastMoveWasHorizontal)
        {
            // Guardar la direcci�n inicial
            fixedRightDirection = right;
            fixedLeftDirection = -right;
            isMovingPurelyHorizontal = true;
        }
        // Si hemos dejado de movernos horizontalmente o hemos a�adido movimiento vertical
        else if (!isPurelyHorizontal)
        {
            isMovingPurelyHorizontal = false;
        }

        // Actualizar el estado del movimiento
        lastMoveWasHorizontal = isPurelyHorizontal;

        // Calcular direcci�n de movimiento, usando direcciones fijas si es necesario
        movementState.MoveDirection = CalculateMoveDirection(input, forward, right);
        movementState.IsMoving = movementState.MoveDirection.magnitude > movementThreshold;
        movementState.CurrentSpeed = CalculateCurrentSpeed(input.y);

        if (movementState.IsMoving && characterController.enabled)
        {
            characterController.Move(movementState.MoveDirection * movementState.CurrentSpeed * Time.deltaTime);
        }

        // Guardar el input horizontal para la pr�xima comparaci�n
        if (Mathf.Abs(input.x) > movementThreshold)
            lastHorizontalInput = input.x;
    }

    private (Vector3 forward, Vector3 right) GetCameraDirections()
    {
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;

        forward.y = 0f;
        right.y = 0f;

        return (forward.normalized, right.normalized);
    }

    private Vector3 CalculateMoveDirection(Vector2 input, Vector3 forward, Vector3 right)
    {
        Vector3 direction = Vector3.zero;

        // Si estamos en movimiento puramente horizontal y tenemos direcciones fijas
        if (isMovingPurelyHorizontal && fixedRightDirection != Vector3.zero)
        {
            // Usar la direcci�n fija seg�n el signo del input
            if (input.x > 0)
                direction = fixedRightDirection * input.x;
            else
                direction = fixedLeftDirection * -input.x; // Negativo porque fixedLeftDirection ya es negativa
        }
        else
        {
            // Comportamiento original para movimiento no puramente horizontal
            direction = right * input.x;

            if (input.y > 0)
            {
                direction += forward * input.y;
            }
            else if (input.y < 0)
            {
                direction += -forward * Mathf.Abs(input.y);
            }
        }

        return direction;
    }

    private float CalculateCurrentSpeed(float verticalInput)
    {
        // Use the sprint status from the Input System instead of the old Input.GetKey
        if (verticalInput > 0 && isSprinting && Mathf.Approximately(currentMovementInput.x, 0))
        {
            return sprintSpeed;
        }
        else if (verticalInput < 0)
        {
            return baseSpeed * backwardsSpeedMultiplier;
        }
        return baseSpeed;
    }

    private void UpdateRotation(Vector2 input)
    {
        // Si no hay movimiento o es SOLO movimiento horizontal, no rotamos
        if (!movementState.IsMoving || IsPurelyHorizontalMovement(input))
        {
            // Mantener la rotaci�n actual del personaje
            rotationState.TargetRotation = transform.rotation;
            return;
        }

        // Solo procesamos rotaci�n si hay movimiento vertical o diagonal
        HandleRotationStates(input);
        ApplyRotation();
        CheckRotationCompletion();
    }

    private void HandleRotationStates(Vector2 input)
    {
        if (input.y != 0)
        {
            HandleVerticalRotation(input);
        }
        else
        {
            HandleHorizontalOnlyRotation(input.x);
        }
    }

    private void HandleVerticalRotation(Vector2 input)
    {
        if (!rotationState.ForwardPressed)
        {
            InitializeForwardMovement();
        }

        if (input.x != 0)
        {
            HandleDiagonalMovement(input);
        }
        else
        {
            HandleStraightMovement(input.y);
        }
    }

    private void InitializeForwardMovement()
    {
        rotationState.ForwardPressed = true;
        rotationState.HorizontalPressedAfterForward = false;
        rotationState.WasMovingForwardAndHorizontal = false;
        rotationState.IsTransitioning = true;
        rotationState.CurrentRotationSpeed = baseRotationSpeed * initialRotationMultiplier;
    }

    private void HandleDiagonalMovement(Vector2 input)
    {
        rotationState.HorizontalPressedAfterForward = true;
        rotationState.WasMovingForwardAndHorizontal = true;

        Vector3 forward = mainCamera.transform.forward;
        forward.y = 0;
        forward.Normalize();

        if (input.y < 0)
        {
            rotationState.TargetRotation = Quaternion.LookRotation(forward);
            rotationState.TargetRotation *= Quaternion.Euler(0, -diagonalRotationAngle * Mathf.Sign(input.x), 0);
        }
        else
        {
            Vector3 rotatedDirection = Quaternion.Euler(0, diagonalRotationAngle * Mathf.Sign(input.x), 0) * forward;
            rotationState.TargetRotation = Quaternion.LookRotation(rotatedDirection);
        }

        rotationState.CurrentRotationSpeed = Mathf.Lerp(rotationState.CurrentRotationSpeed, baseRotationSpeed, Time.deltaTime);
    }

    private void HandleStraightMovement(float verticalInput)
    {
        Vector3 forward = mainCamera.transform.forward;
        forward.y = 0;
        forward.Normalize();

        rotationState.TargetRotation = Quaternion.LookRotation(forward);
        rotationState.HorizontalPressedAfterForward = false;
        rotationState.CurrentRotationSpeed = Mathf.Lerp(rotationState.CurrentRotationSpeed, baseRotationSpeed, Time.deltaTime);
    }

    private void HandleHorizontalOnlyRotation(float horizontalInput)
    {
        if (rotationState.WasMovingForwardAndHorizontal && horizontalInput != 0)
        {
            if (!rotationState.IsTransitioning)
            {
                rotationState.IsTransitioning = true;
                rotationState.CurrentRotationSpeed = baseRotationSpeed * transitionRotationMultiplier;
            }
            Vector3 forward = mainCamera.transform.forward;
            forward.y = 0;
            forward.Normalize();
            rotationState.TargetRotation = Quaternion.LookRotation(forward);
        }
        else if (horizontalInput == 0)
        {
            ResetMovementState();
        }

        rotationState.CurrentRotationSpeed = Mathf.Lerp(rotationState.CurrentRotationSpeed, baseRotationSpeed, Time.deltaTime);
    }

    private void ResetMovementState()
    {
        rotationState.ForwardPressed = false;
        rotationState.HorizontalPressedAfterForward = false;
        rotationState.WasMovingForwardAndHorizontal = false;

        Vector3 forward = mainCamera.transform.forward;
        forward.y = 0;
        forward.Normalize();
        rotationState.TargetRotation = Quaternion.LookRotation(forward);
        rotationState.CurrentRotationSpeed = baseRotationSpeed;
    }

    private void ApplyRotation()
    {
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotationState.TargetRotation,
            Time.deltaTime * rotationState.CurrentRotationSpeed
        );
    }

    private void CheckRotationCompletion()
    {
        if (Quaternion.Angle(transform.rotation, rotationState.TargetRotation) < rotationThreshold)
        {
            rotationState.IsTransitioning = false;
            rotationState.CurrentRotationSpeed = baseRotationSpeed;
        }
    }

    private void ApplyGravity()
    {
        // Verificaci�n adicional por seguridad
        if (!characterController.enabled) return;

        movementState.Velocity.y += gravity * Time.deltaTime;
        characterController.Move(movementState.Velocity * Time.deltaTime);
    }

    public void SetMovementEnabled(bool enabled)
    {
        if (characterController != null)
        {
            // Si estamos desactivando el controlador, resetear tambi�n los inputs de movimiento
            if (!enabled)
            {
                // Resetear los inputs de movimiento
                currentMovementInput = Vector2.zero;
                isSprinting = false;

                // Forzar actualizaci�n de animaci�n inmediatamente
                if (animator != null)
                {
                    // Asegurar que todas las animaciones de movimiento est�n desactivadas
                    animator.SetBool(isWalkingHash, false);
                    animator.SetBool(isRunningHash, false);
                    animator.SetBool(isWalkingBackHash, false);
                    animator.SetBool(isWalkingLeftHash, false);
                    animator.SetBool(isWalkingRightHash, false);
                }
            }

            characterController.enabled = enabled;
        }
        else
        {
            Debug.LogError($"CharacterController not found on {gameObject.name}!");
        }
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
            Debug.LogError("No se encontr� 'CharacterController'!");
        }
    }

    private void UpdateAnimationState()
    {
        // Verificar si el CharacterController est� habilitado
        if (characterController == null || !characterController.enabled || !gameObject.activeInHierarchy)
        {
            // NUEVO C�DIGO: Cuando el CharacterController est� desactivado,
            // forzar el estado de idle reseteando todas las variables de animaci�n
            if (animator != null)
            {
                // Resetear todos los estados de animaci�n a false
                isWalking = false;
                isRunning = false;
                isWalkingBack = false;
                isWalkingLeft = false;
                isWalkingRight = false;

                // Actualizar los par�metros del Animator para forzar estado idle
                animator.SetBool(isWalkingHash, false);
                animator.SetBool(isRunningHash, false);
                animator.SetBool(isWalkingBackHash, false);
                animator.SetBool(isWalkingLeftHash, false);
                animator.SetBool(isWalkingRightHash, false);

                // Tambi�n asegurarnos de que no est� en estado AFK
                isPlayingAfkAnimation = false;

                // Si hay alg�n par�metro espec�fico para el idle, podr�amos activarlo aqu�
                // Por ejemplo: animator.SetBool("isIdle", true);
            }
            return;
        }


        if (isPlayingAfkAnimation && animator.GetCurrentAnimatorStateInfo(0).IsName("Afk_Animation"))
            return;

        // Determinar el tipo de movimiento basado en los inputs
        bool movingForward = currentMovementInput.y > 0;
        bool movingBackward = currentMovementInput.y < 0;
        bool movingLeft = currentMovementInput.x < 0;
        bool movingRight = currentMovementInput.x > 0;
        bool moving = currentMovementInput.magnitude > movementThreshold;

        // Determinar si estamos corriendo
        bool shouldRun = isSprinting && movingForward && Mathf.Approximately(currentMovementInput.x, 0);

        // Actualizar los estados de animaci�n
        isWalking = moving && movingForward && !shouldRun && !movingLeft && !movingRight;
        isRunning = shouldRun;
        isWalkingBack = moving && movingBackward && !movingLeft && !movingRight;
        isWalkingLeft = moving && movingLeft && !movingForward && !movingBackward;
        isWalkingRight = moving && movingRight && !movingForward && !movingBackward;

        // Para movimientos diagonales, priorizar adelante/atr�s sobre izquierda/derecha
        if ((movingForward || movingBackward) && (movingLeft || movingRight))
        {
            isWalkingLeft = false;
            isWalkingRight = false;

            if (movingForward)
            {
                isWalking = !shouldRun;
                isRunning = shouldRun;
                isWalkingBack = false;
            }
            else if (movingBackward)
            {
                isWalkingBack = true;
                isWalking = false;
                isRunning = false;
            }
        }

        // Actualizar los par�metros del Animator
        if (animator != null)
        {
            animator.SetBool(isWalkingHash, isWalking);
            animator.SetBool(isRunningHash, isRunning);
            animator.SetBool(isWalkingBackHash, isWalkingBack);
            animator.SetBool(isWalkingLeftHash, isWalkingLeft);
            animator.SetBool(isWalkingRightHash, isWalkingRight);
        }
    }
    private void CheckInactivity()
    {
        // Verificamos si hay alg�n input de movimiento
        bool isMoving = currentMovementInput.magnitude > movementThreshold;

        // Si hay movimiento, resetear el timer y las flags
        if (isMoving || isSprinting)
        {
            inactivityTimer = 0f;
            isPlayingAfkAnimation = false;
            return;
        }

        // Si estamos reproduciendo la animaci�n AFK, no incrementamos el timer
        if (isPlayingAfkAnimation && animator.GetCurrentAnimatorStateInfo(0).IsName("Afk_Animation"))
        {
            return;
        }

        // Incrementar el timer si no hay movimiento y no estamos en animaci�n AFK
        inactivityTimer += Time.deltaTime;

        // Verificar si se alcanz� el l�mite de tiempo de inactividad
        if (inactivityTimer >= afkTimeThreshold)
        {
            // Activar la animaci�n AFK
            TriggerAfkAnimation();
            // Resetear el timer para que comience a contar de nuevo
            inactivityTimer = 0f;
        }
    }
    private bool IsPurelyHorizontalMovement(Vector2 input)
    {
        // Consideramos movimiento puramente horizontal si:
        // 1. Hay input horizontal significativo
        // 2. No hay input vertical significativo
        return Mathf.Abs(input.x) > movementThreshold && Mathf.Abs(input.y) < movementThreshold;
    }
    private void TriggerAfkAnimation()
    {
        if (animator != null)
        {
            // Activar el trigger para la animaci�n AFK
            animator.SetTrigger(triggerAfkHash);
            isPlayingAfkAnimation = true;

            Debug.Log("El jugador est� AFK, activando animaci�n");
        }
    }

    public void OnAfkAnimationComplete()
    {
        isPlayingAfkAnimation = false;
        isAfk = false;
    }
}