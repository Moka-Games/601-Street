using UnityEngine;

/// <summary>
/// Script auxiliar para configurar fácilmente el CallAnimationManager en el editor
/// </summary>
public class CallAnimationSetup : MonoBehaviour
{
    [Header("Configuración Automática")]
    [Tooltip("Buscar automáticamente las referencias al iniciar")]
    [SerializeField] private bool autoSetupOnStart = true;

    [Header("Referencias Manuales")]
    [Tooltip("Animator del jugador (se buscará automáticamente si está vacío)")]
    [SerializeField] private Animator playerAnimator;

    [Tooltip("GameObject del teléfono que se mostrará durante las llamadas")]
    [SerializeField] private GameObject phoneGameObject;

    [Header("Nombres de Animaciones")]
    [SerializeField] private string takeCallAnimationName = "TakeCall";
    [SerializeField] private string idleAnimationName = "Idle";
    [SerializeField] private string callEndedParameterName = "callEnded";

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupCallAnimationManager();
        }
    }

    /// <summary>
    /// Configura automáticamente el CallAnimationManager
    /// </summary>
    [ContextMenu("Setup Call Animation Manager")]
    public void SetupCallAnimationManager()
    {
        if (enableDebugLogs)
            Debug.Log("CallAnimationSetup: Iniciando configuración automática...");

        // Verificar si existe el CallAnimationManager
        if (CallAnimationManager.Instance == null)
        {
            Debug.LogError("CallAnimationSetup: No se encontró CallAnimationManager en la escena. Asegúrate de tener un GameObject con el script CallAnimationManager.");
            return;
        }

        // Buscar el animator del jugador si no está asignado
        if (playerAnimator == null)
        {
            playerAnimator = FindPlayerAnimator();
        }

        // Buscar el teléfono si no está asignado
        if (phoneGameObject == null)
        {
            phoneGameObject = FindPhoneGameObject();
        }

        // Configurar las referencias en el CallAnimationManager
        if (playerAnimator != null || phoneGameObject != null)
        {
            CallAnimationManager.Instance.SetReferences(playerAnimator, phoneGameObject);

            if (enableDebugLogs)
                Debug.Log($"CallAnimationSetup: Referencias configuradas - Animator: {(playerAnimator != null ? "✓" : "✗")}, Phone: {(phoneGameObject != null ? "✓" : "✗")}");
        }

        // Configurar nombres de animaciones
        CallAnimationManager.Instance.SetAnimationNames(takeCallAnimationName, idleAnimationName, callEndedParameterName);

        if (enableDebugLogs)
            Debug.Log("CallAnimationSetup: Configuración completada");
    }

    /// <summary>
    /// Busca el Animator del jugador automáticamente
    /// </summary>
    private Animator FindPlayerAnimator()
    {
        Animator foundAnimator = null;

        // Buscar por PlayerController
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            foundAnimator = playerController.GetComponent<Animator>();
            if (foundAnimator != null)
            {
                if (enableDebugLogs)
                    Debug.Log("CallAnimationSetup: Animator encontrado en PlayerController");
                return foundAnimator;
            }
        }

        // Buscar por tag "Player"
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            foundAnimator = player.GetComponent<Animator>();
            if (foundAnimator != null)
            {
                if (enableDebugLogs)
                    Debug.Log("CallAnimationSetup: Animator encontrado en GameObject con tag 'Player'");
                return foundAnimator;
            }
        }

        // Buscar cualquier Animator que tenga las animaciones necesarias
        Animator[] allAnimators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
        foreach (Animator anim in allAnimators)
        {
            if (HasRequiredAnimations(anim))
            {
                foundAnimator = anim;
                if (enableDebugLogs)
                    Debug.Log($"CallAnimationSetup: Animator encontrado en {anim.gameObject.name} (tiene las animaciones requeridas)");
                break;
            }
        }

        if (foundAnimator == null && enableDebugLogs)
        {
            Debug.LogWarning("CallAnimationSetup: No se pudo encontrar un Animator válido automáticamente");
        }

        return foundAnimator;
    }

    /// <summary>
    /// Verifica si el Animator tiene las animaciones requeridas
    /// </summary>
    private bool HasRequiredAnimations(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        bool hasTakeCall = false;
        bool hasIdle = false;
        bool hasCallEndedParam = false;

        // Verificar animaciones
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == takeCallAnimationName)
                hasTakeCall = true;
            if (clip.name == idleAnimationName)
                hasIdle = true;
        }

        // Verificar parámetro callEnded
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == callEndedParameterName && param.type == AnimatorControllerParameterType.Bool)
            {
                hasCallEndedParam = true;
                break;
            }
        }

        if (enableDebugLogs && !hasCallEndedParam)
        {
            Debug.LogWarning($"CallAnimationSetup: El Animator no tiene el parámetro bool '{callEndedParameterName}'");
        }

        return hasTakeCall && hasIdle && hasCallEndedParam;
    }

    /// <summary>
    /// Busca el GameObject del teléfono automáticamente
    /// </summary>
    private GameObject FindPhoneGameObject()
    {
        GameObject foundPhone = null;

        // Lista de nombres comunes para el teléfono
        string[] phoneNames = { "Phone", "Telefono", "PhoneModel", "CellPhone", "Mobile", "Smartphone" };

        foreach (string phoneName in phoneNames)
        {
            foundPhone = GameObject.Find(phoneName);
            if (foundPhone != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"CallAnimationSetup: Teléfono encontrado con nombre '{phoneName}'");
                return foundPhone;
            }
        }

        // Buscar por componente específico si existe
        // (Aquí podrías agregar búsqueda por componentes específicos del teléfono)

        if (foundPhone == null && enableDebugLogs)
        {
            Debug.LogWarning("CallAnimationSetup: No se pudo encontrar el GameObject del teléfono automáticamente. Asígnalo manualmente.");
        }

        return foundPhone;
    }

    /// <summary>
    /// Valida la configuración actual
    /// </summary>
    [ContextMenu("Validate Setup")]
    public void ValidateSetup()
    {
        Debug.Log("=== VALIDACIÓN DE CONFIGURACIÓN ===");

        // Verificar CallAnimationManager
        if (CallAnimationManager.Instance != null)
        {
            Debug.Log("✓ CallAnimationManager encontrado");
        }
        else
        {
            Debug.LogError("✗ CallAnimationManager NO encontrado");
        }

        // Verificar Animator
        if (playerAnimator != null)
        {
            Debug.Log($"✓ Player Animator: {playerAnimator.gameObject.name}");

            if (HasRequiredAnimations(playerAnimator))
            {
                Debug.Log($"✓ Animaciones y parámetros requeridos encontrados: {takeCallAnimationName}, {idleAnimationName}, {callEndedParameterName}");
            }
            else
            {
                Debug.LogWarning($"⚠ Faltan animaciones o parámetros en el Animator. Requeridos: {takeCallAnimationName}, {idleAnimationName}, parámetro bool '{callEndedParameterName}'");
            }
        }
        else
        {
            Debug.LogWarning("⚠ Player Animator no asignado");
        }

        // Verificar teléfono
        if (phoneGameObject != null)
        {
            Debug.Log($"✓ Phone GameObject: {phoneGameObject.name}");
        }
        else
        {
            Debug.LogWarning("⚠ Phone GameObject no asignado");
        }

        // Verificar CallSystem
        if (CallSystem.Instance != null)
        {
            Debug.Log("✓ CallSystem encontrado");
        }
        else
        {
            Debug.LogError("✗ CallSystem NO encontrado");
        }

        Debug.Log("=== FIN DE VALIDACIÓN ===");
    }
}