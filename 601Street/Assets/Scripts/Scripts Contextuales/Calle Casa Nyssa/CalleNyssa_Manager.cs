using UnityEngine;

public class CalleNyssa_Manager : MonoBehaviour
{
    public GameObject abuela;
    public GameObject abuela_Eco;

    public static bool policiaInteractuado = false;


    void Start()
    {
        if(policiaInteractuado)
        {
            abuela.SetActive(true);
            abuela_Eco.SetActive(false);
        }
        else
        {
            abuela.SetActive(false);
        }
    }

}
