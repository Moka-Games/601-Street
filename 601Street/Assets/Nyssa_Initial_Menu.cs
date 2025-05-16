using UnityEngine;
public class Nyssa_Initial_Menu : MonoBehaviour
{
    private Animator animator;
    private float kickTimer = 0f;
    private float afkTimer = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Incrementar ambos temporizadores
        kickTimer += Time.deltaTime;
        afkTimer += Time.deltaTime;

        // Comprobar si toca activar el trigger afk (cada 45 segundos)
        if (afkTimer >= 60f)
        {
            // Reiniciar el temporizador afk
            afkTimer = 0f;

            // Reiniciar también el temporizador kick para evitar que se active 
            // inmediatamente después del afk
            kickTimer = 0f;

            // Activar el trigger afk
            animator.SetTrigger("afk");
        }
        // Comprobar si toca activar el trigger kick (cada 15 segundos)
        else if (kickTimer >= 30f)
        {
            // Reiniciar el temporizador kick
            kickTimer = 0f;

            // Activar el trigger kick
            animator.SetTrigger("kick");
        }
    }
}