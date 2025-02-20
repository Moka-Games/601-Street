using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("GameSceneManager no está inicializado!");
            }
            return instance;
        }
    }

    private Camera_Script camera_Reference;
    private FadeManager fadeManager;


    public bool gameStarted = false;
    public float delayToStart;
    private void Start()
    {
        camera_Reference = FindAnyObjectByType<Camera_Script>();
        fadeManager = FindAnyObjectByType<FadeManager>();

        StartGame();
    }

    IEnumerator ActivateCameraWithDelay(float delay)
    {
        camera_Reference.FreezeCamera();

        yield return new WaitForSeconds(delay);

        camera_Reference.UnfreezeCamera();
    }

    

    public void StartGame()
    {
        gameStarted = true;
        StartCoroutine(ActivateCameraWithDelay(2.5f));
        fadeManager.BlackScreenIntoFadeOut(2f);
    }

    public void EndGame()
    {
        gameStarted = false;
    }

}
