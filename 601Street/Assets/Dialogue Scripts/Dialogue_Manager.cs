using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using Cinemachine;

[System.Serializable]
public class Dialogue
{
    public string speakerName;
    [TextArea(4, 4)]
    public string content;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public GameObject dialogueUI;
    public TMP_Text speakerNameText;
    public TMP_Text contentText;
    public TypewriterEffect typewriterEffect;

    public GameObject optionsUI;
    public Button[] optionButtons;

    private Conversation currentConversation;
    private NPC currentNPC;
    private int currentDialogueIndex = 0;
    public bool isTyping = false;

    public UnityEvent onConversationEnd;
    public UnityEvent onConversationEndHouse;

    public GameObject npc_1;
    public GameObject npc_2;
    public GameObject npc_3;

    public CinemachineVirtualCamera virtualCamera;

    [Header("Dice Protoype Interface Variables")]
    [SerializeField] private Dice_Manager diceManager;
    [SerializeField] private GameObject diceInterface;
    [SerializeField] private GameObject dialogueInterface;
    public GameObject failObject;
    public GameObject sucessObject;


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
        if (dialogueUI != null)
        {
            dialogueUI.SetActive(true);
        }
        if (optionsUI != null)
        {
            optionsUI.SetActive(false);
        }

        if (currentNPC.npcId == 1)
        {
            virtualCamera.LookAt = npc_1.transform;
        }
        if (currentNPC.npcId == 2)
        {
            virtualCamera.LookAt = npc_2.transform;
        }
        if (currentNPC.npcId == 3)
        {
            virtualCamera.LookAt = npc_3.transform;
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
        if (currentConversation != null && optionIndex < currentConversation.dialogueOptions.Length)
        {
            Conversation nextConversation = currentConversation.dialogueOptions[optionIndex].nextDialogue;
            StartConversation(nextConversation, currentNPC);
        }
    }

    public void NextDialogue()
    {
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
            if (currentNPC.npcId == 2)
            {
                onConversationEndHouse.Invoke();
            }
            virtualCamera.LookAt = null;
        }

        Debug.Log("Conversación finalizada");
    }

    public void SelectOption()
    {
        dialogueInterface.SetActive(false);
        diceInterface.SetActive(true);
        diceManager.ResetUI();
    }
}
