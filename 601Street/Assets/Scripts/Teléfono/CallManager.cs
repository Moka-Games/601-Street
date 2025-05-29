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
    public List<ScheduledCall> scheduledCalls = new List<ScheduledCall>();

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

    private void OnDestroy()
    {
        // Limpiar todas las rutinas activas al destruir
        foreach (var routine in activeCallRoutines.Values)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }
        activeCallRoutines.Clear();
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
                Coroutine routine = StartCoroutine(ScheduleCall(call));
                activeCallRoutines[call.id] = routine;
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

        // Verificar si el CallSystem está disponible y no hay llamada activa
        if (CallSystem.Instance != null && !CallSystem.Instance.IsCallActive())
        {
            // Iniciar la llamada
            TriggerCall(call.id);
        }
        else
        {
            Debug.LogWarning($"CallManager: No se puede iniciar la llamada {call.id} - CallSystem no disponible o llamada ya activa");
        }

        // Remover de rutinas activas
        if (activeCallRoutines.ContainsKey(call.id))
        {
            activeCallRoutines.Remove(call.id);
        }
    }

    /// <summary>
    /// Activa una llamada programada por su ID.
    /// </summary>
    public void TriggerCall(string callId)
    {
        ScheduledCall call = scheduledCalls.Find(c => c.id == callId);
        if (call == null)
        {
            Debug.LogWarning($"CallManager: No se encontró una llamada con ID: {callId}");
            return;
        }

        // Si la llamada no es repetible y ya ha sido disparada, no hacer nada
        if (!call.repeatable && call.hasBeenTriggered)
        {
            Debug.Log($"CallManager: Llamada {callId} ya ha sido disparada y no es repetible.");
            return;
        }

        // Verificar que tengamos una conversación válida
        if (call.callConversation == null)
        {
            Debug.LogError($"CallManager: Error - La llamada con ID '{callId}' no tiene una conversación asignada");
            return;
        }

        // Verificar si el sistema de llamadas está disponible
        if (CallSystem.Instance == null)
        {
            Debug.LogError("CallManager: No se encontró CallSystem en la escena");
            return;
        }

        // Verificar si ya hay una llamada activa
        if (CallSystem.Instance.IsCallActive())
        {
            Debug.LogWarning($"CallManager: No se puede iniciar la llamada {callId} - ya hay una llamada activa");
            return;
        }

        // Marcar como disparada
        call.hasBeenTriggered = true;

        Debug.Log($"CallManager: Activando llamada '{callId}' con conversación: {call.callConversation.name}");

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
            Debug.LogWarning($"CallManager: Ya existe una llamada con ID: {newCall.id}");
            return;
        }

        scheduledCalls.Add(newCall);

        // Si la llamada no se dispara por evento, programarla
        if (!newCall.triggeredByEvent)
        {
            Coroutine routine = StartCoroutine(ScheduleCall(newCall));
            activeCallRoutines[newCall.id] = routine;
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
                // Cancelar rutina existente si existe
                CancelScheduledCall(callId);

                Coroutine routine = StartCoroutine(ScheduleCall(call));
                activeCallRoutines[call.id] = routine;
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
            if (routine != null)
            {
                StopCoroutine(routine);
                Debug.Log($"CallManager: Cancelada llamada programada: {callId}");
            }
            activeCallRoutines.Remove(callId);
        }
    }

    /// <summary>
    /// Cancela todas las llamadas programadas pendientes.
    /// </summary>
    public void CancelAllScheduledCalls()
    {
        foreach (var kvp in activeCallRoutines)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        activeCallRoutines.Clear();
        Debug.Log("CallManager: Todas las llamadas programadas han sido canceladas");
    }

    /// <summary>
    /// Verifica si una llamada específica ya ha sido disparada.
    /// </summary>
    public bool HasCallBeenTriggered(string callId)
    {
        ScheduledCall call = scheduledCalls.Find(c => c.id == callId);
        return call != null && call.hasBeenTriggered;
    }

    /// <summary>
    /// Verifica si hay una llamada programada pendiente.
    /// </summary>
    public bool IsCallScheduled(string callId)
    {
        return activeCallRoutines.ContainsKey(callId);
    }

    /// <summary>
    /// Realiza una llamada inmediata sin programación.
    /// </summary>
    public void MakeImmediateCall(string callerName, string callerDescription, Conversation conversation, Sprite avatar = null)
    {
        if (CallSystem.Instance == null)
        {
            Debug.LogError("CallManager: No se encontró CallSystem en la escena");
            return;
        }

        // Verificar si ya hay una llamada activa
        if (CallSystem.Instance.IsCallActive())
        {
            Debug.LogWarning("CallManager: No se puede realizar llamada inmediata - ya hay una llamada activa");
            return;
        }

        // Verificar que tengamos una conversación válida
        if (conversation == null)
        {
            Debug.LogError("CallManager: No se puede realizar llamada inmediata - conversación no válida");
            return;
        }

        Debug.Log($"CallManager: Realizando llamada inmediata a: {callerName}");

        // Crear datos de la llamada
        CallSystem.CallData callData = new CallSystem.CallData
        {
            callerName = callerName,
            callerDescription = callerDescription,
            callConversation = conversation,
            callerAvatar = avatar,
            onCallAccepted = new UnityEvent(),
            onCallRejected = new UnityEvent(),
            onCallFinished = new UnityEvent()
        };

        // Iniciar la llamada
        CallSystem.Instance.StartCall(callData);
    }

    /// <summary>
    /// Realiza una llamada inmediata con eventos personalizados.
    /// </summary>
    public void MakeImmediateCallWithEvents(string callerName, string callerDescription, Conversation conversation,
        Sprite avatar = null, UnityEvent onAccepted = null, UnityEvent onRejected = null, UnityEvent onFinished = null)
    {
        if (CallSystem.Instance == null)
        {
            Debug.LogError("CallManager: No se encontró CallSystem en la escena");
            return;
        }

        // Verificar si ya hay una llamada activa
        if (CallSystem.Instance.IsCallActive())
        {
            Debug.LogWarning("CallManager: No se puede realizar llamada inmediata - ya hay una llamada activa");
            return;
        }

        // Verificar que tengamos una conversación válida
        if (conversation == null)
        {
            Debug.LogError("CallManager: No se puede realizar llamada inmediata - conversación no válida");
            return;
        }

        Debug.Log($"CallManager: Realizando llamada inmediata con eventos a: {callerName}");

        // Crear datos de la llamada
        CallSystem.CallData callData = new CallSystem.CallData
        {
            callerName = callerName,
            callerDescription = callerDescription,
            callConversation = conversation,
            callerAvatar = avatar,
            onCallAccepted = onAccepted ?? new UnityEvent(),
            onCallRejected = onRejected ?? new UnityEvent(),
            onCallFinished = onFinished ?? new UnityEvent()
        };

        // Iniciar la llamada
        CallSystem.Instance.StartCall(callData);
    }

    /// <summary>
    /// Suscribe un callback al evento de llamada finalizada.
    /// </summary>
    public void SubscribeToCallFinishedEvent(string callId, UnityAction callback)
    {
        ScheduledCall call = scheduledCalls.Find(c => c.id == callId);
        if (call != null)
        {
            call.onCallFinished.AddListener(callback);
            Debug.Log($"CallManager: Suscrito al evento onCallFinished para la llamada {callId}");
        }
        else
        {
            Debug.LogWarning($"CallManager: No se encontró una llamada con ID: {callId}");
        }
    }

    /// <summary>
    /// Suscribe un callback al evento de llamada aceptada.
    /// </summary>
    public void SubscribeToCallAcceptedEvent(string callId, UnityAction callback)
    {
        ScheduledCall call = scheduledCalls.Find(c => c.id == callId);
        if (call != null)
        {
            call.onCallAccepted.AddListener(callback);
            Debug.Log($"CallManager: Suscrito al evento onCallAccepted para la llamada {callId}");
        }
        else
        {
            Debug.LogWarning($"CallManager: No se encontró una llamada con ID: {callId}");
        }
    }

    /// <summary>
    /// Suscribe un callback al evento de llamada rechazada.
    /// </summary>
    public void SubscribeToCallRejectedEvent(string callId, UnityAction callback)
    {
        ScheduledCall call = scheduledCalls.Find(c => c.id == callId);
        if (call != null)
        {
            call.onCallRejected.AddListener(callback);
            Debug.Log($"CallManager: Suscrito al evento onCallRejected para la llamada {callId}");
        }
        else
        {
            Debug.LogWarning($"CallManager: No se encontró una llamada con ID: {callId}");
        }
    }

    /// <summary>
    /// Obtiene información de una llamada programada por su ID.
    /// </summary>
    public ScheduledCall GetScheduledCall(string callId)
    {
        return scheduledCalls.Find(c => c.id == callId);
    }

    /// <summary>
    /// Fuerza la finalización de cualquier llamada activa.
    /// </summary>
    public void ForceEndActiveCall()
    {
        if (CallSystem.Instance != null && CallSystem.Instance.IsCallActive())
        {
            Debug.Log("CallManager: Forzando finalización de llamada activa");
            CallSystem.Instance.ForceEndCall();
        }
    }

    /// <summary>
    /// Obtiene el estado actual del sistema de llamadas.
    /// </summary>
    public bool IsCallSystemActive()
    {
        return CallSystem.Instance != null && CallSystem.Instance.IsCallActive();
    }
}