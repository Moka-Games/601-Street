using UnityEngine;
using Cinemachine;

public class Camera_Script : MonoBehaviour
{
    public CinemachineFreeLook freeLookCamera;

    private bool isCameraFrozen = false;
    private Vector3 frozenPosition;
    private Quaternion frozenRotation;

    void Update()
    {
        //Testing Inputs
        if (Input.GetKeyDown(KeyCode.F)) 
        {
            FreezeCamera();
        }
        if (Input.GetKeyDown(KeyCode.U)) 
        {
            UnfreezeCamera();
        }
    }

    public void FreezeCamera()
    {
        if (freeLookCamera != null)
        {
            isCameraFrozen = true;
            frozenPosition = freeLookCamera.transform.position;
            frozenRotation = freeLookCamera.transform.rotation;
            freeLookCamera.enabled = false; 
        }
    }

    public void UnfreezeCamera()
    {
        if (freeLookCamera != null && isCameraFrozen)
        {
            freeLookCamera.enabled = true; 
            StartCoroutine(SmoothTransitionToFrozenPoint());
        }
    }

    private System.Collections.IEnumerator SmoothTransitionToFrozenPoint()
    {
        float transitionDuration = 1.5f;
        float elapsedTime = 0f;

        Vector3 startPosition = freeLookCamera.transform.position;
        Quaternion startRotation = freeLookCamera.transform.rotation;

        while (elapsedTime < transitionDuration)
        {
            freeLookCamera.transform.position = Vector3.Lerp(startPosition, frozenPosition, elapsedTime / transitionDuration);
            freeLookCamera.transform.rotation = Quaternion.Lerp(startRotation, frozenRotation, elapsedTime / transitionDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        freeLookCamera.transform.position = frozenPosition;
        freeLookCamera.transform.rotation = frozenRotation;

        isCameraFrozen = false;
    }
}
