using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    public string SceneName;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("SceneChange"))
        {
            SceneManager.LoadScene(SceneName);
        }
    }
}
