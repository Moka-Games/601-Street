using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldStateManager : MonoBehaviour
{
    // Singleton para acceso global
    private static WorldStateManager instance;
    private static bool isApplicationQuitting = false; // Nueva bandera

    public static WorldStateManager Instance
    {
        get
        {
            // Si la aplicación se está cerrando, no crear instancias
            if (isApplicationQuitting)
            {
                return null;
            }

            if (instance == null)
            {
                // Verificar si ya existe en la escena
                instance = FindFirstObjectByType<WorldStateManager>();

                if (instance == null)
                {
                    // Solo crear si no estamos en proceso de destrucción
                    if (!isApplicationQuitting && Application.isPlaying)
                    {
                        GameObject obj = new GameObject("WorldStateManager");
                        instance = obj.AddComponent<WorldStateManager>();
                        DontDestroyOnLoad(obj);
                    }
                }
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

        // Suscribirse a eventos de cambio de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnApplicationQuit()
    {
        // Marcar que la aplicación se está cerrando
        isApplicationQuitting = true;

        // Limpiar todos los listeners para evitar errores
        CleanupAllListeners();
    }

    private void OnDestroy()
    {
        // Desuscribirse de eventos
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Limpiar listeners si no es por cierre de aplicación
        if (!isApplicationQuitting)
        {
            CleanupAllListeners();
        }

        // Limpiar la instancia
        if (instance == this)
        {
            instance = null;
        }
    }

    private void CleanupAllListeners()
    {
        // Limpiar todos los eventos y listeners
        OnFlagChanged = null;
        OnCounterChanged = null;
        OnStringChanged = null;

        flagListeners.Clear();
        counterListeners.Clear();
        stringListeners.Clear();
        pendingObjectStates.Clear();
    }

    // Método estático para verificar si el manager está disponible de forma segura
    public static bool IsAvailable()
    {
        return !isApplicationQuitting && Instance != null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Solo procesar si no estamos cerrando la aplicación
        if (isApplicationQuitting) return;

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
        // No hacer nada si la aplicación se está cerrando
        if (isApplicationQuitting) return;

        // Verificar si la escena está realmente cargada
        bool isSceneLoaded = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == sceneName && SceneManager.GetSceneAt(i).isLoaded)
            {
                isSceneLoaded = true;
                break;
            }
        }

        if (!isSceneLoaded)
        {
            Debug.LogWarning($"WorldStateManager: La escena {sceneName} no está cargada. No se pueden aplicar estados.");
            return;
        }

        Debug.Log($"WorldStateManager: Buscando WorldStateListeners en escena: {sceneName}");

        // Buscar todos los objetos con WorldStateListener en la escena específica
        WorldStateListener[] allListeners = FindObjectsByType<WorldStateListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int applyCount = 0;

        foreach (var listener in allListeners)
        {
            if (listener != null && listener.gameObject != null && listener.gameObject.scene.name == sceneName)
            {
                Debug.Log($"Aplicando estado a listener: {listener.gameObject.name} con ID: {listener.ObjectID} en escena {sceneName}");
                listener.ApplyState();
                applyCount++;
            }
        }

        Debug.Log($"WorldStateManager: Se aplicó estado a {applyCount} objetos en la escena {sceneName}");

        // Aplicar también cualquier estado pendiente para esta escena
        ApplyPendingObjectStates(sceneName);
    }

    #region Flag State Methods

    public bool GetFlag(string flagID, bool defaultValue = false)
    {
        if (flagStates.TryGetValue(flagID, out bool value))
        {
            return value;
        }
        return defaultValue;
    }

    public void SetFlag(string flagID, bool value)
    {
        // No hacer nada si la aplicación se está cerrando
        if (isApplicationQuitting) return;

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
                    try
                    {
                        listener?.Invoke(value);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Error al invocar listener para flag {flagID}: {e.Message}");
                    }
                }
            }
        }
    }

    public void RegisterFlagListener(string flagID, Action<bool> callback)
    {
        // No registrar si la aplicación se está cerrando
        if (isApplicationQuitting) return;

        if (!flagListeners.ContainsKey(flagID))
        {
            flagListeners[flagID] = new List<Action<bool>>();
        }

        flagListeners[flagID].Add(callback);

        // Invocar inmediatamente con el valor actual
        if (flagStates.TryGetValue(flagID, out bool currentValue))
        {
            try
            {
                callback?.Invoke(currentValue);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error al invocar callback inicial para flag {flagID}: {e.Message}");
            }
        }
    }

    public void RemoveFlagListener(string flagID, Action<bool> callback)
    {
        // No hacer nada si la aplicación se está cerrando
        if (isApplicationQuitting) return;

        if (flagListeners.TryGetValue(flagID, out var listeners))
        {
            listeners.Remove(callback);
        }
    }

    #endregion

    #region Counter State Methods

    public int GetCounter(string counterID, int defaultValue = 0)
    {
        if (counterStates.TryGetValue(counterID, out int value))
        {
            return value;
        }
        return defaultValue;
    }

    public void SetCounter(string counterID, int value)
    {
        if (isApplicationQuitting) return;

        bool hasChanged = !counterStates.TryGetValue(counterID, out int currentValue) || currentValue != value;

        counterStates[counterID] = value;

        if (hasChanged)
        {
            Debug.Log($"Counter cambiado: {counterID} = {value}");
            OnCounterChanged?.Invoke(counterID, value);

            if (counterListeners.TryGetValue(counterID, out var listeners))
            {
                foreach (var listener in listeners)
                {
                    try
                    {
                        listener?.Invoke(value);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Error al invocar listener para counter {counterID}: {e.Message}");
                    }
                }
            }
        }
    }

    public void IncrementCounter(string counterID, int amount = 1)
    {
        if (isApplicationQuitting) return;

        int currentValue = GetCounter(counterID);
        SetCounter(counterID, currentValue + amount);
    }

    public void RegisterCounterListener(string counterID, Action<int> callback)
    {
        if (isApplicationQuitting) return;

        if (!counterListeners.ContainsKey(counterID))
        {
            counterListeners[counterID] = new List<Action<int>>();
        }

        counterListeners[counterID].Add(callback);

        if (counterStates.TryGetValue(counterID, out int currentValue))
        {
            try
            {
                callback?.Invoke(currentValue);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error al invocar callback inicial para counter {counterID}: {e.Message}");
            }
        }
    }

    #endregion

    #region String State Methods

    public string GetString(string stringID, string defaultValue = "")
    {
        if (stringStates.TryGetValue(stringID, out string value))
        {
            return value;
        }
        return defaultValue;
    }

    public void SetString(string stringID, string value)
    {
        if (isApplicationQuitting) return;

        bool hasChanged = !stringStates.TryGetValue(stringID, out string currentValue) || currentValue != value;

        stringStates[stringID] = value;

        if (hasChanged)
        {
            Debug.Log($"String cambiado: {stringID} = {value}");
            OnStringChanged?.Invoke(stringID, value);

            if (stringListeners.TryGetValue(stringID, out var listeners))
            {
                foreach (var listener in listeners)
                {
                    try
                    {
                        listener?.Invoke(value);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Error al invocar listener para string {stringID}: {e.Message}");
                    }
                }
            }
        }
    }

    public void RegisterStringListener(string stringID, Action<string> callback)
    {
        if (isApplicationQuitting) return;

        if (!stringListeners.ContainsKey(stringID))
        {
            stringListeners[stringID] = new List<Action<string>>();
        }

        stringListeners[stringID].Add(callback);

        if (stringStates.TryGetValue(stringID, out string currentValue))
        {
            try
            {
                callback?.Invoke(currentValue);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error al invocar callback inicial para string {stringID}: {e.Message}");
            }
        }
    }

    #endregion

    #region Scene Object Methods

    public void SetObjectActive(string sceneName, string objectID, bool active)
    {
        if (isApplicationQuitting) return;

        if (string.IsNullOrEmpty(sceneName))
        {
            sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

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
            string stateKey = $"Object_{sceneName}_{objectID}";
            SetFlag(stateKey, active);
            Debug.Log($"Escena {sceneName} cargada, cambio aplicado inmediatamente: {objectID} = {active}");

            if (sceneName == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
            {
                WorldStateListener[] listeners = FindObjectsByType<WorldStateListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var listener in listeners)
                {
                    if (listener != null && listener.ObjectID == objectID)
                    {
                        listener.ApplyState();
                        break;
                    }
                }
            }
        }
        else
        {
            RegisterPendingObjectState(sceneName, objectID, active);
            string stateKey = $"Object_{sceneName}_{objectID}";
            SetFlagWithoutNotification(stateKey, active);
        }
    }

    public bool GetObjectActive(string sceneName, string objectID, bool defaultState = true)
    {
        string stateKey = $"Object_{sceneName}_{objectID}";
        return GetFlag(stateKey, defaultState);
    }

    #endregion

    #region NPC Methods

    public void SetNPCDialogue(string sceneName, string npcID, string dialogueID)
    {
        if (isApplicationQuitting) return;

        string stateKey = $"NPCDialogue_{sceneName}_{npcID}";
        SetString(stateKey, dialogueID);
    }

    public string GetNPCDialogue(string sceneName, string npcID, string defaultDialogue = "")
    {
        string stateKey = $"NPCDialogue_{sceneName}_{npcID}";
        return GetString(stateKey, defaultDialogue);
    }

    public void SetNPCActive(string sceneName, string npcID, bool active)
    {
        SetObjectActive(sceneName, npcID, active);
    }

    #endregion

    #region Save / Load

    [Serializable]
    private class SaveData
    {
        public Dictionary<string, bool> flags = new Dictionary<string, bool>();
        public Dictionary<string, int> counters = new Dictionary<string, int>();
        public Dictionary<string, string> strings = new Dictionary<string, string>();
    }

    public void SaveState()
    {
        if (isApplicationQuitting) return;

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

    public void LoadState()
    {
        if (isApplicationQuitting) return;

        if (PlayerPrefs.HasKey("WorldState"))
        {
            string json = PlayerPrefs.GetString("WorldState");
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            flagStates = data.flags;
            counterStates = data.counters;
            stringStates = data.strings;

            ApplyStateToScene(SceneManager.GetActiveScene().name);

            Debug.Log("Estado del mundo cargado");
        }
    }

    public void ResetState()
    {
        if (isApplicationQuitting) return;

        flagStates.Clear();
        counterStates.Clear();
        stringStates.Clear();

        Debug.Log("Estado del mundo reiniciado");
    }

    public void CleanupInvalidListeners()
    {
        if (isApplicationQuitting) return;

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
        if (isApplicationQuitting) return;

        flagStates[flagID] = value;
        Debug.Log($"Flag cambiado sin notificación: {flagID} = {value}");
    }

    private void RegisterPendingObjectState(string sceneName, string objectID, bool active)
    {
        if (isApplicationQuitting) return;

        if (!pendingObjectStates.ContainsKey(sceneName))
        {
            pendingObjectStates[sceneName] = new Dictionary<string, bool>();
        }

        pendingObjectStates[sceneName][objectID] = active;
        Debug.Log($"Estado pendiente registrado para {objectID} en {sceneName}: {active}");
    }

    private void ApplyPendingObjectStates(string sceneName)
    {
        if (isApplicationQuitting || !pendingObjectStates.ContainsKey(sceneName))
            return;

        Debug.Log($"Aplicando {pendingObjectStates[sceneName].Count} estados pendientes para escena {sceneName}");

        WorldStateListener[] listeners = FindObjectsByType<WorldStateListener>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var pair in pendingObjectStates[sceneName])
        {
            string objectID = pair.Key;
            bool shouldBeActive = pair.Value;

            WorldStateListener targetListener = null;
            foreach (var listener in listeners)
            {
                if (listener != null && listener.ObjectID == objectID)
                {
                    targetListener = listener;
                    break;
                }
            }

            if (targetListener != null)
            {
                targetListener.gameObject.SetActive(shouldBeActive);
                Debug.Log($"Aplicado estado pendiente: {objectID} = {shouldBeActive}");

                string stateKey = $"Object_{sceneName}_{objectID}";
                SetFlag(stateKey, shouldBeActive);
            }
            else
            {
                Debug.LogWarning($"No se encontró objeto con ID {objectID} en escena {sceneName}");
            }
        }

        pendingObjectStates.Remove(sceneName);
    }

    public void ApplyStateToObject(string objectID, bool active)
    {
        if (isApplicationQuitting) return;

        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string stateKey = $"Object_{currentSceneName}_{objectID}";

        SetFlag(stateKey, active);

        WorldStateListener[] listeners = FindObjectsByType<WorldStateListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var listener in listeners)
        {
            if (listener != null && listener.ObjectID == objectID)
            {
                listener.ApplyState();
                Debug.Log($"Estado aplicado inmediatamente a {objectID}: {active}");
                break;
            }
        }
    }

    public void ForceObjectState(string objectID, bool active, bool logDetails = true)
    {
        if (isApplicationQuitting) return;

        if (logDetails)
        {
            Debug.Log($"Forzando estado para objeto con ID {objectID} a {active}");
        }

        bool foundAny = false;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            string stateKey = $"Object_{scene.name}_{objectID}";

            SetFlag(stateKey, active);

            WorldStateListener[] listeners = FindObjectsByType<WorldStateListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var listener in listeners)
            {
                if (listener != null && listener.ObjectID == objectID)
                {
                    if (logDetails)
                    {
                        Debug.Log($"Aplicando estado forzado a {listener.gameObject.name} en escena {scene.name}");
                    }
                    listener.ApplyState();
                    foundAny = true;
                }
            }
        }

        if (!foundAny && logDetails)
        {
            Debug.LogWarning($"No se encontró ningún objeto con ID {objectID} en las escenas cargadas");
        }
    }

    #endregion
}