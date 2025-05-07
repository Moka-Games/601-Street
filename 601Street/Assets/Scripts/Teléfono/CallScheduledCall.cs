using UnityEngine;

public class CallScheduledCall : MonoBehaviour
{
    [SerializeField] private string callID;
    public bool destroyAfterTrigger = false;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            TriggerScheduledCall();
        }
    }


    private void TriggerScheduledCall()
    {
        CallManager.Instance.TriggerCall(callID);

        if (destroyAfterTrigger)
        {
            Destroy(gameObject);
        }
    }
}
