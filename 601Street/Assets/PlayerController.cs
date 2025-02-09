using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;

    private Vector3 velocity;

    public CharacterController controller;

    private Animator animator;

    private bool forwardPressed = false; 
    private bool horizontalPressedAfterForward = false; 

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Falta por asignar el animator");
        }
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
            }

            if (x != 0 && forwardPressed)
            {
                horizontalPressedAfterForward = true; 
            }

            if (forwardPressed && horizontalPressedAfterForward)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
            else if (x == 0)
            {
                Quaternion toRotation = Quaternion.LookRotation(forward);
                transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 10f);
            }
        }
        else
        {
            if (z == 0)
            {
                forwardPressed = false; 
                horizontalPressedAfterForward = false; 
            }

            if (x == 0)
            {
                Quaternion toRotation = Quaternion.LookRotation(forward);
                transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, Time.deltaTime * 10f);
            }
        }
    }
}
