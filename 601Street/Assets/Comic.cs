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

    public void StopMusic()
    {
        audioSource.Stop();
        persistentDestructible.DestroyPersistently();
    }
}
