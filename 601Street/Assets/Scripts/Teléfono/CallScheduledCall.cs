using System.Collections;
using UnityEngine;

public class CallScheduledCall : MonoBehaviour
{
    [SerializeField] private string callID;
    [SerializeField] private float callDelay;
    public bool destroyAfterTrigger = false;

    public bool callOnTrigger;
    public bool callOnStart;


    private void Start()
    {
        if (callOnStart)
        {
            StartCoroutine(TriggerScheduledCall());

        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && callOnTrigger)
        {
            StartCoroutine(TriggerScheduledCall());
        }
    }

    IEnumerator TriggerScheduledCall()
    {
        yield return new WaitForSeconds(callDelay);

        CallManager.Instance.TriggerCall(callID);

        if (destroyAfterTrigger)
        {
            Destroy(gameObject);
        }
    }
}
