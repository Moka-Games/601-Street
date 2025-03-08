using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Cinemachine; // Añadimos el namespace de Cinemachine
using Cinemachine.Utility; // Para acceso a utilidades adicionales de Cinemachine

public class LockPick : MonoBehaviour
{
    // Cámara principal (la que usa el Brain de Cinemachine)
    [Tooltip("Si no se asigna, se buscará automáticamente una cámara con el tag MainCamera")]
    public Camera mainCamera;

    // Cámara virtual para el lockpick
    [Tooltip("Si no se asigna, se buscará o creará automáticamente")]
    public CinemachineVirtualCamera lockpickVCam;

    // Prioridad que tendrá la cámara al activarse
    public int lockpickCameraPriority = 15;

    // Prioridad original para restaurar al salir
    private int originalPriority;

    // Offset para posicionar la cámara virtual respecto al lockpick
    public Vector3 cameraPositionOffset = new Vector3(0, 0.3f, -0.5f);

    // Offset para el punto de mira de la cámara
    public Vector3 cameraLookAtOffset = Vector3.zero;

    // Referencia a un transform que mantendrá la posición constante de la ganzúa
    private Transform lockpickAnchor;

    // Rotación inicial del objeto
    private Quaternion initialRotation;

    // Ángulo de la ganzúa cuando se presiona E
    private float lockedPickAngle;

    // Posición inicial del objeto relativa a la cámara
    private Vector3 initialPositionFromCamera;

    public Transform innerLock;
    public Transform pickPosition;
    public float maxAngle = 90;
    public float lockSpeed = 10;
    [Range(1, 25)]
    public float lockRange = 10;
    // Representa la dificultad de la cerradura
    // O lo que es lo mismo
    // El rango de tolerancia para el desbloqueo
    public TMP_Text difficultyText;
    private float eulerAngle;
    private float unlockAngle;
    private Vector2 unlockRange;
    private float keyPressTime = 0;
    private bool movePick = true;
    private bool isLockpickModeActive = false;

    public UnityEvent OnUnlocked;
    public UnityEvent OnLockpickModeEntered;
    public UnityEvent OnLockpickModeExited;

