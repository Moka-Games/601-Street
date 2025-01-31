using UnityEngine.Events;
using UnityEngine;

[CreateAssetMenu(fileName = "NewConversation", menuName = "Dialogue/Conversation")]
public class Conversation : ScriptableObject
{
    public Dialogue[] dialogues;
    public DialogueOption[] dialogueOptions;
}