using UnityEngine;

[System.Serializable]
public class DialogueOption
{
    public string optionText;           // Texto que se muestra en la opción de diálogo
    public string actionId;            // ID de la acción que se debe ejecutar cuando se selecciona la opción
    public bool requiresDiceRoll;      // Determina si se requiere una tirada de dado
    public int difficultyClass;        // Clase de dificultad para la tirada de dado
    public Conversation nextDialogue;  // El siguiente diálogo o conversación que se inicia después de seleccionar esta opción

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
