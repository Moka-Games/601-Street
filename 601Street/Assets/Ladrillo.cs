using UnityEngine;

public class Ladrillo : MonoBehaviour
{


    public bool isLadrillo1;
    public bool isLadrillo2;
    public bool isLadrillo3;

    public Animator animator;

    public static bool laddrillo_1_Interacted = false;
    public static bool laddrillo_2_Interacted = false;
    public static bool laddrillo_3_Interacted = false;

    private void Start()
    {
        if (animator != null)
        {
            animator.enabled = false;
        }
        else
        {
            print("Animator is  null");
        }
    }

    public void InteractLadrillo()
    {
        

        animator.enabled = true; 

        if(isLadrillo1)
        {
            laddrillo_1_Interacted = true;
            print("Ladrillo 1 Interacted");

        }
        else if (isLadrillo2)
        {
            laddrillo_2_Interacted = true;
            print("Ladrillo 2 Interacted");

        }
        else if (isLadrillo3)
        {
            laddrillo_3_Interacted = true;
            print("Ladrillo 3 Interacted");
        }
    }

    
}
