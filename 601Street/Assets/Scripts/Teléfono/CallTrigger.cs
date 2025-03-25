using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Componente para activar llamadas en momentos espec�ficos (zonas, eventos, etc.).
/// </summary>
public class CallTrigger : MonoBehaviour
{
    [Header("Configuraci�n de la Llamada")]
    [SerializeField] private string callID;
    [Tooltip("Si est� marcado, la llamada se activar� cuando el jugador entre en el trigger")]
    [SerializeField] private bool triggerOnEnter = true;
    [Tooltip("Si est� marcado, este trigger se destruir� despu�s de activar la llamada")]
    [SerializeField] private bool destroyAfterTrigger = true;
    [Tooltip("Si est� marcado, la llamada solo se activar� una vez")]
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Llamada Inmediata")]
    [Tooltip("Si est� marcado, se crear� una llamada inmediata con los siguientes par�metros")]
    [SerializeField] private bool useImmediateCall = false;
    [SerializeField] private string callerName = "Desconocido";
    [SerializeField] private string callerDescription = "N�mero desconocido";
    [SerializeField] private Conversation callConversation;

    [Header("Eventos")]
    [SerializeField] private UnityEvent onTriggerActivated;

    private bool hasBeenTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter || !other.CompareTag("Player"))
            return;

        TriggerCall();
    }

    /// <summary>
    /// Activa la llamada configurada.
    /// </summary>
    public void TriggerCall()
    {
        // Si ya se ha activado y solo debe activarse una vez, salir
        if (hasBeenTriggered && triggerOnlyOnce)
            return;

        // Marcar como activado
        hasBeenTriggered = true;

        // Invocar evento
        onTriggerActivated?.Invoke();

        // Activar la llamada
        if (useImmediateCall)
        {
            if (CallManager.Instance != null && callConversation != null)
            {
                CallManager.Instance.MakeImmediateCall(callerName, callerDescription, callConversation);
            }
            else
            {
                Debug.LogWarning("No se pudo realizar la llamada inmediata: falta el CallManager o la conversaci�n");
            }
        }
        else
        {
            if (CallManager.Instance != null && !string.IsNullOrEmpty(callID))
            {
                CallManager.Instance.TriggerCall(callID);
            }
            else
            {
                Debug.LogWarning("No se pudo activar la llamada: falta el CallManager o el ID de llamada");
            }
        }

        // Destruir el trigger si est� configurado para ello
        if (destroyAfterTrigger)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Reinicia el estado del trigger para que pueda volver a activarse.
    /// </summary>
    public void ResetTrigger()
    {
        hasBeenTriggered = false;
    }
}