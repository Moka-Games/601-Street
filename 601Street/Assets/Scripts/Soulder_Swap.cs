using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Soulder_Swap : MonoBehaviour
{
    public CinemachineFreeLook rightSoulder_Camera;
    public CinemachineFreeLook leftSoulder_Camera;

    private bool rightSoulder_IsActive = false;
    private bool leftSoulder_IsActive = false;
    void Start()
    {
        rightSoulder_IsActive = true;

        InitializeCamera();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            print("Input Pressed");
            if(rightSoulder_IsActive)
            {
                print("Enabling Right Soulder Camera...");

                Enable_LeftSoulder_Camera();
                rightSoulder_IsActive = false;
            }
            else if(leftSoulder_IsActive) 
            {
                print("Enabling Left Soulder Camera...");
                Enable_RightSoulder_Camera();
                leftSoulder_IsActive = false;
            }
            else
            {
                Debug.LogError("Ningún bool en true");
            }
        }
    }

    public void Enable_LeftSoulder_Camera()
    {
        leftSoulder_Camera.enabled = true;
        rightSoulder_Camera.enabled = false;
        leftSoulder_IsActive = true ;
    }
    public void Enable_RightSoulder_Camera()
    {
        rightSoulder_Camera.enabled = true;
        leftSoulder_Camera.enabled = true;
        rightSoulder_IsActive = true ;
    }

    public void InitializeCamera()
    {
        if (rightSoulder_IsActive && rightSoulder_Camera != null)
        {
            Enable_RightSoulder_Camera();
        }
        else if (leftSoulder_IsActive && leftSoulder_Camera != null)
        {
            Enable_LeftSoulder_Camera();
        }
        else
        {
            Debug.LogError("Ninguna cámara añadida al inspector");
        }
    }
}
