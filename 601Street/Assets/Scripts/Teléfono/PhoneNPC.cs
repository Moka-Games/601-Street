using UnityEngine;

/// <summary>
/// Clase espec�fica para NPCs que realizan llamadas telef�nicas.
/// Extiende de la clase NPC base para mantener compatibilidad con el sistema de di�logos.
/// </summary>
public class PhoneNPC : NPC
{
    [Header("Informaci�n de Llamada")]
    [SerializeField] private string callerDisplayName;
    [SerializeField] private Sprite callerAvatar;
    [SerializeField] private string callerDescription;

    // Getter para el nombre a mostrar en la llamada
    public string CallerDisplayName => string.IsNullOrEmpty(callerDisplayName) ? "Desconocido" : callerDisplayName;

    // Getter para la descripci�n a mostrar en la llamada
    public string CallerDescription => callerDescription;

    // Getter para el avatar a mostrar en la llamada
    public Sprite CallerAvatar => callerAvatar;

    // Esta clase no necesita componente de collider ya que no interact�a f�sicamente
    protected void Awake()
    {
        // Desactivar el componente de colisi�n si existe
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    /// <summary>
    /// Inicia una llamada desde este NPC.
    /// </summary>
    public void InitiateCall()
    {
        if (CallSystem.Instance == null)
        {
            Debug.LogError("No se encontr� el sistema de llamadas en la escena.");
            return;
        }

        // Crear los datos de la llamada
        CallSystem.CallData callData = new CallSystem.CallData
        {
            callerName = CallerDisplayName,
            callerDescription = CallerDescription,
            callConversation = conversation, // Usamos la conversaci�n base del NPC
            onCallAccepted = new UnityEngine.Events.UnityEvent(),
            onCallRejected = new UnityEngine.Events.UnityEvent(),
            onCallFinished = new UnityEngine.Events.UnityEvent()
        };

        // Iniciar la llamada
        CallSystem.Instance.StartCall(callData);
    }

    /// <summary>
    /// Inicia una llamada con una conversaci�n espec�fica.
    /// </summary>
    /// <param name="specificConversation">La conversaci�n a usar para esta llamada</param>
    public void InitiateCall(Conversation specificConversation)
    {
        if (CallSystem.Instance == null)
        {
            Debug.LogError("No se encontr� el sistema de llamadas en la escena.");
            return;
        }

        // Crear los datos de la llamada
        CallSystem.CallData callData = new CallSystem.CallData
        {
            callerName = CallerDisplayName,
            callerDescription = CallerDescription,
            callConversation = specificConversation,
            onCallAccepted = new UnityEngine.Events.UnityEvent(),
            onCallRejected = new UnityEngine.Events.UnityEvent(),
            onCallFinished = new UnityEngine.Events.UnityEvent()
        };

        // Iniciar la llamada
        CallSystem.Instance.StartCall(callData);
    }
}