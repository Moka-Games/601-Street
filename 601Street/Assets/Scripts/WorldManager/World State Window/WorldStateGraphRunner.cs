// WorldStateGraphRunner.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;

public class WorldStateGraphRunner : MonoBehaviour
{
    public WorldStateGraph stateGraph;
    [SerializeField] private bool activateInitialStateOnStart = true;

    private string currentStateID;
    private Dictionary<string, WorldStateNode> nodeCache = new Dictionary<string, WorldStateNode>();

    // Evento para notificar cambios de estado
    public event Action<string, string> OnStateChanged; // oldState -> newState

    [Header("Configuración de Misiones")]
    [SerializeField] private bool completarMisionAlCambiarEstado = false;
    [SerializeField] private bool soloCompletarMisionSiEsDelNodoActual = true;

    private void Start()
    {
        if (stateGraph == null)
        {
            Debug.LogError("No WorldStateGraph assigned!");
            return;
        }

        // Construye caché para acceso rápido
        BuildNodeCache();

        if (activateInitialStateOnStart)
        {
            ActivateInitialState();
        }
    }

    private void BuildNodeCache()
    {
        nodeCache.Clear();

        if (stateGraph == null || stateGraph.nodes == null)
            return;

        foreach (var node in stateGraph.nodes)
        {
            nodeCache[node.id] = node;
        }
    }

    public void ActivateInitialState()
    {
        if (stateGraph == null) return;

        // Buscar el nodo inicial
        WorldStateNode initialNode = null;

        if (!string.IsNullOrEmpty(stateGraph.initialNodeID))
        {
            initialNode = stateGraph.FindNodeByID(stateGraph.initialNodeID);
        }

        // Si no hay nodo inicial establecido, buscar por propiedad isInitialNode
        if (initialNode == null)
        {
            foreach (var node in stateGraph.nodes)
            {
                if (node.isInitialNode)
                {
                    initialNode = node;
                    break;
                }
            }
        }

        // Si todavía no hay nodo inicial, usar el primero
        if (initialNode == null && stateGraph.nodes.Count > 0)
        {
            initialNode = stateGraph.nodes[0];
        }

        if (initialNode != null)
        {
            ApplyStateActivations(initialNode);
            currentStateID = initialNode.id;
            OnStateChanged?.Invoke("", currentStateID);
        }
        else
        {
            Debug.LogWarning("No initial state found in graph!");
        }
    }

    public bool ActivateState(string stateID)
    {
        if (!nodeCache.ContainsKey(stateID))
        {
            return false;
        }

        // Si ya estamos en este estado, no hacer nada
        if (currentStateID == stateID)
            return true;

        string oldStateID = currentStateID;
        WorldStateNode newState = nodeCache[stateID];
        WorldStateNode currentState = null;

        // Verifica si el cambio es válido (solo adyacentes)
        if (!string.IsNullOrEmpty(currentStateID))
        {
            currentState = nodeCache[currentStateID];

            // Si no es adyacente, error
            if (!currentState.connectedNodeIDs.Contains(stateID))
            {
                return false;
            }

            // Completar misión actual si está configurado
            if (completarMisionAlCambiarEstado && MisionManager.Instance != null && MisionManager.Instance.TieneMisionActiva)
            {
                // Si solo queremos completar cuando la misión es del nodo actual
                if (soloCompletarMisionSiEsDelNodoActual)
                {
                    // Verificar si la misión actual es la misma que la asociada al nodo actual
                    if (currentState.misionAsociada != null &&
                        MisionManager.Instance.MisionActual != null &&
                        MisionManager.Instance.MisionActual.ID == currentState.misionAsociada.ID)
                    {
                        MisionManager.Instance.CompletarMisionActual();
                    }
                }
                else
                {
                    MisionManager.Instance.CompletarMisionActual();
                }
            }
        }

        // Aplica cambios de estado
        ApplyStateActivations(newState);

        // Actualiza estado actual
        currentStateID = stateID;

        // Notifica el cambio
        OnStateChanged?.Invoke(oldStateID, currentStateID);
        print("State Changed");

        return true;
    }
    public bool ActivateNextState()
    {
        if (string.IsNullOrEmpty(currentStateID))
        {
            Debug.LogError("No current state to transition from!");
            return false;
        }

        WorldStateNode currentState = nodeCache[currentStateID];

        // Si hay múltiples conexiones, error
        if (currentState.connectedNodeIDs.Count > 1)
        {
            return false;
        }

        // Si no hay conexiones, es estado final
        if (currentState.connectedNodeIDs.Count == 0)
        {
            return false;
        }

        // Activa el único siguiente estado
        return ActivateState(currentState.connectedNodeIDs[0]);
    }

