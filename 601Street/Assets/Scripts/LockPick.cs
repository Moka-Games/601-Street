using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Cinemachine;
using Cinemachine.Utility;

public class LockPick : MonoBehaviour
{
    // Referencias de cámaras
    [Header("Referencias de Cámara")]
    [Tooltip("Si no se asigna, se buscará automáticamente una cámara con el tag MainCamera")]
    public Camera mainCamera;

    [Tooltip("Si no se asigna, se buscará o creará automáticamente")]
    public CinemachineVirtualCamera lockpickVCam;

    [Tooltip("Prioridad que tendrá la cámara al activarse")]
    public int lockpickCameraPriority = 15;

    // Prioridad original para restaurar al salir
    private int originalPriority;

    // Offset para posicionar la cámara virtual respecto al lockpick
    public Vector3 cameraPositionOffset = new Vector3(0, 0.3f, -0.5f);

    // Offset para el punto de mira de la cámara
    public Vector3 cameraLookAtOffset = Vector3.zero;

    // Referencia a un objeto opcional que servirá como punto de mira
    [Tooltip("Si se asigna, la cámara mirará a este objeto en lugar de al transform principal")]
    public Transform lookAtTarget;

    [Header("Referencias de Lockpick")]
    public Transform innerLock;
    public Transform pickPosition;

    [Header("Configuración de Dificultad")]
    public float maxAngle = 90;
    public float lockSpeed = 10;
    [Range(1, 25)]
    public float lockRange = 10;

    // Representa la dificultad de la cerradura
    public TMP_Text difficultyText;

    [Header("Referencias del Sistema")]
    public PlayerInteraction playerInteraction;

    [Header("Eventos")]
    public UnityEvent OnUnlocked;
    public UnityEvent OnLockpickModeEntered;
    public UnityEvent OnLockpickModeExited;

    // Variables privadas
    private float eulerAngle;
    private float unlockAngle;
    private Vector2 unlockRange;
    private float keyPressTime = 0;
    private bool movePick = true;
    private bool isLockpickModeActive = false;

    // Referencia a un transform que mantendrá la posición constante de la ganzúa
    private Transform lockpickAnchor;

    // Rotación inicial del objeto
    private Quaternion initialPickRotation;

    // Rotación inicial del innerLock
    private Quaternion initialInnerLockRotation;

    // Ángulo de la ganzúa cuando se presiona E
    private float lockedPickAngle;

    // Valor de rotación actual del innerLock (relativo a su posición inicial)
    private float currentInnerLockRotation = 0f;

    // Variables para la transición suave
    private float returnLerpSpeed = 5f;
    private bool isReturning = false;
    private float returnTime = 0f;
    private Quaternion targetPickRotation;
    private Quaternion fromPickRotation;
    private float targetInnerLockAngle;
    private float fromInnerLockAngle;

    // Referencia al estado de interacción anterior
    private bool previousInteractionState = false;

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

        // Buscar el PlayerInteraction si no está asignado
        if (playerInteraction == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerInteraction = player.GetComponent<PlayerInteraction>();
            }

