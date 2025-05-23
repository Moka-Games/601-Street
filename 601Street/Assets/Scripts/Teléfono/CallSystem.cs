using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CallSystem : MonoBehaviour
{
    // Singleton para acceso global
    public static CallSystem Instance { get; private set; }

    [Header("UI Referencias")]
    [SerializeField] private GameObject callPopupPanel;
    [SerializeField] private Animator callPanelAnimator;
    [SerializeField] private TMPro.TMP_Text callerNameText;
    [SerializeField] private TMPro.TMP_Text callerDescriptionText;
    [Tooltip("Referencia directa a la imagen que mostrará el avatar del llamante")]
    [SerializeField] private Image callerAvatarImage;

    [Header("Configuración")]
    [SerializeField] private string showPopupAnimName = "ShowCallPopup";
    [SerializeField] private string hidePopupAnimName = "HideCallPopup";
    [SerializeField] private float popupDuration = 15f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip incomingCallSound;
    [SerializeField] private AudioClip callAcceptedSound;
    [SerializeField] private AudioClip callRejectedSound;

    // Input System
    private PlayerControls playerControls;
    private InputAction acceptCallAction;

    // Evento que se dispara cuando cambia el estado de la llamada
    public event Action<bool> OnCallStateChanged;

    // Variables de estado
    private bool isCallActive = false;
    private bool isPopupVisible = false;
    private Conversation currentCallConversation;
    private Coroutine activeCallCoroutine;
    private Coroutine popupTimerCoroutine;

    // Referencia a los eventos de la llamada actual
    private UnityEvent currentCallAcceptedEvent;
    private UnityEvent currentCallRejectedEvent;
    private UnityEvent currentCallFinishedEvent;

    private bool conversationEnded = false;
    private Coroutine safetyCheckCoroutine;

    // Estructura para almacenar datos de la llamada
    [System.Serializable]
    public class CallData
    {
        public string callerName;
        public string callerDescription;
        public Conversation callConversation;
        public Sprite callerAvatar;
        public UnityEvent onCallAccepted;
        public UnityEvent onCallRejected;
        public UnityEvent onCallFinished;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Inicializar Input System
        InitializeInputSystem();

        // Verificar referencias necesarias
        if (callPopupPanel == null)
        {
            Debug.LogError("CallSystem: No se asignó el panel de popup");
        }

        if (callPanelAnimator == null && callPopupPanel != null)
        {
            callPanelAnimator = callPopupPanel.GetComponent<Animator>();
            if (callPanelAnimator == null)
            {
                Debug.LogWarning("CallSystem: No se encontró un Animator en el panel de popup");
            }
        }

        // Configurar el audio
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Asegurarse de que el popup esté oculto al inicio
        if (callPopupPanel != null)
        {
            callPopupPanel.SetActive(false);
        }
    }

    private void InitializeInputSystem()
    {
        playerControls = new PlayerControls();
        acceptCallAction = playerControls.Gameplay.AcceptCall;

        // Suscribirse al evento de aceptar llamada
        acceptCallAction.performed += OnAcceptCallInput;
    }

    private void OnEnable()
    {
        playerControls?.Gameplay.Enable();

        // Suscribirse al evento de diálogo finalizado para cerrar la llamada
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.onConversationEnd.AddListener(HandleConversationEnd);
        }
    }

    private void OnDisable()
    {
        playerControls?.Gameplay.Disable();

        // Desuscribirse del evento
        DialogueManager dialogueManager = DialogueManager.Instance;
        if (dialogueManager != null)
        {
            dialogueManager.onConversationEnd.RemoveListener(HandleConversationEnd);
        }
    }

    private void OnDestroy()
    {
        if (acceptCallAction != null)
        {
            acceptCallAction.performed -= OnAcceptCallInput;
        }

        playerControls?.Dispose();
    }

    private void Start()
    {
        StartCoroutine(FindAndSubscribeToDialogueManager());
    }

    // Callback para el Input System
    private void OnAcceptCallInput(InputAction.CallbackContext context)
    {
        if (isPopupVisible && !isCallActive)
        {
            AcceptCall();
        }
    }

    private IEnumerator FindAndSubscribeToDialogueManager()
    {
        // Esperar dos frames para que otros objetos se inicialicen
        yield return null;
        yield return null;

        Debug.Log("CallSystem: Buscando DialogueManager para suscribirse...");

        DialogueManager dialogueManager = null;
        int attempts = 0;

        // Intentar encontrar el DialogueManager varias veces
        while (dialogueManager == null && attempts < 5)
        {
            dialogueManager = DialogueManager.Instance;

            if (dialogueManager == null)
            {
                dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            if (dialogueManager != null)
            {
                Debug.Log("CallSystem: DialogueManager encontrado exitosamente.");
                dialogueManager.onConversationEnd.AddListener(HandleConversationEnd);
                Debug.Log("CallSystem: Suscrito al evento onConversationEnd del DialogueManager");
                break;
            }

            attempts++;
            Debug.Log($"CallSystem: Intento {attempts}/5 fallido para encontrar DialogueManager");
            yield return new WaitForSeconds(0.2f);
        }

        if (dialogueManager == null)
        {
            Debug.LogError("CallSystem: No se pudo encontrar el DialogueManager después de varios intentos. Las llamadas no terminarán correctamente.");
        }
    }

    // Método público para iniciar una llamada
    public void StartCall(CallData callData)
    {
        if (isCallActive)
        {
            Debug.LogWarning("CallSystem: Ya hay una llamada en curso");
            return;
        }

        // Guardar datos de la llamada
        currentCallConversation = callData.callConversation;
        currentCallAcceptedEvent = callData.onCallAccepted;
        currentCallRejectedEvent = callData.onCallRejected;
        currentCallFinishedEvent = callData.onCallFinished;

        // Mostrar popup
        Debug.Log($"CallSystem: Iniciando llamada con avatar: {(callData.callerAvatar != null ? "Presente" : "No proporcionado")}");
        ShowCallPopup(callData.callerName, callData.callerDescription, callData.callerAvatar);

        // Iniciar temporizador para la llamada
        if (activeCallCoroutine != null)
        {
            StopCoroutine(activeCallCoroutine);
        }
        activeCallCoroutine = StartCoroutine(CallSequence(callData));
    }

    // Mostrar el popup de llamada
    private void ShowCallPopup(string callerName, string description, Sprite avatar = null)
    {
        // Actualizar textos
        if (callerNameText != null)
        {
            callerNameText.text = callerName;
        }

        if (callerDescriptionText != null)
        {
            // Obtener el texto del binding actual para mostrar al usuario
            string acceptKey = GetAcceptCallKeyDisplayText();
            callerDescriptionText.text = description + $"\nPress {acceptKey} to accept";
        }

        // Actualizar avatar
        if (callerAvatarImage != null && avatar != null)
        {
            callerAvatarImage.sprite = avatar;
            callerAvatarImage.gameObject.SetActive(true);
            Debug.Log("CallSystem: Avatar configurado correctamente");
        }
        else if (callerAvatarImage != null)
        {
            callerAvatarImage.gameObject.SetActive(false);
            Debug.Log("CallSystem: No se proporcionó avatar, elemento desactivado");
        }

        // Activar panel
        if (callPopupPanel != null)
        {
            callPopupPanel.SetActive(true);

            // Reproducir animación si existe
            if (callPanelAnimator != null)
            {
                callPanelAnimator.Play(showPopupAnimName);
            }
        }

        // Reproducir sonido de llamada entrante
        if (audioSource != null && incomingCallSound != null)
        {
            audioSource.Stop();
            audioSource.clip = incomingCallSound;
            audioSource.loop = true;
            audioSource.Play();
            Debug.Log("CallSystem: Reproduciendo sonido de llamada entrante en bucle");
        }

        isPopupVisible = true;
    }

    // Obtener el texto de la tecla de aceptar llamada para mostrar al usuario
    private string GetAcceptCallKeyDisplayText()
    {
        if (acceptCallAction != null && acceptCallAction.bindings.Count > 0)
        {
            // Obtener el primer binding para mostrar
            var binding = acceptCallAction.bindings[0];
            string displayString = InputControlPath.ToHumanReadableString(binding.effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
            return displayString;
        }
        return "F"; // Fallback
    }

    // Ocultar el popup de llamada
    private void HideCallPopup()
    {
        if (popupTimerCoroutine != null)
        {
            StopCoroutine(popupTimerCoroutine);
            popupTimerCoroutine = null;
        }

        if (callPanelAnimator != null && callPopupPanel != null && callPopupPanel.activeSelf)
        {
            callPanelAnimator.Play(hidePopupAnimName);
            StartCoroutine(DisablePopupAfterAnimation(hidePopupAnimName));
        }
        else if (callPopupPanel != null)
        {
            callPopupPanel.SetActive(false);
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("CallSystem: Deteniendo todos los sonidos");
        }

        isPopupVisible = false;
    }

    // Aceptar llamada
    public void AcceptCall()
    {
        if (!isPopupVisible || isCallActive) return;

        if (audioSource != null)
        {
            audioSource.Stop();

            if (callAcceptedSound != null)
            {
                audioSource.loop = false;
                audioSource.clip = callAcceptedSound;
                audioSource.PlayOneShot(callAcceptedSound);
                Debug.Log("CallSystem: Reproduciendo sonido de llamada aceptada");
            }
        }

        currentCallAcceptedEvent?.Invoke();
        Debug.Log("CallSystem: Evento OnCallAccepted invocado");

        isCallActive = true;
        OnCallStateChanged?.Invoke(true);
        StartSafetyCheck();

        if (callerDescriptionText != null)
        {
            callerDescriptionText.text = "Ongoing call...";
        }

        if (popupTimerCoroutine != null)
        {
            StopCoroutine(popupTimerCoroutine);
            popupTimerCoroutine = null;
        }

        // Iniciar la conversación
        if (currentCallConversation != null && DialogueManager.Instance != null)
        {
            if (DialogueManager.Instance.dialogueUI != null && !DialogueManager.Instance.dialogueUI.activeSelf)
            {
                DialogueManager.Instance.dialogueUI.SetActive(true);
            }

            NPC callerNPC = FindCallerNPC();
            if (callerNPC != null)
            {
                Debug.Log("CallSystem: Iniciando conversación telefónica");

                Enabler enabler = Enabler.Instance;
                if (enabler != null)
                {
                    enabler.BlockPlayer();
                }

                DialogueManager.Instance.StartConversation(currentCallConversation, callerNPC);

                if (DialogueManager.Instance.dialogueUI != null)
                {
                    DialogueManager.Instance.dialogueUI.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("CallSystem: No se pudo encontrar o crear un NPC válido");
            }
        }
    }

    // Rechazar llamada
    public void RejectCall()
    {
        if (!isPopupVisible) return;

        if (audioSource != null)
        {
            audioSource.Stop();

            if (callRejectedSound != null)
            {
                audioSource.loop = false;
                audioSource.clip = callRejectedSound;
                audioSource.PlayOneShot(callRejectedSound);
                Debug.Log("CallSystem: Reproduciendo sonido de llamada rechazada");
            }
        }

        currentCallRejectedEvent?.Invoke();
        Debug.Log("CallSystem: Evento OnCallRejected invocado");

        HideCallPopup();

        if (activeCallCoroutine != null)
        {
            StopCoroutine(activeCallCoroutine);
            activeCallCoroutine = null;
        }

        currentCallConversation = null;
        isCallActive = false;
        OnCallStateChanged?.Invoke(false);
    }

    // Buscar un NPC para la llamada (o crear uno temporal)
    private NPC FindCallerNPC()
    {
        NPC callerNPC = FindAnyObjectByType<PhoneNPC>();

        if (callerNPC == null)
        {
            callerNPC = FindAnyObjectByType<NPC>();

            if (callerNPC == null)
            {
                Debug.Log("CallSystem: Creando NPC temporal para la llamada telefónica");

                GameObject tempNPC = new GameObject("TempCallerNPC");
                DontDestroyOnLoad(tempNPC);
                callerNPC = tempNPC.AddComponent<NPC>();

                if (currentCallConversation != null)
                {
                    callerNPC.conversation = currentCallConversation;
                    callerNPC.funnyConversation = currentCallConversation;
                    callerNPC.npcId = -999;
                }

                StartCoroutine(DestroyTemporaryNPC(tempNPC));
            }
        }

        return callerNPC;
    }

    private IEnumerator DestroyTemporaryNPC(GameObject tempNPC)
    {
        yield return new WaitUntil(() => !isCallActive);
        yield return new WaitForSeconds(1f);

        if (tempNPC != null)
        {
            Destroy(tempNPC);
            Debug.Log("CallSystem: NPC temporal destruido");
        }
    }

    private void HandleConversationEnd()
    {
        Debug.Log("CallSystem: Evento OnConversationEnd recibido desde DialogueManager");

        if (isCallActive)
        {
            Debug.Log("CallSystem: Finalizando llamada tras terminar la conversación");
            ReleasePlayerMultipleWays();

            if (callerDescriptionText != null)
            {
                callerDescriptionText.text = "Call ended";
            }

            HideCallPopup();
            EndCall();
        }
    }

    private void EndCall()
    {
        Debug.Log("CallSystem: Llamada finalizada");

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (currentCallFinishedEvent != null)
        {
            currentCallFinishedEvent.Invoke();
            Debug.Log("CallSystem: Evento OnCallFinished invocado");
        }

        currentCallConversation = null;
        currentCallAcceptedEvent = null;
        currentCallRejectedEvent = null;
        currentCallFinishedEvent = null;
        isCallActive = false;

        OnCallStateChanged?.Invoke(false);
    }

    public void ForceEndCall()
    {
        Debug.Log("CallSystem: Finalizando llamada forzosamente");
        HideCallPopup();
        EndCall();
    }

    public bool IsCallActive()
    {
        return isCallActive;
    }

    private IEnumerator CallSequence(CallData callData)
    {
        yield return new WaitUntil(() => !isPopupVisible || isCallActive);

        if (isCallActive)
        {
            yield return new WaitUntil(() => !isCallActive);
        }

        activeCallCoroutine = null;
    }

    private IEnumerator DisablePopupAfterAnimation(string animName)
    {
        if (callPanelAnimator != null)
        {
            AnimationClip[] clips = callPanelAnimator.runtimeAnimatorController.animationClips;
            float animDuration = 1f;

            foreach (AnimationClip clip in clips)
            {
                if (clip.name == animName)
                {
                    animDuration = clip.length;
                    break;
                }
            }

            yield return new WaitForSeconds(animDuration);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (callPopupPanel != null)
        {
            callPopupPanel.SetActive(false);
        }
    }

    public void StartSafetyCheck()
    {
        if (safetyCheckCoroutine != null)
        {
            StopCoroutine(safetyCheckCoroutine);
        }
        safetyCheckCoroutine = StartCoroutine(SafetyCheckCoroutine());
    }

    private IEnumerator SafetyCheckCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (isCallActive && DialogueManager.Instance != null &&
            DialogueManager.Instance.dialogueUI != null &&
            !DialogueManager.Instance.dialogueUI.activeSelf)
        {
            Debug.LogWarning("CallSystem: Forzando liberación del jugador por seguridad");

            Enabler enabler = Enabler.Instance;
            if (enabler != null)
            {
                enabler.ReleasePlayer();
            }

            PlayerController playerController = FindAnyObjectByType<PlayerController>();
            if (playerController != null)
            {
                playerController.SetMovementEnabled(true);
            }

            Camera_Script cameraScript = FindAnyObjectByType<Camera_Script>();
            if (cameraScript != null)
            {
                cameraScript.UnfreezeCamera();
            }

            EndCall();
        }
        safetyCheckCoroutine = null;
    }

    private void ReleasePlayerMultipleWays()
    {
        Debug.Log("CallSystem: Intentando liberar al jugador por múltiples métodos...");

        Enabler enabler = Enabler.Instance;
        if (enabler != null)
        {
            Debug.Log("CallSystem: Liberando al jugador con Enabler.ReleasePlayer()");
            enabler.ReleasePlayer();
        }

        if (GameStateManager.Instance != null)
        {
            Debug.Log("CallSystem: Cambiando estado a GameplayState");
            GameStateManager.Instance.EnterGameplayState();
        }

        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("CallSystem: Habilitando movimiento directamente con PlayerController");
            playerController.SetMovementEnabled(true);
        }

        Camera_Script cameraScript = FindFirstObjectByType<Camera_Script>();
        if (cameraScript != null)
        {
            Debug.Log("CallSystem: Desbloqueando cámara directamente");
            cameraScript.UnfreezeCamera();
        }

        PlayerInteraction.SetSceneTransitionState(false);
    }
}