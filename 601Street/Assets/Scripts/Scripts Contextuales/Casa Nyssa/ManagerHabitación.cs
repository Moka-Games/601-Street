using UnityEngine;

public class ManagerHabitación : MonoBehaviour
{
    public InteractableObject puerta;
    private void Start()
    {   
        if(Ordenador.ordenadorInteractuado)
        {
            puerta.enabled = true;
        }
        else
        {
            puerta.enabled = false;
        }
    }
}
