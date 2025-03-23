using UnityEngine;

public class MisionTrigger : MonoBehaviour
{
    public string idMision;

    public bool terminarMisión;

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
                MisionManager.Instance.IniciarMision(idMision);
            }

            Destroy(gameObject);
        }
    }

}