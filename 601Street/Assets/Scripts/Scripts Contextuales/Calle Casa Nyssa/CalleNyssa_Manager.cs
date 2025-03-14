using Unity.VisualScripting;
using UnityEngine;

public class CalleNyssa_Manager : MonoBehaviour
{
    public GameObject barrera_1;

    public GameObject teleportComisaria;

    public GameObject puerta;
    public GameObject pensamiento;

    public GameObject abuela;

    public static bool policiaInteractuado = false;

    void Start()
    {
        if(ManagerHabitación.segundoDia)
        {
            barrera_1.SetActive(false);
            teleportComisaria.SetActive(true);
            puerta.SetActive(false);
            pensamiento.SetActive(true);        
        }
        else 
        {
            barrera_1.SetActive(true);
            teleportComisaria.SetActive(false);
            pensamiento.SetActive(false);
        }

        if(policiaInteractuado)
        {
            abuela.SetActive(true);
        }
        else
        {
            abuela.SetActive(false);
        }
    }

}
