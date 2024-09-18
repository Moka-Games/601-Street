using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    public GameObject inventario;
    public bool abierto;

    private void Start()
    {
        inventario.SetActive(false);
        abierto = false;
    }

    void Update()
    {
        // Obtener entrada del teclado
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Crear un vector de movimiento ajustado a la perspectiva isométrica
        Vector3 movement = new Vector3(horizontal, 0f, vertical);
        movement = Quaternion.Euler(0, 45, 0) * movement; // Rotar el vector 45 grados para la vista isométrica

        // Normalizar el vector para evitar moverse más rápido en diagonal
        if (movement.magnitude > 1)
        {
            movement.Normalize();
        }

        // Mover el personaje
        transform.position += movement * moveSpeed * Time.deltaTime;

        //Inventario
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            AbrirCerrarInventario();
        }
    }

    public void AbrirCerrarInventario()
    {
        if (abierto == true) 
        {
            abierto = false;
            inventario.SetActive(false);
        }
        else
        {
            abierto = true;
            inventario.SetActive(true);
        }
    }

    public void CerrarInvenrtario()
    {
        abierto = false;
        inventario.SetActive(false);
    }
}