            if (playerInteraction == null)
            {
                Debug.LogWarning("No se encontró PlayerInteraction. Algunas funcionalidades pueden no estar disponibles.");
            }
        }
    }

    void Start()
    {
        // Guardamos la prioridad original
        if (lockpickVCam != null)
        {
            originalPriority = lockpickVCam.Priority;
        }

        // Guardamos la rotación inicial del innerLock
        if (innerLock != null)
        {
            initialInnerLockRotation = innerLock.rotation;
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
                lockpickVCam.Follow = transform;
                lockpickVCam.LookAt = lookAtTarget != null ? lookAtTarget : transform;

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
            // Guardar el estado de interacción actual
            if (playerInteraction != null)
            {
                previousInteractionState = playerInteraction.enabled;
                playerInteraction.enabled = false; // Desactivar temporalmente la interacción del jugador
            }

            // Crear un objeto vacío para mantener una posición fija en el espacio
            CreateLockpickAnchor();

            // Aumentamos la prioridad para que esta cámara sea la activa
            lockpickVCam.Priority = lockpickCameraPriority;
            isLockpickModeActive = true;

            // Guardamos las rotaciones iniciales
            initialPickRotation = transform.rotation;

            // Aseguramos que hayamos guardado la rotación inicial del innerLock
            if (initialInnerLockRotation == Quaternion.identity && innerLock != null)
            {
                initialInnerLockRotation = innerLock.rotation;
            }

            // Reiniciamos el valor de rotación actual
            currentInnerLockRotation = 0f;

            // Invocamos el evento si hay listeners
            OnLockpickModeEntered?.Invoke();

            // También podemos activar el cursor si estaba oculto
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Notificar a cualquier sistema que necesite saber sobre el cambio de cámara
            // Esto es importante para los sistemas de UI e interacción
            BroadcastMessage("OnCameraChanged", lockpickVCam, SendMessageOptions.DontRequireReceiver);

            // Pausar el movimiento del jugador si es posible
            DisablePlayerMovement();

            Debug.Log("Modo lockpick activado");
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

            // Restaurar el estado de interacción anterior
            if (playerInteraction != null)
            {
                // Damos un pequeño delay para asegurar que la transición de cámara se completa
                StartCoroutine(RestoreInteractionAfterDelay(0.2f));
            }

            // Notificar a cualquier sistema que necesite saber sobre el cambio de cámara
            BroadcastMessage("OnCameraChanged", mainCamera, SendMessageOptions.DontRequireReceiver);

            // Restaurar el movimiento del jugador
            EnablePlayerMovement();

            Debug.Log("Modo lockpick desactivado");
        }
    }

    private IEnumerator RestoreInteractionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (playerInteraction != null)
        {
            playerInteraction.enabled = previousInteractionState;

            // Forzar una actualización del estado de interacción
            if (playerInteraction.enabled)
            {
                playerInteraction.ForceUpdateInteraction();
            }
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
    }

    // Aplica una rotación al innerLock relativa a su posición inicial
    private void SetInnerLockRotation(float angle)
    {
        innerLock.rotation = initialInnerLockRotation * Quaternion.Euler(0, 0, angle);
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

        // Procesamos la transición suave de retorno
        if (isReturning)
        {
            returnTime += Time.deltaTime * returnLerpSpeed;
            float t = Mathf.Clamp01(returnTime);

            // Aplicar interpolación suave a la ganzúa
            transform.rotation = Quaternion.Slerp(fromPickRotation, targetPickRotation, t);

            // Aplicar interpolación suave al ángulo del innerLock
            float newAngle = Mathf.Lerp(fromInnerLockAngle, targetInnerLockAngle, t);
            SetInnerLockRotation(newAngle);

            // Cuando la transición está completa
            if (t >= 0.99f)
            {
                isReturning = false;
                movePick = true;
                transform.rotation = targetPickRotation;
                SetInnerLockRotation(targetInnerLockAngle);
                currentInnerLockRotation = targetInnerLockAngle;
            }

            // Durante la transición no permitimos mover la ganzúa
            return;
        }

        // Procesamos la acción de la tecla E
        if (Input.GetKeyDown(KeyCode.E))
        {
            movePick = false;
            keyPressTime = 1;
            // Guardamos el ángulo actual de la ganzúa al presionar E
            lockedPickAngle = eulerAngle;
            // Reiniciamos el valor de rotación actual del innerLock
            currentInnerLockRotation = 0f;
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            // En lugar de restaurar inmediatamente, iniciar la transición suave
            isReturning = true;
            returnTime = 0f;

            // Guardar las rotaciones actuales como punto de partida
            fromPickRotation = transform.rotation;
            fromInnerLockAngle = currentInnerLockRotation;

            // Calcular las rotaciones objetivo
            targetPickRotation = initialPickRotation * Quaternion.Euler(0, 0, eulerAngle);
            targetInnerLockAngle = 0f;

            // No restauramos inmediatamente
            keyPressTime = 0;
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
            transform.rotation = initialPickRotation * Quaternion.Euler(0, 0, eulerAngle);
        }
        else
        {
            // Si no estamos moviendo la ganzúa (pulsamos E), calculamos las rotaciones

            // Calculamos la distancia angular más corta entre el ángulo actual y el ángulo de desbloqueo
            float angularDistance = Mathf.Abs(Mathf.DeltaAngle(lockedPickAngle, unlockAngle));

            // Convertimos a un porcentaje inversamente proporcional (0% lejos, 100% cerca)
            // La distancia máxima posible es maxAngle*2 (de -maxAngle a +maxAngle)
            float percentage = 100 * (1 - (angularDistance / (maxAngle * 2)));

            // Limitamos el porcentaje para que no sea negativo (por si acaso)
            percentage = Mathf.Max(0, percentage);

            // Calculamos la rotación máxima basada en el porcentaje
            float lockRotation = ((percentage / 100) * maxAngle) * keyPressTime;
            float maxRotation = (percentage / 100) * maxAngle;

            // Calculamos la nueva rotación del innerLock basada en el tiempo
            currentInnerLockRotation = Mathf.Lerp(currentInnerLockRotation, lockRotation, Time.deltaTime * lockSpeed);

            // Aplicamos la rotación al innerLock
            SetInnerLockRotation(currentInnerLockRotation);

            // Hacemos que la ganzúa siga el movimiento del innerLock (como si estuvieran acoplados)
            transform.rotation = initialPickRotation * Quaternion.Euler(0, 0, lockedPickAngle + currentInnerLockRotation);

            if (currentInnerLockRotation >= maxRotation - 1)
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
        if (difficultyText == null)
            return;

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

    // Métodos para gestionar el movimiento del jugador
    void DisablePlayerMovement()
    {
        // Buscar el PlayerController y deshabilitarlo si es posible
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
        }
    }

    void EnablePlayerMovement()
    {
        // Restaurar el PlayerController
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
        }
    }

    // Método para asegurarnos de que se limpien todas las referencias si se destruye este objeto
    private void OnDestroy()
    {
        if (isLockpickModeActive)
        {
            // Asegurarnos de que restauramos todo si el objeto es destruido mientras está en modo lockpick
            ExitLockpickMode();
        }

        if (lockpickAnchor != null)
        {
            Destroy(lockpickAnchor.gameObject);
        }
    }
}