    void Awake()
    {
        // Buscar la cámara principal si no está asignada
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // Como alternativa, buscamos por tag
                GameObject mainCameraObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (mainCameraObj != null)
                {
                    mainCamera = mainCameraObj.GetComponent<Camera>();
                }

                if (mainCamera == null)
                {
                    Debug.LogError("No se encontró la cámara principal. Asegúrate de que exista una cámara con tag MainCamera.");
                }
            }
        }

        // Verificar que la cámara principal tenga CinemachineBrain
        if (mainCamera != null && mainCamera.GetComponent<CinemachineBrain>() == null)
        {
            Debug.LogWarning("La cámara principal no tiene un CinemachineBrain. Añadiendo uno automáticamente.");
            mainCamera.gameObject.AddComponent<CinemachineBrain>();
        }

        // Configurar o crear la cámara virtual
        SetupVirtualCamera();
    }

    void Start()
    {
        // Guardamos la prioridad original
        if (lockpickVCam != null)
        {
            originalPriority = lockpickVCam.Priority;
        }

        newLock();
    }

    private void SetupVirtualCamera()
    {
        // Si no hay cámara virtual asignada, intentamos crearla
        if (lockpickVCam == null)
        {
            // Primero buscamos si ya existe alguna cámara virtual con el nombre apropiado
            GameObject vcamObj = GameObject.Find("CM_LockpickCamera");

            if (vcamObj == null)
            {
                // Crear una nueva cámara virtual
                vcamObj = new GameObject("CM_LockpickCamera");
                lockpickVCam = vcamObj.AddComponent<CinemachineVirtualCamera>();

                // Configurar la cámara virtual
                lockpickVCam.Priority = 5; // Prioridad baja por defecto

                // Configuración del Body
                CinemachineTransposer transposer = lockpickVCam.AddCinemachineComponent<CinemachineTransposer>();
                transposer.m_FollowOffset = cameraPositionOffset;

                // Configuración del Aim
                CinemachineComposer composer = lockpickVCam.AddCinemachineComponent<CinemachineComposer>();
                composer.m_TrackedObjectOffset = cameraLookAtOffset;

                // Configurar el seguimiento
                lockpickVCam.Follow = pickPosition != null ? pickPosition : transform;
                lockpickVCam.LookAt = pickPosition != null ? pickPosition : transform;

                Debug.Log("Cámara virtual de lockpick creada automáticamente.");
            }
            else
            {
                // Usar la cámara existente
                lockpickVCam = vcamObj.GetComponent<CinemachineVirtualCamera>();
                if (lockpickVCam == null)
                {
                    Debug.LogError("Se encontró un objeto con nombre CM_LockpickCamera pero no contiene el componente CinemachineVirtualCamera.");
                }
            }
        }

        // Guardar la prioridad original
        if (lockpickVCam != null)
        {
            originalPriority = lockpickVCam.Priority;
        }
    }

    // Método para activar el modo lockpick
    public void EnterLockpickMode()
    {
        if (!isLockpickModeActive && lockpickVCam != null)
        {
            // Creamos un objeto vacío para mantener una posición fija en el espacio
            CreateLockpickAnchor();

            // Aumentamos la prioridad para que esta cámara sea la activa
            lockpickVCam.Priority = lockpickCameraPriority;
            isLockpickModeActive = true;

            // Guardamos la rotación inicial
            initialRotation = transform.rotation;

            // Invocamos el evento si hay listeners
            OnLockpickModeEntered?.Invoke();

            // También podemos activar el cursor si estaba oculto
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    // Método para desactivar el modo lockpick
    public void ExitLockpickMode()
    {
        if (isLockpickModeActive && lockpickVCam != null)
        {
            // Restauramos la prioridad original
            lockpickVCam.Priority = originalPriority;
            isLockpickModeActive = false;

            // Eliminamos el anchor
            if (lockpickAnchor != null)
            {
                Destroy(lockpickAnchor.gameObject);
            }

            // Invocamos el evento si hay listeners
            OnLockpickModeExited?.Invoke();

            // Podemos volver a ocultar/bloquear el cursor si lo necesita el juego
            // Estos valores dependerán de tu sistema de control
            // Cursor.visible = false;
            // Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // Método para crear un ancla de posición fija para la ganzúa
    private void CreateLockpickAnchor()
    {
        // Creamos un GameObject vacío para servir como ancla
        GameObject anchorObj = new GameObject("LockpickAnchor");
        lockpickAnchor = anchorObj.transform;

        // Configuramos su posición para que coincida con la posición del pickPosition
        if (pickPosition != null)
        {
            lockpickAnchor.position = pickPosition.position;
        }
        else
        {
            lockpickAnchor.position = transform.position;
            Debug.LogWarning("pickPosition no está asignado. Usando la posición del objeto principal.");
        }

        lockpickAnchor.rotation = transform.rotation;

        // Guardamos la posición relativa a la cámara
        initialPositionFromCamera = lockpickAnchor.position - mainCamera.transform.position;
    }

    void Update()
    {
        // Solo procesamos la lógica si está activo el modo lockpick
        if (!isLockpickModeActive)
            return;

        // Forzar que la ganzúa siga al pickPosition
        if (pickPosition != null)
        {
            transform.position = pickPosition.position;
        }

        // Procesamos la acción de la tecla E
        if (Input.GetKeyDown(KeyCode.E))
        {
            movePick = false;
            keyPressTime = 1;
            // Guardamos el ángulo actual de la ganzúa al presionar E
            lockedPickAngle = eulerAngle;
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            movePick = true;
            keyPressTime = 0;
            // Restauramos la rotación inicial de la ganzúa
            transform.rotation = initialRotation * Quaternion.Euler(0, 0, eulerAngle);
            // Restauramos la rotación del innerLock a 0
            innerLock.eulerAngles = Vector3.zero;
        }

        // Lógica para salir del modo lockpick con Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitLockpickMode();
            return;
        }

        if (movePick)
        {
            // Cuando movePick es true, controlamos la ganzúa con el ratón
            // Calculamos el centro de la pantalla
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            // Calculamos la dirección desde el centro de la pantalla hacia la posición del mouse
            Vector2 mouseDirection = Input.mousePosition - new Vector3(screenCenter.x, screenCenter.y, 0);

            // Normalizamos la dirección para que solo nos interese la orientación
            mouseDirection.Normalize();

            // Calculamos el ángulo entre la dirección del mouse y el vector "arriba"
            eulerAngle = Vector2.SignedAngle(Vector2.up, mouseDirection);

            // Limitamos el ángulo al rango permitido
            eulerAngle = Mathf.Clamp(eulerAngle, -maxAngle, maxAngle);

            // Aplicamos la rotación respetando la rotación inicial del objeto
            transform.rotation = initialRotation * Quaternion.Euler(0, 0, eulerAngle);
        }
        else
        {
            // Si no estamos moviendo la ganzúa (pulsamos E), calculamos las rotaciones
            float percentage = Mathf.Round(100 - Mathf.Abs(((lockedPickAngle - unlockAngle) / 100) * 100));
            float lockRotation = ((percentage / 100) * maxAngle) * keyPressTime;
            float maxRotation = (percentage / 100) * maxAngle;

            // Aplicamos la rotación al innerLock
            float lockLerp = Mathf.LerpAngle(innerLock.eulerAngles.z, lockRotation, Time.deltaTime * lockSpeed);
            innerLock.eulerAngles = new Vector3(0, 0, lockLerp);

            // Hacemos que la ganzúa siga el movimiento del innerLock (como si estuvieran acoplados)
            transform.rotation = initialRotation * Quaternion.Euler(0, 0, lockedPickAngle + lockLerp);

            if (lockLerp >= maxRotation - 1)
            {
                if (lockedPickAngle < unlockRange.y && lockedPickAngle > unlockRange.x)
                {
                    Debug.Log("Unlocked!");
                    OnUnlocked.Invoke();

                    // Al desbloquear, salimos del modo lockpick
                    ExitLockpickMode();

                    movePick = true;
                    keyPressTime = 0;
                }
                else
                {
                    float randomRotation = Random.insideUnitCircle.x;
                    transform.eulerAngles += new Vector3(0, 0, Random.Range(-randomRotation, randomRotation));
                }
            }
        }
    }

    void newLock()
    {
        unlockAngle = Random.Range(-maxAngle + lockRange, maxAngle - lockRange);
        unlockRange = new Vector2(unlockAngle - lockRange, unlockAngle + lockRange);
        UpdateDifficultyText();
    }

    void UpdateDifficultyText()
    {
        if (lockRange <= 10)
        {
            difficultyText.text = "Dificil";
        }
        else if (lockRange > 10 && lockRange <= 15)
        {
            difficultyText.text = "Medio";
        }
        else if (lockRange > 15 && lockRange <= 25)
        {
            difficultyText.text = "Fácil";
        }
    }
}