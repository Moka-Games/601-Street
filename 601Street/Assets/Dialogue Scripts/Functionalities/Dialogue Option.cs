using UnityEngine;

[System.Serializable]
public class DialogueOption
{
    public string optionText;           // Texto que se muestra en la opci�n de di�logo
    public string actionId;            // ID de la acci�n que se debe ejecutar cuando se selecciona la opci�n
    public bool requiresDiceRoll;      // Determina si se requiere una tirada de dado
    public int difficultyClass;        // Clase de dificultad para la tirada de dado
    public Conversation nextDialogue;  // El siguiente di�logo o conversaci�n que se inicia despu�s de seleccionar esta opci�n

    // Constructor
    public DialogueOption(string optionText, string actionId, bool requiresDiceRoll, int difficultyClass, Conversation nextDialogue)
    {
        this.optionText = optionText;
        this.actionId = actionId;
        this.requiresDiceRoll = requiresDiceRoll;
        this.difficultyClass = difficultyClass;
        this.nextDialogue = nextDialogue;
    }
}
