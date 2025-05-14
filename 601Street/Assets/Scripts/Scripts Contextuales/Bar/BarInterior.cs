using UnityEngine;

public class BarInterior : MonoBehaviour
{

    public GameObject botella;
    public GameObject activadorPolicias;
    public GameObject salida;

    private void Start()
    {
        botella.SetActive(false);
        activadorPolicias.SetActive(false);
    }
    public void ActivarBotella()
    {
        botella.SetActive(true);
    }

    public void CambiarPolicias()
    {
        activadorPolicias.SetActive(true);
        salida.SetActive(true);
    }
}
