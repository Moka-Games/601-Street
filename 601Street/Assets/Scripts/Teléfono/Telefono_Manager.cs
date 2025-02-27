using UnityEngine;
public class Telefono_Manager : MonoBehaviour
{
    public GameObject Telefono_HUD; //Cierra el componente Teléfono HUD
    public GameObject Telefono_UI; //Cierra el componente Teléfono UI 
    public GameObject Apps;
    [Header("Interfaces")]
    public GameObject Ecos;
    public GameObject Contactos;
    public GameObject Secta;
    // Padre de las interfaces
    // Se declara para poder desactivar todas las interfaces al mismo tiempo
    public GameObject Interfaces;
    [Header("Referencias Acciones Individuales")]
    private Pensamientos_Manager pensamientos_Manager;

    private void Start()
    {
        pensamientos_Manager = FindAnyObjectByType<Pensamientos_Manager>();
        Apps.SetActive(true);
    }

    public void OpenApps()
    {
        Apps.SetActive(true);
        foreach (Transform hijo in Interfaces.transform)
        {
            hijo.gameObject.SetActive(false);
        }
    }

    public void ClosePhone()
    {
        Telefono_UI.SetActive(false);
        Telefono_HUD.SetActive(true);
    }

    public void MostrarPensamientoDeseado()
    {
        Telefono_UI.SetActive(false);
        Telefono_HUD.SetActive(true);

        /*Pensamiento pensamientoID1 = pensamientos_Manager.GetPensamientoByID(1);
        if (pensamientoID1 != null)
        {
            pensamientos_Manager.MostrarPensamiento(pensamientoID1.pensamientoPrincipal);
        }
        else
        {
            Debug.LogWarning("No se encontró el pensamiento con ID 1");
        }*/
    }
}