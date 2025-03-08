using UnityEngine;

public class UI_Manager : MonoBehaviour
{
    public GameObject inventoryItemFeedback;
    public GameObject interactableItemFeedback;

    private Inventory_Item inventoryItem;
    private InteractableObject interactableObject;

    private void Start()
    {
        inventoryItem = FindAnyObjectByType<Inventory_Item>();
        interactableObject = FindAnyObjectByType<InteractableObject>();



        if (inventoryItem == null)
        {
            inventoryItemFeedback.SetActive(false);
        }
        else
        {
            print("Scripts inventory item encontrado");
        }

        if (interactableObject == null)
        {
            interactableItemFeedback.SetActive(false);
        }
        else
        {
            print("Scripts interactable object encontrado");
        }
    }



}
