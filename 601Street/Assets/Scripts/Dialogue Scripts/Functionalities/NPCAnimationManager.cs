using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Gestor avanzado de animaciones para NPCs con soporte para colas de animaciones
/// y configuraciones personalizadas por NPC
/// </summary>
[System.Serializable]
public class AnimationMapping
{
    [Header("Configuración de Animación")]
    public string tagName;           // Nombre de la etiqueta (ej: "happy")
    public string animationName;     // Nombre del estado o trigger en el Animator
    public AnimationType type;       // Tipo de animación (Trigger, State, Legacy)
    public float duration = -1f;     // Duración forzada (-1 = usar duración del clip)
    public bool interruptible = true; // Si puede ser interrumpida por otra animación

    public enum AnimationType
    {
        Trigger,    // Usar SetTrigger()
        State,      // Usar Play()
        Legacy      // Usar métodos legacy del NPC
    }
}

public class NPCAnimationManager : MonoBehaviour
{
    [Header("Configuración de Animaciones")]
    [SerializeField] private List<AnimationMapping> customAnimations = new List<AnimationMapping>();

    [Header("Configuración Avanzada")]
    [SerializeField] private bool enableAnimationQueue = true;
    [SerializeField] private float defaultAnimationDuration = 2f;
    [SerializeField] private bool logAnimationEvents = true;

    // Referencias
    private NPC npcComponent;
    private Animator animator;

    // Cola de animaciones
    private Queue<AnimationMapping> animationQueue = new Queue<AnimationMapping>();
    private bool isPlayingAnimation = false;
    private Coroutine currentAnimationCoroutine;

    // Mapeo de animaciones por defecto
    private Dictionary<string, AnimationMapping> defaultAnimations;

    private void Awake()
    {
        // Obtener componentes
        npcComponent = GetComponent<NPC>();
        if (npcComponent == null)
        {
            npcComponent = GetComponentInParent<NPC>();
        }

        // Buscar animator
        FindAnimator();

        // Configurar animaciones por defecto
        SetupDefaultAnimations();

        // Procesar animaciones personalizadas
        ProcessCustomAnimations();
    }

