using UnityEngine;

public class Puerta : MonoBehaviour
{
    private Pensamientos_Manager pensamientosManager;

    public string pensamientoPuerta_Txt;

    public GameObject puertaCanvas;

    private void Start()
    {
        ///No tiene que ver con la lógica pero se utiliza
        ///este script para desactivar el objeto desde un
        ///inicio
       
        puertaCanvas.SetActive(false);
        pensamientosManager = FindAnyObjectByType<Pensamientos_Manager>();
    }

    public void PensamientoPuerta()
    {
        pensamientosManager.MostrarPensamiento(pensamientoPuerta_Txt);
    }
}
