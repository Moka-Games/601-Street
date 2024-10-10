using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f; // Velocidad de movimiento del jugador
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

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        // Salto
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Aplicar gravedad
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}

