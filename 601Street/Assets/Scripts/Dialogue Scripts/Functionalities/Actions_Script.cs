using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Actions_Script : MonoBehaviour
{
    public static Actions_Script Instance { get; private set; }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void Option1_Action1()
    {
        print("1111");
    }
    public void Option2_Action2()
    {
        print("1111");

    }
    public void Option3_Action3()
    {
        print("3333");

    }

    public void ActivarObjetosPolicia2()
    {
        ComisariManager comisariaManager = FindAnyObjectByType<ComisariManager>();
        comisariaManager.ObjetosPostPolicia();
    }

    public void ActivarObjetosPolicia2Fail()
    {
        ComisariManager comisariaManager = FindAnyObjectByType<ComisariManager>();
        comisariaManager.ObjetosPostPoliciaFracaso();
    }

    public void MostrarPensamiento(string pensamiento)
    {
        Pensamientos_Manager pensamientosManager = FindAnyObjectByType<Pensamientos_Manager>();
        pensamientosManager.MostrarPensamiento(pensamiento);
    }

    public void PoliciaInteractuado()
    {
        CalleBar_Manager.puertaAbiertaBool = true;
        CalleNyssa_Manager.policiaInteractuado = true;
    }


    public void PoliciaSucess()
    {
        ComisariManager comisariaManager = FindAnyObjectByType<ComisariManager>();

        comisariaManager.ObjetosPostPolicia();
    }

    public void PoliciaFail()
    {
        ComisariManager comisariaManager = FindAnyObjectByType<ComisariManager>();

        comisariaManager.ObjetosPostPoliciaFracaso();
    }

    public void ActivarLlamadaDaichiPostComisaria()
    {
        ComisariManager comisariaManager = FindAnyObjectByType<ComisariManager>();
        comisariaManager.ActivarLlamadaDaichi();
    }

    public void QuemarCasaCall()
    {
        QuemarCasaTrigger quemarCasaTrigger = FindAnyObjectByType<QuemarCasaTrigger>();
        quemarCasaTrigger.QuemarCasa();
    }

    public void NoQuemarCasaCall()
    {
        QuemarCasaTrigger quemarCasaTrigger = FindAnyObjectByType<QuemarCasaTrigger>();
        quemarCasaTrigger.NoQuemarCasa();
    }
}
