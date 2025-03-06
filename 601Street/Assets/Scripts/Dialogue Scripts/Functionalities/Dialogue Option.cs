[System.Serializable]
public class DialogueOption
{
    public string optionText;
    public string actionId;
    public bool requiresDiceRoll;
    public int difficultyClass;
    public Conversation nextDialogue; // Di�logo est�ndar
    public Conversation successDialogue; // Nuevo: Di�logo si la tirada es un �xito
    public Conversation failureDialogue; // Nuevo: Di�logo si la tirada es un fallo
}
