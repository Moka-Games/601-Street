using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Canica : MonoBehaviour
{
    public UnityEvent OnWin;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Laberinto_Win"))
        {
            OnWin.Invoke();
        }
    }

    public void WinFunction()
    {
        print("Has ganado!!??");
    }
}

