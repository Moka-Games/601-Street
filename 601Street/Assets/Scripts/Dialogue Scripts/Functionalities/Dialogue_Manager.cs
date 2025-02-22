using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using Cinemachine;
using Unity.VisualScripting;
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

    [Header("Cinemachine Camera")]
    [SerializeField] private CinemachineFreeLook freeLookCamera;
    [SerializeField] private Transform playerLookAtTarget; // El objetivo de LookAt del jugador
    private Transform npcLookAtTarget;

    [Header("Camera Transition Settings")]
    [SerializeField] private float transitionDuration = 1.0f; // Duración de la transición
    private Transform currentLookAtTarget; // Objetivo actual de LookAt
    private Coroutine transitionCoroutine;

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
        if (conversation == null)
        {
            Debug.LogError("Conversation is null");
            return;
        }

        currentConversation = conversation;
        currentNPC = npc;
        currentDialogueIndex = 0;

        // Cambiar el LookAt de la cámara al NPC con transición suave
        npcLookAtTarget = currentNPC.transform.Find("LookAt");
        if (npcLookAtTarget != null && freeLookCamera != null)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine); // Detener la transición actual si hay una en curso
            }
            transitionCoroutine = StartCoroutine(ChangeLookAtTarget(npcLookAtTarget));
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
                typewriterEffect.StartTyping(currentDialogue.content, currentNPC);
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
        // Guardamos el índice de la opción seleccionada
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

                    diceManager.OnRollComplete = (isSuccess) =>
                    {
                        diceRollResult = isSuccess;  // Guardar el resultado del dado
                    };
                }
                else
                {
                    // Ejecutar acción estándar
                    ActionController.Instance.InvokeAction(selectedOption.actionId);
                }
            }

            // Guardamos la referencia de la próxima conversación
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


    public void NextDialogue()
    {
        Debug.Log("Next Dialogue");
        if (currentConversation != null && currentDialogueIndex < currentConversation.dialogues.Length - 1)
        {
            currentDialogueIndex++;
            ShowDialogue();
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
        if (currentNPC != null)
        {
            currentNPC.SetInteracted();
            currentNPC.InvokeOnConversationEnd();
            onConversationEnd.Invoke();
        }

        // Restaurar el LookAt de la cámara al jugador con transición suave
        if (freeLookCamera != null && playerLookAtTarget != null)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine); // Detener la transición actual si hay una en curso
            }
            transitionCoroutine = StartCoroutine(ChangeLookAtTarget(playerLookAtTarget));
        }

        Debug.Log("Conversación finalizada");
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
        // Verificar si se ha seleccionado una opción y si hay un resultado de dado
        if (selectedOptionIndex != -1 && diceRollResult.HasValue && currentConversation != null && selectedOptionIndex < currentConversation.dialogueOptions.Length)
        {
            DialogueOption selectedOption = currentConversation.dialogueOptions[selectedOptionIndex];

            // Ejecutamos la acción basada en el resultado del dado
            ActionController.Instance.InvokeAction(selectedOption.actionId, diceRollResult.Value);

            diceRollResult = null;
            selectedOptionIndex = -1;  
        }
        else
        {
            Debug.LogWarning("No dice roll result available or no option selected.");
        }

        diceInterface.SetActive(false);
    }

    private IEnumerator ChangeLookAtTarget(Transform newTarget)
    {
        if (freeLookCamera == null || newTarget == null)
        {
            yield break;
        }

        // Crear un objeto temporal para la transición
        GameObject tempTarget = new GameObject("TempLookAtTarget");
        tempTarget.transform.position = freeLookCamera.LookAt.position; // Posición inicial
        freeLookCamera.LookAt = tempTarget.transform;

        float elapsedTime = 0f;
        Vector3 initialPosition = tempTarget.transform.position;

        while (elapsedTime < transitionDuration)
        {
            if (newTarget == null)
            {
                Destroy(tempTarget); // Limpiar el objeto temporal
                yield break;
            }

            // Interpolar suavemente entre la posición inicial y la del nuevo objetivo
            tempTarget.transform.position = Vector3.Lerp(
                initialPosition,
                newTarget.position,
                elapsedTime / transitionDuration
            );

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Asignar el nuevo objetivo final
        freeLookCamera.LookAt = newTarget;

        // Destruir el objeto temporal
        Destroy(tempTarget);
    }

}

[System.Serializable]
public class Dialogue
{
    public string speakerName;
    [TextArea(4, 4)]
    public string content;
}