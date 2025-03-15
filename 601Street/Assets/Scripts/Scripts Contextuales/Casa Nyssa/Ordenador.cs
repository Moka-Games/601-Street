using UnityEngine;

public class Ordenador : MonoBehaviour
{
    private Pensamientos_Manager pensamientosManager;

    public string textoPensamiento;

    public static bool ordenadorInteractuado = false;
    private void Start()
    {
        pensamientosManager = FindAnyObjectByType<Pensamientos_Manager>();
    }

    public void PensamientoPostOrdenador()
    {
        pensamientosManager.MostrarPensamiento(textoPensamiento);
    }

    public void OrdenadorInteractuado()
    {
        ordenadorInteractuado = true;
    }
}
