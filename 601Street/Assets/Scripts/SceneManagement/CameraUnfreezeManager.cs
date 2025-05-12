using UnityEngine;
using System.Collections;

public class CameraUnfreezeManager : MonoBehaviour
{
    public static CameraUnfreezeManager Instance { get; private set; }

    [Tooltip("Intervalo de verificación en segundos")]
    [SerializeField] private float checkInterval = 2f;

    [Tooltip("Cantidad máxima de tiempo que la cámara puede estar congelada")]
    [SerializeField] private float maxFreezeTime = 5f;

    private float lastUnfreezeTime = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        lastUnfreezeTime = Time.time;
        StartCoroutine(CameraCheckRoutine());
    }

    public void RegisterCameraUnfreeze()
    {
        lastUnfreezeTime = Time.time;
    }

    private IEnumerator CameraCheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            // Si ha pasado demasiado tiempo desde el último desbloqueo conocido
            if (Time.time - lastUnfreezeTime > maxFreezeTime)
            {
                UnfreezeAllCameras();
            }
        }
    }

    public void UnfreezeAllCameras()
    {
        Camera_Script[] cameras = FindObjectsByType<Camera_Script>(FindObjectsSortMode.None);

        foreach (Camera_Script camera in cameras)
        {
            if (camera != null)
            {
                Debug.Log("CameraUnfreezeManager: Desbloqueando cámara preventivamente");
                camera.UnfreezeCamera();
            }
        }

        lastUnfreezeTime = Time.time;
    }

    // Método para llamar desde eventos de escena
    public void OnSceneTransitionComplete()
    {
        UnfreezeAllCameras();
    }
}