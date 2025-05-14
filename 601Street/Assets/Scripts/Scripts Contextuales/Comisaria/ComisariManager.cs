using UnityEngine;

public class ComisariManager : MonoBehaviour
{
    public GameObject ganz�a;
    public GameObject puertaCaja;
    public GameObject contrase�a;

    private Pensamientos_Manager pensamientosManager;

    public string pensamientoPostInteracci�n = "";

    public GameObject llamadaDaichi_Activator;
    void Start()
    {
        pensamientosManager = FindAnyObjectByType<Pensamientos_Manager>();
        llamadaDaichi_Activator.SetActive(false);
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
        pensamientosManager.MostrarPensamiento(pensamientoPostInteracci�n);

    }


    public void ActivarLlamadaDaichi()
    {
        llamadaDaichi_Activator.SetActive(true);
    }
}
