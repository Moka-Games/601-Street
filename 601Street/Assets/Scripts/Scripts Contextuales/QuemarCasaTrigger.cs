using UnityEngine;

public class QuemarCasaTrigger : MonoBehaviour
{
    public GameObject panelNegro;
    public GameObject C�maraSecuencia;
    public GameObject VFXFuego;
    public void QuemarCasa()
    {
        panelNegro.SetActive(true);
        C�maraSecuencia.SetActive(true);
        VFXFuego.SetActive(true);
    }

    public void NoQuemarCasa()
    {
        panelNegro.SetActive(false);
    }
}
