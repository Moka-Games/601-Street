using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaberintoController : MonoBehaviour
{
    [SerializeField] private int rotationSpeed; 

    void Update()
    {
        float rotationX = 0f;
        float rotationZ = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            rotationX = 1f; 
        }
        if (Input.GetKey(KeyCode.S))
        {
            rotationX = -1f; 
        }
        if (Input.GetKey(KeyCode.A))
        {
            rotationZ = 1f; 
        }
        if (Input.GetKey(KeyCode.D))
        {
            rotationZ = -1f; 
        }

        Vector3 rotation = new Vector3(rotationX, 0f, rotationZ) * rotationSpeed * Time.deltaTime;

        transform.Rotate(rotation);
    }
}