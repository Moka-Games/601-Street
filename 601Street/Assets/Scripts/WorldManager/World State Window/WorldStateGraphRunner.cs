// WorldStateGraphRunner.cs
using UnityEngine;
using System;
using System.Collections.Generic;

public class WorldStateGraphRunner : MonoBehaviour
{
    [SerializeField] private WorldStateGraph stateGraph;
    [SerializeField] private bool activateInitialStateOnStart = true;

    private string currentStateID;
    private Dictionary<string, WorldStateNode> nodeCache = new Dictionary<string, WorldStateNode>();

    // Evento para notificar cambios de estado
    public event Action<string, string> OnStateChanged; // oldState, newState

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
            Debug.LogError($"State with ID {stateID} not found!");
            return false;
        }

        // Si ya estamos en este estado, no hacer nada
        if (currentStateID == stateID)
            return true;

        string oldStateID = currentStateID;
        WorldStateNode newState = nodeCache[stateID];

        // Verifica si el cambio es válido (solo adyacentes)
        if (!string.IsNullOrEmpty(currentStateID))
        {
            WorldStateNode currentState = nodeCache[currentStateID];

            // Si no es adyacente, error
            if (!currentState.connectedNodeIDs.Contains(stateID))
            {
                Debug.LogError($"Cannot transition from {currentState.name} to {newState.name} - not connected!");
                return false;
            }
        }

        // Aplica cambios de estado
        ApplyStateActivations(newState);

        // Actualiza estado actual
        currentStateID = stateID;

        // Notifica el cambio
        OnStateChanged?.Invoke(oldStateID, currentStateID);

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
            Debug.LogError($"State {currentState.name} has multiple outgoing connections. Please specify a target state ID.");
            return false;
        }

        // Si no hay conexiones, es estado final
        if (currentState.connectedNodeIDs.Count == 0)
        {
            Debug.LogWarning($"State {currentState.name} is a final state (no outgoing connections).");
            return false;
        }

        // Activa el único siguiente estado
        return ActivateState(currentState.connectedNodeIDs[0]);
    }

    // Método principal para aplicar un estado
    private void ApplyStateActivations(WorldStateNode state)
    {
        if (WorldStateManager.Instance == null)
        {
            Debug.LogError("WorldStateManager is not available!");
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
    }

    // Método para obtener el ID del estado actual
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