using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueOption", menuName = "Dialogue Option/Option")]
public class DialogueOption : ScriptableObject
{
    public string optionText;
    public Conversation nextDialogue;
}
