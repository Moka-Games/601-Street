using UnityEngine;
using UnityEngine.Events;

[System.Serializable]

[CreateAssetMenu(fileName = "NewDialogueOption", menuName = "Dialogue Option/Option")]
public class DialogueOption : ScriptableObject
{
    public string optionText;
    public Conversation nextDialogue;
    public string actionId; 

}