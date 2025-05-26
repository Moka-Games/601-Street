using UnityEngine;

public class Step_Sound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip stepSound;

    [SerializeField] private float audioSource_volume;


    private void Start()
    {
        audioSource.volume = audioSource_volume;
    }
    public void Make_Step_Sound()
    {
        audioSource.PlayOneShot(stepSound);
    }
}
