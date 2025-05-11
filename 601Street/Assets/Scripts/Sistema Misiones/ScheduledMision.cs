using System.Collections;
using UnityEngine;

public class ScheduledMision : MonoBehaviour
{
    [SerializeField] private string idMision;

    [SerializeField] private float startDelay;

    private static bool misionEnabled = false;
    void Start()
    {
        misionEnabled = false;
        StartCoroutine(StartMision());
    }

    IEnumerator StartMision()
    {
        yield return new WaitForSeconds(startDelay);
        MisionManager.Instance.IniciarMision(idMision);
        misionEnabled = true;
    }
}
