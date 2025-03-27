using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Componente para activar llamadas en momentos específicos (zonas, eventos, etc.).
/// </summary>
public class CallTrigger : MonoBehaviour
{
    [Header("Configuración de la Llamada")]
    [SerializeField] private string callID;
    [Tooltip("Si está marcado, la llamada se activará cuando el jugador entre en el trigger")]
    [SerializeField] private bool triggerOnEnter = true;
    [Tooltip("Si está marcado, este trigger se destruirá después de activar la llamada")]
    [SerializeField] private bool destroyAfterTrigger = true;
    [Tooltip("Si está marcado, la llamada solo se activará una vez")]
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Llamada Inmediata")]
    [Tooltip("Si está marcado, se creará una llamada inmediata con los siguientes parámetros")]
    [SerializeField] private bool useImmediateCall = false;
    [SerializeField] private string callerName = "Desconocido";
    [SerializeField] private string callerDescription = "Número desconocido";
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
                Debug.LogWarning("No se pudo realizar la llamada inmediata: falta el CallManager o la conversación");
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

        // Destruir el trigger si está configurado para ello
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