using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public GameObject dialogueUI;
    public TMP_Text speakerNameText;
    public TMP_Text contentText;
    public TypewriterEffect typewriterEffect;
    public GameObject Next_bubble;

    public GameObject optionsUI;
    public Button[] optionButtons;

    private Conversation currentConversation;
    private NPC currentNPC;
    private int currentDialogueIndex = 0;
    public bool isTyping = false;

    public UnityEvent onConversationEnd;

    [Header("Dice Protoype Interface Variables")]
    [SerializeField] private Dice_Manager diceManager;
    [SerializeField] private GameObject diceInterface;
    [SerializeField] private GameObject dialogueInterface;
    public GameObject failObject;
    public GameObject sucessObject;

    private bool? diceRollResult = null;
    private int selectedOptionIndex = -1;

    private Conversation nextContextualConversation;

    // Referencias para el control del jugador y la c�mara
    private PlayerController playerController;
    private GameObject npcCamera;
    
    private bool isInConversation = false;

    // Agregar esta variable para un cooldown entre conversaciones
    private float conversationCooldown = 1.5f;
    private float lastConversationEndTime = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {

        playerController = FindAnyObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("No se encontr� el PlayerController. No se podr� pausar el movimiento del jugador.");
        }

        failObject.SetActive(false);
        sucessObject.SetActive(false);

        if (dialogueUI != null)
        {
            dialogueUI.SetActive(false);
        }
        if (optionsUI != null)
        {
            optionsUI.SetActive(false);
        }
        if (Next_bubble != null)
        {
            Next_bubble.SetActive(false);
        }

        if (typewriterEffect != null && typewriterEffect.textComponent == null)
        {
            Debug.LogError("TypewriterEffect no tiene asignado textComponent. Asignando contentText por defecto.");
            typewriterEffect.textComponent = contentText;
        }

        if (contentText != null)
        {
            contentText.richText = true;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (dialogueUI != null && dialogueUI.activeSelf)
            {
                if (isTyping)
                {
                    if (typewriterEffect != null)
                    {
                        typewriterEffect.StopTyping();
                    }
                    isTyping = false;
                }
                else
                {
                    if (Next_bubble != null)
                    {
                        Next_bubble.SetActive(false);
                    }
                    NextDialogue();
                }
            }
        }
    }

    public void StartConversation(Conversation conversation, NPC npc)
    {
        /*if (isInConversation || Time.time - lastConversationEndTime < conversationCooldown)
        {
            Debug.Log("No se puede iniciar una nueva conversaci�n: ya hay una en curso o es demasiado pronto");
            return;
        }*/
        if (conversation == null)
        {
            Debug.LogError("Conversation is null");
            return;
        }

        isInConversation = true;
        currentConversation = conversation;
        currentNPC = npc;
        currentDialogueIndex = 0;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.EnterDialogueState();
        }

        // Pausar el movimiento del jugador
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
            Debug.Log("Movimiento del jugador pausado durante la conversaci�n");
        }

        npcCamera = null;
        if (currentNPC != null)
        {
            npcCamera = currentNPC.transform.Find("NPC_Camera")?.gameObject;
            if (npcCamera != null)
            {
                npcCamera.SetActive(true);
                Debug.Log("C�mara del NPC activada: " + npcCamera.name);
            }
            else
            {
                Debug.LogWarning("No se encontr� la c�mara 'NPC_Camera' en el NPC: " + currentNPC.name);
            }
        }

        // Reiniciar el TypewriterEffect antes de comenzar
        if (typewriterEffect != null)
        {
            typewriterEffect.Reset();
        }

        if (dialogueUI != null)
        {
            dialogueUI.SetActive(true);
        }
        if (optionsUI != null)
        {
            optionsUI.SetActive(false);
        }
        if (Next_bubble != null)
        {
            Next_bubble.SetActive(false);
        }

        // Peque�o delay para asegurar que la UI est� completamente activa antes de mostrar el di�logo
        StartCoroutine(DelayedShowDialogue());
    }
    private IEnumerator DelayedShowDialogue()
    {
        // Esperar un frame para asegurar que los componentes est�n activos
        yield return null;

        // Ahora mostrar el di�logo
        ShowDialogue();
    }
    public void ShowDialogue()
    {
        if (currentConversation != null && currentDialogueIndex < currentConversation.dialogues.Length)
        {
            Dialogue currentDialogue = currentConversation.dialogues[currentDialogueIndex];
            if (speakerNameText != null)
            {
                speakerNameText.text = currentDialogue.speakerName;
            }

            isTyping = true;

            if (Next_bubble != null)
            {
                Next_bubble.SetActive(false);
            }

            if (typewriterEffect != null)
            {
                // Verificar que contentText est� asignado
                if (typewriterEffect.textComponent == null)
                {
                    typewriterEffect.textComponent = contentText;
                    Debug.Log("Asignando contentText a typewriterEffect.textComponent");
                }

                // Iniciar la animaci�n de escritura
                typewriterEffect.StartTyping(currentDialogue.content, currentNPC);
            }
            else
            {
                // Si no hay efecto de tipeo, procesamos las etiquetas directamente
                if (contentText != null)
                {
                    contentText.text = TextFormatHelper.ProcessTextTags(currentDialogue.content);
                    contentText.richText = true;
                }
                OnTypingComplete();
            }
        }
        else
        {
            ShowOptions();
        }
    }

    public void ShowOptions()
    {
        if (currentConversation != null && currentConversation.dialogueOptions != null && currentConversation.dialogueOptions.Length > 0)
        {
            if (optionsUI != null)
            {
                optionsUI.SetActive(true);
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < currentConversation.dialogueOptions.Length)
                {
                    optionButtons[i].gameObject.SetActive(true);
                    optionButtons[i].GetComponentInChildren<TMP_Text>().text = currentConversation.dialogueOptions[i].optionText;
                    int optionIndex = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => SelectOption(optionIndex));
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            EndConversation();
        }
    }

    public void SelectOption(int optionIndex)
    {
        selectedOptionIndex = optionIndex;

        if (currentConversation != null && optionIndex < currentConversation.dialogueOptions.Length)
        {
            DialogueOption selectedOption = currentConversation.dialogueOptions[optionIndex];

            if (!string.IsNullOrEmpty(selectedOption.actionId))
            {
                if (selectedOption.requiresDiceRoll)
                {
                    SelectDiceOption();
                    diceManager.SetDifficultyClass(selectedOption.difficultyClass);

                    // Asignamos el callback para almacenar el resultado y la conversaci�n correspondiente
                    diceManager.OnRollComplete = (isSuccess) =>
                    {
                        diceRollResult = isSuccess;
                        nextContextualConversation = isSuccess ? selectedOption.successDialogue : selectedOption.failureDialogue;
                    };
                }
                else
                {
                    // Ejecutar acci�n est�ndar sin dados
                    ActionController.Instance.InvokeAction(selectedOption.actionId);

                    // Continuar con la siguiente conversaci�n est�ndar (si existe)
                    Conversation nextConversation = selectedOption.nextDialogue;
                    if (nextConversation != null)
                    {
                        StartConversation(nextConversation, currentNPC);
                    }
                    else
                    {
                        EndConversation();
                    }
                }
            }
        }
    }


    public void NextDialogue()
    {
        Debug.Log("Next Dialogue");

        // Detener cualquier animaci�n en curso
        if (typewriterEffect != null && isTyping)
        {
            typewriterEffect.StopTyping();
            isTyping = false;
        }

        if (currentConversation != null && currentDialogueIndex < currentConversation.dialogues.Length - 1)
        {
            currentDialogueIndex++;

            // Peque�o delay para asegurar que todo est� listo
            StartCoroutine(DelayedShowDialogue());
        }
        else
        {
            ShowOptions();
        }
    }

    public void EndConversation()
    {
        if (dialogueUI != null)
        {
            dialogueUI.SetActive(false);
        }
        if (optionsUI != null)
        {
            optionsUI.SetActive(false);
        }

        // Ejecutar la acci�n asociada a la conversaci�n si existe
        if (currentConversation != null && !string.IsNullOrEmpty(currentConversation.actionId))
        {
            ActionController.Instance.InvokeAction(currentConversation.actionId);
        }

        // Desactivar la c�mara del NPC si est� activa
        if (npcCamera != null)
        {
            npcCamera.SetActive(false);
            Debug.Log("C�mara del NPC desactivada");
        }

        // Reactivar el movimiento del jugador
        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
            Debug.Log("Movimiento del jugador reactivado despu�s de la conversaci�n");
        }

        if (currentNPC != null)
        {
            currentNPC.SetInteracted();
            // Agregar: Establecer flag de conversaci�n activa en el NPC
            currentNPC.EndCurrentConversation();

            onConversationEnd.Invoke();
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.EnterGameplayState();
        }

        // Actualizar el tiempo de finalizaci�n y el estado de conversaci�n
        lastConversationEndTime = Time.time;
        isInConversation = false;

        Debug.Log("Conversaci�n finalizada - Cooldown iniciado");
    }
    public void SelectDiceOption()
    {
        dialogueInterface.SetActive(false);
        diceInterface.SetActive(true);
        diceManager.ResetUI();
    }

    public void OnTypingComplete()
    {
        isTyping = false;
        if (Next_bubble != null)
        {
            Next_bubble.SetActive(true);
        }
    }

    public void RandomFunction()
    {
        print("I like cucumbers");
    }

    public void OnDiceRollCompleteButtonPressed()
    {
        if (selectedOptionIndex != -1 && diceRollResult.HasValue && currentConversation != null && selectedOptionIndex < currentConversation.dialogueOptions.Length)
        {
            DialogueOption selectedOption = currentConversation.dialogueOptions[selectedOptionIndex];

            // Ejecutar la acci�n con el resultado de la tirada
            ActionController.Instance.InvokeAction(selectedOption.actionId, diceRollResult.Value);

            // Iniciar la conversaci�n que se asign� previamente
            if (nextContextualConversation != null)
            {
                StartConversation(nextContextualConversation, currentNPC);
                nextContextualConversation = null; // Limpiar la referencia tras usarla
            }
            else
            {
                EndConversation();
            }

            // Reset de variables
            diceRollResult = null;
            selectedOptionIndex = -1;
        }
        else
        {
            Debug.LogWarning("No hay resultado de dado disponible o ninguna opci�n seleccionada.");
        }

        diceInterface.SetActive(false);
    }

    public bool IsInConversation()
    {
        return isInConversation;
    }

}

