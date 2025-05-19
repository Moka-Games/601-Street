using UnityEngine;

public class ComisariManager : MonoBehaviour
{
    public GameObject ganz�a;
    public GameObject puertaCaja;
    public GameObject contrase�a;

    public GameObject activadorPuertaBar;

    public GameObject llamadaDaichi_Activator;
    void Start()
    {
        llamadaDaichi_Activator.SetActive(false);
    }

    public void Interacci�nPolicia()
    {
        activadorPuertaBar.SetActive(true);
    }

    public void ObjetosPostPolicia()
    {
        contrase�a.SetActive(true);
        puertaCaja.SetActive(false);
    }

    public void ObjetosPostPoliciaFracaso()
    {
        contrase�a.SetActive(true);
        ganz�a.SetActive(true);

    }


    public void ActivarLlamadaDaichi()
    {
        llamadaDaichi_Activator.SetActive(true);
    }
}
