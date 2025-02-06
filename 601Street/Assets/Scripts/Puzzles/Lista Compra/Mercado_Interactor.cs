using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;

public class Mercado_Interactor : MonoBehaviour
{

    private bool letterOpened = false;

    public float raycastDistance = 1.5f;
    public LayerMask collectibleLayerMask;

    private CapsuleCollider playerCollider;

    private bool objectInRange = false;

    public GameObject[] listaObjectos_Feedback;

    private void Start()
    {
        playerCollider = GetComponent<CapsuleCollider>();

        for (int i = 0; i < listaObjectos_Feedback.Length; i++)
        {
            listaObjectos_Feedback[i].gameObject.SetActive(false);
        }
    }
    void Update()
    {
        RaycastHit hit;
        Vector3 raycastOrigin = playerCollider.bounds.center;

        objectInRange = Physics.Raycast(raycastOrigin, transform.forward, out hit, raycastDistance, collectibleLayerMask);

        if (objectInRange)
        {
            Debug.DrawRay(raycastOrigin, transform.forward * raycastDistance, Color.green);

            ObjectoLista objecto = hit.collider.GetComponent<ObjectoLista>();

            if (objecto != null && Input.GetKeyDown(KeyCode.E))
            {
                if (objecto.ID == 1)
                {
                    listaObjectos_Feedback[0].SetActive(true);
                    objecto.objectAddedToList = true;

                    Debug.Log(objecto.objectName + " recogido");
                }
                else if (objecto.ID == 2)
                {
                    listaObjectos_Feedback[1].SetActive(true);
                    objecto.objectAddedToList = true;

                    Debug.Log(objecto.objectName + " recogido");
                }
                else if (objecto.ID == 3)
                {
                    listaObjectos_Feedback[2].SetActive(true);
                    objecto.objectAddedToList = true;

                    Debug.Log(objecto.objectName + " recogido");
                }
                else if (objecto.ID == 4)
                {
                    listaObjectos_Feedback[3].SetActive(true);
                    objecto.objectAddedToList = true;

                    Debug.Log(objecto.objectName + " recogido");
                }
            }
        }
        else
        {
            Debug.DrawRay(raycastOrigin, transform.forward * raycastDistance, Color.red);
            if (letterOpened)
            {
                letterOpened = false;
            }
        }
    }
}
