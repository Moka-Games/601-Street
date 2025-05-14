using UnityEngine;
using Cinemachine;
using System.Collections;

public class Camera_Script : MonoBehaviour
{
    public CinemachineFreeLook freeLookCamera;
    public Transform playerLookAtTarget; //Look At del jugador (Debe ser accesible para utilizarlo en el DialogueManager)

    private bool isCameraFrozen = false;
    private Vector3 frozenPosition;
    private Quaternion frozenRotation;

    private Transform currentLookAtTarget; 
    private Coroutine transitionCoroutine;
    private float lastTransitionTime = 0f; 
    private float lastFreezeTime = 0f;
    private bool wasRecentlyFrozen = false;

    void Awake()
    {
        if (freeLookCamera != null)
        {
            playerLookAtTarget = freeLookCamera.LookAt;
        }
        else
        {
            Debug.LogWarning("CinemachineFreeLook no está asignado en Camera_Script.");
        }
    }

    void Update()
    {
        // Auto-desbloqueo si la cámara ha estado congelada por más de 3 segundos
        if (wasRecentlyFrozen && Time.time - lastFreezeTime > 2f)
        {
            if (freeLookCamera != null && !freeLookCamera.enabled)
            {
                Debug.Log("Auto-desbloqueo de cámara después de 3 segundos");
                UnfreezeCamera();
            }
            wasRecentlyFrozen = false;
        }
    }
    public void FreezeCamera()
    {
        if (freeLookCamera != null)
        {
            isCameraFrozen = true;
            lastFreezeTime = Time.time;
            wasRecentlyFrozen = true;
            frozenPosition = freeLookCamera.transform.position;
            frozenRotation = freeLookCamera.transform.rotation;
            freeLookCamera.enabled = false;
        }
    }

    public void UnfreezeCamera()
    {
        // Verificar si podemos descongelar la cámara
        if (freeLookCamera != null)
        {
            // Intentar habilitar la cámara independientemente del estado anterior
            freeLookCamera.enabled = true;

            // Solo iniciar transición suave si estaba congelada
            if (isCameraFrozen)
            {
                StartCoroutine(SmoothTransitionToFrozenPoint());
            }

            // Notificar al sistema de seguridad
            CameraUnfreezeManager unfreezeManager = FindFirstObjectByType<CameraUnfreezeManager>();
            if (unfreezeManager != null)
            {
                unfreezeManager.RegisterCameraUnfreeze();
            }

            Debug.Log("Cámara desbloqueada exitosamente");
            isCameraFrozen = false;
        }
        else
        {
            Debug.LogWarning("Intento de desbloquear cámara con freeLookCamera nulo");
        }
    }
    private IEnumerator SmoothTransitionToFrozenPoint()
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

    public void ChangeLookAtTarget(Transform newTarget, float transitionDuration = 1.0f)
    {
        print("Realizando cambio de LookAt");
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine); // Detener la transición actual si hay una en curso
        }
        transitionCoroutine = StartCoroutine(ChangeLookAtTargetCoroutine(newTarget, transitionDuration));
    }

    private IEnumerator ChangeLookAtTargetCoroutine(Transform newTarget, float transitionDuration)
    {
        if (freeLookCamera == null || newTarget == null)
        {
            yield break;
        }

        // Crear un objeto temporal para la transición
        GameObject tempTarget = new GameObject("TempLookAtTarget");
        tempTarget.transform.position = freeLookCamera.LookAt.position; // Posición inicial
        freeLookCamera.LookAt = tempTarget.transform;

        float elapsedTime = 0f;
        Vector3 initialPosition = tempTarget.transform.position;

        while (elapsedTime < transitionDuration)
        {
            if (newTarget == null)
            {
                Destroy(tempTarget); // Limpiar el objeto temporal
                yield break;
            }

            // Interpolar suavemente entre la posición inicial y la del nuevo objetivo
            tempTarget.transform.position = Vector3.Lerp(
                initialPosition,
                newTarget.position,
                elapsedTime / transitionDuration
            );

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Asignar el nuevo objetivo final
        freeLookCamera.LookAt = newTarget;

        // Destruir el objeto temporal
        Destroy(tempTarget);
    }
    public void RegisterTransition()
    {
        lastTransitionTime = Time.time;
    }
}