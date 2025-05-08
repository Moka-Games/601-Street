using Cinemachine;
using UnityEngine;
using UnityEngine.Events;

public class NPC : MonoBehaviour
{
    public int npcId;
    public Conversation conversation;           // Conversación normal
    public Conversation achievementConversation; // Conversación si se logra algún objetivo
    public Conversation funnyConversation;      // Conversación después de haber interactuado una vez

    [Header("Diálogo especial para la botella")]
    public bool isNakamura = false;                // Marcar si este NPC es Nakamura
    public Conversation conversacionDespuesDeInteraccion; // Conversación después de interactuar con la botella
    public string pensamientoFalloTirada = "Parece que Nakamura no está dispuesto a hablar. Quizás si le doy algo de beber...";

    private bool tiradaFallada = false;
    public bool hasInteracted = false;
    private Animator animator;

    [SerializeField] private bool interactOnce = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        // Si es Nakamura, registramos las acciones específicas
        if (isNakamura)
        {
            RegisterNakamuraActions();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Si es Nakamura, usamos la lógica específica
        if (isNakamura)
        {
            HandleNakamuraConversation();
        }
        // Si no es Nakamura, usamos la lógica normal
        else if (!hasInteracted && !interactOnce)
        {
            Debug.Log("Interactor Triggered");
            DialogueManager.Instance.StartConversation(conversation, this);
        }
        else if (hasInteracted && !interactOnce)
        {
            DialogueManager.Instance.StartConversation(funnyConversation, this);
        }
    }

    private void HandleNakamuraConversation()
    {
        // Si el jugador ya ha interactuado con la botella
        if (Botella.objectInteracted)
        {
            DialogueManager.Instance.StartConversation(conversacionDespuesDeInteraccion, this);
            // Opcionalmente, resetear la variable si es una interacción única
            Botella.objectInteracted = false;
        }
        // Si ha fallado la tirada previamente pero no ha interactuado con la botella
        else if (tiradaFallada)
        {
            DialogueManager.Instance.StartConversation(funnyConversation, this);
        }
        // Primera interacción, mostramos la conversación normal
        else
        {
            DialogueManager.Instance.StartConversation(conversation, this);
        }
    }

    private void RegisterNakamuraActions()
    {
        // Registrar las acciones necesarias
        ActionController actionController = ActionController.Instance;
        if (actionController != null)
        {
            // Acción para el resultado de la tirada
            actionController.RegisterAction("NakamuraTirada", new DialogueAction(
                // Acción estándar (sin tirada)
                () => {
                    Debug.Log("Comenzando conversación con Nakamura");
                },
                // Acción de éxito
                () => {
                    Debug.Log("Tirada exitosa con Nakamura");
                },
                // Acción de fracaso
                () => {
                    Debug.Log("Tirada fallida con Nakamura");
                    tiradaFallada = true;
                    Pensamientos_Manager pensamientosManager = FindAnyObjectByType<Pensamientos_Manager>();
                    if (pensamientosManager != null)
                    {
                        pensamientosManager.MostrarPensamiento(pensamientoFalloTirada);
                    }
                }
            ));
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

    public void InvokeOnConversationEnd()
    {
        // Implementación vacía para satisfacer cualquier llamada desde el DialogueManager
    }
}