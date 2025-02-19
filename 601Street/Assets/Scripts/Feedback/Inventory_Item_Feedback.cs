using UnityEngine;
using TMPro;

public class Inventory_Item_Feedback : MonoBehaviour
{
    public GameObject visualParent;

    public GameObject positional_Feedback;
    public GameObject input_Feedback;

    private Inventory_Interactor inventory_Interactor;

    private bool playerOnRange = false;

    private void Start()
    {
        inventory_Interactor = FindAnyObjectByType<Inventory_Interactor>();
        
        visualParent.SetActive(true);
        positional_Feedback.SetActive(false);
        input_Feedback.SetActive(false);
        playerOnRange = false;

    }

    private void Update()
    {
        if(playerOnRange)
        {
            positional_Feedback.SetActive(true);
            
            if(inventory_Interactor.canInteract == true)
            {
                input_Feedback.SetActive(true);
                positional_Feedback.SetActive(false);
            }
            else
            {
                input_Feedback.SetActive(false);
                positional_Feedback.SetActive(true);
            }
        }
        else if(!playerOnRange)
        {
            print("player not on range");
            positional_Feedback.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            playerOnRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerOnRange = false;
        }
    }
}
