using UnityEngine;

public class ComisariManager : MonoBehaviour
{
    public GameObject policia1;
    public GameObject policia2;

    public GameObject ganz�a;
    public GameObject puertaCaja;
    public GameObject contrase�a;

    private Pensamientos_Manager pensamientosManager;

    public string pensamientoPostInteracci�n = "";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pensamientosManager = FindAnyObjectByType<Pensamientos_Manager>();

        print("Conversaci�n con policia terminada = " + BarInterior.conversaci�nPoliciaTerminada);
        if(BarInterior.conversaci�nPoliciaTerminada == true)
        {
            policia2.SetActive(true);
            policia1.SetActive(false);
        }
        else
        {
            policia1.SetActive(true);
            policia2.SetActive(false);
        }
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

}
