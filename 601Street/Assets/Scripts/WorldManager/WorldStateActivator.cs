using UnityEngine;

/// <summary>
/// Componente para activar estados en el WorldStateManager
/// Se puede adjuntar a objetos interactivos para causar cambios en el mundo
/// </summary>
public class WorldStateActivator : MonoBehaviour
{
    [System.Serializable]
    public class StateModification
    {
        [Tooltip("Tipo de modificación")]
        public ModificationType type = ModificationType.SetFlag;
        [Tooltip("ID del estado a modificar")]
        public string stateID;

        // Valores para diferentes tipos
        [Tooltip("Valor booleano para SetFlag")]
        public bool flagValue = true;
        [Tooltip("Valor entero para SetCounter/IncrementCounter")]
        public int counterValue = 1;
        [Tooltip("Valor string para SetString")]
        public string stringValue = "";

        // Modificación de objetos
        [Tooltip("Escena del objeto (para SetObjectActive)")]
        public string objectSceneName;
        [Tooltip("ID del objeto (para SetObjectActive)")]
        public string objectID;
    }

    public enum ModificationType
    {
        SetFlag,
        SetCounter,
        IncrementCounter,
        SetString,
        SetObjectActive
    }

    [Header("Modificaciones de Estado")]
    [SerializeField] private StateModification[] stateModifications;

    [Header("Opciones")]
    [SerializeField] private bool activateOnStart = false;
    [SerializeField] private bool activateOnInteract = true;
    [SerializeField] private bool activateOnce = false;

    // Control de activación
    private bool hasActivated = false;

    private void Start()
    {
        if (activateOnStart && !hasActivated)
        {
            ActivateStateChanges();

            if (activateOnce)
            {
                hasActivated = true;
            }
        }
    }

    // Si está en un InteractableObject, este método será llamado
    public void OnInteract()
    {
        if (activateOnInteract && (!activateOnce || !hasActivated))
        {
            ActivateStateChanges();

            if (activateOnce)
            {
                hasActivated = true;
            }
        }
    }

    public void ActivateStateChanges()
    {
        if (WorldStateManager.Instance == null || stateModifications == null)
            return;

        foreach (var mod in stateModifications)
        {
            // Si es una modificación de objeto en otra escena, verifica si la escena está cargada
            if (mod.type == ModificationType.SetObjectActive)
            {
                if (IsSceneLoaded(mod.objectSceneName))
                {
                    // La escena está cargada, aplica inmediatamente
                    ApplyModification(mod);
                }
                else
                {
                    // La escena no está cargada, guarda el cambio para aplicarlo cuando se cargue
                    Debug.Log($"Guardando cambio para objeto {mod.objectID} en escena {mod.objectSceneName} para aplicar después");

                    // Guardar el cambio en el WorldStateManager sin notificar a los listeners
                    string stateKey = $"Object_{mod.objectSceneName}_{mod.objectID}";
                    WorldStateManager.Instance.SetFlagWithoutNotification(stateKey, mod.flagValue);
                }
            }
            else
            {
                // Otras modificaciones se aplican normalmente
                ApplyModification(mod);
            }
        }
    }

    // Aplicar una modificación específica
    private void ApplyModification(StateModification mod)
    {
        switch (mod.type)
        {
            case ModificationType.SetFlag:
                WorldStateManager.Instance.SetFlag(mod.stateID, mod.flagValue);
                break;

            case ModificationType.SetCounter:
                WorldStateManager.Instance.SetCounter(mod.stateID, mod.counterValue);
                break;

            case ModificationType.IncrementCounter:
                WorldStateManager.Instance.IncrementCounter(mod.stateID, mod.counterValue);
                break;

            case ModificationType.SetString:
                WorldStateManager.Instance.SetString(mod.stateID, mod.stringValue);
                break;

            case ModificationType.SetObjectActive:
                WorldStateManager.Instance.SetObjectActive(
                    mod.objectSceneName,
                    mod.objectID,
                    mod.flagValue);
                break;
        }
    }
    private bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name == sceneName)
            {
                return true;
            }
        }
        return false;
    }
    public void ResetActivationState()
    {
        hasActivated = false;
    }
}