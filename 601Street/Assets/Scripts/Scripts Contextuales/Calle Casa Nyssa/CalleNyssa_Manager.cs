using Unity.VisualScripting;
using UnityEngine;

public class CalleNyssa_Manager : MonoBehaviour
{
    public GameObject barrera_1;

    public GameObject teleportComisaria;

    public GameObject pensamiento;

    public GameObject abuela;

    public static bool policiaInteractuado = false;

    [Header("Portal")]
    public GameObject portalFeedback;
    public GameObject portalTriggerFinal;

    void Start()
    {
        if(Ordenador.ordenadorInteractuado)
        {
            barrera_1.SetActive(false);
            teleportComisaria.SetActive(true);
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

    private void Update()
    {
        if(Runa.runeInteracted)
        {
            portalFeedback.SetActive(false);
            portalTriggerFinal.SetActive(true);
        }
    }
}
