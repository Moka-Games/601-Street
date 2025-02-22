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

    public void StartGame()
    {
        gameStarted = true;
        StartCoroutine(InitialCameraFreeze(2.05f));
        fadeManager.BlackScreenIntoFadeOut(2f);
    }

    public void EndGame()
    {
        gameStarted = false;
    }

   IEnumerator InitialCameraFreeze(float duration)
    {
        yield return new WaitForSeconds(0.2f);

        camera_Reference.FreezeCamera();

        yield return new WaitForSeconds(duration);

        camera_Reference.UnfreezeCamera();
    }
}
