using UnityEngine;

[CreateAssetMenu(fileName = "NewConversation", menuName = "Dialogue/Conversation")]
public class Conversation : ScriptableObject
{
    public string actionId; // Nuevo campo para identificar la acción a ejecutar cuando termina la conversación
    public Dialogue[] dialogues;
    public DialogueOption[] dialogueOptions;
}
