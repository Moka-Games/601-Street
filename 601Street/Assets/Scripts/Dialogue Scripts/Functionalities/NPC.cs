using Cinemachine;
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
    public bool hasInteracted = false;
    private Animator animator;

    [SerializeField] private bool interactOnce = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        // Si es Nakamura, registramos las acciones espec�ficas
        if (isNakamura)
        {
            RegisterNakamuraActions();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Si es Nakamura, usamos la l�gica espec�fica
        if (isNakamura)
        {
            HandleNakamuraConversation();
        }
        // Si no es Nakamura, usamos la l�gica normal
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
        Debug.Log("Conversaci�n Terminada");
    }

    public void SetNOTInteracted()
    {
        hasInteracted = false;
        Debug.Log("Conversaci�n Terminada");
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

    public void InvokeOnConversationEnd()
    {
        // Implementaci�n vac�a para satisfacer cualquier llamada desde el DialogueManager
    }
}