[System.Serializable]
public class Dialogue
{
    public string speakerName;
    [TextArea(4, 4)]
    public string content;
}
public static class TextFormatHelper
{
    public static string ProcessTextTags(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string processed = input;

        // Procesamos las etiquetas personalizadas y las convertimos en etiquetas de TextMeshPro

        // Negrita: <Bold>(texto)</Bold> -> <b>texto</b>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Bold>\((.*?)\)</Bold>",
            "<b>$1</b>");

        // Versi�n simplificada: <Bold>texto</Bold> -> <b>texto</b>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Bold>(.*?)</Bold>",
            "<b>$1</b>");

        // Cursiva: <Italic>(texto)</Italic> -> <i>texto</i>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Italic>\((.*?)\)</Italic>",
            "<i>$1</i>");

        // Versi�n simplificada: <Italic>texto</Italic> -> <i>texto</i>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Italic>(.*?)</Italic>",
            "<i>$1</i>");

        // Subrayado: <Underline>(texto)</Underline> -> <u>texto</u>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Underline>\((.*?)\)</Underline>",
            "<u>$1</u>");

        // Versi�n simplificada: <Underline>texto</Underline> -> <u>texto</u>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Underline>(.*?)</Underline>",
            "<u>$1</u>");

        // Color: <Color=red>(texto)</Color> -> <color=red>texto</color>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Color=([^>]*?)>\((.*?)\)</Color>",
            "<color=$1>$2</color>");

        // Versi�n simplificada: <Color=red>texto</Color> -> <color=red>texto</color>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Color=([^>]*?)>(.*?)</Color>",
            "<color=$1>$2</color>");

        // Tama�o: <Size=150%>(texto)</Size> -> <size=150%>texto</size>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Size=([^>]*?)>\((.*?)\)</Size>",
            "<size=$1>$2</size>");

        // Versi�n simplificada: <Size=150%>texto</Size> -> <size=150%>texto</size>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Size=([^>]*?)>(.*?)</Size>",
            "<size=$1>$2</size>");

        return processed;
    }
    
}
