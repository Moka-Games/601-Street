using UnityEngine;

public class Porton_Portal : MonoBehaviour
{
    private Animator anim;
    public GameObject niebla;

    private void Start()
    {
        anim = GetComponent<Animator>();
        anim.enabled = false;
        niebla.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            anim.enabled = true;
            niebla.SetActive(true);
        }
    }
}
