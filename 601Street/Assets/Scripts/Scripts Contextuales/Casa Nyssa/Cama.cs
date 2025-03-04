using UnityEngine;
using UnityEngine.SceneManagement;

public class Cama : MonoBehaviour
{
    private GameSceneManager gameSceneManager;

    public string SceneToLoad;
    public void CamaFunction()
    {
        gameSceneManager = FindAnyObjectByType<GameSceneManager>();

        gameSceneManager.LoadScene(SceneToLoad);
    }

}
