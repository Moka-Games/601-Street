using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using DG.Tweening; // Añadido para DOTween

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

    // Referencias para el control del jugador y la cámara
    private PlayerController playerController;
    private GameObject npcCamera;

    private bool isInConversation = false;

    // Agregar esta variable para un cooldown entre conversaciones
    private float conversationCooldown = 1.5f;
    private float lastConversationEndTime = 0f;

    // Nuevas variables para animación del diálogo
    [Header("Dialog Animation Settings")]
    [SerializeField] private float dialogEntryDuration = 0.3f;
    [SerializeField] private Ease dialogEntryEase = Ease.OutBack;

    [Header("Dialog Animation Settings")]
    [SerializeField] private float dialogExitDuration = 0.3f;
    [SerializeField] private Ease dialogExitEase = Ease.InBack;
    [SerializeField] private float slideDistance = 100f;
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
            Debug.LogWarning("No se encontró el PlayerController. No se podrá pausar el movimiento del jugador.");
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

    // Nuevo método para animar la aparición del diálogo
    private void AnimateDialogueEntry()
    {
        // Asegurarnos de que dialogueUI existe y está activo
        if (dialogueUI == null || !dialogueUI.activeSelf) return;

        // Obtenemos el RectTransform
        RectTransform dialogueRect = dialogueUI.GetComponent<RectTransform>();
        if (dialogueRect == null) return;

        // Guardamos la posición original
        Vector2 originalPosition = dialogueRect.anchoredPosition;

        // Posición inicial (abajo de su posición final)
        dialogueRect.anchoredPosition = new Vector2(originalPosition.x, originalPosition.y - slideDistance);

        // Animamos hacia la posición original
        dialogueRect.DOAnchorPos(originalPosition, dialogEntryDuration)
            .SetEase(dialogEntryEase)
            .OnComplete(() => {
                // Se puede agregar algún efecto adicional aquí si se desea
            });
    }


    public void StartConversation(Conversation conversation, NPC npc)
    {
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
            Debug.Log("Movimiento del jugador pausado durante la conversación");
        }

        npcCamera = null;
        if (currentNPC != null)
        {
            npcCamera = currentNPC.transform.Find("NPC_Camera")?.gameObject;
            if (npcCamera != null)
            {
                npcCamera.SetActive(true);
                Debug.Log("Cámara del NPC activada: " + npcCamera.name);
            }
            else
            {
                Debug.LogWarning("No se encontró la cámara 'NPC_Camera' en el NPC: " + currentNPC.name);
            }
        }

        // Reiniciar el TypewriterEffect antes de comenzar
        if (typewriterEffect != null)
        {
            typewriterEffect.Reset();
        }

        // Activar la interfaz de diálogo
        if (dialogueUI != null)
        {
            dialogueUI.SetActive(true);
            // Animar la entrada del diálogo
            AnimateDialogueEntry();
        }

        if (optionsUI != null)
        {
            optionsUI.SetActive(false);
        }
        if (Next_bubble != null)
        {
            Next_bubble.SetActive(false);
        }

        // Pequeño delay para asegurar que la UI esté completamente activa antes de mostrar el diálogo
        StartCoroutine(DelayedShowDialogue());
    }

    private IEnumerator DelayedShowDialogue()
    {
        // Esperar un frame para asegurar que los componentes estén activos
        yield return null;

        // Ahora mostrar el diálogo
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
                // Verificar que contentText está asignado
                if (typewriterEffect.textComponent == null)
                {
                    typewriterEffect.textComponent = contentText;
                    Debug.Log("Asignando contentText a typewriterEffect.textComponent");
                }

                // Iniciar la animación de escritura
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

                    // Asignamos el callback para almacenar el resultado y la conversación correspondiente
                    diceManager.OnRollComplete = (isSuccess) =>
                    {
                        diceRollResult = isSuccess;
                        nextContextualConversation = isSuccess ? selectedOption.successDialogue : selectedOption.failureDialogue;
                    };
                }
                else
                {
                    // Ejecutar acción estándar sin dados
                    ActionController.Instance.InvokeAction(selectedOption.actionId);

                    // Continuar con la siguiente conversación estándar (si existe)
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

        // Detener cualquier animación en curso
        if (typewriterEffect != null && isTyping)
        {
            typewriterEffect.StopTyping();
            isTyping = false;
        }

        if (currentConversation != null && currentDialogueIndex < currentConversation.dialogues.Length - 1)
        {
            currentDialogueIndex++;

            // Pequeño delay para asegurar que todo esté listo
            StartCoroutine(DelayedShowDialogue());
        }
        else
        {
            ShowOptions();
        }
    }

    public void EndConversation()
    {
        // Ejecutar la acción asociada a la conversación si existe
        if (currentConversation != null && !string.IsNullOrEmpty(currentConversation.actionId))
        {
            ActionController.Instance.InvokeAction(currentConversation.actionId);
        }

        // Desactivar la cámara del NPC si está activa
        if (npcCamera != null)
        {
            npcCamera.SetActive(false);
            Debug.Log("Cámara del NPC desactivada");
        }

        // Reactivar el movimiento del jugador
        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
            Debug.Log("Movimiento del jugador reactivado después de la conversación");
        }

        if (currentNPC != null)
        {
            // Pasamos la referencia de la conversación que terminó al NPC
            currentNPC.ConversationEnded(currentConversation);

            currentNPC.SetInteracted();
            currentNPC.EndCurrentConversation();

            onConversationEnd.Invoke();
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.EnterGameplayState();
        }

        // Animar la salida del diálogo ANTES de desactivarlo
        AnimateDialogueExit();

        // Actualizar el tiempo de finalización y el estado de conversación
        lastConversationEndTime = Time.time;
        isInConversation = false;

        Debug.Log("Conversación finalizada - Cooldown iniciado");
    }
    public void SelectDiceOption()
    {
        // Desactivar la interfaz de diálogo
        dialogueInterface.SetActive(false);

        // Activar la interfaz de dados
        diceInterface.SetActive(true);

        // Resetear la interfaz de dados
        diceManager.ResetUI();

        // Configurar el DC para la tirada
        diceManager.SetDifficultyClass(currentConversation.dialogueOptions[selectedOptionIndex].difficultyClass);
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

            // Ocultar la interfaz de dados
            diceInterface.SetActive(false);

            // Ocultar también el panel de opciones para evitar que aparezca brevemente
            if (optionsUI != null)
            {
                optionsUI.SetActive(false);
            }

            // Ejecutar la acción con el resultado de la tirada
            ActionController.Instance.InvokeAction(selectedOption.actionId, diceRollResult.Value);

            // Iniciar la conversación que se asignó previamente
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
            Debug.LogWarning("No hay resultado de dado disponible o ninguna opción seleccionada.");
            diceInterface.SetActive(false);
        }
    }
    private void AnimateDialogueExit()
    {
        // Verificar que dialogueUI existe y está activo
        if (dialogueUI == null || !dialogueUI.activeSelf)
        {
            if (optionsUI != null)
            {
                optionsUI.SetActive(false);
            }
            return;
        }

        // Obtener el RectTransform
        RectTransform dialogueRect = dialogueUI.GetComponent<RectTransform>();
        if (dialogueRect == null)
        {
            dialogueUI.SetActive(false);
            if (optionsUI != null) optionsUI.SetActive(false);
            return;
        }

        // Detener cualquier animación actual
        dialogueRect.DOKill();

        // Posición destino (abajo de su posición actual)
        Vector2 originalPosition = dialogueRect.anchoredPosition;
        Vector2 targetPosition = new Vector2(originalPosition.x, originalPosition.y - slideDistance);

        // Animar el movimiento hacia abajo
        dialogueRect.DOAnchorPos(targetPosition, dialogExitDuration)
            .SetEase(dialogExitEase)
            .OnComplete(() => {
                // Desactivar la interfaz de diálogo cuando la animación termine
                if (dialogueUI != null)
                {
                    dialogueUI.SetActive(false);
                }
                if (optionsUI != null)
                {
                    optionsUI.SetActive(false);
                }
            });
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

        // Versión simplificada: <Bold>texto</Bold> -> <b>texto</b>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Bold>(.*?)</Bold>",
            "<b>$1</b>");

        // Cursiva: <Italic>(texto)</Italic> -> <i>texto</i>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Italic>\((.*?)\)</Italic>",
            "<i>$1</i>");

        // Versión simplificada: <Italic>texto</Italic> -> <i>texto</i>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Italic>(.*?)</Italic>",
            "<i>$1</i>");

        // Subrayado: <Underline>(texto)</Underline> -> <u>texto</u>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Underline>\((.*?)\)</Underline>",
            "<u>$1</u>");

        // Versión simplificada: <Underline>texto</Underline> -> <u>texto</u>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Underline>(.*?)</Underline>",
            "<u>$1</u>");

        // Color: <Color=red>(texto)</Color> -> <color=red>texto</color>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Color=([^>]*?)>\((.*?)\)</Color>",
            "<color=$1>$2</color>");

        // Versión simplificada: <Color=red>texto</Color> -> <color=red>texto</color>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Color=([^>]*?)>(.*?)</Color>",
            "<color=$1>$2</color>");

        // Tamaño: <Size=150%>(texto)</Size> -> <size=150%>texto</size>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Size=([^>]*?)>\((.*?)\)</Size>",
            "<size=$1>$2</size>");

        // Versión simplificada: <Size=150%>texto</Size> -> <size=150%>texto</size>
        processed = System.Text.RegularExpressions.Regex.Replace(
            processed,
            @"<Size=([^>]*?)>(.*?)</Size>",
            "<size=$1>$2</size>");

        return processed;
    }
    
}
