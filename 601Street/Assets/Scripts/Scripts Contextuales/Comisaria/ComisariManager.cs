using UnityEngine;

public class ComisariManager : MonoBehaviour
{
    public GameObject policia1;
    public GameObject policia2;

    public GameObject ganz�a;
    public GameObject puertaCaja;
    public GameObject contrase�a;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        print(BarInterior.conversaci�nPoliciaTerminada + "aaaaaa");
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

    }

}
