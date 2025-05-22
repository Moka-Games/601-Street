using Cinemachine;
using System.Collections;
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
    public bool hasInteracted = false; // Cambiado de static a instancia para tracking individual
    public bool singleInteraction = false;
    private Animator animator;


    public UnityEvent OnConversationEnded;

    [Header("Control de Interacción")]
    private bool isInConversation = false;
    private float conversationCooldown = 1.5f;
    private float lastInteractionTime = 0f;
    private Collider myCollider;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        myCollider = GetComponent<Collider>();

        // Si es Nakamura, registramos las acciones específicas
        if (isNakamura)
        {
            RegisterNakamuraActions();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificar si podemos iniciar una nueva conversación
        if (isInConversation || Time.time - lastInteractionTime < conversationCooldown)
        {
            return;
        }

        if (!other.CompareTag("Player"))
            return;

        // Verificar también si el DialogueManager ya está en una conversación
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsInConversation())
        {
            return;
        }

        // Si singleInteraction es true y ya hemos interactuado, no hacer nada
        if (singleInteraction && hasInteracted)
        {
            return;
        }

        isInConversation = true;
        lastInteractionTime = Time.time;

        if (isNakamura)
        {
            HandleNakamuraConversation();
        }
        else if (!hasInteracted && !singleInteraction)
        {
            // Interacción normal (múltiples interacciones permitidas)
            DialogueManager.Instance.StartConversation(conversation, this);
        }
        else if (hasInteracted && !singleInteraction)
        {
            // Segunda+ interacción cuando se permiten múltiples interacciones
            DialogueManager.Instance.StartConversation(funnyConversation, this);
        }
        else if (!hasInteracted && singleInteraction)
        {
            // Primera y única interacción cuando singleInteraction es true
            DialogueManager.Instance.StartConversation(conversation, this);
        }
        // Nota: Eliminamos el caso "else if (hasInteracted && singleInteraction)" 
        // porque ahora está manejado por el return temprano arriba
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
        Debug.Log("Conversación Terminada - NPC marcado como interactuado: " + gameObject.name);
    }

    public void SetNOTInteracted()
    {
        hasInteracted = false;
        Debug.Log("Estado de interacción reiniciado - NPC: " + gameObject.name);
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
    public void EndCurrentConversation()
    {
        isInConversation = false;
        lastInteractionTime = Time.time;

        // Opcionalmente, deshabilitar temporalmente el collider para evitar reactivación
        StartCoroutine(TemporarilyDisableCollider());
    }

    private IEnumerator TemporarilyDisableCollider()
    {
        if (myCollider != null)
        {
            myCollider.enabled = false;
            yield return new WaitForSeconds(1.0f);
            myCollider.enabled = true;
        }
    }
    public void ConversationEnded(Conversation endedConversation)
    {
        // Verificar si la conversación que terminó es la principal
        if (endedConversation == conversation)
        {
            Debug.Log($"La conversación principal del NPC {gameObject.name} ha terminado");

            // Marcar como interactuado al finalizar la conversación principal
            SetInteracted();

            // Invocar el evento solo si la conversación terminada es la principal
            OnConversationEnded?.Invoke();
        }
        else
        {
            Debug.Log($"Otra conversación del NPC {gameObject.name} ha terminado");
        }
    }
}