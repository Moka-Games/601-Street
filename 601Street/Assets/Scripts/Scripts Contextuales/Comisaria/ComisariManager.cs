using UnityEngine;

public class ComisariManager : MonoBehaviour
{
    public GameObject ganzúa;
    public GameObject puertaCaja;

    public void ObjetosPostPolicia()
    {
        puertaCaja.SetActive(false);
    }

    public void ObjetosPostPoliciaFracaso()
    {
        ganzúa.SetActive(true);

    }
}
