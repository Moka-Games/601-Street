using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CallScheduledCall : MonoBehaviour
{
    [SerializeField] private string callID;
    [SerializeField] private float callDelay;
    //public bool destroyAfterTrigger = false;

    public bool callOnTrigger;
    public bool callOnStart;

    [Header("Eventos")]
    [Tooltip("Este evento se dispara cuando la llamada ha finalizado")]
    public UnityEvent OnEndedCall;

    private void Start()
    {
        if (callOnStart)
        {
            StartCoroutine(TriggerScheduledCall());
        }

        // Suscribirse al evento de fin de llamada
        if (CallManager.Instance != null)
        {
            CallManager.Instance.SubscribeToCallFinishedEvent(callID, OnCallFinished);
        }
        else
        {
            Debug.LogWarning("CallScheduledCall: No se encontró CallManager en la escena");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && callOnTrigger)
        {
            StartCoroutine(TriggerScheduledCall());
        }
    }

    IEnumerator TriggerScheduledCall()
    {
        yield return new WaitForSeconds(callDelay);

        if (CallManager.Instance != null)
        {
            CallManager.Instance.TriggerCall(callID);
        }
        else
        {
            Debug.LogError("CallScheduledCall: No se puede activar la llamada, CallManager no encontrado");
        }

        /*if (destroyAfterTrigger)
        {
            Destroy(gameObject);
        }*/
    }

    // Método que se llamará cuando termine la llamada
    private void OnCallFinished()
    {
        Debug.Log($"CallScheduledCall: Llamada {callID} finalizada, invocando OnEndedCall");
        OnEndedCall?.Invoke();
    }
}