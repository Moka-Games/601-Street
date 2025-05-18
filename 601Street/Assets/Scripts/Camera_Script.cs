using UnityEngine;
using Cinemachine;
using System.Collections;

public class Camera_Script : MonoBehaviour
{
    public CinemachineFreeLook freeLookCamera;
    public Transform playerLookAtTarget; //Look At del jugador (Debe ser accesible para utilizarlo en el DialogueManager)

    [Header("Configuraci�n de Transici�n")]
    [Tooltip("Duraci�n de la transici�n suave al descongelar la c�mara (segundos)")]
    public float transitionDuration = 1.5f;

    [Tooltip("Curva de suavizado para la transici�n (1=lineal, >1=acelerado, <1=desacelerado)")]
    public float transitionSmoothness = 0.5f;

    [Tooltip("Velocidad de actualizaci�n de los valores de la c�mara durante la transici�n")]
    public float cameraUpdateRate = 5f;

    private bool isCameraFrozen = false;
    private Vector3 frozenPosition;
    private Quaternion frozenRotation;

    private Transform currentLookAtTarget;
    private Coroutine transitionCoroutine;
    private float lastTransitionTime = 0f;
    private float lastFreezeTime = 0f;
    private bool wasRecentlyFrozen = false;

    // Valores guardados para la transici�n suave
    private float initialXAxisValue;
    private float initialYAxisValue;
    private bool isTransitioning = false;

    void Awake()
    {
        if (freeLookCamera != null)
        {
            playerLookAtTarget = freeLookCamera.LookAt;
        }
        else
        {
            Debug.LogWarning("CinemachineFreeLook no est� asignado en Camera_Script.");
        }
    }

    void Update()
    {
        // Auto-desbloqueo si la c�mara ha estado congelada por m�s de 2 segundos
        if (wasRecentlyFrozen && Time.time - lastFreezeTime > 2f)
        {
            if (freeLookCamera != null && !freeLookCamera.enabled)
            {
                Debug.Log("Auto-desbloqueo de c�mara despu�s de 2 segundos");
                UnfreezeCamera();
            }
            wasRecentlyFrozen = false;
        }

        // Actualizaci�n para la transici�n suave
        if (isTransitioning && freeLookCamera != null && freeLookCamera.enabled)
        {
            // Si el mouse se mueve durante la transici�n, ajustar la velocidad
            // para que la transici�n sea m�s suave
            Camera_ControlInputSystem();
        }
    }

    private void Camera_ControlInputSystem()
    {
        // Este m�todo se encarga de controlar el input durante la transici�n
        // para evitar movimientos bruscos al descongelar la c�mara

        // Detectar si hay input del mouse para ajustar la velocidad
        bool hasMouseInput = Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0;

        if (hasMouseInput)
        {
            // Si hay input del mouse, permitir que la c�mara lo utilice
            // pero con una velocidad controlada
            float smoothFactor = Time.deltaTime * cameraUpdateRate;

            // Permitir que el input gradualmente afecte a la c�mara
            // De esta manera, la influencia del input crece con el tiempo
            smoothFactor *= Mathf.Min(1.0f, (Time.time - lastFreezeTime) / transitionDuration);

            // No es necesario aplicar ning�n ajuste adicional al input,
            // ya que la Cinemachine maneja el input por s� misma
        }
    }

    public void FreezeCamera()
    {
        // Detener cualquier transici�n en curso
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        isTransitioning = false;

        if (freeLookCamera != null)
        {
            // Guardar los valores actuales de los ejes antes de congelar
            initialXAxisValue = freeLookCamera.m_XAxis.Value;
            initialYAxisValue = freeLookCamera.m_YAxis.Value;

            isCameraFrozen = true;
            lastFreezeTime = Time.time;
            wasRecentlyFrozen = true;

            // Guardar la posici�n y rotaci�n actuales antes de congelar
            frozenPosition = freeLookCamera.transform.position;
            frozenRotation = freeLookCamera.transform.rotation;

            // Desactivar la c�mara FreeLook
            freeLookCamera.enabled = false;

            Debug.Log("C�mara congelada en posici�n: " + frozenPosition);
        }
    }

    public void UnfreezeCamera()
    {
        // Si no hay c�mara o ya est� en proceso de transici�n, salir
        if (freeLookCamera == null || isTransitioning)
            return;

        // Detener cualquier transici�n anterior si existe
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }

        // Iniciar la transici�n suave
        if (isCameraFrozen)
        {
            transitionCoroutine = StartCoroutine(SmoothUnfreezeCamera());
        }
        else
        {
            // Si no estaba congelada, simplemente activarla
            freeLookCamera.enabled = true;
            Debug.Log("C�mara desbloqueada (no estaba congelada)");
        }
    }

    private IEnumerator SmoothUnfreezeCamera()
    {
        Debug.Log("Iniciando transici�n suave de descongelado de c�mara");

        isTransitioning = true;

        // Paso 1: Habilitar la c�mara pero mantener sus valores iniciales
        // Esto evita el "tir�n" inicial
        freeLookCamera.m_XAxis.Value = initialXAxisValue;
        freeLookCamera.m_YAxis.Value = initialYAxisValue;

        // Activar la c�mara
        freeLookCamera.enabled = true;

        // Esperar un frame para que la c�mara se actualice
        yield return null;

        // Paso 2: Realizar una transici�n suave permitiendo que el input
        // afecte gradualmente a la c�mara (gestionado en Update)
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Paso 3: Finalizar la transici�n
        isTransitioning = false;
        isCameraFrozen = false;
        transitionCoroutine = null;

        Debug.Log("Transici�n de descongelado completada");

        // Notificar al sistema de seguridad si existe
        CameraUnfreezeManager unfreezeManager = FindFirstObjectByType<CameraUnfreezeManager>();
        if (unfreezeManager != null)
        {
            unfreezeManager.RegisterCameraUnfreeze();
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
            StopCoroutine(transitionCoroutine); // Detener la transici�n actual si hay una en curso
        }
        transitionCoroutine = StartCoroutine(ChangeLookAtTargetCoroutine(newTarget, transitionDuration));
    }

    private IEnumerator ChangeLookAtTargetCoroutine(Transform newTarget, float transitionDuration)
    {
        if (freeLookCamera == null || newTarget == null)
        {
            yield break;
        }

        // Crear un objeto temporal para la transici�n
        GameObject tempTarget = new GameObject("TempLookAtTarget");
        tempTarget.transform.position = freeLookCamera.LookAt.position; // Posici�n inicial
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

            // Interpolar suavemente entre la posici�n inicial y la del nuevo objetivo
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