using UnityEngine;

public class CalleBar_Manager : MonoBehaviour
{
    public GameObject puertaCerrada;
    public GameObject puertaAbierta;

    public static bool puertaAbiertaBool = false;
    private void Start()
    {
        print(puertaAbiertaBool);
        if(!puertaAbiertaBool)
        {
            puertaCerrada.SetActive(true);
            puertaAbierta.SetActive(false);
        }
        else if (puertaAbiertaBool)
        {
            puertaCerrada.SetActive(false);
            puertaAbierta.SetActive(true);
        }
        
    }
}
