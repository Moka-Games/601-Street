using UnityEngine;

public class QuemarCasaTrigger : MonoBehaviour
{
    public GameObject panelNegro;
    public GameObject CámaraSecuencia;
    public GameObject VFXFuego;
    public void QuemarCasa()
    {
        panelNegro.SetActive(true);
        CámaraSecuencia.SetActive(true);
        VFXFuego.SetActive(true);
        print("Quemando la casa");
    }

    public void NoQuemarCasa()
    {
        print("No quemando la casa");
        panelNegro.SetActive(false);
        CámaraSecuencia.SetActive(false);
        VFXFuego.SetActive(false);
    }
}
