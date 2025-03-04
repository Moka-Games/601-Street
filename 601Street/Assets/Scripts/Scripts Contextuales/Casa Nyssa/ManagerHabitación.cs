using Unity.VisualScripting;
using UnityEngine;

public class ManagerHabitación : MonoBehaviour
{
    public static bool primerDia;
    public static bool segundoDia;

    public GameObject globalLightInicial;
    public GameObject globalLightSegundoDia;

    private Pensamientos_Manager pensamientosManager;
    public InteractableObject interactableObject;

    public string pensamientoSegundaEscena;

    private void Start()
    {
        if(primerDia)
        {
            primerDia = true;
            segundoDia = false;

            globalLightInicial.SetActive(true);
        }
        if(segundoDia)
        {
            SegundoDia();
        }

    }

    public void SegundoDia()
    {
        pensamientosManager = FindAnyObjectByType<Pensamientos_Manager>();

        pensamientosManager.MostrarPensamiento(pensamientoSegundaEscena);

        interactableObject.enabled = false;

        globalLightInicial.SetActive(false);
        globalLightSegundoDia.SetActive(true);
    }

    public void AcabarDia()
    {
        primerDia = false;
        segundoDia = true;
    }

}
