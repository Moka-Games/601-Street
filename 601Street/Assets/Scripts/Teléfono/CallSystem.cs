using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
    [SerializeField] private KeyCode acceptCallKey = KeyCode.F;
    [SerializeField] private KeyCode rejectCallKey = KeyCode.Escape;
    [SerializeField] private string showPopupAnimName = "ShowCallPopup";
    [SerializeField] private string hidePopupAnimName = "HideCallPopup";
    [SerializeField] private float popupDuration = 15f; // Tiempo que se muestra el popup antes de cerrarse automáticamente

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip incomingCallSound;
    [SerializeField] private AudioClip callAcceptedSound;
    [SerializeField] private AudioClip callRejectedSound;

    // Evento que se dispara cuando cambia el estado de la llamada
    public event Action<bool> OnCallStateChanged; // true = llamada activa, false = llamada inactiva

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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

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

    private void Start()
    {
        StartCoroutine(FindAndSubscribeToDialogueManager());
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
                // Intentar buscar en todos los objetos de todas las escenas cargadas
                dialogueManager = FindFirstObjectByType<DialogueManager>();
            }

            if (dialogueManager != null)
            {
                Debug.Log("CallSystem: DialogueManager encontrado exitosamente.");

                // Suscribirse al evento
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

    private void OnEnable()
    {
        // Suscribirse al evento de diálogo finalizado para cerrar la llamada
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.onConversationEnd.AddListener(HandleConversationEnd);
        }
        else
        {
            Debug.LogWarning("CallSystem: No se encontró el DialogueManager");
        }
    }

    private void OnDisable()
    {
        // Desuscribirse del evento
        DialogueManager dialogueManager = DialogueManager.Instance;
        if (dialogueManager != null)
        {
            dialogueManager.onConversationEnd.RemoveListener(HandleConversationEnd);
        }
    }

    private void Update()
    {
        // Solo procesar input para aceptar la llamada si el popup está visible
        if (isPopupVisible)
        {
            // Aceptar llamada
            if (Input.GetKeyDown(acceptCallKey))
            {
                AcceptCall();
            }
            // Ya no tendremos opción de rechazar la llamada
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

        // Guardar referencia a los eventos
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
            callerDescriptionText.text = description;
        }

        // Actualizar avatar directamente usando la referencia
        if (callerAvatarImage != null && avatar != null)
        {
            callerAvatarImage.sprite = avatar;
            callerAvatarImage.gameObject.SetActive(true);
            Debug.Log("CallSystem: Avatar configurado correctamente");
        }
        else if (callerAvatarImage != null)
        {
            // Si no hay avatar, podemos ocultar el elemento o usar uno predeterminado
            // Por ahora, simplemente lo desactivamos
            callerAvatarImage.gameObject.SetActive(false);
            Debug.Log("CallSystem: No se proporcionó avatar, elemento desactivado");
        }
        else
        {
            Debug.LogWarning("CallSystem: No se ha asignado referencia a callerAvatarImage en el Inspector");
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

        // Reproducir sonido de llamada entrante (con configuración clara del bucle)
        if (audioSource != null && incomingCallSound != null)
        {
            audioSource.Stop(); // Detener cualquier sonido anterior
            audioSource.clip = incomingCallSound;
            audioSource.loop = true; // Configurar explícitamente para bucle
            audioSource.Play();
            Debug.Log("CallSystem: Reproduciendo sonido de llamada entrante en bucle");
        }

        isPopupVisible = true;

        // En este nuevo sistema, el popup permanecerá visible hasta que finalice la conversación
        // El usuario solo tiene la opción de aceptar la llamada, así que mostramos un mensaje adecuado
        if (callerDescriptionText != null)
        {
            callerDescriptionText.text = description + "\nPress " + acceptCallKey.ToString() + " to accept";
        }
    }

    // Ocultar el popup de llamada
    private void HideCallPopup()
    {
        // Detener el temporizador si existe
        if (popupTimerCoroutine != null)
        {
            StopCoroutine(popupTimerCoroutine);
            popupTimerCoroutine = null;
        }

        // Reproducir animación de ocultar
        if (callPanelAnimator != null && callPopupPanel != null && callPopupPanel.activeSelf)
        {
            callPanelAnimator.Play(hidePopupAnimName);
            StartCoroutine(DisablePopupAfterAnimation(hidePopupAnimName));
        }
        else if (callPopupPanel != null)
        {
            callPopupPanel.SetActive(false);
        }

        // Detener sonido explícitamente
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
        if (!isPopupVisible) return;

        // Detener el sonido de llamada entrante
        if (audioSource != null)
        {
            audioSource.Stop(); // Primero detenemos cualquier sonido que esté reproduciéndose

            // Ahora reproducimos el sonido de llamada aceptada una única vez
            if (callAcceptedSound != null)
            {
                audioSource.loop = false; // Asegurar que no haga bucle
                audioSource.clip = callAcceptedSound;
                audioSource.PlayOneShot(callAcceptedSound); // Usamos PlayOneShot para asegurar una sola reproducción
                Debug.Log("CallSystem: Reproduciendo sonido de llamada aceptada");
            }
        }

        // Invocar el evento de llamada aceptada
        currentCallAcceptedEvent?.Invoke();
        Debug.Log("CallSystem: Evento OnCallAccepted invocado");

        isCallActive = true;
        OnCallStateChanged?.Invoke(true);
        StartSafetyCheck();

        if (callerDescriptionText != null)
        {
            callerDescriptionText.text = "Ongoing call...";
        }

        // Detener el temporizador de cierre automático si existe
        if (popupTimerCoroutine != null)
        {
            StopCoroutine(popupTimerCoroutine);
            popupTimerCoroutine = null;
        }

        // Iniciar la conversación si hay una válida
        if (currentCallConversation != null && DialogueManager.Instance != null)
        {
            // Asegurar que el DialogueManager esté listo para una nueva conversación
            if (DialogueManager.Instance.dialogueUI != null && !DialogueManager.Instance.dialogueUI.activeSelf)
            {
                DialogueManager.Instance.dialogueUI.SetActive(true);
            }

            // Buscar el NPC apropiado o usar uno temporal
            NPC callerNPC = FindCallerNPC();
            if (callerNPC != null)
            {
                Debug.Log("CallSystem: Iniciando conversación telefónica");

                // Forzar la activación del diálogo
                Enabler enabler = Enabler.Instance;
                if (enabler != null)
                {
                    // Bloquear al jugador durante la conversación
                    enabler.BlockPlayer();
                }

                // Iniciar la conversación con el DialogueManager
                DialogueManager.Instance.StartConversation(currentCallConversation, callerNPC);

                // Asegurar que se muestre la interfaz de diálogo
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
        else
        {
            if (currentCallConversation == null)
            {
                Debug.LogError("CallSystem: No hay conversación asignada para esta llamada");
            }
            if (DialogueManager.Instance == null)
            {
                Debug.LogError("CallSystem: DialogueManager no encontrado");
            }
        }
    }

    // Rechazar llamada
    public void RejectCall()
    {
        if (!isPopupVisible) return;

        // Detener el sonido de llamada entrante primero
        if (audioSource != null)
        {
            audioSource.Stop();

            // Reproducir sonido de llamada rechazada una única vez
            if (callRejectedSound != null)
            {
                audioSource.loop = false;
                audioSource.clip = callRejectedSound;
                audioSource.PlayOneShot(callRejectedSound);
                Debug.Log("CallSystem: Reproduciendo sonido de llamada rechazada");
            }
        }

        // Invocar el evento de llamada rechazada
        currentCallRejectedEvent?.Invoke();
        Debug.Log("CallSystem: Evento OnCallRejected invocado");

        // Ocultar popup
        HideCallPopup();

        // Marcar llamada como rechazada
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
        // Buscar un NPC específico para llamadas
        NPC callerNPC = FindAnyObjectByType<PhoneNPC>();

        if (callerNPC == null)
        {
            // Si no hay un NPC específico, buscar cualquier NPC
            callerNPC = FindAnyObjectByType<NPC>();

            if (callerNPC == null)
            {
                Debug.Log("CallSystem: Creando NPC temporal para la llamada telefónica");

                // Como último recurso, crear un NPC temporal
                GameObject tempNPC = new GameObject("TempCallerNPC");

                // Asegurarse de que no se destruya durante la conversación
                DontDestroyOnLoad(tempNPC);

                // Agregar componente NPC
                callerNPC = tempNPC.AddComponent<NPC>();

                // Configuración específica para diálogos telefónicos
                if (currentCallConversation != null)
                {
                    // Asignar la conversación actual
                    callerNPC.conversation = currentCallConversation;

                    // Asignar versiones alternativas de la conversación si es necesario
                    callerNPC.funnyConversation = currentCallConversation;

                    // Configurar opciones adicionales del NPC para que funcione con el sistema de diálogo
                    // Estas propiedades dependerán de la implementación específica de tu clase NPC
                    callerNPC.npcId = -999; // ID especial para NPCs de llamadas
                }

                // Programar la destrucción del NPC temporal después de que termine la conversación
                StartCoroutine(DestroyTemporaryNPC(tempNPC));
            }
            else
            {
                Debug.Log("CallSystem: Usando NPC existente para la llamada");
            }
        }
        else
        {
            Debug.Log("CallSystem: Usando PhoneNPC específico para la llamada");
        }

        return callerNPC;
    }

    // Corrutina para destruir el NPC temporal después de que termine la conversación
    private IEnumerator DestroyTemporaryNPC(GameObject tempNPC)
    {
        // Esperar hasta que la llamada ya no esté activa
        yield return new WaitUntil(() => !isCallActive);

        // Esperar un poco más para asegurarse de que todas las referencias se han liberado
        yield return new WaitForSeconds(1f);

        // Destruir el NPC temporal
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

            // Liberar al jugador - Enfoque con múltiples métodos para garantizar que funcione
            ReleasePlayerMultipleWays();

            // Actualizar el texto de estado antes de ocultar el popup
            if (callerDescriptionText != null)
            {
                callerDescriptionText.text = "Call ended";
            }

            // Ocultar el popup
            HideCallPopup();

            // Finalizar la llamada - esto invocará el evento OnCallFinished
            EndCall();
        }
        else
        {
            Debug.Log("CallSystem: OnConversationEnd recibido pero isCallActive es false");
        }
    }

    // Finalizar llamada
    private void EndCall()
    {
        Debug.Log("CallSystem: Llamada finalizada");

        // Asegurarnos de que no hay sonidos reproduciéndose
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Invocar el evento de finalización si existe y no es nulo
        if (currentCallFinishedEvent != null)
        {
            currentCallFinishedEvent.Invoke();
            Debug.Log("CallSystem: Evento OnCallFinished invocado");
        }

        // Restablecer variables
        currentCallConversation = null;
        currentCallAcceptedEvent = null;
        currentCallRejectedEvent = null;
        currentCallFinishedEvent = null;
        isCallActive = false;

        // Notificar
        OnCallStateChanged?.Invoke(false);
    }

    // Método público para forzar el final de una llamada
    public void ForceEndCall()
    {
        Debug.Log("CallSystem: Finalizando llamada forzosamente");

        // Asegurarse de ocultar el popup
        HideCallPopup();

        // Finalizar la llamada
        EndCall();
    }

    // Método para verificar si hay una llamada activa (para el monitor)
    public bool IsCallActive()
    {
        return isCallActive;
    }

    // Corrutina para manejar una secuencia de llamada
    private IEnumerator CallSequence(CallData callData)
    {
        // Esperar hasta que la interacción con el popup finalice
        yield return new WaitUntil(() => !isPopupVisible || isCallActive);

        // Si la llamada está activa, entonces fue aceptada
        if (isCallActive)
        {
            // Esperar a que termine la conversación (la llamada ya no está activa)
            yield return new WaitUntil(() => !isCallActive);

            // No necesitamos invocar el evento de finalización aquí,
            // ya se invoca en el método EndCall
        }
        // Si no está activa y no está visible, fue rechazada
        // (no necesitamos hacer nada aquí, ya se maneja en RejectCall)

        activeCallCoroutine = null;
    }

    // Temporizador para ocultar el popup automáticamente
    private IEnumerator PopupTimer()
    {
        yield return new WaitForSeconds(popupDuration);

        if (isPopupVisible)
        {
            // Si el popup sigue visible después del tiempo, rechazar automáticamente
            RejectCall();
        }

        popupTimerCoroutine = null;
    }

    // Desactivar el panel después de la animación
    private IEnumerator DisablePopupAfterAnimation(string animName)
    {
        // Esperar a que termine la animación
        if (callPanelAnimator != null)
        {
            AnimationClip[] clips = callPanelAnimator.runtimeAnimatorController.animationClips;
            float animDuration = 1f; // Valor por defecto

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

        // Desactivar el panel
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
        // Esperar un tiempo razonable para que la conversación termine
        yield return new WaitForSeconds(0.5f);

        // Si la llamada sigue activa pero el diálogo terminó, forzar la liberación del jugador
        if (isCallActive && DialogueManager.Instance != null &&
            DialogueManager.Instance.dialogueUI != null &&
            !DialogueManager.Instance.dialogueUI.activeSelf)
        {
            Debug.LogWarning("CallSystem: Forzando liberación del jugador por seguridad");

            // Liberar al jugador
            Enabler enabler = Enabler.Instance;
            if (enabler != null)
            {
                enabler.ReleasePlayer();
            }

            // Desbloquear directamente si es necesario
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

            // Finalizar la llamada
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
        else
        {
            Debug.LogWarning("CallSystem: Enabler.Instance es null, intentando métodos alternativos");
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