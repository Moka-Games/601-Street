using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cargar escenas

public class Portal : MonoBehaviour
{
    public void VolverMen�Principal()
    {
        PauseMenu pauseMenu = FindAnyObjectByType<PauseMenu>();
        pauseMenu.BackToMainMenu();

    }
}