using UnityEngine;

public class ComisariManager : MonoBehaviour
{
    public GameObject ganz�a;
    public GameObject puertaCaja;

    public void ObjetosPostPolicia()
    {
        puertaCaja.SetActive(false);
    }

    public void ObjetosPostPoliciaFracaso()
    {
        ganz�a.SetActive(true);

    }
}
