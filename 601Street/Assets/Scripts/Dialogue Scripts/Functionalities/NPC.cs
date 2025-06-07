using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Animations.Rigging;

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

    [Header("Animation Rigging - Look At System")]
    [SerializeField] private MultiAimConstraint lookAtConstraint; // Referencia directa al constraint
    [SerializeField] private Transform lookAtTarget; // El objeto pre-configurado que actúa como target
    [SerializeField] private bool autoFindConstraint = true; // Buscar automáticamente el constraint
    [SerializeField] private string constraintName = "HeadAim"; // Nombre del constraint a buscar
    [SerializeField] private float lookAtTransitionDuration = 0.5f; // Duración de la transición
    [SerializeField] private bool moveLookAtTarget = true; // Activar/desactivar el movimiento del target

    private Transform playerLookAtTarget; // El objeto "NPC_LookAt" del jugador
    private Vector3 originalTargetPosition; // Posición original del target
    private bool isLookingAtPlayer = false;
    private Coroutine lookAtTransitionCoroutine;

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

        // NUEVO: Configurar el sistema de Look At
        SetupLookAtSystem();

        // Si es Nakamura, registramos las acciones específicas
        if (isNakamura)
        {
            RegisterNakamuraActions();
        }
    }

    private void SetupLookAtSystem()
    {
        // Buscar el jugador y su componente NPC_LookAt
        FindPlayerLookAtTarget();

        // Buscar el constraint si está habilitado
        if (autoFindConstraint && lookAtConstraint == null)
        {
            FindLookAtConstraint();
        }

        // NUEVO: Guardar la posición original del target si existe
        if (lookAtTarget != null)
        {
            originalTargetPosition = lookAtTarget.localPosition;
            Debug.Log($"Posición original del target guardada: {originalTargetPosition}");
        }

        if (lookAtConstraint != null && lookAtTarget != null)
        {
            Debug.Log($"Sistema Look At configurado para NPC {gameObject.name}");
            Debug.Log($"Constraint: {lookAtConstraint.gameObject.name}");
            Debug.Log($"Target: {lookAtTarget.name}");
        }
        else
        {
            Debug.LogWarning($"Sistema Look At incompleto para NPC {gameObject.name}. " +
                           $"Constraint: {(lookAtConstraint != null ? "OK" : "FALTA")} | " +
                           $"Target: {(lookAtTarget != null ? "OK" : "FALTA")}");
        }
    }

    private void FindPlayerLookAtTarget()
    {
        // Buscar el jugador por tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log($"Jugador encontrado: {player.name}");

            // Buscar el componente "NPC_LookAt" en el jugador y sus hijos
            Transform lookAtTransform = FindChildRecursive(player.transform, "NPC_LookAt");

            if (lookAtTransform != null)
            {
                playerLookAtTarget = lookAtTransform;
                Debug.Log($"Target Look At encontrado para {gameObject.name}: {playerLookAtTarget.name} en la ruta: {GetGameObjectPath(playerLookAtTarget.gameObject)}");
            }
            else
            {
                Debug.LogWarning($"No se encontró el objeto 'NPC_LookAt' en el jugador {player.name} o sus hijos para NPC {gameObject.name}");

                // Debug: Mostrar todos los hijos del jugador para diagnóstico
                Debug.Log("=== HIJOS DEL JUGADOR ===");
                LogAllChildren(player.transform, 0);
                Debug.Log("=== FIN HIJOS DEL JUGADOR ===");
            }
        }
        else
        {
            Debug.LogWarning($"No se encontró jugador con tag 'Player' para NPC {gameObject.name}");
        }
    }

    /// <summary>
    /// NUEVO: Busca un hijo de forma recursiva por nombre
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        // Verificar hijos directos primero
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        // Buscar en nietos recursivamente
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Transform result = FindChildRecursive(child, childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// NUEVO: Obtiene la ruta completa de un GameObject en la jerarquía
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    /// <summary>
    /// NUEVO: Muestra todos los hijos para debug
    /// </summary>
    private void LogAllChildren(Transform parent, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log($"{indent}- {parent.name}");

        for (int i = 0; i < parent.childCount; i++)
        {
            LogAllChildren(parent.GetChild(i), depth + 1);
        }
    }

    /// <summary>
    /// NUEVO: Método para forzar la búsqueda del target manualmente
    /// </summary>
    [ContextMenu("Force Find Player Target")]
    public void ForceFindPlayerTarget()
    {
        Debug.Log("=== BÚSQUEDA FORZADA DEL TARGET DEL JUGADOR ===");
        FindPlayerLookAtTarget();

        if (playerLookAtTarget != null)
        {
            Debug.Log($"✅ Target encontrado: {playerLookAtTarget.name}");
        }
        else
        {
            Debug.LogError("❌ Target NO encontrado");
        }
        Debug.Log("=== FIN BÚSQUEDA FORZADA ===");
    }

    /// <summary>
    /// NUEVO: Método para configurar manualmente el target del jugador
    /// </summary>
    public void SetPlayerLookAtTarget(Transform target)
    {
        playerLookAtTarget = target;
        Debug.Log($"Target del jugador configurado manualmente para {gameObject.name}: {target.name}");
    }

    private void FindLookAtConstraint()
    {
        // Buscar primero en el propio objeto
        lookAtConstraint = GetComponent<MultiAimConstraint>();

        if (lookAtConstraint == null)
        {
            // Buscar en los hijos por nombre
            lookAtConstraint = GetComponentInChildren<MultiAimConstraint>();

            // Si hay múltiples, intentar encontrar el correcto por nombre
            if (lookAtConstraint == null && !string.IsNullOrEmpty(constraintName))
            {
                MultiAimConstraint[] constraints = GetComponentsInChildren<MultiAimConstraint>();
                foreach (var constraint in constraints)
                {
                    if (constraint.gameObject.name.Contains(constraintName))
                    {
                        lookAtConstraint = constraint;
                        break;
                    }
                }
            }
        }

        if (lookAtConstraint != null)
        {
            Debug.Log($"MultiAimConstraint encontrado para {gameObject.name}: {lookAtConstraint.gameObject.name}");
        }
    }

    /// <summary>
    /// NUEVO: Activar el sistema de Look At hacia el jugador
    /// </summary>
    public void StartLookingAtPlayer()
    {
        if (!moveLookAtTarget)
        {
            Debug.Log($"Movimiento del target deshabilitado para {gameObject.name}");
            return;
        }

        if (lookAtConstraint == null || lookAtTarget == null || playerLookAtTarget == null)
        {
            Debug.LogWarning($"No se puede activar Look At para {gameObject.name} - " +
                           $"Constraint: {(lookAtConstraint != null ? "OK" : "FALTA")}, " +
                           $"LookAtTarget: {(lookAtTarget != null ? "OK" : "FALTA")}, " +
                           $"PlayerTarget: {(playerLookAtTarget != null ? "OK" : "FALTA")}");
            return;
        }

        if (isLookingAtPlayer) return; // Ya está mirando al jugador

        // Detener cualquier transición anterior
        if (lookAtTransitionCoroutine != null)
        {
            StopCoroutine(lookAtTransitionCoroutine);
        }

        // SOLO mover el target pre-configurado a la posición del jugador
        lookAtTarget.position = playerLookAtTarget.position;

        // Iniciar la transición suave del weight
        lookAtTransitionCoroutine = StartCoroutine(TransitionLookAtWeight(0f, 1f));
        isLookingAtPlayer = true;

        Debug.Log($"{gameObject.name} comenzó a mirar al jugador - Target movido a {playerLookAtTarget.position}");
    }

    /// <summary>
    /// NUEVO: Desactivar el sistema de Look At
    /// </summary>
    public void StopLookingAtPlayer()
    {
        if (!moveLookAtTarget || lookAtConstraint == null || lookAtTarget == null || !isLookingAtPlayer)
        {
            return;
        }

        // Detener cualquier transición anterior
        if (lookAtTransitionCoroutine != null)
        {
            StopCoroutine(lookAtTransitionCoroutine);
        }

        // Iniciar la transición para devolver el target a su posición original
        lookAtTransitionCoroutine = StartCoroutine(StopLookAtAndReturnTarget());
        isLookingAtPlayer = false;

        Debug.Log($"{gameObject.name} dejó de mirar al jugador");
    }

    /// <summary>
    /// NUEVO: Corrutina para detener el Look At y devolver el target
    /// </summary>
    private IEnumerator StopLookAtAndReturnTarget()
    {
        // Primero hacer la transición del weight a 0
        float elapsed = 0f;
        float startWeight = lookAtConstraint.weight;

        while (elapsed < lookAtTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lookAtTransitionDuration;

            // Usar una curva suave para la transición
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float currentWeight = Mathf.Lerp(startWeight, 0f, smoothT);

            lookAtConstraint.weight = currentWeight;

            yield return null;
        }

        // Asegurar que llegue exactamente a 0
        lookAtConstraint.weight = 0f;

        // Devolver el target a su posición original
        if (lookAtTarget != null)
        {
            lookAtTarget.localPosition = originalTargetPosition;
            Debug.Log($"Target devuelto a la posición original: {originalTargetPosition}");
        }

        lookAtTransitionCoroutine = null;
    }

    /// <summary>
    /// Corrutina para transición suave del peso del constraint
    /// </summary>
    private IEnumerator TransitionLookAtWeight(float fromWeight, float toWeight)
    {
        float elapsed = 0f;

        while (elapsed < lookAtTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lookAtTransitionDuration;

            // Usar una curva suave para la transición
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float currentWeight = Mathf.Lerp(fromWeight, toWeight, smoothT);

            lookAtConstraint.weight = currentWeight;

            yield return null;
        }

        // Asegurar que llegue exactamente al valor final
        lookAtConstraint.weight = toWeight;

        lookAtTransitionCoroutine = null;
    }

    /// <summary>
    /// NUEVO: Método público para configurar manualmente el constraint
    /// </summary>
    public void SetLookAtConstraint(MultiAimConstraint constraint)
    {
        lookAtConstraint = constraint;
        Debug.Log($"Constraint Look At configurado manualmente para {gameObject.name}: {constraint.gameObject.name}");
    }

    /// <summary>
    /// NUEVO: Método público para configurar manualmente el target del NPC
    /// </summary>
    public void SetLookAtTarget(Transform target)
    {
        lookAtTarget = target;
        if (target != null)
        {
            originalTargetPosition = target.localPosition;
            Debug.Log($"Target Look At configurado manualmente para {gameObject.name}: {target.name}");
        }
    }

    /// <summary>
    /// NUEVO: Método público para verificar si el sistema está configurado
    /// </summary>
    public bool IsLookAtSystemReady()
    {
        // Si no tenemos el target del jugador, intentar buscarlo de nuevo
        if (playerLookAtTarget == null)
        {
            Debug.Log($"Target del jugador no encontrado para {gameObject.name}, intentando buscar de nuevo...");
            FindPlayerLookAtTarget();
        }

        bool isReady = lookAtConstraint != null && lookAtTarget != null && playerLookAtTarget != null;

        if (!isReady)
        {
            Debug.LogWarning($"Sistema Look At para {gameObject.name}: " +
                           $"Constraint: {(lookAtConstraint != null ? "✅" : "❌")} | " +
                           $"NPC Target: {(lookAtTarget != null ? "✅" : "❌")} | " +
                           $"Player Target: {(playerLookAtTarget != null ? "✅" : "❌")}");
        }

        return isReady;
    }

    /// <summary>
    /// NUEVO: Método para probar el movimiento del target
    /// </summary>
    [ContextMenu("Test Target Movement")]
    public void TestTargetMovement()
    {
        if (!IsLookAtSystemReady())
        {
            Debug.LogError("Sistema Look At no está listo");
            return;
        }

        Debug.Log("=== PROBANDO MOVIMIENTO DEL TARGET ===");
        Debug.Log($"Posición original del target: {originalTargetPosition}");
        Debug.Log($"Posición actual del target: {lookAtTarget.localPosition}");
        Debug.Log($"Posición del jugador: {playerLookAtTarget.position}");

        // Mover a la posición del jugador
        lookAtTarget.position = playerLookAtTarget.position;
        Debug.Log($"Target movido a la posición del jugador: {lookAtTarget.position}");

        // Esperar y devolver
        StartCoroutine(TestReturnAfterDelay());
    }

    private IEnumerator TestReturnAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        lookAtTarget.localPosition = originalTargetPosition;
        Debug.Log($"Target devuelto a la posición original: {lookAtTarget.localPosition}");
        Debug.Log("=== FIN PRUEBA DE MOVIMIENTO ===");
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

        // NUEVO: Detener el Look At cuando termine la conversación
        StopLookingAtPlayer();

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