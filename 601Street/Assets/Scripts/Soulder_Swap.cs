using Cinemachine;
using UnityEngine;

public class Shoulder_Swap : MonoBehaviour
{
    public CinemachineFreeLook rightShoulder_Camera;
    public CinemachineFreeLook leftShoulder_Camera;

    private bool isRightShoulderActive = true;

    private void Start()
    {
        InitializeCamera();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwapShoulders();
        }

        // Sincronizar la rotación de las cámaras
        SyncCameraRotation();
    }

    private void SwapShoulders()
    {
        isRightShoulderActive = !isRightShoulderActive;

        if (isRightShoulderActive)
        {
            print("Enabling Right Shoulder Camera...");
            Enable_RightShoulder_Camera();
        }
        else
        {
            print("Enabling Left Shoulder Camera...");
            Enable_LeftShoulder_Camera();
        }
    }

    private void Enable_LeftShoulder_Camera()
    {
        AdjustTrackedObjectOffset(leftShoulder_Camera, -0.75f);
        AdjustTrackedObjectOffset(rightShoulder_Camera, -0.75f);
        SetCameraPriority(11, leftShoulder_Camera);
        SetCameraPriority(10, rightShoulder_Camera);
    }

    private void Enable_RightShoulder_Camera()
    {
        AdjustTrackedObjectOffset(leftShoulder_Camera, 0.75f);
        AdjustTrackedObjectOffset(rightShoulder_Camera, 0.75f);
        SetCameraPriority(11, rightShoulder_Camera);
        SetCameraPriority(10, leftShoulder_Camera);
    }

    private void InitializeCamera()
    {
        if (rightShoulder_Camera == null || leftShoulder_Camera == null)
        {
            Debug.LogError("Ninguna cámara añadida al inspector");
            return;
        }

        if (isRightShoulderActive)
        {
            Enable_RightShoulder_Camera();
        }
        else
        {
            Enable_LeftShoulder_Camera();
        }
    }

    private void SyncCameraRotation()
    {
        if (isRightShoulderActive)
        {
            CopyCameraRotation(rightShoulder_Camera, leftShoulder_Camera);
        }
        else
        {
            CopyCameraRotation(leftShoulder_Camera, rightShoulder_Camera);
        }
    }

    private void CopyCameraRotation(CinemachineFreeLook source, CinemachineFreeLook target)
    {
        target.m_XAxis.Value = source.m_XAxis.Value;
        target.m_YAxis.Value = source.m_YAxis.Value;
    }

    private void AdjustTrackedObjectOffset(CinemachineFreeLook camera, float offsetX)
    {
        if (camera != null)
        {
            // Ajustar el offset para todos los rigs
            for (int i = 0; i < 3; i++)
            {
                var composer = camera.GetRig(i).GetCinemachineComponent<CinemachineComposer>();
                if (composer != null)
                {
                    composer.m_TrackedObjectOffset.x = offsetX;
                }
            }
        }
    }

    private void SetCameraPriority(int priority, CinemachineFreeLook camera)
    {
        camera.Priority = priority;
    }
}