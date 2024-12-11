using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f; // Velocidad de movimiento del jugador
    public float sprintSpeed = 8f; // Velocidad de sprint
    public float gravity = -9.81f; // Gravedad
    public float jumpHeight = 1.5f; // Altura del salto

    private Vector3 velocity; // Almacena la velocidad actual del jugador
    private bool isGrounded; // Indica si el jugador está en el suelo

    public CharacterController controller; // Referencia al componente CharacterController
    public Transform groundCheck; // Objeto que detecta si el jugador está en el suelo
    public float groundDistance = 0.4f; // Distancia para comprobar si el jugador toca el suelo
    public LayerMask groundMask; // Capa para identificar el suelo

    void Update()
    {
        // Verifica si el jugador está en el suelo
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Estabiliza la gravedad cuando está en el suelo
        }

        // Movimiento horizontal
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Direccionar el movimiento hacia la dirección de la cámara
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = right * x + forward * z;

        // Determinar si se está haciendo sprint solo hacia adelante
        float currentSpeed = (Input.GetKey(KeyCode.LeftShift) && z > 0) ? sprintSpeed : speed;

        controller.Move(move * currentSpeed * Time.deltaTime);

        // Rotar el jugador hacia la dirección de movimiento si avanza
        if (move.magnitude > 0 && z > 0)
        {
            Quaternion toRotation = Quaternion.LookRotation(forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 10f);
        }

        // Salto
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Aplicar gravedad
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
