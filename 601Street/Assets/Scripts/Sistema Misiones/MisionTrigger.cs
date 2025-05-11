using System.Collections;
using UnityEngine;

public class MisionTrigger : MonoBehaviour
{
    public string idMision;

    public bool terminarMisi�n;

    public float startDelay;

    private void OnTriggerEnter(Collider other) 
    {
        if (other.CompareTag("Player"))
        {
            if(terminarMisi�n)
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