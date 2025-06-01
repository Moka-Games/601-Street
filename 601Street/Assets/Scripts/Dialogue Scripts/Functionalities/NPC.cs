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

    private Animator cachedAnimator;
    private bool animatorSearched = false;
    private void Awake()
    {
        // Código existente
        animator = GetComponent<Animator>();
        myCollider = GetComponent<Collider>();

        // NUEVO: Buscar el Animator al inicializar
        FindAnimatorComponent();

        // Si es Nakamura, registramos las acciones específicas
        if (isNakamura)
        {
            RegisterNakamuraActions();
        }
    }
    private void FindAnimatorComponent()
    {
        if (animatorSearched) return;

        // Primero intentar obtenerlo del objeto actual
        cachedAnimator = GetComponent<Animator>();

        // Si no está en el objeto actual, buscar en el padre
        if (cachedAnimator == null)
        {
            cachedAnimator = GetComponentInParent<Animator>();
        }

        // Si tampoco está en el padre, buscar en todos los hijos
        if (cachedAnimator == null)
        {
            cachedAnimator = GetComponentInChildren<Animator>();
        }

        animatorSearched = true;

        if (cachedAnimator != null)
        {
            Debug.Log($"Animator encontrado para NPC {gameObject.name} en: {cachedAnimator.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"No se encontró Animator para NPC {gameObject.name}");
        }
    }
    public void PlayAnimation(string animationName)
    {
        // Verificar si tenemos un NPCAnimationManager
        NPCAnimationManager animationManager = GetComponent<NPCAnimationManager>();
        if (animationManager == null)
        {
            animationManager = GetComponentInChildren<NPCAnimationManager>();
        }

        // Si tenemos el manager avanzado, usarlo
        if (animationManager != null)
        {
            animationManager.PlayAnimation(animationName);
            return;
        }

        // Fallback al sistema anterior si no hay manager avanzado
        PlayAnimationFallback(animationName);
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
        if (cachedAnimator == null)
        {
            FindAnimatorComponent();
            if (cachedAnimator == null)
            {
                Debug.LogWarning($"No se puede ejecutar emoción '{emotion}' - No hay Animator en NPC {gameObject.name}");
                return;
            }
        }

        switch (emotion)
        {
            case "happy":
                if (HasAnimatorState("CharacterArmature_Wave"))
                {
                    cachedAnimator.Play("CharacterArmature_Wave");
                }
                else if (HasAnimatorParameter("Happy", AnimatorControllerParameterType.Trigger))
                {
                    cachedAnimator.SetTrigger("Happy");
                }
                break;
            case "sad":
                if (HasAnimatorParameter("Sad", AnimatorControllerParameterType.Trigger))
                {
                    cachedAnimator.SetTrigger("Sad");
                }
                break;
            default:
                Debug.LogWarning($"Emoción desconocida: {emotion}");
                break;
        }
    }

    public void PerformAction(string action)
    {
        if (cachedAnimator == null)
        {
            FindAnimatorComponent();
            if (cachedAnimator == null)
            {
                Debug.LogWarning($"No se puede ejecutar acción '{action}' - No hay Animator en NPC {gameObject.name}");
                return;
            }
        }

        switch (action)
        {
            case "think":
                if (HasAnimatorParameter("Think", AnimatorControllerParameterType.Trigger))
                {
                    cachedAnimator.SetTrigger("Think");
                    Debug.Log("Trigger Think ejecutado");
                }
                break;
            case "shake":
                if (HasAnimatorParameter("Shake", AnimatorControllerParameterType.Trigger))
                {
                    cachedAnimator.SetTrigger("Shake");
                }
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

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (cachedAnimator == null || cachedAnimator.runtimeAnimatorController == null)
            return false;

        foreach (AnimatorControllerParameter parameter in cachedAnimator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// NUEVO: Verifica si el Animator tiene un estado específico
    /// </summary>
    private bool HasAnimatorState(string stateName)
    {
        if (cachedAnimator == null || cachedAnimator.runtimeAnimatorController == null)
            return false;

        // Verificar en todas las capas del Animator
        for (int layerIndex = 0; layerIndex < cachedAnimator.layerCount; layerIndex++)
        {
            if (cachedAnimator.HasState(layerIndex, Animator.StringToHash(stateName)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// NUEVO: Ejecuta animaciones usando los métodos legacy existentes
    /// </summary>
    private void ExecuteLegacyAnimation(string animationName)
    {
        string lowerAnimationName = animationName.ToLower();

        switch (lowerAnimationName)
        {
            case "happy":
                PerformEmotion("happy");
                break;
            case "sad":
                PerformEmotion("sad");
                break;
            case "think":
                PerformAction("think");
                break;
            case "shake":
                PerformAction("shake");
                break;
            case "wave":
                if (cachedAnimator != null)
                {
                    cachedAnimator.Play("CharacterArmature_Wave");
                }
                break;
            default:
                Debug.LogWarning($"Animación desconocida: {animationName} para NPC {gameObject.name}");
                break;
        }
    }



    /// <summary>
    /// NUEVO: Sistema de animaciones fallback (el código anterior)
    /// </summary>
    private void PlayAnimationFallback(string animationName)
    {
        // Asegurar que tenemos el Animator
        if (cachedAnimator == null)
        {
            FindAnimatorComponent();
        }

        if (cachedAnimator == null)
        {
            Debug.LogWarning($"No se puede reproducir animación '{animationName}' - No hay Animator en NPC {gameObject.name}");
            return;
        }

        try
        {
            // Intentar reproducir la animación
            // Primero verificar si existe como trigger
            if (HasAnimatorParameter(animationName, AnimatorControllerParameterType.Trigger))
            {
                Debug.Log($"Ejecutando trigger '{animationName}' en NPC {gameObject.name}");
                cachedAnimator.SetTrigger(animationName);
            }
            // Si no es un trigger, intentar como estado directo
            else if (HasAnimatorState(animationName))
            {
                Debug.Log($"Reproduciendo estado '{animationName}' en NPC {gameObject.name}");
                cachedAnimator.Play(animationName);
            }
            // Si no existe, intentar los métodos legacy
            else
            {
                Debug.Log($"Usando método legacy para animación '{animationName}' en NPC {gameObject.name}");
                ExecuteLegacyAnimation(animationName);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al reproducir animación '{animationName}' en NPC {gameObject.name}: {e.Message}");
        }
    }

}