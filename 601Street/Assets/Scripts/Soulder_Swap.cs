using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shoulder_Swap : MonoBehaviour
{
    public CinemachineFreeLook rightShoulder_Camera;
    public CinemachineFreeLook leftShoulder_Camera;

    private bool rightShoulder_IsActive = false;
    private bool leftShoulder_IsActive = false;

    void Start()
    {
        rightShoulder_IsActive = true;
        InitializeCamera();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (rightShoulder_IsActive)
            {
                print("Enabling Left Shoulder Camera...");
                Enable_LeftShoulder_Camera();
                rightShoulder_IsActive = false;
            }
            else if (leftShoulder_IsActive)
            {
                print("Enabling Right Shoulder Camera...");
                Enable_RightShoulder_Camera();
                leftShoulder_IsActive = false;
            }
            else
            {
                Debug.LogError("Ningún bool en true");
            }
        }
    }

    public void Enable_LeftShoulder_Camera()
    {
        SetCameraPriority(11, leftShoulder_Camera);
        SetCameraPriority(10, rightShoulder_Camera);
        leftShoulder_IsActive = true;

        AdjustTrackedObjectOffset(leftShoulder_Camera, -0.75f);
    }

    public void Enable_RightShoulder_Camera()
    {
        SetCameraPriority(11, rightShoulder_Camera);
        SetCameraPriority(10, leftShoulder_Camera);
        rightShoulder_IsActive = true;

        AdjustTrackedObjectOffset(rightShoulder_Camera, 0.75f);
    }

    public void InitializeCamera()
    {
        if (rightShoulder_IsActive && rightShoulder_Camera != null)
        {
            Enable_RightShoulder_Camera();
        }
        else if (leftShoulder_IsActive && leftShoulder_Camera != null)
        {
            Enable_LeftShoulder_Camera();
        }
        else
        {
            Debug.LogError("Ninguna cámara añadida al inspector");
        }
    }

    public void SetCameraPriority(int priority, CinemachineFreeLook camera)
    {
        camera.Priority = priority;
    }

    private void AdjustTrackedObjectOffset(CinemachineFreeLook camera, float offsetX)
    {
        if (camera != null)
        {
            CinemachineComposer composer = camera.GetRig(1).GetCinemachineComponent<CinemachineComposer>();
            if (composer != null)
            {
                composer.m_TrackedObjectOffset.x = offsetX;
            }
        }
    }
}
