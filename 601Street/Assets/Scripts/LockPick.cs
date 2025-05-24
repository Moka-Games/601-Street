using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Events;
using Cinemachine;
using Cinemachine.Utility;

public class LockPick : MonoBehaviour, PlayerControls.IGameplayActions
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

    [Header("Configuración de Input")]
    [Tooltip("Sensibilidad del input del lockpick (más alto = más sensible)")]
    [SerializeField] private float lockpickInputSensitivity = 2f;
    [Tooltip("Suavizado del input del lockpick")]
    [SerializeField] private float lockpickInputSmoothing = 5f;
    [Tooltip("Zona muerta para el input del lockpick")]
    [Range(0f, 1f)]
    [SerializeField] private float lockpickDeadzone = 0.1f;
    [Tooltip("Invertir el input horizontal del lockpick")]
    [SerializeField] private bool invertLockpickInput = true;

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

    // Variables del nuevo sistema de input
    private PlayerControls playerControls;
    private Vector2 lockpickInput;
    private Vector2 smoothedLockpickInput;
    private bool tryLockPickPressed = false;
    private bool tryLockPickHeld = false;

    // Referencia a un transform que mantendrá la posición constante de la ganzúa
    private Transform lockpickAnchor;

    // Rotación inicial del objeto
    private Quaternion initialPickRotation;

    // Rotación inicial del innerLock
    private Quaternion initialInnerLockRotation;

    // Ángulo de la ganzúa cuando se presiona el botón
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

    // Cámara del jugador (FreeLookCamera)
    private CinemachineFreeLook playerCamera;

    void Awake()
    {
        // Inicializar el sistema de input
        playerControls = new PlayerControls();

        // Buscar la cámara principal si no está asignada
        FindPlayerCamera();

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

    #region Input System Callbacks

    public void OnWalking(InputAction.CallbackContext context)
    {
        // No se usa en LockPick - se maneja en PlayerController
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        // No se usa en LockPick - se maneja en PlayerController
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        // No se usa en LockPick - se maneja en PlayerInteraction
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // No se usa en LockPick - se maneja en FreeLookCameraController
    }

    public void OnAcceptCall(InputAction.CallbackContext context)
    {
        // No se usa en LockPick
    }

    public void OnToggleInventory(InputAction.CallbackContext context)
    {
        // No se usa en LockPick
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        // Lógica para salir del modo lockpick con Escape
        if (context.performed && isLockpickModeActive)
        {
            ExitLockpickMode();
        }
    }

    public void OnSkipDialogue(InputAction.CallbackContext context)
    {
        // No se usa en LockPick
    }

    public void OnLockpick(InputAction.CallbackContext context)
    {
        // Solo procesar el input si estamos en modo lockpick
        if (!isLockpickModeActive) return;

        if (context.canceled)
        {
            lockpickInput = Vector2.zero;
        }
        else
        {
            lockpickInput = context.ReadValue<Vector2>();
        }
    }

    // Método para manejar el input de girar el lockpick (si existe)
    public void OnTurn_Lockpick(InputAction.CallbackContext context)
    {
        // Usar el mismo método que OnLockpick para compatibilidad
        OnLockpick(context);
    }

    // Método para manejar el input de intentar girar la cerradura
    public void OnTry_LockPick(InputAction.CallbackContext context)
    {
        // Solo procesar si estamos en modo lockpick
        if (!isLockpickModeActive) return;

        if (context.started)
        {
            tryLockPickPressed = true;
            tryLockPickHeld = true;
        }
        else if (context.canceled)
        {
            tryLockPickHeld = false;
        }
    }

    #endregion

    private void OnEnable()
    {
        if (playerControls != null)
        {
            playerControls.Gameplay.AddCallbacks(this);
            // Solo habilitamos los controles cuando entramos en modo lockpick
        }
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.Gameplay.RemoveCallbacks(this);
            playerControls.Gameplay.Disable();
        }
    }

    private void OnDestroy()
    {
        if (isLockpickModeActive)
        {
            ExitLockpickMode();
        }

        if (lockpickAnchor != null)
        {
            Destroy(lockpickAnchor.gameObject);
        }

        if (playerControls != null)
        {
            playerControls.Dispose();
        }
    }

    // Método para buscar la cámara de tipo FreeLook
    private void FindPlayerCamera()
    {
        // Usar el nuevo método FindObjectsByType que es más eficiente
        CinemachineFreeLook[] freeLookCameras = FindObjectsByType<CinemachineFreeLook>(FindObjectsSortMode.None);

        if (freeLookCameras.Length > 0)
        {
            // Usar la primera encontrada
            playerCamera = freeLookCameras[0];
            Debug.Log("Cámara del jugador (FreeLookCamera) encontrada: " + playerCamera.name);

            if (freeLookCameras.Length > 1)
            {
                Debug.LogWarning("Se encontraron múltiples CinemachineFreeLookCamera. Usando la primera encontrada: " + playerCamera.name);
            }
        }
        else
        {
            Debug.LogWarning("No se encontró ninguna CinemachineFreeLookCamera en la escena.");
        }
    }

    void Start()
    {
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
            // Habilitar los controles de input
            if (playerControls != null)
            {
                playerControls.Gameplay.Enable();
            }

            // Guardar el estado de interacción actual
            if (playerInteraction != null)
            {
                previousInteractionState = playerInteraction.enabled;
                playerInteraction.enabled = false; // Desactivar temporalmente la interacción del jugador
            }

            // Desactivamos la cámara del jugador
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(false);
                Debug.Log("Cámara del jugador desactivada");
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

            // Resetear variables de input
            lockpickInput = Vector2.zero;
            smoothedLockpickInput = Vector2.zero;
            tryLockPickPressed = false;
            tryLockPickHeld = false;

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
            // Deshabilitar los controles de input
            if (playerControls != null)
            {
                playerControls.Gameplay.Disable();
            }

            // Restauramos la prioridad original
            lockpickVCam.Priority = originalPriority;
            isLockpickModeActive = false;

            // Reactivamos la cámara del jugador
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
                Debug.Log("Cámara del jugador reactivada");
            }

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
                // Restauramos inmediatamente el PlayerInteraction
                playerInteraction.enabled = previousInteractionState;

                // Forzar una actualización del estado de interacción
                if (playerInteraction.enabled)
                {
                    playerInteraction.ForceUpdateInteraction();
                }
            }

            // Notificar a cualquier sistema que necesite saber sobre el cambio de cámara
            BroadcastMessage("OnCameraChanged", mainCamera, SendMessageOptions.DontRequireReceiver);

            // Restaurar el movimiento del jugador
            EnablePlayerMovement();

            Debug.Log("Modo lockpick desactivado");
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

    // Procesar el input del lockpick
    private void ProcessLockpickInput()
    {
        // Aplicar zona muerta
        Vector2 processedInput = lockpickInput;
        if (processedInput.magnitude < lockpickDeadzone)
        {
            processedInput = Vector2.zero;
        }

        // Suavizar el input
        smoothedLockpickInput = Vector2.Lerp(smoothedLockpickInput, processedInput,
            Time.deltaTime * lockpickInputSmoothing);

        // Convertir el input horizontal a ángulo
        // Usamos solo el componente X para el movimiento izquierda/derecha
        float horizontalInput = smoothedLockpickInput.x;

        // Invertir el input si está configurado para hacerlo
        if (invertLockpickInput)
        {
            horizontalInput = -horizontalInput;
        }

        float inputAngle = horizontalInput * lockpickInputSensitivity * maxAngle;

        // Acumular el ángulo manteniendo los límites
        eulerAngle = Mathf.Clamp(inputAngle, -maxAngle, maxAngle);
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

        // Procesamos la acción del botón Try_LockPick
        if (tryLockPickPressed)
        {
            movePick = false;
            keyPressTime = 1;
            // Guardamos el ángulo actual de la ganzúa al presionar el botón
            lockedPickAngle = eulerAngle;
            // Reiniciamos el valor de rotación actual del innerLock
            currentInnerLockRotation = 0f;

            tryLockPickPressed = false; // Resetear el flag
        }

        if (!tryLockPickHeld && keyPressTime > 0)
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

        if (movePick)
        {
            // Cuando movePick es true, controlamos la ganzúa con el input
            ProcessLockpickInput();

            // Aplicamos la rotación respetando la rotación inicial del objeto
            transform.rotation = initialPickRotation * Quaternion.Euler(0, 0, eulerAngle);
        }
        else
        {
            // Si no estamos moviendo la ganzúa (pulsamos el botón), calculamos las rotaciones

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

                    // IMPORTANTE: Restaurar playerInteraction antes de invocar OnUnlocked
                    // para que los listeners puedan interactuar correctamente
                    if (playerInteraction != null)
                    {
                        playerInteraction.enabled = previousInteractionState;
                        if (playerInteraction.enabled)
                        {
                            playerInteraction.ForceUpdateInteraction();
                        }
                    }

                    // Invocar el evento de desbloqueo
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

    void DisablePlayerMovement()
    {
        PlayerController playerController = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length > 0
            ? FindObjectsByType<PlayerController>(FindObjectsSortMode.None)[0]
            : null;

        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
        }
    }

    void EnablePlayerMovement()
    {
        PlayerController playerController = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length > 0
            ? FindObjectsByType<PlayerController>(FindObjectsSortMode.None)[0]
            : null;

        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
        }
    }

    #region Public Configuration Methods

    /// <summary>
    /// Configura la sensibilidad del input del lockpick
    /// </summary>
    public void SetLockpickInputSensitivity(float sensitivity)
    {
        lockpickInputSensitivity = Mathf.Max(0.1f, sensitivity);
    }

    /// <summary>
    /// Configura el suavizado del input del lockpick
    /// </summary>
    public void SetLockpickInputSmoothing(float smoothing)
    {
        lockpickInputSmoothing = Mathf.Max(0.1f, smoothing);
    }

    /// <summary>
    /// Configura la zona muerta del input del lockpick
    /// </summary>
    public void SetLockpickDeadzone(float deadzone)
    {
        lockpickDeadzone = Mathf.Clamp01(deadzone);
    }

    /// <summary>
    /// Configura si se debe invertir el input horizontal del lockpick
    /// </summary>
    public void SetInvertLockpickInput(bool invert)
    {
        invertLockpickInput = invert;
    }

    #endregion
}