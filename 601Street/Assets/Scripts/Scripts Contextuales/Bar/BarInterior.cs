using UnityEngine;

public class BarInterior : MonoBehaviour
{
    public static bool conversaci�nPoliciaTerminada = false;

    public GameObject botellas;

    private void Update()
    {
        if(conversaci�nPoliciaTerminada)
        {
            botellas.SetActive(true);
        }
        else
        {
            botellas.SetActive(false);
        }
    }

    public void Conversaci�nTerminada()
    {
        conversaci�nPoliciaTerminada = true;
    }
}