    private void ApplyStateActivations(WorldStateNode state)
    {
        if (WorldStateManager.Instance == null)
        {
            return;
        }

        // Obtener el nombre de la escena actual
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // Activar objetos de la lista "activeObjectIDs"
        foreach (var objectID in state.activeObjectIDs)
        {
            WorldStateManager.Instance.SetObjectActive(currentScene, objectID, true);
        }

        // Desactivar objetos de la lista "inactiveObjectIDs"
        foreach (var objectID in state.inactiveObjectIDs)
        {
            WorldStateManager.Instance.SetObjectActive(currentScene, objectID, false);
        }

        // Iniciar la misión asociada al nodo si existe
        if (state.misionAsociada != null)
        {
            // Verificar si MisionManager está disponible
            if (MisionManager.Instance != null)
            {
                // Si hay un retraso, usar una corrutina
                if (state.misionDelay > 0)
                {
                    StartCoroutine(IniciarMisionConDelay(state.misionAsociada, state.misionDelay, state.name));
                }
                else
                {
                    // Iniciar la misión inmediatamente
                    MisionManager.Instance.IniciarMision(state.misionAsociada);
                }
            }
            else
            {
                Debug.LogError("MisionManager no está disponible. No se puede iniciar la misión asociada.");
            }
        }
    }

    // Añadir esta nueva corrutina para manejar el retraso
    private IEnumerator IniciarMisionConDelay(Mision mision, float delay, string nombreEstado)
    {
                                                     
        yield return new WaitForSeconds(delay);

        // Verificar que aún podemos iniciar la misión (el usuario podría haber cambiado de estado durante la espera)
        if (MisionManager.Instance != null)
        {
            Debug.Log($"Iniciando misión '{mision.Nombre}' asociada al estado '{nombreEstado}' después de {delay} segundos");
            MisionManager.Instance.IniciarMision(mision);
        }
    }
    public void OnSceneLoaded(string sceneName)
    {

        if (string.IsNullOrEmpty(currentStateID))
        {
            Debug.LogWarning("No hay estado actual definido. Activando el estado inicial para la nueva escena.");
            ActivateInitialState();
            return;
        }

        // Buscar el nodo actual y reaplicarlo específicamente para esta escena
        WorldStateNode currentNode = stateGraph.FindNodeByID(currentStateID);
        if (currentNode != null)
        {
            Debug.Log($"Reaplicando estado '{currentNode.name}' a la escena: {sceneName}");
            ApplyStateActivationsForScene(currentNode, sceneName);
        }
        else
        {
            Debug.LogError($"No se pudo encontrar el nodo con ID: {currentStateID}");
        }
    }

