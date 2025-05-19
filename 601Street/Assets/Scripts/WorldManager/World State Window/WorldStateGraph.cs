using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WorldStateNode
{
    public string id;
    public string name;
    public Rect position;
    public List<string> activeObjectIDs = new List<string>();
    public List<string> inactiveObjectIDs = new List<string>();
    public List<string> connectedNodeIDs = new List<string>();
    public bool isInitialNode;

    // Constructor para crear nodos fácilmente
    public WorldStateNode(string nodeName, Vector2 pos)
    {
        id = System.Guid.NewGuid().ToString();
        name = nodeName;
        position = new Rect(pos.x, pos.y, 200, 150);
        isInitialNode = false;
    }
}

[System.Serializable]
public class WorldStateConnection
{
    public string fromNodeID;
    public string toNodeID;

    public WorldStateConnection(string from, string to)
    {
        fromNodeID = from;
        toNodeID = to;
    }
}

[CreateAssetMenu(fileName = "WorldStateGraph", menuName = "World State/Graph")]
public class WorldStateGraph : ScriptableObject
{
    public List<WorldStateNode> nodes = new List<WorldStateNode>();
    public List<WorldStateConnection> connections = new List<WorldStateConnection>();
    public string initialNodeID;

    public WorldStateNode FindNodeByID(string nodeID)
    {
        return nodes.Find(n => n.id == nodeID);
    }

    // Método auxiliar para encontrar conexiones de un nodo
    public List<WorldStateConnection> FindConnectionsFromNode(string nodeID)
    {
        return connections.FindAll(c => c.fromNodeID == nodeID);
    }
}