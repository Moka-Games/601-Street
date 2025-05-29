using UnityEngine;

public class CameraFinder : MonoBehaviour
{
    private Canvas canvas;
    [SerializeField] private Camera cameraToFind;

    private void Start()
    {
        canvas = GetComponent<Canvas>();

        cameraToFind = Camera.main;

        canvas.planeDistance = 0.8f;

        canvas.worldCamera = cameraToFind;
    }
}
