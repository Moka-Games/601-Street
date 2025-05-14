using UnityEngine;

public class ComisariManager : MonoBehaviour
{
    public GameObject ganzúa;
    public GameObject puertaCaja;
    public GameObject contraseña;

    private Pensamientos_Manager pensamientosManager;

    public string pensamientoPostInteracción = "";

    public GameObject llamadaDaichi_Activator;
    void Start()
    {
        pensamientosManager = FindAnyObjectByType<Pensamientos_Manager>();
        llamadaDaichi_Activator.SetActive(false);
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
        pensamientosManager.MostrarPensamiento(pensamientoPostInteracción);

    }


    public void ActivarLlamadaDaichi()
    {
        llamadaDaichi_Activator.SetActive(true);
    }
}
