using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private Dice_Manager diceManager;
    [SerializeField] private GameObject diceInterface;
    [SerializeField] private GameObject dialogueInterface;

    [SerializeField] private DialogueOption[] dialogueOptions; // Opciones configurables desde el Inspector

    private DialogueOption currentOption; // Almacena la opci�n seleccionada

    public GameObject failObject;
    public GameObject sucessObject;

    private void Start()
    {
        failObject.SetActive(false);
        sucessObject.SetActive(false);
        diceManager.OnRollComplete += HandleRollResult;
    }

    public void SelectOption(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= dialogueOptions.Length)
        {
            Debug.LogWarning("�ndice de opci�n inv�lido.");
            return;
        }

        currentOption = dialogueOptions[optionIndex];

        dialogueInterface.SetActive(false);
        diceInterface.SetActive(true);

        diceManager.ResetUI();
    }

    private void HandleRollResult(bool isSuccess)
    {
        if (currentOption == null)
        {
            Debug.LogWarning("No se ha seleccionado ninguna opci�n.");
            return;
        }

        if (isSuccess)
        {
            sucessObject.SetActive(true);
            Debug.Log("�xito: " + currentOption.SuccessOutcome);
        }
        else
        {
            failObject.SetActive(true);
            Debug.Log("Fallo: " + currentOption.FailOutcome);
        }
    }
}


[System.Serializable]
public class DialogueOption
{
    public string OptionText;
    public string SuccessOutcome;
    public string FailOutcome;
}
