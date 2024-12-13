using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private Dice_Manager diceManager;
    [SerializeField] private GameObject diceInterface;
    [SerializeField] private GameObject dialogueInterface;
    public GameObject failObject;
    public GameObject sucessObject;

    private void Start()
    {
        failObject.SetActive(false);
        sucessObject.SetActive(false);
    }

    public void SelectOption()
    {
        dialogueInterface.SetActive(false);
        diceInterface.SetActive(true);
        diceManager.ResetUI();
    }
}


