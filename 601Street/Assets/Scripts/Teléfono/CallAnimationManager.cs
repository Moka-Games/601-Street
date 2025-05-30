using UnityEngine;

/// <summary>
/// Gestor de animaciones y objetos durante las llamadas telefónicas
/// </summary>
public class CallAnimationManager : MonoBehaviour
{
    [Header("Referencias del Jugador")]
    [SerializeField] private Animator playerAnimator;
    [Tooltip("GameObject del teléfono que se activará durante la llamada")]
    [SerializeField] private GameObject phoneGameObject;

    [Header("Configuración de Animación")]
    [SerializeField] private string takeCallAnimationName = "TakeCall";
    [SerializeField] private string idleAnimationName = "Idle";
    [SerializeField] private string callEndedParameterName = "callEnded";
    [Tooltip("Tiempo de transición entre animaciones")]
    [SerializeField] private float animationTransitionTime = 0.2f;

    [Header("Configuración del Teléfono")]
    [SerializeField] private bool hidePhoneOnStart = true;

    // Hash de animaciones para mejor rendimiento
    private int takeCallAnimHash;
    private int idleAnimHash;
    private int callEndedParamHash;

    // Estado actual
    private bool isInCall = false;
    private bool isAnimationActive = false;

    // Singleton para acceso fácil
    public static CallAnimationManager Instance { get; private set; }

    private void Awake()
    {
        // Configurar singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Inicializar hashes de animación
        InitializeAnimationHashes();

        // Buscar componentes automáticamente si no están asignados
        FindComponentsIfNeeded();
    }

    private void Start()
    {
        // Ocultar el teléfono al inicio si está configurado
        if (hidePhoneOnStart && phoneGameObject != null)
        {
            phoneGameObject.SetActive(false);
        }

        // Suscribirse a los eventos del sistema de llamadas
        SubscribeToCallEvents();
    }

    private void OnDestroy()
    {
        // Limpiar singleton
        if (Instance == this)
        {
            Instance = null;
        }

        // Desuscribirse de eventos
        UnsubscribeFromCallEvents();
    }

    private void InitializeAnimationHashes()
    {
        takeCallAnimHash = Animator.StringToHash(takeCallAnimationName);
        idleAnimHash = Animator.StringToHash(idleAnimationName);
        callEndedParamHash = Animator.StringToHash(callEndedParameterName);
    }

    private void FindComponentsIfNeeded()
    {
        // Buscar el animator del jugador si no está asignado
        if (playerAnimator == null)
        {
            // Primero intentar en el PlayerController
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                playerAnimator = playerController.GetComponent<Animator>();
            }

            // Si no se encuentra, buscar cualquier Animator en objetos con tag "Player"
            if (playerAnimator == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerAnimator = player.GetComponent<Animator>();
                }
            }

