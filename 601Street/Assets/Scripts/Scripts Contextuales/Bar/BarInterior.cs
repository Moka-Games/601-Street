using UnityEngine;

public class BarInterior : MonoBehaviour
{
    public static bool conversaciónPoliciaTerminada = false;

    public GameObject botellas;

    private void Update()
    {
        if(conversaciónPoliciaTerminada)
        {
            botellas.SetActive(true);
        }
        else
        {
            botellas.SetActive(false);
        }
    }

    public void ConversaciónTerminada()
    {
        conversaciónPoliciaTerminada = true;
    }
}
