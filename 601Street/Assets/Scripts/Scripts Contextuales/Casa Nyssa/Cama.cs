using UnityEngine;

public class Cama : MonoBehaviour
{
    private GameSceneManager gameSceneManager;

    public string SceneToLoad;

    private void Start()
    {
        gameSceneManager = FindAnyObjectByType<GameSceneManager>();
    }

    public void CamaFunction()
    {
        gameSceneManager.LoadScene(SceneToLoad);
    }

}
