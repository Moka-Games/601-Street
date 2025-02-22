using UnityEditor;
using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;
    public float rotationSpeed = 5f; // Reducida para hacer la rotación más suave

    private Vector3 velocity;
    public CharacterController controller;
    private Animator animator;

    private bool forwardPressed = false;
    private bool horizontalPressedAfterForward = false;
    private bool wasMovingForwardAndHorizontal = false;
    private Quaternion targetRotation;
    private bool isTransitioning = false;
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

    void FixedUpdate()
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

        float currentSpeed = speed;
        if (z > 0 && Input.GetKey(KeyCode.LeftShift) && x == 0)
        {
            currentSpeed = sprintSpeed;
        }
        else if (z < 0)
        {
            currentSpeed *= 0.7f;
        }

        controller.Move(move * currentSpeed * Time.deltaTime);

        HandleRotation(x, z, move, forward);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleRotation(float x, float z, Vector3 move, Vector3 forward)
    {
        if (z > 0)
        {
            if (!forwardPressed)
            {
                forwardPressed = true;
                horizontalPressedAfterForward = false;
                wasMovingForwardAndHorizontal = false;
                isTransitioning = true;
                currentRotationSpeed = rotationSpeed * 0.5f; // Más lento al iniciar el movimiento
            }

            if (x != 0)
            {
                horizontalPressedAfterForward = true;
                wasMovingForwardAndHorizontal = true;
                targetRotation = Quaternion.LookRotation(move);
                currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, rotationSpeed, Time.deltaTime);
            }
            else
            {
                targetRotation = Quaternion.LookRotation(forward);
                horizontalPressedAfterForward = false;
                currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, rotationSpeed, Time.deltaTime);
            }
        }
        else
        {
            if (z == 0)
            {
                if (wasMovingForwardAndHorizontal && x != 0)
                {
                    if (!isTransitioning)
                    {
                        isTransitioning = true;
                        currentRotationSpeed = rotationSpeed * 0.3f; // Más lento durante la transición
                    }
                    targetRotation = Quaternion.LookRotation(forward);
                }
                else if (x == 0)
                {
                    forwardPressed = false;
                    horizontalPressedAfterForward = false;
                    wasMovingForwardAndHorizontal = false;
                    targetRotation = Quaternion.LookRotation(forward);
                    currentRotationSpeed = rotationSpeed;
                }

                currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, rotationSpeed, Time.deltaTime);
            }
        }

        // Aplicar la rotación de forma suave
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * currentRotationSpeed);

        // Comprobar si hemos llegado cerca de la rotación objetivo
        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            isTransitioning = false;
            currentRotationSpeed = rotationSpeed;
        }
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