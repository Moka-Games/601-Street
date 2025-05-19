using UnityEngine;

public class ComisariManager : MonoBehaviour
{
    public GameObject ganzúa;
    public GameObject puertaCaja;
    public GameObject contraseña;

    public GameObject activadorPuertaBar;

    public GameObject llamadaDaichi_Activator;
    void Start()
    {
        llamadaDaichi_Activator.SetActive(false);
    }

    public void InteracciónPolicia()
    {
        activadorPuertaBar.SetActive(true);
    }

    public void ObjetosPostPolicia()
    {
        contraseña.SetActive(true);
        puertaCaja.SetActive(false);
    }

    public void ObjetosPostPoliciaFracaso()
    {
        contraseña.SetActive(true);
        ganzúa.SetActive(true);

    }


    public void ActivarLlamadaDaichi()
    {
        llamadaDaichi_Activator.SetActive(true);
    }
}
