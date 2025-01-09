using UnityEngine;

[CreateAssetMenu(fileName = "New Conversation", menuName = "Dialogue/Conversation")]
public class Conversation : ScriptableObject
{
    public Dialogue[] dialogues;
    public DialogueOption[] dialogueOptions;
}
