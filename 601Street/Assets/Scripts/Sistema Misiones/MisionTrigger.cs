using System.Collections;
using UnityEngine;

public class MisionTrigger : MonoBehaviour
{
    public string idMision;

    public bool terminarMisión;

    public float startDelay;

    private void OnTriggerEnter(Collider other) 
    {
        if (other.CompareTag("Player"))
        {
            if(terminarMisión)
            {
                MisionManager.Instance.CompletarMisionActual();

            }
            else
            {
                StartCoroutine(StartMision());
            }

            Destroy(gameObject);
        }
    }
    
    IEnumerator StartMision()
    {
        yield return new WaitForSeconds(startDelay);
        MisionManager.Instance.IniciarMision(idMision);
    }
}