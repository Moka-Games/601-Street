using UnityEngine;

public class CameraFinder : MonoBehaviour
{
    private Canvas canvas;
    [SerializeField] private Camera cameraToFind;

    [SerializeField] private float planeDistance = 0.8f;

    private void Start()
    {
        canvas = GetComponent<Canvas>();

        cameraToFind = Camera.main;

        canvas.planeDistance = planeDistance;

        canvas.worldCamera = cameraToFind;
    }
}
