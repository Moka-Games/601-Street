using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CallSystem : MonoBehaviour
{
    // Singleton para acceso global
    public static CallSystem Instance { get; private set; }

    [Header("UI Referencias")]
    [SerializeField] private GameObject callPopupPanel;
    [SerializeField] private Animator callPanelAnimator;
    [SerializeField] private TMPro.TMP_Text callerNameText;
    [SerializeField] private TMPro.TMP_Text callerDescriptionText;

    [Header("Configuraci�n")]
    [SerializeField] private KeyCode acceptCallKey = KeyCode.F;
    [SerializeField] private KeyCode rejectCallKey = KeyCode.Escape;
    [SerializeField] private string showPopupAnimName = "ShowCallPopup";
    [SerializeField] private string hidePopupAnimName = "HideCallPopup";
    [SerializeField] private float popupDuration = 15f; // Tiempo que se muestra el popup antes de cerrarse autom�ticamente

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

    // Estructura para almacenar datos de la llamada
    [System.Serializable]
    public class CallData
    {
        public string callerName;
        public string callerDescription;
        public Conversation callConversation;
        public UnityEvent onCallAccepted;
        public UnityEvent onCallRejected;
        public UnityEvent onCallFinished;
    }

    private void Awake()
    {
        // Configuraci�n del singleton
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
            Debug.LogError("CallSystem: No se asign� el panel de popup");
        }

        if (callPanelAnimator == null && callPopupPanel != null)
        {
            callPanelAnimator = callPopupPanel.GetComponent<Animator>();
            if (callPanelAnimator == null)
            {
                Debug.LogWarning("CallSystem: No se encontr� un Animator en el panel de popup");
            }
        }

        // Configurar el audio
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Asegurarse de que el popup est� oculto al inicio
        if (callPopupPanel != null)
        {
            callPopupPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // Suscribirse al evento de di�logo finalizado para cerrar la llamada
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.onConversationEnd.AddListener(HandleConversationEnd);
        }
        else
        {
            Debug.LogWarning("CallSystem: No se encontr� el DialogueManager");
        }
    }

    private void OnDisable()
    {
        // Desuscribirse del evento
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.onConversationEnd.RemoveListener(HandleConversationEnd);
        }
    }

    private void Update()
    {
        // Solo procesar input para aceptar la llamada si el popup est� visible
        if (isPopupVisible)
        {
            // Aceptar llamada
            if (Input.GetKeyDown(acceptCallKey))
            {
                AcceptCall();
            }
            // Ya no tendremos opci�n de rechazar la llamada
        }
    }

    // M�todo p�blico para iniciar una llamada
    public void StartCall(CallData callData)
    {
        if (isCallActive)
        {
            Debug.LogWarning("CallSystem: Ya hay una llamada en curso");
            return;
        }

        // Guardar datos de la llamada
        currentCallConversation = callData.callConversation;

        // Mostrar popup
        ShowCallPopup(callData.callerName, callData.callerDescription);

        // Iniciar temporizador para la llamada
        if (activeCallCoroutine != null)
        {
            StopCoroutine(activeCallCoroutine);
        }
        activeCallCoroutine = StartCoroutine(CallSequence(callData));
    }

    // Mostrar el popup de llamada
    private void ShowCallPopup(string callerName, string description)
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

        // Activar panel
        if (callPopupPanel != null)
        {
            callPopupPanel.SetActive(true);

            // Reproducir animaci�n si existe
            if (callPanelAnimator != null)
            {
                callPanelAnimator.Play(showPopupAnimName);
            }
        }

        // Reproducir sonido
        if (audioSource != null && incomingCallSound != null)
        {
            audioSource.clip = incomingCallSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        isPopupVisible = true;

        // En este nuevo sistema, el popup permanecer� visible hasta que finalice la conversaci�n
        // El usuario solo tiene la opci�n de aceptar la llamada, as� que mostramos un mensaje adecuado
        if (callerDescriptionText != null)
        {
            callerDescriptionText.text = description + "\nPresiona " + acceptCallKey.ToString() + " para atender";
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

        // Reproducir animaci�n de ocultar
        if (callPanelAnimator != null && callPopupPanel != null && callPopupPanel.activeSelf)
        {
            callPanelAnimator.Play(hidePopupAnimName);
            StartCoroutine(DisablePopupAfterAnimation(hidePopupAnimName));
        }
        else if (callPopupPanel != null)
        {
            callPopupPanel.SetActive(false);
        }

        // Detener sonido
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        isPopupVisible = false;
    }

    // Aceptar llamada
    public void AcceptCall()
    {
        if (!isPopupVisible) return;

        // Reproducir sonido
        if (audioSource != null && callAcceptedSound != null)
        {
            audioSource.loop = false;
            audioSource.clip = callAcceptedSound;
            audioSource.Play();
        }

        // Marcar la llamada como activa
        isCallActive = true;
        OnCallStateChanged?.Invoke(true);

        // Ya no ocultamos el popup, permanecer� visible durante la conversaci�n
        // Podemos actualizar el texto o la interfaz para indicar que la llamada est� en curso
        if (callerDescriptionText != null)
        {
            callerDescriptionText.text = "Llamada en curso...";
        }

        // Detener el temporizador de cierre autom�tico si existe
        if (popupTimerCoroutine != null)
        {
            StopCoroutine(popupTimerCoroutine);
            popupTimerCoroutine = null;
        }

        // Iniciar la conversaci�n si hay una v�lida
        if (currentCallConversation != null && DialogueManager.Instance != null)
        {
            // Asegurar que el DialogueManager est� listo para una nueva conversaci�n
            if (DialogueManager.Instance.dialogueUI != null && !DialogueManager.Instance.dialogueUI.activeSelf)
            {
                DialogueManager.Instance.dialogueUI.SetActive(true);
            }

            // Buscar el NPC apropiado o usar uno temporal
            NPC callerNPC = FindCallerNPC();
            if (callerNPC != null)
            {
                Debug.Log("CallSystem: Iniciando conversaci�n telef�nica");

                // Forzar la activaci�n del di�logo
                Enabler enabler = Enabler.Instance;
                if (enabler != null)
                {
                    // Bloquear al jugador durante la conversaci�n
                    enabler.BlockPlayer();
                }

                // Iniciar la conversaci�n con el DialogueManager
                DialogueManager.Instance.StartConversation(currentCallConversation, callerNPC);

                // Asegurar que se muestre la interfaz de di�logo
                if (DialogueManager.Instance.dialogueUI != null)
                {
                    DialogueManager.Instance.dialogueUI.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("CallSystem: No se pudo encontrar o crear un NPC v�lido");
            }
        }
        else
        {
            if (currentCallConversation == null)
            {
                Debug.LogError("CallSystem: No hay conversaci�n asignada para esta llamada");
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

        // Ocultar popup
        HideCallPopup();

        // Reproducir sonido
        if (audioSource != null && callRejectedSound != null)
        {
            audioSource.loop = false;
            audioSource.clip = callRejectedSound;
            audioSource.Play();
        }

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
        // Buscar un NPC espec�fico para llamadas
        NPC callerNPC = FindAnyObjectByType<PhoneNPC>();

        if (callerNPC == null)
        {
            // Si no hay un NPC espec�fico, buscar cualquier NPC
            callerNPC = FindAnyObjectByType<NPC>();

            if (callerNPC == null)
            {
                Debug.Log("CallSystem: Creando NPC temporal para la llamada telef�nica");

                // Como �ltimo recurso, crear un NPC temporal
                GameObject tempNPC = new GameObject("TempCallerNPC");

                // Asegurarse de que no se destruya durante la conversaci�n
                DontDestroyOnLoad(tempNPC);

                // Agregar componente NPC
                callerNPC = tempNPC.AddComponent<NPC>();

                // Configuraci�n espec�fica para di�logos telef�nicos
                if (currentCallConversation != null)
                {
                    // Asignar la conversaci�n actual
                    callerNPC.conversation = currentCallConversation;

                    // Asignar versiones alternativas de la conversaci�n si es necesario
                    callerNPC.funnyConversation = currentCallConversation;

                    // Configurar opciones adicionales del NPC para que funcione con el sistema de di�logo
                    // Estas propiedades depender�n de la implementaci�n espec�fica de tu clase NPC
                    callerNPC.npcId = -999; // ID especial para NPCs de llamadas
                }

                // Programar la destrucci�n del NPC temporal despu�s de que termine la conversaci�n
                StartCoroutine(DestroyTemporaryNPC(tempNPC));
            }
            else
            {
                Debug.Log("CallSystem: Usando NPC existente para la llamada");
            }
        }
        else
        {
            Debug.Log("CallSystem: Usando PhoneNPC espec�fico para la llamada");
        }

        return callerNPC;
    }

    // Corrutina para destruir el NPC temporal despu�s de que termine la conversaci�n
    private IEnumerator DestroyTemporaryNPC(GameObject tempNPC)
    {
        // Esperar hasta que la llamada ya no est� activa
        yield return new WaitUntil(() => !isCallActive);

        // Esperar un poco m�s para asegurarse de que todas las referencias se han liberado
        yield return new WaitForSeconds(1f);

        // Destruir el NPC temporal
        if (tempNPC != null)
        {
            Destroy(tempNPC);
            Debug.Log("CallSystem: NPC temporal destruido");
        }
    }

    // Manejar el fin de la conversaci�n
    private void HandleConversationEnd()
    {
        if (isCallActive)
        {
            Debug.Log("CallSystem: Finalizando llamada tras terminar la conversaci�n");

            // Liberar al jugador si se bloque� durante la llamada
            Enabler enabler = Enabler.Instance;
            if (enabler != null)
            {
                enabler.ReleasePlayer();
            }

            // Actualizar el texto de estado antes de ocultar el popup
            if (callerDescriptionText != null)
            {
                callerDescriptionText.text = "Llamada finalizada";
            }

            // Ahora s� ocultamos el popup con la animaci�n correspondiente
            HideCallPopup();

            // Restablecer el estado del di�logo si es necesario
            if (DialogueManager.Instance != null && DialogueManager.Instance.dialogueUI != null &&
                DialogueManager.Instance.dialogueUI.activeSelf)
            {
                // Ya el DialogueManager se encarga de ocultar su interfaz, no necesitamos hacerlo aqu�
            }

            EndCall();
        }
    }

    // Finalizar llamada
    private void EndCall()
    {
        Debug.Log("CallSystem: Llamada finalizada");

        // Restablecer variables
        currentCallConversation = null;
        isCallActive = false;

        // Notificar
        OnCallStateChanged?.Invoke(false);
    }

    // Corrutina para manejar una secuencia de llamada
    private IEnumerator CallSequence(CallData callData)
    {
        // Esperar a que el usuario acepte o rechace la llamada
        while (isPopupVisible)
        {
            yield return null;
        }

        // Si la llamada est� activa, entonces fue aceptada
        if (isCallActive)
        {
            // Invocar evento de llamada aceptada
            callData.onCallAccepted?.Invoke();

            // Esperar a que termine la conversaci�n
            yield return new WaitUntil(() => !isCallActive);

            // Invocar evento de finalizaci�n
            callData.onCallFinished?.Invoke();
        }
        else
        {
            // La llamada fue rechazada
            callData.onCallRejected?.Invoke();
        }

        activeCallCoroutine = null;
    }

    // Temporizador para ocultar el popup autom�ticamente
    private IEnumerator PopupTimer()
    {
        yield return new WaitForSeconds(popupDuration);

        if (isPopupVisible)
        {
            // Si el popup sigue visible despu�s del tiempo, rechazar autom�ticamente
            RejectCall();
        }

        popupTimerCoroutine = null;
    }

    // Desactivar el panel despu�s de la animaci�n
    private IEnumerator DisablePopupAfterAnimation(string animName)
    {
        // Esperar a que termine la animaci�n
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
}
