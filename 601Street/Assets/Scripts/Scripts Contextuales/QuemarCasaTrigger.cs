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
    }

    public void NoQuemarCasa()
    {
        panelNegro.SetActive(false);
    }
}
