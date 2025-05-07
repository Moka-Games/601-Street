using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gestor de llamadas telefónicas para programar y controlar las llamadas en el juego.
/// </summary>
public class CallManager : MonoBehaviour
{
    // Singleton para acceso global
    public static CallManager Instance { get; private set; }

    [System.Serializable]
    public class ScheduledCall
    {
        public string id;
        public string callerName;
        public string callerDescription;
        [Tooltip("Conversación que se activará durante la llamada")]
        public Conversation callConversation;
        [Tooltip("Avatar personalizado para la llamada")]
        public Sprite callerAvatar; 
        public float delay;
        public bool triggeredByEvent;
        public bool repeatable = false; 
        public bool hasBeenTriggered = false; 
        public UnityEvent onCallAccepted;
        public UnityEvent onCallRejected;
        public UnityEvent onCallFinished;
    }

    [Header("Llamadas Programadas")]
    [SerializeField] private List<ScheduledCall> scheduledCalls = new List<ScheduledCall>();

    // Llamadas en progreso y pendientes
    private Dictionary<string, Coroutine> activeCallRoutines = new Dictionary<string, Coroutine>();
    private PhoneNPC defaultCallerNPC;

    private void Awake()
    {
        // Configuración del singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Buscar o crear un PhoneNPC por defecto
        defaultCallerNPC = FindAnyObjectByType<PhoneNPC>();
        if (defaultCallerNPC == null)
        {
            GameObject npcObj = new GameObject("DefaultPhoneNPC");
            defaultCallerNPC = npcObj.AddComponent<PhoneNPC>();
            npcObj.transform.parent = transform; // Hacerlo hijo de este objeto
        }

        // Iniciar llamadas programadas por tiempo
        StartScheduledCalls();
    }

    /// <summary>
    /// Inicia todas las llamadas programadas por tiempo.
    /// </summary>
    private void StartScheduledCalls()
    {
        foreach (var call in scheduledCalls)
        {
            if (!call.triggeredByEvent && !call.hasBeenTriggered)
            {
                StartCoroutine(ScheduleCall(call));
            }
        }
    }

    /// <summary>
    /// Corrutina para programar una llamada después de un retraso.
    /// </summary>
    private IEnumerator ScheduleCall(ScheduledCall call)
    {
        yield return new WaitForSeconds(call.delay);

        // Verificar si el juego no está en pausa o en otro estado incompatible
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsInGameplayState())
        {
            // Si no estamos en gameplay, esperar hasta que lo estemos
            yield return new WaitUntil(() => GameStateManager.Instance.IsInGameplayState());
        }

        // Iniciar la llamada
        TriggerCall(call.id);
    }

    /// <summary>
    /// Activa una llamada programada por su ID.
    /// </summary>
    public void TriggerCall(string callId)
    {
        ScheduledCall call = scheduledCalls.Find(c => c.id == callId);
        if (call == null)
        {
            Debug.LogWarning($"No se encontró una llamada con ID: {callId}");
            return;
        }

        // Si la llamada no es repetible y ya ha sido disparada, no hacer nada
        if (!call.repeatable && call.hasBeenTriggered)
        {
            Debug.Log($"Llamada {callId} ya ha sido disparada y no es repetible.");
            return;
        }

        // Marcar como disparada
        call.hasBeenTriggered = true;

        // Verificar que tengamos una conversación válida
        if (call.callConversation == null)
        {
            Debug.LogError($"Error: La llamada con ID '{callId}' no tiene una conversación asignada");
            return;
        }

        // Verificar si el sistema de llamadas está disponible
        if (CallSystem.Instance == null)
        {
            Debug.LogError("No se encontró CallSystem en la escena");
            return;
        }

        Debug.Log($"Activando llamada '{callId}' con conversación: {call.callConversation.name}");

        // Crear datos de la llamada
        CallSystem.CallData callData = new CallSystem.CallData
        {
            callerName = call.callerName,
            callerDescription = call.callerDescription,
            callConversation = call.callConversation,
            callerAvatar = call.callerAvatar,
            onCallAccepted = call.onCallAccepted,
            onCallRejected = call.onCallRejected,
            onCallFinished = call.onCallFinished
        };

        // Iniciar la llamada
        CallSystem.Instance.StartCall(callData);
    }
    /// <summary>
    /// Añade una nueva llamada programada en tiempo de ejecución.
    /// </summary>
    public void AddScheduledCall(ScheduledCall newCall)
    {
        // Verificar que no exista ya una llamada con el mismo ID
        if (scheduledCalls.Exists(c => c.id == newCall.id))
        {
            Debug.LogWarning($"Ya existe una llamada con ID: {newCall.id}");
            return;
        }

        scheduledCalls.Add(newCall);

        // Si la llamada no se dispara por evento, programarla
        if (!newCall.triggeredByEvent)
        {
            StartCoroutine(ScheduleCall(newCall));
        }
    }

    /// <summary>
    /// Restaura una llamada para que pueda volver a ocurrir.
    /// </summary>
    public void ResetCall(string callId)
    {
        ScheduledCall call = scheduledCalls.Find(c => c.id == callId);
        if (call != null)
        {
            call.hasBeenTriggered = false;

            // Si la llamada se dispara por tiempo, programarla de nuevo
            if (!call.triggeredByEvent)
            {
                StartCoroutine(ScheduleCall(call));
            }
        }
    }

    /// <summary>
    /// Cancela una llamada programada si está pendiente.
    /// </summary>
    public void CancelScheduledCall(string callId)
    {
        if (activeCallRoutines.TryGetValue(callId, out Coroutine routine))
        {
            StopCoroutine(routine);
            activeCallRoutines.Remove(callId);
        }
    }

    /// <summary>
    /// Verifica si una llamada específica ya ha sido disparada.
    /// </summary>
    public bool HasCallBeenTriggered(string callId)
    {
        ScheduledCall call = scheduledCalls.Find(c => c.id == callId);
        return call != null && call.hasBeenTriggered;
    }
    public void MakeImmediateCall(string callerName, string callerDescription, Conversation conversation, Sprite avatar = null)
    {
        if (CallSystem.Instance == null)
        {
            Debug.LogError("No se encontró CallSystem en la escena");
            return;
        }

        // Crear datos de la llamada
        CallSystem.CallData callData = new CallSystem.CallData
        {
            callerName = callerName,
            callerDescription = callerDescription,
            callConversation = conversation,
            callerAvatar = avatar, // Incluir el avatar
            onCallAccepted = new UnityEvent(),
            onCallRejected = new UnityEvent(),
            onCallFinished = new UnityEvent()
        };

        // Iniciar la llamada
        CallSystem.Instance.StartCall(callData);
    }
}