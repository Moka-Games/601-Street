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
        LadrilloManager ladrilloManager = FindAnyObjectByType<LadrilloManager>();
        ladrilloManager.CheckLadrillos();

        animator.enabled = true; 

        if(isLadrillo1)
        {
            laddrillo_1_Interacted = true;
        }
        else if (isLadrillo2)
        {
            laddrillo_2_Interacted = true;

        }
        else if (isLadrillo3)
        {
            laddrillo_3_Interacted = true;
        }
    }

    
}
