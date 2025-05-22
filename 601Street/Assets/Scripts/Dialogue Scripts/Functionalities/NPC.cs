using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class NPC : MonoBehaviour
{
    public int npcId;
    public Conversation conversation;           // Conversaci�n normal
    public Conversation achievementConversation; // Conversaci�n si se logra alg�n objetivo
    public Conversation funnyConversation;      // Conversaci�n despu�s de haber interactuado una vez

    [Header("Di�logo especial para la botella")]
    public bool isNakamura = false;                // Marcar si este NPC es Nakamura
    public Conversation conversacionDespuesDeInteraccion; // Conversaci�n despu�s de interactuar con la botella
    public string pensamientoFalloTirada = "Parece que Nakamura no est� dispuesto a hablar. Quiz�s si le doy algo de beber...";

    private bool tiradaFallada = false;
    public bool hasInteracted = false; // Cambiado de static a instancia para tracking individual
    public bool singleInteraction = false;
    private Animator animator;


    public UnityEvent OnConversationEnded;

    [Header("Control de Interacci�n")]
    private bool isInConversation = false;
    private float conversationCooldown = 1.5f;
    private float lastInteractionTime = 0f;
    private Collider myCollider;
    private void Awake()
    {
        animator = GetComponent<Animator>();
        myCollider = GetComponent<Collider>();

        // Si es Nakamura, registramos las acciones espec�ficas
        if (isNakamura)
        {
            RegisterNakamuraActions();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificar si podemos iniciar una nueva conversaci�n
        if (isInConversation || Time.time - lastInteractionTime < conversationCooldown)
        {
            return;
        }

        if (!other.CompareTag("Player"))
            return;

        // Verificar tambi�n si el DialogueManager ya est� en una conversaci�n
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
            // Interacci�n normal (m�ltiples interacciones permitidas)
            DialogueManager.Instance.StartConversation(conversation, this);
        }
        else if (hasInteracted && !singleInteraction)
        {
            // Segunda+ interacci�n cuando se permiten m�ltiples interacciones
            DialogueManager.Instance.StartConversation(funnyConversation, this);
        }
        else if (!hasInteracted && singleInteraction)
        {
            // Primera y �nica interacci�n cuando singleInteraction es true
            DialogueManager.Instance.StartConversation(conversation, this);
        }
        // Nota: Eliminamos el caso "else if (hasInteracted && singleInteraction)" 
        // porque ahora est� manejado por el return temprano arriba
    }

    private void HandleNakamuraConversation()
    {
        // Si el jugador ya ha interactuado con la botella
        if (Botella.objectInteracted)
        {
            DialogueManager.Instance.StartConversation(conversacionDespuesDeInteraccion, this);
            // Opcionalmente, resetear la variable si es una interacci�n �nica
            Botella.objectInteracted = false;
        }
        // Si ha fallado la tirada previamente pero no ha interactuado con la botella
        else if (tiradaFallada)
        {
            DialogueManager.Instance.StartConversation(funnyConversation, this);
        }
        // Primera interacci�n, mostramos la conversaci�n normal
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
            // Acci�n para el resultado de la tirada
            actionController.RegisterAction("NakamuraTirada", new DialogueAction(
                // Acci�n est�ndar (sin tirada)
                () => {
                    Debug.Log("Comenzando conversaci�n con Nakamura");
                },
                // Acci�n de �xito
                () => {
                    Debug.Log("Tirada exitosa con Nakamura");
                },
                // Acci�n de fracaso
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
        Debug.Log("Conversaci�n Terminada - NPC marcado como interactuado: " + gameObject.name);
    }

    public void SetNOTInteracted()
    {
        hasInteracted = false;
        Debug.Log("Estado de interacci�n reiniciado - NPC: " + gameObject.name);
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
                Debug.LogWarning($"Emoci�n desconocida: {emotion}");
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
                Debug.LogWarning($"Acci�n desconocida: {action}");
                break;
        }
    }
    public void EndCurrentConversation()
    {
        isInConversation = false;
        lastInteractionTime = Time.time;

        // Opcionalmente, deshabilitar temporalmente el collider para evitar reactivaci�n
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
        // Verificar si la conversaci�n que termin� es la principal
        if (endedConversation == conversation)
        {
            Debug.Log($"La conversaci�n principal del NPC {gameObject.name} ha terminado");

            // Marcar como interactuado al finalizar la conversaci�n principal
            SetInteracted();

            // Invocar el evento solo si la conversaci�n terminada es la principal
            OnConversationEnded?.Invoke();
        }
        else
        {
            Debug.Log($"Otra conversaci�n del NPC {gameObject.name} ha terminado");
        }
    }
}