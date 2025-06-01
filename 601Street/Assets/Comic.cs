using UnityEngine;

public class Comic : MonoBehaviour
{
    private AudioSource audioSource;
    private PersistentDestructible persistentDestructible;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        persistentDestructible = GetComponent<PersistentDestructible>();
    }


    private void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            Time.timeScale = 10f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }
    public void StopMusic()
    {
        audioSource.Stop();
        persistentDestructible.DestroyPersistently();
    }
}
