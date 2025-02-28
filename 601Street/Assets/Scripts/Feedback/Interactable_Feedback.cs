using UnityEngine;

public class Interactable_Feedback : MonoBehaviour
{
    public GameObject detectionFeedback;
    public GameObject canInteractFeedback;

    private bool playerOnRange = false;

    private PlayerInteraction playerInteractionScript;
    void Start()
    {
        playerInteractionScript = FindAnyObjectByType<PlayerInteraction>();

        detectionFeedback.SetActive(false);
        canInteractFeedback.SetActive(false);
    }

    void Update()
    {
        if(playerOnRange)
        {
            detectionFeedback.SetActive(true);

            if(playerInteractionScript != null && playerInteractionScript.canInteract)
            {
                canInteractFeedback.SetActive(true);
                detectionFeedback.SetActive(false);
            }
            else
            {
                canInteractFeedback.SetActive(false);
            }
        }
        else
        {
            detectionFeedback.SetActive(false);
            canInteractFeedback.SetActive(false);

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            playerOnRange = true;
            print("Player on range");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerOnRange = false;
            print("Player NOT on range");

        }
    }
}
