using Unity.VisualScripting;
using UnityEngine;

public class CalleNyssa_Manager : MonoBehaviour
{
    public GameObject barrera_1;

    public GameObject teleportComisaria;

    public GameObject runa;
    public GameObject puerta;
    public GameObject pensamiento;

    void Start()
    {
        if(ManagerHabitación.segundoDia)
        {
            barrera_1.SetActive(false);
            teleportComisaria.SetActive(true);
            puerta.SetActive(false);
            pensamiento.SetActive(true);

            runa.SetActive(true);
            if(!Runa.runeInteracted)
            {
                runa.SetActive(true);
            }
            else
            {
                runa.SetActive(false);
            }
         
        }
        else 
        {
            barrera_1.SetActive(true);
            teleportComisaria.SetActive(false);
            runa.SetActive(false);
            pensamiento.SetActive(false);
        }
    }

}
