[System.Serializable]
public class DialogueOption
{
    public string optionText;
    public string actionId;
    public bool requiresDiceRoll;
    public int difficultyClass;
    public Conversation nextDialogue; 
    public Conversation successDialogue; 
    public Conversation failureDialogue; 
    public Conversation preDiceConversation; 
}