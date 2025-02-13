using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Basic_Feedback : MonoBehaviour
{

    private bool feedbackIsActive;

    public GameObject feedbackVisual;

    private void Start()
    {
        feedbackVisual.SetActive(false);
    }
    void Update()
    {
        if(feedbackIsActive)
        {
            feedbackVisual.SetActive(true);
        }
        else if(!feedbackIsActive)
        {
            feedbackVisual.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            feedbackIsActive = true;

            print("Jugador dentro del collider");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            feedbackIsActive = false;

            print("Jugador fuera del collider");

        }
    }
}
