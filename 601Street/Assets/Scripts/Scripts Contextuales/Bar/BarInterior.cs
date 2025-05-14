using UnityEngine;

public class BarInterior : MonoBehaviour
{

    public GameObject botella;
    public GameObject activadorPolicias;
    public GameObject salida;
    public GameObject c�digoCajaFuerte;

    private void Start()
    {
        botella.SetActive(false);
        activadorPolicias.SetActive(false);
        c�digoCajaFuerte.SetActive(false);
    }
    public void ActivarBotella()
    {
        botella.SetActive(true);
    }

    public void CambiarPolicias()
    {
        activadorPolicias.SetActive(true);
        salida.SetActive(true);
        c�digoCajaFuerte.SetActive(true);
    }
}
