using Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class NPC : MonoBehaviour
{
    public int npcId;
    public Conversation conversation;
    public Conversation achievementConversation;
    public Conversation funnyConversation;
    public bool hasInteracted = false;
    private Animator animator;

    private bool InteractionEnded = false;

    public UnityEvent onObjectDelivered;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasInteracted)
        {
            Debug.Log("Interactor Triggered");
            DialogueManager.Instance.StartConversation(conversation, this);
        }
        else if (other.CompareTag("Player") && hasInteracted)
        {
            DialogueManager.Instance.StartConversation(funnyConversation, this);
        }
    }

    public void SetInteracted()
    {
        hasInteracted = true;
        Debug.Log("Conversación Terminada");
    }

    public void SetNOTInteracted()
    {
        hasInteracted = false;
        Debug.Log("Conversación Terminada");
    }
    public void InvokeOnConversationEnd()
    {

    }

    public void PerformEmotion(string emotion)
    {
        switch (emotion)
        {
            case "happy":
                animator.Play("CharacterArmature_Wave");
                break;
            case "sad":
                animator.SetTrigger("Sad");
                break;
            default:
                Debug.LogWarning($"Emoción desconocida: {emotion}");
                break;
        }
    }

    public void PerformAction(string action)
    {
        switch (action)
        {
            case "think":
                animator.SetTrigger("Think");
                Debug.Log("asdasd");
                break;
            case "shake":
                animator.SetTrigger("Shake");
                break;
            default:
                Debug.LogWarning($"Acción desconocida: {action}");
                break;
        }
    }
}
