using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Canica : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Laberinto_Win"))
        {
            print("Win!!"); 
        }
    }
}
