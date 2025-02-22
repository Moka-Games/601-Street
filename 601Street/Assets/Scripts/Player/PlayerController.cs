using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;
    public float rotationSpeed = 5f;

    private Vector3 velocity;
    public CharacterController controller;
    private Animator animator;

    private bool isMoving = false;
    private Quaternion targetRotation;
    private float currentRotationSpeed;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Falta por asignar el animator");
        }
        targetRotation = transform.rotation;
        currentRotationSpeed = rotationSpeed;
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = right * x + forward * z;

        if (move.magnitude > 0.1f)
        {
            isMoving = true;
            HandleMovement(move, forward);
        }
        else
        {
            isMoving = false;
        }

        HandleRotation(x, z, move, forward);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleMovement(Vector3 move, Vector3 forward)
    {
        float currentSpeed = speed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = sprintSpeed;
        }

        controller.Move(move.normalized * currentSpeed * Time.deltaTime);
    }

    private void HandleRotation(float x, float z, Vector3 move, Vector3 forward)
    {
        if (isMoving)
        {
            targetRotation = Quaternion.LookRotation(move);
            currentRotationSpeed = rotationSpeed;
        }
        else
        {
            targetRotation = transform.rotation;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * currentRotationSpeed);
    }

    public void Respawn(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (controller != null)
        {
            controller.enabled = false;
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            controller.enabled = true;
            Debug.Log("Jugador movido al punto de spawn");
        }
        else
        {
            Debug.LogError("No se encontró 'CharacterController'!");
        }
    }
}