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
    private Animator animator;

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
        // Este método necesita ser implementado debido a la interfaz,
        // pero la lógica real está en PlayerInteraction
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // La lógica de rotación de cámara se maneja en el FreeLookCameraController
        // Si no existe ese script, esta implementación vacía evita el error
    }

    private void InitializeComponents()
    {
        mainCamera = Camera.main;
        animator = GetComponent<Animator>();

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

    private void FixedUpdate()
    {
        // Verificar si el CharacterController está habilitado antes de ejecutar la lógica
        if (characterController == null || !characterController.enabled || !gameObject.activeInHierarchy)
        {
            return; // No ejecutar la lógica de movimiento si el controller está desactivado
        }

        // Use the input from the Input System
        Vector2 input = currentMovementInput;

        UpdateMovement(input);
        UpdateRotation(input);
        ApplyGravity();
    }

    private void UpdateMovement(Vector2 input)
    {
        // Verificación adicional por seguridad
        if (!characterController.enabled) return;

        (Vector3 forward, Vector3 right) = GetCameraDirections();

        movementState.MoveDirection = CalculateMoveDirection(input, forward, right);
        movementState.IsMoving = movementState.MoveDirection.magnitude > movementThreshold;
        movementState.CurrentSpeed = CalculateCurrentSpeed(input.y);

        if (movementState.IsMoving && characterController.enabled)
        {
            characterController.Move(movementState.MoveDirection * movementState.CurrentSpeed * Time.deltaTime);
        }
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
        Vector3 direction = right * input.x;

        if (input.y > 0)
        {
            direction += forward * input.y;
        }
        else if (input.y < 0)
        {
            direction += -forward * Mathf.Abs(input.y);
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
        if (!movementState.IsMoving)
        {
            rotationState.TargetRotation = transform.rotation;
            return;
        }

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
        // Verificación adicional por seguridad
        if (!characterController.enabled) return;

        movementState.Velocity.y += gravity * Time.deltaTime;
        characterController.Move(movementState.Velocity * Time.deltaTime);
    }

    public void SetMovementEnabled(bool enabled)
    {
        if (characterController != null)
        {
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
            Debug.LogError("No se encontró 'CharacterController'!");
        }
    }
}