using UnityEngine;

public class MisionTrigger : MonoBehaviour
{
    public string idMision;

    public bool terminarMisi�n;

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
                MisionManager.Instance.IniciarMision(idMision);
            }

            Destroy(gameObject);
        }
    }

}