    private void FindAnimator()
    {
        // Buscar en el objeto actual
        animator = GetComponent<Animator>();

        // Buscar en el padre
        if (animator == null)
        {
            animator = GetComponentInParent<Animator>();
        }

        // Buscar en los hijos
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null && logAnimationEvents)
        {
            Debug.Log($"NPCAnimationManager: Animator encontrado en {animator.gameObject.name}");
        }
    }

    private void SetupDefaultAnimations()
    {
        defaultAnimations = new Dictionary<string, AnimationMapping>
        {
            { "happy", new AnimationMapping { tagName = "happy", animationName = "CharacterArmature_Wave", type = AnimationMapping.AnimationType.State } },
            { "sad", new AnimationMapping { tagName = "sad", animationName = "Sad", type = AnimationMapping.AnimationType.Trigger } },
            { "think", new AnimationMapping { tagName = "think", animationName = "Think", type = AnimationMapping.AnimationType.Trigger } },
            { "shake", new AnimationMapping { tagName = "shake", animationName = "Shake", type = AnimationMapping.AnimationType.Trigger } },
            { "wave", new AnimationMapping { tagName = "wave", animationName = "CharacterArmature_Wave", type = AnimationMapping.AnimationType.State } },
            { "angry", new AnimationMapping { tagName = "angry", animationName = "Angry", type = AnimationMapping.AnimationType.Trigger } },
            { "surprised", new AnimationMapping { tagName = "surprised", animationName = "Surprised", type = AnimationMapping.AnimationType.Trigger } },
            { "laugh", new AnimationMapping { tagName = "laugh", animationName = "Laugh", type = AnimationMapping.AnimationType.Trigger } },
            { "nod", new AnimationMapping { tagName = "nod", animationName = "Nod", type = AnimationMapping.AnimationType.Trigger } },
            { "no", new AnimationMapping { tagName = "no", animationName = "ShakeHead", type = AnimationMapping.AnimationType.Trigger } }
        };
    }

    private void ProcessCustomAnimations()
    {
        foreach (var customAnim in customAnimations)
        {
            if (!string.IsNullOrEmpty(customAnim.tagName))
            {
                // Sobrescribir animaciones por defecto con las personalizadas
                if (defaultAnimations.ContainsKey(customAnim.tagName.ToLower()))
                {
                    defaultAnimations[customAnim.tagName.ToLower()] = customAnim;
                }
                else
                {
                    defaultAnimations.Add(customAnim.tagName.ToLower(), customAnim);
                }

                if (logAnimationEvents)
                {
                    Debug.Log($"Animación personalizada configurada: {customAnim.tagName} -> {customAnim.animationName}");
                }
            }
        }
    }

    /// <summary>
    /// Ejecuta una animación por nombre de etiqueta
    /// </summary>
    public void PlayAnimation(string tagName)
    {
        if (string.IsNullOrEmpty(tagName)) return;

        string lowerTagName = tagName.ToLower();

        if (defaultAnimations.TryGetValue(lowerTagName, out AnimationMapping animMapping))
        {
            if (enableAnimationQueue)
            {
                QueueAnimation(animMapping);
            }
            else
            {
                ExecuteAnimation(animMapping);
            }
        }
        else
        {
            // Intentar con el NPC directamente como fallback
            if (npcComponent != null)
            {
                if (logAnimationEvents)
                {
                    Debug.Log($"Animación no mapeada '{tagName}', usando método directo del NPC");
                }
                npcComponent.PlayAnimation(tagName);
            }
        }
    }

    /// <summary>
    /// Añade una animación a la cola
    /// </summary>
    private void QueueAnimation(AnimationMapping animMapping)
    {
        animationQueue.Enqueue(animMapping);

        if (!isPlayingAnimation)
        {
            ProcessAnimationQueue();
        }
    }

    /// <summary>
    /// Procesa la cola de animaciones
    /// </summary>
    private void ProcessAnimationQueue()
    {
        if (animationQueue.Count == 0)
        {
            isPlayingAnimation = false;
            return;
        }

        AnimationMapping nextAnimation = animationQueue.Dequeue();
        ExecuteAnimation(nextAnimation);
    }

    /// <summary>
    /// Ejecuta una animación específica
    /// </summary>
    private void ExecuteAnimation(AnimationMapping animMapping)
    {
        if (animator == null)
        {
            if (logAnimationEvents)
            {
                Debug.LogWarning($"No se puede ejecutar animación '{animMapping.tagName}' - No hay Animator");
            }
            ProcessAnimationQueue();
            return;
        }

        // Interrumpir animación actual si es necesario
        if (isPlayingAnimation && !animMapping.interruptible)
        {
            if (logAnimationEvents)
            {
                Debug.Log($"Animación '{animMapping.tagName}' añadida a la cola (animación actual no interrumpible)");
            }
            return;
        }

        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
        }

        isPlayingAnimation = true;

        switch (animMapping.type)
        {
            case AnimationMapping.AnimationType.Trigger:
                ExecuteTriggerAnimation(animMapping);
                break;
            case AnimationMapping.AnimationType.State:
                ExecuteStateAnimation(animMapping);
                break;
            case AnimationMapping.AnimationType.Legacy:
                ExecuteLegacyAnimation(animMapping);
                break;
        }
    }

    private void ExecuteTriggerAnimation(AnimationMapping animMapping)
    {
        if (HasParameter(animMapping.animationName, AnimatorControllerParameterType.Trigger))
        {
            if (logAnimationEvents)
            {
                Debug.Log($"Ejecutando trigger '{animMapping.animationName}' para etiqueta '{animMapping.tagName}'");
            }

            animator.SetTrigger(animMapping.animationName);

            float duration = animMapping.duration > 0 ? animMapping.duration : defaultAnimationDuration;
            currentAnimationCoroutine = StartCoroutine(WaitForAnimationComplete(duration));
        }
        else
        {
            if (logAnimationEvents)
            {
                Debug.LogWarning($"Trigger '{animMapping.animationName}' no encontrado en Animator");
            }
            ProcessAnimationQueue();
        }
    }

    private void ExecuteStateAnimation(AnimationMapping animMapping)
    {
        if (HasState(animMapping.animationName))
        {
            if (logAnimationEvents)
            {
                Debug.Log($"Reproduciendo estado '{animMapping.animationName}' para etiqueta '{animMapping.tagName}'");
            }

            animator.Play(animMapping.animationName);

            float duration = animMapping.duration > 0 ? animMapping.duration : GetClipLength(animMapping.animationName);
            currentAnimationCoroutine = StartCoroutine(WaitForAnimationComplete(duration));
        }
        else
        {
            if (logAnimationEvents)
            {
                Debug.LogWarning($"Estado '{animMapping.animationName}' no encontrado en Animator");
            }
            ProcessAnimationQueue();
        }
    }

    private void ExecuteLegacyAnimation(AnimationMapping animMapping)
    {
        if (npcComponent != null)
        {
            if (logAnimationEvents)
            {
                Debug.Log($"Ejecutando animación legacy '{animMapping.tagName}'");
            }

            npcComponent.PlayAnimation(animMapping.tagName);

            float duration = animMapping.duration > 0 ? animMapping.duration : defaultAnimationDuration;
            currentAnimationCoroutine = StartCoroutine(WaitForAnimationComplete(duration));
        }
        else
        {
            if (logAnimationEvents)
            {
                Debug.LogWarning($"No se puede ejecutar animación legacy - No hay componente NPC");
            }
            ProcessAnimationQueue();
        }
    }

    private IEnumerator WaitForAnimationComplete(float duration)
    {
        yield return new WaitForSeconds(duration);

        isPlayingAnimation = false;
        ProcessAnimationQueue();
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasState(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        for (int layerIndex = 0; layerIndex < animator.layerCount; layerIndex++)
        {
            if (animator.HasState(layerIndex, Animator.StringToHash(stateName)))
            {
                return true;
            }
        }

        return false;
    }

    private float GetClipLength(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return defaultAnimationDuration;

        AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(0);
        foreach (var clipInfo in clipInfos)
        {
            if (clipInfo.clip.name == stateName)
            {
                return clipInfo.clip.length;
            }
        }

        return defaultAnimationDuration;
    }

    /// <summary>
    /// Detiene todas las animaciones en cola
    /// </summary>
    public void StopAllAnimations()
    {
        animationQueue.Clear();

        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        isPlayingAnimation = false;

        if (logAnimationEvents)
        {
            Debug.Log("Todas las animaciones detenidas");
        }
    }

    /// <summary>
    /// Añade una animación personalizada en tiempo de ejecución
    /// </summary>
    public void AddCustomAnimation(string tagName, string animationName, AnimationMapping.AnimationType type, float duration = -1f)
    {
        var newMapping = new AnimationMapping
        {
            tagName = tagName.ToLower(),
            animationName = animationName,
            type = type,
            duration = duration
        };

        defaultAnimations[tagName.ToLower()] = newMapping;

        if (logAnimationEvents)
        {
            Debug.Log($"Animación personalizada añadida: {tagName} -> {animationName}");
        }
    }

    /// <summary>
    /// Verifica si hay animaciones en cola
    /// </summary>
    public bool HasQueuedAnimations()
    {
        return animationQueue.Count > 0 || isPlayingAnimation;
    }

    /// <summary>
    /// Obtiene información sobre el estado actual de las animaciones
    /// </summary>
    public string GetAnimationStatus()
    {
        return $"Reproduciendo: {isPlayingAnimation}, En cola: {animationQueue.Count}";
    }

    #region Métodos de Debug

    [ContextMenu("Debug Animation Mappings")]
    public void DebugAnimationMappings()
    {
        Debug.Log("=== ANIMACIONES CONFIGURADAS ===");
        foreach (var mapping in defaultAnimations)
        {
            Debug.Log($"{mapping.Key} -> {mapping.Value.animationName} ({mapping.Value.type})");
        }
    }

    [ContextMenu("Test Happy Animation")]
    public void TestHappyAnimation()
    {
        PlayAnimation("happy");
    }

    [ContextMenu("Test Sad Animation")]
    public void TestSadAnimation()
    {
        PlayAnimation("sad");
    }

    [ContextMenu("Stop All Animations")]
    public void StopAllAnimationsFromContext()
    {
        StopAllAnimations();
    }

    #endregion
}