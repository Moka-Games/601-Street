using UnityEngine;

public class EntradaAulaExtra : MonoBehaviour
{

    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator no encontrado en el objeto " + gameObject.name);
        }
        animator.enabled = false; 
    }

    public void OpenDoors()
    {
        animator.enabled = true; 
    }
}
