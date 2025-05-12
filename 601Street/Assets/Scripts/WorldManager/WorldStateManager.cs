using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldStateManager : MonoBehaviour
{
    // Singleton para acceso global
    private static WorldStateManager instance;
    public static WorldStateManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = GameObject.Find("WorldStateManager");
                if (obj == null)
                {
                    obj = new GameObject("WorldStateManager");
                    obj.AddComponent<WorldStateManager>();
                }
                instance = obj.GetComponent<WorldStateManager>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    // Diccionarios para almacenar estados de diferentes tipos
    private Dictionary<string, bool> flagStates = new Dictionary<string, bool>();
    private Dictionary<string, int> counterStates = new Dictionary<string, int>();
    private Dictionary<string, string> stringStates = new Dictionary<string, string>();

    // Eventos para notificar cambios de estado
    public event Action<string, bool> OnFlagChanged;
    public event Action<string, int> OnCounterChanged;
    public event Action<string, string> OnStringChanged;

    // Registro de objetos que escuchan cambios específicos
    private Dictionary<string, List<Action<bool>>> flagListeners = new Dictionary<string, List<Action<bool>>>();
    private Dictionary<string, List<Action<int>>> counterListeners = new Dictionary<string, List<Action<int>>>();
    private Dictionary<string, List<Action<string>>> stringListeners = new Dictionary<string, List<Action<string>>>();
    private Dictionary<string, Dictionary<string, bool>> pendingObjectStates =
    new Dictionary<string, Dictionary<string, bool>>();


    private void Awake()
    {
        // Singleton setup
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Suscribirse a eventos de cambio de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Desuscribirse de eventos
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        Debug.Log($"Escena cargada: {sceneName}");

        // Primero aplicar estados pendientes
        ApplyPendingObjectStates(sceneName);

        // Luego notificar a los listeners normales
        ApplyStateToScene(sceneName);
    }

    // Aplicar estados a objetos en una escena
    public void ApplyStateToScene(string sceneName)
    {
        // Encontrar todos los WorldStateListener en la escena
        WorldStateListener[] listeners = FindObjectsByType<WorldStateListener>(FindObjectsInactive.Include, FindObjectsSortMode.None); 
        foreach (var listener in listeners)
        {
            listener.ApplyState();
        }
    }

    #region Flag State Methods

    // Obtener un estado booleano
    public bool GetFlag(string flagID, bool defaultValue = false)
    {
        if (flagStates.TryGetValue(flagID, out bool value))
        {
            return value;
        }
        return defaultValue;
    }

    // Establecer un estado booleano
    // Establecer un estado booleano
    public void SetFlag(string flagID, bool value)
    {
        bool hasChanged = !flagStates.TryGetValue(flagID, out bool currentValue) || currentValue != value;

        // Actualizar el valor
        flagStates[flagID] = value;

        // Notificar solo si cambió
        if (hasChanged)
        {
            Debug.Log($"Flag cambiado: {flagID} = {value}");

            // Notificar event global
            OnFlagChanged?.Invoke(flagID, value);

            // Notificar listeners específicos
            if (flagListeners.TryGetValue(flagID, out var listeners))
            {
                // Crear una copia de la lista de listeners para evitar problemas de modificación durante la iteración
                var listenersCopy = new List<Action<bool>>(listeners);

                foreach (var listener in listenersCopy)
                {
                    listener?.Invoke(value);
                }
            }
        }
    }

    // Registrar un listener para un flag específico
    public void RegisterFlagListener(string flagID, Action<bool> callback)
    {
        if (!flagListeners.ContainsKey(flagID))
        {
            flagListeners[flagID] = new List<Action<bool>>();
        }

        flagListeners[flagID].Add(callback);

        // Invocar inmediatamente con el valor actual
        if (flagStates.TryGetValue(flagID, out bool currentValue))
        {
            callback?.Invoke(currentValue);
        }
    }

    // Remover un listener
    public void RemoveFlagListener(string flagID, Action<bool> callback)
    {
        if (flagListeners.TryGetValue(flagID, out var listeners))
        {
            listeners.Remove(callback);
        }
    }

    #endregion

    #region Counter State Methods

    // Obtener un contador
    public int GetCounter(string counterID, int defaultValue = 0)
    {
        if (counterStates.TryGetValue(counterID, out int value))
        {
            return value;
        }
        return defaultValue;
    }

    // Establecer un contador
    public void SetCounter(string counterID, int value)
    {
        bool hasChanged = !counterStates.TryGetValue(counterID, out int currentValue) || currentValue != value;

        // Actualizar el valor
        counterStates[counterID] = value;

        // Notificar solo si cambió
        if (hasChanged)
        {
            Debug.Log($"Counter cambiado: {counterID} = {value}");

            // Notificar event global
            OnCounterChanged?.Invoke(counterID, value);

            // Notificar listeners específicos
            if (counterListeners.TryGetValue(counterID, out var listeners))
            {
                foreach (var listener in listeners)
                {
                    listener?.Invoke(value);
                }
            }
        }
    }

    // Incrementar un contador
    public void IncrementCounter(string counterID, int amount = 1)
    {
        int currentValue = GetCounter(counterID);
        SetCounter(counterID, currentValue + amount);
    }

    // Registrar un listener para un contador específico
    public void RegisterCounterListener(string counterID, Action<int> callback)
    {
        if (!counterListeners.ContainsKey(counterID))
        {
            counterListeners[counterID] = new List<Action<int>>();
        }

        counterListeners[counterID].Add(callback);

        // Invocar inmediatamente con el valor actual
        if (counterStates.TryGetValue(counterID, out int currentValue))
        {
            callback?.Invoke(currentValue);
        }
    }

    #endregion

    #region String State Methods

    // Obtener un valor string
    public string GetString(string stringID, string defaultValue = "")
    {
        if (stringStates.TryGetValue(stringID, out string value))
        {
            return value;
        }
        return defaultValue;
    }

    // Establecer un valor string
    public void SetString(string stringID, string value)
    {
        bool hasChanged = !stringStates.TryGetValue(stringID, out string currentValue) || currentValue != value;

        // Actualizar el valor
        stringStates[stringID] = value;

        // Notificar solo si cambió
        if (hasChanged)
        {
            Debug.Log($"String cambiado: {stringID} = {value}");

            // Notificar event global
            OnStringChanged?.Invoke(stringID, value);

            // Notificar listeners específicos
            if (stringListeners.TryGetValue(stringID, out var listeners))
            {
                foreach (var listener in listeners)
                {
                    listener?.Invoke(value);
                }
            }
        }
    }

    // Registrar un listener para un string específico
    public void RegisterStringListener(string stringID, Action<string> callback)
    {
        if (!stringListeners.ContainsKey(stringID))
        {
            stringListeners[stringID] = new List<Action<string>>();
        }

        stringListeners[stringID].Add(callback);

        // Invocar inmediatamente con el valor actual
        if (stringStates.TryGetValue(stringID, out string currentValue))
        {
            callback?.Invoke(currentValue);
        }
    }

    #endregion

    #region Scene Object Methods

    public void SetObjectActive(string sceneName, string objectID, bool active)
    {
        // Si no se proporciona nombre de escena, usar la escena actual
        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        // Verificar si la escena está cargada
        bool isSceneLoaded = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == sceneName && SceneManager.GetSceneAt(i).isLoaded)
            {
                isSceneLoaded = true;
                break;
            }
        }

        if (isSceneLoaded)
        {
            // La escena está cargada, aplicar inmediatamente
            string stateKey = $"Object_{sceneName}_{objectID}";
            SetFlag(stateKey, active);
            Debug.Log($"Escena {sceneName} cargada, cambio aplicado inmediatamente: {objectID} = {active}");

            // Si es la escena actual, intentar encontrar el objeto y aplicar el estado directamente
            if (sceneName == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
            {
                WorldStateListener[] listeners = FindObjectsByType<WorldStateListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var listener in listeners)
                {
                    if (listener.ObjectID == objectID)
                    {
                        listener.ApplyState();
                        break;
                    }
                }
            }
        }
        else
        {
            // La escena no está cargada, registrar para aplicar después
            RegisterPendingObjectState(sceneName, objectID, active);

            // También actualizamos el flag sin notificación
            string stateKey = $"Object_{sceneName}_{objectID}";
            SetFlagWithoutNotification(stateKey, active);
        }
    }

    // Obtener el estado de un objeto
    public bool GetObjectActive(string sceneName, string objectID, bool defaultState = true)
    {
        string stateKey = $"Object_{sceneName}_{objectID}";
        return GetFlag(stateKey, defaultState);
    }

    #endregion

    #region NPC Methods

    // Cambiar diálogo de NPC
    public void SetNPCDialogue(string sceneName, string npcID, string dialogueID)
    {
        string stateKey = $"NPCDialogue_{sceneName}_{npcID}";
        SetString(stateKey, dialogueID);
    }

    // Obtener diálogo actual de un NPC
    public string GetNPCDialogue(string sceneName, string npcID, string defaultDialogue = "")
    {
        string stateKey = $"NPCDialogue_{sceneName}_{npcID}";
        return GetString(stateKey, defaultDialogue);
    }

    // Activar/desactivar un NPC
    public void SetNPCActive(string sceneName, string npcID, bool active)
    {
        SetObjectActive(sceneName, npcID, active);
    }

    #endregion

    #region Save / Load

    // Estructura para serialización
    [Serializable]
    private class SaveData
    {
        public Dictionary<string, bool> flags = new Dictionary<string, bool>();
        public Dictionary<string, int> counters = new Dictionary<string, int>();
        public Dictionary<string, string> strings = new Dictionary<string, string>();
    }

    // Guardar estado
    public void SaveState()
    {
        SaveData data = new SaveData
        {
            flags = flagStates,
            counters = counterStates,
            strings = stringStates
        };

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("WorldState", json);
        PlayerPrefs.Save();

        Debug.Log("Estado del mundo guardado");
    }

    // Cargar estado
    public void LoadState()
    {
        if (PlayerPrefs.HasKey("WorldState"))
        {
            string json = PlayerPrefs.GetString("WorldState");
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            flagStates = data.flags;
            counterStates = data.counters;
            stringStates = data.strings;

            // Notificar a todos los listeners
            ApplyStateToScene(SceneManager.GetActiveScene().name);

            Debug.Log("Estado del mundo cargado");
        }
    }

    // Reiniciar estado
    public void ResetState()
    {
        flagStates.Clear();
        counterStates.Clear();
        stringStates.Clear();

        Debug.Log("Estado del mundo reiniciado");
    }
    public void CleanupInvalidListeners()
    {
        // Limpiar listeners de flags
        foreach (var key in flagListeners.Keys.ToList())
        {
            flagListeners[key] = flagListeners[key].Where(listener =>
                listener != null && listener.Target != null).ToList();

            if (flagListeners[key].Count == 0)
            {
                flagListeners.Remove(key);
            }
        }
    }
    public void SetFlagWithoutNotification(string flagID, bool value)
    {
        flagStates[flagID] = value;

        Debug.Log($"Flag cambiado sin notificación: {flagID} = {value}");
    }
    private void RegisterPendingObjectState(string sceneName, string objectID, bool active)
    {
        // Inicializar el diccionario para la escena si no existe
        if (!pendingObjectStates.ContainsKey(sceneName))
        {
            pendingObjectStates[sceneName] = new Dictionary<string, bool>();
        }

        // Guardar el estado pendiente
        pendingObjectStates[sceneName][objectID] = active;

        Debug.Log($"Estado pendiente registrado para {objectID} en {sceneName}: {active}");
    }
    private void ApplyPendingObjectStates(string sceneName)
    {
        if (!pendingObjectStates.ContainsKey(sceneName))
            return;

        Debug.Log($"Aplicando {pendingObjectStates[sceneName].Count} estados pendientes para escena {sceneName}");

        // Obtener todos los objetos con WorldStateListener en la escena
        WorldStateListener[] listeners = FindObjectsByType<WorldStateListener>(
    FindObjectsInactive.Include,
    FindObjectsSortMode.None
);

        // Para cada estado pendiente, buscar y aplicar
        foreach (var pair in pendingObjectStates[sceneName])
        {
            string objectID = pair.Key;
            bool shouldBeActive = pair.Value;

            // Buscar el listener correspondiente
            WorldStateListener targetListener = null;
            foreach (var listener in listeners)
            {
                if (listener.ObjectID == objectID)
                {
                    targetListener = listener;
                    break;
                }
            }

            if (targetListener != null)
            {
                // Actualizar el estado directamente en el gameObject
                targetListener.gameObject.SetActive(shouldBeActive);
                Debug.Log($"Aplicado estado pendiente: {objectID} = {shouldBeActive}");

                // También configuramos el flag para futuras referencias
                string stateKey = $"Object_{sceneName}_{objectID}";
                SetFlag(stateKey, shouldBeActive);
            }
            else
            {
                Debug.LogWarning($"No se encontró objeto con ID {objectID} en escena {sceneName}");
            }
        }

        // Limpiar los estados pendientes para esta escena
        pendingObjectStates.Remove(sceneName);
    }
    public void ApplyStateToObject(string objectID, bool active)
    {
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string stateKey = $"Object_{currentSceneName}_{objectID}";

        // Actualizar el estado
        SetFlag(stateKey, active);

        // Buscar el objeto y actualizar su estado
        WorldStateListener[] listeners = FindObjectsByType<WorldStateListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var listener in listeners)
        {
            if (listener.ObjectID == objectID)
            {
                listener.ApplyState();
                Debug.Log($"Estado aplicado inmediatamente a {objectID}: {active}");
                break;
            }
        }
    }
    #endregion
}