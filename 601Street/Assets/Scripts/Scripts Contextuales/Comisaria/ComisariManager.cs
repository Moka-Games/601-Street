using UnityEngine;

public class ComisariManager : MonoBehaviour
{
    public GameObject policia1;
    public GameObject policia2;

    public GameObject ganzúa;
    public GameObject puertaCaja;
    public GameObject contraseña;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        print(BarInterior.conversaciónPoliciaTerminada + "aaaaaa");
        if(BarInterior.conversaciónPoliciaTerminada == true)
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
        contraseña.SetActive(true);
        puertaCaja.SetActive(false);
    }

    public void ObjetosPostPoliciaFracaso()
    {
        contraseña.SetActive(true);
        ganzúa.SetActive(true);

    }

}