    private void ApplyStateActivationsForScene(WorldStateNode state, string sceneName)
    {
        if (WorldStateManager.Instance == null)
        {
            Debug.LogError("WorldStateManager is not available!");
            return;
        }


        // Activar objetos de la lista "activeObjectIDs" en la escena específica
        foreach (var objectID in state.activeObjectIDs)
        {
            WorldStateManager.Instance.SetObjectActive(sceneName, objectID, true);
            Debug.Log($"Activando objeto {objectID} en escena {sceneName}");
        }

        // Desactivar objetos de la lista "inactiveObjectIDs" en la escena específica
        foreach (var objectID in state.inactiveObjectIDs)
        {
            WorldStateManager.Instance.SetObjectActive(sceneName, objectID, false);
            Debug.Log($"Desactivando objeto {objectID} en escena {sceneName}");
        }

        // Iniciar la misión asociada al nodo si existe
        if (state.misionAsociada != null)
        {
            // Verificar si MisionManager está disponible
            if (MisionManager.Instance != null)
            {
                // Si hay un retraso, usar una corrutina
                if (state.misionDelay > 0)
                {
                    StartCoroutine(IniciarMisionConDelay(state.misionAsociada, state.misionDelay, state.name));
                }
                else
                {
                    // Iniciar la misión inmediatamente
                    Debug.Log($"Iniciando misión '{state.misionAsociada.Nombre}' asociada al estado '{state.name}'");
                    MisionManager.Instance.IniciarMision(state.misionAsociada);
                }
            }
            else
            {
                Debug.LogError("MisionManager no está disponible. No se puede iniciar la misión asociada.");
            }
        }
    }
    public void TestStateSystem()
    {

        // Imprimir información del estado actual
        string currentStateName = GetCurrentStateName();
        Debug.Log($"Estado actual: {currentStateName} (ID: {currentStateID})");

        // Listar todos los nodos en el grafo
        if (stateGraph != null && stateGraph.nodes != null)
        {
            Debug.Log($"Nodos disponibles en el grafo ({stateGraph.nodes.Count}):");
            foreach (var node in stateGraph.nodes)
            {
                string isInitial = node.isInitialNode ? " (INICIAL)" : "";
                Debug.Log($"- {node.name} (ID: {node.id}){isInitial}");
                Debug.Log($"  * Objetos activos: {string.Join(", ", node.activeObjectIDs)}");
                Debug.Log($"  * Objetos inactivos: {string.Join(", ", node.inactiveObjectIDs)}");
            }
        }

        // Buscar todos los listeners en todas las escenas
        WorldStateListener[] allListeners = FindObjectsByType<WorldStateListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"WorldStateListeners encontrados: {allListeners.Length}");
        foreach (var listener in allListeners)
        {
            Debug.Log($"- {listener.gameObject.name} (ID: {listener.ObjectID}) en escena: {listener.gameObject.scene.name}, Activo: {listener.gameObject.activeSelf}");
        }

        // Forzar actualización
        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log($"Forzando actualización para escena: {currentScene.name}");
        WorldStateManager.Instance.ApplyStateToScene(currentScene.name);
    }
    public void ApplyCurrentStateToScene(string sceneName)
    {
        if (string.IsNullOrEmpty(currentStateID))
        {
            Debug.LogWarning($"WorldStateGraphRunner: No hay estado actual definido para aplicar a escena {sceneName}");
            return;
        }

        WorldStateNode currentNode = stateGraph.FindNodeByID(currentStateID);
        if (currentNode != null)
        {
            Debug.Log($"WorldStateGraphRunner: Aplicando estado '{currentNode.name}' a escena {sceneName}");

            // Aplicar sólo a la escena especificada
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            // Activar objetos
            foreach (var objectID in currentNode.activeObjectIDs)
            {
                Debug.Log($"WorldStateGraphRunner: Activando objeto {objectID} en escena {sceneName}");
                WorldStateManager.Instance.SetObjectActive(sceneName, objectID, true);
            }

            // Desactivar objetos
            foreach (var objectID in currentNode.inactiveObjectIDs)
            {
                Debug.Log($"WorldStateGraphRunner: Desactivando objeto {objectID} en escena {sceneName}");
                WorldStateManager.Instance.SetObjectActive(sceneName, objectID, false);
            }
        }
        else
        {
            Debug.LogError($"WorldStateGraphRunner: No se encontró nodo con ID {currentStateID}");
        }
    }
    public bool CompletarMisionActualEIniciarSiguiente()
    {
        if (MisionManager.Instance != null && MisionManager.Instance.TieneMisionActiva)
        {
            // Completar misión actual
            MisionManager.Instance.CompletarMisionActual();

            // La siguiente misión será iniciada automáticamente por el nodo actual
            return true;
        }

        return false;
    }
    public string GetCurrentStateID()
    {
        return currentStateID;
    }

    // Método para obtener el nombre del estado actual
    public string GetCurrentStateName()
    {
        if (string.IsNullOrEmpty(currentStateID) || !nodeCache.ContainsKey(currentStateID))
            return "None";

        return nodeCache[currentStateID].name;
    }
}