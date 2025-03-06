using UnityEngine;

[CreateAssetMenu(fileName = "NewConversation", menuName = "Dialogue/Conversation")]
public class Conversation : ScriptableObject
{
    public string actionId; // Nuevo campo para identificar la acci�n a ejecutar cuando termina la conversaci�n
    public Dialogue[] dialogues;
    public DialogueOption[] dialogueOptions;
}
