using UnityEngine;

public class Ordenador : MonoBehaviour
{
    private Pensamientos_Manager pensamientosManager;

    public string textoPensamiento;
    private void Start()
    {
        pensamientosManager = FindAnyObjectByType<Pensamientos_Manager>();
    }

    public void PensamientoPostOrdenador()
    {
        pensamientosManager.MostrarPensamiento(textoPensamiento);
    }
}
