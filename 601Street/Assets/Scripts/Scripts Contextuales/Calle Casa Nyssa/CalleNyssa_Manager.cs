using UnityEngine;

public class CalleNyssa_Manager : MonoBehaviour
{
    public GameObject barrera_1;

    public GameObject teleportComisaria;

    public GameObject runa;
    public InteractableObject puerta;
    void Start()
    {
        if(ManagerHabitación.segundoDia)
        {

            barrera_1.SetActive(false);
            teleportComisaria.SetActive(true);
            runa.SetActive(true);
            puerta.enabled = false;
        }
        else 
        {
            barrera_1.SetActive(true);
            teleportComisaria.SetActive(false);
            runa.SetActive(false);
        }
    }

}
