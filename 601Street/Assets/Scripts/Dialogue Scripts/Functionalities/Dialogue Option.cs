[System.Serializable]
public class DialogueOption
{
    public string optionText;
    public string actionId;
    public bool requiresDiceRoll;
    public int difficultyClass;
    public Conversation nextDialogue; // Diálogo estándar
    public Conversation successDialogue; // Nuevo: Diálogo si la tirada es un éxito
    public Conversation failureDialogue; // Nuevo: Diálogo si la tirada es un fallo
}