            if (playerAnimator == null)
            {
                Debug.LogWarning("CallAnimationManager: No se pudo encontrar el Animator del jugador automáticamente. Asígnalo manualmente.");
            }
        }

        // Buscar el GameObject del teléfono si no está asignado
        if (phoneGameObject == null)
        {
            // Buscar por nombre común
            phoneGameObject = GameObject.Find("Phone");
            if (phoneGameObject == null)
            {
                phoneGameObject = GameObject.Find("Telefono");
            }
            if (phoneGameObject == null)
            {
                phoneGameObject = GameObject.Find("PhoneModel");
            }

            if (phoneGameObject == null)
            {
                Debug.LogWarning("CallAnimationManager: No se pudo encontrar el GameObject del teléfono automáticamente. Asígnalo manualmente.");
            }
        }
    }

    private void SubscribeToCallEvents()
    {
        if (CallSystem.Instance != null)
        {
            CallSystem.Instance.OnCallStateChanged += HandleCallStateChanged;
            Debug.Log("CallAnimationManager: Suscrito a eventos del CallSystem");
        }
        else
        {
            Debug.LogWarning("CallAnimationManager: CallSystem no encontrado para suscribirse a eventos");
        }
    }

    private void UnsubscribeFromCallEvents()
    {
        if (CallSystem.Instance != null)
        {
            CallSystem.Instance.OnCallStateChanged -= HandleCallStateChanged;
        }
    }

    /// <summary>
    /// Maneja los cambios de estado de la llamada
    /// </summary>
    private void HandleCallStateChanged(bool isCallActive)
    {
        if (isCallActive)
        {
            StartCallAnimation();
        }
        else
        {
            EndCallAnimation();
        }
    }

    /// <summary>
    /// Inicia la animación y activación de objetos para la llamada
    /// </summary>
    public void StartCallAnimation()
    {
        if (isInCall) return; // Ya estamos en llamada

        isInCall = true;
        isAnimationActive = true;

        Debug.Log("CallAnimationManager: Iniciando animación de llamada");

        // IMPORTANTE: Asegurar que callEnded esté en false ANTES de iniciar la nueva llamada
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(callEndedParamHash, false);
            Debug.Log("CallAnimationManager: Parámetro callEnded establecido a FALSE antes de iniciar llamada");
        }

        // Activar el teléfono
        if (phoneGameObject != null)
        {
            phoneGameObject.SetActive(true);
            Debug.Log("CallAnimationManager: Teléfono activado");
        }

        // Reproducir animación de tomar llamada usando CrossFade
        if (playerAnimator != null)
        {
            // Usar CrossFade para una transición suave a TakeCall
            playerAnimator.CrossFade(takeCallAnimHash, animationTransitionTime);
            Debug.Log("CallAnimationManager: Reproduciendo animación TakeCall");
        }
        else
        {
            Debug.LogError("CallAnimationManager: PlayerAnimator no asignado, no se puede reproducir la animación");
        }
    }

    /// <summary>
    /// Termina la animación y desactivación de objetos para la llamada
    /// </summary>
    public void EndCallAnimation()
    {
        if (!isInCall) return; // No estamos en llamada

        isInCall = false;

        Debug.Log("CallAnimationManager: Finalizando animación de llamada");

        // Desactivar el teléfono
        if (phoneGameObject != null)
        {
            phoneGameObject.SetActive(false);
            Debug.Log("CallAnimationManager: Teléfono desactivado");
        }

        // Activar el parámetro callEnded para que el Animator maneje la transición
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(callEndedParamHash, true);
            Debug.Log("CallAnimationManager: Parámetro callEnded establecido a TRUE - transición a Idle");
        }

        isAnimationActive = false;
    }

    /// <summary>
    /// Fuerza el final de la animación (para casos de emergencia)
    /// </summary>
    public void ForceEndCallAnimation()
    {
        Debug.Log("CallAnimationManager: Forzando fin de animación de llamada");
        EndCallAnimation();
    }

    /// <summary>
    /// Verifica si actualmente estamos en una llamada
    /// </summary>
    public bool IsInCall()
    {
        return isInCall;
    }

    /// <summary>
    /// Verifica si la animación está activa
    /// </summary>
    public bool IsAnimationActive()
    {
        return isAnimationActive;
    }

    /// <summary>
    /// Resetea el parámetro callEnded a false (útil para preparar futuras llamadas)
    /// </summary>
    public void ResetCallEndedParameter()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(callEndedParamHash, false);
            Debug.Log("CallAnimationManager: Parámetro callEnded reseteado a FALSE");
        }
    }

    /// <summary>
    /// Prepara el sistema para una nueva llamada (asegura que callEnded esté en false)
    /// </summary>
    public void PrepareForNewCall()
    {
        Debug.Log("CallAnimationManager: Preparando para nueva llamada");

        // Asegurar que callEnded esté en false
        ResetCallEndedParameter();

        // Si hay una llamada activa, terminarla primero
        if (isInCall)
        {
            Debug.LogWarning("CallAnimationManager: Había una llamada activa, terminándola antes de preparar la nueva");
            ForceEndCallAnimation();
        }

        // Resetear estados
        isInCall = false;
        isAnimationActive = false;
    }

    /// <summary>
    /// Configura manualmente las referencias (útil para configuración dinámica)
    /// </summary>
    public void SetReferences(Animator animator, GameObject phone)
    {
        playerAnimator = animator;
        phoneGameObject = phone;

        Debug.Log("CallAnimationManager: Referencias configuradas manualmente");

        // Ocultar el teléfono si está configurado
        if (hidePhoneOnStart && phoneGameObject != null)
        {
            phoneGameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Configura los nombres de las animaciones y parámetros
    /// </summary>
    public void SetAnimationNames(string takeCallAnim, string idleAnim, string callEndedParam = "callEnded")
    {
        takeCallAnimationName = takeCallAnim;
        idleAnimationName = idleAnim;
        callEndedParameterName = callEndedParam;

        // Reinicializar hashes
        InitializeAnimationHashes();

        Debug.Log($"CallAnimationManager: Nombres configurados - TakeCall: {takeCallAnim}, Idle: {idleAnim}, CallEnded: {callEndedParam}");
    }

    /// <summary>
    /// Método público para testing/debugging
    /// </summary>
    [ContextMenu("Test Start Call Animation")]
    public void TestStartCallAnimation()
    {
        StartCallAnimation();
    }

    /// <summary>
    /// Método público para testing/debugging
    /// </summary>
    [ContextMenu("Test End Call Animation")]
    public void TestEndCallAnimation()
    {
        EndCallAnimation();
    }

    /// <summary>
    /// Método público para testing/debugging
    /// </summary>
    [ContextMenu("Reset Call Ended Parameter")]
    public void TestResetCallEndedParameter()
    {
        ResetCallEndedParameter();
    }

    /// <summary>
    /// Método público para testing/debugging - Prepara para nueva llamada
    /// </summary>
    [ContextMenu("Prepare For New Call")]
    public void TestPrepareForNewCall()
    {
        PrepareForNewCall();
    }
}