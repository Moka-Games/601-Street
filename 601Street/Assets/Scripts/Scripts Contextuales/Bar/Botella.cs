using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class Botella : MonoBehaviour
{
    public static bool objectInteracted = false;

    public void OnInteract()
    {
        objectInteracted = true;
        Debug.Log("Objeto interactuado correctamente");

        // Opcional: mostrar un pensamiento al jugador
        Pensamientos_Manager pensamientosManager = FindAnyObjectByType<Pensamientos_Manager>();
        if (pensamientosManager != null)
        {
            pensamientosManager.MostrarPensamiento("I should talk to Nakamura again.");
        }
    }
}
